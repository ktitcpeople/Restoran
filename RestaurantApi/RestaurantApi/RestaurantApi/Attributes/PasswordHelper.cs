using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using RestaurantApi.Models;

namespace RestaurantApi.Helpers
{
    public static class PasswordHelper
    {
        private const int Iterations = 100_000;
        private const int SaltSize = 16;
        private const int KeySize = 32;
        private const string LegacyPrefix = "PBKDF2";
        private static readonly PasswordHasher<User> IdentityHasher = new();

        public static string Hash(string password)
        {
            return IdentityHasher.HashPassword(new User(), password);
        }

        public static bool Verify(string password, string stored)
        {
            if (string.IsNullOrEmpty(stored)) return false;

            // Совместимость со старыми записями, где пароль хранился открытым текстом.
            if (!stored.StartsWith(LegacyPrefix + "$", StringComparison.Ordinal))
            {
                if (stored.StartsWith("AQAAAA", StringComparison.Ordinal))
                {
                    try { return IdentityHasher.VerifyHashedPassword(new User(), stored, password) != PasswordVerificationResult.Failed; }
                    catch { return false; }
                }
                return string.Equals(password, stored, StringComparison.Ordinal);
            }

            try
            {
                var parts = stored.Split('$');
                if (parts.Length != 4 || !int.TryParse(parts[1], out var iterations)) return false;

                var salt = Convert.FromBase64String(parts[2]);
                var expected = Convert.FromBase64String(parts[3]);
                var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
                return CryptographicOperations.FixedTimeEquals(actual, expected);
            }
            catch
            {
                return false;
            }
        }

        public static bool NeedsUpgrade(string stored) => !stored.StartsWith("AQAAAA", StringComparison.Ordinal);
    }
}
