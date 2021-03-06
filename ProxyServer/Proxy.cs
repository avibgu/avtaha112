﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;


/// Browser                            Proxy                     HTTP server
///   Open TCP connection  
///   Send HTTP request  ----------->                       
///                                  Read HTTP header
///                                  detect Host header
///                                  Send request to HTTP ----------->
///                                  Server
///                                                       <-----------
///                                  Read response and send
///                    <-----------  it back to the browser
/// Render content

namespace ProxyServer
{
    interface Proxy
    {
        /// <summary>
        /// Interface for the proxy server.
        /// </summary>
        void run();
        void setContext(HttpListenerContext context);
    }
}
