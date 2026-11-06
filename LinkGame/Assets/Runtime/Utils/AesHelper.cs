using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Kyrnel
{
    public static class AesHelper
    {
        public static string EncryptString(string plainText, string key, string iv)
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] ivBytes = Encoding.UTF8.GetBytes(iv);
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);

            byte[] cipherBytes = EncryptBytes(plainBytes, keyBytes, ivBytes);
            return Convert.ToBase64String(cipherBytes);
        }

        public static string DecryptString(string cipherText, string key, string iv)
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] ivBytes = Encoding.UTF8.GetBytes(iv);
            byte[] plainBytes = Convert.FromBase64String(cipherText);

            byte[] cipherBytes = DecryptBytes(plainBytes, keyBytes, ivBytes);
            return Encoding.UTF8.GetString(cipherBytes);
        }

        public static byte[] EncryptBytes(byte[] plainBytes, byte[] keyBytes, byte[] ivBytes)
        {
            ValidateKeyAndIv(keyBytes, ivBytes);

            using (Aes aes = Aes.Create())
            {
                aes.Key = keyBytes;
                aes.IV = ivBytes;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (ICryptoTransform encryptor = aes.CreateEncryptor())
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                        {
                            cs.Write(plainBytes, 0, plainBytes.Length);
                            cs.FlushFinalBlock();
                        }

                        return ms.ToArray();
                    }
                }
            }
        }

        public static byte[] DecryptBytes(byte[] cipherBytes, byte[] keyBytes, byte[] ivBytes)
        {
            ValidateKeyAndIv(keyBytes, ivBytes);

            using (Aes aes = Aes.Create())
            {
                aes.Key = keyBytes;
                aes.IV = ivBytes;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (ICryptoTransform decryptor = aes.CreateDecryptor())
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Write))
                        {
                            cs.Write(cipherBytes, 0, cipherBytes.Length);
                            cs.FlushFinalBlock();
                        }

                        return ms.ToArray();
                    }
                }
            }
        }

        private static void ValidateKeyAndIv(byte[] keyBytes, byte[] ivBytes)
        {
            if (keyBytes == null || (keyBytes.Length != 16 && keyBytes.Length != 24 && keyBytes.Length != 32))
                throw new ArgumentException("Key 长度必须为 16、24 或 32 字节 (128/192/256 位)！");

            if (ivBytes == null || ivBytes.Length != 16)
                throw new ArgumentException("IV 长度必须固定为 16 字节 (128 位)！");
        }
    }
}