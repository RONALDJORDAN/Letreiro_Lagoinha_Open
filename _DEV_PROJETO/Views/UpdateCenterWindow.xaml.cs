using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using LetreiroDigital.Models;
using LetreiroDigital.Services;

namespace LetreiroDigital.Views
{
    public partial class UpdateCenterWindow : Window
    {
        private readonly UpdateService _updateService;
        private UpdateInfo? _pendingUpdate;
        private bool _isForced = false;

        /// <summary>
        /// Indica se o usuário iniciou a instalação (para o App.xaml.cs saber se deve fechar).
        /// </summary>
        public bool DidStartUpdate { get; private set; } = false;

        public UpdateCenterWindow()
        {
            InitializeComponent();
            _updateService = new UpdateService();
            _updateService.StateChanged += OnUpdateStateChanged;

            lblCurrentVersion.Text = $"v{UpdateService.CurrentVersion} (build {UpdateService.CurrentBuildNumber})";
        }

        /// <summary>
        /// Construtor que já abre com uma atualização pré-detectada (do startup silencioso).
        /// </summary>
        public UpdateCenterWindow(UpdateInfo preDetected) : this()
        {
            _pendingUpdate = preDetected;
            ShowUpdateAvailable(preDetected);
        }

        /// <summary>
        /// Construtor para modo FORÇADO: não permite fechar sem atualizar.
        /// </summary>
        public UpdateCenterWindow(UpdateInfo preDetected, bool forceUpdate) : this(preDetected)
        {
            _isForced = forceUpdate;
            if (_isForced)
            {
                Title = "⚠️ ATUALIZAÇÃO RECOMENDADA";
                lblStatusMessage.Text = "Esta atualização é importante. Recomendamos instalar para garantir o funcionamento.";
                lblStatusMessage.Foreground = (SolidColorBrush)FindResource("AccentGold");
                lblStatusIcon.Text = "🔔";
            }
        }

        // ═══════════════════════════════════════════════════════════
        // EVENT HANDLERS
        // ═══════════════════════════════════════════════════════════

        private async void BtnCheckUpdate_Click(object sender, RoutedEventArgs e)
        {
            btnCheckUpdate.IsEnabled = false;
            btnCheckUpdate.Content = "⏳  Verificando...";

            var update = await _updateService.CheckForUpdateAsync();

            if (update != null)
            {
                _pendingUpdate = update;
                ShowUpdateAvailable(update);
            }

            btnCheckUpdate.IsEnabled = true;
            btnCheckUpdate.Content = "🔍  VERIFICAR ATUALIZAÇÕES";
        }

        private async void BtnDownload_Click(object sender, RoutedEventArgs e)
        {
            if (_pendingUpdate == null) return;

            btnDownload.IsEnabled = false;
            btnCheckUpdate.Visibility = Visibility.Collapsed;
            btnCancel.Visibility = Visibility.Visible;
            pnlProgress.Visibility = Visibility.Visible;

            bool success = await _updateService.DownloadUpdateAsync(_pendingUpdate);

            btnCancel.Visibility = Visibility.Collapsed;

            if (success)
            {
                btnDownload.Visibility = Visibility.Collapsed;
                btnInstall.Visibility = Visibility.Visible;
            }
            else
            {
                btnDownload.IsEnabled = true;
            }
        }

