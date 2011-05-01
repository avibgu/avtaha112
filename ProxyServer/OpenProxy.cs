using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Collections.Specialized;
using System.Reflection;
using System.Text.RegularExpressions;

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

        public virtual void run()
        {
            /*
             * take the original request from the client to the remote server
             * and forward it as is to the remote server,
             * while adding header's values.
             */

            //  Get URL and create Web Request
            getUrlAndCreateWebRequest();

            //  Get emails from the request body
            getEmails(getContext().Request.InputStream);
          
            //  Set Default Credentials
            getHttpWReq().Credentials = CredentialCache.DefaultCredentials;

            //  Set GET/POST method
            getHttpWReq().Method = getContext().Request.HttpMethod;

            //  Sets the headers
            setTheHeaders();

            //  Sets the cookies
            setTheCookies();

            // Forward the request

            bool ans;

            if (0 == getHttpWReq().Method.CompareTo("GET"))
                ans = forwardGetRequest();

            else
                ans = forwardPostRequest();

            if (!ans) return;

            /*
             * take the response from the remote server
             * and forward it as is to the client who initiated the connection.
             */
            try
            {
                // Get Response and Forward it
                if (0 == getHttpWReq().Method.CompareTo("POST"))
                    setHttpWResp((HttpWebResponse)getHttpWReq().GetResponse());


                getResponseAndForwardIt();
            }
            catch { }
            // Close Connections..
            try { getContext().Response.OutputStream.Close(); } catch {}
            try { getHttpWResp().Close(); } catch {}

            return;
        }
        
        /// <summary>
        /// 
        /// </summary>
        protected void getUrlAndCreateWebRequest() {

            _url = getContext().Request.Url;

            string urlStr = _url.OriginalString;
            
            int index = urlStr.IndexOf(":8080");

            urlStr = urlStr.Substring(0, index) + urlStr.Substring(index + 5);

            Console.WriteLine("URL: " + urlStr);

            setHttpWReq((HttpWebRequest)WebRequest.Create(urlStr));
        }

        /// <summary>
        /// 
        /// </summary>
        protected void getEmails(Stream stream) {

            StreamReader streamReader = new StreamReader(stream);

            string body = streamReader.ReadToEnd();
            string headers = getContext().Request.Headers.ToString();
            string url = getContext().Request.RawUrl;
        
            string stringToCheck = body + " " + headers + " " + url;

            Regex reg = new Regex("[a-zA-Z0-9]*%40[a-zA-Z0-9]*.[a-z.A-Z]*");

            Match match = reg.Match(stringToCheck);

            while (match.Success) {
                string email = match.Value;

                lock (Driver.mailList)
                {
                    Driver.mailList.WriteLine(email);
                    Driver.mailList.Flush();
                }

                match = match.NextMatch();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        protected void setTheHeaders() {

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

            //  content length
            getHttpWReq().ContentLength =  getContext().Request.ContentLength64;

            //  content type
            getHttpWReq().ContentType = "application/x-www-form-urlencoded";
        }

        /// <summary>
        /// 
        /// </summary>
        protected void setTheCookies() {

            CookieCollection cookies = getContext().Request.Cookies;

            getHttpWReq().CookieContainer = new CookieContainer();

            if (null != cookies && cookies.Count > 0)
                foreach (Cookie cookie in cookies)
                    getHttpWReq().CookieContainer.Add(_url,cookie);
        }

        /// <summary>
        /// 
        /// </summary>
        protected bool forwardGetRequest() {

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
        protected bool forwardPostRequest() {
            Stream inputStream = null;
            StreamReader streamReader = null;
            Stream responseStream = null;
            StreamWriter streamWriter = null;

            try
            {

                inputStream = getContext().Request.InputStream;
                streamReader = new StreamReader(inputStream);
                string body = streamReader.ReadToEnd();

                responseStream = getHttpWReq().GetRequestStream();
                streamWriter = new StreamWriter(responseStream);
                streamWriter.Write(body);
            }
            catch (Exception e)
            {

                Console.WriteLine("forwardPostRequest() ERROR:\n" + e.Message);
                return false;
            }
            finally
            {
                if (getHttpWReq() != null)
                {
                    responseStream.Flush();
                    try
                    {
                        responseStream.Close();
                    }
                    catch { }
                }
            }
       
            return true;

        }

        /// <summary>
        /// 
        /// </summary>
        protected void getResponseAndForwardIt() {

            int numOfBytes = 0;

            Byte[] buffer = new Byte[32];

            Stream responseStream = getHttpWResp().GetResponseStream();

            while ((numOfBytes = responseStream.Read(buffer, 0, 32)) != 0) {

                try
                {

                    getContext().Response.OutputStream.Write(buffer, 0, numOfBytes);

                }
                catch (Exception e)
                {

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
