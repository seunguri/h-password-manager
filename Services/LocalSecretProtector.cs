using System;
using System.Security.Cryptography;
using System.Text;

namespace PasswordProtector.Services
{
    /// <summary>
    /// Windows DPAPI(CurrentUser)로 문자열을 보호합니다. 다른 Windows 사용자나 다른 PC에서는 복호화할 수 없습니다.
    /// </summary>
    public static class LocalSecretProtector
    {
        private const string Prefix = "DPAPI1:";
        private static readonly byte[] OptionalEntropy = Encoding.UTF8.GetBytes("PasswordProtector/v1");

        public static string ProtectForStorage(string? plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return string.Empty;
            if (IsProtectedFormat(plainText))
                return plainText;

            var bytes = Encoding.UTF8.GetBytes(plainText);
            var protectedBytes = ProtectedData.Protect(bytes, OptionalEntropy, DataProtectionScope.CurrentUser);
            return Prefix + Convert.ToBase64String(protectedBytes);
        }

        public static string UnprotectFromStorage(string? stored)
        {
            if (string.IsNullOrEmpty(stored))
                return string.Empty;
            if (!IsProtectedFormat(stored))
                return stored;

            var b64 = stored.Substring(Prefix.Length);
            try
            {
                var protectedBytes = Convert.FromBase64String(b64);
                var plain = ProtectedData.Unprotect(protectedBytes, OptionalEntropy, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(plain);
            }
            catch
            {
                return string.Empty;
            }
        }

        public static bool IsProtectedFormat(string? s) =>
            !string.IsNullOrEmpty(s) && s.StartsWith(Prefix, StringComparison.Ordinal);
    }
}
