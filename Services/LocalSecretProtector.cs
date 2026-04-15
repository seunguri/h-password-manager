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
            if (plainText is null)
                return string.Empty;

            if (plainText.Length == 0)
                return string.Empty;

            if (IsProtectedFormat(plainText))
                return plainText;

            byte[]? plainBytes = null;
            byte[]? protectedBytes = null;

            try
            {
                plainBytes = Encoding.UTF8.GetBytes(plainText);
                protectedBytes = ProtectedData.Protect(
                    plainBytes,
                    OptionalEntropy,
                    DataProtectionScope.CurrentUser);

                return Prefix + Convert.ToBase64String(protectedBytes);
            }
            finally
            {
                if (plainBytes is not null)
                    Array.Clear(plainBytes, 0, plainBytes.Length);

                if (protectedBytes is not null)
                    Array.Clear(protectedBytes, 0, protectedBytes.Length);
            }
        }

        public static string UnprotectFromStorage(string? stored)
        {
            plainText = string.Empty;

            if (string.IsNullOrEmpty(stored))
                return false;

            if (!IsProtectedFormat(stored))
                return false;

            byte[]? protectedBytes = null;
            byte[]? plainBytes = null;

            try
            {
                var b64 = stored.Substring(Prefix.Length);
                protectedBytes = Convert.FromBase64String(b64);

                plainBytes = ProtectedData.Unprotect(
                    protectedBytes,
                    OptionalEntropy,
                    DataProtectionScope.CurrentUser);

                plainText = Encoding.UTF8.GetString(plainBytes);
                return true;
            }
            catch
            {
                return string.Empty;
            }
            finally
            {
                if (protectedBytes is not null)
                    Array.Clear(protectedBytes, 0, protectedBytes.Length);

                if (plainBytes is not null)
                    Array.Clear(plainBytes, 0, plainBytes.Length);
            }
        }

        public static bool IsProtectedFormat(string? s) =>
            !string.IsNullOrEmpty(s) && s.StartsWith(Prefix, StringComparison.Ordinal);
    }
}
