using seacat_wp_client.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace seacat_wp_client.Ping
{
    public class Ping
    {
        protected double deadline; //TODO: Add support for deadline (skip&cancel objects that are behind deadline)

        public Ping()
        {
            PingId = -1;
            deadline = Reactor.Instance.Bridge.time() + 60.0;
        }

        public int PingId { get; set; }

        public bool IsExpired(double now)
        {
            return now >= deadline;
        }

        public void Pong() { }
        public void Cancel() { }
    }

}
