using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation.Metadata;

namespace seacat_wp_client
{
    public class CoreAPI : seacat_core_bridge.ISeacatCoreAPI
    {
        public void LogMessage(char level, string message)
        {
            System.Diagnostics.Debug.WriteLine(message);
        }
    }
}
