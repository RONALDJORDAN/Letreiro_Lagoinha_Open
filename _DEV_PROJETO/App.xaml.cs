using System;
using System.Windows;
using System.Windows.Threading;
using LetreiroDigital.ViewModels;
using LetreiroDigital.Views;

namespace LetreiroDigital
{
    public partial class App : Application
    {
        private AppViewModel _vm = null!;
        private ControlWindow _controlWindow = null!;
        private BannerWindow? _bannerWindow;
        private ClockWindow? _clockWindow;
        private ScheduleWindow? _scheduleWindow;
        private MiniControllerWindow? _miniController;
        private TvModeWindow? _tvModeWindow;

        private readonly DispatcherTimer _syncTimer;

        public App()
        {
            _syncTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            _syncTimer.Tick += SyncWindows;
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            _vm = new AppViewModel();
            
            // Configura eventos de visibilidade
            _vm.VisibilityChanged += () => Dispatcher.Invoke(ApplyVisibility);
            _vm.StateChanged += () => Dispatcher.Invoke(UpdateWindowPositions);
            
            _vm.Initialize();

            // ── VERIFICAÇÃO DE PRIMEIRA EXECUÇÃO (BOAS-VINDAS) ──
            string configDir = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "LetreiroDigital", "config");
            string firstRunFile = System.IO.Path.Combine(configDir, "first_run.json");

            if (!System.IO.File.Exists(firstRunFile))
            {
                var welcome = new WelcomeWindow();
                if (welcome.ShowDialog() != true)
                {
                    // Se o usuário fechar a janela no "X", não abre o painel.
                    return;
                }
            }

            // ── VERIFICAÇÃO DE ATUALIZAÇÃO OBRIGATÓRIA (ANTES de abrir o app) ──
            try
            {
                var updateService = new Services.UpdateService();
                var update = await updateService.CheckSilentlyAsync();

                if (update != null && update.Required)
                {
                    // Abre o Update Center mas permite que o usuário feche/negue
                    var updateWindow = new Views.UpdateCenterWindow(update, forceUpdate: true);
                    updateWindow.ShowDialog();

                    // Se o processo de instalação do ZIP iniciou, o DidStartUpdate será true
                    // e o Application.Current.Shutdown() já terá sido chamado dentro da janela.
                    // Caso o usuário apenas fechou a janela, continuamos para o app.
                    if (updateWindow.DidStartUpdate) return;
                }
            }
            catch { /* Se falhar a checagem, permite o uso normal */ }
            
            _controlWindow = new ControlWindow();
            _controlWindow.SetViewModel(_vm);
            _controlWindow.Show();
            MainWindow = _controlWindow;

            _syncTimer.Start();

            // ── AUTO-UPDATE: Verificação silenciosa (atualizações opcionais) ────
            _ = CheckForUpdateOnStartupAsync();
        }

        private void ApplyVisibility()
        {
            // Banner / TV Mode
            if (_vm.ShowBanner)
            {
                if (_vm.IsTvMode)
                {
                    // TV Mode: show fullscreen TV window
                    _bannerWindow?.Hide();
                    if (_tvModeWindow == null || !_tvModeWindow.IsLoaded)
                    {
                        _tvModeWindow = new TvModeWindow();
                        _tvModeWindow.SetViewModel(_vm);
                        _tvModeWindow.Closed += (s, e) => _tvModeWindow = null;
                    }
                    _tvModeWindow.UpdatePosition();
                    _tvModeWindow.Show();
                }
                else
                {
                    // Projection Mode: show normal scrolling banner
                    _tvModeWindow?.Hide();
                    if (_bannerWindow == null || !_bannerWindow.IsLoaded)
                    {
                        _bannerWindow = new BannerWindow();
                        _bannerWindow.SetViewModel(_vm);
                        _bannerWindow.Closed += (s, e) => _bannerWindow = null;
                    }
                    _bannerWindow.UpdatePosition();
                    _bannerWindow.Show();
                }
            }
            else
            {
                _bannerWindow?.Hide();
                _tvModeWindow?.Hide();
            }

            // Clock
            if (_vm.ShowClock || _vm.ShowTimer)
            {
                if (_clockWindow == null || !_clockWindow.IsLoaded)
                {
                    _clockWindow = new ClockWindow();
                    _clockWindow.SetViewModel(_vm);
                    _clockWindow.Closed += (s, e) => _clockWindow = null;
                }
                _clockWindow.UpdatePosition();
                _clockWindow.Show();
            }
            else
            {
                _clockWindow?.Hide();
            }

            // Schedule
            if (_vm.ShowSchedule)
            {
                if (_scheduleWindow == null || !_scheduleWindow.IsLoaded)
                {
                    _scheduleWindow = new ScheduleWindow();
                    _scheduleWindow.SetViewModel(_vm);
                    _scheduleWindow.Closed += (s, e) => _scheduleWindow = null;
                }
                _scheduleWindow.UpdatePosition();
                _scheduleWindow.Show();
            }
            else
            {
                _scheduleWindow?.Hide();
            }
        }

