using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Net;
using System.IO;
using System.Security.Cryptography;

namespace ProxyServer
{
    class Driver
    {
        List<string> Black_list = new List<string>();
        List<string> White_list = new List<string>();
        string password = "passwordDR0wSS@P6660juht";
        TripleDESCryptoServiceProvider tDESalg;

        public Driver()
        {
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
            // Encrypt the string to an in-memory buffer.
            byte[] Data = EncryptTextToMemory(ip, tDESalg.Key, tDESalg.IV);
            return Black_list.Contains(ByteArraytoString(Data));
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
                          // Encrypt using 3-Des
                          // Encrypt the string to an in-memory buffer.
                          byte[] Data = EncryptTextToMemory(line, tDESalg.Key, tDESalg.IV);
                          lst.Add(ByteArraytoString(Data));
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
                if (driver.inBlackList(client_ip))
                {
                   string response = "<HTML><BODY>Unauthorized user</BODY></HTML>";
                   byte[] b = Encoding.ASCII.GetBytes(response);
                   context.Response.ContentLength64 = b.Length;
                   context.Response.OutputStream.Write(b, 0, b.Length);
                   context.Response.OutputStream.Close();
                    continue;
                } 

//                if (!driver.inWhiteList(client_ip))
//               {
                //          driver.login(context);
//                }

                //new Thread(new ThreadStart(proxy.run)).Start();
            }
        }
    }
}


