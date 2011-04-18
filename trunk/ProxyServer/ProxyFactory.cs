using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace ProxyServer {

    interface ProxyFactory {

        Proxy getProxy(HttpListenerContext context);
    }
}
