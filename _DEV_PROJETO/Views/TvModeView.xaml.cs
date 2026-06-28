using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using LetreiroDigital.Models;
using LetreiroDigital.ViewModels;
using LetreiroDigital.Services;

namespace LetreiroDigital.Views
{
    public partial class TvModeView : UserControl
    {
        private AppViewModel _vm = null!;
        private readonly DispatcherTimer _clockTimer;

        // Code-behind colors for fallback/logic
        private readonly SolidColorBrush RedDim = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#330000"));
        private readonly SolidColorBrush RedBlink = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF4444"));

        // Bug #5 fix: Handlers armazenados para permitir desinscrição correta e evitar memory leaks
        private Action? _onItemSelectedHandler;
        private Action? _onStateChangedHandler;
        private Action? _onScheduleUpdatedHandler;
        private System.ComponentModel.PropertyChangedEventHandler? _onPropertyChangedHandler;
        private Action? _onWarningActivatedHandler;
        private Action? _onWarningDeactivatedHandler;

        // Warning overlay blink timer
        private readonly DispatcherTimer _warningBlinkTimer;
        private bool _warningBlinkState;

        public TvModeView()
        {
            InitializeComponent();
            
            _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _clockTimer.Tick += (s, e) => UpdateClock();
            _clockTimer.Start();

            _warningBlinkTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _warningBlinkTimer.Tick += (s, e) =>
            {
                _warningBlinkState = !_warningBlinkState;
                if (warningOverlay != null)
                    warningOverlay.Opacity = _warningBlinkState ? 1.0 : 0.5;
                // Update countdown label
                if (_vm != null && lblWarningCountdown != null && _vm.TimerSvc != null)
                {
                    int secs = _vm.TimerSvc.Seconds;
                    lblWarningCountdown.Text = secs > 0 ? $"Faltam {secs}s" : "";
                }
            };
            
            UpdateClock();
        }

        public void SetViewModel(AppViewModel vm)
        {
            // Bug #5 fix: Desinscreve do VM antigo antes de trocar (evita callbacks duplicados)
            if (_vm != null)
            {
                if (_onItemSelectedHandler != null) _vm.ItemSelected -= _onItemSelectedHandler;
                if (_onStateChangedHandler != null) _vm.StateChanged -= _onStateChangedHandler;
                if (_onScheduleUpdatedHandler != null) _vm.ScheduleUpdated -= _onScheduleUpdatedHandler;
                if (_onPropertyChangedHandler != null) _vm.PropertyChanged -= _onPropertyChangedHandler;
                if (_onWarningActivatedHandler != null) _vm.WarningActivated -= _onWarningActivatedHandler;
                if (_onWarningDeactivatedHandler != null) _vm.WarningDeactivated -= _onWarningDeactivatedHandler;
            }

            _vm = vm;

            // Cria e armazena referências dos handlers
            _onItemSelectedHandler = () => Dispatcher.Invoke(() => { HideWarningOverlay(); OnItemSelected(); });
            _onStateChangedHandler = () => Dispatcher.Invoke(UpdateDisplay);
            _onScheduleUpdatedHandler = () => Dispatcher.Invoke(RenderScheduleList);
            _onPropertyChangedHandler = (s, e) =>
            {
                if (e.PropertyName is nameof(AppViewModel.TvEventSize)
                    or nameof(AppViewModel.TvListSize)
                    or nameof(AppViewModel.TvTimerSize)
                    or nameof(AppViewModel.TvClockSize)
                    or nameof(AppViewModel.TvFooterSize)
                    or nameof(AppViewModel.TvHeadlineSize)
                    or nameof(AppViewModel.TvTickerSize))
                {
                    Dispatcher.Invoke(UpdateFontSizes);
                    Dispatcher.Invoke(RenderScheduleList);
                }
                if (e.PropertyName is nameof(AppViewModel.TitleFontFamily)
                    or nameof(AppViewModel.BodyFontFamily)
                    or nameof(AppViewModel.DigitalFontFamily)
                    or nameof(AppViewModel.FooterFontFamily)
                    or nameof(AppViewModel.TitleColor)
                    or nameof(AppViewModel.BodyColor))
                {
                    Dispatcher.Invoke(UpdateFontFamilies);
                    Dispatcher.Invoke(RenderScheduleList);
                }
                if (e.PropertyName is nameof(AppViewModel.TimerFormatted)
                    or nameof(AppViewModel.TimerRunning)
                    or nameof(AppViewModel.BlinkPhase)
                    or nameof(AppViewModel.BlinkState))
                {
                    Dispatcher.Invoke(UpdateTimerDisplay);
                }
                if (e.PropertyName is nameof(AppViewModel.ScheduleItemActiveColor)
                    or nameof(AppViewModel.ScheduleItemInactiveColor)
                    or nameof(AppViewModel.TvListSize))
                {
                    Dispatcher.Invoke(RenderScheduleList);
                }
            };

            // Warning overlay handlers
            _onWarningActivatedHandler = () => Dispatcher.Invoke(ShowWarningOverlay);
            _onWarningDeactivatedHandler = () => Dispatcher.Invoke(HideWarningOverlay);

            // Inscreve no novo VM
            _vm.ItemSelected += _onItemSelectedHandler;
            _vm.StateChanged += _onStateChangedHandler;
            _vm.ScheduleUpdated += _onScheduleUpdatedHandler;
            _vm.PropertyChanged += _onPropertyChangedHandler;
            _vm.WarningActivated += _onWarningActivatedHandler;
            _vm.WarningDeactivated += _onWarningDeactivatedHandler;

            DataContext = _vm; // Set DataContext for bindings

            UpdateDisplay();
            UpdateFontSizes();
            UpdateFontFamilies();
            RenderScheduleList();
        }

