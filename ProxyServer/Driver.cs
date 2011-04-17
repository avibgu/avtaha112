using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Net;

namespace ProxyServer
{
    class Driver
    {
        List<string> Black_list = new List<string>();
        List<string> White_list = new List<string>();

        public void addBlackIp(string ip)
        {
            Black_list.Add(ip);

        }

        public void addWhiteIp(string ip)
        {
            White_list.Add(ip);

        }

        public bool inBlackList(string ip)
        {
            return Black_list.Contains(ip);
        }

        public bool inWhiteList(string ip)
        {
            return White_list.Contains(ip);
        }

        static void Main(string[] args)
        {
            Driver driver = new Driver();
            Console.WriteLine("Choose server state:\n" +
                                "1. open.\n" +
                                "2. anonymous.");

            string inputLine = Console.ReadLine();

            Proxy proxy = null;

            if (inputLine.CompareTo("1") == 0)
                proxy = new OpenProxy();

            else
                proxy = new AnonProxy();

            HttpListener listener = new HttpListener();

            listener.Prefixes.Add("http://*:" + args[0] + "/");

            listener.Start();

            Console.WriteLine("Proxy starts..");

            while (true)
            {
                HttpListenerContext context = listener.GetContext();

                Console.WriteLine("listener got context..");

                proxy.setContext(context);

//                string client_ip = context.Request.UserHostAddress;
//
//               if (driver.inBlackList(client_ip))
//                {
//                   Console.WriteLine("Unautorized user {0}", client_ip);
//                    continue;
//                }
//
//                if (!driver.inWhiteList(client_ip))
//               {
//                    Console.Write("Enter Username: ");
//                    string inputLine1 = Console.ReadLine();
//
//                    Console.Write("Enter Password: ");
//                   string inputLine2 = Console.ReadLine();
//                }

                new Thread(new ThreadStart(proxy.run)).Start();

                Console.WriteLine("context thread created..");
            }
        }


    }
}


