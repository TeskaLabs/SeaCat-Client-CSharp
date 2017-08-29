using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Web.Http;
using SeaCatCSharpClient.Core;
using SeaCatCSharpClient.Interfaces;
using SeaCatCSharpClient.Utils;
using HttpRequestMessage = System.Net.Http.HttpRequestMessage;
using HttpResponseMessage = System.Net.Http.HttpResponseMessage;
using HttpStatusCode = System.Net.HttpStatusCode;

namespace SeaCatCSharpClient.Http {

    public class SeacatHttpClientHandler : System.Net.Http.HttpClientHandler {
        private Reactor reactor;
        private int priority;
        public System.Net.Http.HttpClient HttpClient { get; set; }

        public SeacatHttpClientHandler(Reactor reactor, int priority) {
            this.reactor = reactor;
            this.priority = priority;
        }


        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken) {
            if (HttpClient == null) {
                throw new ArgumentException("Http Client mustn't be null!");
            }

            // create a new http sender for each call
            return new HttpSender(HttpClient, reactor, priority).SendAsync(request, cancellationToken);
        }
    }
}
