using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using PasswordProtector.Models;
using PasswordProtector.Services;
using PasswordProtector.Windows;

namespace PasswordProtector
{
    public partial class MainWindow : Window
    {
        private readonly IniFileService _iniFileService;
        private ObservableCollection<Account> _accounts;
        private ObservableCollection<Account> _filteredAccounts;
        private HashSet<string> _selectedTags = new HashSet<string>();

        public MainWindow()
        {
            InitializeComponent();
            _iniFileService = new IniFileService();
            LoadAccounts();
            LoadTagFilters();
            
            // 만료 임박/초과 계정 알림 표시
            ShowExpiryNotification();
            
            // 원본 파일 경로 표시
            FilePathText.Text = _iniFileService.FilePath;
            
            // Register global hotkey when window handle is created
            this.SourceInitialized += MainWindow_SourceInitialized;
            
            // Placeholder text handling
            SearchTextBox.GotFocus += (s, e) => 
            {
                var placeholder = SearchTextBox.Template.FindName("PlaceholderText", SearchTextBox) as TextBlock;
                if (placeholder != null) placeholder.Visibility = Visibility.Collapsed;
            };
            
            SearchTextBox.LostFocus += (s, e) => 
            {
                if (string.IsNullOrEmpty(SearchTextBox.Text))
                {
                    var placeholder = SearchTextBox.Template.FindName("PlaceholderText", SearchTextBox) as TextBlock;
                    if (placeholder != null) placeholder.Visibility = Visibility.Visible;
                }
            };
        }

        private void MainWindow_SourceInitialized(object? sender, EventArgs e)
        {
            var app = Application.Current as App;
            if (app != null)
            {
                app.RegisterHotKey(this);
            }
        }

        private void LoadAccounts()
        {
            var accounts = _iniFileService.LoadAccounts();
            _accounts = new ObservableCollection<Account>(accounts);
            _filteredAccounts = new ObservableCollection<Account>(_accounts);
            AccountCardsControl.ItemsSource = _filteredAccounts;
            UpdateAccountCount();
        }

        private void LoadTagFilters()
        {
            // 태그 파일에서 최신 데이터를 읽기 위해 새 인스턴스 생성
            var tagService = new TagService();
            var allTags = tagService.GetAllTags();
            TagFilterControl.ItemsSource = allTags;
        }

        private void ShowExpiryNotification()
        {
            try
            {
                // 만료된 계정 (D+, 음수 일수)
                var expiredAccounts = _accounts
                    .Where(a => a.DaysUntilExpiry.HasValue && a.DaysUntilExpiry < 0)
                    .OrderBy(a => a.DaysUntilExpiry)
                    .ToList();
                
                // 만료 임박 계정 (7일 이내, D-Day 포함)
                var soonExpiringAccounts = _accounts
                    .Where(a => a.DaysUntilExpiry.HasValue && a.DaysUntilExpiry >= 0 && a.DaysUntilExpiry <= 7)
                    .OrderBy(a => a.DaysUntilExpiry)
                    .ToList();
                
                // 알림할 계정이 없으면 종료
                if (expiredAccounts.Count == 0 && soonExpiringAccounts.Count == 0)
                    return;
                
                // 알림 메시지 구성
                var contentBuilder = new StringBuilder();
                
                if (expiredAccounts.Count > 0)
                {
                    contentBuilder.AppendLine($"[만료됨] ({expiredAccounts.Count}개)");
                    foreach (var account in expiredAccounts.Take(3))
                    {
                        contentBuilder.AppendLine($"  {account.ServiceName} ({account.ExpiryDdayDisplay})");
                    }
                    if (expiredAccounts.Count > 3)
                    {
                        contentBuilder.AppendLine($"  ... 외 {expiredAccounts.Count - 3}개");
                    }
                }
                
                if (soonExpiringAccounts.Count > 0)
                {
                    if (contentBuilder.Length > 0)
                        contentBuilder.AppendLine();
                    
                    contentBuilder.AppendLine($"[만료 임박] ({soonExpiringAccounts.Count}개)");
                    foreach (var account in soonExpiringAccounts.Take(3))
                    {
                        contentBuilder.AppendLine($"  {account.ServiceName} ({account.ExpiryDdayDisplay})");
                    }
                    if (soonExpiringAccounts.Count > 3)
                    {
                        contentBuilder.AppendLine($"  ... 외 {soonExpiringAccounts.Count - 3}개");
                    }
                }
                
                // 시스템 트레이 벌룬 알림 표시 (5초간)
                var app = System.Windows.Application.Current as App;
                var trayIcon = app?.TrayIcon;
                if (trayIcon != null)
                {
                    trayIcon.ShowBalloonTip(
                        5000, // 5초
                        "비밀번호 만료 알림",
                        contentBuilder.ToString().TrimEnd(),
                        System.Windows.Forms.ToolTipIcon.Warning
                    );
                }
            }
            catch (Exception ex)
            {
                // 알림 실패 시 조용히 무시 (프로그램 실행에 영향 없음)
                Debug.WriteLine($"Notification error: {ex.Message}");
            }
        }

