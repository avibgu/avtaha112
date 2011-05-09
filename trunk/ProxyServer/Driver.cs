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
using System.Text.RegularExpressions;


namespace ProxyServer {
    class Driver {
        /// <summary>
        ///  This class is the main class used to run the main function who waits for requests,to read the configuration files, and to 
        ///  handle the white-list, black-list and logger issues.
        /// </summary>
        public static string port;
        public static StreamWriter white;
        public static StreamWriter black;
        public static StreamWriter logger;
        public static StreamWriter mailList;
        List<string> Black_list;
        List<string> White_list;
        public static TripleDESCryptoServiceProvider tDESalg;
        string password; // The key for the triple des algorithm.
        public static int X;
        public static int Y;
        private string loginPassword;
        private string state;
        public static List<User> users;
        public string proxy_ip;

        /// <summary>
        /// Constructor.
        /// Used to initialize the fields of the class.
        /// </summary>
        public Driver() {
            Black_list = new List<string>();
            White_list = new List<string>();

            // Create the triple des object.
            tDESalg = new TripleDESCryptoServiceProvider();
            tDESalg.Key = Encoding.ASCII.GetBytes("passwordDR0wSS@P6660juht");
            tDESalg.IV = new Byte[] { 75, 220, 255, 151, 65, 212, 209, 162 };

            // Create the users list
            users = new List<User>();



            // Run the init function to read the configuration parameters.
            init();
        }

        /// <summary>
        /// The init function used to read the parametrs from the configuration file and create the 
        /// logger and the mail list file.
        /// <author> Shiran Gabay </author>
        /// </summary>
        public void init() {
            // read the password from configuration file
            string passFile = ConfigurationManager.AppSettings["password"];
            StreamReader file = new StreamReader(passFile);
            password = file.ReadLine();
            file.Close();

            // read X & Y from configuration file
            X = Convert.ToInt32(ConfigurationManager.AppSettings["X"]);
            Y = Convert.ToInt32(ConfigurationManager.AppSettings["Y"]);

            // read the login password from configuration file
            loginPassword = ConfigurationManager.AppSettings["loginPassword"];

            // Create the logger file.
            logger = new StreamWriter("..//..//Logger.txt", false);
            bool fileExists = File.Exists("..//..//Logger.txt");
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

            // Read the state of the proxy from the configuration file.
            state = ConfigurationManager.AppSettings["state"];
        }

        public TripleDESCryptoServiceProvider getCrypto() {
            return tDESalg;
        }

        public string getState() {
            return state;
        }

        public string getProxyIp() {
            // Set the external proxy IP
            try {
                WebClient client = new WebClient();
                return client.DownloadString("http://whatismyip.com/automation/n09230945.asp");
            }
            catch (Exception) {
                return "127.0.0.1";
            }
        }

        /// <summary>
        /// Add site which is blocked by the proxy.
        /// <author> Shiran Gabay </author>
        /// </summary>
        /// <param name="site"> The name of the new blocked site.</param>
        public void addBlackIp(string site) {
            Black_list.Add(site);
            black.WriteLine(System.Text.ASCIIEncoding.Unicode.GetString(EncryptTextToMemory(site)));
            black.Flush();

        }

        /// <summary>
        /// Add ip address to the white list. From now, users from this ip won't ask for authentication.
        /// <author> Shiran Gabay </author>
        /// </summary>
        /// <param name="site"> The ip to enter.</param>
        public void addWhiteIp(string ip) {

            // Add the ip to the white list.
            White_list.Add(ip);
            // Encrypt the ip and write it to the white list file.
            white.WriteLine(System.Text.ASCIIEncoding.Unicode.GetString(EncryptTextToMemory(ip)));
            white.Flush();
            // Add new User object to the users list.
            users.Add(new User(ip, X, Y));
        }

        /// <summary>
        /// Get the user object of the user identified by ip.
        /// </summary>
        /// <author> Shiran Gabay </author>
        /// <param name="ip"> the ip of the returned object.</param>
        /// <returns>the user object with the given ip.</returns>
        public User getUser(string ip) {
            for (int i = 0; i < users.Count; ++i) {
                if (ip.Equals(users[i].getIp())) {
                    return users[i];
                }
            }
            return null;
        }

