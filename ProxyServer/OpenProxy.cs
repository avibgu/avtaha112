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
        private Uri _url;
        private HttpWebRequest _httpWReq;
        private HttpWebResponse _httpWResp;

        public OpenProxy(HttpListenerContext context) {
            setContext(context);
            setUrl(null);
            setHttpWReq(null);
            setHttpWResp(null);
        }

        public void run()
        {
            /*
             * take the original request from the client to the remote server
             * and forward it as is to the remote server,
             * while adding header's values.
             */

            //  Get URL and create Web Request
            getUrlAndCreateWebRequest();

            //  Set GET/POST method
            getHttpWReq().Method = getContext().Request.HttpMethod;

            //  Sets the headers
            setTheHeaders();

            //  Sets the cookies
            setTheCookies();

            // Forward the request
            if (!forwardRequest()) return;

            /*
             * take the response from the remote server
             * and forward it as is to the client who initiated the connection.
             */

            // Get Response and Forward it
            getResponseAndForwardIt();

            // Close Connections..
            getContext().Response.OutputStream.Close();
            getHttpWResp().Close();

            return;
        }

        /// <summary>
        /// 
        /// </summary>
        private void getUrlAndCreateWebRequest() {

            _url = getContext().Request.Url;

            string urlStr = "http://" + _url.Host + _url.LocalPath;

            Console.WriteLine("URL: " + urlStr);

            setHttpWReq((HttpWebRequest)WebRequest.Create(urlStr));
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

            if (null != cookies && cookies.Count > 0)
                foreach (Cookie cookie in cookies)
                    getHttpWReq().CookieContainer.Add(_url,cookie);
        }

        /// <summary>
        /// 
        /// </summary>
        private bool forwardRequest() {

            try {

                setHttpWResp((HttpWebResponse)getHttpWReq().GetResponse());
            }
            catch (Exception e) {

                Console.WriteLine("setHttpWResp((HttpWebResponse)getHttpWReq().GetResponse()) ERROR:\n" + e.Message);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        private void getResponseAndForwardIt() {

            int numOfBytes = 0;

            Byte[] buffer = new Byte[32];

            Stream responseStream = getHttpWResp().GetResponseStream();

            while ((numOfBytes = responseStream.Read(buffer, 0, 32)) != 0) {

                try {

                    getContext().Response.OutputStream.Write(buffer, 0, numOfBytes);
                }
                catch (Exception e) {

                    Console.WriteLine("getContext().Response.OutputStream.Write(b, 0, b.Length) ERROR:\n" + e.Message);
                    return;
                }
            }
        }

        public void setContext(HttpListenerContext context){
            _context = context;
        }

        public HttpListenerContext getContext(){
            return _context;
        }

        public void setUrl(Uri url) {
            _url = url;
        }

        public Uri getUrl() {
            return _url;
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
