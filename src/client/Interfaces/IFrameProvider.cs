using SeaCatCSharpClient.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SeaCatCSharpClient.Utils;

namespace SeaCatCSharpClient.Interfaces {

    /// <summary>
    /// Interface for providers with the ability to create new frames
    /// </summary>
    public interface IFrameProvider {
        ByteBuffer BuildFrame(Reactor reactor, out bool keep);
        int FrameProviderPriority { get; }
    }
}
