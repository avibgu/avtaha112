using System;
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
        private HttpListenerContext _context;
        private HttpWebRequest _httpWReq;
        private HttpWebResponse _httpWResp;

        public OpenProxy(HttpListenerContext context) {
            setContext(context);
            setHttpWReq(null);
            setHttpWResp(null);
        }

        public void run()
        {
            // take the original request from the client to the remote server
            // and forward it as is to the remote server,
            // while adding header's values.

            Uri url = getContext().Request.Url;

            string urlStr = "http://" + url.Host + url.LocalPath;

            Console.WriteLine("URL: " + urlStr);

            setHttpWReq((HttpWebRequest)WebRequest.Create(urlStr));

            //  Sets the headers
            setTheHeaders();

            //  Sets the cookies
            setTheCookies();

            try{

                setHttpWResp((HttpWebResponse)getHttpWReq().GetResponse());
            }
            catch(Exception e){

                Console.WriteLine("setHttpWResp((HttpWebResponse)getHttpWReq().GetResponse()) ERROR:\n" + e.Message);
                return;
            }

            // take the response from the remote server
            // and forward it as is to the client who initiate the connection.

            Stream responseStream = getHttpWResp().GetResponseStream();

            string charSet = getHttpWResp().CharacterSet;

            Encoding encode;
            
            try{

                encode = Encoding.GetEncoding(charSet);
            }
            catch{

                encode = Encoding.Default;
            }                

            StreamReader streamReader = new StreamReader(responseStream, encode);

            char[] buffer = new char[100];

            String response = "";

            response = streamReader.ReadToEnd();

            byte[] b = System.Text.UTF8Encoding.UTF8.GetBytes(response);

            try
            {
                getContext().Response.ContentLength64 = b.Length;
                getContext().Response.OutputStream.Write(b, 0, b.Length);
                getContext().Response.OutputStream.Close();
            }
            catch(Exception e){

                Console.WriteLine("getContext().Response.OutputStream.Write(b, 0, b.Length) ERROR:\n" + e.Message);
                return;
            }

            streamReader.Close();
            responseStream.Close();
            getHttpWResp().Close();

            return;
        }

        /// <summary>
        /// 
        /// </summary>
        private void setTheHeaders() {

            //  User-Agent:
            getHttpWReq().UserAgent = getContext().Request.UserAgent;

            //  Accept:
            string[] acceptTypes = getContext().Request.AcceptTypes;

            string acceptTypesStr = "";

            foreach (string type in acceptTypes)
                acceptTypesStr += "," + type;

            getHttpWReq().Accept = acceptTypesStr.Substring(1);

            //  x-forwarded-for:
            System.Net.IPHostEntry ips = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());

            string xForwardedFor = "";

            foreach (IPAddress ip in ips.AddressList)
                xForwardedFor = ip.ToString() + "," + xForwardedFor;

            xForwardedFor = ips.AddressList.GetValue(ips.AddressList.Length - 1).ToString() + ", " + xForwardedFor;

            getHttpWReq().Headers.Add("x-forwarded-for", xForwardedFor);

            //  proxy-version:
            getHttpWReq().Headers.Add("proxy-version", "0.17");
        }

        /// <summary>
        /// 
        /// </summary>
        private void setTheCookies() {

            CookieCollection cookies = getContext().Request.Cookies;

            getHttpWReq().CookieContainer = new CookieContainer();

            if (null != cookies && cookies.Count > 0) {

                foreach (Cookie cookie in cookies) {

                    //TODO - fix it..
                    if (cookie.Domain.Equals(""))
                        continue;

                    getHttpWReq().CookieContainer.Add(cookie);
                }
            }
        }

        public void setContext(HttpListenerContext context){
            _context = context;
        }

        public HttpListenerContext getContext(){
            return _context;
        }

        public void setHttpWReq(HttpWebRequest httpWReq) {
            _httpWReq = httpWReq;
        }

        public HttpWebRequest getHttpWReq() {
            return _httpWReq;
        }

        public void setHttpWResp(HttpWebResponse httpWResp) {
            _httpWResp = httpWResp;
        }

        public HttpWebResponse getHttpWResp(){
            return _httpWResp;
        }
    }
}
