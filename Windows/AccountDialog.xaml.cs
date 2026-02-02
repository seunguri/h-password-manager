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
            LoadTagSuggestions();
        }

        private void LoadTagSuggestions()
        {
            var allTags = _tagService.GetAllTags();
            TagComboBox.ItemsSource = allTags;
        }

        private void TagComboBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AddTag();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                TagComboBox.IsDropDownOpen = false;
            }
        }

        private void TagComboBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            var text = TagComboBox.Text;
            if (!string.IsNullOrWhiteSpace(text))
            {
                var suggestions = _tagService.GetSuggestions(text);
                TagComboBox.ItemsSource = suggestions;
                TagComboBox.IsDropDownOpen = true;
            }
            else
            {
                LoadTagSuggestions();
                TagComboBox.IsDropDownOpen = true;
            }
        }

        private void TagComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (TagComboBox.SelectedItem is string selectedTag)
            {
                TagComboBox.Text = selectedTag;
                AddTag();
                TagComboBox.SelectedItem = null;
            }
        }

        private void AddTag()
        {
            var tagText = TagComboBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(tagText))
            {
                return;
            }

            // Check if tag already exists
            if (_tags.Any(t => t.Equals(tagText, StringComparison.OrdinalIgnoreCase)))
            {
                TagComboBox.Text = string.Empty;
                TagComboBox.IsDropDownOpen = false;
                return;
            }

            _tags.Add(tagText);
            _tagService.AddTag(tagText);
            TagComboBox.Text = string.Empty;
            TagComboBox.IsDropDownOpen = false;
            LoadTagSuggestions();
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
            Account.Password = PasswordBox.Password;
            Account.ResetDate = ResetDatePicker.SelectedDate;
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

        private void AddTagButton_Click(object sender, RoutedEventArgs e)
        {
            AddTag();
        }

        private bool _isPasswordVisible = false;
        private System.Windows.Controls.TextBox? _passwordTextBox;
        
        private void TogglePasswordButton_Click(object sender, RoutedEventArgs e)
        {
            _isPasswordVisible = !_isPasswordVisible;
            var grid = PasswordBox.Parent as System.Windows.Controls.Grid;
            if (grid == null) return;
            
            if (_isPasswordVisible)
            {
                // Show password as text
                if (_passwordTextBox == null)
                {
                    _passwordTextBox = new System.Windows.Controls.TextBox
                    {
                        Text = PasswordBox.Password,
                        Background = PasswordBox.Background,
                        BorderBrush = PasswordBox.BorderBrush,
                        Foreground = PasswordBox.Foreground,
                        Padding = new Thickness(10, 8, 10, 8)
                    };
                }
                else
                {
                    _passwordTextBox.Text = PasswordBox.Password;
                }
                
                grid.Children.Remove(PasswordBox);
                System.Windows.Controls.Grid.SetColumn(_passwordTextBox, 0);
                grid.Children.Insert(0, _passwordTextBox);
            }
            else
            {
                // Show password as password box
                if (_passwordTextBox != null)
                {
                    PasswordBox.Password = _passwordTextBox.Text;
                    grid.Children.Remove(_passwordTextBox);
                }
                System.Windows.Controls.Grid.SetColumn(PasswordBox, 0);
                grid.Children.Insert(0, PasswordBox);
            }
        }
    }
}
