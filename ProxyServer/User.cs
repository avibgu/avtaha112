using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProxyServer
{
    class User
    {
        /// <summary>
        /// The user class holds information about a client. Used to check if the client send more than X requests in Y seconds.
        /// </summary>
        private string ip;
        private List<DateTime> requests;
        private int X;
        private int Y;

        public User(string ip,int x, int y)
        {
            this.X = x;
            this.Y = y;
            requests = new List<DateTime>();
            this.ip = ip;
        }

        public string getIp()
        {
            return ip;
        }


        public void addrequest()
        {
            DateTime currTime = DateTime.Now;
            requests.Add(currTime);
        }

        /// <summary>
        /// Check if the user exceeded the number of permitted request. (less than requests in Y seconds).
        /// </summary>
        /// <author>Shiran Gabay</author>
        /// <returns>return true if it exceeded, and false otherwise.</returns>
        public bool ExceedRequestsIntime()
        {
            DateTime lastrequest;
            int count = 0;
            if (requests.Count > 0)
            {
                lastrequest = requests[requests.Count - 1];
                for (int i = requests.Count - 1; i >= 0; --i)
                {
                    TimeSpan diff = lastrequest - requests[i];
                    if (diff.TotalSeconds <= Y)
                        ++count;
                    else
                        break;
                }
                if (count > this.X){
                   return true;
                }
     
                else
                    return false;
            }
            else return false;
        }

    }
}
