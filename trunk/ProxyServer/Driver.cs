﻿using System;
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
using System.Text.RegularExpressions;

namespace ProxyServer
{
    class Driver
    {
        public static StreamWriter white;
        public static StreamWriter black;
        public static StreamWriter logger;
        public static StreamWriter mailList;
        List<string> Black_list = new List<string>();
        List<string> White_list = new List<string>();
        public static TripleDESCryptoServiceProvider tDESalg;
        string password;
        public static int X;
        public static int Y;
        string loginPassword;
        public static List<User> users;
     
        
        public Driver()
        {
            
            // read from configuration file
            string passFile = ConfigurationManager.AppSettings["password"];
            StreamReader file = new StreamReader(passFile);
            password = file.ReadLine();
            X = Convert.ToInt32(ConfigurationManager.AppSettings["X"]);
            Y = Convert.ToInt32(ConfigurationManager.AppSettings["Y"]);
            loginPassword = ConfigurationManager.AppSettings["loginPassword"];
            logger = new StreamWriter("..//..//Logger.txt", false);
            bool fileExists = File.Exists("../..//Logger.txt");
            if (!fileExists)
                File.Create("..//..//Logger.txt");
            logger.WriteLine("Starting...");
            logger.Flush();
            // Create the mail file
            mailList = new StreamWriter("..//..//MailsList.txt", false);
            fileExists = File.Exists("../..//MailsList.txt");
            if (!fileExists)
                File.Create("..//..//MailsList.txt");
            mailList.Flush();
            // Create the triple des object.
            tDESalg = new TripleDESCryptoServiceProvider();
            tDESalg.Key = Encoding.ASCII.GetBytes("passwordDR0wSS@P6660juht");
            tDESalg.IV = new Byte[] { 75, 220, 255, 151, 65, 212, 209, 162 };

            // Create the users list
            users = new List<User>();
        }

        public TripleDESCryptoServiceProvider getCrypto()
        {
            return tDESalg;
        }

        public void addBlackIp(string site)
        {
            Black_list.Add(site);
            black.WriteLine(System.Text.ASCIIEncoding.Unicode.GetString(EncryptTextToMemory(site)));
            black.Flush();
            
        }

        public void addWhiteIp(string ip)
        {
            
            White_list.Add(ip);
            white.WriteLine(System.Text.ASCIIEncoding.Unicode.GetString(EncryptTextToMemory(ip)));
            white.Flush();
            // Add new User object to the users list.
            users.Add(new User(ip, X, Y));
        }

        public User getUser(string ip)
        {
           for (int i = 0; i < users.Count ; ++i)
            {
               if(ip.Contains(users[i].getIp()))
                //if (users[i].getIp().Contains(ip))
                {
                    return users[i];
                }
            }
                return null;
        }

        public void removeWhiteIp(string ip)
        {
            // Find the user and remove it from the users list.
            for (int i = 0; i < users.Count; ++i)
            {
                if (users[i].getIp().Contains(ip))
                {
                    users.RemoveAt(i);
                    break;
                }
            }
            // Remove the ip from the white list.
            White_list.Remove(ip);
        }

        public bool inBlackList(string site)
        {
            for (int i=0; i<Black_list.Count; ++i)
            {
                 if (Black_list[i].Contains(site) )
                    return true;
            }
             
            return false;
  
        }

