using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SeaCatCSharpClient.Core;
using SeaCatCSharpClient.Interfaces;
using SeaCatCSharpClient.Utils;

namespace SeaCatCSharpClient.Http {

    /// <summary>
    /// Output stream that sends a request frames
    /// </summary>
    public class OutboundStream : Stream, IFrameProvider {

        private Reactor reactor;
        private int streamId = -1;
        private BlockingQueue<ByteBuffer> frameQueue = new BlockingQueue<ByteBuffer>();
        private ByteBuffer currentFrame = null;

        private bool closed = false;
        private int priority;
       
        public OutboundStream(Reactor reactor, int priority) {
            this.reactor = reactor;
            this.priority = priority;
        }

        public int WriteTimeoutMillis { get; set; } = 30 * 1000;
        public int ContentLength { get; set; } = 0;
        public int FrameProviderPriority => priority;

        public void Launch(int streamId) {
            if (this.streamId != -1) throw new IOException("OutputStream is already launched");
            this.streamId = streamId;
            reactor.RegisterFrameProvider(this, true);
        }

        /// <summary>
        /// This is emergency 'close' method -> terminate stream functionality at all cost with no damage
        /// </summary>
        public void Reset() {
            closed = true;

            while (!frameQueue.IsEmpty()) {
                ByteBuffer frame = frameQueue.Dequeue();
                reactor.FramePool.GiveBack(frame);
            }

            if (currentFrame != null) {
                reactor.FramePool.GiveBack(currentFrame);
                currentFrame = null;
            }
        }
        
        public new void Dispose() {
            if (closed) return; // Multiple calls to close() method are supported (and actually required)

            if (currentFrame == null) {
                // TODO: This means that sub-optimal flush()/close() cycle happened 
                // - we will have to send empty DATA frame with FIN_FLAG set
                GetCurrentFrame();
            }
            closed = true;
            FlushCurrentFrame(true);
        }


        public override void Flush() {
            if (currentFrame != null) FlushCurrentFrame(false);
        }

        public override Task FlushAsync(CancellationToken cancellationToken) {
            var tsk = TaskHelper.CreateTask("Flush", Flush);
            tsk.Start();
            return tsk;
        }

        public override int Read(byte[] buffer, int offset, int count) {
            throw new NotImplementedException("Not implemented!");
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
            throw new NotImplementedException("Not implemented!");
        }

        public override int ReadByte() {
            throw new NotImplementedException("Not implemented!");
        }

        public override long Seek(long offset, SeekOrigin loc) {
            throw new NotImplementedException("Not implemented!");
        }

        public override void SetLength(long value) {
            throw new NotImplementedException("Not implemented!");
        }

        public virtual byte[] ToArray() {
            throw new NotImplementedException("Not implemented!");
        }

        public override void Write(byte[] buffer, int offset, int count) {
            if (closed) throw new IOException("OutputStream is already closed");

            ByteBuffer frame = GetCurrentFrame();
            if (frame == null) throw new IOException("Frame not available");
            frame.PutBytes(buffer);
            ContentLength += count;

            if (frame.Remaining == 0) FlushCurrentFrame(false);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
            var tsk = TaskHelper.CreateTask<int>("Write outbound", () => {
                Write(buffer, offset, count);
                return count;
            });

            tsk.Start();
            return tsk;
        }

        public override void WriteByte(byte value) {
            if (closed) throw new IOException("OutputStream is already closed");

            ByteBuffer frame = GetCurrentFrame();
            if (frame == null) throw new IOException("Frame not available");
            frame.PutByte((byte)value);
            ContentLength += 1;

            if (frame.Remaining == 0) FlushCurrentFrame(false);
        }

        public virtual void WriteTo(Stream stream) {
            // TODO this should be implemented in the future
            throw new NotImplementedException("Not implemented!");
        }

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public virtual int Capacity {
            get { return GetCurrentFrame().Capacity; }
            set {
                throw new NotImplementedException("Not implemented!");
            }
        }

        public override long Length => GetCurrentFrame().Length;

        public override long Position {
            get { return GetCurrentFrame().Position; }
            set {
                GetCurrentFrame().Position = (int)value;
            }
        }

        public ByteBuffer BuildFrame(Reactor reactor, out bool keep) {

            keep = false;

            Debug.Assert(streamId > 0);

            // get the next frame and put id of this stream into it
            ByteBuffer frame = frameQueue.Dequeue();
            if (frame != null) {
                frame.PutInt(0, streamId);
                keep = !frameQueue.IsEmpty();
                if ((frame.GetShort(4) & SPDY.FLAG_FIN) == SPDY.FLAG_FIN) {
                    // never keep FIN frame
                    Debug.Assert(!keep);
                }
            }
            return frame;
        }
        
        private ByteBuffer GetCurrentFrame() {
            lock (this) {
                if (closed) throw new IOException("OutputStream is already closed");

                if (currentFrame == null) {
                    currentFrame = reactor.FramePool.Borrow("HttpOutputStream.getCurrentFrame");
                    currentFrame.Position = SPDY.HEADER_SIZE;
                }

                return currentFrame;
            }
        }

        /// <summary>
        /// Once the write procedure is finished, the stream adds current frame to the frame queue 
        /// and registers itself as a frame provider
        /// </summary>
        /// <param name="fin_flag"></param>
        private void FlushCurrentFrame(bool finFlag) {
            lock (this) {
                Debug.Assert(currentFrame != null);
                Debug.Assert(finFlag == closed);

                ByteBuffer aFrame = currentFrame;
                currentFrame = null;

                SPDY.BuildDataFrameFlagLength(aFrame, finFlag);

                long timeoutMillis = this.WriteTimeoutMillis;
                if (timeoutMillis == 0) timeoutMillis = 1000 * 60 * 3; // 3 minutes timeout
                long cutOfTimeMillis = DateTimeOffset.Now.Millisecond + timeoutMillis;

                bool res = false;

                while (res == false) {

                    long awaitMillis = cutOfTimeMillis - DateTimeOffset.Now.Millisecond;
                    if (awaitMillis <= 0) {
                        throw new TimeoutException($"Write timeout: {WriteTimeoutMillis}");
                    }

                    res = frameQueue.Enqueue(aFrame);
                }

                if (this.streamId != -1) reactor.RegisterFrameProvider(this, true);
            }
        }

    }

}
