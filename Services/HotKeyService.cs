using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace PasswordProtector.Services
{
    public class HotKeyService
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("kernel32.dll")]
        private static extern uint GetLastError();

        private const int HOTKEY_ID = 9000;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;
        private const uint VK_P = 0x50; // P key

        private IntPtr _windowHandle;
        private HwndSource? _source;

        public event EventHandler? HotKeyPressed;

        public bool Register(Window window)
        {
            var helper = new WindowInteropHelper(window);
            _windowHandle = helper.Handle;
            
            if (_windowHandle == IntPtr.Zero)
            {
                return false;
            }
            
            _source = HwndSource.FromHwnd(_windowHandle);
            
            if (_source == null)
            {
                return false;
            }
            
            _source.AddHook(HwndHook);

            // Register Ctrl+Shift+P
            bool result = RegisterHotKey(_windowHandle, HOTKEY_ID, MOD_CONTROL | MOD_SHIFT, VK_P);
            
            if (!result)
            {
                uint error = GetLastError();
                // Error 1409: Hotkey is already registered
                // Error 1419: Invalid window handle
                System.Diagnostics.Debug.WriteLine($"RegisterHotKey failed with error: {error}");
            }
            
            return result;
        }

        public void Unregister()
        {
            if (_windowHandle != IntPtr.Zero)
            {
                UnregisterHotKey(_windowHandle, HOTKEY_ID);
            }

            if (_source != null)
            {
                _source.RemoveHook(HwndHook);
                _source = null;
            }
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;
            if (msg == WM_HOTKEY)
            {
                System.Diagnostics.Debug.WriteLine($"WM_HOTKEY received: wParam={wParam}, HOTKEY_ID={HOTKEY_ID}");
                if (wParam.ToInt32() == HOTKEY_ID)
                {
                    System.Diagnostics.Debug.WriteLine("HotKey pressed! Invoking event.");
                    HotKeyPressed?.Invoke(this, EventArgs.Empty);
                    handled = true;
                }
            }
            return IntPtr.Zero;
        }
    }
}
