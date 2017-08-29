﻿using SeaCatCSharpClient.Core;
using SeaCatCSharpClient.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeaCatCSharpClient.Interfaces {

    public interface IStream {

        void Reset();

        // reply -> opens a stream
        bool ReceivedALX1_SYN_REPLY(Reactor reactor, ByteBuffer frame, int frameLength, byte frameFlags);
        // reset stream -> closes a stream (emergency close)
        bool ReceivedSPD3_RST_STREAM(Reactor reactor, ByteBuffer frame, int frameLength, byte frameFlags);
        // received frame with dat
        bool ReceivedDataFrame(Reactor reactor, ByteBuffer frame, int frameLength, byte frameFlags);
    }

}
