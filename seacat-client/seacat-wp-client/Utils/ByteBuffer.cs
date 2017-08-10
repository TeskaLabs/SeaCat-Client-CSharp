using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace seacat_wp_client.Utils
{
    public class ByteBuffer
    {
        int capacity;
        MemoryStream stream;
        BinaryReader reader;
        BinaryWriter writer;

        public ByteBuffer(int capacity)
        {
            this.capacity = capacity;
            Clear();
        }

        public int ReadInt32()
        {
            return reader.ReadInt32();
        }


        public void Write(double value)
        {
            writer.Write(value);
        }

        public void Write(ulong value)
        {
            writer.Write(value);
        }

        public void Write(uint value)
        {
            writer.Write(value);
        }

        public void Write(ushort value)
        {
            writer.Write(value);
        }

        public void Write(string value)
        {
            writer.Write(value);
        }

        public void Write(float value)
        {
            writer.Write(value);
        }

        public void Write(sbyte value)
        {
            writer.Write(value);
        }

        public void Write(long value)
        {
            writer.Write(value);
        }

        public void Write(int value)
        {
            writer.Write(value);
        }

        public void Write(char ch)
        {
            writer.Write(ch);
        }

        public void Write(decimal value)
        {
            writer.Write(value);
        }

        public void Write(short value)
        {
            writer.Write(value);
        }

        public void Write(bool value)
        {
            writer.Write(value);
        }


        public void Clear()
        {
            if(reader != null)
            {
                reader.Dispose();
            }

            if(writer != null)
            {
                writer.Dispose();
            }

            stream = new MemoryStream(capacity);
            reader = new BinaryReader(stream);
            writer = new BinaryWriter(stream);
        }
    }
}
