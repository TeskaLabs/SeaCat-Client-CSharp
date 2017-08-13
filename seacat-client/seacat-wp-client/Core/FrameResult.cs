using seacat_wp_client.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace seacat_wp_client.Core
{
    public class FrameResult
    {
        public ByteBuffer frame;
		public bool keep;

		public FrameResult(ByteBuffer frame, bool keep)
        {
            this.frame = frame;
            this.keep = keep;
        }
    };

}
