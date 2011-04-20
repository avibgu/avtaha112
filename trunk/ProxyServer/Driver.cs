using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Net;
using System.IO;
using System.Security.Cryptography;
using System.Diagnostics;

namespace ProxyServer
{
    class Driver
    {
        public static StreamWriter logger;
        List<byte[]> Black_list = new List<byte[]>();
        List<byte[]> White_list = new List<byte[]>();
        string password = "passwordDR0wSS@P6660juht";
        TripleDESCryptoServiceProvider tDESalg;
        
        public Driver()
        {
            logger = new StreamWriter("..//..//Logger.txt", true);
            bool fileExists = File.Exists("../..//Logger.txt");
            if (!fileExists)
                File.Create("..//..//Logger.txt");
            logger.WriteLine("Starting...");
            logger.Flush();
            tDESalg = new TripleDESCryptoServiceProvider();
            tDESalg.Key = StrToByteArray(password);
        }

        public static byte[] StrToByteArray(string str)
        {
            System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
            return encoding.GetBytes(str);
        }

        public static string ByteArraytoString(byte[] arr)
        {
                string str;
                System.Text.UTF8Encoding enc = new System.Text.UTF8Encoding();
                str = enc.GetString(arr);
                return str;
        }

        public void addBlackIp(byte[] site)
        {
            Black_list.Add(site);

        }

        public void addWhiteIp(byte[] ip)
        {
            White_list.Add(ip);

        }

        public bool inBlackList(byte[] site)
        {
            for (int i=0; i<Black_list.Count; ++i)
            {
                if (ByteArraysEqual(Black_list[i],site))
                    return true;
            }
             
            return false;
  
        }

        public bool inWhiteList(byte[] ip)
        {
           for (int i=0; i<White_list.Count; ++i)
            {
                if (ByteArraysEqual(White_list[i],ip))
                    return true;
            }
             
            return false;
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

        public static byte[] EncryptTextToMemory(string Data, byte[] Key, byte[] IV)
        {
            try
            {
                // Create a MemoryStream.
                MemoryStream mStream = new MemoryStream();

                // Create a CryptoStream using the MemoryStream 
                // and the passed key and initialization vector (IV).
                CryptoStream cStream = new CryptoStream(mStream,
                    new TripleDESCryptoServiceProvider().CreateEncryptor(Key, IV),
                    CryptoStreamMode.Write);

                // Convert the passed string to a byte array.
                byte[] toEncrypt = new ASCIIEncoding().GetBytes(Data);

                // Write the byte array to the crypto stream and flush it.
                cStream.Write(toEncrypt, 0, toEncrypt.Length);
                cStream.FlushFinalBlock();

                // Get an array of bytes from the 
                // MemoryStream that holds the 
                // encrypted data.
                byte[] ret = mStream.ToArray();

                // Close the streams.
                cStream.Close();
                mStream.Close();

                // Return the encrypted buffer.
                return ret;
            }
            catch (CryptographicException e)
            {
                Console.WriteLine("A Cryptographic error occurred: {0}", e.Message);
                return null;
            }

        }

        public static string DecryptTextFromMemory(byte[] Data, byte[] Key, byte[] IV)
        {
            try
            {
                // Create a new MemoryStream using the passed 
                // array of encrypted data.
                MemoryStream msDecrypt = new MemoryStream(Data);

                // Create a CryptoStream using the MemoryStream 
                // and the passed key and initialization vector (IV).
                CryptoStream csDecrypt = new CryptoStream(msDecrypt,
                    new TripleDESCryptoServiceProvider().CreateDecryptor(Key, IV),
                    CryptoStreamMode.Read);

                // Create buffer to hold the decrypted data.
                byte[] fromEncrypt = new byte[Data.Length];

                // Read the decrypted data out of the crypto stream
                // and place it into the temporary buffer.
                csDecrypt.Read(fromEncrypt, 0, fromEncrypt.Length);

                //Convert the buffer into a string and return it.
                return new ASCIIEncoding().GetString(fromEncrypt);
            }
            catch (CryptographicException e)
            {
                Console.WriteLine("A Cryptographic error occurred: {0}", e.Message);
                return null;
            }
        }


        public static bool ByteArraysEqual(byte[] b1, byte[] b2)
        {
            if (b1 == b2) return true;
            if (b1 == null || b2 == null) return false;
            if (b1.Length != b2.Length) return false;
            for (int i = 0; i < b1.Length; i++)
            {
                if (b1[i] != b2[i]) return false;
            }
            return true;
        }

        public void parseFile(string fileName,List<byte[]> lst)
        {
            try
            {
                using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
                {
                    StreamReader file = new StreamReader(fs);
                    string line;
                    while ((line = file.ReadLine()) != null)
                    {
                          // Encrypt using 3-Des
                          // Encrypt the string to an in-memory buffer.
                          byte[] Data = EncryptTextToMemory(line, tDESalg.Key, tDESalg.IV);
                          lst.Add(Data);
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
                string ip = context.Request.UserHostAddress;
                byte[] client_ip = EncryptTextToMemory(ip ,driver.tDESalg.Key,driver.tDESalg.IV);
                string uri = context.Request.RawUrl;
                byte[] client_uri = EncryptTextToMemory(uri, driver.tDESalg.Key, driver.tDESalg.IV);
                logger.WriteLine(ip + " is asking for site " + uri);
                logger.Flush();

 /*               if (driver.inBlackList(uri))
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


