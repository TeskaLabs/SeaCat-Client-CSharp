using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Web.Http;
using seacat_winrt_client.Core;
using seacat_winrt_client.Interfaces;
using seacat_winrt_client.Utils;
using HttpRequestMessage = System.Net.Http.HttpRequestMessage;
using HttpResponseMessage = System.Net.Http.HttpResponseMessage;
using HttpStatusCode = System.Net.HttpStatusCode;

namespace seacat_winrt_client.Http {

    public class SeacatHttpClientHandler : System.Net.Http.HttpClientHandler, IFrameProvider, IStream {

        private Reactor reactor;
        private Uri uri;
        private bool launched = false;
        protected InboundStream inboundStream;
        protected OutboundStream outboundStream = null;
        private Headers responseHeaders = null;
        private Headers.Builder requestHeaders = new Headers.Builder();
        private int streamId = -1;
        private int priority;

        private EventWaitHandle responseReady = new EventWaitHandle(false, EventResetMode.ManualReset);

        // todo set this value
        private System.Net.HttpStatusCode responseCode;
        private string responseMessage;
        private HttpRequestMessage request;

        public SeacatHttpClientHandler(Reactor reactor, int priority) {
            this.reactor = reactor;
            this.priority = priority;
        }

        public System.Net.Http.HttpClient HttpClient { get; set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken) {
            this.uri = request.RequestUri;
            this.inboundStream.SetReadTimeout(HttpClient.Timeout.Milliseconds);

            var tsk = new Task<HttpResponseMessage>(() => {
                var msg = new HttpResponseMessage();
                // wait for response stream
                var inputStream = GetInputStream();

                this.response = new HttpResponseMessage(responseCode);
                response.Content = new StreamContent(inputStream);

                foreach (var name in responseHeaders.Names()) {
                    response.Content.Headers.Add(name, responseHeaders.Get(name));
                }

                return msg;
            });

            tsk.Start();
            return tsk;
        }


        private void Launch() {
            if (!launched) {
                if (outboundStream != null) {
                    int contentLength = outboundStream.GetContentLength();
                    if ((contentLength > 0) && (GetRequestProperty("Content-length") == null)) {
                        // If there is an outboundStream with data, we can determine Content-Length
                        outboundStream.Dispose();

                        SetRequestProperty("Content-length", "" + contentLength);
                    }
                }

                launched = true;
                reactor.RegisterFrameProvider(this, true);
            }
        }

        public bool IsLaunched() {
            return launched;
        }

        /// <summary>
        /// Returns a response body
        /// This is the place where request is isually executed
        /// </summary>
        /// <returns></returns>
        public Stream GetInputStream() {
            Launch();
            WaitForResponse();
            return inboundStream;
        }

        /// <summary>
        /// Returns a response status code
        /// This method triggers an actual request (if needed) and waits for response code from server
        /// </summary>
        /// <returns></returns>
        public HttpStatusCode GetResponseCode() {
            Launch();
            WaitForResponse();
            return responseCode;
        }

        /// <summary>
        /// Blocks and waits for a response from the gateway
        /// Timeouts using IOException after connection timeout
        /// </summary>
        protected void WaitForResponse() {
            long timeoutMillis = HttpClient.Timeout.Milliseconds;
            if (timeoutMillis == 0) timeoutMillis = 1000 * 60 * 3; // 3 minutes timeout
            long cutOfTimeMillis = DateTimeOffset.Now.Millisecond + timeoutMillis;

            responseReady.Reset();

            try {
                if (!responseReady.WaitOne(TimeSpan.FromMilliseconds(cutOfTimeMillis))) {
                    throw new TimeoutException("Connection timeout");
                }
            } finally {
                responseReady.Set();
            }
        }

        public Stream GetOutputStream() {
            lock (this) {
                if (request.Method.Method == "GET") {
                    request.Method = new System.Net.Http.HttpMethod("POST"); // Backward compatibility
                }

                if (outboundStream == null) {
                    if (launched) throw new IOException("Cannot write output after reading input.");
                    outboundStream = new OutboundStream(this.reactor, this.priority);
                }
                return outboundStream;
            }
        }


        public string GetRequestProperty(string field) {
            lock (this) {
                if (field == null) return null;
                return requestHeaders.Get(field);
            }
        }


