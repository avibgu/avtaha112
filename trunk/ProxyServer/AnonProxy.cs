using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;

namespace ProxyServer {
    class AnonProxy : OpenProxy {

        /// <summary>
        /// This is the constructor of the Anonymous Proxy.
        /// Calls the constructor of the Open Proxy.
        /// </summary>
        /// <param name="context"> Gets the context of the connection as argument</param>
        /// <author>Avi Digmi</author>
        public AnonProxy(HttpListenerContext context)
            : base(context) {
        }

        /// <summary>
        /// This is the main method of the Anonymous Proxy.
        /// Responsable on taking the original request from the web browser
        /// and sending it, after some modifications, to the web server.
        /// It also handles the sent of the response from the web server to the web client.
        /// </summary>
        /// <author>Avi Digmi</author>
        public override void run() {

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

            //  Sets a default User-Agent
            getHttpWReq().UserAgent = "Mozilla/5.0";

            //  Print the headers
            printWebRequestHeaders();

            // Forward the request
            bool ans;

            if (getHttpWReq().SendChunked == true || getContext().Request.HttpMethod == "POST")
                ans = forwardChunckedRequest();
            else
                ans = forwardRegularRequest();

            if (!ans)
                return;

            /*
             * take the response from the remote server
             * and forward it as is to the client who initiated the connection.
             */
            try {
                // Get Response and Forward it
                if (getHttpWReq().SendChunked == true || getContext().Request.HttpMethod == "POST")
                    setHttpWResp((HttpWebResponse)getHttpWReq().GetResponse());

                getResponseAndForwardIt();
            }
            catch {
            }

            // Close Connections..
            try {
                getContext().Response.OutputStream.Close();
            }
            catch {
            }
            try {
                getHttpWResp().Close();
            }
            catch {
            }

            return;
        }
    }
}
