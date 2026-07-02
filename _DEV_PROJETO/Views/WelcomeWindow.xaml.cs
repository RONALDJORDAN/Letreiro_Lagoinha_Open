using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace LetreiroDigital.Views
{
    public partial class WelcomeWindow : Window
    {
        public WelcomeWindow()
        {
            InitializeComponent();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private async void BtnIniciar_Click(object sender, RoutedEventArgs e)
        {
            BtnIniciar.Visibility = Visibility.Collapsed;
            PanelLoading.Visibility = Visibility.Visible;

            // Simula o tempo de checagem para efeito visual de "trabalhando"
            LblStatus.Text = "Verificando fontes do sistema...";
            await Task.Delay(1500);

            try
            {
                LblStatus.Text = "Verificando engine gráfica...";
                await Task.Delay(1200);

                LblStatus.Text = "Tudo pronto! Carregando Sistema...";
                await Task.Delay(1000);

                // Grava a flag de primeiro boot
                string configDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "LetreiroDigital", "config");
                    
                if (!Directory.Exists(configDir))
                {
                    Directory.CreateDirectory(configDir);
                }

                string firstRunFile = Path.Combine(configDir, "first_run.json");
                File.WriteAllText(firstRunFile, "{ \"configured\": true, \"date\": \"" + DateTime.Now.ToString("O") + "\" }");

                // Fecha essa janela para permitir que o App.xaml.cs continue o boot
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ocorreu um erro durante a configuração inicial:\n{ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                BtnIniciar.Visibility = Visibility.Visible;
                PanelLoading.Visibility = Visibility.Collapsed;
            }
        }
    }
}
