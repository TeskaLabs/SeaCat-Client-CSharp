using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace seacat_wp_client.Utils
{
    public static class Logger
    {
        public static void Debug(string msg)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine("DEBUG::" + msg);
#endif
        }

        public static void Debug(string tag, string msg)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"{tag}::" + msg);
#endif
        }

        public static void Info(string msg)
        {
            System.Diagnostics.Debug.WriteLine("INFO:: " + msg);
        }

        public static void Info(string tag, string msg)
        {
            System.Diagnostics.Debug.WriteLine($"{tag}:: " + msg);
        }

        public static void Error(string msg)
        {
            System.Diagnostics.Debug.WriteLine("ERROR::" + msg);
        }

        public static void Error(string tag, string msg)
        {
            System.Diagnostics.Debug.WriteLine($"{tag}::" + msg);
        }

        public static void Warning(string msg)
        {
            System.Diagnostics.Debug.WriteLine("WARN::  " + msg);
        }

        public static void Warning(string tag, string msg)
        {
            System.Diagnostics.Debug.WriteLine($"{tag}::  " + msg);
        }
    }
}
