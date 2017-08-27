using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace seacat_winrt_client.Socket {

    public class SocketDomain {

        public static SocketDomain AF_UNIX = new SocketDomain('u');
        public static SocketDomain AF_INET = new SocketDomain('4');
        public static SocketDomain AF_INET6 = new SocketDomain('6');

        private SocketDomain(char value) {
            this.Value = value;
        }
        public char Value { get; set; }
    }

    public class SocketType {
        public static SocketType SOCK_STREAM = new SocketType('s');
        public static SocketType SOCK_DGRAM = new SocketType('d');

        public SocketType(char value) {
            this.Value = value;
        }

        public char Value { get; set; }
    }
}
