using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;

namespace ProxyServer
{
    class OpenProxy : Proxy
    {
        private Socket _socket;
        private  HttpListenerContext _context;

        public void run()
        {
            // TODO:    take the original request from the client to the remote server
            //          and forward it as is to the remote server,
            //          while adding header's values.

            string response = "<html><body><p>Avi and Shiran Proxy</p></body></html>";

            getContext().Response.OutputStream.Write(Encoding.ASCII.GetBytes(response), 0, response.Length);

            // TODO:    take the response from the remote server
            //          and forward it as is to the client who initiate the connection.

            return;
        }

        public void setContext(HttpListenerContext context)
        {
            _context = context;
        }

        public HttpListenerContext getContext()
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
