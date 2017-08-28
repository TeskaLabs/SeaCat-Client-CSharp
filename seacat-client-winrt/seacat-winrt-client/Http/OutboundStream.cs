using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using seacat_winrt_client.Core;
using seacat_winrt_client.Interfaces;
using seacat_winrt_client.Utils;

namespace seacat_winrt_client.Http {

    public class OutboundStream : Stream, IFrameProvider {

        private Reactor reactor;
        private int streamId = -1;

        // TODO: Allow parametrization of LinkedBlockingQueue capacity
        private BlockingQueue<ByteBuffer> frameQueue = new BlockingQueue<ByteBuffer>();
        private ByteBuffer currentFrame = null;

        private bool closed = false;

        private int contentLength = 0;
        private int priority;

        int writeTimeoutMillis = 30 * 1000;


        public OutboundStream(Reactor reactor, int priority) {
            this.reactor = reactor;
            this.priority = priority;
        }

        public void Launch(int streamId) {
            if (this.streamId != -1) throw new IOException("OutputStream is already launched");
            this.streamId = streamId;
            reactor.RegisterFrameProvider(this, true);
        }

        private ByteBuffer GetCurrentFrame() {
            lock (this) {
                if (closed) throw new IOException("OutputStream is already closed");

                if (currentFrame == null) {
                    currentFrame = reactor.FramePool.Borrow("HttpOutputStream.getCurrentFrame");
                    // TODO_RES Make sure that there is a space for DATA header
                    currentFrame.Position = SPDY.HEADER_SIZE;
                }

                return currentFrame;
            }
        }

        private void FlushCurrentFrame(bool fin_flag) {
            lock (this) {
                Debug.Assert(currentFrame != null);
                Debug.Assert(fin_flag == closed);

                ByteBuffer aFrame = currentFrame;
                currentFrame = null;

                SPDY.BuildDataFrameFlagLength(aFrame, fin_flag);

                long timeoutMillis = this.writeTimeoutMillis;
                if (timeoutMillis == 0) timeoutMillis = 1000 * 60 * 3; // 3 minutes timeout
                long cutOfTimeMillis = DateTimeOffset.Now.Millisecond + timeoutMillis;

                bool res = false;

                while (res == false) {

                    long awaitMillis = cutOfTimeMillis - DateTimeOffset.Now.Millisecond;
                    if (awaitMillis <= 0) {
                        throw new TimeoutException($"Write timeout: {writeTimeoutMillis}");
                    }

                    res = frameQueue.Enqueue(aFrame);

                    /*
                    TODO_RES: use waiting timeout    
                
                    bool success;
                    res = frameQueue.Enqueue(aFrame, awaitMillis, out success);


                    if (!success) {
                        // TODO_RES Thread.CurrentThread.Interrupt
                        continue;
                    }*/
                }

                if (this.streamId != -1) reactor.RegisterFrameProvider(this, true);
            }
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

        public int GetContentLength() {
            return contentLength;
        }

        public void Dispose() {
            if (closed) return; // Multiple calls to close() method are supported (and actually required)

            if (currentFrame == null) {
                // TODO: This means that sub-optimal flush()/close() cycle happened - we will have to send empty DATA frame with FIN_FLAG set
                GetCurrentFrame();
            }
            closed = true;
            FlushCurrentFrame(true);
        }


        public override void Flush() {
            if (currentFrame != null) FlushCurrentFrame(false);
        }

        public override Task FlushAsync(CancellationToken cancellationToken) {
            var tsk = new Task(Flush);
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
            throw new NotImplementedException("Not implemented!");
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
            var tsk = new Task<int>(() => {
                // TODO_REF use cancellation token
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
            contentLength += 1;

            if (frame.Remaining == 0) FlushCurrentFrame(false);
        }

        public virtual void WriteTo(Stream stream) {
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

            ByteBuffer frame = frameQueue.Dequeue();
            if (frame != null) {
                frame.PutInt(0, streamId);
                keep = !frameQueue.IsEmpty();
                if ((frame.GetShort(4) & SPDY.FLAG_FIN) == SPDY.FLAG_FIN) {
                    Debug.Assert(keep == false);
                }
            }
            return frame;
        }

        public int GetFrameProviderPriority() {
            return priority;
        }
    }

}
