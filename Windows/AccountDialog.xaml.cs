using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using PasswordProtector.Models;
using PasswordProtector.Services;

namespace PasswordProtector.Windows
{
    public partial class AccountDialog : Window
    {
        public Account Account { get; private set; }
        private readonly TagService _tagService;
        private readonly IniFileService _iniFileService;
        private ObservableCollection<string> _tags;
        private bool _isPasswordVisible = false;
        private bool _isSyncingPassword = false;
        private DateTime? _selectedResetDate;
        private int? _selectedPeriodDays;

        public AccountDialog(Account? account = null)
        {
            InitializeComponent();
            _tagService = new TagService();
            _iniFileService = new IniFileService();
            _tags = new ObservableCollection<string>();
            TagChipsControl.ItemsSource = _tags;
            
            if (account != null)
            {
                this.Title = "계정 수정";
                if (SaveButton != null)
                    SaveButton.Content = "수정";
                Account = new Account
                {
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
                Account = new Account();
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
            Account.Password = _isPasswordVisible ? PasswordTextBox.Text : PasswordBox.Password;
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

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void TogglePasswordButton_Click(object sender, RoutedEventArgs e)
        {
            _isPasswordVisible = !_isPasswordVisible;
            
            if (_isPasswordVisible)
            {
                // Show password as text
                PasswordTextBox.Text = PasswordBox.Password;
                PasswordBox.Visibility = Visibility.Collapsed;
                PasswordTextBox.Visibility = Visibility.Visible;
                TogglePasswordBtn.Content = "👁‍🗨";
            }
            else
            {
                // Show password as password box
                PasswordBox.Password = PasswordTextBox.Text;
                PasswordTextBox.Visibility = Visibility.Collapsed;
                PasswordBox.Visibility = Visibility.Visible;
                TogglePasswordBtn.Content = "👁";
            }
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_isSyncingPassword) return;
            _isSyncingPassword = true;
            PasswordTextBox.Text = PasswordBox.Password;
            _isSyncingPassword = false;
        }

        private void PasswordTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (_isSyncingPassword) return;
            _isSyncingPassword = true;
            PasswordBox.Password = PasswordTextBox.Text;
            _isSyncingPassword = false;
        }

        private void SelectDateButton_Click(object sender, RoutedEventArgs e)
        {
            ResetDatePicker.IsDropDownOpen = true;
        }

        private void ResetDateTextBox_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ResetDatePicker.IsDropDownOpen = true;
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
