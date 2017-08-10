using seacat_wp_client.Core;
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

        bool ReceivedALX1_SYN_REPLY(Reactor reactor, MemoryStream frame, int frameLength, byte frameFlags);
        bool ReceivedSPD3_RST_STREAM(Reactor reactor, MemoryStream frame, int frameLength, byte frameFlags);
        bool ReceivedDataFrame(Reactor reactor, MemoryStream frame, int frameLength, byte frameFlags);
    }

}
