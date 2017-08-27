using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace seacat_winrt_client.Utils {

    /// <summary>
    /// Implementation of memory buffer
    /// Buffer starts by default in WRITE mode, until you call the FLIP method, which puts it into READ mode
    /// </summary>
    public class ByteBuffer {

        private readonly byte[] _buffer;
        private int _pos;  // Must track start of the buffer.

        public int Length { get { return _buffer.Length; } }

        public byte[] Data { get { return _buffer; } }

        public ByteBuffer(int capacity) : this(new byte[capacity]) { }

        public ByteBuffer(byte[] buffer) : this(buffer, 0) { }

        public ByteBuffer(byte[] buffer, int pos) {
            _buffer = buffer;
            _pos = pos;
            // for write mode, the limit is the same as capacity
            Limit = buffer.Length;
        }

        public ByteBuffer(byte[] buffer, int pos, int limit) {
            if (limit <= pos) {
                throw new ArgumentException("Limit can't be lower than position");
            }

            _buffer = buffer;
            _pos = pos;
            Limit = limit;
        }

        public int Limit { get; protected set; }

        public int Capacity { get { return _buffer.Length; } }

        public int Position {
            get { return _pos; }
            set { _pos = value; }
        }

        public void Flip() {
            Limit = _pos;
            _pos = 0;
        }

        public void Reset() {
            _pos = 0;
            Limit = _buffer.Length;
        }

        // Pre-allocated helper arrays for convertion.
        private float[] floathelper = new[] { 0.0f };
        private int[] inthelper = new[] { 0 };
        private double[] doublehelper = new[] { 0.0 };
        private ulong[] ulonghelper = new[] { 0UL };

        // Helper functions for the unsafe version.
        static public ushort ReverseBytes(ushort input) {
            return (ushort)(((input & 0x00FFU) << 8) |
                            ((input & 0xFF00U) >> 8));
        }
        static public uint ReverseBytes(uint input) {
            return ((input & 0x000000FFU) << 24) |
                   ((input & 0x0000FF00U) << 8) |
                   ((input & 0x00FF0000U) >> 8) |
                   ((input & 0xFF000000U) >> 24);
        }
        static public ulong ReverseBytes(ulong input) {
            return (((input & 0x00000000000000FFUL) << 56) |
                    ((input & 0x000000000000FF00UL) << 40) |
                    ((input & 0x0000000000FF0000UL) << 24) |
                    ((input & 0x00000000FF000000UL) << 8) |
                    ((input & 0x000000FF00000000UL) >> 8) |
                    ((input & 0x0000FF0000000000UL) >> 24) |
                    ((input & 0x00FF000000000000UL) >> 40) |
                    ((input & 0xFF00000000000000UL) >> 56));
        }

        // Helper functions for the safe (but slow) access
        protected void Write(int count, ulong data) {
            // Use network byte order -> BigEndian
            //if (BitConverter.IsLittleEndian) {
            //    for (int i = 0; i < count; i++) {
            //        _buffer[_pos++] = (byte)(data >> i * 8);
            //    }
            //} else {
                int offset = _pos;
                for (int i = 0; i < count; i++) {
                    _buffer[offset + count - 1 - i] = (byte)(data >> i * 8);
                }
                _pos += count;
            // }
        }

        protected ulong Read(int count) {
            AssertOffsetAndLength(_pos, count);
            ulong r = 0;

            // Use network byte order -> BigEndian
            //if (BitConverter.IsLittleEndian) {
            //    for (int i = 0; i < count; i++) {
            //        r |= (ulong)_buffer[_pos++] << i * 8;
            //    }
            //} else {
                int offset = _pos;
                for (int i = 0; i < count; i++) {
                    r |= (ulong)_buffer[offset + count - 1 - i] << i * 8;
                }
                _pos += count;
            //}
            return r;
        }


        private void AssertOffsetAndLength(int offset, int length) {
            if (offset < 0 ||
                offset > _buffer.Length - length)
                throw new ArgumentOutOfRangeException();
        }

        public void PutSbyte(sbyte value) {
            AssertOffsetAndLength(_pos, sizeof(sbyte));
            _buffer[_pos++] = (byte)value;
        }

        public void PutByte(byte value) {
            AssertOffsetAndLength(_pos, sizeof(byte));
            _buffer[_pos++] = value;
        }

        public void PutBytes(byte[] values) {
            for (int i = 0; i < values.Length; i++) {
                AssertOffsetAndLength(_pos, sizeof(byte));
                _buffer[_pos++] = values[i];
            }
        }


        public void PutShort(short value) {
            AssertOffsetAndLength(_pos, sizeof(short));
            Write(sizeof(short), (ulong)value);
        }

        public void PutUshort(ushort value) {
            AssertOffsetAndLength(_pos, sizeof(ushort));
            Write(sizeof(ushort), (ulong)value);
        }

        public void PutInt(int value) {
            AssertOffsetAndLength(_pos, sizeof(int));
            Write(sizeof(int), (ulong)value);
        }

        public void PutInt(int offset, int value) {
            // restore pos value when read
            int temp = _pos;
            _pos = offset;
            AssertOffsetAndLength(_pos, sizeof(int));
            Write(sizeof(int), (ulong)value);
            _pos = temp;
        }


        public void PutUint(uint value) {
            AssertOffsetAndLength(_pos, sizeof(uint));
            Write(sizeof(uint), (ulong)value);
        }

        public void PutLong(long value) {
            AssertOffsetAndLength(_pos, sizeof(long));
            Write(sizeof(long), (ulong)value);
        }

        public void PutUlong(ulong value) {
            AssertOffsetAndLength(_pos, sizeof(ulong));
            Write(sizeof(ulong), value);
        }

        public void PutFloat(float value) {
            AssertOffsetAndLength(_pos, sizeof(float));
            floathelper[0] = value;
            Buffer.BlockCopy(floathelper, 0, inthelper, 0, sizeof(float));
            Write(sizeof(float), (ulong)inthelper[0]);
        }

        public void PutDouble(double value) {
            AssertOffsetAndLength(_pos, sizeof(double));
            doublehelper[0] = value;
            Buffer.BlockCopy(doublehelper, 0, ulonghelper, 0, sizeof(double));
            Write(sizeof(double), ulonghelper[0]);
        }

        public sbyte GetSbyte() {
            AssertOffsetAndLength(_pos, sizeof(sbyte));
            return (sbyte)_buffer[_pos++];
        }

        public byte GetByte(int offset) {
            AssertOffsetAndLength(offset, sizeof(byte));
            return _buffer[offset];
        }

        public byte GetByte() {
            AssertOffsetAndLength(_pos, sizeof(byte));
            return _buffer[_pos++];
        }

        public void GetBytes(byte[] buffer, int index, int count) {
            for (int i = 0; i < count; i++) {
                buffer[i] = _buffer[index + i];
            }

            // this is strange, however the same logic is implemented in Java
            _pos += count;
        }

        public short GetShort() {
            return (short)Read(sizeof(short));
        }

        public ushort GetUshort() {
            return (ushort)Read(sizeof(ushort));
        }

        public int GetInt() {
            return (int)Read(sizeof(int));
        }

        public int GetInt(int offset) {
            // restore pos value when read
            int temp = _pos;
            _pos = offset;
            var output = (int)Read(sizeof(int));
            _pos = temp;
            return output;
        }

        public uint GetUint() {
            return (uint)Read(sizeof(uint));
        }

        public long GetLong() {
            return (long)Read(sizeof(long));
        }

        public ulong GetUlong() {
            return Read(sizeof(ulong));
        }

        public float GetFloat() {
            int i = (int)Read(sizeof(float));
            inthelper[0] = i;
            Buffer.BlockCopy(inthelper, 0, floathelper, 0, sizeof(float));
            return floathelper[0];
        }

        public double GetDouble() {
            ulong i = Read(sizeof(double));
            ulonghelper[0] = i;
            Buffer.BlockCopy(ulonghelper, 0, doublehelper, 0, sizeof(double));
            return doublehelper[0];
        }
    }
}
