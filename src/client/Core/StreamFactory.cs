﻿using SeaCatCSharpClient.Interfaces;
using SeaCatCSharpClient.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SeaCatCSharpClient.Core {

    /// <summary>
    /// Factory that orchestrates streams which handle arrived frames.
    /// </summary>
    public class StreamFactory : IFrameConsumer, IFrameProvider {

        private static string TAG = "StreamFactory";
        private IntegerCounter streamIdSequence = new IntegerCounter(1);
        private Dictionary<int, IStream> streams = new Dictionary<int, IStream>();
        private BlockingQueue<ByteBuffer> outboundFrameQueue = new BlockingQueue<ByteBuffer>();

        public StreamFactory() {
        }

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

        /// <summary>
        /// Resets all streams
        /// </summary>
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
                // reset the stream and send INVALID status
                frame.Reset();
                SendRST_STREAM(frame, reactor, streamId, SPDY.RST_STREAM_STATUS_INVALID_STREAM);
                return false;
            }

            bool ret = stream.ReceivedALX1_SYN_REPLY(reactor, frame, frameLength, frameFlags);
            // if a FIN stream arrived, unregister the stream since it is no longer needed
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
                Logger.Error(TAG, $"ReceivedDataFrame: stream not found: {streamId} (can be closed already)");
                // reset the stream and send INVALID status
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
                // add frame into outbound queue
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
            return GetFrameFromOutboundQueue(reactor, out keep);
        }

        /// <summary>
        /// Gets a waiting frame from outbound queue
        /// </summary>
        /// <returns></returns>
        private ByteBuffer GetFrameFromOutboundQueue(Reactor reactor, out bool keep) {
            ByteBuffer frame;

            frame = outboundFrameQueue.Dequeue();
            keep = !outboundFrameQueue.IsEmpty();

            return frame;
        }

        public int FrameProviderPriority => 1;

    }

}
