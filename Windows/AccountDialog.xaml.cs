using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using PasswordProtector.Models;
using PasswordProtector.Services;

namespace PasswordProtector.Windows
{
    public partial class AccountDialog : Window
    {
        public Account Account { get; private set; }
        private readonly TagService _tagService;
        private ObservableCollection<string> _tags;
        private bool _isPasswordVisible = false;
        private bool _isSyncingPassword = false;
        private DateTime? _selectedResetDate;

        public AccountDialog(Account? account = null)
        {
            InitializeComponent();
            _tagService = new TagService();
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
                    Notes = account.Notes,
                    Tags = account.Tags,
                    Order = account.Order
                };
                
                PasswordBox.Password = account.Password;
                _selectedResetDate = account.ResetDate;
                UpdateResetDateDisplay();
                
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
            }
            else
            {
                this.Title = "계정 추가";
                if (SaveButton != null)
                    SaveButton.Content = "저장";
                Account = new Account();
            }
            
            DataContext = Account;
        }

        private void UpdateResetDateDisplay()
        {
            ResetDateTextBox.Text = _selectedResetDate?.ToString("yyyy-MM-dd") ?? "";
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
        }

        private void RemoveTagButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.Tag is string tag)
            {
                _tags.Remove(tag);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Account.Password = _isPasswordVisible ? PasswordTextBox.Text : PasswordBox.Password;
            Account.ResetDate = _selectedResetDate;
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
        }
    }
}
