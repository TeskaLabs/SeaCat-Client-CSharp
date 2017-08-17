using seacat_wp_client.Http;
using seacat_wp_client.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace seacat_wp_client.Core
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

        public static void BuildSPD3Ping(ByteBuffer frame, int pingId)
        {
            // It is SPDY v3 control frame 
            frame.PutShort((short)(0x8000 | CNTL_FRAME_VERSION_SPD3));

            // Type
            frame.PutShort(CNTL_TYPE_PING);

            // Flags and length
            frame.PutInt(4);

            // Ping ID
            frame.PutInt(pingId);
        }

        public static void BuildSPD3RstStream(ByteBuffer frame, int streamId, int statusCode)
        {
            // It is SPDY v3 control frame 
            frame.PutShort((short)(0x8000 | CNTL_FRAME_VERSION_SPD3));

            // Type
            frame.PutShort(CNTL_TYPE_RST_STREAM);

            // Flags and length
            frame.PutInt(8);

            // Stream ID
            frame.PutInt(streamId);

            // Status Code
            frame.PutInt(statusCode);
        }

        public static void BuildALX1SynStream(ByteBuffer buffer, int streamId, Uri url, String method, Headers headers, bool fin_flag, int priority)
        {
            BuildALX1SynStream(buffer, streamId, url.Host, method, url.AbsolutePath, headers, fin_flag, priority);
        }

        public static void BuildALX1SynStream(ByteBuffer buffer, int streamId, String host, String method, String path, Headers headers, bool fin_flag, int priority)
        {
            Debug.Assert((streamId & 0x80000000) == 0);

            buffer.PutShort((short)(0x8000 | CNTL_FRAME_VERSION_ALX1));
            buffer.PutShort(CNTL_TYPE_SYN_STREAM); // Type
            buffer.PutInt(0x04030201);                  // Flags and length (placeholder)
            buffer.PutInt(streamId);                    // Stream ID
            buffer.PutInt(0);                           // Associated-To-Stream-ID - not used
            buffer.PutByte((byte)((priority & 0x07) << 5));   // Priority
            buffer.PutByte((byte)0x00);                     // Slot (reserved)

            Debug.Assert(buffer.Position == 18);

            // Strip .seacat from hosts
            // That's for historical reason (we need to support .seacat extension this way)
            if (host.EndsWith(SeaCatInternals.SeaCatHostSuffix))
            {
                int lastPeriodPos = host.LastIndexOf('.');
                if (lastPeriodPos > 0) host = host.Substring(0, lastPeriodPos);
            }

            AppendVLEString(buffer, host);
            AppendVLEString(buffer, method);
            AppendVLEString(buffer, path);

            for (int i = 0; i < headers.Size(); i++)
            {
                String header = headers.Name(i);
                if (header == null) continue;
                if (header.ToLower() == "host") continue;
                if (header.ToLower() == "connection") continue;

                String value = headers.Value(i);
                if (value == null) continue;

                AppendVLEString(buffer, header);
                AppendVLEString(buffer, value);
            }

            // Update length entry
            int flagLength = buffer.Position - HEADER_SIZE;
            Debug.Assert(flagLength < 0x01000000);
            flagLength |= (fin_flag ? FLAG_FIN : 0) << 24;
            buffer.PutInt(4, flagLength); // Update length of frame
        }

        public static void BuildDataFrameFlagLength(ByteBuffer buffer, bool fin_flag)
        {
            Debug.Assert(buffer != null);
            int flagLength = buffer.Position - HEADER_SIZE;
            Debug.Assert(flagLength < 0x01000000);
            flagLength |= (fin_flag ? FLAG_FIN : 0) << 24;
            buffer.PutInt(4, flagLength); // Update length of frame
        }

        private static void AppendVLEString(ByteBuffer buffer, String text)
        {
            byte[] bytes;
            try
            {
                bytes = System.Text.Encoding.UTF8.GetBytes(text);
            }
            catch (Exception e)
            {
                bytes = new byte[] { (byte)'?', (byte)'?', (byte)'?' };
            }

            Debug.Assert(bytes.Length <= 0xFFFF);

            // Append length
            if (bytes.Length >= 0xFA)
            {
                buffer.PutByte((byte)0xFF);
                buffer.PutShort((short)bytes.Length);
            }
            else
            {
                buffer.PutByte((byte)bytes.Length);
            }

            buffer.PutBytes(bytes);
        }

        public static String ParseVLEString(ByteBuffer buffer)
        {
            int length = ((short)(buffer.GetByte() & 0xff));
            if (length == 0xFF) length = ((int)(buffer.GetShort() & 0xffff));

            Debug.Assert(length >= 0);

            byte[] bytes = new byte[length];
            buffer.GetBytes(bytes, 0, length);

            try
            {
                return System.Text.Encoding.UTF8.GetString(bytes, 0, length);
            }
            catch (Exception e)
            {
                return "???";
            }
        }

        public static int BuildFrameVersionType(short cntlFrameVersion, short cntlType)
        {
            int ret = cntlFrameVersion;
            ret <<= 16;
            ret |= cntlType;
            return ret;
        }

    }

}
