namespace PasswordProtector.Models
{
    public static class AccountFieldLimits
    {
        public const int ServiceNameMaxLength = 120;
        public const int NotesMaxLength = 4000;

        public static string Clamp(string? value, int maxLength)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }
    }
}
