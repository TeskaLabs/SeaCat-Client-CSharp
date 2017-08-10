using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace seacat_wp_client.Utils
{
    public static class MemoryStreamExtensions
    {
        public static void Clear(this MemoryStream stream)
        {
            stream.SetLength(0);
        }

        public static long Remaining(this MemoryStream stream)
        {
            return stream.Length - stream.Position;
        }
    }
}
