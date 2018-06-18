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
using SeaCatCSharpClient.Core;
using SeaCatCSharpClient.Interfaces;
using SeaCatCSharpClient.Utils;
using HttpRequestMessage = System.Net.Http.HttpRequestMessage;
using HttpResponseMessage = System.Net.Http.HttpResponseMessage;
using HttpStatusCode = System.Net.HttpStatusCode;

namespace SeaCatCSharpClient.Http {

    /// <summary>
    /// Http sender that passes HTTP requests to the Seacat flow. Contains I/O streams that process incoming frames.
    /// </summary>
    public class HttpSender : IFrameProvider, IStream {
        // ids, for debug purposes
        private static int idCounter;
        private static object idLock = new object();

        public int SenderId { get; set; }


        private Reactor reactor;
        private Uri uri;
        private System.Net.Http.HttpClient client;
        private bool launched = false;
        private InboundStream inboundStream;
        private OutboundStream outboundStream = null;
        private Task outboundStreamTask = null;
        
        private int streamId = -1;
        private int priority;

        private EventWaitHandle responseReady = new EventWaitHandle(false, EventResetMode.ManualReset);
        
        // request
        private HttpRequestMessage request;
        private Headers.Builder requestHeaders = new Headers.Builder();

        // response
        private HttpResponseMessage response;
        private Headers responseHeaders = null;
        private HttpStatusCode responseCode;
        private string responseMessage;

        public HttpSender(System.Net.Http.HttpClient client, Reactor reactor, int priority) {
            this.client = client;
            this.reactor = reactor;
            this.priority = priority;

            // increment id
            lock (idLock) {
                this.SenderId = idCounter;
                Interlocked.Increment(ref idCounter);
            }

            Logger.Debug(SeaCatInternals.HTTPTAG, $"HTTP handler; id: {SenderId}");
        }

        /// <summary>
        /// Passes asynchronously HTTP request into seacat flow and waits for a response
        /// </summary>
        /// <param name="request">request to process</param>
        /// <returns></returns>
        public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken) {
            this.uri = request.RequestUri;
            this.request = request;

            // init inbound stream
            this.inboundStream = new InboundStream(reactor, SenderId, client.Timeout.Milliseconds);

            // If request has a body, prepare outboundStream too
            HttpContent content = request.Content;
            if (content != null)
            {
                outboundStream = new OutboundStream(reactor, 1);
                outboundStream.ContentLength = (int)content.Headers.ContentLength;
                outboundStreamTask = content.CopyToAsync(outboundStream);
                TaskHelper.CreateTask("HTTP Outbound Task", () =>
                {
                    Task.WaitAll(outboundStreamTask);
                    outboundStream.Dispose();
                }).Start();
            }

            Logger.Debug(SeaCatInternals.HTTPTAG, $"H:{SenderId} URI: {this.uri}");

            // run in separate thread
            var tsk = TaskHelper.CreateTask<HttpResponseMessage>("HTTP Inbound Task", () => {
                // wait for response stream
                var inputStream = GetInputStream();

                // create response
                this.response = new HttpResponseMessage(responseCode);
                response.Content = new StreamContent(inputStream);
                response.RequestMessage = request;

#if DEBUG
                // for debug purposes, add id of this sender
                response.Content.Headers.Add("HANDLER-ID", SenderId.ToString());
#endif

                // pass all HTTP headers into output entity
                if (responseHeaders != null) {
                    foreach (var name in responseHeaders.Names()) {
                        // some headers should be put into content header collection
                        response.Content.Headers.TryAddWithoutValidation(name,
                          responseHeaders[name]);

                        response.Headers.TryAddWithoutValidation(name,
                            responseHeaders[name]);
                    }
                }

                return response;
            });

            // run the asynchronous task
            tsk.Start();
            return tsk;
        }
        
        public void Reset() {
            // received reset stream frame -> pass 500 as a response code
            Logger.Debug(SeaCatInternals.HTTPTAG, $"H:{SenderId} Reset stream");
            Dispose();

            responseCode = HttpStatusCode.InternalServerError;
            responseMessage = HttpStatus.GetMessage(500);
            responseReady.Set();
        }

        public void Dispose() {
            if (outboundStream != null) { outboundStream.Reset(); }
            inboundStream.Reset();
        }


        /// <summary>
        /// Returns a response body
        /// This is the place where request is isually executed
        /// </summary>
        /// <returns></returns>
        public InboundStream GetInputStream() {
            Launch();
            WaitForResponse();
            return inboundStream;
        }

        public string GetRequestProperty(string field) {
            lock (this) {
                if (field == null) return null;
                return requestHeaders.Get(field);
            }
        }

