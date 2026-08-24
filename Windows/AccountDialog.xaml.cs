using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Documents;
using System.Windows.Media;
using PasswordProtector.Models;
using PasswordProtector.Services;

namespace PasswordProtector.Windows
{
    public partial class AccountDialog : Window
    {
        public Account Account { get; private set; }

        /// <summary>상세창 하단 삭제 버튼으로 삭제가 요청되었는지 여부.</summary>
        public bool DeleteRequested { get; private set; }

        private readonly TagService _tagService;
        private readonly IniFileService _iniFileService;
        private ObservableCollection<string> _tags;
        private DateTime? _selectedResetDate;
        private int? _selectedPeriodDays;

        public AccountDialog(Account? account = null)
        {
            InitializeComponent();

            var labelFg = new SolidColorBrush(Color.FromRgb(0xDC, 0xDC, 0xDC));
            var hintFg = new SolidColorBrush(Color.FromRgb(0x80, 0x80, 0x80));
            ServiceNameCaption.Inlines.Add(new Run("서비스명 ") { Foreground = labelFg });
            ServiceNameCaption.Inlines.Add(new Run($"(최대 {AccountFieldLimits.ServiceNameMaxLength}자)") { Foreground = hintFg });
            NotesCaption.Inlines.Add(new Run("메모 ") { Foreground = labelFg });
            NotesCaption.Inlines.Add(new Run($"(최대 {AccountFieldLimits.NotesMaxLength}자)") { Foreground = hintFg });

            _tagService = new TagService();
            _iniFileService = new IniFileService();
            _tags = new ObservableCollection<string>();
            TagChipsControl.ItemsSource = _tags;
            
            if (account != null)
            {
                this.Title = "계정 수정";
                if (DeleteButton != null)
                    DeleteButton.Visibility = Visibility.Visible;
                Account = new Account
                {
                    Id = account.Id,
                    ServiceName = account.ServiceName,
                    Username = account.Username,
                    Password = account.Password,
                    LastPasswordChangeDate = account.LastPasswordChangeDate,
                    ResetDate = account.ResetDate,
                    ResetPeriodDays = account.ResetPeriodDays,
                    Notes = account.Notes,
                    Tags = account.Tags,
                    Order = account.Order
                };
                
                PasswordBox.Password = account.Password;
                _selectedResetDate = account.ResetDate;
                _selectedPeriodDays = account.ResetPeriodDays;
                
                // Load tags
                if (!string.IsNullOrEmpty(account.Tags))
                {
                    var tagList = account.Tags.Split(new[] { ',', '|' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(t => t.Trim())
                        .Where(t => !string.IsNullOrEmpty(t))
                        .ToList();
                    
                    foreach (var tag in tagList)
                    {
                        _tags.Add(tag);
                        _tagService.AddTag(tag);
                    }
                }
                
                // 기존 설정에 맞게 UI 업데이트
                UpdatePeriodButtonSelection();
                UpdateExpiryStatusDisplay();
            }
            else
            {
                this.Title = "계정 추가";
                if (SaveButton != null)
                    SaveButton.Content = "저장";
                // 추가 모드에서는 삭제 버튼이 없으므로 해당 열이 공간을 차지하지 않도록 접음
                DeleteColumn.Width = new GridLength(0);
                Account = new Account { Id = Guid.NewGuid() };
            }
            
            DataContext = Account;
            LoadAvailableTags();
        }

        private void LoadAvailableTags()
        {
            var allTags = _tagService.GetAllTags();
            var availableTags = allTags.Where(t => !_tags.Any(st => st.Equals(t, StringComparison.OrdinalIgnoreCase))).ToList();
            AvailableTagsControl.ItemsSource = availableTags;
        }

        private void UpdateResetDateDisplay()
        {
            ResetDateTextBox.Text = _selectedResetDate?.ToString("yyyy-MM-dd") ?? "";
        }

        private void PeriodButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string tagStr && int.TryParse(tagStr, out int period))
            {
                _selectedPeriodDays = period;
                UpdatePeriodButtonSelection();
                
                // 직접입력 선택 시 날짜 입력 패널 표시
                CustomDatePanel.Visibility = period == -1 ? Visibility.Visible : Visibility.Collapsed;
                
                // 기간 선택 시 만료 상태 표시 업데이트
                UpdateExpiryStatusDisplay();
            }
        }

