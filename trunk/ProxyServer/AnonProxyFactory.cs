using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProxyServer {

    class AnonProxyFactory : ProxyFactory {
        /// <summary>
        /// Returns the anonymous proxy object.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public Proxy getProxy(System.Net.HttpListenerContext context) {

            return new AnonProxy(context);
        }
    }
}
