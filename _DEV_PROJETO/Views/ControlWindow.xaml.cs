using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using LetreiroDigital.Models;
using LetreiroDigital.ViewModels;
using LetreiroDigital.Services;
using Microsoft.Win32;

namespace LetreiroDigital.Views
{
    public partial class ControlWindow : Window
    {
        private AppViewModel _vm = null!;
        private bool _suppressSliderEvents;
        private bool _isFullscreen = false;
        private bool _isDragging = false;
        private Point _dragStartPoint;
        private int _draggedIndex = -1;
        private Border? _ghostCard;
        private Point _ghostOffset;
        private readonly System.Windows.Threading.DispatcherTimer _clockTimer;

        public void SetViewModel(AppViewModel vm)
        {
            _vm = vm;
            DataContext = vm;
            tvPreview.SetViewModel(_vm);
            _vm.StateChanged += () => Dispatcher.Invoke(RefreshUI);
            _vm.VisibilityChanged += () => Dispatcher.Invoke(RefreshUI);
            _vm.ItemSelected += () => Dispatcher.Invoke(RefreshUI);
            _vm.ScheduleUpdated += () => Dispatcher.Invoke(() => { RenderScheduleList(); });
            _vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(_vm.TimerFormatted) ||
                    e.PropertyName == nameof(_vm.TimerRunning) ||
                    e.PropertyName == nameof(_vm.BlinkPhase) ||
                    e.PropertyName == nameof(_vm.BlinkState))
                {
                    Dispatcher.Invoke(UpdateTimerDisplay);
                }
            };
            RefreshUI();
            _ = CheckStatusAsync();
        }

        public ControlWindow()
        {
            InitializeComponent();
            _clockTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _clockTimer.Tick += (s, e) => { if (lblClockPanel != null) lblClockPanel.Text = DateTime.Now.ToString("HH:mm:ss"); };
            _clockTimer.Start();
        }

        // ==================== STATUS CHECKS ====================
        private async Task CheckStatusAsync()
        {
            await CheckConnectionStatusAsync();
        }

        private async Task CheckConnectionStatusAsync()
        {
            try
            {
                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                var response = await http.GetAsync("https://www.google.com/generate_204");
                bool online = response.IsSuccessStatusCode;

                Dispatcher.Invoke(() =>
                {
                    if (online)
                    {
                        dotConnection.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
                        lblConnectionStatus.Text = "Online";
                        lblConnectionStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
                    }
                    else
                    {
                        dotConnection.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F44336"));
                        lblConnectionStatus.Text = "Offline";
                        lblConnectionStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F44336"));
                    }
                });
            }
            catch
            {
                Dispatcher.Invoke(() =>
                {
                    dotConnection.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F44336"));
                    lblConnectionStatus.Text = "Offline";
                    lblConnectionStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F44336"));
                });
            }
        }

        // ==================== REFRESH ALL UI ====================
        private void RefreshUI()
        {
            _suppressSliderEvents = true;

            // Active item
            if (_vm.CurrentItem != null)
            {
                lblActiveIdx.Text = $"#{(_vm.CurrentItemIndex ?? 0) + 1}";
                lblActiveName.Text = _vm.CurrentItem.Content;
            }
            else
            {
                lblActiveIdx.Text = "--";
                lblActiveName.Text = "Nenhum item selecionado";
            }

            // Timer
            UpdateTimerDisplay();



            // TV Mode Granular Sliders
            sliderTvEvent.Value = _vm.TvEventSize;
            sliderTvList.Value = _vm.TvListSize;
            sliderTvTimer.Value = _vm.TvTimerSize;
            sliderTvClock.Value = _vm.TvClockSize;
            sliderTvFooter.Value = _vm.TvFooterSize;

            // Warning feature controls
            chkEnableWarning.IsChecked = _vm.EnableWarning;
            sliderWarningSecs.Value = _vm.WarningSeconds;
            if (lblWarningSecsValue != null)
                lblWarningSecsValue.Text = $"{_vm.WarningSeconds}s";

            // Mode buttons
            // UpdateModeButtons(); // Removed
            // UpdateDayButtons(); // Handled by XAML Binding

            // Monitors — rebuild list if empty, always sync selected index
            if (cboMonitor.Items.Count == 0 && _vm.Monitors.Count > 0)
            {
                foreach (var m in _vm.Monitors)
                    cboMonitor.Items.Add(m.Label);
            }
            // Always sync the combo selection to match the VM's current monitor
            if (cboMonitor.Items.Count > 0)
            {
                int targetIdx = _vm.SelectedMonitorIndex;
                if (targetIdx >= 0 && targetIdx < cboMonitor.Items.Count
                    && cboMonitor.SelectedIndex != targetIdx)
                {
                    cboMonitor.SelectedIndex = targetIdx;
                }
            }

            SyncLayoutUI();

            // TV mode controls (Enforced)
            grpClockBanner.Header = "CONTROLES MODO TV";
            // settingsIndicator is already set in XAML or we can set it here if needed, but keeping it simple.
            lblSettingsTitle.Text = "CONFIGURAÇÕES";

            RenderScheduleList();

            _suppressSliderEvents = false;
        }

        // ==================== TIMER DISPLAY ====================
        private void UpdateTimerDisplay()
        {
            lblTimerBig.Text = _vm.TimerFormatted;
            if (_vm.BlinkPhase == BlinkPhase.Blink)
            {
                lblTimerBig.Foreground = _vm.BlinkState
                    ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF5555"))
                    : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00E676"));
            }

            else if (_vm.BlinkPhase == BlinkPhase.Red)
            {
                lblTimerBig.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF5555"));
            }
            else
            {
                lblTimerBig.Foreground = _vm.TimerRunning
                    ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00E676"))
                    : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#555555"));
            }

            // Glow Effect
            if (_vm.TimerRunning)
            {
                lblTimerBig.Effect = new System.Windows.Media.Effects.DropShadowEffect 
                { 
                    Color = _vm.BlinkPhase == BlinkPhase.Normal ? (Color)ColorConverter.ConvertFromString("#00E676") : (Color)ColorConverter.ConvertFromString("#FF5555"),
                    BlurRadius = 15, ShadowDepth = 0, Opacity = 0.6 
                };
            }
            else
            {
                lblTimerBig.Effect = null;
            }

            // Update Play/Pause Button State
            if (_vm.TimerRunning)
            {
               btnPlayPause.Content = "⏸️";
               btnPlayPause.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFD700")); // Gold
            }
            else
            {
               btnPlayPause.Content = "▶️"; 
               // Indicate paused state (if seconds > 0) vs stopped/reset
               if (_vm.TimerSvc.Seconds > 0) 
                    btnPlayPause.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF9800")); // Orange for Paused
               else
                    btnPlayPause.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50")); // Green for Start
            }
        }

        // ==================== SCHEDULE LIST ====================
        private void RenderScheduleList()
        {
            scheduleList.Children.Clear();
            var schedule = _vm.CurrentSchedule;
            for (int i = 0; i < schedule.Count; i++)
            {
                var item = schedule[i];
                int idx = i;
                bool isActive = _vm.CurrentItemIndex == i;

                var card = new Border
                {
                    Background = new SolidColorBrush(isActive ? (Color)ColorConverter.ConvertFromString("#333333") : (Color)ColorConverter.ConvertFromString("#252525")),
                    CornerRadius = new CornerRadius(4),
                    Margin = new Thickness(0, 0, 0, 3),
                    Cursor = Cursors.Hand,
                    BorderBrush = isActive ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50")) : null,
                    BorderThickness = isActive ? new Thickness(1) : new Thickness(0),
                };

                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(4) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                var bar = new Border
                {
                    Background = AppViewModel.BrushFromHex(item.Color),
                    CornerRadius = new CornerRadius(4, 0, 0, 4),
                };
                Grid.SetColumn(bar, 0);
                grid.Children.Add(bar);

                var content = new StackPanel { Margin = new Thickness(12, 6, 12, 6) };
                content.Children.Add(new TextBlock
                {
                    Text = $"{item.Time} ({item.Duration ?? "-"})",
                    FontSize = 9,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#AAAAAA")),
                });
                content.Children.Add(new TextBlock
                {
                    Text = item.Content,
                    FontSize = 11,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White,
                });
                Grid.SetColumn(content, 1);
                grid.Children.Add(content);

                card.Child = grid;
                card.Tag = idx;

                // Drag and Drop Events
                card.AllowDrop = true;
                card.PreviewMouseLeftButtonDown += Card_PreviewMouseLeftButtonDown;
                card.PreviewMouseMove += Card_PreviewMouseMove;
                card.DragOver += Card_DragOver;
                card.Drop += Card_Drop;

                card.MouseLeftButtonDown += (s, e) =>
                {
                    _vm.SelectItem(item, idx);
                };
                card.MouseRightButtonDown += (s, e) =>
                {
                    ShowContextMenu(item, idx, e.GetPosition(this));
                };

                scheduleList.Children.Add(card);

                // Premium Slide-In Animation
                var slideAnim = new System.Windows.Media.Animation.DoubleAnimation
                {
                    From = 15, To = 0,
                    Duration = TimeSpan.FromMilliseconds(250 + (Math.Min(i, 10) * 35)), 
                    EasingFunction = new System.Windows.Media.Animation.QuadraticEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }
                };
                var fadeAnim = new System.Windows.Media.Animation.DoubleAnimation
                {
                    From = 0, To = 1,
                    Duration = TimeSpan.FromMilliseconds(250 + (Math.Min(i, 10) * 35))
                };
                
                var trans = new TranslateTransform();
                card.RenderTransform = trans;
                trans.BeginAnimation(TranslateTransform.YProperty, slideAnim);
                card.BeginAnimation(UIElement.OpacityProperty, fadeAnim);
            }
        }

        // ==================== DRAG AND DROP ====================
        private void Card_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border card)
            {
                _dragStartPoint = e.GetPosition(null);
                _draggedIndex = (int)card.Tag;
            }
        }

        private void Card_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && !_isDragging)
            {
                Point position = e.GetPosition(this);
                if (Math.Abs(position.X - _dragStartPoint.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(position.Y - _dragStartPoint.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    StartFloatingDrag(sender as Border, position);
                }
            }
        }

        private void StartFloatingDrag(Border? card, Point mousePos)
        {
            if (card == null) return;
            _isDragging = true;
            _draggedIndex = (int)card.Tag;

            // Criar o "fantasma" flutuante do card
            var brush = new VisualBrush(card);
            _ghostCard = new Border
            {
                Width = card.ActualWidth,
                Height = card.ActualHeight,
                Background = brush,
                Opacity = 0.7,
                IsHitTestVisible = false,
                Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 15, Opacity = 0.4, ShadowDepth = 5 }
            };

            // Calcula o offset do mouse dentro do card
            var cardPos = card.TranslatePoint(new Point(0, 0), this);
            _ghostOffset = new Point(mousePos.X - cardPos.X, mousePos.Y - cardPos.Y);

            // Esconde o card original ou diminui opacidade
            card.Opacity = 0.3;

            dragCanvas.Children.Add(_ghostCard);
            UpdateGhostPosition(mousePos);

            // Captura o mouse para garantir que recebamos os movimentos
            this.CaptureMouse();
            this.MouseMove += Window_MouseMove_Drag;
            this.MouseLeftButtonUp += Window_MouseLeftButtonUp_Drag;
        }

        private void UpdateGhostPosition(Point mousePos)
        {
            if (_ghostCard == null) return;
            Canvas.SetLeft(_ghostCard, mousePos.X - _ghostOffset.X);
            Canvas.SetTop(_ghostCard, mousePos.Y - _ghostOffset.Y);
        }

        private void Window_MouseMove_Drag(object sender, MouseEventArgs e)
        {
            if (!_isDragging) return;
            var pos = e.GetPosition(this);
            UpdateGhostPosition(pos);
            
            // Opcional: Aqui poderíamos detectar o item sob o mouse para "abrir espaço"
        }

        private void Window_MouseLeftButtonUp_Drag(object sender, MouseButtonEventArgs e)
        {
            if (!_isDragging) return;
            
            this.ReleaseMouseCapture();
            this.MouseMove -= Window_MouseMove_Drag;
            this.MouseLeftButtonUp -= Window_MouseLeftButtonUp_Drag;

            _isDragging = false;
            dragCanvas.Children.Clear();
            _ghostCard = null;

            // Identifica onde soltou
            var pos = e.GetPosition(scheduleList);
            int newIndex = -1;
            
            // Percorre os itens da lista para achar onde o mouse está
            double currentY = 0;
            for (int i = 0; i < scheduleList.Children.Count; i++)
            {
                var child = (FrameworkElement)scheduleList.Children[i];
                if (pos.Y < currentY + child.ActualHeight / 2)
                {
                    newIndex = i;
                    break;
                }
                currentY += child.ActualHeight + 3; // +3 do Margin
            }
            
            if (newIndex == -1) newIndex = scheduleList.Children.Count - 1;

            if (_draggedIndex != newIndex)
            {
                _vm.ReorderScheduleItem(_draggedIndex, newIndex);
            }
            else
            {
                // Se não mudou, apenas restaura a opacidade (RenderScheduleList será chamado se mudar)
                RenderScheduleList();
            }
        }

        private void Card_DragOver(object sender, DragEventArgs e)
        {
            // Não usado no modo customizado, mas mantido para compatibilidade se necessário
        }

        private void Card_Drop(object sender, DragEventArgs e)
        {
            // Não usado no modo customizado
        }

        // ==================== CONTEXT MENU ====================
        private void ShowContextMenu(ScheduleItem item, int index, Point pos)
        {
            var menu = new ContextMenu();
            var editItem = new MenuItem { Header = "✏️ Editar" };
            editItem.Click += (s, e) => OpenEditor(index, item);
            menu.Items.Add(editItem);

            var delItem = new MenuItem { Header = "🗑️ Excluir" };
            delItem.Click += (s, e) => _vm.DeleteItem(index);
            menu.Items.Add(delItem);

            menu.IsOpen = true;
        }

        // ==================== EDITOR ====================
        private void OpenEditor(int? index = null, ScheduleItem? item = null)
        {
            var dlg = new EditorDialog(item);
            dlg.Owner = this;
            if (dlg.ShowDialog() == true)
            {
                var newItem = dlg.ResultItem;
                var schedule = _vm.CurrentSchedule;
                if (index.HasValue)
                    schedule[index.Value] = newItem;
                else
                    schedule.Add(newItem);

                _vm.UpdateScheduleDay(_vm.CurrentDay, schedule);
            }
        }

        // ==================== MODE BUTTONS ====================
        private void UpdateModeButtons()
        {
            // Removed
        }

        private void UpdateDayButtons()
        {
            // Removed - Handled by XAML DataBinding
        }

        // ==================== EVENT HANDLERS ==================== // Bug #10 fix: comentário duplicado removido
        /* ModeBtn_Click Removed */

        private void DayBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string day)
                _vm.ChangeDay(day);
        }

        private void BtnAddTab_Click(object sender, RoutedEventArgs e)
        {
            var name = ShowInputDialog("Nova Aba", "Nome da aba/evento:");
            if (!string.IsNullOrWhiteSpace(name))
            {
                _vm.AddScheduleTab(name);
            }
        }

        private void MenuRenameTab_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mi && mi.Tag is string oldName)
            {
                var newName = ShowInputDialog("Renomear Aba", "Novo nome:", oldName);
                if (!string.IsNullOrWhiteSpace(newName) && newName != oldName)
                {
                    _vm.RenameScheduleTab(oldName, newName);
                }
            }
        }

        private void MenuDeleteTab_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mi && mi.Tag is string name)
            {
                if (MessageBox.Show($"Deseja excluir a aba '{name}' e todos os seus itens?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    _vm.RemoveScheduleTab(name);
                }
            }
        }

        private string? ShowInputDialog(string title, string prompt, string defaultText = "")
        {
            var dialog = new Window
            {
                Title = title,
                Width = 350,
                Height = 180,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E1E1E")),
                ResizeMode = ResizeMode.NoResize
            };

            var stack = new StackPanel { Margin = new Thickness(20) };
            stack.Children.Add(new TextBlock
            {
                Text = prompt,
                Foreground = Brushes.White,
                FontSize = 13,
                Margin = new Thickness(0, 0, 0, 8)
            });

            var txtName = new TextBox
            {
                Text = defaultText,
                FontSize = 14,
                Padding = new Thickness(8, 5, 8, 5),
                Margin = new Thickness(0, 0, 0, 15)
            };
            stack.Children.Add(txtName);
            txtName.SelectAll();

            var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var btnOk = new Button
            {
                Content = "OK",
                Padding = new Thickness(20, 6, 20, 6),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50")),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 10, 0),
                IsDefault = true
            };
            
            string? result = null;
            btnOk.Click += (s, ev) => { result = txtName.Text; dialog.Close(); };
            
            var btnCancel = new Button
            {
                Content = "Cancelar",
                Padding = new Thickness(20, 6, 20, 6),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#666")),
                Foreground = Brushes.White,
                IsCancel = true
            };
            btnCancel.Click += (s, ev) => dialog.Close();

            btnPanel.Children.Add(btnOk);
            btnPanel.Children.Add(btnCancel);
            stack.Children.Add(btnPanel);

            dialog.Content = stack;
            dialog.ShowDialog();
            return result;
        }





        private void SliderTvEvent_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_suppressSliderEvents || _vm == null) return;
            _vm.TvEventSize = (int)sliderTvEvent.Value;
        }

        private void SliderTvList_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_suppressSliderEvents || _vm == null) return;
            _vm.TvListSize = (int)sliderTvList.Value;
        }

        private void SliderTvTimer_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_suppressSliderEvents || _vm == null) return;
            _vm.TvTimerSize = (int)sliderTvTimer.Value;
        }

        private void SliderTvClock_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_suppressSliderEvents || _vm == null) return;
            _vm.TvClockSize = (int)sliderTvClock.Value;
        }

        private void SliderTvFooter_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_suppressSliderEvents || _vm == null) return;
            _vm.TvFooterSize = (int)sliderTvFooter.Value;
        }

        private void ChkEnableWarning_Changed(object sender, RoutedEventArgs e)
        {
            if (_suppressSliderEvents || _vm == null) return;
            _vm.EnableWarning = chkEnableWarning.IsChecked == true;
            _vm.SaveConfig();
        }

        private void SliderWarningSecs_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_suppressSliderEvents || _vm == null) return;
            _vm.WarningSeconds = (int)sliderWarningSecs.Value;
            if (lblWarningSecsValue != null)
                lblWarningSecsValue.Text = $"{_vm.WarningSeconds}s";
            _vm.SaveConfig();
        }



        private void MonitorSelect_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_vm == null || cboMonitor.SelectedIndex < 0) return;
            _vm.ChangeMonitor(cboMonitor.SelectedIndex);
        }



        private void BtnAdd_Click(object sender, RoutedEventArgs e) => OpenEditor();

        private void BtnResetSchedule_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Restaurar cronograma padrão para todos os dias?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                _vm.ResetSchedule();
        }

        private void BtnMiniController_Click(object sender, RoutedEventArgs e)
        {
            ((App)Application.Current).OpenMiniController();
        }

        // ==================== PLAYBACK CONTROLS ====================
        private void BtnPrevItem_Click(object sender, RoutedEventArgs e)
        {
            _vm.PrevItem();
        }

        private void BtnNextItem_Click(object sender, RoutedEventArgs e)
        {
            _vm.AdvanceToNextItem();
        }

        private void BtnPlayPause_Click(object sender, RoutedEventArgs e)
        {
            _vm.ToggleTimerPause();
        }

        private void BtnAddMin_Click(object sender, RoutedEventArgs e)
        {
            _vm.TimerSvc.AddSeconds(60);
        }

        private void BtnSubMin_Click(object sender, RoutedEventArgs e)
        {
            _vm.TimerSvc.AddSeconds(-60);
        }

        private void BtnResetTimer_Click(object sender, RoutedEventArgs e)
        {
            _vm.TimerSvc.Reset();
        }

        private void SliderProgress_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            _vm.IsUserSeeking = true;
        }

        private void SliderProgress_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            _vm.IsUserSeeking = false;
        }

        private void BtnQuit_Click(object sender, RoutedEventArgs e)
        {
            if (_vm.HasUnsavedChanges)
            {
                var result = MessageBox.Show("Existem alterações não salvas no cronograma. Deseja salvar antes de sair?", "Alterações Pendentes", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Cancel) return;
                
                if (result == MessageBoxResult.Yes)
                {
                    _vm.SaveSchedule();
                }
            }
            else
            {
                if (MessageBox.Show("Deseja realmente encerrar a aplicação?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
                    return;
            }

            _vm.EmergencyStop();
            _vm.SaveConfig();
            Application.Current.Shutdown();
        }

        private void ToggleControlFullscreen()
        {
            _isFullscreen = !_isFullscreen;
            if (_isFullscreen)
            {
                this.WindowStyle = WindowStyle.None;
                this.WindowState = WindowState.Maximized;
                MainMenu.Visibility = Visibility.Collapsed;
            }
            else
            {
                this.WindowStyle = WindowStyle.SingleBorderWindow;
                this.WindowState = WindowState.Normal;
                // Optional: restore maximized if you prefer, but normal is standard
                // this.WindowState = WindowState.Maximized;
                MainMenu.Visibility = Visibility.Visible;
            }
        }



        // ==================== KEYBOARD SHORTCUTS ====================
        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (_vm == null || _vm.KeyBindings == null) return;

            string keyStr = e.Key.ToString();

            // Ignore typing in textboxes
            if (e.OriginalSource is TextBox || e.OriginalSource is PasswordBox)
            {
                return;
            }

            // Find matching action
            var actionPair = _vm.KeyBindings.FirstOrDefault(kb => string.Equals(kb.Value, keyStr, StringComparison.OrdinalIgnoreCase));
            if (actionPair.Key == null) return;

            string action = actionPair.Key;
            bool handled = true;

            switch (action)
            {
                case "Ação_IniciarApresentacao":
                    _vm.ToggleTvMode();
                    break;
                case "Ação_PararApresentacao":
                    if (_isFullscreen)
                        ToggleControlFullscreen();
                    else
                        _vm.EmergencyStop();
                    break;
                case "Ação_ProximoItem":
                    _vm.AdvanceToNextItem();
                    break;
                case "Ação_ItemAnterior":
                    _vm.PrevItem();
                    break;
                case "Ação_ModoTelaCheia":
                    ToggleControlFullscreen();
                    break;
                case "Ação_PausarTimer":
                    _vm.ToggleTimerPause();
                    break;
                case "Ação_ResetarTimer":
                    _vm.TimerSvc.Reset();
                    break;
                case "Ação_Adicionar1Min":
                    _vm.TimerSvc.AddSeconds(60);
                    break;
                case "Ação_Subtrair1Min":
                    _vm.TimerSvc.AddSeconds(-60);
                    break;
                case "Ação_ModoProjecao":
                    _vm.SwitchMode("PROJECTION");
                    break;
                case "Ação_ModoTv":
                    _vm.SwitchMode("TV_MODE");
                    break;
                case "Ação_MostrarOcultarRelogio":
                    _vm.ShowClock = !_vm.ShowClock;
                    break;
                default:
                    handled = false;
                    break;
            }

            if (handled)
            {
                e.Handled = true;
            }
        }

        // ==================== MENU HANDLERS ====================
        
        // ARQUIVO
        private void MenuImportSchedule_Click(object sender, RoutedEventArgs e)
        {
            var openDlg = new OpenFileDialog
            {
                Filter = "Planilha Excel CSV (*.csv)|*.csv|Arquivo JSON (*.json)|*.json|Todos os arquivos (*.*)|*.*",
                Title = "Importar Cronograma"
            };

            if (openDlg.ShowDialog() == true)
            {
                try
                {
                    Dictionary<string, List<ScheduleItem>>? data = null;
                    if (openDlg.FileName.ToLower().EndsWith(".csv"))
                        data = _vm.DataSvc.ImportScheduleFromCsv(openDlg.FileName);
                    else
                        data = _vm.DataSvc.ImportSchedule(openDlg.FileName);

                    if (data != null && data.Count > 0)
                    {
                        if (MessageBox.Show("Deseja importar e adicionar os eventos ao seu cronograma atual?\nNovos eventos serão criados e itens de eventos existentes serão adicionados no final da lista.", "Confirmar Importação", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                        {
                            foreach (var kv in data)
                            {
                                // Se o evento (aba) ainda não existe, criamos a aba na UI
                                if (!_vm.WeeklySchedule.ContainsKey(kv.Key))
                                {
                                    _vm.AddScheduleTab(kv.Key);
                                    _vm.UpdateScheduleDay(kv.Key, kv.Value);
                                }
                                else
                                {
                                    // Se já existe, nós SOMAMOS (adicionamos) aos itens que já estão lá
                                    var currentItens = _vm.WeeklySchedule[kv.Key].ToList();
                                    currentItens.AddRange(kv.Value);
                                    _vm.UpdateScheduleDay(kv.Key, currentItens);
                                }
                            }
                            
                            _vm.SaveSchedule();
                            
                            // Força a atualização da UI para mostrar a última aba editada/criada
                            if (data.Count > 0)
                            {
                                _vm.ChangeDay(data.Keys.First());
                            }

                            MessageBox.Show("Cronograma importado e adicionado com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void MenuExportSchedule_Click(object sender, RoutedEventArgs e)
        {
            var saveDlg = new SaveFileDialog
            {
                Filter = "Planilha Excel CSV (*.csv)|*.csv|Arquivo JSON (*.json)|*.json",
                Title = "Exportar Cronograma",
                FileName = $"Cronograma_{DateTime.Now:yyyyMMdd_HHmm}.csv"
            };

            if (saveDlg.ShowDialog() == true)
            {
                try
                {
                    if (saveDlg.FileName.ToLower().EndsWith(".csv"))
                        _vm.DataSvc.ExportScheduleToCsv(saveDlg.FileName, _vm.WeeklySchedule);
                    else
                        _vm.DataSvc.ExportSchedule(saveDlg.FileName, _vm.WeeklySchedule);

                    MessageBox.Show("Cronograma exportado com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void MenuGeneralSettings_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new GeneralSettingsWindow(_vm)
            {
                Owner = this
            };
            settingsWindow.ShowDialog();
        }

        // TEMAS
        private void MenuThemeEditor_Click(object sender, RoutedEventArgs e)
        {
            var editor = new ThemeEditorWindow(_vm.GetCurrentTheme());
            if (editor.ShowDialog() == true && editor.SelectedTheme != null)
            {
                _vm.ApplyTheme(editor.SelectedTheme);
                _vm.SaveConfig(); // Proactive save after apply
                RefreshUI();
            }
        }

        private void MenuApplyTheme_Click(object sender, RoutedEventArgs e)
        {
            var themeManager = new Services.ThemeManager();
            var themes = themeManager.GetAllThemes();
            
            var dialog = new Window
            {
                Title = "Aplicar Tema",
                Width = 450,
                Height = 400,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E1E1E"))
            };

            var panel = new StackPanel { Margin = new Thickness(20) };

            panel.Children.Add(new TextBlock
            {
                Text = "SELECIONAR TEMA",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFD700")),
                Margin = new Thickness(0, 0, 0, 15)
            });

            var originalTheme = _vm.GetCurrentTheme(); // Bug #6 fix: salva tema original para restaurar ao cancelar

            var listBox = new ListBox
            {
                ItemsSource = themes.Select(t => t.Name),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D2D2D")),
                Foreground = Brushes.White,
                FontSize = 14,
                Height = 200,
                Margin = new Thickness(0, 0, 0, 15)
            };

            // Live preview on selection change
            listBox.SelectionChanged += (s, ev) =>
            {
                if (listBox.SelectedItem is string selectedName)
                {
                    var selectedTheme = themeManager.GetThemeByName(selectedName);
                    _vm.ApplyTheme(selectedTheme);
                    RefreshUI();
                }
            };

            var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };

            var btnApply = new Button
            {
                Content = "✅ Aplicar e Fechar",
                Padding = new Thickness(20, 10, 20, 10),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50")),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 10, 0)
            };
            btnApply.Click += (s, ev) => dialog.Close();

            var btnCancel = new Button
            {
                Content = "Cancelar",
                Padding = new Thickness(20, 10, 20, 10),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#666")),
                Foreground = Brushes.White
            };
            btnCancel.Click += (s, ev) =>
            {
                // Bug #6 fix: restaura o tema original ao cancelar (desfaz o live preview)
                _vm.ApplyTheme(originalTheme);
                RefreshUI();
                dialog.Close();
            };

            btnPanel.Children.Add(btnApply);
            btnPanel.Children.Add(btnCancel);

            panel.Children.Add(listBox);
            panel.Children.Add(btnPanel);
            dialog.Content = panel;
            
            dialog.ShowDialog();
        }

        private void MenuSaveCurrentTheme_Click(object sender, RoutedEventArgs e)
        {
            // Ask for theme name
            var inputDialog = new Window
            {
                Title = "Salvar Tema",
                Width = 350,
                Height = 180,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E1E1E"))
            };

            var stack = new StackPanel { Margin = new Thickness(20) };
            stack.Children.Add(new TextBlock
            {
                Text = "Nome do tema:",
                Foreground = Brushes.White,
                FontSize = 13,
                Margin = new Thickness(0, 0, 0, 8)
            });

            var txtName = new TextBox
            {
                Text = "Meu Tema",
                FontSize = 14,
                Padding = new Thickness(8, 5, 8, 5),
                Margin = new Thickness(0, 0, 0, 15)
            };
            stack.Children.Add(txtName);

            var btnSave = new Button
            {
                Content = "💾 Salvar",
                Padding = new Thickness(20, 10, 20, 10),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50")),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold
            };
            btnSave.Click += (s, ev) =>
            {
                if (string.IsNullOrWhiteSpace(txtName.Text))
                {
                    MessageBox.Show("Digite um nome para o tema.", "Atenção", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var theme = _vm.GetCurrentTheme();
                theme.Name = txtName.Text.Trim();

                var themeManager = new Services.ThemeManager();
                themeManager.SaveTheme(theme);

                MessageBox.Show($"Tema '{theme.Name}' salvo com sucesso!", "Salvo", MessageBoxButton.OK, MessageBoxImage.Information);
                inputDialog.Close();
            };
            stack.Children.Add(btnSave);

            inputDialog.Content = stack;
            inputDialog.ShowDialog();
        }

        private void MenuManageThemes_Click(object sender, RoutedEventArgs e)
        {
            // O novo editor WYSIWYG já possui gestor de temas integrado (Lista, Salvar, Excluir)
            // Abrimos ele diretamente conforme solicitado
            MenuThemeEditor_Click(sender, e);
        }

        // APRESENTAÇÃO

        private void MenuFullscreen_Click(object sender, RoutedEventArgs e)
        {
            ToggleControlFullscreen();
        }

        private void MenuPreviewWindow_Click(object sender, RoutedEventArgs e)
        {
            var preview = new PreviewWindow(_vm);
            preview.Owner = this;
            preview.Show();
        }

        // ATALHOS
        private void MenuKeyboardShortcuts_Click(object sender, RoutedEventArgs e)
        {
            var shortcutsWindow = new KeyboardShortcutsWindow(_vm);
            shortcutsWindow.Owner = this;
            shortcutsWindow.ShowDialog();
        }

        private void MenuResetShortcuts_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Deseja restaurar os atalhos para os valores padrão?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                MessageBox.Show("Atalhos restaurados!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void MenuShowShortcuts_Click(object sender, RoutedEventArgs e)
        {
            MenuKeyboardShortcuts_Click(sender, e);
        }



        // AJUDA
        private void MenuQuickTutorial_Click(object sender, RoutedEventArgs e)
        {
            var tutorial = @"TUTORIAL RÁPIDO - LETREIRO DIGITAL

1️⃣ ADICIONAR ITENS:
   - Use os botões + para adicionar Texto ou Vídeo
   - Configure o título e duração

2️⃣ CRIAR TEMAS:
   - Menu 'Temas' → 'Editor de Temas WYSIWYG'
   - Ajuste fontes, cores, sombras em tempo real
   - Salve com nome personalizado

3️⃣ TRANSIÇÕES:
   - Menu 'Apresentação' → 'Configurar Transições'
   - Defina fade in/out para troca suave

4️⃣ INICIAR:
   - F5 ou botões de modo (Projeção/TV)
   - ESC para parar emergência

5️⃣ ATALHOS:
   - F1 para ver lista completa
   - Menu 'Atalhos' para personalizar";

            MessageBox.Show(tutorial, "Tutorial Rápido", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void MenuAbout_Click(object sender, RoutedEventArgs e)
        {
            string about =
                "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
                "       LETREIRO DIGITAL LAGOINHA\n" +
                "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n\n" +
                $"Versão: {Services.UpdateService.CurrentVersion}\n" +
                $"Build: {Services.UpdateService.CurrentBuildNumber}\n\n" +
                $"Sistema: C# WPF (.NET 8)\n" +
                $"Monitores Detectados: {_vm.Monitors.Count}\n\n" +
                "Desenvolvido por CodAureo DevStudio\n" +
                "Última Atualização: Fev 2026\n\n" +
                "© 2026 — Todos os direitos reservados.";
            MessageBox.Show(about, "Sobre o Sistema", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        // ==================== REMOTE CONTROL ====================
        private void MenuStartWebServer_Click(object sender, RoutedEventArgs e)
        {
            // If custom password is set, use it without asking
            bool hasCustomPass = !string.IsNullOrEmpty(_vm.RemotePassword);
            bool useRandom = false;

            if (!hasCustomPass)
            {
                 var result = MessageBox.Show("Deseja gerar uma senha aleatória para o controle remoto?", "Segurança", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                 if (result == MessageBoxResult.Cancel) return;
                 useRandom = result == MessageBoxResult.Yes;
            }

            _vm.StartWebServer(useRandom);
            
            string msg = $"Servidor Web Iniciado!\nAcesse via navegador:\n{_vm.ServerUrl}";
            
            // Show password if random or custom
            if (!string.IsNullOrEmpty(_vm.WebServerSvc.Password))
            {
                msg += $"\n\nSenha de Acesso: {_vm.WebServerSvc.Password}";
            }
            
            MessageBox.Show(msg, "Servidor Online", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void MenuStopWebServer_Click(object sender, RoutedEventArgs e)
        {
            _vm.StopWebServer();
            MessageBox.Show("Servidor Web Parado.", "Servidor Offline", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void MenuShowQRCode_Click(object sender, RoutedEventArgs e)
        {
            if (!_vm.ServerRunning)
            {
                if (MessageBox.Show("O servidor não está rodando. Deseja iniciar agora?", "Servidor", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    MenuStartWebServer_Click(sender, e);
                    // Return here because MenuStartWebServer_Click shows a message box, 
                    // and we don't want to show the QR code immediately on top of it or if cancelled.
                    // But for better UX, let's just proceed if it started.
                    if (!_vm.ServerRunning) return;
                }
                else return;
            }

            var qrWindow = new QrCodeWindow(_vm.ServerUrl);
            qrWindow.Owner = this;
            qrWindow.ShowDialog();
        }

        private void MenuConfigureRemote_Click(object sender, RoutedEventArgs e)
        {
            var configWindow = new RemoteAccessWindow(_vm);
            configWindow.Owner = this;
            configWindow.ShowDialog();
        }
        
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            // Save state before closing
            _vm.SaveConfig();
            base.OnClosing(e);
        }
        
        // ==================== AJUDA ====================

        private void MenuUpdateCenter_Click(object sender, RoutedEventArgs e)
        {
            var updateWindow = new UpdateCenterWindow();
            updateWindow.Owner = this;
            updateWindow.ShowDialog();
        }


       // ==================== END OF CLASS ====================
        private void SyncLayoutUI()
        {
            if (_vm == null) return;
            if (_vm.TvLayoutMode == 2) rbLayout2.IsChecked = true;
            else rbLayout1.IsChecked = true;
        }

        private void LayoutRadio_Checked(object sender, RoutedEventArgs e)
        {
            if (_vm == null || _suppressSliderEvents) return;
            if (rbLayout1.IsChecked == true) _vm.TvLayoutMode = 1;
            else if (rbLayout2.IsChecked == true) _vm.TvLayoutMode = 2;
        }
        // ==================== USER THEMES (Operator UI) ====================
        private void MenuUserTheme_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem item && item.Tag is string themeName)
            {
                ApplyUserTheme(themeName);
            }
        }

        private void MenuSave_Click(object sender, RoutedEventArgs e) {}
        private void MenuReload_Click(object sender, RoutedEventArgs e) {}

        private void ApplyUserTheme(string themeName)
        {
            // Reset layout defaults
            colList.Width = new GridLength(340);
            colControls.Width = new GridLength(300);
            colPreview.Width = new GridLength(1, GridUnitType.Star);
            Grid.SetColumn(paneList, 0);
            Grid.SetColumn(paneControls, 2);
            paneControls.Visibility = Visibility.Visible;

            switch (themeName)
            {
                case "Cyberpunk":
                    // Restruturação: Preview Gigante + Barra Lateral Única
                    colControls.Width = new GridLength(0);
                    paneControls.Visibility = Visibility.Collapsed;
                    colList.Width = new GridLength(380);
                    
                    Resources["PanelBgBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#080808"));
                    Resources["SidebarBgBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#121212"));
                    Resources["ControlHeaderBgBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F2E63D")); // Amarelo Cyber
                    Resources["UserAccentBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00F2FF"));   // Ciano Neon
                    Resources["UserTextBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));
                    Resources["UserTextDimBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#AAAAAA"));
                    break;
                case "ModernBlue":
                    Resources["PanelBgBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#050A1A"));
                    Resources["SidebarBgBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0D1426"));
                    Resources["ControlHeaderBgBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#15203A"));
                    Resources["UserAccentBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3A86FF"));
                    Resources["UserTextBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));
                    break;
                case "Glass":
                    // Restruturação de Layout: Inverte lateral e controles
                    Grid.SetColumn(paneList, 2);
                    Grid.SetColumn(paneControls, 0);
                    colList.Width = new GridLength(300);
                    colControls.Width = new GridLength(340);
                    
                    Resources["PanelBgBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A1A1A"));
                    Resources["SidebarBgBrush"] = new SolidColorBrush(Color.FromArgb(180, 40, 40, 40));
                    Resources["ControlHeaderBgBrush"] = new SolidColorBrush(Color.FromArgb(200, 30, 30, 30));
                    Resources["UserAccentBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#BB86FC"));
                    Resources["UserTextBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F0F0F0"));
                    break;
                case "Emerald":
                    Resources["PanelBgBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#010A08"));
                    Resources["SidebarBgBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#021A15"));
                    Resources["ControlHeaderBgBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#042B23"));
                    Resources["UserAccentBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00E676"));
                    Resources["UserTextBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E8F5E9"));
                    break;
                default: // Standard Dark
                    Resources["PanelBgBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#121212"));
                    Resources["SidebarBgBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A1A1A"));
                    Resources["ControlHeaderBgBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D2D2D"));
                    Resources["UserAccentBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFD700"));
                    Resources["UserTextBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0"));
                    Resources["UserTextDimBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#888888"));
                    break;
            }
            
            // Refresh visuals
            RefreshUI();
            RenderScheduleList();
        }
    }
}
