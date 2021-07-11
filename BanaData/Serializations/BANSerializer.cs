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

        public void Write(string file, string password)
        {
            using (var aes = AesManaged.Create())
            {
                (byte[] key, byte[] iv) = GetKeyFromPassword(password);
                var encryptor = aes.CreateEncryptor(key, iv);

                using (var fileStream = new FileStream(file, FileMode.Create, FileAccess.Write))
                {
                    using (var encryptStream = new CryptoStream(fileStream, encryptor, CryptoStreamMode.Write))
                    {
                        household.WriteXml(encryptStream);
                        encryptStream.FlushFinalBlock();
                    }
                }
            }
        }

        public void Read(string file, string password)
        {
            household.Clear();
            household.AcceptChanges();

            using (var aes = AesManaged.Create())
            {
                (byte[] key, byte[] iv) = GetKeyFromPassword(password);
                var decryptor = aes.CreateDecryptor(key, iv);

                using (var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read))
                {
                    using (var decryptStream = new CryptoStream(fileStream, decryptor, CryptoStreamMode.Read))
                    {
                        household.ReadXml(decryptStream);
                    }
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
