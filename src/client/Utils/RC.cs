using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeaCatCSharpClient.Utils {

    public static class RC {

        public enum SeacatState {
            NOT_INITED = '*',
            INITED = 'i',
            IDLING = 'D',
            CONNECTING = 'C',
            PROXY_REQ = 'p',    // Proxy support
            PROXY_RESP = 'P',   // Proxy support
            HANDSHAKING = 'H',
            ESTABLISHED = 'E',
            CLOSING = 'c',
            PPK_READY = 'Y',
            GWCONN_ANONYMOUS = 'A',
            GWCONN_SIGNED_IN = 'N',
            ERROR_RETRY = 'r',
            ERROR_NETWORK = 'n',
            ERROR_FATAL = 'f',
        }

        public enum SeacatYields {
            CONNECT = 'c',
            DISCONNECT = 'd',
            RENEW_CERT = 'n',
            RECOVER_FATAL = 'f',
            NETWORK_REACHABLE = 'Q',
            DATA_TO_SEND = 'W'
        }
        
        static public int RC_OK = 0;
        static public int RC_E_GENERIC = -9999;

        public static void CheckAndThrowIOException(String message, int rc) {
            if (rc != RC_OK) throw new IOException($"SeaCat return code {rc} in {message}");
        }

        public static void CheckAndLogError(String message, int rc) {
            if (rc != RC_OK) Logger.Error("CORE", $"SeaCat return code {rc} in {message}");
        }

        public static string TranslateState(string state) {
            var builder = new StringBuilder();
            foreach (char st in state) {
                if (Enum.IsDefined(typeof(SeacatState), (SeacatState)st)) {
                    SeacatState seacatState = (SeacatState)st;
                    string stringValue = seacatState.ToString();
                    builder.Append(stringValue);
                    builder.Append("|");
                }
            }
            return builder.ToString();
        }

        public static string TranslateYield(char yld) {
            if (Enum.IsDefined(typeof(SeacatYields), (SeacatYields)yld)) {
                SeacatState yldVal = (SeacatState)yld;
                return yldVal.ToString();
            }
            return "???";
        }
    }

}
