using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using Crypt.Models;
using QRCoder;
using EASendMail;
using Newtonsoft.Json;
using Color = System.Drawing.Color;
using Random = System.Random;
using SmtpClient = EASendMail.SmtpClient;


namespace Crypt
{
    public class Program
    {
        #region Definitions
        private static string _path;
        private static string _fileName;
        private static string _directoryName;
        private static string _extension;
        private static bool _encrypt;

        private static Config _config;
        private const string FileName = "config.crypt";
        
        #endregion

        #region Constructor
        public static void Main(string[] args)
        {
            PrintCredits();

            CheckConfig();



            if (args.Length != 0)
                ProcessInput(args);
            else
                AskInput();

            EncryptDecrypt();
        }

        private static void CheckConfig()
        {
            try
            {
                if (File.Exists(FileName))
                {
                    string config;
                    var code = Hash.GetHashString(Environment.UserName + Environment.MachineName);
                    using (var reader = new StreamReader(FileName))
                        config = Encryption.Decrypt(reader.ReadToEnd(), code);
                    _config = JsonConvert.DeserializeObject<Config>(config);
                }
                else
                {
                    Console.WriteLine("Creating config file");
                    Console.WriteLine("Insert a Pin and press enter: ");
                    var pin = Console.ReadLine();
                    Console.WriteLine("Insert a Pass and press enter: ");
                    var pass = Console.ReadLine();
                    Console.WriteLine("Insert an email to activate Two factors security (empty if not) and press enter: ");
                    var email = Console.ReadLine();
                    _config = new Config(pin, pass, email);

                    if (_config.TwoFactors)
                    {
                        if (!Configure2Fa())
                        {
                            Console.WriteLine("Press any key to exit...");
                            Console.Read();
                            Environment.Exit(0);
                        }
                    }
                       
                    var json = JsonConvert.SerializeObject(_config);
                    var sw = new StreamWriter(FileName);
                    var code = Hash.GetHashString(Environment.UserName + Environment.MachineName);
                    sw.Write(Encryption.Encrypt(json, code));
                    sw.Close();
                }

            }
            catch (Exception exception)
            {
                Console.WriteLine("Error reading config file");
                Console.WriteLine($"ERROR: {exception.Message}\n\n{exception.StackTrace}");
                Environment.Exit(1);
            }
        }

        #endregion

        #region Methods
        private static void ProcessInput(string[] args)
        {
            _path = string.Join(" ", args);
            ProcessPath();
        }
        private static void AskInput()
        {
            PrintCredits();
            Console.WriteLine("Path: ");
            var inputLine = Console.ReadLine() ?? throw new InvalidOperationException();
            _path = string.Join("", inputLine).Replace('"', ' ').Trim();
            ProcessPath();
        }

        private static bool Configure2Fa()
        {
            PrintCredits();
            var pin = !AskCode("PIN: ", _config.Pin);
            var pass = !AskCode("PASS: ", _config.Pass);
            var email = !AskCode("EMAIL: ", _config.Email);
            if (pin || pass || email)
            {
                WrongPass("PIN-PASS-EMAIL combination invalid, configure again.");
                return false;
            }


            var code = RandomString(8);
            SendEmail(code);


            Console.WriteLine("Enter email code (you have 60s): ");
            var watch = System.Diagnostics.Stopwatch.StartNew();


            var input = Console.ReadLine();

            if (!code.Equals(input))
            {
                WrongPass("email code invalid, configure again.");
                return false;
            }

            watch.Stop();
            var elapsedS = watch.ElapsedMilliseconds / 1000;
            if (elapsedS > 60)
            {
                Console.WriteLine("too much time to confirming code, configure again.");
                return false;
            }


            ShowTwoFactorQr(Secret32());
            Console.WriteLine("Enter code to confirm: ");
            CheckTwoFactors();
            return true;
        }


        private static void ProcessPath()
        {
            _fileName = Path.GetFileNameWithoutExtension(_path);
            _directoryName = Path.GetDirectoryName(_path);
            _extension = Path.GetExtension(_path);
            if (_fileName != null) _encrypt = _fileName.EndsWith("_");
        }

