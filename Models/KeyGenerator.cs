using System.Security.Cryptography;

namespace ShoraWebsite.Models
{
    public static class KeyGenerator
    {
        /// <summary>
        /// Retorna uma chave de segurança de 32 bytes
        /// </summary>
        /// <param name="keySizeBytes"></param>
        /// <returns></returns>
        public static string GerarUmaChaveDeSegurança()
        {
            using (RNGCryptoServiceProvider rngCrypto = new RNGCryptoServiceProvider())
            {
                byte[] key = new byte[32];
                rngCrypto.GetBytes(key);

                return Convert.ToBase64String(key);
            }
        }


        public static string GerarIVDaMensagem()
        {
            using (RNGCryptoServiceProvider rngCrypto = new RNGCryptoServiceProvider())
            {
                byte[] iv = new byte[16];
                rngCrypto.GetBytes(iv);

                return Convert.ToBase64String(iv);
            }
        }
    }
}
