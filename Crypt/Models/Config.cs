namespace Crypt.Models
{
    internal class Config
    {

        public string Pin { get; set; } 
        public string Pass { get; set; } 
        public const int SleepTime = 1000;
        public bool TwoFactors { get; set; }
        public string Email { get; set; }

        public Config(string pin, string pass, string email ="")
        {
            Pin = pin;
            Pass = pass;
            Email = email;
            TwoFactors = email != "";
        }

        public string PassPhrase()
        {
            return Hash.GetHashString(string.Concat(Pin, Pass));
        }

    }
}
