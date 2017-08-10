using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using seacat_core_bridge;
using Windows.Storage;
using Windows.ApplicationModel;
using System.Reflection;
using seacat_wp_client.Utils;
using seacat_wp_client.Ping;
using seacat_wp_client.Core;
using System.ComponentModel;
using seacat_wp_client.Communication;
using System.Threading;
using seacat_wp_client;

namespace seacat_wp_client
{

    public class Reactor
    {

        private static Reactor _instance;

        private Reactor()
        {

        }

        public static Reactor Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new Reactor();
                }

                return _instance;
            }
        }


        public FramePool FramePool { get; private set; }
        public CoreAPI CoreAPI { get; private set; }
        public SeacatBridge Bridge { get; private set; }

        public void Init()
        {
            FramePool = new FramePool();
            this.ccoreThread = new Task(() => _run());
            //MTODO this.ccoreThread.setName("SeaCatCCoreThread");
            //MTODO this.ccoreThread.setDaemon(true);

            //MTODO this.workerExecutor = new ThreadPoolExecutor(0, 1000, 5, TimeUnit.SECONDS, new BlockingQueue<IAsyncResult>());

            CoreAPI = new CoreAPI();
            Bridge = new SeacatBridge();

            StorageFolder local = Windows.Storage.ApplicationData.Current.LocalFolder;
            Package package = Package.Current;

            int rc = Bridge.init((ISeacatCoreAPI)CoreAPI, package.Id.Name, "dev", "win",
                local.Path + "\\.seacat"); // subdir must be specified since the core api adds a suffix to it

            RC.CheckAndThrowIOException("seacatcc.init", rc);

            lastState = Bridge.state();

            var frameProvidersComparator = System.Collections.Generic.Comparer<IFrameProvider>.Create((p1, p2) =>
            {
                int p1pri = p1.GetFrameProviderPriority();
                int p2pri = p2.GetFrameProviderPriority();
                if (p1pri < p2pri) return -1;
                else if (p1pri == p2pri) return 0;
                else return 1;
            });

            // Setup frame provider priority queue
            //MTODO frameProviders = new PriorityBlockingQueue<>(11, frameProvidersComparator);


            // Create and register stream factory as control frame consumer
            streamFactory = new StreamFactory();
            cntlFrameConsumers.Add(SPDY.buildFrameVersionType(SPDY.CNTL_FRAME_VERSION_ALX1, SPDY.CNTL_TYPE_SYN_REPLY), streamFactory);
            cntlFrameConsumers.Add(SPDY.buildFrameVersionType(SPDY.CNTL_FRAME_VERSION_SPD3, SPDY.CNTL_TYPE_RST_STREAM), streamFactory);


            // Create and register ping factory as control frame consumer
            pingFactory = new PingFactory();
            cntlFrameConsumers.Add(SPDY.buildFrameVersionType(SPDY.CNTL_FRAME_VERSION_SPD3, SPDY.CNTL_TYPE_PING), pingFactory);

            // Start reactor thread
            ccoreThread.Start();

            /*MTODO
		// Wait till started
		eventLoopNotStartedlock.lock();
		while (!eventLoopStarted)
		{
			eventLoopStartedCond.awaitUninterruptibly();
		}
            eventLoopNotStartedlock.unlock();
            */
        }


        private Task ccoreThread;

        // MTODO Monitor eventLoopNotStartedlock = Monitor.Enter();
        // MTODO Condition eventLoopStartedCond = eventLoopNotStartedlock.newCondition();

        private bool eventLoopStarted = false;

        // MTODO private Executor workerExecutor;

        public PingFactory pingFactory;
        public StreamFactory streamFactory;

        private Dictionary<int, IFrameConsumer> cntlFrameConsumers = new Dictionary<int, IFrameConsumer>();
        private BlockingQueue<IFrameProvider> frameProviders;

        private String lastState;

        private String clientId = "ANONYMOUS_CLIENT";
        private String clientTag = "[ANONYMOUS0CLIENT]";

        static String packageName = null;


        public void Shutdown()
        {
            int rc = Bridge.shutdown();
            RC.CheckAndThrowIOException("seacatcc.shutdown", rc);

            /* MTODO
            while (true)
            {
                try
                {
                    ccoreThread.join(5000);
                }
                catch (InterruptedException e)
                {
                    Thread.currentThread().interrupt();
                    continue;
                }

                if (ccoreThread.isAlive())
                {
                    throw new IOException(String.format("%s is still alive", this.ccoreThread.getName()));
                }

                break;
            }*/
        }


        private static void _run()
        {
            int rc = Reactor.Instance.Bridge.run();
            if (rc != RC.RC_OK)
                System.Diagnostics.Debug.WriteLine(String.Format("return code %d in %s", rc, "seacatcc.run"));
        }

        ///


        public void RegisterFrameProvider(IFrameProvider provider, bool single)
        {
            lock (frameProviders)

            {
                if ((single) && (frameProviders.Contains(provider))) return;

                frameProviders.Enqueue(provider);
            }

            // Yield to C-Core that we have frame to send
            int rc = Bridge.yield('W');
            if ((rc > 7900) && (rc < 8000))

            {
                System.Diagnostics.Debug.WriteLine(String.Format("return code %d in %s", rc, "seacatcc.yield"));
                rc = RC.RC_OK;
            }
            RC.CheckAndThrowIOException("seacatcc.yield", rc);
        }


        public void BroadcastState()
        {
            /* MTODO
            Intent intent = SeaCatInternals.createIntent(SeaCatClient.ACTION_SEACAT_STATE_CHANGED);
            intent.putExtra(SeaCatClient.EXTRA_STATE, seacatcc.state());
            intent.putExtra(SeaCatClient.EXTRA_PREV_STATE, lastState);
            this.sendBroadcast(intent);*
        }

        ///

        private bool ReceivedControlFrame(ByteBuffer frame)
        {
            int frameVersionType = frame.getInt() & 0x7fffffff;

            int frameLength = frame.getInt();
            byte frameFlags = (byte)(frameLength >> 24);
            frameLength &= 0xffffff;

            if (frameLength + SPDY.HEADER_SIZE != frame.limit())
            {
                Log.w(SeaCatInternals.L, String.format("Incorrect frame received: %d %x %d %x - closing connection", frame.limit(), frameVersionType, frameLength, frameFlags));

                // Invalid frame received -> disconnect from a gateway
                seacatcc.yield('d');
                return true;
            }

            ICntlFrameConsumer consumer = cntlFrameConsumers.get(frameVersionType);
            if (consumer == null)
            {
                Log.w(SeaCatInternals.L, String.format("Unidentified Control frame received: %d %x %d %x", frame.limit(), frameVersionType, frameLength, frameFlags));
                return true;
            }

            return consumer.receivedControlFrame(this, frame, frameVersionType, frameLength, frameFlags);
        }


        protected void ConfigureProxyServer()
        {
            /* MTODO
            SharedPreferences sp = this.getSharedPreferences(SeaCatInternals.SeaCatPreferences, Context.MODE_PRIVATE);

            String proxy_host = sp.getString("HTTPSProxyHost", "");
            String proxy_port = sp.getString("HTTPSProxyPort", "");

            if (proxy_host.isEmpty())
            {
                proxy_host = System.getProperty("https.proxyHost", "");
                proxy_port = System.getProperty("https.proxyPort", "");
                //String proxy_user = System.getProperty("https.proxyUser", "");
                //String proxy_password = System.getProperty("https.proxyPassword", "");
            }

            int rc = seacatcc.set_proxy_server_worker(proxy_host, proxy_port);
            RC.CheckAndLogError("seacatcc.set_proxy_server_worker", rc);
            */
        }

        ///

        public String GetClientTag()
        {
            return this.clientTag;
        }

        public String GetClientId()
        {
            return this.clientId;
        }

        public static void SetPackageName(String packageName)
        {
            Reactor.packageName = packageName;
        }

        public void CallbackWorkerRequest(char worker)
        {
            switch (worker)
            {
                case 'P':
                    new Task(() => Bridge.ppkgen_worker()).Start();
                    break;
                case 'C':
                    Task CSRWorker = CSR.CreateDefault();  
                    if (CSRWorker != null) CSRWorker.Start();
                    break;
                default:
                    System.Diagnostics.Debug.WriteLine(String.Format("Unknown worker requested %c", worker));
                    break;
            }
        }
    }
}
