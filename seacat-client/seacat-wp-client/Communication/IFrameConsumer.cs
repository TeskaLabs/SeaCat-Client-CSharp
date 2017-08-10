using seacat_wp_client.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace seacat_wp_client.Communication
{
    public interface IFrameConsumer
    {
        bool ReceivedControlFrame(Reactor reactor, BinaryReader frame, int frameVersionType, int frameLength, byte frameFlags);
    }

}
