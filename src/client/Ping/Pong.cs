﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeaCatCSharpClient.Ping {

    /// <summary>
    /// Pong that will be sent back to server just when the ping arrives
    /// </summary>
    public class Pong : Ping {
        public Pong(int pingId) {
            PingId = pingId;
        }

    }
}
