using System.Security.Cryptography;
using System.Text;

namespace Xia {
    public static class Auxiliary {
        public static string HashPassword(string password) {
            var salt = RandomNumberGenerator.GetBytes(16);
            var hash = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, 350_000, HashAlgorithmName.SHA512, 16);
            var hexSalt = Convert.ToHexString(salt);
            var hexHash = Convert.ToHexString(hash);
            return $"{hexHash}{hexSalt}";
        }
        public static bool VerifyHashPassword(string hashPass, string password) {
            var hashBytes = Convert.FromHexString(hashPass);
            var hash = hashBytes[..16];
            var salt = hashBytes[16..];
            var newHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 350_000,
                HashAlgorithmName.SHA512, 16);
            return newHash.SequenceEqual(hash);
        }
    }
}
