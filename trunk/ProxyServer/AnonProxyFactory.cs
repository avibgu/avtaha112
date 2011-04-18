using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProxyServer {

    class AnonProxyFactory : ProxyFactory {

        public Proxy getProxy(System.Net.HttpListenerContext context) {

            return new AnonProxy(context);
        }
    }
}
