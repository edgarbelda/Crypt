using System;
using System.IO;
using System.Text;
using Crypt.Models;

namespace Crypt
{
    class Program
    {

        #region Definitions

        private static string _path;
        private static string _fileName;
        private static string _directoryName;
        private static string _extension;
        private static bool _encrypt;

        private const string Pin = "1234";
        private const string Pass = "password";
        private static readonly string PassPhrase = Hash.GetHashString(string.Concat(Pin,Pass));
        private const int SleepTime = 5000;
        #endregion
        static void Main(string[] args)
        {
            PrintCredits();

            if(args.Length!=0)
                ProcessInput(args);
            else
                AskInput();
            
            EncryptDecrypt();
        }

        private static void EncryptDecrypt()
        {
            if (_encrypt)
            {
                if (!AskCode("PIN: ", Pin) || !AskCode("PASS: ", Pass)) return;

                using (var reader = new StreamReader(_path))
                {
                    try
                    {
                        var value = Encryption.Decrypt(reader.ReadToEnd(), PassPhrase);
                        var combine = Path.Combine(_directoryName, string.Concat(_fileName.Replace("_", ""), _extension));
                        var fs = new FileStream(combine, FileMode.CreateNew);
                        var sw = new StreamWriter(fs, Encoding.GetEncoding("iso-8859-1"));
                        sw.Write(value);
                        sw.Close();
                        reader.Close();
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
                    var sw = new StreamWriter(Path.Combine(_directoryName, string.Concat(_fileName,"_",_extension)));
                    sw.Write(Encryption.Encrypt(sr, PassPhrase));
                    sw.Close();
                    reader.Close();
                    File.Delete(_path);
                }
            }

        }

        private static bool AskCode(string message, string code)
        {
            System.Threading.Thread.Sleep(SleepTime);
            Console.WriteLine(message);
            var inputLine = Console.ReadLine() ?? throw new InvalidOperationException();
            return inputLine.Equals(code);
        }


        private static void ProcessInput(string[] args)
        {
            _path = string.Join("",args);
            ProcessPath();
        }
        private static void AskInput()
        {
            Console.WriteLine("Path: ");
            var inputLine = Console.ReadLine() ?? throw new InvalidOperationException();
            _path = string.Join("", inputLine).Replace('"',' ').Trim();
            ProcessPath();
        }

        private static void ProcessPath()
        {
            _fileName = Path.GetFileNameWithoutExtension(_path);
            _directoryName = Path.GetDirectoryName(_path);
            _extension = Path.GetExtension(_path);
            if (_fileName != null) _encrypt = _fileName.EndsWith("_");
        }
        private static void PrintCredits()
        {
            Console.WriteLine("Crypt - Fast line command program to encrypt\\decrypt files using algorithm(AES 256 - bit)");
            Console.WriteLine("https://github.com/edgarbelda/Crypt");
            Console.WriteLine();

        }

    }
}
