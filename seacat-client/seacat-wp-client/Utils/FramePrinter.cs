using seacat_wp_client.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace seacat_wp_client.Utils
{
    public class FramePrinter
    {
        static public string FrameToString(ByteBuffer frame)
        {
            if (frame == null) return "[null]";

            byte fb = frame.GetByte(0);
            if ((fb & (1L << 7)) != 0)
            {
                return ControlFrameToString(frame);
            }

            else
            {
                return DataFrameToString(frame);
            }
        }

        private static string ControlFrameToString(ByteBuffer frame)
        {
            //TODO: This ...
            return "[C??]";
        }


        private static string DataFrameToString(ByteBuffer frame)
        {
            int streamId = frame.GetInt(0);
            int frameLength = frame.GetInt(4);
            byte frameFlags = (byte)(frameLength >> 24);
            frameLength &= 0xffffff;

            return String.Format("[D %d %d%s]",
                    streamId,
                    frameLength,
                    ((frameFlags & SPDY.FLAG_FIN) == SPDY.FLAG_FIN) ? " FIN" : ""
            );
        }
    }

}
