using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeaCatCSharpClient.Utils {

    public static class RC {

        public enum SeacatState {
            SEACATCC_STATE_NOT_INITED = '*',
            SEACATCC_STATE_INITED = 'i',
            SEACATCC_STATE_IDLING = 'D',
            SEACATCC_STATE_CONNECTING = 'C',
            SEACATCC_STATE_PROXY_REQ = 'p',    // Proxy support
            SEACATCC_STATE_PROXY_RESP = 'P',   // Proxy support
            SEACATCC_STATE_HANDSHAKING = 'H',
            SEACATCC_STATE_ESTABLISHED = 'E',
            SEACATCC_STATE_CLOSING = 'c',

            SEACATCC_STATE_ERROR_RETRY = 'r',
            SEACATCC_STATE_ERROR_NETWORK = 'n',
            SEACATCC_STATE_ERROR_FATAL = 'f',
        }

        static public int RC_OK = (0);
        static public int RC_E_GENERIC = (-9999);

        public static void CheckAndThrowIOException(String message, int rc) {
            if (rc != RC_OK) throw new IOException($"SeaCat return code {rc} in {message}");
        }

        public static void CheckAndLogError(String message, int rc) {
            if (rc != RC_OK) Logger.Error("CORE", $"SeaCat return code {rc} in {message}");
        }

        public static string TranslateState(string state) {
            var builder = new StringBuilder();
            foreach (char st in state) {
                try {
                    SeacatState seacatState = (SeacatState)st;
                    string stringValue = seacatState.ToString();
                    builder.Append(stringValue);
                    builder.Append(";");
                } catch {
                    // nothing to do here
                }
            }
            return builder.ToString();
        }
    }

}
