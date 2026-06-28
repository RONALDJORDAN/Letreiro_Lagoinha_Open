using System.Windows;
using LetreiroDigital.ViewModels;

namespace LetreiroDigital.Views
{
    public partial class PreviewWindow : Window
    {
        public PreviewWindow(AppViewModel vm)
        {
            InitializeComponent();
            tvView.SetViewModel(vm);
        }
    }
}
