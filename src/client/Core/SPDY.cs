using SeaCatCSharpClient.Http;
using SeaCatCSharpClient.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeaCatCSharpClient.Core {

    /// <summary>
    /// SPDY protocol helper methods and attributes
    /// </summary>
    public class SPDY {
        static public int HEADER_SIZE = 8;

        static public ushort CNTL_FRAME_VERSION_SPD3 = 0x03;
        static public ushort CNTL_FRAME_VERSION_ALX1 = 0xA1;

        static public ushort CNTL_TYPE_SYN_STREAM = 1;
        static public ushort CNTL_TYPE_SYN_REPLY = 2;
        static public ushort CNTL_TYPE_RST_STREAM = 3;
        static public ushort CNTL_TYPE_PING = 6;

        static public ushort CNTL_TYPE_STATS_REQ = 0xA1;
        static public ushort CNTL_TYPE_STATS_REP = 0xA2;

        static public ushort CNTL_TYPE_CSR = 0xC1;
        static public ushort CNTL_TYPE_CERT_QUERY = 0xC2;
        static public ushort CNTL_TYPE_CERT = 0xC3;

        static public byte FLAG_FIN = (byte)0x01;
        static public byte FLAG_UNIDIRECTIONAL = (byte)0x02;
        static public byte FLAG_CSR_NOT_FOUND = (byte)0x80;

        static public int RST_STREAM_STATUS_INVALID_STREAM = 2;
        static public int RST_STREAM_STATUS_STREAM_ALREADY_CLOSED = 9;

        /// <summary>
        /// Builds SPD3 Ping frame
        /// </summary>
        /// <param name="frame">frame to write to</param>
        /// <param name="pingId">id of the ping</param>
        public static void BuildSPD3Ping(ByteBuffer frame, int pingId) {
            // It is SPDY v3 control frame 
            frame.PutShort((short)(0x8000 | CNTL_FRAME_VERSION_SPD3));

            // Type
            frame.PutShort((short)CNTL_TYPE_PING);

            // Flags and length
            frame.PutInt(4);

            // Ping ID
            frame.PutInt(pingId);
        }

        /// <summary>
        /// Builds SPD3 RST stream frame
        /// </summary>
        /// <param name="frame">frame to write to</param>
        /// <param name="streamId">id of the stream</param>
        /// <param name="statusCode">status code</param>
        public static void BuildSPD3RstStream(ByteBuffer frame, int streamId, int statusCode) {
            // It is SPDY v3 control frame 
            frame.PutShort((short)(0x8000 | CNTL_FRAME_VERSION_SPD3));

            // Type
            frame.PutShort((short)CNTL_TYPE_RST_STREAM);

            // Flags and length
            frame.PutInt(8);

            // Stream ID
            frame.PutInt(streamId);

            // Status Code
            frame.PutInt(statusCode);
        }

        /// <summary>
        /// Builds ALX1 Syn stream frame
        /// </summary>
        /// <param name="frame">frame to write to</param>
        /// <param name="streamId">id of the stream</param>
        /// <param name="url">target url</param>
        /// <param name="method">http method</param>
        /// <param name="headers">collection of http headers</param>
        /// <param name="finFlag">indicator whether a fin flag should be appended</param>
        /// <param name="priority">priority</param>
        public static void BuildALX1SynStream(ByteBuffer frame, int streamId, Uri url, string method, Headers headers, bool finFlag, int priority) {
            BuildALX1SynStream(frame, streamId, url.Host, method, url.AbsolutePath, headers, finFlag, priority);
        }

        /// <summary>
        /// Builds ALX1 Syn Stream frame
        /// </summary>
        /// <param name="frame">frame to write to</param>
        /// <param name="streamId">id of the stream</param>
        /// <param name="host">target host</param>
        /// <param name="method">http method</param>
        /// <param name="path">target path</param>
        /// <param name="headers">collection of http headers</param>
        /// <param name="finFlag">indicator whether a fin flag should be appended</param>
        /// <param name="priority">priority</param>
        public static void BuildALX1SynStream(ByteBuffer frame, int streamId, string host, string method, string path, Headers headers, bool finFlag, int priority) {

            Debug.Assert((streamId & 0x80000000) == 0);

            frame.PutShort((short)(0x8000 | CNTL_FRAME_VERSION_ALX1));  // Frame Version Type
            frame.PutShort((short)CNTL_TYPE_SYN_STREAM);                // Type
            frame.PutInt(0x04030201);                                   // Flags and length (placeholder)
            frame.PutInt(streamId);                                     // Stream ID
            frame.PutInt(0);                                            // Associated-To-Stream-ID - not used
            frame.PutByte((byte)((priority & 0x07) << 5));              // Priority
            frame.PutByte((byte)0x00);                                  // Slot (reserved)

            Debug.Assert(frame.Position == 18);

            // Strip .seacat from hosts
            // That's for historical reason (we need to support .seacat extension this way)
            if (host.EndsWith(SeaCatInternals.SeaCatHostSuffix)) {
                int lastPeriodPos = host.LastIndexOf('.');
                if (lastPeriodPos > 0) host = host.Substring(0, lastPeriodPos);
            }

            AppendVLEString(frame, host);
            AppendVLEString(frame, method);
            AppendVLEString(frame, path);

            for (int i = 0; i < headers.Size(); i++) {
                string header = headers.Name(i);
                if (header == null) continue;
                if (header.ToLower() == "host") continue;
                if (header.ToLower() == "connection") continue;

                string value = headers.Value(i);
                if (value == null) continue;

                AppendVLEString(frame, header);
                AppendVLEString(frame, value);
            }

            // Update length entry
            int flagLength = frame.Position - HEADER_SIZE;
            Debug.Assert(flagLength < 0x01000000);
            flagLength |= (finFlag ? FLAG_FIN : 0) << 24;
            frame.PutInt(4, flagLength); // Update length of frame
        }

        /// <summary>
        /// Appends length of data frame
        /// </summary>
        /// <param name="frame">frame to write to</param>
        /// <param name="finFlag">indicator whether a FIN flag should be appended</param>
        public static void BuildDataFrameFlagLength(ByteBuffer frame, bool finFlag) {
            Debug.Assert(frame != null);
            int flagLength = frame.Position - HEADER_SIZE;
            Debug.Assert(flagLength < 0x01000000);
            flagLength |= (finFlag ? FLAG_FIN : 0) << 24;
            frame.PutInt(4, flagLength); // Update length of frame
        }

        /// <summary>
        /// Appends a UTF8 string
        /// </summary>
        /// <param name="frame">frame to write to</param>
        /// <param name="text">text to write</param>
        private static void AppendVLEString(ByteBuffer frame, String text) {
            byte[] bytes;
            try {
                bytes = System.Text.Encoding.UTF8.GetBytes(text);
            } catch (Exception) {
                Logger.Error("SPDY", $"Can't append VLES string: {text}");
                bytes = new byte[] { (byte)'?', (byte)'?', (byte)'?' };
            }

            Debug.Assert(bytes.Length <= 0xFFFF);

            // Append length
            if (bytes.Length >= 0xFA) {
                frame.PutByte((byte)0xFF);
                frame.PutShort((short)bytes.Length);
            } else {
                frame.PutByte((byte)bytes.Length);
            }

            frame.PutBytes(bytes);
        }

        /// <summary>
        /// Parses VLES string from given frame
        /// </summary>
        /// <param name="frame">frame to read from</param>
        /// <returns></returns>
        public static String ParseVLEString(ByteBuffer frame) {
            int length = ((short)(frame.GetByte() & 0xff));
            if (length == 0xFF) length = ((int)(frame.GetShort() & 0xffff));

            Debug.Assert(length >= 0);

            byte[] bytes = new byte[length];
            frame.GetBytes(bytes, frame.Position, length);

            try {
                var output = UTF8Encoding.UTF8.GetString(bytes, 0, length);
                return output;
            } catch (Exception) {
                return "???";
            }
        }

        public static int BuildFrameVersionType(ushort cntlFrameVersion, ushort cntlType) {
            uint ret = cntlFrameVersion;
            ret <<= 16;
            ret |= cntlType;
            return (int)ret;
        }
    }

}
