﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeaCatCSharpClient {

    /// <summary>
    /// This class is for internal SeaCat client use only.
    /// It is not part of public API.
    /// </summary>
    public class SeaCatInternals {

        public static String SeaCatHostSuffix = ".seacat";
        public static String SeaCatPreferences = "seacat_preferences";
        public static String HTTPTAG = "::HTTP";

        private static Task CSRWorker = null;
        public static String applicationIdSuffix = null;
        public static bool logDebug = true;

        public static void SetCSRWorker(Task csrWorker) {
            CSRWorker = csrWorker;
        }
        public static Task GetCSRWorker() {
            return CSRWorker;
        }
    }
}
