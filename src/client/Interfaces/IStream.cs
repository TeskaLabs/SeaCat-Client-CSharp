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
    /// Interface for common frame consumers 
    /// </summary>
    public interface IStream {

        void Reset();

        /// <summary>
        /// Received reply which results in opening a new stream
        /// </summary>
        /// <param name="reactor">reactor</param>
        /// <param name="frame">received frame</param>
        /// <param name="frameLength">length of received frame</param>
        /// <param name="frameFlags">falgs of received frame</param>
        /// <returns></returns>
        bool ReceivedALX1_SYN_REPLY(Reactor reactor, ByteBuffer frame, int frameLength, byte frameFlags);

        /// <summary>
        /// Received reply which results in emergency close
        /// </summary>
        /// <param name="reactor">reactor</param>
        /// <param name="frame">received frame</param>
        /// <param name="frameLength">length of received frame</param>
        /// <param name="frameFlags">falgs of received frame</param>
        /// <returns></returns>
        bool ReceivedSPD3_RST_STREAM(Reactor reactor, ByteBuffer frame, int frameLength, byte frameFlags);

        /// <summary>
        /// Received data frame
        /// </summary>
        /// <param name="reactor">reactor</param>
        /// <param name="frame">received frame</param>
        /// <param name="frameLength">length of received frame</param>
        /// <param name="frameFlags">falgs of received frame</param>
        /// <returns></returns>
        bool ReceivedDataFrame(Reactor reactor, ByteBuffer frame, int frameLength, byte frameFlags);
    }

}
