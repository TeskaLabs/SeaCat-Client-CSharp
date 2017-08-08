using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace seacat_client
{
    public class CoreAPI : seacat_core_bridge.ISeacatCoreAPI
    {
        public void LogMessage(IntPtr message)
        {
            string msgStr = Marshal.PtrToStringAnsi(message);
            System.Diagnostics.Debug.WriteLine(msgStr);
        }
    }
}
