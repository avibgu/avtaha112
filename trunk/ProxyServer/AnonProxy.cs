using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;

namespace ProxyServer
{
    class AnonProxy : OpenProxy
    {

        public AnonProxy(HttpListenerContext context): base(context)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        public override void run()
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
            setOriginalRequestHeaders();

            //  Sets a default User-Agent
            getHttpWReq().UserAgent = "Mozilla/5.0";

            //  Print the headers
            printHeaders();

            // Forward the request

            bool ans;

            if (getHttpWReq().SendChunked == false)
                ans = forwardRegularRequest();
            else
                ans = forwardChunckedRequest();

            if (!ans)
                return;

            /*
             * take the response from the remote server
             * and forward it as is to the client who initiated the connection.
             */
            try {
                // Get Response and Forward it
                if (getHttpWReq().SendChunked == true)
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