        private static void EncryptDecrypt()
        {
            if (_encrypt)
            {


                var pin = !AskCode("PIN: ", _config.Pin);
                var pass = !AskCode("PASS: ", _config.Pass);
                if (pin || pass)
                {
                    WrongPass("PIN-PASS combination invalid.");
                    return;
                }

                if (_config.TwoFactors)
                {
                    Console.WriteLine("TwoFactorsCode: ");
                    CheckTwoFactors();
                }

                using (var reader = new StreamReader(_path))
                {
                    try
                    {
                        var currentDirectory = Directory.GetCurrentDirectory();
                        var value = Encryption.Decrypt(reader.ReadToEnd(), _config.PassPhrase());
                        var combine = Path.Combine(currentDirectory, string.Concat(_fileName.Replace("_", ""), _extension));
                        var fs = new FileStream(combine, FileMode.CreateNew);
                        var sw = new StreamWriter(fs, Encoding.GetEncoding("iso-8859-1"));
                        sw.Write(value);
                        sw.Close();
                        reader.Close();
                        if (_directoryName.Equals(currentDirectory))
                            File.Delete(_path);
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("Error decrypting");
                    }
                }
            }
            else
            {
                using (var reader = new StreamReader(_path, Encoding.GetEncoding("iso-8859-1")))
                {
                    var sr = reader.ReadToEnd();
                    var sw = new StreamWriter(Path.Combine(_directoryName, string.Concat(_fileName, "_", _extension)));
                    sw.Write(Encryption.Encrypt(sr, _config.PassPhrase()));
                    sw.Close();
                    reader.Close();
                    File.Delete(_path);
                }
            }

        }

        private static void CheckTwoFactors()
        {
            var input = Console.ReadLine();
            var otpSharp = new OtpSharp.Totp(Secret32().ToByteArray());
            if (otpSharp.ComputeTotp().Equals(input))
            {
                Console.WriteLine("Two factors code right.");
                return;
            }

            WrongPass("Two factors code wrong.");
            Environment.Exit(1);

        }

        private static void WrongPass(string message)
        {
            Console.WriteLine(message);
            Console.ReadLine();
        }

        private static bool AskCode(string message, string code)
        {
            System.Threading.Thread.Sleep(Config.SleepTime);
            Console.WriteLine(message);
            var inputLine = Console.ReadLine() ?? throw new InvalidOperationException();
            return inputLine.Equals(code);
        }

        private static void PrintCredits()
        {
            Console.Clear();
            Console.WriteLine("Crypt - Fast line command program to encrypt\\decrypt files using algorithm (AES 256 - bit)");
            Console.WriteLine("https://github.com/edgarbelda/Crypt");
            Console.WriteLine();

        }

        public static void ConsoleWriteImage(Bitmap bmpSrc)
        {
            for (var i = 0; i < bmpSrc.Height; i++)
            {
                for (var j = 0; j < bmpSrc.Width; j++)
                {
                    var c = bmpSrc.GetPixel(j, i);
                    const int t = 150;
                    if (c.R > t | c.G > t | c.B > t)
                        Console.ForegroundColor = ConsoleColor.White;
                    else
                        Console.ForegroundColor = ConsoleColor.Black;


                    Console.Write("██");
                }
                Console.WriteLine();
            }
        }

        public static void ShowTwoFactorQr(string secret32)
        {
            Console.Clear();
            Console.WriteLine("Scan code:");
            var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(string.Concat("otpauth://totp/Crypt?secret=", string.Concat(secret32, "&algorithm=SHA1&digits=6&period=30")), QRCodeGenerator.ECCLevel.Q);
            var qrCode = new QRCode(qrCodeData);
            var qrCodeImage = qrCode.GetGraphic(1, Color.Black, Color.White, null);
            ConsoleWriteImage(qrCodeImage);
            Console.WriteLine("Code (for manually entrance): " + secret32);
        }

        private static string Secret32()
        {
            var secret = Hash.GetHashString(_config.PassPhrase()).Substring(0, 10).ToLower();
            var secret32 = secret.EncodeAsBase32String(false);
            return secret32;
        }

        private static void SendEmail(string code)
        {
            try
            {
                var oMail = new SmtpMail("TryIt");

                oMail.From = "crypt@crypt.com";
                oMail.To = _config.Email;
                oMail.Subject = "Code for Two factors";
                oMail.TextBody = "The two factors code is: " + code;
                var oServer = new SmtpServer("");

                Console.WriteLine("start to send email directly ...");

                var oSmtp = new SmtpClient();
                oSmtp.SendMail(oServer, oMail);

                Console.WriteLine("email was sent successfully! (Check SPAM folder!)");
            }
            catch (Exception ep)
            {
                Console.WriteLine("failed to send email with the following error:");
                Console.WriteLine(ep.Message);
            }


        }

        private static string RandomString(int length)
        {
            var random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        #endregion
    }
}

