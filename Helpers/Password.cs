namespace WebApi.Helpers
{
    using System.Text;
    using System.Security.Cryptography;

    public class Password
    {
        string salt;
        public Password(AppSettings appSettings)
        {
            salt = appSettings.salt;
        }

        public string GenerateSaltedHashString(string password)
        {
            return System.Text.Encoding.UTF8.GetString(
                this.GenerateSaltedHash(Encoding.ASCII.GetBytes(password),
                Encoding.ASCII.GetBytes(salt)));
        }

        public byte[] GenerateSaltedHash(byte[] plainText, byte[] salt)
        {
            HashAlgorithm algorithm = new SHA256Managed();

            byte[] plainTextWithSaltBytes =
              new byte[plainText.Length + salt.Length];

            for (int i = 0; i < plainText.Length; i++)
            {
                plainTextWithSaltBytes[i] = plainText[i];
            }
            for (int i = 0; i < salt.Length; i++)
            {
                plainTextWithSaltBytes[plainText.Length + i] = salt[i];
            }

            return algorithm.ComputeHash(plainTextWithSaltBytes);
        }
    }
}