        /// <summary>
        /// Builds SYN STREAM frame and registers a new stream
        /// </summary>
        /// <returns></returns>
        public ByteBuffer BuildFrame(Reactor reactor, out bool keep) {
            lock (this) {
                Debug.Assert(this.reactor == reactor);

                // if there is no outbound stream, add FIN FLAG to the current frame
                bool finFlag = (outboundStream == null);

                // get a free frame
                ByteBuffer frame = reactor.FramePool.Borrow("HttpClientHandler.buildSYN_STREAM");

                // register a new stream and build the frame
                streamId = reactor.StreamFactory.RegisterStream(this);
                inboundStream.StreamId = streamId;
                SPDY.BuildALX1SynStream(frame, streamId, uri, request.Method.Method, GetRequestHeaders(), finFlag, priority);


                // If there is an outbound stream, launch that
                if (outboundStream != null) {
                    Debug.Assert((frame.GetByte(4) & SPDY.FLAG_FIN) == 0);
                    outboundStream.Launch(streamId);
                    Logger.Debug(SeaCatInternals.HTTPTAG, $"H:{SenderId} Sending SYN");
                } else {
                    Logger.Debug(SeaCatInternals.HTTPTAG, $"H:{SenderId} Sending FIN");
                    Debug.Assert((frame.GetByte(4) & SPDY.FLAG_FIN) == SPDY.FLAG_FIN);
                }

                keep = false;
                return frame;
            }
        }

        public int FrameProviderPriority => priority;

        public bool ReceivedALX1_SYN_REPLY(Reactor reactor, ByteBuffer frame, int frameLength, byte frameFlags) {

            // received HTTP response

            lock (this) {
                //TODO: Check stage - should disregards frames that come prior proper state
                Logger.Debug(SeaCatInternals.HTTPTAG, $"H:{SenderId} SYN REPLY of length {frameLength} arrived");

                // get response code and message
                int respCodeInt = frame.GetShort();
                responseCode = (HttpStatusCode)respCodeInt;
                responseMessage = HttpStatus.GetMessage(respCodeInt);
                Logger.Debug(SeaCatInternals.HTTPTAG, $"H:{SenderId} Response:: {respCodeInt}:{responseMessage}");

                // Reserved (unused) 16 bits
                frame.GetShort();

                // Parse response headers
                Headers.Builder headerBuilder = new Headers.Builder();

                while(frame.Position < frame.Limit) {
                    string key = SPDY.ParseVLEString(frame);
                    string val = SPDY.ParseVLEString(frame);
                    headerBuilder.Add(key, val);
                }

                responseHeaders = headerBuilder.Build();
                // response is now ready
                responseReady.Set();

                if ((frameFlags & SPDY.FLAG_FIN) == SPDY.FLAG_FIN) inboundStream.Dispose();
                return true;
            }
        }

        public bool ReceivedSPD3_RST_STREAM(Reactor reactor, ByteBuffer frame, int frameLength, byte frameFlags) {
            lock (this) {
                // reset stream received
                Logger.Debug(SeaCatInternals.HTTPTAG, $"H:{SenderId} RST STREAM ARRIVED");
                Reset();
                return true;
            }
        }

        public bool ReceivedDataFrame(Reactor reactor, ByteBuffer frame, int frameLength, byte frameFlags) {
            lock (this) {
                Logger.Debug(SeaCatInternals.HTTPTAG, $"H:{SenderId} DATA FRAME of length {frameLength} arrived");
                //TODO: Check stage - should disregards frames that come prior proper state
                bool ret = inboundStream.InboundData(frame);
                if ((frameFlags & SPDY.FLAG_FIN) == SPDY.FLAG_FIN) {
                    Logger.Debug(SeaCatInternals.HTTPTAG, $"H:{SenderId} FIN detected -> closing stream");
                    inboundStream.Dispose();
                }
                return ret;
            }
        }

        public void AddHeaders(IEnumerator<KeyValuePair<string, IEnumerable<string>>> enumerator)
        {
            while (enumerator.MoveNext())
            {
                var key_value = enumerator.Current;
                var valueEnum = key_value.Value.GetEnumerator();
                var value = "";
                while (valueEnum.MoveNext())
                {
                    value += valueEnum.Current + " ";
                }
                requestHeaders.Add(key_value.Key, value);
            }
        }

        public Headers GetRequestHeaders() {
            AddHeaders(this.request.Headers.GetEnumerator());
            AddHeaders(this.request.Content.Headers.GetEnumerator());
            lock (this) {
                return requestHeaders.Build();
            }
        }

        public int GetStreamId() { return streamId; }

        private void Launch() {
            if (!launched) {
                if (outboundStream != null) {
                    int contentLength = outboundStream.ContentLength;
                    if ((contentLength > 0) && (GetRequestProperty("Content-length") == null)) {
                        // If there is an outboundStream with data, we can determine Content-Length
                        requestHeaders.Set("Content-Length", contentLength.ToString());
                    }
                }

                launched = true;
                Logger.Debug(SeaCatInternals.HTTPTAG, $"H:{SenderId} Launched");
                reactor.RegisterFrameProvider(this, true);
            }
        }


        /// <summary>
        /// Blocks and waits for a response from the gateway
        /// Timeouts using IOException after connection timeout
        /// </summary>
        protected void WaitForResponse() {

            long timeoutMillis = client.Timeout.Milliseconds;
            if (timeoutMillis == 0) timeoutMillis = 1000 * 60 * 3; // 3 minutes timeout
            long cutOfTimeMillis = DateTimeOffset.Now.Millisecond + timeoutMillis;

            responseReady.Reset();

            try {
                if (!responseReady.WaitOne(TimeSpan.FromMilliseconds(cutOfTimeMillis))) {
                    Logger.Error(SeaCatInternals.HTTPTAG, $"H:{SenderId} Reponse didn't arrive!");
                    throw new TimeoutException("Connection timeout");
                }
            } finally {
                responseReady.Set();
            }
        }
    }
}
