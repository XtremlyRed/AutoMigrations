using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace AutoMigrations.Extensions
{
    internal static class BytesExtensions
    {
        public static string Encrypt(this byte[] bytes)
        {
            using var md5 = MD5.Create();

            var buffer = md5.ComputeHash(bytes);

            string hash = BitConverter.ToString(buffer).Replace("-", "").ToLower();

            return hash;
        }

        public static bool Verify(this byte[] bytes, string encryptResult)
        {
            var encrytString = Encrypt(bytes);

            return encrytString == encryptResult;
        }
    }
}
