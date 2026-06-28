using System;
using System.Windows.Threading;

namespace LetreiroDigital.Services
{
    public enum BlinkPhase
    {
        Normal,
        Red,
        Blink
    }

    public class TimerTickEventArgs : EventArgs
    {
        public int Seconds { get; set; }
        public bool Running { get; set; }
        public string Formatted { get; set; } = "--:--";
        public BlinkPhase BlinkPhase { get; set; }
        public bool BlinkState { get; set; }
    }

    public class TimerService
    {
        private readonly DispatcherTimer _timer;
        private int _seconds;
        private int _totalSeconds;
        private bool _running;
        private bool _blinkState;
        private BlinkPhase _blinkPhase = BlinkPhase.Normal;

        public event EventHandler<TimerTickEventArgs>? TimerTick;
        public event EventHandler? TimerFinished;
        public event EventHandler? WarningReached;

        private int _warningThreshold = 0;
        private bool _warningFired = false;

        public int Seconds 
        { 
            get => _seconds;
            set 
            {
                _seconds = Math.Clamp(value, 0, _totalSeconds > 0 ? _totalSeconds : int.MaxValue);
                RaiseTimerTick();
            }
        }
        public int TotalSeconds => _totalSeconds;
        public bool Running => _running;
        public BlinkPhase Phase => _blinkPhase;
        public bool BlinkState => _blinkState;
        public string Formatted => DataService.FormatTimer(_seconds);

        public TimerService()
        {
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += OnTick;
        }

        public void SetWarningThreshold(int seconds)
        {
            _warningThreshold = seconds;
        }

        public void Start(int seconds)
        {
            if (seconds <= 0) { Stop(); return; }
            _totalSeconds = seconds;
            _seconds = seconds;
            _running = true;
            _blinkState = false;
            _blinkPhase = BlinkPhase.Normal;
            _warningFired = false;
            _timer.Start();
            RaiseTimerTick();
        }

        public void Reset()
        {
            Stop();
        }

        public void AddSeconds(int delta)
        {
            _seconds = Math.Max(0, _seconds + delta);
            RaiseTimerTick();
        }

        public void Stop()
        {
            _running = false;
            // _seconds = 0; // Don't reset seconds on Stop, let Reset() do that if needed, or caller handles it.
            // Actually, "Stop" usually means "Reset" in this context based on usage. 
            // Let's keep Stop as "Full Stop/Reset" and add Pause.
            _seconds = 0; 
            _blinkPhase = BlinkPhase.Normal;
            _timer.Stop();
            RaiseTimerTick();
        }

        public void Pause()
        {
            if (!_running) return;
            _running = false;
            _timer.Stop();
            RaiseTimerTick();
        }

        public void Resume()
        {
            if (_seconds <= 0) return;
            _running = true;
            _timer.Start();
            RaiseTimerTick();
        }

        private void OnTick(object? sender, EventArgs e)
        {
            if (!_running) return;

            if (_seconds > 0)
            {
                _seconds--;

                if (_seconds <= 30)
                {
                    _blinkPhase = BlinkPhase.Blink;
                    _blinkState = !_blinkState;
                }
                else if (_seconds <= 60)
                {
                    _blinkPhase = BlinkPhase.Red;
                }
                else
                {
                    _blinkPhase = BlinkPhase.Normal;
                }

                RaiseTimerTick();

                // Fire warning once when threshold is crossed
                if (_warningThreshold > 0 && !_warningFired && _seconds <= _warningThreshold)
                {
                    _warningFired = true;
                    WarningReached?.Invoke(this, EventArgs.Empty);
                }
            }
            else
            {
                // Timer finished — auto advance
                _running = false;
                _blinkPhase = BlinkPhase.Normal;
                _timer.Stop();
                RaiseTimerTick();
                TimerFinished?.Invoke(this, EventArgs.Empty);
            }
        }

        private void RaiseTimerTick()
        {
            TimerTick?.Invoke(this, new TimerTickEventArgs
            {
                Seconds = _seconds,
                Running = _running,
                Formatted = DataService.FormatTimer(_seconds),
                BlinkPhase = _blinkPhase,
                BlinkState = _blinkState,
            });
        }
    }
}
