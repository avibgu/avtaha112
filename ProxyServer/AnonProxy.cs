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

        public AnonProxy(HttpListenerContext context)
            : base(context)
        {
            //setContext(context);
        }

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
            getEmails();

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
            try { getContext().Response.OutputStream.Close(); }
            catch { }
            try { getHttpWResp().Close(); }
            catch { }

            return;
        }

        /// <summary>
        /// 
        /// </summary>
        protected void setTheHeaders()
        {

            //  User-Agent:
            getHttpWReq().UserAgent = "GeoBot/1.0 ";

            //  Accept:
            string[] acceptTypes = getContext().Request.AcceptTypes;

            string acceptTypesStr = "";

            foreach (string type in acceptTypes)
                acceptTypesStr += "," + type;

            getHttpWReq().Accept = acceptTypesStr.Substring(1);
            

            //  content length
            getHttpWReq().ContentLength = getContext().Request.ContentLength64;
        }

    }        
}
