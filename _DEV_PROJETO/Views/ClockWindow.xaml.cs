using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using LetreiroDigital.ViewModels;
using LetreiroDigital.Services;

namespace LetreiroDigital.Views
{
    public partial class ClockWindow : Window
    {
        private AppViewModel _vm = null!;
        private readonly DispatcherTimer _clockTimer;

        public ClockWindow()
        {
            InitializeComponent();
            _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _clockTimer.Tick += (s, e) => UpdateClock();
            _clockTimer.Start();
            Loaded += (s, e) => UpdatePosition();
        }

        public void SetViewModel(AppViewModel vm)
        {
            _vm = vm;
            _vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName is nameof(AppViewModel.TimerFormatted) or nameof(AppViewModel.BlinkPhase) or nameof(AppViewModel.BlinkState)
                    or nameof(AppViewModel.TimerRunning) or nameof(AppViewModel.ShowTimer) or nameof(AppViewModel.ShowClock))
                {
                    Dispatcher.Invoke(UpdateTimer);
                }
            };
            _vm.StateChanged += () => Dispatcher.Invoke(UpdateAll);
            UpdateAll();
        }

        private void UpdateAll()
        {
            if (_vm == null) return;
            rootBorder.Background = AppViewModel.BrushFromHex(_vm.BgColor);
            clockDisplay.FontSize = _vm.ClockSize;
            UpdateClock();
            UpdateTimer();

            timerDisplay.Visibility = _vm.ShowTimer ? Visibility.Visible : Visibility.Collapsed;
            clockDisplay.Visibility = _vm.ShowClock ? Visibility.Visible : Visibility.Collapsed;
        }

        private void UpdateClock()
        {
            clockDisplay.Text = DateTime.Now.ToString("HH:mm:ss");
        }

        private void UpdateTimer()
        {
            if (_vm == null) return;
            timerDisplay.Text = _vm.TimerFormatted;

            if (_vm.BlinkPhase == BlinkPhase.Blink)
                timerDisplay.Foreground = _vm.BlinkState ? Brushes.Red : Brushes.White;
            else if (_vm.BlinkPhase == BlinkPhase.Red)
                timerDisplay.Foreground = Brushes.Red;
            else
                timerDisplay.Foreground = Brushes.White;
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
            Width = _vm.GetClockWindowWidth();
            Height = _vm.GetClockWindowHeight();
            Left = (_vm.ClockX ?? _vm.GetClockDefaultX()) / dpiScaleX;
            Top = (_vm.ClockY ?? _vm.GetClockDefaultY()) / dpiScaleY;
        }

        protected override void OnClosed(EventArgs e)
        {
            _clockTimer.Stop();
            base.OnClosed(e);
        }
    }
}
