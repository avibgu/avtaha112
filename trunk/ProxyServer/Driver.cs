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
        static void Main(string[] args)
        {
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

            listener.Prefixes.Add("http://*:17170/");

            listener.Start();

            Console.WriteLine("Proxy starts..");

            while (true)
            {
                HttpListenerContext context = listener.GetContext();

                Console.WriteLine("listener got context..");

                proxy.setContext(context);

                new Thread(new ThreadStart(proxy.run)).Start();

                Console.WriteLine("context thread created..");
            }
        }
    }
}


