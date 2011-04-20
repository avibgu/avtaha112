using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Net;
using System.IO;

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


        public void login(HttpListenerContext context)
        {
            string response = System.IO.File.ReadAllText("..\\..\\LoginPage.htm");
            byte[] b = Encoding.UTF8.GetBytes(response);
            context.Response.ContentLength64 = b.Length;
            context.Response.OutputStream.Write(b, 0, b.Length);
            //context.Response.Redirect("C:\\Users\\shiran\\Documents\\Visual Studio 2010\\Projects\\ProxyServer(2)\\ProxyServer\\LoginPage.htm");
            //context.Response.OutputStream.Close();
        }

        public void parseFile(string fileName,List<string> lst)
        {
            try
            {
                using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
                {
                    StreamReader file = new StreamReader(fs);
                    string line;
                    while ((line = file.ReadLine()) != null)
                    {
                          lst.Add(line);
                    }
                }
            }
            catch (FileNotFoundException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        static void Main(string[] args)
        {
            Driver driver = new Driver();
            driver.parseFile("white-list.txt",driver.White_list);
            driver.parseFile("black-list.txt", driver.Black_list);

            Console.WriteLine("Choose server state:\n" +
                                "1. open.\n" +
                                "2. anonymous.");

            string inputLine = Console.ReadLine();

            ProxyFactory proxyFactory = null;

            if (inputLine.CompareTo("1") == 0)
                proxyFactory = new OpenProxyFactory();

            else
                proxyFactory = new AnonProxyFactory();

            HttpListener listener = new HttpListener();

            listener.Prefixes.Add("http://*:" + args[0] + "/");

            listener.Start();

            Console.WriteLine("Proxy starts..");

            while (true)
            {
                HttpListenerContext context = listener.GetContext();

                Proxy proxy = proxyFactory.getProxy(context);
        
                string client_ip = context.Request.UserHostAddress;
 /*             if (driver.inBlackList(client_ip))
                {
                   string response = "<HTML><BODY>Unauthorized user</BODY></HTML>";
                   byte[] b = Encoding.ASCII.GetBytes(response);
                   context.Response.ContentLength64 = b.Length;
                   context.Response.OutputStream.Write(b, 0, b.Length);
                   context.Response.OutputStream.Close();
                    continue;
                } */

//                if (!driver.inWhiteList(client_ip))
//               {
                //          driver.login(context);
//                }

                new Thread(new ThreadStart(proxy.run)).Start();
            }
        }
    }
}


