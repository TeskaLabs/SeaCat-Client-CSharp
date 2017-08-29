using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SeaCatCSharpBridge;
using Windows.Storage;
using Windows.ApplicationModel;
using SeaCatCSharpClient.Utils;
using SeaCatCSharpClient.Ping;
using System.Threading;
using SeaCatCSharpClient.Interfaces;
using System.IO;
using System.Net.Http;

namespace SeaCatCSharpClient.Core {

    /// <summary>
    /// Reactor responsible for communication between Seacat library and other layers
    /// </summary>
    public class Reactor : ISeacatCoreAPI {
        private static string TAG = "Reactor";

        public FramePool FramePool { get; private set; }
        public SeacatBridge Bridge { get; private set; }
        public string ProxyHost { get; set; }
        public string ProxyPort { get; set; }
        public string ClientTag { get; private set; } = "000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000";
        public string ClientId { get; private set; } = "[AAAAAAAAAAAAAAAA]";

        // handle that can be used to wait until the seacat is ready
        public EventWaitHandle IsReadyHandle { get; private set; } = new EventWaitHandle(false, EventResetMode.ManualReset);
        public PingFactory PingFactory { get; private set; }
        public StreamFactory StreamFactory { get; private set; }

        // frame consumers, divided by frame version type
        private Dictionary<int, IFrameConsumer> cntlFrameConsumers = new Dictionary<int, IFrameConsumer>();
        // queue of all frame providers
        private BlockingQueue<IFrameProvider> frameProviders;

        // last seacat state
        private string lastState;
        private Task ccoreThread;
        private EventWaitHandle eventLoopStarted = new EventWaitHandle(false, EventResetMode.ManualReset);

        public void Init(string appName, string appSuffix, string platform, string storageDir) {
            FramePool = new FramePool();
            StreamFactory = new StreamFactory();
            PingFactory = new PingFactory();

            try {
                Bridge = new SeacatBridge();
            } catch {
                throw new Exception("Either Seacat library or Bridge couldn't be loaded!");
            }
            
            // add seacat folder to the end
            if (!storageDir.EndsWith(".seacat")) {
                storageDir += "\\.seacat";
            }

            // init seacat
            int rc = Bridge.init((ISeacatCoreAPI)this, appName, appSuffix ?? "", platform, storageDir); // always
            RC.CheckAndThrowIOException("seacatcc.init", rc);
            lastState = Bridge.state();

            // Setup frame provider priority queue with given comparator
            frameProviders = new PriorityBlockingQueue<IFrameProvider>(Comparer<IFrameProvider>.Create((p1, p2) => {
                int p1pri = p1.FrameProviderPriority;
                int p2pri = p2.FrameProviderPriority;
                if (p1pri < p2pri) return -1;
                else if (p1pri == p2pri) return 0;
                else return 1;
            }));


            // Register stream and ping factories as control frame consumer
            cntlFrameConsumers.Add(SPDY.BuildFrameVersionType(SPDY.CNTL_FRAME_VERSION_ALX1, SPDY.CNTL_TYPE_SYN_REPLY), StreamFactory);
            cntlFrameConsumers.Add(SPDY.BuildFrameVersionType(SPDY.CNTL_FRAME_VERSION_SPD3, SPDY.CNTL_TYPE_RST_STREAM), StreamFactory);
            cntlFrameConsumers.Add(SPDY.BuildFrameVersionType(SPDY.CNTL_FRAME_VERSION_SPD3, SPDY.CNTL_TYPE_PING), PingFactory);

            // Start reactor thread
            this.ccoreThread = TaskHelper.CreateTask("CoreThread", () =>
            {
                int crc = Bridge.run();
                if (crc != RC.RC_OK) {
                    Logger.Debug(TAG, $"Return code {rc} in seacatcc.run");
                }
            });
            ccoreThread.Start();

            // wait for event loop
            eventLoopStarted.WaitOne();
        }

        /// <summary>
        /// Shuts the seacat down
        /// </summary>
        public void Shutdown() {
            Logger.Debug(TAG, "Shutdown");
            int rc = Bridge.shutdown();
            RC.CheckAndThrowIOException("seacatcc.shutdown", rc);
            TaskHelper.AbortTask(ccoreThread);
            if (!ccoreThread.Wait(5000)) {
                throw new IOException("Core thread is still alive!");
            }
        }

        /// <summary>
        /// Adds frame provider to the blocking queue
        /// </summary>
        /// <param name="provider">provider to add</param>
        /// <param name="single">if true, duplicate values won't be allowed</param>
        public void RegisterFrameProvider(IFrameProvider provider, bool single) {
            lock (frameProviders) {
                if ((single) && (frameProviders.Contains(provider))) return;
                frameProviders.Enqueue(provider);
            }

            // Yield to C-Core that we have frame to send
            int rc = Bridge.yield((char)RC.SeacatYields.DATA_TO_SEND);
            if ((rc > 7900) && (rc < 8000)) {
                // ignore error
                Logger.Debug(TAG, $"Return code {rc} in seacatcc.yield");
                rc = RC.RC_OK;
            }
            RC.CheckAndThrowIOException("seacatcc.yield", rc);
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
                Bridge.yield((char)RC.SeacatYields.DISCONNECT); // disconnect
                return true;
            }

            // get initialized consumer according to the frame version type
            IFrameConsumer consumer = null;
            cntlFrameConsumers.TryGetValue(frameVersionType, out consumer);