        private void UpdateAccountCount()
        {
            AccountCountText.Text = $"총 {_filteredAccounts.Count}개의 계정";
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AccountDialog();
            if (dialog.ShowDialog() == true)
            {
                // 파일에서 최신 데이터 로드 후 새 계정 추가
                var freshAccounts = _iniFileService.LoadAccounts();
                freshAccounts.Add(dialog.Account);
                _iniFileService.SaveAccounts(freshAccounts);
            }
            // 변경사항 반영을 위해 다시 로드
            LoadAccounts();
            LoadTagFilters();
        }

        private void AccountCard_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is Account account)
            {
                var originalServiceName = account.ServiceName;
                var originalUsername = account.Username;
                
                var dialog = new AccountDialog(account);
                if (dialog.ShowDialog() == true)
                {
                    // 파일에서 최신 데이터 로드 (다이얼로그에서 태그 삭제 등의 변경사항 반영)
                    var freshAccounts = _iniFileService.LoadAccounts();
                    
                    // 수정된 계정 찾아서 업데이트
                    var index = freshAccounts.FindIndex(a => 
                        a.ServiceName == originalServiceName && a.Username == originalUsername);
                    
                    if (index >= 0)
                    {
                        freshAccounts[index] = dialog.Account;
                        _iniFileService.SaveAccounts(freshAccounts);
                    }
                }
                // 변경사항 반영을 위해 다시 로드
                LoadAccounts();
                LoadTagFilters();
            }
        }

        private void TogglePassword_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Account account)
            {
                account.IsPasswordVisible = !account.IsPasswordVisible;
                // Force UI update by refreshing the collection
                var index = _filteredAccounts.IndexOf(account);
                if (index >= 0)
                {
                    var temp = _filteredAccounts[index];
                    _filteredAccounts[index] = null;
                    _filteredAccounts[index] = temp;
                }
            }
        }

        private void ShowAllPasswordsCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            SetAllPasswordsVisibility(true);
        }

        private void ShowAllPasswordsCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            SetAllPasswordsVisibility(false);
        }

        private void SetAllPasswordsVisibility(bool visible)
        {
            foreach (var account in _filteredAccounts)
            {
                account.IsPasswordVisible = visible;
            }
            
            // Force UI update by refreshing the collection
            var tempList = _filteredAccounts.ToList();
            _filteredAccounts.Clear();
            foreach (var account in tempList)
            {
                _filteredAccounts.Add(account);
            }
        }

        private void CopyPassword_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Account account)
            {
                Clipboard.SetText(account.Password);
                // MessageBox.Show("비밀번호가 클립보드에 복사되었습니다.", "복사 완료", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void DeleteAccount_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true; // Prevent card click event from firing
            
            if (sender is Button button && button.Tag is Account account)
            {
                var result = MessageBox.Show(
                    $"'{account.ServiceName}' 계정을 삭제하시겠습니까?\n\n이 작업은 되돌릴 수 없습니다.",
                    "계정 삭제 확인",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);
                
                if (result == MessageBoxResult.Yes)
                {
                    // 파일에서 최신 데이터 로드 후 계정 삭제
                    var freshAccounts = _iniFileService.LoadAccounts();
                    var accountToRemove = freshAccounts.FirstOrDefault(a => 
                        a.ServiceName == account.ServiceName && a.Username == account.Username);
                    
                    if (accountToRemove != null)
                    {
                        freshAccounts.Remove(accountToRemove);
                        _iniFileService.SaveAccounts(freshAccounts);
                    }
                    
                    // 변경사항 반영을 위해 다시 로드
                    LoadAccounts();
                    LoadTagFilters();
                }
            }
        }

        private void TagFilter_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is string tag)
            {
                if (_selectedTags.Contains(tag))
                {
                    _selectedTags.Remove(tag);
                    border.Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#2D2D30"));
                }
                else
                {
                    _selectedTags.Add(tag);
                    border.Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#007ACC"));
                }
                ApplyFilters();
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ApplyFilters();
            }
        }

        private void ApplyFilters()
        {
            var searchText = SearchTextBox.Text?.ToLower() ?? string.Empty;
            
            var filtered = _accounts.Where(a =>
            {
                // Search filter
                bool matchesSearch = string.IsNullOrEmpty(searchText) ||
                    (a.ServiceName?.ToLower().Contains(searchText) ?? false) ||
                    (a.Username?.ToLower().Contains(searchText) ?? false) ||
                    (a.Tags?.ToLower().Contains(searchText) ?? false) ||
                    (a.Notes?.ToLower().Contains(searchText) ?? false);
                
                // Tag filter
                bool matchesTags = _selectedTags.Count == 0;
                if (!matchesTags && !string.IsNullOrEmpty(a.Tags))
                {
                    var accountTags = a.Tags.Split(new[] { ',', '|' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(t => t.Trim().ToLower())
                        .ToHashSet();
                    matchesTags = _selectedTags.Any(t => accountTags.Contains(t.ToLower()));
                }
                
                return matchesSearch && matchesTags;
            }).ToList();

            _filteredAccounts.Clear();
            foreach (var account in filtered)
            {
                _filteredAccounts.Add(account);
            }
            
            UpdateAccountCount();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.WindowState = WindowState.Minimized;
            }
            else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.N)
            {
                AddButton_Click(sender, e);
            }
            else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.F)
            {
                SearchTextBox.Focus();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Minimize to tray instead of closing
            e.Cancel = true;
            this.WindowState = WindowState.Minimized;
            this.Hide();
        }

        private void OpenFilePath_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var filePath = _iniFileService.FilePath;
                if (File.Exists(filePath))
                {
                    // 파일이 있는 폴더를 열고 해당 파일을 선택
                    Process.Start("explorer.exe", $"/select,\"{filePath}\"");
                }
                else
                {
                    // 파일이 없으면 폴더만 열기
                    var directory = Path.GetDirectoryName(filePath);
                    if (directory != null && Directory.Exists(directory))
                    {
                        Process.Start("explorer.exe", directory);
                    }
                    else
                    {
                        MessageBox.Show("파일 경로를 찾을 수 없습니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"파일 경로를 열 수 없습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #region 시작프로그램 설정

        private const string StartupRegistryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "PasswordProtector";
        private bool _isStartupCheckBoxLoading = false;

        private async void StartupCheckBox_Loaded(object sender, RoutedEventArgs e)
        {
            _isStartupCheckBoxLoading = true;
            
            // 비동기로 레지스트리 상태 확인 (UI 블로킹 방지)
            var isEnabled = await Task.Run(() => IsStartupEnabled());
            
            // UI 스레드에서 체크박스 상태 업데이트
            StartupCheckBox.IsChecked = isEnabled;
            
            _isStartupCheckBoxLoading = false;
        }

        private async void StartupCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            // 로딩 중에는 레지스트리 변경하지 않음
            if (_isStartupCheckBoxLoading) return;
            
            // 비동기로 레지스트리 설정 (UI 블로킹 방지)
            await Task.Run(() => SetStartupEnabled(true));
        }

        private async void StartupCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            // 로딩 중에는 레지스트리 변경하지 않음
            if (_isStartupCheckBoxLoading) return;
            
            // 비동기로 레지스트리 설정 (UI 블로킹 방지)
            await Task.Run(() => SetStartupEnabled(false));
        }

        private bool IsStartupEnabled()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(StartupRegistryKey, false))
                {
                    if (key != null)
                    {
                        var value = key.GetValue(AppName);
                        return value != null;
                    }
                }
            }
            catch
            {
                // 레지스트리 접근 실패 시 false 반환
            }
            return false;
        }

        private void SetStartupEnabled(bool enable)
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(StartupRegistryKey, true))
                {
                    if (key != null)
                    {
                        if (enable)
                        {
                            // 현재 실행 파일 경로를 레지스트리에 등록
                            var exePath = Process.GetCurrentProcess().MainModule?.FileName;
                            if (!string.IsNullOrEmpty(exePath))
                            {
                                key.SetValue(AppName, $"\"{exePath}\"");
                            }
                        }
                        else
                        {
                            // 레지스트리에서 삭제
                            key.DeleteValue(AppName, false);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // UI 스레드에서 메시지 박스 표시
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"시작프로그램 설정을 변경할 수 없습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        #endregion
    }
}
