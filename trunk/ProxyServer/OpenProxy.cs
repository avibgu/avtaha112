﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Collections.Specialized;
using System.Reflection;

namespace ProxyServer
{
    class OpenProxy : Proxy
    {
        private Socket _socket;
        private HttpListenerContext _context;

        public OpenProxy(HttpListenerContext context) {
            setContext(context);
        }

        public void run()
        {
            // take the original request from the client to the remote server
            // and forward it as is to the remote server,
            // while adding header's values.

            Uri url = getContext().Request.Url;

            // TODO: download files..
            if (url.IsFile)
                return;

            string urlStr = "http://" + url.Host + url.LocalPath;

            Console.WriteLine("URL: " + urlStr);

            HttpWebRequest HttpWReq = (HttpWebRequest)WebRequest.Create(urlStr);

            NameValueCollection headers = getContext().Request.Headers;
/*
            PropertyInfo p = headers.GetType().GetProperty("IsReadOnly", BindingFlags.Instance |
                                                    BindingFlags.IgnoreCase | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
            p.SetValue(headers, false, null);
*/
            getContext().Request.Headers.Add(HttpWReq.Headers);

            //  HttpWReq.TransferEncoding = getContext().Request.T;
            HttpWReq.Accept = getContext().Request.AcceptTypes[0];
            //  HttpWReq.Connection = ;
            HttpWReq.ContentType= getContext().Request.ContentType;
            HttpWReq.ContentLength = getContext().Request.ContentLength64;
            //  HttpWReq.Expect = ;
            //  HttpWReq.IfModifiedSince = ;
            //  HttpWReq.Proxy = ;
            //  HttpWReq.Referer = getContext().Request.UrlReferrer.AbsoluteUri;
            HttpWReq.UserAgent = getContext().Request.UserAgent;

/*
            foreach (string key in headers.Keys)
            {
                string[] values = headers.GetValues(key);

                foreach (string value in values)
                    HttpWReq.Headers.Add(key, value);
            }
*/
            HttpWReq.Headers.Add("x-forwarded-for", "127.0.0.1");
            HttpWReq.Headers.Add("proxy-version", "0.17");

            CookieCollection cookies = getContext().Request.Cookies;

            HttpWReq.CookieContainer = new CookieContainer();

            if (null != cookies && cookies.Count > 0) {

                foreach (Cookie cookie in cookies) {

                    if (cookie.Domain.Equals(""))
                        continue;

                    HttpWReq.CookieContainer.Add(cookie);
                }
            }

            HttpWebResponse HttpWResp = (HttpWebResponse)HttpWReq.GetResponse();

            // take the response from the remote server
            // and forward it as is to the client who initiate the connection.

            Stream responseStream = HttpWResp.GetResponseStream();

            string charSet = HttpWResp.CharacterSet;

            Encoding encode;
            
            if (String.IsNullOrEmpty(charSet))
                encode = Encoding.Default;

            else
                encode = Encoding.GetEncoding(charSet);

            StreamReader streamReader = new StreamReader(responseStream, encode);

            char[] buffer = new char[100];

            String response = "";

            while (streamReader.Read(buffer, 0, buffer.Length) > 0)
                response += new String(buffer, 0, buffer.Length);

            byte[] b = Encoding.UTF8.GetBytes(response);
            getContext().Response.ContentLength64 = b.Length;
            getContext().Response.OutputStream.Write(b, 0, b.Length);
            getContext().Response.OutputStream.Close();

            streamReader.Close();
            responseStream.Close();
            HttpWResp.Close();

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
