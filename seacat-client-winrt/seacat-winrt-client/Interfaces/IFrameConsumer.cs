using seacat_winrt_client.Core;
using seacat_winrt_client.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace seacat_winrt_client.Interfaces {

    public interface IFrameConsumer {
        bool ReceivedControlFrame(Reactor reactor, ByteBuffer frame, int frameVersionType, int frameLength, byte frameFlags);
    }

}
