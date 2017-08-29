using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SeaCatCSharpClient.Http {
    public class HttpFrameContent : StreamContent {

        private InboundStream stream;

        public HttpFrameContent(InboundStream stream) : base(stream)
        {
        }
    }
}
