using seacat_wp_client.Core;
using seacat_wp_client.Interfaces;
using seacat_wp_client.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace seacat_wp_client.Ping
{
    public class PingFactory : IFrameConsumer, IFrameProvider
    {
        private IntegerCounter idSequence = new IntegerCounter(1);
        private BlockingQueue<Ping> outboundPingQueue = new BlockingQueue<Ping>();
        private Dictionary<int, Ping> waitingPingDict = new Dictionary<int, Ping>();

        public void Ping(Reactor reactor, Ping ping)
        {
            lock (this)
            {
                outboundPingQueue.Enqueue(ping);
                reactor.RegisterFrameProvider(this, true);
            }
        }


        public void Reset()
        {
            lock (this)
            {
                idSequence.Set(1);

                foreach (var key in waitingPingDict.Keys)
                {
                    Ping ping = waitingPingDict[key];
                    waitingPingDict.Remove(key);
                    ping.Cancel();
                }
            }
        }


        public void HeartBeat(double now)
        {
            lock (this)
            {
                foreach (var key in waitingPingDict.Keys.ToList())
                {
                    Ping ping = waitingPingDict[key];
                    if (ping.IsExpired(now))
                    {
                        waitingPingDict.Remove(key);
                        ping.Cancel();
                    }
                }

                foreach (var ping in outboundPingQueue.Queue.Items.ToList<Ping>())
                {
                    if (ping.IsExpired(now))
                    {
                        outboundPingQueue.Remove(ping);
                        ping.Cancel();
                    }
                }
            }
        }


        public FrameResult BuildFrame(Reactor reactor)
        {
            lock (this)
            {
                ByteBuffer frame = null;

                //Integer pingId
                Ping ping = outboundPingQueue.Dequeue();
                if (ping == null) return new FrameResult(null, false);

                // This is pong object (response to gateway)
                if (ping is Pong)
                {

                }
                else // This is ping object (request to gateway)
                {
                    ping.PingId = idSequence.GetAndAdd(2);
                    waitingPingDict.Add(ping.PingId, ping);
                }

                frame = reactor.FramePool.Borrow("PingFactory.ping");
                SPDY.BuildSPD3Ping(frame, ping.PingId);
                return new FrameResult(frame, !outboundPingQueue.IsEmpty());
            }
        }


        public bool ReceivedControlFrame(Reactor reactor, ByteBuffer frame, int frameVersionType, int frameLength, byte frameFlags)
        {
            lock (this)
            {
                //TODO: pingId is unsigned (based on SPDY specifications)
                int pingId = frame.GetInt();
                if ((pingId % 2) == 1)
                {
                    // Pong frame received ...
                    Ping ping = waitingPingDict[pingId];
                    waitingPingDict.Remove(pingId);

                    if (ping != null) ping.Pong();
                    else Logger.Warning("received pong with unknown id: " + pingId);

                }
                else
                {
                    //Send pong back to server
                    outboundPingQueue.Enqueue(new Pong(pingId));
                    try
                    {
                        reactor.RegisterFrameProvider(this, true);
                    }
                    catch (Exception e)
                    {
                        // We can ignore error in this case
                    }
                }

                return true;
            }
        }

        public int GetFrameProviderPriority()
        {
            return 0;
        }
    }
}