        public bool inWhiteList(string ip)
        {
           for (int i=0; i < White_list.Count; ++i)
            {
                if (White_list[i].Contains(ip))
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
               context.Response.OutputStream.Close();
       
          
        }
        public static byte[] EncryptTextToMemory(string Data)
        {
            try
            {
                // Create a MemoryStream.
                MemoryStream mStream = new MemoryStream();

                // Create a CryptoStream using the MemoryStream 
                // and the passed key and initialization vector (IV).
                CryptoStream cStream = new CryptoStream(mStream,
                    new TripleDESCryptoServiceProvider().CreateEncryptor(tDESalg.Key,tDESalg.IV),
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

        public static string DecryptTextFromMemory(byte[] Data)
        {
            try
            {
                // Create a new MemoryStream using the passed 
                // array of encrypted data.
                MemoryStream msDecrypt = new MemoryStream(Data);

                // Create a CryptoStream using the MemoryStream 
                // and the passed key and initialization vector (IV).
                CryptoStream csDecrypt = new CryptoStream(msDecrypt,
                    new TripleDESCryptoServiceProvider().CreateDecryptor(tDESalg.Key,tDESalg.IV),
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

        /// <summary> 
        /// Parse the given file to the given list.
        /// Insert each line in the file to another location in the list.
        /// </summary>
        /// <param name="fileString"> The string to parse.</param>
        /// <param name="lst">The list to insert into.</param>
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
                    outputStream.Close();
                }
                inputStream.Close();
            }
        }

        /// <summary>
        /// Decrypts the specified file and returns the contents.
        /// </summary>
        /// <param name="inputFile">The file to decrypt.</param>
        /// <returns>Contents of the file.</returns>
        private string DecryptFile(string inputFile)
        {
            string ans = "";
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
                        
                        ans= reader.ReadToEnd(); 
                              
                    }

                }
                inputStream.Close();
                return ans;
            }

        }

        public void EncryptFile2(string inputFile, StreamWriter outputFile)
        {

            using (outputFile)
            {
                string line;
                StreamReader input = new StreamReader(inputFile);
                line = input.ReadLine();

                while (line != null)
                {
                     //  outputFile.WriteLine("hello");
                    outputFile.WriteLine(System.Text.ASCIIEncoding.Unicode.GetString(EncryptTextToMemory(line)));
                    outputFile.Flush();
                    line = input.ReadLine();


                }
               
            }
            

        }

        public void DecryptFileToList(string inputFile, List<string> lst)
        {
            using (StreamReader input = new StreamReader(inputFile))
            {
                string line;
                string dec;

                line = input.ReadLine();
                
                while (line != null)
                {
                   
                    dec = DecryptTextFromMemory(System.Text.ASCIIEncoding.Unicode.GetBytes(line));
                    System.Text.RegularExpressions.Regex nonNumericCharacters = new System.Text.RegularExpressions.Regex(@"\D");
                    string numericOnlyString = nonNumericCharacters.Replace(dec, String.Empty);

                    Regex ip = new Regex(@"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b");
                    MatchCollection result = ip.Matches(dec);
                    if (result.Count > 0)
                    {
                        lst.Add(result[0].ToString());
                        users.Add(new User(result[0].ToString(), X, Y));
                    }
                    line = input.ReadLine();
                
                }
            }
            
        }

        public static void sendResponse(string strToResponse,HttpListenerContext context){
                   string response = "<HTML><BODY>"+strToResponse+"</BODY></HTML>";
                   byte[] b = Encoding.ASCII.GetBytes(response);
                   context.Response.ContentLength64 = b.Length;
                   context.Response.OutputStream.Write(b, 0, b.Length);
                   context.Response.OutputStream.Close();
        }

        public bool checkNumOfPackets(HttpListenerContext context)
        {
            return true;
        }

        static void Main(string[] args)
        {
            Driver driver = new Driver();

            // Create the encrypted files.

            // black = new StreamWriter("black-list.txt", false);
           //  driver.EncryptFile2("b.txt", black);
            // white = new StreamWriter("white-list.txt", false);
            //driver.EncryptFile2("a.txt", white);
             //  white.Close();
          //     black.Close();
              

            driver.DecryptFileToList("black-list.txt", driver.Black_list);
            driver.DecryptFileToList("white-list.txt", driver.White_list);
         
            white = new StreamWriter("white-list.txt", true);
        
            black = new StreamWriter("black-list.txt", true);

              
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
                // Get the request ip without the port. to the logger.
                string ip = ipWithPort.Substring(0, ipWithPort.IndexOf(':'));
                // Get the request URL.
                string uri = context.Request.RawUrl;
                string findPassword="";
               // Check if we got login request
               int num1 = uri.IndexOf("loginPassword=");
                if (num1 > 0)
                {
                    findPassword = uri;
                    findPassword = findPassword.Substring(num1+14);
                    if (findPassword.Equals(driver.loginPassword))
                    {
                        driver.addWhiteIp(ip);
                        sendResponse("Successfull login :)",context);
                    }
                    else
                        sendResponse("Unsuccessfull login :(",context);
                    continue;
                }
                
                             
                // Write thw request
                logger.WriteLine(ip + " is asking for site " + uri);
                logger.Flush();
               
                if (driver.inBlackList(uri))
                {
                    sendResponse("Unauthorized user", context);
                    continue;
                }

                if (!driver.inWhiteList(ip))
                {
                    driver.login(context);
                }
                else
                {
                    User tempUser = driver.getUser(ip);
                    if (tempUser != null)
                    {
                        tempUser.addrequest();
                        if (tempUser.ExceedRequestsIntime())
                        {
                            driver.removeWhiteIp(ip);
                            sendResponse("You exceeded the max packets. Connect again!", context);
                        }
                        else
                            new Thread(new ThreadStart(proxy.run)).Start();
                    }
                    else
                        sendResponse("You exceeded the max packets. Connect again!", context);
                }
            }
        }
    }
}