        private void UpdatePeriodButtonSelection()
        {
            // 모든 버튼 기본 스타일로 리셋
            var defaultBg = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D2D30"));
            var defaultBorder = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3F3F46"));
            var selectedBg = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#007ACC"));
            var selectedBorder = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#007ACC"));

            Period30Btn.Background = defaultBg;
            Period30Btn.BorderBrush = defaultBorder;
            Period60Btn.Background = defaultBg;
            Period60Btn.BorderBrush = defaultBorder;
            Period90Btn.Background = defaultBg;
            Period90Btn.BorderBrush = defaultBorder;
            PeriodCustomBtn.Background = defaultBg;
            PeriodCustomBtn.BorderBrush = defaultBorder;

            // 선택된 버튼 하이라이트
            Button? selectedButton = _selectedPeriodDays switch
            {
                30 => Period30Btn,
                60 => Period60Btn,
                90 => Period90Btn,
                -1 => PeriodCustomBtn,
                _ => null
            };

            if (selectedButton != null)
            {
                selectedButton.Background = selectedBg;
                selectedButton.BorderBrush = selectedBorder;
            }

            // 직접입력 패널 표시/숨김
            CustomDatePanel.Visibility = _selectedPeriodDays == -1 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void UpdateExpiryStatusDisplay()
        {
            // 기간이 선택되지 않았으면 숨김
            if (!_selectedPeriodDays.HasValue || _selectedPeriodDays == 0)
            {
                ExpiryStatusPanel.Visibility = Visibility.Collapsed;
                return;
            }

            // 직접입력이고 날짜가 선택되지 않았으면 숨김
            if (_selectedPeriodDays == -1 && !_selectedResetDate.HasValue)
            {
                ExpiryStatusPanel.Visibility = Visibility.Collapsed;
                return;
            }

            // 만료일 계산
            DateTime? expiryDate;
            if (_selectedPeriodDays == -1)
            {
                expiryDate = _selectedResetDate;
            }
            else
            {
                // 신규 계정은 현재 시간 기준, 기존 계정은 수정일 기준
                var baseDate = Account.LastPasswordChangeDate ?? DateTime.Now;
                expiryDate = baseDate.AddDays(_selectedPeriodDays.Value);
            }

            if (!expiryDate.HasValue)
            {
                ExpiryStatusPanel.Visibility = Visibility.Collapsed;
                return;
            }

            ExpiryStatusPanel.Visibility = Visibility.Visible;

            // 만료일 표시
            ExpiryDateText.Text = expiryDate.Value.ToString("yyyy-MM-dd");

            // D-day 계산
            var daysUntil = (expiryDate.Value.Date - DateTime.Now.Date).Days;

            // D-day 텍스트
            if (daysUntil == 0)
                DdayText.Text = "D-Day";
            else if (daysUntil > 0)
                DdayText.Text = $"D-{daysUntil}";
            else
                DdayText.Text = $"D+{Math.Abs(daysUntil)}";

            // 상태 및 색상 설정
            if (daysUntil < 0)
            {
                ExpiryStatusText.Text = "만료됨";
                ExpiryStatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F48771"));
                DdayText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F48771"));
            }
            else if (daysUntil == 0)
            {
                ExpiryStatusText.Text = "오늘 만료";
                ExpiryStatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F48771"));
                DdayText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F48771"));
            }
            else if (daysUntil <= 7)
            {
                ExpiryStatusText.Text = "곧 만료";
                ExpiryStatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CE9178"));
                DdayText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CE9178"));
            }
            else
            {
                ExpiryStatusText.Text = "정상";
                ExpiryStatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4EC9B0"));
                DdayText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4EC9B0"));
            }
        }

        private bool _isPasswordVisible;

        private void TogglePasswordVisibility_Click(object sender, RoutedEventArgs e)
        {
            _isPasswordVisible = !_isPasswordVisible;

            if (_isPasswordVisible)
            {
                // 마스킹 → 평문
                PasswordPlainBox.Text = PasswordBox.Password;
                PasswordPlainBox.Visibility = Visibility.Visible;
                PasswordBox.Visibility = Visibility.Collapsed;
                TogglePasswordButton.Content = "🙈";
                PasswordPlainBox.CaretIndex = PasswordPlainBox.Text.Length;
                PasswordPlainBox.Focus();
            }
            else
            {
                // 평문 → 마스킹
                PasswordBox.Password = PasswordPlainBox.Text;
                PasswordBox.Visibility = Visibility.Visible;
                PasswordPlainBox.Visibility = Visibility.Collapsed;
                TogglePasswordButton.Content = "👁";
                PasswordBox.Focus();
            }
        }

        /// <summary>현재 보이는 입력 컨트롤 기준으로 비밀번호 값을 반환합니다.</summary>
        private string GetCurrentPassword()
        {
            return _isPasswordVisible ? PasswordPlainBox.Text : PasswordBox.Password;
        }

        private void TagTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AddTag();
                e.Handled = true;
            }
        }

