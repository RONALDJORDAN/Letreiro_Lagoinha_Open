using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using LetreiroDigital.ViewModels;

namespace LetreiroDigital.Views
{
    public partial class TvModeWindow : Window
    {
        // Win32 API for pixel-perfect fullscreen positioning
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
            int X, int Y, int cx, int cy, uint uFlags);

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint SWP_SHOWWINDOW = 0x0040;
        private const uint SWP_NOACTIVATE = 0x0010;

        private AppViewModel _vm = null!;

        public TvModeWindow()
        {
            InitializeComponent();
            Loaded += (s, e) => UpdatePosition();
        }

        public void SetViewModel(AppViewModel vm)
        {
            _vm = vm;
            DataContext = _vm;
            
            // Delegate to the UserControl
            tvView.SetViewModel(vm);

            // Initial positioning
            UpdatePosition();
        }

        // ==================== FULLSCREEN POSITIONING ====================
        public void UpdatePosition()
        {
            if (_vm == null) return;

            var hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd != IntPtr.Zero)
            {
                // Win32 API: physical pixels directly — no DPI gaps
                SetWindowPos(hwnd, HWND_TOPMOST,
                    _vm.MonitorX, _vm.MonitorY,
                    _vm.MonitorWidth, _vm.MonitorHeight,
                    SWP_SHOWWINDOW | SWP_NOACTIVATE);
            }
            else
            {
                // Fallback before window handle exists
                Left = _vm.MonitorX;
                Top = _vm.MonitorY;
                Width = _vm.MonitorWidth;
                Height = _vm.MonitorHeight;
            }
        }

        // ==================== KEYBOARD HANDLER (ESC to close) ====================
        private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                _vm?.EmergencyStop();
                e.Handled = true;
            }
        }
    }
}
