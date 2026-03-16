using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using Microsoft.Toolkit.Uwp.Notifications;
using PasswordProtector.Services;
using Application = System.Windows.Application;

namespace PasswordProtector
{
    public partial class App : Application
    {
        private static Mutex? _singleInstanceMutex;
        private NotifyIcon? _notifyIcon;
        private HotKeyService? _hotKeyService;
        
        // 외부에서 벌룬 알림을 표시할 수 있도록 공개
        public NotifyIcon? TrayIcon => _notifyIcon;


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
            // 비-UI 스레드 예외 - 로깅 후 종료 
            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "fatal.log");
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                File.AppendAllText(logPath, $"[{timestamp}]\n{args.ExceptionObject}\n\n");
            };

            // UI 스레드 예외는 복구 가능
            DispatcherUnhandledException += (s, args) =>
            {
                var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error.log");
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                File.AppendAllText(logPath, $"[{timestamp}] UI Exception\n{args.Exception}\n\n");
                
                System.Windows.MessageBox.Show("오류가 발생했습니다. 로그를 확인해주세요.", "오류");
                args.Handled = true; // 앱 계속 실행
            };

            const string mutexName = "PasswordProtector_SingleInstance_Mutex";
            _singleInstanceMutex = new Mutex(true, mutexName, out bool createdNew);
            if (!createdNew)
            {
                System.Windows.MessageBox.Show(
                    "프로그램이 이미 실행 중입니다.",
                    "계정 관리",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                Shutdown();
                return;
            }

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

            // MainWindow가 로드된 후 토스트 알림 표시 (ToastWindow가 MainWindow로 설정되는 것 방지)
            Dispatcher.BeginInvoke(new Action(() =>
            {
                ShowStartupToast();
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private static void ShowStartupToast()
        {
            ToastWindow.ShowToast();
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
            _singleInstanceMutex?.ReleaseMutex();
            _singleInstanceMutex?.Dispose();
            base.OnExit(e);
        }
    }
}
