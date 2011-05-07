﻿using System;
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
        /// <summary>
        ///  This class used for the open state. run the user request thread.
        /// </summary>
        private HttpListenerContext _context;
        private Uri _url;
        private HttpWebRequest _httpWReq;
        private HttpWebResponse _httpWResp;
        private string _xForwardedFor;
        private string _proxyVersion;
        public string _requestString;

        /// <summary>
        /// This is the constructor of the Open Proxy.
        /// </summary>
        /// <param name="context"> Gets the context of the connection as argument</param>
        /// <author>Avi Digmi</author>
        public OpenProxy(HttpListenerContext context) {
            setContext(context);
            setUrl(null);
            setHttpWReq(null);
            setHttpWResp(null);
            setXForwardedFor("");
            setProxyVersion("0.17");
        }

        /// <summary>
        /// This is the main method of the Open Proxy.
        /// Responsable on taking the original request from the web browser
        /// and sending it, after some modifications, to the web server.
        /// It also handles the sent of the response from the web server to the web client.
        /// </summary>
        /// <author>Avi Digmi</author>
        public virtual void run()
        {
            /*
             * take the original request from the client to the remote server
             * and forward it as is to the remote server,
             * while adding header's values.
             */

            // Set the input stream of the request
            _requestString = getInputStream();

            //  Get URL and create Web Request
            getUrlAndCreateWebRequest();

            //  Get emails from the request body
            getRequestEmails();
          
            //  Set Default Credentials
            getHttpWReq().Credentials = CredentialCache.DefaultCredentials;

            //  Set GET/POST method
            getHttpWReq().Method = getContext().Request.HttpMethod;

            //  Sets the headers
            setOriginalRequestHeaders();
            setAdditionalHeaders();

            //  Sets the cookies
            setTheCookies();

            //  Print the headers
            printWebRequestHeaders();

            // Forward the request
            bool ans;

            if (getHttpWReq().SendChunked == true || getContext().Request.HttpMethod == "POST")
                ans = forwardChunckedRequest();
            else
                ans = forwardRegularRequest();

            if (!ans) return;

            /*
             * take the response from the remote server
             * and forward it as is to the client who initiated the connection.
             */
            try
            {
                // Get Response and Forward it
                if (getHttpWReq().SendChunked == true || getContext().Request.HttpMethod == "POST")
                    setHttpWResp((HttpWebResponse)getHttpWReq().GetResponse());

                  getResponseAndForwardIt();
             //   getResponseAndForwardIt2();
            }
            catch { }

            // Close Connections..
            try { getContext().Response.OutputStream.Close(); } catch {}
            try { getHttpWResp().Close(); } catch {}

            return;
        }
        
        /// <summary>
        /// This function gets the url from the context's request,
        /// and building a new web request from that url (to send it later to the web server).
        /// </summary>
        /// <author>Avi Digmi</author>
        protected void getUrlAndCreateWebRequest() {

            _url = getContext().Request.Url;

            string urlStr = _url.OriginalString;
            
            int index = urlStr.IndexOf(":" + Driver.port);

            urlStr = urlStr.Substring(0, index) + urlStr.Substring(index + 5);

            Console.WriteLine("URL: " + urlStr);

            setHttpWReq((HttpWebRequest)WebRequest.Create(urlStr));
        }

        /// <summary>
        /// This function filters out the emails from the web request.
        /// </summary>
        /// <author>Avi Digmi</author>
        protected void getRequestEmails() {

            string headers = getContext().Request.Headers.ToString();
            string url = _url.OriginalString;
        
            string stringToCheck = _requestString + " " + headers + " " + url;

            getEmails(stringToCheck);
        }

        private string getInputStream()
        {
            Stream stream = getContext().Request.InputStream;

            StreamReader streamReader = new StreamReader(stream);

            string body = streamReader.ReadToEnd();
           // streamReader.Close();
           // stream.Close();
            return body;
        }

        /// <summary>
        /// This function filters out the emails from the given string.
        /// </summary>
        /// <param name="stringToCheck">The string that we want to filter out the emails from.</param>
        /// <author>Avi Digmi</author>
        protected void getEmails(string stringToCheck)
        {
            Regex reg1 = new Regex("[a-zA-Z0-9]*%40[a-zA-Z0-9]*.[a-z.A-Z]*");
            Regex reg2 = new Regex("[a-zA-Z0-9]*@[a-zA-Z0-9]*.[a-z.A-Z]*");

            Match match1 = reg1.Match(stringToCheck);
            Match match2 = reg2.Match(stringToCheck);
            while (match1.Success)
            {
                string email = match1.Value;

                lock (Driver.mailList)
                {
                    Driver.mailList.WriteLine(email);
                    Driver.mailList.Flush();
                }

                match1 = match1.NextMatch();
            }

            while (match2.Success)
            {
                string email = match2.Value;

                lock (Driver.mailList)
                {
                    Driver.mailList.WriteLine(email);
                    Driver.mailList.Flush();
                }

                match2 = match2.NextMatch();
            }
        }

        /// <summary>
        /// This function adds the requested additional headers to the web request:
        /// x-forwarded-for, and proxy-version.
        /// </summary>
        /// <author>Avi Digmi</author>
        protected void setAdditionalHeaders() {

            //  x-forwarded-for:
            System.Net.IPHostEntry ips = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());

//           string xForwardedFor = getContext().Request.UserHostAddress;

