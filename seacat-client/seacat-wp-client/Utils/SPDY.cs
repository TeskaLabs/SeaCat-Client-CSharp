using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace seacat_wp_client.Utils
{
    public class SPDY
    {
        static public int HEADER_SIZE = 8;

        static public short CNTL_FRAME_VERSION_SPD3 = 0x03;
        static public short CNTL_FRAME_VERSION_ALX1 = 0xA1;

        static public short CNTL_TYPE_SYN_STREAM = 1;
        static public short CNTL_TYPE_SYN_REPLY = 2;
        static public short CNTL_TYPE_RST_STREAM = 3;
        static public short CNTL_TYPE_PING = 6;

        static public short CNTL_TYPE_STATS_REQ = 0xA1;
        static public short CNTL_TYPE_STATS_REP = 0xA2;

        static public short CNTL_TYPE_CSR = 0xC1;
        static public short CNTL_TYPE_CERT_QUERY = 0xC2;
        static public short CNTL_TYPE_CERT = 0xC3;

        static public byte FLAG_FIN = (byte)0x01;
        static public byte FLAG_UNIDIRECTIONAL = (byte)0x02;
        static public byte FLAG_CSR_NOT_FOUND = (byte)0x80;

        static public int RST_STREAM_STATUS_INVALID_STREAM = 2;
        static public int RST_STREAM_STATUS_STREAM_ALREADY_CLOSED = 9;

        ///

        public static void buildSPD3Ping(MemoryStream frame, int pingId)
        {
            using(BinaryWriter wr = new BinaryWriter(frame))
            {
                // It is SPDY v3 control frame 
                wr.Write((short)(0x8000 | CNTL_FRAME_VERSION_SPD3));

                // Type
                wr.Write(CNTL_TYPE_PING);

                // Flags and length
                wr.Write(4);

                // Ping ID
                wr.Write(pingId);
            }
        }


        public static void buildSPD3RstStream(MemoryStream frame, int streamId, int statusCode)
        {
            using (BinaryWriter wr = new BinaryWriter(frame))
            {
                // It is SPDY v3 control frame 
                wr.Write((short)(0x8000 | CNTL_FRAME_VERSION_SPD3));

                // Type
                wr.Write(CNTL_TYPE_RST_STREAM);

                // Flags and length
                wr.Write(8);

                // Stream ID
                wr.Write(streamId);

                // Status Code
                wr.Write(statusCode);
            }
        }
        /*
        public static void buildALX1SynStream(BinaryWriter buffer, int streamId, Url url, String method, Headers headers, bool fin_flag, int priority)
        {
            buildALX1SynStream(buffer, streamId, url.getHost(), method, url.getFile(), headers, fin_flag, priority);
        }

        public static void buildALX1SynStream(BinaryWriter buffer, int streamId, String host, String method, String path, Headers headers, bool fin_flag, int priority)
        {
            assert((streamId & 0x80000000) == 0);

            buffer.Write((short)(0x8000 | CNTL_FRAME_VERSION_ALX1));
            buffer.Write(CNTL_TYPE_SYN_STREAM); // Type
            buffer.Write(0x04030201);                  // Flags and length (placeholder)
            buffer.Write(streamId);                    // Stream ID
            buffer.Write(0);                           // Associated-To-Stream-ID - not used
            buffer.Write((byte)((priority & 0x07) << 5));   // Priority
            buffer.Write((byte)0x00);                     // Slot (reserved)

            assert buffer.position() == 18;

            // Strip .seacat from hosts
            // That's for historical reason (we need to support .seacat extension this way)
            if (host.endsWith(SeaCatInternals.SeaCatHostSuffix))
            {
                final int lastPeriodPos = host.lastIndexOf('.');
                if (lastPeriodPos > 0) host = host.substring(0, lastPeriodPos);
            }

            appendVLEString(buffer, host);
            appendVLEString(buffer, method);
            appendVLEString(buffer, path);

            for (int i = 0; i < headers.size(); i++)
            {
                String header = headers.name(i);
                if (header == null) continue;
                if (header.equalsIgnoreCase("host")) continue;
                if (header.equalsIgnoreCase("connection")) continue;

                String value = headers.value(i);
                if (value == null) continue;

                appendVLEString(buffer, header);
                appendVLEString(buffer, value);
            }

            // Update length entry
            int flagLength = buffer.position() - HEADER_SIZE;
            assert flagLength < 0x01000000;
            flagLength |= (fin_flag ? FLAG_FIN : 0) << 24;
            buffer.putInt(4, flagLength); // Update length of frame
        }

    
        public static void buildDataFrameFlagLength(ByteBuffer buffer, boolean fin_flag)
        {
            assert buffer != null;
            int flagLength = buffer.position() - HEADER_SIZE;
            assert flagLength < 0x01000000;
            flagLength |= (fin_flag ? FLAG_FIN : 0) << 24;
            buffer.putInt(4, flagLength); // Update length of frame
        }

        ///

        private static void appendVLEString(ByteBuffer buffer, String text)
        {
            byte[] bytes;
            try
            {
                bytes = text.getBytes("UTF-8");
            }
            catch (UnsupportedEncodingException e)
            {
                bytes = new byte[] { '?', '?', '?' };
            }

            assert bytes.length <= 0xFFFF;

            // Append length
            if (bytes.length >= 0xFA)
            {
                buffer.put((byte)0xFF);
                buffer.putShort((short)bytes.length);
            }
            else
            {
                buffer.put((byte)bytes.length);
            }

            buffer.put(bytes);
        }
        
        ///

        public static String parseVLEString(BinaryReader buffer)
        {
            int length = ((short)(buffer.Read() & 0xff));
            if (length == 0xFF) length = ((int)(buffer.ReadInt16() & 0xffff));

            assert length >= 0;

            byte[] bytes = new byte[length];
            buffer.Read(bytes, 0, length);

            try
            {
                return new String(bytes, "UTF-8");
            }
            catch
            {
                return "???";
            }
        }*/

        ///

        public static int buildFrameVersionType(short cntlFrameVersion, short cntlType)
        {
            int ret = cntlFrameVersion;
            ret <<= 16;
            ret |= cntlType;
            return ret;
        }

    }

}
