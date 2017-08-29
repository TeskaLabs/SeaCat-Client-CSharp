using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeaCatCSharpClient.Utils {

    /// <summary>
    /// Seacat logger
    /// </summary>
    public static class Logger {
        public static Action<string, string> loggerDelegate = null;

        /// <summary>
        /// Sets logger delegate so that all logs will be redirected
        /// </summary>
        /// <param name="logDel"></param>
        public static void SetDelegate(Action<string, string> logDel) {
            loggerDelegate = logDel;
        }

        public static void Debug(string tag, string msg) {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"{tag}:: {msg}");
#endif
            if (loggerDelegate != null) {
                loggerDelegate(tag, msg);
            }
        }

        public static void Info(string tag, string msg) {
            System.Diagnostics.Debug.WriteLine($"{tag}:: {msg}");
            if (loggerDelegate != null) {
                loggerDelegate(tag, msg);
            }
        }

        public static void Error(string tag, string msg) {
            System.Diagnostics.Debug.WriteLine($"___WARN___ {tag}:: {msg}");
            if (loggerDelegate != null) {
                loggerDelegate(tag, msg);
            }
        }

        public static void Warning(string tag, string msg) {
            System.Diagnostics.Debug.WriteLine($"___ERROR___ {tag}:: {msg}");
            if (loggerDelegate != null) {
                loggerDelegate(tag, msg);
            }
        }
    }
}
