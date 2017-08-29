using SeaCatCSharpClient.Core;
using SeaCatCSharpClient.Http;
using SeaCatCSharpClient.Socket;
using SeaCatCSharpClient.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SeaCatCSharpClient {

    /// <summary>
    /// This class represents the main interface of TeskaLabs SeaCat client for Windows Phone (aka WP SeaCat SDK).<br>
    /// It consists exclusively of static methods that provide SeaCat functions.
    /// </summary>
    public class SeaCatClient {

        private static Reactor reactor = null;

        /// <summary>
        /// The event category for all intents sent by SeaCat client
        /// </summary>
        public static String CATEGORY_SEACAT = "mobi.seacat.client.event.category.SEACAT";

        /// <summary>
        /// The event action used to inform that client is up and running
        /// </summary>
        public static String ACTION_SEACAT_EVLOOP_STARTED = "mobi.seacat.client.event.action.EVLOOP_STARTED";

        /// <summary>
        /// The event action used to inform that client is successfully connected to the gateway.
        /// </summary>
        public static String ACTION_SEACAT_GWCONN_CONNECTED = "mobi.seacat.client.event.action.GWCONN_CONNECTED";

        /// <summary>
        /// The event action used to inform that client disconnected from the gateway.
        /// </summary>
        public static String ACTION_SEACAT_GWCONN_RESET = "mobi.seacat.client.event.action.GWCONN_RESET";

        /// <summary>
        /// The event action used to inform that client needs to produce CSR.
        /// </summary>
        public static String ACTION_SEACAT_CSR_NEEDED = "mobi.seacat.client.event.action.CSR_NEEDED";

        /// <summary>
        /// The event action used to inform that client state changed.
        /// Detailed information about state change is in EXTRA_STATE.
        /// </summary>
        public static String ACTION_SEACAT_STATE_CHANGED = "mobi.seacat.client.event.action.STATE_CHANGED";

        public static String ACTION_SEACAT_CLIENTID_CHANGED = "mobi.seacat.client.event.action.CLIENTID_CHANGED";

        /// <summary>
        /// The key to event extras with information about client state.<br>
        /// Used in ACTION_SEACAT_STATE_CHANGED events.
        /// </summary>
        public static String EXTRA_STATE = "SEACAT_STATE";
        public static String EXTRA_PREV_STATE = "SEACAT_PREV_STATE";

        public static String EXTRA_CLIENT_ID = "SEACAT_CLIENT_ID";
        public static String EXTRA_CLIENT_TAG = "SEACAT_CLIENT_TAG";

        /// <summary>
        /// Initialize SeaCat Windows Phone client.<br/>
        /// SeaCat client needs to be initialized prior any other function is called.<br/>
        /// Please refer to example above.
        /// </summary>
        public static void Initialize(string appName, string platform, string storageDir) {
            SeaCatClient.Initialize(CSR.CreateDefault(), appName, null, platform, storageDir);
        }

        public static void Initialize(string appName, string appSuffix, string platform, string storageDir) {
            SeaCatClient.Initialize(CSR.CreateDefault(), appName, appSuffix, platform, storageDir);
        }

        public static void Initialize(Task CSRworker, string appName, string appSuffix, string platform, string storageDir) {
            SeaCatInternals.applicationIdSuffix = appSuffix;
            SetCSRWorker(CSRworker);

            try {
                reactor = new Reactor();
                reactor.Init(appName, appSuffix, platform, storageDir);
                // Process plugins
                SeaCatPlugin.CommitCapabilities();
            } catch (IOException e) {
                Logger.Error("SeaCatClient", $"Exception during SeaCat reactor start {e.Message}");
            }
        }

        /// <summary>
        /// Triggers sending of an ACTION_SEACAT_STATE_CHANGED event even if the state has not changed.
        /// </summary>
        public static void BroadcastState() {
            Reactor reactor = GetReactor();
            reactor?.BroadcastState();
        }

        public static Reactor GetReactor() {
            return SeaCatClient.reactor;
        }


        /// <summary>
        /// Pings SeaCat gateway.
        ///
        /// This function can be used to keep the connection to SeaCat gateway open.
        /// </summary>
        public static void Ping(Ping.Ping ping) {
            GetReactor().PingFactory.Ping(reactor, ping);
        }

        public static void Ping() {
            GetReactor().PingFactory.Ping(reactor, new Ping.Ping() { });
        }


        public static HttpClient Open()
        {
            var handler = new SeacatHttpClientHandler(GetReactor(), 3);
            var client = new HttpClient(handler);
            handler.HttpClient = client;
            return client;
        }

        /// <summary>
        /// Obtains the state string describing operational conditions of a SeaCat client.
        ///
        /// The state string is a fixed-length six characters long representation of different SeaCat components.
        /// Refer to SeaCat C-Core documentation for detailed information.
        ///
        /// <returns>The actual state string.</returns>
        /// </summary>
        public static String GetState() => reactor.Bridge.state();

        /// <summary>
        /// Connects to SeaCat gateway
        /// </summary>
        public static void Connect() {
            int rc = reactor.Bridge.yield('c');
            RC.CheckAndThrowIOException("seacatcc.yield(connect)", rc);
        }

        /// <summary>
        /// Disconnect from SeaCat gateway.
        /// 
        /// Instruct SeaCat client to close a connection to the SeaCat gateway.
        /// There is only little need to call this function directly, SeaCat client control connection automatically.
        /// </summary>
        public static void Disconnect() {
            int rc = reactor.Bridge.yield('d');
            RC.CheckAndThrowIOException("seacatcc.yield(disconnect)", rc);
        }

        /// <summary>
        /// Resets the identity of the SeaCat client.
        /// 
        /// Removes client private key and all relevant artifacts such as client certificate.
        /// It puts client state to an initial form, effectively restarts all automated routines to obtain identity via CSR.
        /// </summary>
        public static void Reset() {
            int rc = reactor.Bridge.yield('r');
            RC.CheckAndThrowIOException("seacatcc.yield(reset)", rc);
        }

        public static void Renew() {
            int rc = reactor.Bridge.yield('n');
            RC.CheckAndThrowIOException("seacatcc.yield(renew)", rc);
        }

        public static void SetCSRWorker(Task csrWorker) {
            SeaCatInternals.SetCSRWorker(csrWorker);
        }

        public static void SetLogMask(LogFlag mask) {
            int rc = reactor.Bridge.log_set_mask(mask.Value);
            RC.CheckAndThrowIOException("seacatcc.log_set_mask()", rc);

            SeaCatInternals.logDebug = mask.ContainsMask(LogFlag.DEBUG_GENERIC.Value);
        }

        public static void CconfigureSocket(int port, SocketDomain domain, SocketType type, int protocol, string peerAddress, string peerPort) {
            int rc = reactor.Bridge.socket_configure_worker(port, domain.Value, type.Value, protocol, peerAddress, peerPort);
            RC.CheckAndThrowIOException("seacatcc.socket_configure_worker()", rc);
        }

        public static string GetClientId() {
            return GetReactor()?.ClientId;
        }

        public static string GetClientTag() {
            return GetReactor()?.ClientTag;
        }

        private SeaCatClient() { }  // This is static-only class, so we hide constructor
    }
}
