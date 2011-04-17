using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.IO;

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
            Uri url = getContext().Request.Url;

            string urlStr = "http://" + url.Host + url.LocalPath;

            Console.WriteLine("URL: " + urlStr);

            HttpWebRequest HttpWReq = (HttpWebRequest)WebRequest.Create(urlStr);

            HttpWReq.Headers.Add("x-forwarded-for", "127.0.0.1");
            HttpWReq.Headers.Add("proxy-version", "0.17");

            HttpWebResponse HttpWResp = (HttpWebResponse)HttpWReq.GetResponse();

            Stream responseStream = HttpWResp.GetResponseStream();

            Encoding encode = System.Text.Encoding.GetEncoding("ascii");

            StreamReader streamReader = new StreamReader(responseStream, encode);

            char[] buffer = new char[100];

            String response = "";

            while (streamReader.Read(buffer, 0, buffer.Length) > 0)
                response += new String(buffer, 0, buffer.Length);

            byte[] b = Encoding.ASCII.GetBytes(response);
            //getContext().Response.ContentLength64 = b.Length;
            getContext().Response.OutputStream.Write(b, 0, b.Length);
            getContext().Response.OutputStream.Close();

            streamReader.Close();
            responseStream.Close();
            HttpWResp.Close();

            //Console.WriteLine("Trying to send response..");
            //Console.WriteLine("Response has been sent..");

            //TODO:    take the response from the remote server
            //         and forward it as is to the client who initiate the connection.

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
