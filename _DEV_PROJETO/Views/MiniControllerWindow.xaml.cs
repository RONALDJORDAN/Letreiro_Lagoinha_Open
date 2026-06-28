using System.Windows;
using System.Windows.Media;
using LetreiroDigital.ViewModels;
using LetreiroDigital.Services;

namespace LetreiroDigital.Views
{
    public partial class MiniControllerWindow : Window
    {
        private AppViewModel _vm = null!;

        public MiniControllerWindow()
        {
            InitializeComponent();
        }

        public void SetViewModel(AppViewModel vm)
        {
            _vm = vm;
            _vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName is nameof(AppViewModel.TimerFormatted) or nameof(AppViewModel.BlinkPhase)
                    or nameof(AppViewModel.BlinkState) or nameof(AppViewModel.CurrentItem))
                {
                    Dispatcher.Invoke(UpdateUI);
                }
            };
            _vm.ItemSelected += () => Dispatcher.Invoke(UpdateUI);
            _vm.StateChanged += () => Dispatcher.Invoke(UpdateUI);
            UpdateUI();
        }

        private void UpdateUI()
        {
            if (_vm == null) return;
            lblTimer.Text = _vm.TimerFormatted;
            lblItemName.Text = _vm.CurrentItem?.Content ?? "Nenhum item";

            if (_vm.BlinkPhase == BlinkPhase.Blink)
                lblTimer.Foreground = _vm.BlinkState ? Brushes.Red : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00E676"));
            else if (_vm.BlinkPhase == BlinkPhase.Red)
                lblTimer.Foreground = Brushes.Red;
            else
                lblTimer.Foreground = _vm.TimerRunning
                    ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00E676"))
                    : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#555555"));
        }

        private void BtnPrev_Click(object sender, RoutedEventArgs e)
        {
            _vm?.PrevItem();
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            _vm?.EmergencyStop();
        }

        private void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            _vm?.AdvanceToNextItem();
        }
    }
}
