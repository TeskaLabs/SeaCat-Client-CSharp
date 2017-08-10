using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using seacat_wp_client.Ping;

namespace seacat_wp_client.Core
{
    public class Reactor
    {
        public FramePool FramePool { get; private set; }

        public Reactor()
        {
            FramePool = new FramePool();
        }

        internal void RegisterFrameProvider(PingFactory pingFactory, bool v)
        {
            throw new NotImplementedException();
        }
    }
}
