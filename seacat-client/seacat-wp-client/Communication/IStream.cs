using seacat_wp_client.Core;
using seacat_wp_client.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace seacat_wp_client.Communication
{
    public interface IStream
    {
        void Reset();

        bool ReceivedALX1_SYN_REPLY(SeacatClient reactor, ByteBuffer frame, int frameLength, byte frameFlags);
        bool ReceivedSPD3_RST_STREAM(SeacatClient reactor, ByteBuffer frame, int frameLength, byte frameFlags);
        bool ReceivedDataFrame(SeacatClient reactor, ByteBuffer frame, int frameLength, byte frameFlags);
    }

}
