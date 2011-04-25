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
using System.Configuration;

namespace ProxyServer
{
    class Driver
    {
        public static StreamWriter logger;
        List<string> Black_list = new List<string>();
        List<string> White_list = new List<string>();
        TripleDESCryptoServiceProvider tDESalg;
        string password;
        List<string> mailList;
        int X;
        int Y;
        
        public Driver()
        {
            
            // read from configuration file
            string passFile = ConfigurationManager.AppSettings["password"];
            StreamReader file = new StreamReader(passFile);
            password = file.ReadLine();
            X = Convert.ToInt32(ConfigurationManager.AppSettings["X"]);
            Y = Convert.ToInt32(ConfigurationManager.AppSettings["Y"]);
            mailList = new List<string>();
            logger = new StreamWriter("..//..//Logger.txt", false);
            bool fileExists = File.Exists("../..//Logger.txt");
            if (!fileExists)
                File.Create("..//..//Logger.txt");
            logger.WriteLine("Starting...");
            logger.Flush();
            tDESalg = new TripleDESCryptoServiceProvider();
            tDESalg.Key = Encoding.ASCII.GetBytes("passwordDR0wSS@P6660juht");
            tDESalg.IV = new Byte[] { 75, 220, 255, 151, 65, 212, 209, 162 };
        }

        public TripleDESCryptoServiceProvider getCrypto()
        {
            return tDESalg;
        }

        public void addBlackIp(string site)
        {
            Black_list.Add(site);
            
        }

        public void addWhiteIp(string ip)
        {
            White_list.Add(ip);

        }

        public bool inBlackList(string site)
        {
            for (int i=0; i<Black_list.Count; ++i)
            {
                if (Black_list[i].Equals(site))
                    return true;
            }
             
            return false;
  
        }

        public bool inWhiteList(string ip)
        {
           for (int i=0; i<White_list.Count; ++i)
            {
                if (White_list[i].Equals(ip))
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
         //   context.Response.Redirect("..\\..\\LoginPage.htm");
            Console.ReadLine();
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

        public void parseFile(string fileString,List<string> lst)
        {
            try
            {
                using (StringReader reader = new StringReader(fileString))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
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

        /// <summary>
        /// Encrypts a file and saves the output to a new file.
        /// </summary>
        /// <param name="inputFile">The file to encrypt.</param>
        /// <param name="outputFile">File to save encrypted version.</param>
        private void EncryptFile(string inputFile, string outputFile)
        {
            
            // Get filestream for input file.
            using (var inputStream =
               new FileStream(inputFile, FileMode.Open, FileAccess.Read))
            {
                // Get filestream for output file.
                using (var outputStream =
                   new FileStream(outputFile, FileMode.Create, FileAccess.Write))
                {
                    // Create an encryption stream.
                    using (var cryptoStream =
                       new CryptoStream(outputStream,
                          getCrypto().CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        // Copy the input file stream to the encryption stream.
                        inputStream.CopyTo(cryptoStream);
                    }
                }
            }
        }

        /// <summary>
        /// Decrypts the specified file and returns the contents.
        /// </summary>
        /// <param name="inputFile">The file to decrypt.</param>
        /// <returns>Contents of the file.</returns>
        private string DecryptFile(string inputFile)
        {

            // Get FileInfo. Check if the file is empty or not/
            FileInfo fi = new FileInfo(ConfigurationManager.AppSettings["white-list"]);
            if (fi.Length == 0)
                return "";

            // Get filestream for input file.
            using (var inputStream =
               new FileStream(inputFile, FileMode.Open, FileAccess.Read))
            {
                // Create an encryption stream.
                using (var cryptoStream =
                   new CryptoStream(inputStream,
                      getCrypto().CreateDecryptor(), CryptoStreamMode.Read))
                {
                    using (var reader = new StreamReader(cryptoStream))
                    {
                        return reader.ReadToEnd(); 
                              
                    }
                }
            }
        }
        static void Main(string[] args)
        {
            Driver driver = new Driver();
            // driver.EncryptFile("C:\\Users\\shiran\\Documents\\Visual Studio 2010\\Projects\\ProxyServer(5)\\ProxyServer\\bin\\Debug\\b.txt", "C:\\Users\\shiran\\Documents\\Visual Studio 2010\\Projects\\ProxyServer(5)\\ProxyServer\\bin\\Debug\\black-list.txt");

            // Decrypt the lists and parse them to the application lists.
            driver.parseFile(driver.DecryptFile(ConfigurationManager.AppSettings["white-list"]), driver.White_list);
            driver.parseFile(driver.DecryptFile(ConfigurationManager.AppSettings["black-list"]), driver.Black_list);
            Console.WriteLine(driver.White_list[0]);
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
                Uri url = context.Request.Url;
                string ipWithPort = context.Request.UserHostAddress;
                string ip = ipWithPort.Substring(0, ipWithPort.IndexOf(':'));
                string uri = context.Request.RawUrl;

                logger.WriteLine(ip + " is asking for site " + uri);
                logger.Flush();
   

                if (driver.inBlackList(uri))
                {
                   string response = "<HTML><BODY>Unauthorized user</BODY></HTML>";
                   byte[] b = Encoding.ASCII.GetBytes(response);
                   context.Response.ContentLength64 = b.Length;
                   context.Response.OutputStream.Write(b, 0, b.Length);
                   context.Response.OutputStream.Close();
                    continue;
                }

                if (!driver.inWhiteList(ip))
               {
                   driver.login(context);
               }

                new Thread(new ThreadStart(proxy.run)).Start();
            }
        }
    }
}


