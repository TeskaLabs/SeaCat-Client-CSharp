using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace seacat_wp_client.Communication
{
    public class FrameResult
    {
        public MemoryStream frame;
		public bool keep;

		public FrameResult(MemoryStream frame, bool keep)
        {
            this.frame = frame;
            this.keep = keep;
        }
    };

}