        // ==================== ITEM SELECTION ====================
        private void OnItemSelected()
        {
            UpdateDisplay();
            RenderScheduleList();
        }

        // ==================== DISPLAY UPDATE ====================
        private void UpdateDisplay()
        {
            // Switch layout panels
            bool isLayout2 = _vm?.TvLayoutMode == 2;
            if (mainGrid != null) mainGrid.Visibility = isLayout2 ? Visibility.Collapsed : Visibility.Visible;
            if (layout2Grid != null) layout2Grid.Visibility = isLayout2 ? Visibility.Visible : Visibility.Collapsed;

            if (_vm?.CurrentItem != null)
            {
                var item = _vm.CurrentItem;

                // Main content: Format like "APRESENTAÇÃO B – TEMA X"
                string mainText = item.Content.ToUpper();
                if (!string.IsNullOrWhiteSpace(item.Sigla) && item.Sigla.Length > 2)
                {
                    mainText = $"{item.Sigla.ToUpper()} – {item.Content.ToUpper()}";
                }

                lblMainContent.Text = mainText;
                if (layout2MainContent != null) layout2MainContent.Text = mainText;

                // Event time: parse "18:20 as 18:25" → "INÍCIO 18:20 — FIM 18:25"
                ParseAndDisplayEventTime(item.Time);
            }
            else
            {
                lblEventTimeRange.Text = "INÍCIO --:-- — FIM --:--";
                if (layout2TimeRange != null) layout2TimeRange.Text = "INÍCIO --:-- — FIM --:--";
            }

            PlayTransition();
        }

        private void PlayTransition()
        {
            if (_vm == null) return;
            
            double durationSeconds = _vm.TransitionDuration > 0 ? _vm.TransitionDuration : 0.6;
            var duration = new Duration(TimeSpan.FromSeconds(durationSeconds));

            // 1. Opacity Animation
            var fadeAnim = new DoubleAnimation { From = 0.0, To = 1.0, Duration = duration };
            
            // 2. Blur Animation
            var blurEffect = new BlurEffect { Radius = 15 };
            var blurAnim = new DoubleAnimation { From = 15, To = 0, Duration = duration, EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } };
            
            // 3. Scale Animation (Subtle Zoom)
            var scale = new ScaleTransform(1.1, 1.1);
            var scaleAnim = new DoubleAnimation { From = 1.1, To = 1.0, Duration = duration, EasingFunction = new ExponentialEase { EasingMode = EasingMode.EaseOut, Exponent = 5 } };

