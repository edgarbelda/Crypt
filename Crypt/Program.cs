using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using Crypt.Models;
using QRCoder;
using EASendMail;
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

        private const string Pin = "1234";
        private const string Pass = "password";
        private static readonly string PassPhrase = Hash.GetHashString(string.Concat(Pin, Pass));
        private const int SleepTime = 1000;
        private const bool TwoFactors = true;
        private const string Email = "put your email";
        #endregion

        #region Constructor
        public static void Main(string[] args)
        {
            PrintCredits();

            if (args.Length != 0)
                ProcessInput(args);
            else
                AskInput();

            EncryptDecrypt();
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
            Console.WriteLine("Path or (2fa): ");
            var inputLine = Console.ReadLine() ?? throw new InvalidOperationException();
            if (inputLine.Equals("2fa"))
                Configure2Fa();
            _path = string.Join("", inputLine).Replace('"', ' ').Trim();
            ProcessPath();
        }

        private static void Configure2Fa()
        {
            var pin = !AskCode("PIN: ", Pin);
            var pass = !AskCode("PASS: ", Pass);
            if (pin || pass)
            {
                WrongPass("PIN-PASS combination invalid.");
                Environment.Exit(0);
            }


            var code = RandomString(8);
            SendEmail(code);

            Console.WriteLine("Enter email code:");
            var input = Console.ReadLine();

            if (!code.Equals(input))
            {
                WrongPass("email code invalid.");
                Environment.Exit(0);
            }

            ShowTwoFactorQr(Secret32());
            Console.WriteLine("Enter code to confirm: ");
            CheckTwoFactors();
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


                var pin = !AskCode("PIN: ", Pin);
                var pass = !AskCode("PASS: ", Pass);
                if (pin || pass)
                {
                    WrongPass("PIN-PASS combination invalid.");
                    return;
                }

                if (TwoFactors)
                {
                    Console.WriteLine("TwoFactorsCode: ");
                    CheckTwoFactors();
                }

                using (var reader = new StreamReader(_path))
                {
                    try
                    {
                        var currentDirectory = Directory.GetCurrentDirectory();
                        var value = Encryption.Decrypt(reader.ReadToEnd(), PassPhrase);
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
                    sw.Write(Encryption.Encrypt(sr, PassPhrase));
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
                Console.WriteLine("Two factors code right.");
            else
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
            System.Threading.Thread.Sleep(SleepTime);
            Console.WriteLine(message);
            var inputLine = Console.ReadLine() ?? throw new InvalidOperationException();
            return inputLine.Equals(code);
        }

        private static void PrintCredits()
        {
            Console.WriteLine("Crypt - Fast line command program to encrypt\\decrypt files using algorithm (AES 256 - bit)");
            Console.WriteLine("https://github.com/edgarbelda/Crypt");
            Console.WriteLine();
            Console.WriteLine("Enter path or press 2fa to configure the 2 authorization factors");

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
        }

        private static string Secret32()
        {
            var secret = Hash.GetHashString(PassPhrase).Substring(0, 10).ToLower();
            var secret32 = secret.EncodeAsBase32String(false);
            return secret32;
        }

        private static void SendEmail(string code)
        {
            try
            {
                var oMail = new SmtpMail("TryIt");

                oMail.From = "crypt@crypt.com";
                oMail.To = Email;
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
