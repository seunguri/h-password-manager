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
        public int? ResetPeriodDays { get; set; } // null=미설정, 30/60/90=기간, -1=직접입력
        public string Notes { get; set; } = string.Empty;
        public string Tags { get; set; } = string.Empty;
        public int Order { get; set; }
        
        /// <summary>
        /// 실제 만료일 계산 (수정일 기준 + 설정 기간)
        /// </summary>
        public DateTime? CalculatedExpiryDate
        {
            get
            {
                if (!ResetPeriodDays.HasValue || ResetPeriodDays == 0)
                    return null;
                
                if (ResetPeriodDays == -1) // 직접입력
                    return ResetDate;
                
                // 기간 기반 계산 (수정일 + 설정 기간)
                if (LastPasswordChangeDate.HasValue)
                    return LastPasswordChangeDate.Value.AddDays(ResetPeriodDays.Value);
                
                return null;
            }
        }
        
        /// <summary>
        /// 만료까지 남은 일수
        /// </summary>
        public int? DaysUntilExpiry
        {
            get
            {
                var expiryDate = CalculatedExpiryDate;
                if (!expiryDate.HasValue)
                    return null;
                
                return (expiryDate.Value.Date - DateTime.Now.Date).Days;
            }
        }
        
        /// <summary>
        /// D-day 표시 문자열
        /// </summary>
        public string ExpiryDdayDisplay
        {
            get
            {
                var days = DaysUntilExpiry;
                if (!days.HasValue)
                    return "";
                
                if (days == 0)
                    return "D-Day";
                else if (days > 0)
                    return $"D-{days}";
                else
                    return $"D+{Math.Abs(days.Value)}";
            }
        }
        
        /// <summary>
        /// 만료 상태 표시 문자열
        /// </summary>
        public string ExpiryStatusDisplay
        {
            get
            {
                var days = DaysUntilExpiry;
                if (!days.HasValue)
                    return "";
                
                if (days < 0)
                    return "만료됨";
                else if (days == 0)
                    return "오늘 만료";
                else if (days <= 7)
                    return "곧 만료";
                else
                    return "정상";
            }
        }
        
        public SolidColorBrush StatusColor
        {
            get
            {
                var days = DaysUntilExpiry;
                if (!days.HasValue)
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4EC9B0")); // Green
                
                if (days < 0)
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F48771")); // Red - 만료됨
                else if (days <= 7)
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CE9178")); // Orange - 곧 만료
                else
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4EC9B0")); // Green - 정상
            }
        }
        
        public bool IsPasswordVisible { get; set; } = false;
        
        public string DisplayPassword
        {
            get
            {
                if (IsPasswordVisible)
                    return Password;
                return string.IsNullOrEmpty(Password) ? "" : "••••••••";
            }
        }
        
        public string ModifiedDateDisplay
        {
            get
            {
                return LastPasswordChangeDate.HasValue 
                    ? LastPasswordChangeDate.Value.ToString("yyyy.MM.dd.") 
                    : "";
            }
        }
        
        public string ResetDateDisplay
        {
            get
            {
                var expiryDate = CalculatedExpiryDate;
                return expiryDate.HasValue 
                    ? expiryDate.Value.ToString("yyyy.MM.dd.") 
                    : "";
            }
        }
        
        public string ResetPeriodDisplay
        {
            get
            {
                if (!ResetPeriodDays.HasValue || ResetPeriodDays == 0)
                    return "미설정";
                else if (ResetPeriodDays == -1)
                    return "직접입력";
                else
                    return $"{ResetPeriodDays}일";
            }
        }
    }
}