        private void AddTag()
        {
            var tagText = TagTextBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(tagText))
            {
                return;
            }

            // Check if tag already exists
            if (_tags.Any(t => t.Equals(tagText, StringComparison.OrdinalIgnoreCase)))
            {
                TagTextBox.Text = string.Empty;
                return;
            }

            _tags.Add(tagText);
            _tagService.AddTag(tagText);
            TagTextBox.Text = string.Empty;
            LoadAvailableTags();
        }

        private void RemoveTagButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.Tag is string tag)
            {
                _tags.Remove(tag);
                LoadAvailableTags();
            }
        }

        private void AvailableTag_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is System.Windows.Controls.Border border && border.DataContext is string tag)
            {
                if (!_tags.Any(t => t.Equals(tag, StringComparison.OrdinalIgnoreCase)))
                {
                    _tags.Add(tag);
                    LoadAvailableTags();
                }
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Account.ServiceName = AccountFieldLimits.Clamp(Account.ServiceName?.Trim(), AccountFieldLimits.ServiceNameMaxLength);
            Account.Notes = AccountFieldLimits.Clamp(Account.Notes, AccountFieldLimits.NotesMaxLength);
            Account.Password = GetCurrentPassword();
            Account.ResetPeriodDays = _selectedPeriodDays;
            Account.ResetDate = _selectedPeriodDays == -1 ? _selectedResetDate : null;
            Account.LastPasswordChangeDate = DateTime.Now;
            Account.Tags = string.Join(",", _tags);
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                $"'{Account.ServiceName}' 계정을 삭제하시겠습니까?\n\n이 작업은 되돌릴 수 없습니다.",
                "계정 삭제 확인",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                DeleteRequested = true;
                DialogResult = true;
                Close();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void SelectDateButton_Click(object sender, RoutedEventArgs e)
        {
            ResetDatePicker.IsDropDownOpen = true;
        }

        private void ResetDateTextBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ResetDatePicker.IsDropDownOpen = true;
            e.Handled = true;
        }

        private void ResetDatePicker_SelectedDateChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            _selectedResetDate = ResetDatePicker.SelectedDate;
            UpdateResetDateDisplay();
            UpdateExpiryStatusDisplay();
        }

        private void DeleteTagButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.Tag is string tag)
            {
                e.Handled = true; // Prevent the click from bubbling to the parent Border
                
                var result = MessageBox.Show($"'{tag}' 태그를 삭제하시겠습니까?\n\n※ 모든 계정에서 해당 태그가 제거됩니다.", 
                    "태그 삭제", MessageBoxButton.YesNo, MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    // Remove from tag service
                    _tagService.RemoveTag(tag);
                    
                    // Remove from all accounts
                    var accounts = _iniFileService.LoadAccounts();
                    bool hasChanges = false;
                    
                    foreach (var account in accounts)
                    {
                        if (!string.IsNullOrEmpty(account.Tags))
                        {
                            var tagList = account.Tags.Split(new[] { ',', '|' }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(t => t.Trim())
                                .Where(t => !t.Equals(tag, StringComparison.OrdinalIgnoreCase))
                                .ToList();
                            
                            var newTags = string.Join(",", tagList);
                            if (newTags != account.Tags)
                            {
                                account.Tags = newTags;
                                hasChanges = true;
                            }
                        }
                    }
                    
                    if (hasChanges)
                    {
                        _iniFileService.SaveAccounts(accounts);
                    }
                    
                    // Remove from current selection
                    var toRemove = _tags.FirstOrDefault(t => t.Equals(tag, StringComparison.OrdinalIgnoreCase));
                    if (toRemove != null)
                    {
                        _tags.Remove(toRemove);
                    }
                    
                    LoadAvailableTags();
                }
            }
        }
    }
}
