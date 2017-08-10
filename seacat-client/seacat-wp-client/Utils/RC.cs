using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace seacat_wp_client.Utils
{
    public static class RC
    {
        static public int RC_OK = (0);
        static public int RC_E_GENERIC = (-9999);

        public static void CheckAndThrowIOException(String message, int rc)
        {
            if (rc != RC_OK) throw new IOException(String.Format("SeaCat return code %d in %s", rc, message));
        }

        public static void CheckAndLogError(String message, int rc)
        {
            if (rc != RC_OK) System.Diagnostics.Debug.WriteLine(String.Format("SeaCat return code %d in %s", rc, message));
        }

    }

}
