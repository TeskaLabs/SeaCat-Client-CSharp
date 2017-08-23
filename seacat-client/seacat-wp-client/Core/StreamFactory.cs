using seacat_wp_client.Interfaces;
using seacat_wp_client.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace seacat_wp_client.Core {

    public class StreamFactory : IFrameConsumer, IFrameProvider {
        private static string TAG = "StreamFactory";
        private IntegerCounter streamIdSequence = new IntegerCounter(1); // Synchronized access via streams!
        private Dictionary<int, IStream> streams = new Dictionary<int, IStream>(); // Synchronized access!
        private BlockingQueue<ByteBuffer> outboundFrameQueue = new BlockingQueue<ByteBuffer>(); // Access to this element has to be synchronized

        ///

        public StreamFactory() {
        }

        ///

        public int RegisterStream(IStream stream) {
            lock (this) {
                int streamId = streamIdSequence.GetAndAdd(2);
                Debug.Assert(!streams.ContainsKey(streamId));
                streams.Add(streamId, stream);
                return streamId;
            }
        }

        public void UnregisterStream(int streamId) {
            lock (this) {
                streams.Remove(streamId);
            }
        }

        public void Reset() {
            lock (this) {
                foreach (var key in streams.Keys) {
                    var current = streams[key];
                    current.Reset();
                }

                streamIdSequence.Set(1);
                streams.Clear();
            }
        }

        protected IStream GetStream(int streamId) => streams[streamId];

        protected bool ReceivedALX1_SYN_REPLY(Reactor reactor, ByteBuffer frame, int frameLength, byte frameFlags) {
            int streamId = frame.GetInt();
            IStream stream = GetStream(streamId);
            if (stream == null) {
                Logger.Error(TAG, $"ReceivedALX1_SYN_REPLY stream not found {streamId} (can be closed already)");
                frame.Reset();
                SendRST_STREAM(frame, reactor, streamId, SPDY.RST_STREAM_STATUS_INVALID_STREAM);
                return false;
            }

            bool ret = stream.ReceivedALX1_SYN_REPLY(reactor, frame, frameLength, frameFlags);
            if ((frameFlags & SPDY.FLAG_FIN) == SPDY.FLAG_FIN) UnregisterStream(streamId);
            return ret;
        }


        protected bool ReceivedSPD3_RST_STREAM(Reactor reactor, ByteBuffer frame, int frameLength, byte frameFlags) {
            int streamId = frame.GetInt();
            IStream stream = GetStream(streamId);
            if (stream == null) {
                Logger.Error(TAG, $"receivedSPD3_RST_STREAM stream not found: {streamId} (can be closed already)");
                return true;
            }

            bool ret = stream.ReceivedSPD3_RST_STREAM(reactor, frame, frameLength, frameFlags);
            // Remove stream from active map
            UnregisterStream(streamId);
            return ret;
        }


        public bool ReceivedDataFrame(Reactor reactor, ByteBuffer frame) {
            int streamId = frame.GetInt();
            IStream stream = GetStream(streamId);
            if (stream == null) {
                Logger.Error(TAG, $"receivedDataFrame stream not found: {streamId} (can be closed already)");
                frame.Reset();
                SendRST_STREAM(frame, reactor, streamId, SPDY.RST_STREAM_STATUS_INVALID_STREAM);
                return false;
            }

            int frameLength = frame.GetInt();
            byte frameFlags = (byte)(frameLength >> 24);
            frameLength &= 0xffffff;

            bool ret = stream.ReceivedDataFrame(reactor, frame, frameLength, frameFlags);
            if ((frameFlags & SPDY.FLAG_FIN) == SPDY.FLAG_FIN) UnregisterStream(streamId);
            return ret;
        }

        public bool ReceivedControlFrame(Reactor reactor, ByteBuffer frame, int frameVersionType, int frameLength, byte frameFlags) {
            // Dispatch control frame
            if (frameVersionType == ((SPDY.CNTL_FRAME_VERSION_ALX1 << 16) | SPDY.CNTL_TYPE_SYN_REPLY)) {
                return ReceivedALX1_SYN_REPLY(reactor, frame, frameLength, frameFlags);
            } else if (frameVersionType == ((SPDY.CNTL_FRAME_VERSION_SPD3 << 16) | SPDY.CNTL_TYPE_RST_STREAM)) {
                return ReceivedSPD3_RST_STREAM(reactor, frame, frameLength, frameFlags);
            } else {
                Logger.Error(TAG, $"StreamFactory.receivedControlFrame cannot handle frame: {frameVersionType}");
                return true;
            }
        }


        public void SendRST_STREAM(ByteBuffer frame, Reactor reactor, int streamId, int statusCode) {
            SPDY.BuildSPD3RstStream(frame, streamId, statusCode);

            try {
                AddOutboundFrame(frame, reactor);
            } catch (IOException e) {
                reactor.FramePool.GiveBack(frame); // Return frame
                Logger.Error(TAG, e.Message);
            }
        }

        private void AddOutboundFrame(ByteBuffer frame, Reactor reactor) {
            outboundFrameQueue.Enqueue(frame);
            reactor.RegisterFrameProvider(this, true);
        }

        public ByteBuffer BuildFrame(Reactor reactor, out bool keep) {
            ByteBuffer frame;

            frame = outboundFrameQueue.Dequeue();
            keep = !outboundFrameQueue.IsEmpty();

            return frame;
        }

        public int GetFrameProviderPriority() => 1;

    }

}
