using seacat_wp_client.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace seacat_wp_client.Communication
{
    public interface IFrameProvider
    {
        FrameResult BuildFrame(Reactor reactor);
        int GetFrameProviderPriority();
    }
}
