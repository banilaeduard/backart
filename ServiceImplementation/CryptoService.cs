using ServiceInterface.Storage;

namespace ServiceImplementation
{
    public class CryptoService : ICryptoService
    {
        System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();

        public string GetMd5(string input)
        {
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
            byte[] hashBytes = md5.ComputeHash(inputBytes);

            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }
    }
}
