using SeaCatCSharpClient.Core;
using SeaCatCSharpClient.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeaCatCSharpClient.Interfaces {

    /// <summary>
    /// Interface for control frame consumers
    /// </summary>
    public interface IFrameConsumer {

        bool ReceivedControlFrame(Reactor reactor, ByteBuffer frame, int frameVersionType, int frameLength, byte frameFlags);

    }
}
