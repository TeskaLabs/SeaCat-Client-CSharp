using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using seacat_winrt_client.Core;
using seacat_winrt_client.Interfaces;
using seacat_winrt_client.Utils;

namespace seacat_winrt_client.Http {

    public class InboundStream : MemoryStream {

        private Reactor reactor;
        private int streamId = -1;

        // TODO: Allow parametrization of LinkedBlockingQueue capacity
        private BlockingQueue<ByteBuffer> frameQueue = new BlockingQueue<ByteBuffer>();
        private ByteBuffer currentFrame = null;
        private bool closed = false;

        int readTimeoutMillis = 30 * 1000;

        static private ByteBuffer QUEUE_IS_CLOSED = new ByteBuffer(0);

        public InboundStream(Reactor reactor, int readTimeoutMillis) {
            this.reactor = reactor;
            this.readTimeoutMillis = readTimeoutMillis;
        }

        ~InboundStream() {
            if (currentFrame != null) {
                reactor.FramePool.GiveBack(currentFrame);
                currentFrame = null;
            }

            Dispose();
        }

        public void SetStreamId(int streamId) {
            this.streamId = streamId;
        }

        public void SetReadTimeout(int readTimeoutMillis) {
            this.readTimeoutMillis = readTimeoutMillis;
        }

        public bool InboundData(ByteBuffer frame) {
            if (closed) {
                // This stream is closed -> send RST_STREAM back
                frame.Clear();
                reactor.streamFactory.SendRST_STREAM(frame, reactor, this.streamId, SPDY.RST_STREAM_STATUS_STREAM_ALREADY_CLOSED);
                return false;
            }
            frameQueue.Enqueue(frame);
            return false; // We will return frame to pool on our own
        }

        protected ByteBuffer GetCurrentFrame() {
            if (currentFrame != null) {
                if (currentFrame == QUEUE_IS_CLOSED) return null;

                else if (currentFrame.Remaining == 0) {
                    reactor.FramePool.GiveBack(currentFrame);
                    currentFrame = null;
                } else return currentFrame;
            }

            long timeoutMillis = this.readTimeoutMillis;
            if (timeoutMillis == 0) timeoutMillis = 1000 * 60 * 3; // 3 minutes timeout
            long cutOfTimeMillis = DateTimeOffset.Now.Millisecond + timeoutMillis;

            while (currentFrame == null) {
                long awaitMillis = cutOfTimeMillis - DateTimeOffset.Now.Millisecond;
                if (awaitMillis <= 0) throw new TimeoutException($"Read timeout: {this.readTimeoutMillis}");

                bool success;
                currentFrame = frameQueue.Dequeue((int)awaitMillis, out success);

                if (!success) {
                    // TODO_RES Thread.CurrentThread.Interrupt
                    continue;
                }

                if (currentFrame == QUEUE_IS_CLOSED) {
                    frameQueue.Enqueue(QUEUE_IS_CLOSED);
                    currentFrame = null;
                    break;
                }
            }

            return currentFrame;
        }



        protected override void Dispose(bool disposing) {
            if (closed) return;
            closed = true;
            frameQueue.Enqueue(QUEUE_IS_CLOSED);
        }

        public void Reset() {
            //TODO: Set some kind of error  

            frameQueue.Enqueue(QUEUE_IS_CLOSED);
            while (frameQueue.Queue.Count > 1) {
                ByteBuffer frame = frameQueue.Dequeue();
                if (frame != QUEUE_IS_CLOSED) reactor.FramePool.GiveBack(frame);
            }

            if (currentFrame != null) {
                reactor.FramePool.GiveBack(currentFrame);
                currentFrame = null;
            }

            Dispose();
        }

        public override void Flush() {
            // nothing to do here
        }

        public override Task FlushAsync(CancellationToken cancellationToken) {
            var tsk = new Task(() => { });
            tsk.Start();
            return tsk;
        }

        public override int Read(byte[] buffer, int offset, int count) {
            if (offset < 0 || count < 0 || offset + count > buffer.Length) throw new IndexOutOfRangeException();

            ByteBuffer frame = GetCurrentFrame();
            if (frame == null) return -1;

            if (count > frame.Remaining) count = frame.Remaining;
            frame.GetBytes(buffer, offset, count);
            return count;
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
            var tsk = new Task<int>(() => {
                // TODO_REF use cancellation token
                Read(buffer, offset, count);
                return count;
            });

            tsk.Start();
            return tsk;
        }

        public override int ReadByte() {
            ByteBuffer frame = GetCurrentFrame();
            if (frame == null) return -1;
            return frame.GetByte();
        }

        public override long Seek(long offset, SeekOrigin loc) {
            switch (loc) {
                case SeekOrigin.Begin:
                Position = offset;
                break;
                case SeekOrigin.Current:
                Position += offset;
                break;
                case SeekOrigin.End:
                Position -= offset;
                break;
            }
            return Position;
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
            throw new NotImplementedException("Not implemented!");
        }

        public override void WriteByte(byte value) {
            throw new NotImplementedException("Not implemented!");
        }

        public virtual void WriteTo(Stream stream) {
            throw new NotImplementedException("Not implemented!");
        }

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => false;

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

    }

}
