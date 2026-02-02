using System;
using System.Windows;
using System.Windows.Forms;
using PasswordProtector.Services;
using Application = System.Windows.Application;

namespace PasswordProtector
{
    public partial class App : Application
    {
        private NotifyIcon? _notifyIcon;
        private HotKeyService? _hotKeyService;

        public void RegisterHotKey(Window window)
        {
            if (_hotKeyService == null)
            {
                _hotKeyService = new HotKeyService();
                _hotKeyService.HotKeyPressed += (s, args) =>
                {
                    // Execute on UI thread
                    window?.Dispatcher.Invoke(() =>
                    {
                        ShowMainWindow();
                    });
                };
            }

            var registered = _hotKeyService.Register(window);
            if (!registered)
            {
                // Try to register with a different ID or show error
                System.Windows.MessageBox.Show(
                    "전역 단축키 등록에 실패했습니다. 다른 애플리케이션에서 같은 단축키를 사용 중일 수 있습니다.",
                    "단축키 등록 실패",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Create system tray icon
            var iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
            _notifyIcon = new NotifyIcon
            {
                Icon = System.IO.File.Exists(iconPath) 
                    ? new System.Drawing.Icon(iconPath) 
                    : System.Drawing.SystemIcons.Application,
                Text = "계정 관리 (Ctrl+Shift+P)",
                Visible = true
            };

            _notifyIcon.DoubleClick += (sender, args) =>
            {
                ShowMainWindow();
            };

            // Create context menu
            var contextMenu = new ContextMenuStrip();
            var showMenuItem = new ToolStripMenuItem("보이기 (Ctrl+Shift+P)");
            showMenuItem.Click += (s, args) =>
            {
                ShowMainWindow();
            };
            var exitMenuItem = new ToolStripMenuItem("종료");
            exitMenuItem.Click += (s, args) =>
            {
                _hotKeyService?.Unregister();
                _notifyIcon?.Dispose();
                Shutdown();
            };

            contextMenu.Items.Add(showMenuItem);
            contextMenu.Items.Add(exitMenuItem);
            _notifyIcon.ContextMenuStrip = contextMenu;
        }

        private void ShowMainWindow()
        {
            if (MainWindow != null)
            {
                MainWindow.Show();
                MainWindow.WindowState = WindowState.Normal;
                MainWindow.Activate();
                MainWindow.Focus();
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _hotKeyService?.Unregister();
            _notifyIcon?.Dispose();
            base.OnExit(e);
        }
    }
}