            // Apply to Main Content
            if (lblMainContent != null)
            {
                lblMainContent.Effect = blurEffect;
                lblMainContent.RenderTransformOrigin = new Point(0.5, 0.5);
                lblMainContent.RenderTransform = scale;
                
                lblMainContent.BeginAnimation(UIElement.OpacityProperty, fadeAnim);
                blurEffect.BeginAnimation(BlurEffect.RadiusProperty, blurAnim);
                scale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnim);
                scale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim);
            }

            // Apply to Layout 2
            if (layout2MainContent != null)
            {
                var blur2 = new BlurEffect { Radius = 15 };
                var scale2 = new ScaleTransform(1.1, 1.1);
                layout2MainContent.Effect = blur2;
                layout2MainContent.RenderTransformOrigin = new Point(0.5, 0.5);
                layout2MainContent.RenderTransform = scale2;

                layout2MainContent.BeginAnimation(UIElement.OpacityProperty, fadeAnim);
                blur2.BeginAnimation(BlurEffect.RadiusProperty, blurAnim);
                scale2.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnim);
                scale2.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim);
            }

            // Other elements just fade
            if (lblEventTimeHeader != null) lblEventTimeHeader.BeginAnimation(UIElement.OpacityProperty, fadeAnim);
            if (lblEventTimeRange != null) lblEventTimeRange.BeginAnimation(UIElement.OpacityProperty, fadeAnim);
            if (scheduleList != null) scheduleList.BeginAnimation(UIElement.OpacityProperty, fadeAnim);
            if (layout2TimeRange != null) layout2TimeRange.BeginAnimation(UIElement.OpacityProperty, fadeAnim);
        }

        private void ParseAndDisplayEventTime(string? timeStr)
        {
            string display;
            if (string.IsNullOrWhiteSpace(timeStr))
            {
                display = "INÍCIO --:-- — FIM --:--";
            }
            else if (timeStr.Contains(" as "))
            {
                var parts = timeStr.Split(" as ");
                display = parts.Length == 2
                    ? $"INÍCIO {parts[0].Trim()} — FIM {parts[1].Trim()}"
                    : $"HORÁRIO: {timeStr}";
            }
            else
            {
                display = $"HORÁRIO: {timeStr}";
            }

            lblEventTimeRange.Text = display;
            if (layout2TimeRange != null) layout2TimeRange.Text = display;
        }

        // ==================== FONT SIZES (Granular TV Mode Controls) ====================
        private void UpdateFontSizes()
        {
            if (_vm == null) return;
            
            // Use specific granular properties for each element
            lblMainContent.FontSize = Math.Clamp(_vm.TvEventSize, 40, 180);
            
            // LED displays use their own specific sizes
            lblTimer.FontSize = Math.Clamp(_vm.TvTimerSize, 50, 200);
            lblClock.FontSize = Math.Clamp(_vm.TvClockSize, 45, 180);
            lblEventTimeRange.FontSize = Math.Clamp(_vm.TvFooterSize, 20, 80);
        }

        // ==================== FONT FAMILIES (Live Update) ====================
        private void UpdateFontFamilies()
        {
            if (_vm == null) return;

            // Resolve font paths (embedded → ./Fonts/#Name, system → Name)
            var titlePath = AppViewModel.ResolveFontPath(_vm.TitleFontFamily);
            var bodyPath = AppViewModel.ResolveFontPath(_vm.BodyFontFamily);
            var digitalPath = AppViewModel.ResolveFontPath(_vm.DigitalFontFamily);
            var footerPath = AppViewModel.ResolveFontPath(_vm.FooterFontFamily);

            // Update XAML font resources
            var titleFont = new FontFamily($"{titlePath}, Impact, Arial Black");
            var bodyFont = new FontFamily($"{bodyPath}, Segoe UI, Arial");
            var digitalFont = new FontFamily($"{digitalPath}, Consolas, Courier New");
            var footerFont = new FontFamily($"{footerPath}, Consolas, Courier New");

            // Apply to UserControl Resources so all bindings update
            Resources["CondensedFont"] = titleFont;
            Resources["StandardFont"] = bodyFont;
            Resources["DigitalFont"] = digitalFont;
            Resources["DotMatrixFont"] = footerFont;

            // Also set directly on key controls (in case they don't use DynamicResource)
            lblMainContent.FontFamily = titleFont;
            lblEventTimeRange.FontFamily = footerFont;

            // Update title color
            try
            {
                var titleBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(_vm.TitleColor));
                lblMainContent.Foreground = titleBrush;
            }
            catch { }
        }

        // ==================== TIMER DISPLAY (LED Style) ====================
        private void UpdateTimerDisplay()
        {
            if (_vm == null) return;

            if (_vm.TimerRunning)
            {
                lblTimer.Text = _vm.TimerFormatted;

                // Blink effect when in blink phase
                if (_vm.BlinkPhase == BlinkPhase.Blink)
                {
                    var activeBrush = AppViewModel.BrushFromHex(_vm.DigitalTimerColor);
                    lblTimer.Foreground = _vm.BlinkState ? activeBrush : RedBlink;
                }
                else if (_vm.BlinkPhase == BlinkPhase.Red)
                {
                    lblTimer.Foreground = AppViewModel.BrushFromHex(_vm.DigitalTimerColor);
                }
                else
                {
                    lblTimer.Foreground = AppViewModel.BrushFromHex(_vm.DigitalTimerColor);
                }
            }
            else
            {
                lblTimer.Text = "--:--";
                lblTimer.Foreground = RedDim; // Dimmed when not running
            }
        }

        // ==================== SCHEDULE LIST (Cronograma) ====================
        private void RenderScheduleList()
        {
            scheduleList.Children.Clear();
            if (_vm?.CurrentSchedule == null) return;

            var items = _vm.CurrentSchedule;
            int currentIdx = _vm.CurrentItemIndex ?? -1;

            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                bool isActive = (i == currentIdx);
                
                // Colors
                var activeColor = AppViewModel.BrushFromHex(_vm.ScheduleItemActiveColor);
                var inactiveColor = AppViewModel.BrushFromHex(_vm.ScheduleItemInactiveColor);
                var itemColor = isActive ? activeColor : inactiveColor;

                // Container for each row
                var border = new Border
                {
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(12, 8, 12, 8),
                    Margin = new Thickness(0, 0, 0, 6),
                    Background = isActive ? new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)) : Brushes.Transparent,
                    BorderBrush = isActive ? activeColor : Brushes.Transparent,
                    BorderThickness = new Thickness(isActive ? 1 : 0)
                };

                // Grid layout: [Time] [Content]
                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Time column
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Content column

                // Time Text (Parse to show only start time if range, e.g. "18:00")
                string timeText = item.Time?.Split(" as ")[0].Trim() ?? "";
                if (string.IsNullOrEmpty(timeText)) timeText = "•"; // Bullet for items without time

                var txtTime = new TextBlock
                {
                    Text = timeText,
                    Foreground = isActive ? activeColor : new SolidColorBrush(Color.FromArgb(160, 255, 255, 255)), // Dimmed for inactive
                    FontWeight = FontWeights.SemiBold,
                    FontSize = Math.Clamp(_vm.TvListSize * 0.85, 12, 36), // Slightly smaller than content
                    Margin = new Thickness(0, 0, 15, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    FontFamily = (FontFamily)FindResource("StandardFont") // Ensure consistent font
                };
                Grid.SetColumn(txtTime, 0);

                // Content Text
                var txtContent = new TextBlock
                {
                    Text = item.Content.ToUpper(),
                    Foreground = itemColor,
                    FontWeight = isActive ? FontWeights.Bold : FontWeights.Normal,
                    FontSize = Math.Clamp(_vm.TvListSize, 14, 40),
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontFamily = (FontFamily)FindResource("StandardFont")
                };
                
                // Add glow to active content
                if (isActive)
                {
                    txtContent.Effect = new DropShadowEffect
                    {
                        Color = ((SolidColorBrush)activeColor).Color,
                        BlurRadius = 10,
                        ShadowDepth = 0,
                        Opacity = 0.5
                    };
                }

                Grid.SetColumn(txtContent, 1);

                grid.Children.Add(txtTime);
                grid.Children.Add(txtContent);
                border.Child = grid;

                scheduleList.Children.Add(border);
                
                // Auto-scroll to active item
                if (isActive)
                {
                    border.BringIntoView();
                }
            }
        }

        // ==================== CLOCK (LED Display) ====================
        private void UpdateClock()
        {
            var now = DateTime.Now;
            lblClock.Text = now.ToString("HH:mm");
        }

        // ==================== WARNING OVERLAY ====================
        private void ShowWarningOverlay()
        {
            if (warningOverlay == null) return;
            warningOverlay.Visibility = Visibility.Visible;
            warningOverlay.Opacity = 1.0;
            _warningBlinkState = true;
            _warningBlinkTimer.Start();
        }

        private void HideWarningOverlay()
        {
            _warningBlinkTimer.Stop();
            if (warningOverlay != null)
            {
                warningOverlay.Visibility = Visibility.Collapsed;
                warningOverlay.Opacity = 1.0;
            }
            if (lblWarningCountdown != null)
                lblWarningCountdown.Text = "";
        }
    }
}
