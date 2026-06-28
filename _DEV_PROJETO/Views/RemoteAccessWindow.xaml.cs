using System;
using System.Windows;
using LetreiroDigital.ViewModels;

namespace LetreiroDigital.Views
{
    public partial class RemoteAccessWindow : Window
    {
        private readonly AppViewModel _vm;

        public RemoteAccessWindow(AppViewModel vm)
        {
            InitializeComponent();
            _vm = vm;
            
            // Load current values
            txtPort.Text = _vm.RemotePort.ToString();
            txtPassword.Text = _vm.RemotePassword;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(txtPort.Text, out int port) && port > 1024 && port < 65535)
            {
                _vm.RemotePort = port;
                _vm.RemotePassword = txtPassword.Text.Trim();
                
                // Save to config (implicitly done via AppViewModel property setters if we wired them up, 
                // but for now we might need to manually trigger save or ensure properties call Save)
                // Assuming AppViewModel handles persistence or we trigger it.
                _vm.SaveConfig(); 

                MessageBox.Show("Configurações salvas!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Porta inválida! Use um valor entre 1025 e 65535.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