        private void UpdateWindowPositions()
        {
            _bannerWindow?.UpdatePosition();
            _clockWindow?.UpdatePosition();
            _scheduleWindow?.UpdatePosition();
        }

        public void OpenMiniController()
        {
            if (_miniController == null || !_miniController.IsLoaded)
            {
                _miniController = new MiniControllerWindow();
                _miniController.SetViewModel(_vm);
                _miniController.Closed += (s, e) => _miniController = null;
            }
            _miniController.Show();
            _miniController.Activate();
        }

        private void SyncWindows(object? sender, EventArgs e)
        {
            // Periodic sync — keeps positions updated when dragged from preview
            if (_vm.ShowBanner)
            {
                if (_vm.IsTvMode)
                    _tvModeWindow?.UpdatePosition();
                else
                    _bannerWindow?.UpdatePosition();
            }
            if (_vm.ShowClock || _vm.ShowTimer) _clockWindow?.UpdatePosition();
            if (_vm.ShowSchedule) _scheduleWindow?.UpdatePosition();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _syncTimer.Stop();
            _vm?.SaveConfig(); // Bug #4 fix: null-conditional evita crash se app fechar antes da licença
            _bannerWindow?.Close();
            _tvModeWindow?.Close();
            _clockWindow?.Close();
            _scheduleWindow?.Close();
            _miniController?.Close();
            base.OnExit(e);
        }

        /// <summary>
        /// Verifica silenciosamente se há atualizações disponíveis
        /// 5 segundos após o app iniciar (não bloqueia a UI).
        /// </summary>
        private async System.Threading.Tasks.Task CheckForUpdateOnStartupAsync()
        {
            try
            {
                // Espera o app carregar completamente antes de verificar
                await System.Threading.Tasks.Task.Delay(5000);

                var updateService = new Services.UpdateService();
                var update = await updateService.CheckSilentlyAsync();

                if (update != null)
                {
                    // Roda na thread UI
                    await Dispatcher.InvokeAsync(() =>
                    {
                        string severity = update.Severity?.ToLower() switch
                        {
                            "critical" => "⚠️ CRÍTICA",
                            "recommended" => "📦 Recomendada",
                            _ => "ℹ️ Opcional"
                        };

                        var result = MessageBox.Show(
                            $"Uma nova versão está disponível!\n\n" +
                            $"Versão atual: v{Services.UpdateService.CurrentVersion}\n" +
                            $"Nova versão: v{update.Version} ({severity})\n\n" +
                            "Deseja abrir a Central de Atualizações?",
                            "Atualização Disponível",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Information);

                        if (result == MessageBoxResult.Yes)
                        {
                            var updateWindow = new Views.UpdateCenterWindow(update);
                            updateWindow.Owner = _controlWindow;
                            updateWindow.ShowDialog();
                        }
                    });
                }
            }
            catch { /* Silencioso — se falhar, não mostra nada */ }
        }
    }
}
