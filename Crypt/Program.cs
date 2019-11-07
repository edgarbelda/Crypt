using System;
using System.IO;
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
        private static readonly string PassPhrase =string.Concat(Pin,Pass);
        private const int SleepTime = 5000;
        #endregion
        static void Main(string[] args)
        {
            ProcessInput(args);
            EncryptDecrypt();
        }

        private static void EncryptDecrypt()
        {
            if (_encrypt)
            {
                if (!AskCode("PIN: ", Pin) || !AskCode("PASS: ", Pass)) return;

                using (var reader = new StreamReader(_path))
                {
                    var value = Encryption.Decrypt(reader.ReadToEnd(), PassPhrase);
                    var sw = new StreamWriter(Path.Combine(_directoryName,
                        string.Concat(_fileName.Replace("_", ""), _extension)));
                    sw.Write(value);
                    sw.Close();
                    reader.Close();
                    File.Delete(_path);
                }
            }
            else
            {
                using (var reader = new StreamReader(_path))
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
            _fileName = Path.GetFileNameWithoutExtension(_path);
            _directoryName = Path.GetDirectoryName(_path);
            _extension = Path.GetExtension(_path);
            _encrypt = _fileName.EndsWith("_");
        }
    }
}
