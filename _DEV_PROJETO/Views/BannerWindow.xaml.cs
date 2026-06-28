using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using LetreiroDigital.ViewModels;

namespace LetreiroDigital.Views
{
    public partial class BannerWindow : Window
    {
        private AppViewModel _vm = null!;
        private double _scrollX;
        private readonly DispatcherTimer _scrollTimer;

        public BannerWindow()
        {
            InitializeComponent();
            _scrollTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) }; // ~60fps
            _scrollTimer.Tick += ScrollTick;
            Loaded += (s, e) => UpdatePosition();
        }

        public void SetViewModel(AppViewModel vm)
        {
            _vm = vm;
            _vm.StateChanged += () => Dispatcher.Invoke(UpdateUI);
            _vm.PropertyChanged += (s, e) => Dispatcher.Invoke(UpdateUI);
            _scrollTimer.Start();
            UpdateUI();
        }

        private void UpdateUI()
        {
            if (_vm == null) return;
            rootBorder.Background = AppViewModel.BrushFromHex(_vm.BgColor);
            scrollText.Text = _vm.DisplayText;
            scrollText.FontSize = Math.Max(10, _vm.BannerHeight * 0.55);

            if (_vm.CurrentItem != null)
            {
                try { scrollText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(_vm.CurrentItem.Color)); }
                catch { scrollText.Foreground = Brushes.White; }
            }
            else
            {
                scrollText.Foreground = Brushes.White;
            }
        }

        private void ScrollTick(object? sender, EventArgs e)
        {
            if (_vm == null) return;
            _scrollX -= _vm.Speed;
            double textW = scrollText.ActualWidth;
            double canvasW = scrollCanvas.ActualWidth;
            if (textW > 0 && _scrollX < -textW) _scrollX = canvasW;
            System.Windows.Controls.Canvas.SetLeft(scrollText, _scrollX);
            System.Windows.Controls.Canvas.SetTop(scrollText, (scrollCanvas.ActualHeight - scrollText.ActualHeight) / 2);
        }

        public void UpdatePosition()
        {
            if (_vm == null) return;
            double dpiScaleX = 1.0, dpiScaleY = 1.0;
            var source = PresentationSource.FromVisual(this);
            if (source?.CompositionTarget != null)
            {
                var matrix = source.CompositionTarget.TransformToDevice;
                dpiScaleX = matrix.M11;
                dpiScaleY = matrix.M22;
            }
            Left = _vm.BannerX / dpiScaleX;
            Top = _vm.BannerY / dpiScaleY;
            Width = _vm.MonitorWidth / dpiScaleX;
            Height = _vm.BannerHeight; // Already in DIPs
        }

        protected override void OnClosed(EventArgs e)
        {
            _scrollTimer.Stop();
            base.OnClosed(e);
        }
    }
}
