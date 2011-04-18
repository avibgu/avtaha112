using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace ProxyServer {

    class OpenProxyFactory : ProxyFactory {
        
        public Proxy getProxy(HttpListenerContext context) {
    
            return new OpenProxy(context);
        }
    }
}
