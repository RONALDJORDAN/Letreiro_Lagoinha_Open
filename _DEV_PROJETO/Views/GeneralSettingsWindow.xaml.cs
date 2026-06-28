using System.Windows;
using LetreiroDigital.ViewModels;

namespace LetreiroDigital.Views
{
    public partial class GeneralSettingsWindow : Window
    {
        private readonly AppViewModel _vm;

        public GeneralSettingsWindow(AppViewModel vm)
        {
            InitializeComponent();
            _vm = vm;
            DataContext = _vm;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