        /// <summary>
        /// Remove ip from the white lisr according to exceeding the maximum request rate.
        /// </summary>
        /// <author> Shiran Gabay </author>
        /// <param name="ip"> The ip to remove. </param>
        public void removeWhiteIp(string ip) {
            // Find the user and remove it from the users list.
            for (int i = 0; i < users.Count; ++i) {
                if (users[i].getIp().Equals(ip)) {
                    users.RemoveAt(i);
                    break;
                }
            }
            // Remove the ip from the white list.
            White_list.Remove(ip);
        }

        public bool inBlackList(string site) {
            for (int i = 0; i < Black_list.Count; ++i) {
                if (Black_list[i].Contains(site))
                    return true;
            }

            return false;

        }

        public bool inWhiteList(string ip) {
            for (int i = 0; i < White_list.Count; ++i) {
                Console.WriteLine("INWHITELIST" + White_list[i] + "DGFD");
                if (White_list[i].Contains(ip))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Send authenticate page to the user.
        /// </summary>
        /// <author> Shiran Gabay </author>
        /// <param name="context">The context of the request.</param>
        public void login(HttpListenerContext context) {
            try {
                string response = System.IO.File.ReadAllText("..\\..\\LoginPage.htm");
                string ip = getProxyIp();
                response = response.Replace("127.0.0.1", getProxyIp());
                byte[] b = Encoding.UTF8.GetBytes(response);
                context.Response.ContentLength64 = b.Length;
                context.Response.OutputStream.Write(b, 0, b.Length);
                context.Response.OutputStream.Close();
            }
            catch {
            }
        }


        /// <summary>
        /// Encrypt the given text with triple des algorithm.
        /// </summary>
        /// <param name="Data">The string to encrypt.</param>
        /// <returns>The encrypted data</returns>
        public static byte[] EncryptTextToMemory(string Data) {
            try {
                // Create a MemoryStream.
                MemoryStream mStream = new MemoryStream();

                // Create a CryptoStream using the MemoryStream 
                // and the passed key and initialization vector (IV).
                CryptoStream cStream = new CryptoStream(mStream,
                    new TripleDESCryptoServiceProvider().CreateEncryptor(tDESalg.Key, tDESalg.IV),
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
            catch (CryptographicException e) {
                Console.WriteLine("A Cryptographic error occurred: {0}", e.Message);
                return null;
            }
        }

        /// <summary>
        /// Decrypt the given data with triple des algorithm.
        /// </summary>
        /// <param name="Data">The byte array to decrypt.</param>
        /// <returns>The decrypted data</returns>
        public static string DecryptTextFromMemory(byte[] Data) {
            try {
                // Create a new MemoryStream using the passed 
                // array of encrypted data.
                MemoryStream msDecrypt = new MemoryStream(Data);

                // Create a CryptoStream using the MemoryStream 
                // and the passed key and initialization vector (IV).
                CryptoStream csDecrypt = new CryptoStream(msDecrypt,
                    new TripleDESCryptoServiceProvider().CreateDecryptor(tDESalg.Key, tDESalg.IV),
                    CryptoStreamMode.Read);

                // Create buffer to hold the decrypted data.
                byte[] fromEncrypt = new byte[Data.Length];

                // Read the decrypted data out of the crypto stream
                // and place it into the temporary buffer.
                csDecrypt.Read(fromEncrypt, 0, fromEncrypt.Length);

                //Convert the buffer into a string and return it.
                return new ASCIIEncoding().GetString(fromEncrypt);
            }
            catch (CryptographicException e) {
                Console.WriteLine("A Cryptographic error occurred: {0}", e.Message);
                return null;
            }
        }


        /// <summary> 
        /// Parse the given file to the given list.
        /// Insert each line in the file to another location in the list.
        /// </summary>
        ///  <author> Shiran Gabay </author>
        /// <param name="fileString"> The string to parse.</param>
        /// <param name="lst">The list to insert into.</param>
        public void parseFile(string fileString, List<string> lst) {
            try {
                using (StringReader reader = new StringReader(fileString)) {
                    string line;
                    while ((line = reader.ReadLine()) != null) {
                        lst.Add(line);
                    }
                }
            }
            catch (FileNotFoundException ex) {
                Console.WriteLine(ex.Message);
            }


        }

        /// <summary>
        /// Encrypt the data in the given file and write the encrypted data to the output file.
        /// </summary>
        /// <author> Shiran Gabay </author>
        /// <param name="inputFile">The file to encrypt</param>
        /// <param name="outputFile">The file to write the encrypted text into.</param>
        public void EncryptFile(string inputFile, StreamWriter outputFile) {

            using (outputFile) {
                string line;
                StreamReader input = new StreamReader(inputFile);
                line = input.ReadLine();

                while (line != null) {
                    outputFile.WriteLine(System.Text.ASCIIEncoding.Unicode.GetString(EncryptTextToMemory(line)));
                    outputFile.Flush();
                    line = input.ReadLine();
                }
            }
        }

        /// <summary>
        /// Decrypt the text in the input file and insert each decrypted line to the given list.
        /// </summary>
        /// <author> Shiran Gabay </author>
        /// <param name="inputFile">The file to decrypt.</param>
        /// <param name="lst">The list to insert the decrypted lines into.</param>
        public void DecryptSiteToList(string inputFile, List<string> lst) {
            using (StreamReader input = new StreamReader(inputFile)) {
                string line;
                string dec;

                line = input.ReadLine();

                while (line != null) {
                    // Decrypt the line.
                    dec = DecryptTextFromMemory(System.Text.ASCIIEncoding.Unicode.GetBytes(line));
                    // Add the decrypted line to the list.
                    lst.Add(dec);
                    // Add the site to the users list.
                    users.Add(new User(dec, X, Y));
                    line = input.ReadLine();

                }
            }

        }

        /// <summary>
        /// Decrypt the text in the input file and insert each decrypted line to the given list.
        /// </summary>
        /// <author> Shiran Gabay </author>
        /// <param name="inputFile">The file to decrypt.</param>
        /// <param name="lst">The list to insert the decrypted lines into.</param>
        public void DecryptIpToList(string inputFile, List<string> lst) {
            using (StreamReader input = new StreamReader(inputFile)) {
                string line;
                string dec;

                line = input.ReadLine();

                while (line != null) {
                    // Decrypt the line.
                    dec = DecryptTextFromMemory(System.Text.ASCIIEncoding.Unicode.GetBytes(line));
                    // Remove unneccesary chars. 
                    System.Text.RegularExpressions.Regex nonNumericCharacters = new System.Text.RegularExpressions.Regex(@"\D");
                    string numericOnlyString = nonNumericCharacters.Replace(dec, String.Empty);
                    Regex ip = new Regex(@"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b");
                    MatchCollection result = ip.Matches(dec);

                    if (result.Count > 0) {
                        // Add the decrypted line to the list.
                        lst.Add(result[0].ToString());
                        // Add the ip to the users list.
                        users.Add(new User(result[0].ToString(), X, Y));
                    }
                    line = input.ReadLine();

                }
            }

        }

        /// <summary>
        /// Send response to the context's user with the given string.
        /// </summary>
        /// <author> Shiran Gabay </author>
        /// <param name="strToResponse">The response/</param>
        /// <param name="context">The request context.</param>
        public static void sendResponse(string strToResponse, HttpListenerContext context) {
            string response = "<HTML><BODY>" + strToResponse + "</BODY></HTML>";
            byte[] b = Encoding.ASCII.GetBytes(response);
            context.Response.ContentLength64 = b.Length;
            context.Response.OutputStream.Write(b, 0, b.Length);
            context.Response.OutputStream.Close();
        }


        /// <summary>
        /// This is the main function of our program.
        /// Initializes all the classes, manages the black and the white lists,
        /// listens for new connections and creates threads to handle them.
        /// </summary>
        /// <param name="args">parameters from the user, the first one is the proxy port</param>
        static void Main(string[] args) {
            // Create driver instance.
            Driver driver = new Driver();
            WebRequest.DefaultWebProxy = null;


            // Create the encrypted files.

            /* black = new StreamWriter("black-list.txt", false);
             driver.EncryptFile("b.txt", black);
             white = new StreamWriter("white-list.txt", false);
             driver.EncryptFile("a.txt", white);
             white.Close();
             black.Close();*/

            // Decrypt the files to the lists.
            driver.DecryptSiteToList(ConfigurationManager.AppSettings["black-list"], driver.Black_list);
            driver.DecryptIpToList(ConfigurationManager.AppSettings["white-list"], driver.White_list);

            // Create the stream writers of the white users and black sites.
            white = new StreamWriter(ConfigurationManager.AppSettings["white-list"], true);
            black = new StreamWriter(ConfigurationManager.AppSettings["black-list"], true);

            ProxyFactory proxyFactory = null;

            // Check the state of the proxy - open or anonymous.
            if (driver.getState().Equals("open"))
                proxyFactory = new OpenProxyFactory();

            else
                proxyFactory = new AnonProxyFactory();

            // Create the listener object.
            HttpListener listener = new HttpListener();


            if (args.Count() == 0) {
                Console.WriteLine("You should specify port number!!!");
                return;
            }
            // args[0]= The proxy port.
            Driver.port = args[0];
            listener.Prefixes.Add("http://*:" + args[0] + "/");

            // start the listener...
            listener.Start();

            Console.WriteLine("Proxy starts..");

            ThreadPool.SetMaxThreads(4, 0);

            while (true) {
                // Waiting to get context.
                HttpListenerContext context = listener.GetContext();
                Proxy proxy = proxyFactory.getProxy(context);

                string ipWithPort = context.Request.UserHostAddress;
                // Get the request ip without the port. to the logger.
                string ip = ipWithPort.Substring(0, ipWithPort.IndexOf(':'));
                // Get the request URL.
                string uri = context.Request.RawUrl;
                string findPassword = "";

                // Check if we got login request. If it is, check the password. If the password equals to the login password
                // the ip will be added to the white list.

                int num1 = uri.IndexOf("loginPassword=");
                if (num1 > 0) {
                    findPassword = uri;
                    findPassword = findPassword.Substring(num1 + 14);
                    if (findPassword.Equals(driver.loginPassword)) {
                        Console.WriteLine("AAAAAAAAAAAA" + ip + "FFFFF");
                        driver.addWhiteIp(ip);
                        sendResponse("Successfull login :)", context);
                    }
                    else
                        sendResponse("Unsuccessfull login :(", context);
                    continue;
                }


                // Write the request to the logger.
                logger.WriteLine(ip + " is asking for site " + uri);
                logger.Flush();

                // Check if the uri is in the black list
                if (driver.inBlackList(uri)) {
                    sendResponse("Unauthorized site", context);
                    continue;
                }

                // Check if the user is authenticated.
                if (!driver.inWhiteList(ip)) {
                    driver.login(context); // send the user authentication page.
                }
                else // The user in the white list.
                {
                    User tempUser = driver.getUser(ip);
                    if (tempUser != null) {
                        tempUser.addrequest();
                        if (tempUser.ExceedRequestsIntime()) // check if the user has send more than X requests in Y seconds.
                        {
                            driver.removeWhiteIp(ip); // remove the user from the white list. 
                            sendResponse("You exceeded the max packets. Connect again!", context);
                        }
                        else // run the thread who response to the user.
                            ThreadPool.QueueUserWorkItem(new WaitCallback(StartThreadCallBack),
                                new Thread(new ThreadStart(proxy.run)));
                    }
                    else // Exceed the maximum number of requests.
                        sendResponse("You exceeded the max packets. Connect again!", context);
                }
            }
        }

        static void StartThreadCallBack(Object stateInfo) {
            ((Thread)stateInfo).Start();
        }
    }
}