            if (consumer == null) {
                Logger.Error(TAG, $"Unidentified Control frame received: {frame.Limit} {frameVersionType} {frameLength} {frameFlags}");
                return true;
            }

            return consumer.ReceivedControlFrame(this, frame, frameVersionType, frameLength, frameFlags);
        }

        protected void ConfigureProxyServer(string proxyHost, string proxyPort) {
            if (!string.IsNullOrEmpty(proxyHost) && !string.IsNullOrEmpty(proxyPort)) {
                Logger.Debug(TAG, "Reconfiguring proxy server");
                this.ProxyHost = proxyHost;
                this.ProxyPort = proxyPort;
                int rc = Bridge.set_proxy_server_worker(proxyHost, proxyPort);
                RC.CheckAndLogError("seacatcc.set_proxy_server_worker", rc);
            }
        }

        /// <summary>
        /// Creates wrapper for a byte buffer that is to be passed to unmanaged code
        /// </summary>
        /// <returns></returns>
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

        // =====================================================================================
        // ============================== METHODS CALLED FROM C++ ==============================
        // =====================================================================================

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
                        // find provider that will build the frame
                        IFrameProvider provider = frameProviders.Dequeue();
                        if (provider == null) break;

                        // indicates whether the provider should be put back into the queue
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

                // flip frame to read mode
                frame?.Flip();
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
                // borrow a free frame and pass it to the seacat
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

            // prepare buffer for writing (data won't be copied!)
            ByteBuffer frame = new ByteBuffer(frameWr.data, frameWr.position, frameWr.limit);
            frame.Flip();

            // get type of frame
            byte fb = frame.GetByte(0);
            bool giveBackFrame = true;

            try {
                if ((fb & (1L << 7)) != 0) {
                    // 1xxx -> control frame
                    giveBackFrame = ReceivedControlFrame(frame);
                } else {
                    // 0xxx -> data frame
                    giveBackFrame = StreamFactory.ReceivedDataFrame(this, frame);
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
            // just give the allocated frame back to the frame pool since it is no longer needed
            FramePool.GiveBack(new ByteBuffer(frame.data, frame.position, frame.limit));
        }

        public void CallbackWorkerRequest(char worker) {
            TaskHelper.CheckInterrupt();
            Logger.Debug(TAG, "CallbackWorkerRequest : " + worker);

            switch (worker) {
                case 'P':
                // call ppkgen worker in a separate thread
                TaskHelper.CreateTask("PPkGen worker", () => Bridge.ppkgen_worker()).Start();
                break;
                case 'C':
                // create and call csr worker in a separate thread
                Task CSRWorker = CSR.CreateDefault();
                CSRWorker?.Start();
                // notify observers
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
            return 5.0; // in seconds
        }

        public void CallbackEvloopStarted() {
            Logger.Debug(TAG, "CallbackEvloopStarted");
            // set the handle and notify observers
            eventLoopStarted.Set();
            var evt = new EventMessage(SeaCatClient.ACTION_SEACAT_EVLOOP_STARTED);
            EventDispatcher.Dispatcher.SendBroadcast(evt);
        }

        public void CallbackGwconnConnected() {
            Logger.Debug(TAG, "CallbackGwconnConnected");
            // notify observers
            var evt = new EventMessage(SeaCatClient.ACTION_SEACAT_GWCONN_CONNECTED);
            EventDispatcher.Dispatcher.SendBroadcast(evt);
        }

        public void CallbackGwconnReset() {
            Logger.Debug(TAG, "CallbackGwconnReset");
            PingFactory.Reset();
            StreamFactory.Reset();
            // notify observers
            var evt = new EventMessage(SeaCatClient.ACTION_SEACAT_GWCONN_RESET);
            EventDispatcher.Dispatcher.SendBroadcast(evt);
        }

        public void CallbackStateChanged(string state) {
            Logger.Debug(TAG, $"CallbackStateChanged to {state} :: {RC.TranslateState(state)}");

            // notify observers
            var evt = new EventMessage(SeaCatClient.ACTION_SEACAT_STATE_CHANGED);
            evt.PutExtra(SeaCatClient.EXTRA_STATE, state);
            evt.PutExtra(SeaCatClient.EXTRA_PREV_STATE, lastState);
            EventDispatcher.Dispatcher.SendBroadcast(evt);

            if ((lastState[0] != (char)RC.SeacatState.CONNECTING) && (state[0] == (char)RC.SeacatState.CONNECTING)) {
                ConfigureProxyServer(ProxyHost, ProxyPort);
            }
            
            // check ready state
            bool isReady = (
                state[3] == (char)RC.SeacatState.PPK_READY && 
                state[4] == (char)RC.SeacatState.GWCONN_SIGNED_IN && 
                state[0] != (char)RC.SeacatState.ERROR_FATAL);

            if (isReady) {
                IsReadyHandle.Set();
            } else {
                IsReadyHandle.Reset();
            }

            lastState = state;
        }

        public void CallbackClientidChanged(string clientId, string clientTag) {
            Logger.Debug(TAG, $"CallbackClientidChanged {clientId} : {clientTag}");
            ClientId = clientId;
            ClientTag = clientTag;
            // notify observers
            var evt = new EventMessage(SeaCatClient.ACTION_SEACAT_CLIENTID_CHANGED);
            evt.PutExtra(SeaCatClient.EXTRA_CLIENT_ID, clientId);
            evt.PutExtra(SeaCatClient.EXTRA_CLIENT_TAG, clientTag);
            EventDispatcher.Dispatcher.SendBroadcast(evt);
        }
    }
}
