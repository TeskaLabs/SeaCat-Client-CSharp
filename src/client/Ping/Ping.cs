using SeaCatCSharpClient.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SeaCatCSharpClient.Utils;

namespace SeaCatCSharpClient.Ping {

    /// <summary>
    /// Simple structure that can be used to test seacat connection using the ping flow
    /// </summary>
    public class Ping {

        protected double deadline;

        public Ping() {
            PingId = -1;
            deadline = SeaCatClient.Reactor.Bridge.time() + 60.0;
        }

        public int PingId { get; set; }

        public bool IsExpired(double now) {
            return now >= deadline;
        }

        public void Pong() {
            Logger.Info("Ping", "===== PONG =====");
        }

        public void Cancel() {
            // nothing to do here
        }
    }

}
