using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;

namespace ProxyServer
{
    class AnonProxy : Proxy
    {
        private Socket _socket;
        private HttpListenerContext _context;

        public AnonProxy(HttpListenerContext context) {
            setContext(context);
        }

        public void run()
        {
            return;
        }

        public void setContext(HttpListenerContext context)
        {
            _context = context;
        }

        public HttpListenerContext setContext()
        {
            return _context;
        }

        public void setSocket(Socket socket)
        {
            _socket = socket;
        }

        public Socket getSocket()
        {
            return _socket;
        }
    }
}
