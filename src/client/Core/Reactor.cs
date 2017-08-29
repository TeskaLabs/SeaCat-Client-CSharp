﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using seacat_winrt_bridge;
using Windows.Storage;
using Windows.ApplicationModel;
using seacat_winrt_client.Utils;
using seacat_winrt_client.Ping;
using System.Threading;
using seacat_winrt_client.Interfaces;
using System.IO;
using System.Net.Http;

namespace seacat_winrt_client.Core {

    public class Reactor : seacat_winrt_bridge.ISeacatCoreAPI {
        private static string TAG = "Reactor";
        private static Reactor _instance;

        private Task ccoreThread;

        object eventLoopNotStartedlock = new object();
        private EventWaitHandle eventLoopStarted = new EventWaitHandle(false, EventResetMode.ManualReset);

        public EventWaitHandle isReadyHandle = new EventWaitHandle(false, EventResetMode.ManualReset);

        public PingFactory PingFactory { get; set; }
        public StreamFactory streamFactory;

        private Dictionary<int, IFrameConsumer> cntlFrameConsumers = new Dictionary<int, IFrameConsumer>();
        private BlockingQueue<IFrameProvider> frameProviders;

        private String lastState;
        private String clientId = "000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000";
        private String clientTag = "[AAAAAAAAAAAAAAAA]";
        static String packageName = null;

        public Reactor() {

        }

        public FramePool FramePool { get; private set; }
        public SeacatBridge Bridge { get; private set; }
        public string ProxyHost { get; set; }
        public string ProxyPort { get; set; }


        public void Init() {
            if (packageName == null) packageName = "seacat_winrt_client";

            FramePool = new FramePool();
            // works as a background thread by default
            this.ccoreThread = TaskHelper.CreateTask("CoreThread", () => _run());

            Bridge = new SeacatBridge();

            StorageFolder local = ApplicationData.Current.LocalFolder;
            Package package = Package.Current;


            int rc = Bridge.init((ISeacatCoreAPI)this, package.Id.Name, "dev", "wp8",
                local.Path + "\\.seacat"); // subdir must be specified since the core api adds a suffix to it

            RC.CheckAndThrowIOException("seacatcc.init", rc);

            lastState = Bridge.state();

            var frameProvidersComparator = Comparer<IFrameProvider>.Create((p1, p2) => {
                int p1pri = p1.GetFrameProviderPriority();
                int p2pri = p2.GetFrameProviderPriority();
                if (p1pri < p2pri) return -1;
                else if (p1pri == p2pri) return 0;
                else return 1;
            });

            // Setup frame provider priority queue
            frameProviders = new PriorityBlockingQueue<IFrameProvider>(frameProvidersComparator);


            // Create and register stream factory as control frame consumer
            streamFactory = new StreamFactory();
            cntlFrameConsumers.Add(SPDY.BuildFrameVersionType(SPDY.CNTL_FRAME_VERSION_ALX1, SPDY.CNTL_TYPE_SYN_REPLY), streamFactory);
            cntlFrameConsumers.Add(SPDY.BuildFrameVersionType(SPDY.CNTL_FRAME_VERSION_SPD3, SPDY.CNTL_TYPE_RST_STREAM), streamFactory);


            // Create and register ping factory as control frame consumer
            PingFactory = new PingFactory();
            cntlFrameConsumers.Add(SPDY.BuildFrameVersionType(SPDY.CNTL_FRAME_VERSION_SPD3, SPDY.CNTL_TYPE_PING), PingFactory);

            // Start reactor thread
            ccoreThread.Start();

            // Wait till started
            eventLoopStarted.WaitOne();
        }

        public void Shutdown() {
            Logger.Debug(TAG, "Shutdown");
            int rc = Bridge.shutdown();
            
            RC.CheckAndThrowIOException("seacatcc.shutdown", rc);

            TaskHelper.AbortTask(ccoreThread);
            if (!ccoreThread.Wait(5000)) {
                throw new IOException("Core thread is still alive!");
            }
        }


        private static void _run() {
            int rc = SeaCatClient.GetReactor().Bridge.run();
            if (rc != RC.RC_OK) {
                Logger.Debug(TAG, $"Return code {rc} in seacatcc.run");
            }
        }

        public void RegisterFrameProvider(IFrameProvider provider, bool single) {
            lock (frameProviders) {
                if ((single) && (frameProviders.Contains(provider))) return;
                frameProviders.Enqueue(provider);
            }

            // Yield to C-Core that we have frame to send
            int rc = Bridge.yield('W');
            if ((rc > 7900) && (rc < 8000)) {
                Logger.Debug(TAG, $"Return code {rc} in seacatcc.yield");
                rc = RC.RC_OK;
            }
            RC.CheckAndThrowIOException("seacatcc.yield", rc);
        }


        // ====== methods called from C++ ====== 

