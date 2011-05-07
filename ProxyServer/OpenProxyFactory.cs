using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace ProxyServer {

    class OpenProxyFactory : ProxyFactory {
        /// <summary>
        /// Returns the open proxy object.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public Proxy getProxy(HttpListenerContext context) {
    
            return new OpenProxy(context);
        }
    }
}
