using System;
using System.Windows.Media;

namespace PasswordProtector.Models
{
    public class Account
    {
        public string ServiceName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public DateTime? LastPasswordChangeDate { get; set; }
        public DateTime? ResetDate { get; set; }
        public string Notes { get; set; } = string.Empty;
        public string Tags { get; set; } = string.Empty;
        public int Order { get; set; }
        
        public SolidColorBrush StatusColor
        {
            get
            {
                if (!ResetDate.HasValue)
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4EC9B0"));
                
                var daysUntilReset = (ResetDate.Value - DateTime.Now).Days;
                
                if (daysUntilReset < 0)
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F48771")); // Red
                else if (daysUntilReset <= 7)
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CE9178")); // Orange
                else
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4EC9B0")); // Green
            }
        }
        
        public bool IsPasswordVisible { get; set; } = false;
    }
}
