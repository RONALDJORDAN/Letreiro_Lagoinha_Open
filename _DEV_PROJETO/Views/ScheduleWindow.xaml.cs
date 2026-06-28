using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using LetreiroDigital.Models;
using LetreiroDigital.ViewModels;

namespace LetreiroDigital.Views
{
    public partial class ScheduleWindow : Window
    {
        private AppViewModel _vm = null!;

        public ScheduleWindow()
        {
            InitializeComponent();
            Loaded += (s, e) => UpdatePosition();
        }

        public void SetViewModel(AppViewModel vm)
        {
            _vm = vm;
            _vm.StateChanged += () => Dispatcher.Invoke(RenderSchedule);
            _vm.ScheduleUpdated += () => Dispatcher.Invoke(RenderSchedule);
            RenderSchedule();
        }

        private void RenderSchedule()
        {
            if (_vm == null) return;
            scheduleList.Children.Clear();
            var schedule = _vm.CurrentSchedule;
            int activeIdx = _vm.CurrentItemIndex ?? -1;

            for (int i = 0; i < schedule.Count; i++)
            {
                var item = schedule[i];
                bool isActive = i == activeIdx;

                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(3) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                var bar = new Border
                {
                    Background = AppViewModel.BrushFromHex(item.Color),
                    CornerRadius = new CornerRadius(3, 0, 0, 3),
                };
                Grid.SetColumn(bar, 0);
                grid.Children.Add(bar);

                var sp = new StackPanel { Margin = new Thickness(8, 3, 8, 3) };
                sp.Children.Add(new TextBlock
                {
                    Text = item.Time,
                    FontSize = Math.Max(7, _vm.ScheduleSize * 0.7),
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#AAAAAA")),
                    FontWeight = FontWeights.Bold,
                });
                sp.Children.Add(new TextBlock
                {
                    Text = item.Content,
                    FontSize = _vm.ScheduleSize,
                    Foreground = Brushes.White,
                    FontWeight = isActive ? FontWeights.Bold : FontWeights.Normal,
                });
                Grid.SetColumn(sp, 1);
                grid.Children.Add(sp);

                var card = new Border
                {
                    Child = grid,
                    Background = isActive
                        ? new SolidColorBrush(Color.FromArgb(80, 76, 175, 80))
                        : new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)),
                    CornerRadius = new CornerRadius(3),
                    Margin = new Thickness(0, 0, 0, 2),
                };
                scheduleList.Children.Add(card);
            }
        }

        public void UpdatePosition()
        {
            if (_vm == null) return;
            double dpiScaleX = 1.0, dpiScaleY = 1.0;
            var source = PresentationSource.FromVisual(this);
            if (source?.CompositionTarget != null)
            {
                var matrix = source.CompositionTarget.TransformToDevice;
                dpiScaleX = matrix.M11;
                dpiScaleY = matrix.M22;
            }
            Width = _vm.ScheduleWidth;
            Height = 400;
            Left = (_vm.ScheduleX ?? _vm.GetScheduleDefaultX()) / dpiScaleX;
            Top = (_vm.ScheduleY ?? _vm.GetScheduleDefaultY()) / dpiScaleY;
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
        }
    }
}
