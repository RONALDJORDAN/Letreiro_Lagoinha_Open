using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using LetreiroDigital.ViewModels;

namespace LetreiroDigital.Views
{
    public partial class KeyboardShortcutsWindow : Window
    {
        private readonly AppViewModel _vm;
        private Dictionary<string, string> _tempBindings;
        
        // Maps the internal binding key to a friendly display name
        private readonly Dictionary<string, string> _actionNames = new()
        {
            { "Ação_IniciarApresentacao", "Iniciar (Modo TV)" },
            { "Ação_PararApresentacao", "Parar / Sair do Fullscreen" },
            { "Ação_ProximoItem", "Avançar para Próximo Item" },
            { "Ação_ItemAnterior", "Voltar para Item Anterior" },
            { "Ação_ModoTelaCheia", "Alternar Modo Tela Cheia" },
            { "Ação_PausarTimer", "Pausar / Retomar Timer" },
            { "Ação_ResetarTimer", "Zerar Timer" },
            { "Ação_Adicionar1Min", "Adicionar 1 Minuto no Timer" },
            { "Ação_Subtrair1Min", "Subtrair 1 Minuto no Timer" },
            { "Ação_ModoProjecao", "Mudar para Modo Projeção" },
            { "Ação_ModoTv", "Mudar para Modo TV" },
            { "Ação_MostrarOcultarRelogio", "Mostrar / Ocultar Relógio Avulso" }
        };

        private string? _listeningAction = null;
        private Button? _activeButton = null;

        public KeyboardShortcutsWindow(AppViewModel vm)
        {
            InitializeComponent();
            _vm = vm;
            
            // Create a working copy so we can cancel without saving
            _tempBindings = _vm.KeyBindings != null 
                ? new Dictionary<string, string>(_vm.KeyBindings) 
                : new Dictionary<string, string>(AppViewModel.DefaultKeyBindings);

            BuildUI();
        }

        private void BuildUI()
        {
            ShortcutsPanel.Children.Clear();

            foreach (var action in _actionNames)
            {
                var grid = new Grid { Margin = new Thickness(0, 5, 0, 5) };
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(300) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                var lblAction = new TextBlock
                {
                    Text = action.Value,
                    Foreground = Brushes.White,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = 14
                };
                Grid.SetColumn(lblAction, 0);

                string currentKey = _tempBindings.ContainsKey(action.Key) ? _tempBindings[action.Key] : "Nenhum";

                var btnKey = new Button
                {
                    Content = currentKey,
                    Tag = action.Key,
                    Style = (Style)FindResource("ActionButton"),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Height = 35
                };
                
                btnKey.Click += BtnKey_Click;
                
                Grid.SetColumn(btnKey, 1);

                grid.Children.Add(lblAction);
                grid.Children.Add(btnKey);
                ShortcutsPanel.Children.Add(grid);
            }
        }

        private void BtnKey_Click(object sender, RoutedEventArgs e)
        {
            // Reset previous button if any
            if (_activeButton != null && _listeningAction != null)
            {
                _activeButton.Content = _tempBindings.ContainsKey(_listeningAction) ? _tempBindings[_listeningAction] : "Nenhum";
            }

            _activeButton = sender as Button;
            if (_activeButton == null || _activeButton.Tag == null) return;

            _listeningAction = _activeButton.Tag.ToString();
            _activeButton.Content = "Aperte a nova tecla...";
            _activeButton.Focus(); // Ensure it has focus to catch KeyDown
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (_listeningAction == null || _activeButton == null) return;

            e.Handled = true;

            // Ignore modifier keys by themselves
            if (e.Key == Key.LeftShift || e.Key == Key.RightShift || 
                e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl || 
                e.Key == Key.LeftAlt || e.Key == Key.RightAlt || 
                e.Key == Key.System)
            {
                return; 
            }

            string newKey = e.Key.ToString();

            // Allow Backspace to clear a binding
            if (e.Key == Key.Back)
            {
                newKey = "Nenhum";
                _tempBindings.Remove(_listeningAction);
            }
            else
            {
                // Check if key is already in use by another action
                var existing = _tempBindings.FirstOrDefault(kb => kb.Value == newKey);
                if (existing.Key != null && existing.Key != _listeningAction)
                {
                    MessageBox.Show($"A tecla '{newKey}' já está sendo usada para: {_actionNames[existing.Key]}.\nEscolha outra tecla.", "Conflito de Atalho", MessageBoxButton.OK, MessageBoxImage.Warning);
                    
                    // Restore text
                    _activeButton.Content = _tempBindings.ContainsKey(_listeningAction) ? _tempBindings[_listeningAction] : "Nenhum";
                    _listeningAction = null;
                    _activeButton = null;
                    return;
                }

                _tempBindings[_listeningAction] = newKey;
            }

            _activeButton.Content = newKey;
            
            // Stop listening
            _listeningAction = null;
            _activeButton = null;
        }

        private void BtnRestoreDefaults_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Deseja realmente restaurar todos os atalhos para o padrão de fábrica?", "Atenção", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                _tempBindings = new Dictionary<string, string>(AppViewModel.DefaultKeyBindings);
                BuildUI();
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            _vm.KeyBindings = new Dictionary<string, string>(_tempBindings);
            _vm.SaveConfig();
            
            MessageBox.Show("Atalhos salvos com sucesso!", "Salvo", MessageBoxButton.OK, MessageBoxImage.Information);
            Close();
        }
    }
}
