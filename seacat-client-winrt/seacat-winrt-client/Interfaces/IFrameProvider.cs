using seacat_winrt_client.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using seacat_winrt_client.Utils;

namespace seacat_winrt_client.Interfaces {

    public interface IFrameProvider {
        ByteBuffer BuildFrame(Reactor reactor, out bool keep);
        int GetFrameProviderPriority();
    }
}
