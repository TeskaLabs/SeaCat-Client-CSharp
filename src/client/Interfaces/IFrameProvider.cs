using SeaCatCSharpClient.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SeaCatCSharpClient.Utils;

namespace SeaCatCSharpClient.Interfaces {

    public interface IFrameProvider {
        ByteBuffer BuildFrame(Reactor reactor, out bool keep);
        int FrameProviderPriority { get; }
    }
}
