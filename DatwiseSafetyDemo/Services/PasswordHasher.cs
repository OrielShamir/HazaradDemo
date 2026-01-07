using System;
using System.Security.Cryptography;

namespace DatwiseSafetyDemo.Services
{
    public interface IPasswordHasher
    {
        PasswordHash Hash(string password, int iterations = 100_000);
        bool Verify(string password, byte[] salt, byte[] expectedHash, int iterations);
    }

    public sealed class PasswordHash
    {
        public byte[] Salt { get; set; }
        public byte[] Hash { get; set; }
        public int Iterations { get; set; }
        public string Algorithm { get; set; }
    }

    /// <summary>
    /// PBKDF2-HMACSHA1 (Rfc2898DeriveBytes) for .NET Framework compatibility.
    /// </summary>
    public sealed class Pbkdf2PasswordHasher : IPasswordHasher
    {
        private const int SaltSize = 16;
        private const int KeySize = 32;

        public PasswordHash Hash(string password, int iterations = 100_000)
        {
            if (password == null) throw new ArgumentNullException(nameof(password));
            if (iterations < 10_000) iterations = 10_000;

            var salt = new byte[SaltSize];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            byte[] key;
            using (var derive = new Rfc2898DeriveBytes(password, salt, iterations))
            {
                key = derive.GetBytes(KeySize);
            }

            return new PasswordHash
            {
                Salt = salt,
                Hash = key,
                Iterations = iterations,
                Algorithm = "PBKDF2-SHA1"
            };
        }

        public bool Verify(string password, byte[] salt, byte[] expectedHash, int iterations)
        {
            if (password == null) return false;
            if (salt == null || expectedHash == null) return false;
            if (iterations <= 0) return false;

            byte[] key;
            using (var derive = new Rfc2898DeriveBytes(password, salt, iterations))
            {
                key = derive.GetBytes(expectedHash.Length);
            }

            return FixedTimeEquals(key, expectedHash);
        }

        private static bool FixedTimeEquals(byte[] a, byte[] b)
        {
            if (a == null || b == null) return false;
            if (a.Length != b.Length) return false;

            var diff = 0;
            for (int i = 0; i < a.Length; i++)
            {
                diff |= a[i] ^ b[i];
            }
            return diff == 0;
        }
    }
}