        private void BtnInstall_Click(object sender, RoutedEventArgs e)
        {
            var state = _updateService.State;
            if (string.IsNullOrEmpty(state.DownloadedFilePath)) return;

            if (!_isForced)
            {
                var result = MessageBox.Show(
                    "O aplicativo será encerrado para instalar a atualização.\n" +
                    "Ele reiniciará automaticamente após a instalação.\n\n" +
                    "Deseja continuar?",
                    "Instalar Atualização",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes) return;
            }

            bool launched = _updateService.InstallUpdate(state.DownloadedFilePath);

            if (launched)
            {
                DidStartUpdate = true;
                Application.Current.Shutdown();
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            _updateService.CancelDownload();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        // ═══════════════════════════════════════════════════════════
        // UI UPDATES
        // ═══════════════════════════════════════════════════════════

        private void OnUpdateStateChanged(UpdateState state)
        {
            Dispatcher.Invoke(() =>
            {
                // Status message
                lblStatusMessage.Text = state.StatusMessage;

                // Icon
                if (state.HasError)
                {
                    lblStatusIcon.Text = "❌";
                    lblStatusMessage.Foreground = (SolidColorBrush)FindResource("AccentRed");
                }
                else if (state.IsReady)
                {
                    lblStatusIcon.Text = "✅";
                    lblStatusMessage.Foreground = (SolidColorBrush)FindResource("AccentGreen");
                }
                else if (state.IsDownloading)
                {
                    lblStatusIcon.Text = "⬇️";
                    lblStatusMessage.Foreground = (SolidColorBrush)FindResource("TextPrimary");
                }
                else if (state.IsChecking)
                {
                    lblStatusIcon.Text = "🔍";
                    lblStatusMessage.Foreground = (SolidColorBrush)FindResource("TextPrimary");
                }
                else if (state.HasUpdate)
                {
                    lblStatusIcon.Text = "🆕";
                    lblStatusMessage.Foreground = (SolidColorBrush)FindResource("AccentGold");
                }
                else
                {
                    lblStatusIcon.Text = "✅";
                    lblStatusMessage.Foreground = (SolidColorBrush)FindResource("AccentGreen");
                }

                // Progress bar
                if (state.IsDownloading)
                {
                    pnlProgress.Visibility = Visibility.Visible;

                    double percent = state.DownloadProgress * 100;
                    lblProgressText.Text = $"{percent:F0}%";

                    if (state.TotalBytes > 0)
                    {
                        lblProgressSize.Text =
                            $"{state.DownloadedBytes / 1048576.0:F1} MB / {state.TotalBytes / 1048576.0:F1} MB";
                    }

                    // Animate progress bar width
                    double maxWidth = cardStatus.ActualWidth - 42; // padding
                    if (maxWidth > 0)
                    {
                        progressBar.Width = maxWidth * state.DownloadProgress;
                    }
                }
                else if (state.IsReady)
                {
                    lblProgressText.Text = "100%";
                    double maxWidth = cardStatus.ActualWidth - 42;
                    if (maxWidth > 0) progressBar.Width = maxWidth;
                }
            });
        }

        private void ShowUpdateAvailable(UpdateInfo update)
        {
            cardUpdateInfo.Visibility = Visibility.Visible;
            btnDownload.Visibility = Visibility.Visible;
            btnDownload.IsEnabled = true;

            // Version
            lblNewVersion.Text = $"v{update.Version}";

            // Severity badge
            switch (update.Severity?.ToLower())
            {
                case "critical":
                    lblSeverity.Text = "CRÍTICA";
                    badgeSeverity.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7F1D1D"));
                    lblSeverity.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FCA5A5"));
                    break;
                case "recommended":
                    lblSeverity.Text = "RECOMENDADA";
                    badgeSeverity.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E3A5F"));
                    lblSeverity.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#93C5FD"));
                    break;
                default:
                    lblSeverity.Text = "OPCIONAL";
                    badgeSeverity.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#374151"));
                    lblSeverity.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9CA3AF"));
                    break;
            }

            // Release date
            if (DateTime.TryParse(update.ReleaseDate, out var date))
                lblReleaseDate.Text = date.ToString("dd/MM/yyyy");
            else
                lblReleaseDate.Text = update.ReleaseDate;

            // File size
            lblFileSize.Text = update.FileSizeMb > 0
                ? $"{update.FileSizeMb:F1} MB"
                : "Desconhecido";

            // Changelog
            changelogList.Children.Clear();
            if (update.Changelog != null)
            {
                foreach (var entry in update.Changelog)
                {
                    var item = new TextBlock
                    {
                        Text = $"• {entry}",
                        FontSize = 11,
                        Foreground = (SolidColorBrush)FindResource("TextPrimary"),
                        Margin = new Thickness(0, 2, 0, 2),
                        TextWrapping = TextWrapping.Wrap
                    };
                    changelogList.Children.Add(item);
                }
            }

            // Status
            lblStatusIcon.Text = "🆕";
            lblStatusMessage.Text = $"Nova versão disponível: v{update.Version}";
            lblStatusMessage.Foreground = (SolidColorBrush)FindResource("AccentGold");
        }
    }
}
