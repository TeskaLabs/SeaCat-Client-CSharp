using SeaCatCSharpClient.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SeaCatCSharpClient.Core {
    
    /// <summary>
    /// Pool where frames not actually used are stored for future need
    /// </summary>
    public class FramePool {
        private static string TAG = "FramePool";
        private Stack<ByteBuffer> stack = new Stack<ByteBuffer>();
        private int lowWaterMark;
        private int highWaterMark;
        private int frameCapacity;

        public static int DEFAULT_LOW_WATER_MARK = 16;
        public static int DEFAULT_HIGH_WATER_MARK = 40960;
        public static int DEFAULT_FRAME_CAPACITY = 16 * 1024;

        protected double before = 0;
        private int totalCount = 0;

        public FramePool() {
            this.lowWaterMark = DEFAULT_LOW_WATER_MARK;
            this.highWaterMark = DEFAULT_HIGH_WATER_MARK;
            this.frameCapacity = DEFAULT_FRAME_CAPACITY;
        }

        public FramePool(int lowWaterMark, int highWaterMark, int frameCapacity) {
            this.lowWaterMark = lowWaterMark;
            this.highWaterMark = highWaterMark;
            this.frameCapacity = frameCapacity;
        }
        
        /// <summary>
        /// Borrows a frame for specific reason
        /// Note that all borrowed frames should be given back after some time
        /// </summary>
        /// <param name="reason"></param>
        /// <returns></returns>
        public ByteBuffer Borrow(String reason) {
            Logger.Debug(TAG, $"Borrowing frame; reason: {reason}");
            ByteBuffer frame;

            try {
                lock (stack) {
                    frame = stack.Pop();
                }
            } catch (InvalidOperationException) {
                if (totalCount >= highWaterMark) throw new IOException("No more available frames in the pool.");
                frame = CreateByteBuffer();
            }

            return frame;
        }

        /// <summary>
        /// Gives back a borrowed frame
        /// </summary>
        /// <param name="frame"></param>
        public void GiveBack(ByteBuffer frame) {
            Logger.Debug(TAG, $"Giving back frame of length: {frame.Length}");

            if (totalCount > lowWaterMark) {
                // Discard the frame since the pool has enough frames available
                frame.Reset();
                Interlocked.Decrement(ref totalCount);
            } else {
                // Store the frame back to the pool
                frame.Reset();
                lock (stack) {
                    stack.Push(frame);
                    Logger.Debug(TAG, $"Frames on the stack: {stack.Count}");
                }
            }
        }

        public int Size() {
            lock (this) {
                return stack.Count();
            }
        }

        public int Capacity() => totalCount;

        public void HeartBeat(double now) {
            // nothing to do here for now
        }

        private ByteBuffer CreateByteBuffer() {
            lock (this) {
                Interlocked.Increment(ref totalCount);
                Logger.Debug(TAG, $"Creating byte buffer; total count: {totalCount}");
                ByteBuffer frame = new ByteBuffer(frameCapacity);
                return frame;
            }
        }

    }

}
