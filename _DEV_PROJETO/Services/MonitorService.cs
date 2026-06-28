using System.Collections.ObjectModel;
using WinForms = System.Windows.Forms;

namespace LetreiroDigital.Services
{
    public class MonitorInfo
    {
        public int Index { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public string Label { get; set; } = "";
    }

    public class MonitorService
    {
        public ObservableCollection<MonitorInfo> Monitors { get; } = new();

        public MonitorInfo? PrimaryMonitor => Monitors.Count > 0 ? Monitors[0] : null;

        public void DetectMonitors()
        {
            Monitors.Clear();
            var screens = WinForms.Screen.AllScreens;
            for (int i = 0; i < screens.Length; i++)
            {
                var s = screens[i];
                Monitors.Add(new MonitorInfo
                {
                    Index = i,
                    X = s.Bounds.X,
                    Y = s.Bounds.Y,
                    Width = s.Bounds.Width,
                    Height = s.Bounds.Height,
                    Label = $"Monitor {i}: {s.Bounds.Width}x{s.Bounds.Height}"
                });
            }
        }

        public MonitorInfo? GetMonitor(int index)
        {
            if (index >= 0 && index < Monitors.Count)
                return Monitors[index];
            return null;
        }
    }
}
