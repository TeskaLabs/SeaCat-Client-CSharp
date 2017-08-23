using seacat_wp_client.Core;
using seacat_wp_client.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace seacat_wp_client.Interfaces {

    public interface IFrameConsumer {
        bool ReceivedControlFrame(Reactor reactor, ByteBuffer frame, int frameVersionType, int frameLength, byte frameFlags);
    }

}