        public void LogMessage(char level, string message) {
            switch (level) {
                case 'D':
                Logger.Debug("CORE", message);
                break;
                case 'I':
                Logger.Info("CORE", message);
                break;
                case 'E':
                Logger.Error("CORE", message);
                break;
                case 'W':
                Logger.Warning("CORE", message);
                break;
                default:
                Logger.Info("CORE", message);
                break;
            }
        }

        public ByteBuffWrapper CallbackWriteReady() {
            TaskHelper.CheckInterrupt();

            Logger.Debug(TAG, "CallbackWriteReady");
            try {
                ByteBuffer frame = null;
                var providersToKeep = new List<IFrameProvider>();

                lock (frameProviders) {
                    while (frame == null) {
                        IFrameProvider provider = frameProviders.Dequeue();
                        if (provider == null) break;

                        bool keep = false;
                        frame = provider.BuildFrame(this, out keep);

                        if (keep) {
                            providersToKeep.Add(provider);
                        }
                    }

                    // order is irrelevant since the providers will be reordered in the queue
                    if (providersToKeep.Any()) {
                        foreach (var provider in providersToKeep) {
                            frameProviders.Enqueue(provider);
                        }
                    }
                }

                if (frame != null) frame.Flip();
                return CreateWrapper(frame);
            } catch (Exception e) {
                Logger.Error(TAG, $"Error while WriteReady: {e.Message}");
                return null;
            }
        }

        public ByteBuffWrapper CallbackReadReady() {
            TaskHelper.CheckInterrupt();

            Logger.Debug(TAG, "CallbackReadReady");
            try {
                // TODO_REF: what is the purpose of the reason parameter?
                var buffer = FramePool.Borrow("Reactor.CallbackReadReady");
                return CreateWrapper(buffer);
            } catch (Exception e) {
                Logger.Error(TAG, $"Error while ReadReady {e.Message}");
                return null;
            }
        }

        public void CallbackFrameReceived(ByteBuffWrapper frameWr, int frameLength) {
            TaskHelper.CheckInterrupt();

            Logger.Debug(TAG, $"CallbackFrameReceived {frameLength} length, {frameWr.position} position ");
            int pos = frameWr.position;
            frameWr.position = (pos + frameLength);

            ByteBuffer frame = new ByteBuffer(frameWr.data, frameWr.position, frameWr.limit);
            frame.Flip();

            byte fb = frame.GetByte(0);
            bool giveBackFrame = true;

            try {
                if ((fb & (1L << 7)) != 0) {
                    giveBackFrame = ReceivedControlFrame(frame);
                } else {
                    giveBackFrame = streamFactory.ReceivedDataFrame(this, frame);
                }
            } catch (Exception e) {
                Logger.Error(TAG, $"Erorr while receiving frame: {e.Message}");
                giveBackFrame = true;
            } finally {
                if (giveBackFrame) FramePool.GiveBack(frame);
            }
        }

        public void CallbackFrameReturn(ByteBuffWrapper frame) {
            TaskHelper.CheckInterrupt();

            Logger.Debug(TAG, $"CallbackFrameReturn {frame.data.Length} length, {frame.position} position ");
            FramePool.GiveBack(new ByteBuffer(frame.data, frame.position, frame.limit));
        }

        public void CallbackWorkerRequest(char worker) {
            TaskHelper.CheckInterrupt();

            Logger.Debug(TAG, "CallbackWorkerRequest : " + worker);
            switch (worker) {
                case 'P':
                // call ppkgen worker
                TaskHelper.CreateTask("PPkGen worker", () => Bridge.ppkgen_worker()).Start();
                break;
                case 'C':
                // create and call csr worker
                Task CSRWorker = CSR.CreateDefault();
                CSRWorker?.Start();
                var evt = new EventMessage(SeaCatClient.ACTION_SEACAT_CSR_NEEDED);
                EventDispatcher.Dispatcher.SendBroadcast(evt);
                break;
                default:
                Logger.Error(TAG, $"Unknown worker requested {worker}");
                break;
            }
        }



        public double CallbackEvLoopHeartBeat(double now) {
            // This method is called periodically from event loop (period is fairly arbitrary)
            // Return value of this method represent the longest time when it should be called again
            // It will very likely be called in shorter period too (as a result of heart beat triggered by other events)

            PingFactory.HeartBeat(now);
            FramePool.HeartBeat(now);

            // TODO: 26/11/2016 Find the best sleeping interval, can be much longer that 5 seconds, I guess
            return 5.0; // Seconds
        }

        public void CallbackEvloopStarted() {
            Logger.Debug(TAG, "CallbackEvloopStarted");
            Monitor.Enter(eventLoopNotStartedlock);
            eventLoopStarted.Set();
            Monitor.Exit(eventLoopNotStartedlock);

            Logger.Debug(TAG, "CallbackEvloopStarted : Sending broadcast");
            var evt = new EventMessage(SeaCatClient.ACTION_SEACAT_EVLOOP_STARTED);
            EventDispatcher.Dispatcher.SendBroadcast(evt);
        }