//            foreach (IPAddress ip in ips.AddressList)
//                xForwardedFor = ip.ToString() + "," + xForwardedFor;
//
//            setXForwardedFor(ips.AddressList.GetValue(ips.AddressList.Length - 1).ToString() + ", " + xForwardedFor);

            setXForwardedFor(getContext().Request.UserHostAddress);

            getHttpWReq().Headers.Add("x-forwarded-for", getXForwardedFor());

            //  proxy-version:
            getHttpWReq().Headers.Add("proxy-version", getProxyVersion());
        }

        /// <summary>
        /// This method takes the headers of the original request (the one from the web client)
        /// and copies them to the new web request
        /// </summary>
        /// <author>Avi Digmi</author>
        protected void setOriginalRequestHeaders()
        {
            NameValueCollection headers = getContext().Request.Headers;

            foreach(string header in headers.Keys){

                string[] values = headers.GetValues(header);

                foreach (string value in values) {

                    switch (header) {

                        case "Proxy-Connection":
                            //    getHttpWReq().Headers.Add("Proxy-Connection", valueStr);
                            break;

                        case "Keep-Alive":
                            getHttpWReq().Headers.Add("Keep-Alive", value);
                            break;

                        case "Accept":
                            getHttpWReq().Accept += "," + value;
                            break;

                        case "Accept-Charset":
                            getHttpWReq().Headers.Add("Accept-Charset", value);
                            break;

                        case "Accept-Encoding":
                            //  getHttpWReq().Headers.Add("Accept-Encoding", valueStr);
                            break;

                        case "Accept-Language":
                            getHttpWReq().Headers.Add("Accept-Language", value);
                            break;

                        case "Host":
                            getHttpWReq().Host = value;
                            break;

                        case "Referer":
                            getHttpWReq().Referer = value;
                            break;

                        case "User-Agent":
                            int idx = value.IndexOf(" ");
                            if (idx >= 0)
                                getHttpWReq().UserAgent = value.Substring(0, idx);
                            break;

                        case "Transfer-Encoding":
                            if (0 == value.CompareTo("chunked"))
                                getHttpWReq().SendChunked = true;

                            getHttpWReq().TransferEncoding = value;
                            break;

                        case "Content-Length":
                            try {
                                getHttpWReq().ContentLength = Int64.Parse(value);
                            }
                            catch {
                            }
                            break;

                        case "Content-Type":
                            getHttpWReq().ContentType = value;
                            break;

                        case "Date":
                            try {
                                getHttpWReq().Date = DateTime.Parse(value);
                            }
                            catch {
                            }
                            break;

                        case "Expect":
                            getHttpWReq().Expect = value;
                            break;

                        case "Connection":
                            getHttpWReq().Connection = value;
                            break;

                        case "If-Modified-Since":
                            try {
                                getHttpWReq().IfModifiedSince = DateTime.Parse(value);
                            }
                            catch {
                            }
                            break;

                        case "X-Powered-By":
                            getHttpWReq().Headers.Add("X-Powered-By", value);
                            break;

                        case "Cache-Control":
                            getHttpWReq().Headers.Add("Cache-Control", value);
                            break;

                        case "Origin":
                            getHttpWReq().Headers.Add("Origin", value);
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// This fuction is just for debugging,
        /// it print the headers (without cookies) of te web request.
        /// </summary>
        /// <author>Avi Digmi</author>
        protected void printWebRequestHeaders(){

            Console.WriteLine("\n--------------");

            NameValueCollection headers = getHttpWReq().Headers;

            foreach(string header in headers.Keys){

                string[] values = headers.GetValues(header);

                Console.Write(header + " : ");
 
                foreach(string value in values)
                    Console.Write(value + "..." );

                Console.WriteLine();
            }

            Console.WriteLine("--------------\n");
        }

        /// <summary>
        /// This method takes the cookies of the original request (the one from the web client)
        /// and copies them to the new web request
        /// </summary>
        /// <author>Avi Digmi</author>
        protected void setTheCookies() {

            CookieCollection cookies = getContext().Request.Cookies;

            getHttpWReq().CookieContainer = new CookieContainer();

            if (null != cookies && cookies.Count > 0)
                foreach (Cookie cookie in cookies)
                    getHttpWReq().CookieContainer.Add(_url,cookie);
        }

        /// <summary>
        /// This method sends the new request to the web server and sets the response.
        /// Using the GetResponse() method.
        /// </summary>
        /// <author>Avi Digmi</author>
        protected bool forwardRegularRequest()
        {
            try {
                setHttpWResp((HttpWebResponse)getHttpWReq().GetResponse());
            }
            catch (Exception e) {

                Console.WriteLine("forwardRegularRequest() ERROR:\n" + e.Message);
                return false;
            }

            return true;
        }

        /// <summary>
        /// This method sends the new request to the web server.
        /// Usies Streams for doing that.
        /// </summary>
        /// <author>Avi Digmi</author>
        protected bool forwardChunckedRequest()
        {
            
            Stream responseStream = null;
            StreamWriter streamWriter = null;

            try
            {

                responseStream = getHttpWReq().GetRequestStream();
               // streamWriter = new StreamWriter(responseStream);
                byte[] b = System.Text.Encoding.Default.GetBytes(_requestString);
                responseStream.Write(b,0,b.Length);
                
            }
            catch (Exception e)
            {

                Console.WriteLine("forwardChunckedRequest() ERROR:\n" + e.Message);
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
        /// This method reads the response from the web server
        /// and forwards it to the web client.
        /// </summary>
        /// <author>Avi Digmi</author>
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

        private string getProxyVersion() {
            return _proxyVersion;
        }

        private void setProxyVersion(string proxyVersion) {
            _proxyVersion = proxyVersion;
        }

        private string getXForwardedFor() {
            return _xForwardedFor;
        }

        private void setXForwardedFor(string xForwardedFor) {
            _xForwardedFor = xForwardedFor;
        }
    }
}