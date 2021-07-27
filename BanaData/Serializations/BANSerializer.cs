using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using BanaData.Database;

namespace BanaData.Serializations
{
    /// <summary>
    /// Encrypt/decrypt .BAN files
    /// </summary>
    class BANSerializer
    {
        private readonly Household household;

        public BANSerializer(Household _household) => household = _household;

        public void Write(FileStream fileStream, string password)
        {
            using (var aes = AesManaged.Create())
            {
                (byte[] key, byte[] iv) = GetKeyFromPassword(password);
                var encryptor = aes.CreateEncryptor(key, iv);

                fileStream.SetLength(0);
                using (var encryptStream = new CryptoStream(fileStream, encryptor, CryptoStreamMode.Write, true))
                {
                    household.WriteXml(encryptStream);
                    encryptStream.FlushFinalBlock();
                }
                fileStream.Flush();
            }
        }

        public void Read(FileStream fileStream, string password)
        {
            household.Clear();
            household.AcceptChanges();

            using (var aes = AesManaged.Create())
            {
                (byte[] key, byte[] iv) = GetKeyFromPassword(password);
                var decryptor = aes.CreateDecryptor(key, iv);

                fileStream.Position = 0;
                using (var decryptStream = new CryptoStream(fileStream, decryptor, CryptoStreamMode.Read, true))
                {
                    household.ReadXml(decryptStream);
                }
            }

            household.AcceptChanges();
        }

        private (byte[] key, byte[] iv) GetKeyFromPassword(string password)
        {
            byte[] salt = { (byte)'B', (byte)'a', (byte)'m', (byte)'b', (byte)'o', (byte)'u', (byte)'l', (byte)'a' };
            if (string.IsNullOrEmpty(password))
            {
                password = "Bananas";
            }

            Rfc2898DeriveBytes k = new Rfc2898DeriveBytes(password, salt, 1000);

            return (k.GetBytes(32), k.GetBytes(48).Skip(32).ToArray());
        }
    }
}