        public void CallbackGwconnConnected() {
            Logger.Debug(TAG, "CallbackGwconnConnected");
            var evt = new EventMessage(SeaCatClient.ACTION_SEACAT_GWCONN_CONNECTED);
            EventDispatcher.Dispatcher.SendBroadcast(evt);
        }

        public void CallbackGwconnReset() {
            Logger.Debug(TAG, "CallbackGwconnReset");
            PingFactory.Reset();
            streamFactory.Reset();
            var evt = new EventMessage(SeaCatClient.ACTION_SEACAT_GWCONN_RESET);
            EventDispatcher.Dispatcher.SendBroadcast(evt);
        }

        private bool isReady = false;

        public void CallbackStateChanged(string state) {
            Logger.Debug(TAG, "CallbackStateChanged to " + state);

            var evt = new EventMessage(SeaCatClient.ACTION_SEACAT_STATE_CHANGED);
            evt.PutExtra(SeaCatClient.EXTRA_STATE, state);
            evt.PutExtra(SeaCatClient.EXTRA_PREV_STATE, lastState);
            EventDispatcher.Dispatcher.SendBroadcast(evt);

            if ((lastState[0] != 'C') && (state[0] == 'C')) {
                ConfigureProxyServer(ProxyHost, ProxyPort);
            }


            bool isAnonymous = state[4] == 'A';
            bool isReady = (state[3] == 'Y' && state[4] == 'N' && state[0] != 'f');

            if (isReady) {
                this.isReadyHandle.Set();
            } else {
                this.isReadyHandle.Reset(); ;
            }

            lastState = state;
        }

        public void CallbackClientidChanged(string clientId, string clientTag) {
            Logger.Debug(TAG, $"CallbackClientidChanged {clientId} : {clientTag}");
            this.clientId = clientId;
            this.clientTag = clientTag;
            var evt = new EventMessage(SeaCatClient.ACTION_SEACAT_CLIENTID_CHANGED);
            evt.PutExtra(SeaCatClient.EXTRA_CLIENT_ID, clientId);
            evt.PutExtra(SeaCatClient.EXTRA_CLIENT_TAG, clientTag);
            EventDispatcher.Dispatcher.SendBroadcast(evt);
        }

        public void BroadcastState() {
            var evt = new EventMessage(SeaCatClient.ACTION_SEACAT_STATE_CHANGED);
            evt.PutExtra(SeaCatClient.EXTRA_STATE, Bridge.state());
            evt.PutExtra(SeaCatClient.EXTRA_PREV_STATE, lastState);
            EventDispatcher.Dispatcher.SendBroadcast(evt);
        }

        private bool ReceivedControlFrame(ByteBuffer frame) {
            int frameVersionType = frame.GetInt() & 0x7fffffff;

            int frameLength = frame.GetInt();
            byte frameFlags = (byte)(frameLength >> 24);
            frameLength &= 0xffffff;

            // check if frame is valid
            if (frameLength + SPDY.HEADER_SIZE != frame.Limit) {
                Logger.Error(TAG, $"Incorrect frame received: {frame.Limit} {frameVersionType} {frameLength} {frameFlags} - closing connection");
                // Invalid frame received -> disconnect from a gateway
                Bridge.yield('d'); // disconnect
                return true;
            }

            IFrameConsumer consumer = cntlFrameConsumers[frameVersionType];
            if (consumer == null) {
                Logger.Error(TAG, $"Unidentified Control frame received: {frame.Limit} {frameVersionType} {frameLength} {frameFlags}");
                return true;
            }

            return consumer.ReceivedControlFrame(this, frame, frameVersionType, frameLength, frameFlags);
        }

        protected void ConfigureProxyServer(string proxyHost, string proxyPort) {
            // TODO_RES: where to get proxyHost and proxyPort?
            if (!string.IsNullOrEmpty(proxyHost) && !string.IsNullOrEmpty(proxyPort)) {
                Logger.Debug(TAG, "Reconfiguring proxy server");
                this.ProxyHost = proxyHost;
                this.ProxyPort = proxyPort;
                int rc = Bridge.set_proxy_server_worker(proxyHost, proxyPort);
                RC.CheckAndLogError("seacatcc.set_proxy_server_worker", rc);
            }
        }

        public String GetClientTag() {
            return this.clientTag;
        }

        public String GetClientId() {
            return this.clientId;
        }

        public static void SetPackageName(String packageName) {
            Reactor.packageName = packageName;
        }

        private static ByteBuffWrapper CreateWrapper(ByteBuffer buffer) {
            if (buffer == null) {
                return null;
            }

            ByteBuffWrapper wrapper = new ByteBuffWrapper();
            wrapper.data = buffer.Data;
            wrapper.capacity = buffer.Capacity;
            wrapper.limit = buffer.Limit;
            wrapper.position = buffer.Position;
            return wrapper;
        }
    }
}