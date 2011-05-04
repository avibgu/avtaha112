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
        private bool _chuncked;

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
            getRequestEmails();
          
            //  Set Default Credentials
            getHttpWReq().Credentials = CredentialCache.DefaultCredentials;

            //  Set GET/POST method
            getHttpWReq().Method = getContext().Request.HttpMethod;

            //  Sets the headers
            //  setTheHeaders();
            setHeadersNew();

            //  Sets the cookies
            setTheCookies();

            //  Print the headers
            printHeaders();

            // Forward the request

            bool ans;

            if (getChuncked() == false)
                ans = forwardRegularRequest();

            else
                ans = forwardChunckedRequest();

            if (!ans) return;

            /*
             * take the response from the remote server
             * and forward it as is to the client who initiated the connection.
             */
            try
            {
                // Get Response and Forward it
                if (getChuncked() == true)
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
            
            int index = urlStr.IndexOf(":" + Driver.port);

            urlStr = urlStr.Substring(0, index) + urlStr.Substring(index + 5);

            Console.WriteLine("URL: " + urlStr);

            setHttpWReq((HttpWebRequest)WebRequest.Create(urlStr));
        }

        /// <summary>
        /// 
        /// </summary>
        protected void getRequestEmails() {

            Stream stream = getContext().Request.InputStream;

            StreamReader streamReader = new StreamReader(stream);

            string body = streamReader.ReadToEnd();
            string headers = getContext().Request.Headers.ToString();
            string url = _url.OriginalString;
        
            string stringToCheck = body + " " + headers + " " + url;

            getEmails(stringToCheck);
        }

        protected void getEmails(string stringToCheck)
        {
            Regex reg = new Regex("[a-zA-Z0-9]*%40[a-zA-Z0-9]*.[a-z.A-Z]*");

            Match match = reg.Match(stringToCheck);

            while (match.Success)
            {
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
            getHttpWReq().ContentType = getContext().Request.ContentType;

            //  transfer encoding and chuncked
            string bla = getContext().Request.Headers.Get("Transfer-Encoding");

            if (0 == getContext().Request.ContentEncoding.EncodingName.CompareTo("chunked") ||
                (bla != null && 0 == bla.CompareTo("chunked")) )
            {
                Console.WriteLine("\n\n" + "chunked" + "\n");
                setChuncked(true);
                getHttpWReq().SendChunked = true;
                getHttpWReq().TransferEncoding = getContext().Request.ContentEncoding.EncodingName;
            }
        }

        protected void setHeadersNew()
        {
            NameValueCollection headers = getContext().Request.Headers;

            foreach(string header in headers.Keys){

                string[] values = headers.GetValues(header);

                string valueStr = "";

                foreach (string value in values)
                    valueStr += value + ";";

                valueStr = valueStr.Substring(0, valueStr.Length - 1);

                switch(header){

                    case "Proxy-Connection":
                        //getHttpWReq(). = valueStr;
                        break;

                    case "Keep-Alive":
                        getHttpWReq().KeepAlive = (0 == valueStr.CompareTo("true")) ? true : false;
                        break;

                    case "Accept":
                        getHttpWReq().Accept = valueStr;
                        break;

                    case "Accept-Charset":
                        //getHttpWReq().Char = valueStr;
                        break;

                    case "Accept-Encoding":
                        //getHttpWReq().TransferEncoding = valueStr;
                        break;

                    case "Transfer-Encoding":
                        getHttpWReq().TransferEncoding = valueStr;
                        break;

                    case "Accept-Language":
                        //getHttpWReq(). = valueStr;
                        break;

                    case "Host":
                        getHttpWReq().Host = valueStr;
                        break;

                    case "Referer":
                        getHttpWReq().Referer = valueStr;
                        break;

                    case "User-Agent":
                        getHttpWReq().UserAgent = valueStr;
                        break;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        protected void printHeaders(){

            Console.WriteLine("\n--------------");

            NameValueCollection headers = getHttpWReq().Headers;

            foreach(string header in headers.Keys){

                string[] values = headers.GetValues(header);

                Console.Write(header + " : ");
 
                foreach(string value in values)
                    Console.Write(value);

                Console.WriteLine();
            }

            Console.WriteLine("--------------\n");
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
        protected bool forwardRegularRequest()
        {
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
        protected bool forwardChunckedRequest()
        {
            
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

            StringBuilder responeContent = new StringBuilder();

            Stream responseStream = getHttpWResp().GetResponseStream();

            while ((numOfBytes = responseStream.Read(buffer, 0, 32)) != 0) {

                try
                {
                    responeContent.Append(buffer);
                    getContext().Response.OutputStream.Write(buffer, 0, numOfBytes);

                }
                catch (Exception e)
                {

                    Console.WriteLine("getContext().Response.OutputStream.Write(b, 0, b.Length) ERROR:\n" + e.Message);
                    return;
                }
            }

            getEmails(responeContent.ToString());
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

        public void setChuncked(bool value)
        {
            _chuncked = value;
        }

        public bool getChuncked()
        {
            return _chuncked;
        }
    }
}
