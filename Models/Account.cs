using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;

namespace PasswordProtector.Models
{
    public class Account : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        /// <summary>INI에 저장되는 고유 식별자. 서비스명·아이디가 같아도 항목을 구분합니다.</summary>
        public Guid Id { get; set; }

        public string ServiceName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        /// <summary>비밀번호가 설정되어 있는지 여부.</summary>
        public bool HasPassword => !string.IsNullOrEmpty(Password);

        private bool _isPasswordVisible;
        /// <summary>대시보드 카드에서 비밀번호를 임시로 표시할지 여부(눈 버튼 토글).</summary>
        public bool IsPasswordVisible
        {
            get => _isPasswordVisible;
            set
            {
                if (_isPasswordVisible == value)
                    return;
                _isPasswordVisible = value;
                OnPropertyChanged(nameof(IsPasswordVisible));
                OnPropertyChanged(nameof(PasswordDisplay));
            }
        }

        /// <summary>카드에 표시할 비밀번호 문자열(기본 마스킹, 눈 버튼으로 임시 표시).</summary>
        public string PasswordDisplay
        {
            get
            {
                if (!HasPassword)
                    return "";
                return IsPasswordVisible ? Password : "••••••••••••";
            }
        }
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
                    return GetThemeBrush("StatusGreen");
                
                if (days < 0)
                    return GetThemeBrush("StatusRed");
                else if (days <= 7)
                    return GetThemeBrush("StatusOrange");
                else
                    return GetThemeBrush("StatusGreen");
            }
        }

        private static SolidColorBrush GetThemeBrush(string resourceKey) =>
            Application.Current?.TryFindResource(resourceKey) as SolidColorBrush ?? Brushes.Gray;
        
        public bool HasNotesContent => !string.IsNullOrWhiteSpace(Notes);

        /// <summary>목록 카드에 표시할 비고(내용이 있을 때만 영역 노출).</summary>
        public string NotesCardBody => string.IsNullOrWhiteSpace(Notes) ? string.Empty : Notes.Trim();

        /// <summary>태그 칩이 하나라도 있으면 true.</summary>
        public bool HasTags
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Tags))
                    return false;
                foreach (var part in Tags.Split(new[] { ',', '|' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (!string.IsNullOrWhiteSpace(part.Trim()))
                        return true;
                }
                return false;
            }
        }

        /// <summary>카드 한 줄 요약(수정일·만료 D-day). 둘 다 없으면 빈 문자열 → 레이아웃에서 숨김.</summary>
        public string CardScheduleSummary
        {
            get
            {
                var mod = LastPasswordChangeDate.HasValue ? $"수정 {ModifiedDateDisplay}" : "";
                var exp = !string.IsNullOrEmpty(ExpiryDdayDisplay) ? ExpiryDdayDisplay : "";
                if (string.IsNullOrEmpty(mod) && string.IsNullOrEmpty(exp))
                    return string.Empty;
                if (string.IsNullOrEmpty(mod))
                    return exp;
                if (string.IsNullOrEmpty(exp))
                    return mod;
                return $"{mod} · {exp}";
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
