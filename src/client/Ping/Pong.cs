using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeaCatCSharpClient.Ping {

    public class Pong : Ping {
        public Pong(int pingId) {
            PingId = pingId;
        }

    }
}