        public ByteBuffer BuildFrame(Reactor reactor, out bool keep) {
            lock (this) {
                Debug.Assert(this.reactor == reactor);

                // TODO_RES distinguish fixed content length

                //TODO_RES: which request property belongs to windows?
                AddRequestProperty("X-SC-SDK", "win");

                // Add If-Modified-Since header
                // TODO_RES -> where to get this attribute?
                //long ifModifiedSince = getIfModifiedSince();
                //if (ifModifiedSince != 0) addRequestProperty("If-Modified-Since", HttpDate.format(new Date(ifModifiedSince)));

                bool fin_flag = (outboundStream == null);

                ByteBuffer frame = reactor.FramePool.Borrow("HttpClientHandler.buildSYN_STREAM");

                streamId = reactor.streamFactory.RegisterStream(this);
                inboundStream.SetStreamId(streamId);

                // Build SYN_STREAM frame
                SPDY.BuildALX1SynStream(frame, streamId, uri, request.Method.Method, GetRequestHeaders(), fin_flag,
                    this.priority);

                // If there is outbound stream, launch that
                if (outboundStream != null) {
                    Debug.Assert((frame.GetShort(4) & SPDY.FLAG_FIN) == 0);
                    outboundStream.Launch(streamId);
                } else {
                    Debug.Assert((frame.GetShort(4) & SPDY.FLAG_FIN) == SPDY.FLAG_FIN);
                }

                keep = false;
                return frame;
            }
        }


        public int GetFrameProviderPriority() {
            return this.priority;
        }

        public void Reset() {
            if (outboundStream != null) { outboundStream.Reset(); }
            inboundStream.Reset();
        }

        private HttpResponseMessage response;

        public bool ReceivedALX1_SYN_REPLY(Reactor reactor, ByteBuffer frame, int frameLength, byte frameFlags) {
            lock (this) {
                //TODO: Check stage - should disregards frames that come prior proper state

                // Status
                int respCodeInt = frame.GetShort();
                responseCode = (HttpStatusCode)respCodeInt;
                responseMessage = HttpStatus.GetMessage(respCodeInt);

                // Reserved (unused) 16 bits
                frame.GetShort();

                // Parse response headers
                Headers.Builder headerBuilder = new Headers.Builder();
                headerBuilder.Add(null, "HTTP/1.1 " + responseCode + " " + responseMessage); // To mimic HttpURLConnection behaviour

                for (; frame.Position < frame.Limit;) {
                    String k = SPDY.ParseVLEString(frame);
                    String v = SPDY.ParseVLEString(frame);
                    headerBuilder.Add(k, v);
                }
                responseHeaders = headerBuilder.Build();
                responseReady.Set();

                if ((frameFlags & SPDY.FLAG_FIN) == SPDY.FLAG_FIN) inboundStream.Dispose();
                return true;
            }
        }

        public bool ReceivedSPD3_RST_STREAM(Reactor reactor, ByteBuffer frame, int frameLength, byte frameFlags) {
            lock (this) {
                Reset();
                return true;
            }
        }

        public bool ReceivedDataFrame(Reactor reactor, ByteBuffer frame, int frameLength, byte frameFlags) {
            lock (this) {
                //TODO: Check stage - should disregards frames that come prior proper state
                bool ret = inboundStream.InboundData(frame);
                if ((frameFlags & SPDY.FLAG_FIN) == SPDY.FLAG_FIN) inboundStream.Dispose();
                return ret;
            }
        }


        public Headers GetRequestHeaders() {
            lock (this) {
                return requestHeaders.Build();
            }
        }

        public void SetRequestProperty(String field, String newValue) {
            lock (this) {
                //TODO: Consider this: if (stage >= HEADER_SENT) throw new IllegalStateException("Cannot set request property after connection is made");
                if (field == null) {
                    throw new NullReferenceException("field == null");
                }

                if (newValue == null) {
                    // Silently ignore null header values for backwards compatibility with older
                    // android versions as well as with other URLConnection implementations.
                    //
                    // Some implementations send a malformed HTTP header when faced with
                    // such requests, we respect the spec and ignore the header.
                    return;
                }

                requestHeaders.Set(field, newValue);
            }
        }

        public void AddRequestProperty(String field, String newValue) {
            lock (this) {
                //TODO: Consider this: if (stage >= HEADER_SENT) throw new IllegalStateException("Cannot set request property after connection is made");

                if (field == null) {
                    throw new NullReferenceException("field == null");
                }

                if (newValue == null) {
                    // Silently ignore null header values for backwards compatibility with older
                    // android versions as well as with other URLConnection implementations.
                    //
                    // Some implementations send a malformed HTTP header when faced with
                    // such requests, we respect the spec and ignore the header.
                    return;
                }

                requestHeaders.Add(field, newValue);
            }
        }


        public int GetStreamId() { return streamId; }
    }
}
