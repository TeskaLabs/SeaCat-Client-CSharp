﻿using seacat_wp_client.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace seacat_wp_client.Core
{
    public class FramePool
    {
        private Stack<ByteBuffer> stack = new Stack<ByteBuffer>();
        private int lowWaterMark;
        private int highWaterMark;
        private int frameCapacity;

        public static int DEFAULT_LOW_WATER_MARK = 16;
        public static int DEFAULT_HIGH_WATER_MARK = 40960;
        public static int DEFAULT_FRAME_CAPACITY = 16 * 1024;

        private int totalCount = 0;

        // Preference keys for this package
        private static String LOW_WATER_MARK = "lowWaterMark";
        private static String HIGH_WATER_MARK = "highWaterMark";
        private static String FRAME_CAPACITY = "frameCapacity";

        public FramePool()
        {
            this.lowWaterMark = DEFAULT_LOW_WATER_MARK;
            this.highWaterMark = DEFAULT_HIGH_WATER_MARK;
            this.frameCapacity = DEFAULT_FRAME_CAPACITY;
        }

        public FramePool(int lowWaterMark, int highWaterMark, int frameCapacity)
        {
            this.lowWaterMark = lowWaterMark;
            this.highWaterMark = highWaterMark;
            this.frameCapacity = frameCapacity;
        }


        public ByteBuffer Borrow(String reason)
        {
            ByteBuffer frame;
            try

            {
                lock (stack)

                {
                    frame = stack.Pop();
                }
            }
            catch (InvalidOperationException e)

            {
                if (totalCount >= highWaterMark) throw new IOException("No more available frames in the pool.");
                frame = CreateByteBuffer();
            }

            return frame;
        }

        
        public void GiveBack(ByteBuffer frame)
        {
            if (totalCount > lowWaterMark)
            {
                frame.Clear();
                Interlocked.Decrement(ref totalCount);
                // Discard frame
            }
            else
            {
                frame.Clear();
                lock (stack)
            {
                    stack.Push(frame);
                }
            }
        }


        private ByteBuffer CreateByteBuffer()
        {
            lock (this)
            {
                Interlocked.Increment(ref totalCount);
                ByteBuffer frame = new ByteBuffer(frameCapacity);
                return frame;
            }
        }


        public int Size()
        {
            lock (stack)
            {
                return stack.Count();
            }
        }


        public int Capacity()
        {
            return totalCount;
        }

        protected double before = 0;
        public void HeartBeat(double now)
        {
            /*
                    if (now > (before + 5))
                    {
                        before = now;
                        Log.d("SeaCat", "FramePool stats / size:"+size()+", capacity:"+capacity());
                    }
            */
        }
    }

}
