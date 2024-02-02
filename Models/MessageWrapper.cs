using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ShoraWebsite.Models
{
    public static class MessageWrapper
    {

        public static string EncryptString(string plainText, string keyString, string ivString)
        {

            //tornar a chave em bytes
            byte[] key = Convert.FromBase64String(keyString);
            //tornas a iv em bytes
            byte[] iv = Convert.FromBase64String(ivString);


            byte[] encrypted;

            // Cria um aes com uma key e uma iv
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;

                // Cria uma memory stream para armanezenar os bytes encriptados
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    //CryptoStream serve para encriptar
                    using (CryptoStream cryptoStream = new CryptoStream(memoryStream, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        // Aqui encriptamos a mensagem
                        using (StreamWriter streamWriter = new StreamWriter(cryptoStream))
                        {
                            streamWriter.Write(plainText);
                        }
                        //guardar os bytes encriptados
                        encrypted = memoryStream.ToArray();
                    }
                }
            }
            //return dos bytes em string para armazenar
            return Convert.ToBase64String(encrypted);
        }


       public static string DecryptString(string cipherTextString, string keyString, string ivString)
        {

            //tornar a chave em bytes
            byte[] key = Convert.FromBase64String(keyString);
            //tornas a iv em bytes
            byte[] iv = Convert.FromBase64String(ivString);

            string decrypted;

            byte[] cipherText = Convert.FromBase64String(cipherTextString);

            // Cria um aes com uma key e uma iv
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;

                // Cria uma memory stream para armanezenar os bytes desencriptados
                using (MemoryStream memoryStream = new MemoryStream(cipherText))
                {
                    //CryptoStream serve para desencriptar
                    using (CryptoStream cryptoStream = new CryptoStream(memoryStream, aes.CreateDecryptor(), CryptoStreamMode.Read))
                    {
                        // Desencripta
                        using (StreamReader streamReader = new StreamReader(cryptoStream))
                        {
                            decrypted = streamReader.ReadToEnd();
                        }
                    }
                }
            }
            //retorna a string desencriptada
            return decrypted;
        }

    }

    
}
