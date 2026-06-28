using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using LetreiroDigital.Models;
using LetreiroDigital.Services;
using WinForms = System.Windows.Forms;
using System.Windows.Input;

namespace LetreiroDigital.ViewModels
{


    public class AppViewModel : BaseViewModel
    {
        private readonly DataService _dataService;
        private readonly TimerService _timerService;
        private readonly WebServerService _webServerService;
        private readonly MonitorService _monitorService;

        // ==================== SHORTCUTS ====================
        public static readonly Dictionary<string, string> DefaultKeyBindings = new()
        {
            { "Ação_IniciarApresentacao", "F5" },
            { "Ação_PararApresentacao", "Escape" },
            { "Ação_ProximoItem", "Down" },
            { "Ação_ItemAnterior", "Up" },
            { "Ação_ModoTelaCheia", "F11" },
            { "Ação_PausarTimer", "Space" },
            { "Ação_ResetarTimer", "R" },
            { "Ação_Adicionar1Min", "OemPlus" }, // or Add
            { "Ação_Subtrair1Min", "OemMinus" }, // or Subtract
            { "Ação_ModoProjecao", "F1" },
            { "Ação_ModoTv", "F2" },
            { "Ação_MostrarOcultarRelogio", "F9" }
        };

        private Dictionary<string, string> _keyBindings = new();
        public Dictionary<string, string> KeyBindings
        {
            get => _keyBindings;
            set => SetProperty(ref _keyBindings, value);
        }

        // ==================== STATE ====================
        private string _currentMode = "TV_MODE";
        private Dictionary<string, ModeConfig> _modeConfigs;
        private string _currentDay = "Culto Fé"; // Bug #1 fix: valor inicial válido
        private Dictionary<string, List<ScheduleItem>> _weeklySchedule;
        private ScheduleItem? _currentItem;
        private int? _currentItemIndex;
        private int _speed = 2;
        private string _bgColor = "#CC0000";
        private int _bannerHeight = 80;
        private int _clockSize = 30;
        private double _clockScale = 1.0;
        private int _scheduleSize = 12;
        private int _scheduleWidth = 300;
        private string _textMode = "full";
        private int _tvHeadlineSize = 22;
        private int _tvTickerSize = 14;
        
        // TV Mode Specific Controls (new granular settings)
        private int _tvEventSize = 72;         // Main event font size
        private int _tvListSize = 24;          // Schedule list font size
        private int _tvTimerSize = 96;         // LED timer display size
        private int _tvClockSize = 88;         // LED clock display size
        private int _tvFooterSize = 38;        // Bottom time bar size

        // Font families (reactive for live preview)
        private string _titleFontFamily = "Oswald";
        private string _bodyFontFamily = "Segoe UI";
        private string _digitalFontFamily = "Digital-7 Mono";
        private string _footerFontFamily = "Led Board-7";
        private string _titleColor = "#FF3333";
        private string _bodyColor = "#FFFFFF";

        // New Theme Properties
        private string _eventLabelColor = "#FF3333";
        private string _scheduleHeaderColor = "#FF3333";
        private string _simpleLabelColor = "#FFFFFF";
        private string _scheduleItemActiveColor = "#FFFFFF";
        private string _scheduleItemInactiveColor = "#CCCCCC";
        private string _digitalTimerColor = "#FF0000";
        private string _digitalClockColor = "#FF0000";
        private string _footerBgColor = "#050508";
        private string _footerTextColor = "#FF4411";
        private string _panelPrimaryBgColor = "#11141B";
        private string _panelSecondaryBgColor = "#1A1D2E";
        private string _liveIndicatorColor = "#00FF00";

        // Visibility
        private bool _showBanner;
        private bool _showClock;
        private bool _showTimer;
        private bool _showSchedule;

        // Remove Control
        private bool _serverRunning;
        private string _serverUrl = string.Empty;

        // Monitor / Positions
        private int _monitorX;
        private int _monitorY;
        private int _monitorWidth = 1920;
        private int _monitorHeight = 1080;
        private int _bannerX;
        private int _bannerY;
        private int? _clockX;
        private int? _clockY;
        private int? _scheduleX;
        private int? _scheduleY;

        // Timer display
        private string _timerFormatted = "--:--";
        private bool _timerRunning;
        private BlinkPhase _blinkPhase = BlinkPhase.Normal;
        private bool _blinkState;
        private double _timerProgress;
        private bool _isUserSeeking;

        // Warning feature
        private bool _enableWarning = false;
        private int _warningSeconds = 30;
        private bool _warningActive = false;

        // Layout mode: 1 = Layout Completo (padrão), 2 = Letreiro Inferior
        private int _tvLayoutMode = 1;

        // ==================== EVENTS ====================
        public event Action? StateChanged;
        public event Action? VisibilityChanged;
        public event Action? ItemSelected;
        public event Action? ScheduleUpdated;
        public event Action? WarningActivated;
        public event Action? WarningDeactivated;

        // ==================== PROPERTIES ====================
        public string CurrentMode
        {
            get => _currentMode;
            set { SetProperty(ref _currentMode, value); OnPropertyChanged(nameof(IsTvMode)); }
        }

        public bool IsTvMode => _currentMode == "TV_MODE";

        public string CurrentDay
        {
            get => _currentDay;
            set { SetProperty(ref _currentDay, value); OnPropertyChanged(nameof(CurrentSchedule)); }
        }

        public ScheduleItem? CurrentItem
        {
            get => _currentItem;
            set { SetProperty(ref _currentItem, value); OnPropertyChanged(nameof(DisplayText)); OnPropertyChanged(nameof(HasCurrentItem)); }
        }

        public int? CurrentItemIndex
        {
            get => _currentItemIndex;
            set => SetProperty(ref _currentItemIndex, value);
        }

        public bool HasCurrentItem => _currentItem != null;

        public int Speed
        {
            get => _speed;
            set => SetProperty(ref _speed, value);
        }

        public string BgColor
        {
            get => _bgColor;
            set { SetProperty(ref _bgColor, value); OnPropertyChanged(nameof(BgBrush)); }
        }

        public Brush BgBrush => BrushFromHex(_bgColor);

        public int BannerHeight
        {
            get => _bannerHeight;
            set => SetProperty(ref _bannerHeight, value);
        }

        public int ClockSize
        {
            get => _clockSize;
            set => SetProperty(ref _clockSize, value);
        }

        public double ClockScale
        {
            get => _clockScale;
            set => SetProperty(ref _clockScale, value);
        }

        public int ScheduleSize
        {
            get => _scheduleSize;
            set => SetProperty(ref _scheduleSize, value);
        }

        public int ScheduleWidth
        {
            get => _scheduleWidth;
            set => SetProperty(ref _scheduleWidth, value);
        }

        public string TextMode
        {
            get => _textMode;
            set { SetProperty(ref _textMode, value); OnPropertyChanged(nameof(DisplayText)); }
        }

        public int TvHeadlineSize
        {
            get => _tvHeadlineSize;
            set => SetProperty(ref _tvHeadlineSize, value);
        }

        public int TvTickerSize
        {
            get => _tvTickerSize;
            set => SetProperty(ref _tvTickerSize, value);
        }

        // TV Mode Granular Controls
        public int TvEventSize
        {
            get => _tvEventSize;
            set => SetProperty(ref _tvEventSize, value);
        }

        public int TvListSize
        {
            get => _tvListSize;
            set => SetProperty(ref _tvListSize, value);
        }

        public int TvTimerSize
        {
            get => _tvTimerSize;
            set => SetProperty(ref _tvTimerSize, value);
        }

        public int TvClockSize
        {
            get => _tvClockSize;
            set => SetProperty(ref _tvClockSize, value);
        }

        public int TvFooterSize
        {
            get => _tvFooterSize;
            set => SetProperty(ref _tvFooterSize, value);
        }

        // Font Families (reactive)
        public string TitleFontFamily
        {
            get => _titleFontFamily;
            set { SetProperty(ref _titleFontFamily, value); StateChanged?.Invoke(); }
        }

        public string BodyFontFamily
        {
            get => _bodyFontFamily;
            set { SetProperty(ref _bodyFontFamily, value); StateChanged?.Invoke(); }
        }

        public string DigitalFontFamily
        {
            get => _digitalFontFamily;
            set { SetProperty(ref _digitalFontFamily, value); StateChanged?.Invoke(); }
        }

        public string FooterFontFamily
        {
            get => _footerFontFamily;
            set { SetProperty(ref _footerFontFamily, value); StateChanged?.Invoke(); }
        }

        public string TitleColor
        {
            get => _titleColor;
            set { SetProperty(ref _titleColor, value); StateChanged?.Invoke(); }
        }

        public string BodyColor
        {
            get => _bodyColor;
            set { SetProperty(ref _bodyColor, value); StateChanged?.Invoke(); }
        }

        // New Observable Properties
        public string EventLabelColor { get => _eventLabelColor; set { SetProperty(ref _eventLabelColor, value); StateChanged?.Invoke(); } }
        public string ScheduleHeaderColor { get => _scheduleHeaderColor; set { SetProperty(ref _scheduleHeaderColor, value); StateChanged?.Invoke(); } }
        public string SimpleLabelColor { get => _simpleLabelColor; set { SetProperty(ref _simpleLabelColor, value); StateChanged?.Invoke(); } }
        public string ScheduleItemActiveColor { get => _scheduleItemActiveColor; set { SetProperty(ref _scheduleItemActiveColor, value); StateChanged?.Invoke(); } }
        public string ScheduleItemInactiveColor { get => _scheduleItemInactiveColor; set { SetProperty(ref _scheduleItemInactiveColor, value); StateChanged?.Invoke(); } }
        public string DigitalTimerColor { get => _digitalTimerColor; set { SetProperty(ref _digitalTimerColor, value); StateChanged?.Invoke(); } }
        public string DigitalClockColor { get => _digitalClockColor; set { SetProperty(ref _digitalClockColor, value); StateChanged?.Invoke(); } }
        public string FooterBgColor { get => _footerBgColor; set { SetProperty(ref _footerBgColor, value); StateChanged?.Invoke(); } }
        public string FooterTextColor { get => _footerTextColor; set { SetProperty(ref _footerTextColor, value); StateChanged?.Invoke(); } }
        public string PanelPrimaryBgColor { get => _panelPrimaryBgColor; set { SetProperty(ref _panelPrimaryBgColor, value); StateChanged?.Invoke(); } }
        public string PanelSecondaryBgColor { get => _panelSecondaryBgColor; set { SetProperty(ref _panelSecondaryBgColor, value); StateChanged?.Invoke(); } }
        public string LiveIndicatorColor { get => _liveIndicatorColor; set { SetProperty(ref _liveIndicatorColor, value); StateChanged?.Invoke(); } }

        // ==================== FONT PATH RESOLVER ====================
        public static string ResolveFontPath(string fontName)
        {
            return FontService.ResolveFontPath(fontName);
        }

        // Visibility
        public bool ShowBanner
        {
            get => _showBanner;
            set { SetProperty(ref _showBanner, value); VisibilityChanged?.Invoke(); }
        }

        public bool ShowClock
        {
            get => _showClock;
            set { SetProperty(ref _showClock, value); VisibilityChanged?.Invoke(); }
        }

        public bool ShowTimer
        {
            get => _showTimer;
            set { SetProperty(ref _showTimer, value); VisibilityChanged?.Invoke(); }
        }

        public bool ShowSchedule
        {
            get => _showSchedule;
            set { SetProperty(ref _showSchedule, value); VisibilityChanged?.Invoke(); }
        }

        // Positions
        public int MonitorX { get => _monitorX; set => SetProperty(ref _monitorX, value); }
        public int MonitorY { get => _monitorY; set => SetProperty(ref _monitorY, value); }
        public int MonitorWidth { get => _monitorWidth; set => SetProperty(ref _monitorWidth, value); }
        public int MonitorHeight { get => _monitorHeight; set => SetProperty(ref _monitorHeight, value); }
        public int BannerX { get => _bannerX; set => SetProperty(ref _bannerX, value); }
        public int BannerY { get => _bannerY; set => SetProperty(ref _bannerY, value); }
        public int? ClockX { get => _clockX; set => SetProperty(ref _clockX, value); }
        public int? ClockY { get => _clockY; set => SetProperty(ref _clockY, value); }
        public int? ScheduleX { get => _scheduleX; set => SetProperty(ref _scheduleX, value); }
        public int? ScheduleY { get => _scheduleY; set => SetProperty(ref _scheduleY, value); }

        // Timer
        public string TimerFormatted { get => _timerFormatted; set => SetProperty(ref _timerFormatted, value); }
        public bool TimerRunning { get => _timerRunning; set => SetProperty(ref _timerRunning, value); }
        public BlinkPhase BlinkPhase { get => _blinkPhase; set => SetProperty(ref _blinkPhase, value); }
        public bool BlinkState { get => _blinkState; set => SetProperty(ref _blinkState, value); }
        
        public double TimerProgress
        {
            get => _timerProgress;
            set { if (SetProperty(ref _timerProgress, value) && _isUserSeeking) SeekTimer(value); }
        }

        public bool IsUserSeeking
        {
            get => _isUserSeeking;
            set => SetProperty(ref _isUserSeeking, value);
        }

        public int TimerTotalSeconds => _timerService.TotalSeconds;

        // Warning feature
        public bool EnableWarning
        {
            get => _enableWarning;
            set
            {
                SetProperty(ref _enableWarning, value);
                _timerService.SetWarningThreshold(_enableWarning ? _warningSeconds : 0);
            }
        }

        public int WarningSeconds
        {
            get => _warningSeconds;
            set
            {
                SetProperty(ref _warningSeconds, value);
                if (_enableWarning) _timerService.SetWarningThreshold(_warningSeconds);
            }
        }

        public bool WarningActive
        {
            get => _warningActive;
            set { SetProperty(ref _warningActive, value); }
        }

        public int TvLayoutMode
        {
            get => _tvLayoutMode;
            set { SetProperty(ref _tvLayoutMode, value); StateChanged?.Invoke(); }
        }

        // Computed
        public List<ScheduleItem> CurrentSchedule =>
            _weeklySchedule.TryGetValue(_currentDay, out var list) ? list : new List<ScheduleItem>();

        public string DisplayText
        {
            get
            {
                if (_currentItem == null) return "AGUARDANDO SELEÇÃO...";
                return _textMode == "full"
                    ? $"{_currentItem.Time} - {_currentItem.Content}"
                    : _currentItem.Content;
            }
        }

        public string TextColor => _currentItem?.Color ?? "#FFFFFF";

        public ObservableCollection<MonitorInfo> Monitors => _monitorService.Monitors;

        public DataService DataSvc => _dataService;
        public TimerService TimerSvc => _timerService;
        public WebServerService WebServerSvc => _webServerService;

        public bool ServerRunning { get => _serverRunning; set => SetProperty(ref _serverRunning, value); }
        public string ServerUrl { get => _serverUrl; set => SetProperty(ref _serverUrl, value); }
        
        private bool _hasUnsavedChanges;
        public bool HasUnsavedChanges { get => _hasUnsavedChanges; set => SetProperty(ref _hasUnsavedChanges, value); }

        // ==================== CONSTRUCTOR ====================
        public AppViewModel()
        {
            _dataService = new DataService();
            _timerService = new TimerService();
            _monitorService = new MonitorService();
            _webServerService = new WebServerService(this);
            _serverUrl = string.Empty;
            _modeConfigs = DataService.DefaultModeConfigs.ToDictionary(kv => kv.Key, kv => kv.Value.Clone());
            _weeklySchedule = new Dictionary<string, List<ScheduleItem>>();

            _timerService.TimerTick += OnTimerTick;
            _timerService.TimerFinished += OnTimerFinished;
            _timerService.WarningReached += (s, e) =>
            {
                _warningActive = true;
                WarningActivated?.Invoke();
            };
        }

        public ObservableCollection<string> ScheduleTabs { get; private set; } = new ObservableCollection<string>();

        // ==================== INITIALIZATION ====================
        public void Initialize()
        {
            // Load schedule
            // Load schedule
            _weeklySchedule = _dataService.LoadSchedule();
            RefreshScheduleTabs();
            HasUnsavedChanges = false;

            // Load config
            var config = _dataService.LoadConfig();
            _currentMode = config.CurrentMode;
            _currentDay = config.LastDay ?? "Culto Fé";
            RemotePort = config.RemotePort > 0 ? config.RemotePort : 8080;
            RemotePassword = config.RemotePassword ?? "";
            
            // Load custom key bindings or apply defaults
            if (config.KeyBindings != null && config.KeyBindings.Count > 0)
            {
                _keyBindings = new Dictionary<string, string>(config.KeyBindings);
            }
            else
            {
                _keyBindings = new Dictionary<string, string>(DefaultKeyBindings);
            }
            
            if (config.LastTheme != null)
            {
                ApplyTheme(config.LastTheme);
            }

            if (config.ModeConfigs.Count > 0)
            {
                foreach (var kv in config.ModeConfigs)
                {
                    if (_modeConfigs.ContainsKey(kv.Key))
                    {
                        var src = kv.Value;
                        var dst = _modeConfigs[kv.Key];
                        dst.ClockSize = src.ClockSize;
                        dst.ClockScale = src.ClockScale;
                        dst.BannerHeight = src.BannerHeight;
                        dst.ScheduleSize = src.ScheduleSize;
                        dst.ScheduleWidth = src.ScheduleWidth;
                        dst.TextMode = src.TextMode;
                        dst.BgColor = src.BgColor;
                        dst.Speed = src.Speed;
                        dst.TvHeadlineSize = src.TvHeadlineSize;
                        dst.TvTickerSize = src.TvTickerSize;
                        dst.TvEventSize = src.TvEventSize;
                        dst.TvListSize = src.TvListSize;
                        dst.TvTimerSize = src.TvTimerSize;
                        dst.TvClockSize = src.TvClockSize;
                        dst.TvFooterSize = src.TvFooterSize;
                        dst.PosBannerX = src.PosBannerX;
                        dst.PosBannerY = src.PosBannerY;
                        dst.ClockX = src.ClockX;
                        dst.ClockY = src.ClockY;
                        dst.ScheduleX = src.ScheduleX;
                        dst.ScheduleY = src.ScheduleY;
                    }
                }
            }

            // Detect monitors
            DetectMonitors();

            // Apply current mode (Force TV_MODE at startup)
            _currentMode = "TV_MODE";
            ApplyMode(_currentMode, false);

            NotifyAll();
        }

        public void DetectMonitors()
        {
            _monitorService.DetectMonitors();

            // Default to primary monitor (index 0)
            if (Monitors.Count > 0)
            {
                var primary = Monitors[0];
                _monitorX = primary.X;
                _monitorY = primary.Y;
                _monitorWidth = primary.Width;
                _monitorHeight = primary.Height;
                _bannerX = primary.X;
                _bannerY = primary.Y;
                _selectedMonitorIndex = 0;
            }
        }

        private int _selectedMonitorIndex = 0;
        public int SelectedMonitorIndex
        {
            get => _selectedMonitorIndex;
            set { SetProperty(ref _selectedMonitorIndex, value); }
        }

        // ==================== MODE ====================
        public void ApplyMode(string mode, bool forceHide = true)
        {
            if (!_modeConfigs.ContainsKey(mode)) return;
            var cfg = _modeConfigs[mode];
            CurrentMode = mode;
            BgColor = cfg.BgColor;
            BannerHeight = cfg.BannerHeight;
            ClockSize = cfg.ClockSize;
            ClockScale = cfg.ClockScale;
            ScheduleSize = cfg.ScheduleSize;
            ScheduleWidth = cfg.ScheduleWidth;
            TextMode = cfg.TextMode;
            Speed = cfg.Speed > 0 ? cfg.Speed : 2;
            TvHeadlineSize = cfg.TvHeadlineSize > 0 ? cfg.TvHeadlineSize : 22;
            TvTickerSize = cfg.TvTickerSize > 0 ? cfg.TvTickerSize : 14;
            TvEventSize = cfg.TvEventSize > 0 ? cfg.TvEventSize : 72;
            TvListSize = cfg.TvListSize > 0 ? cfg.TvListSize : 24;
            TvTimerSize = cfg.TvTimerSize > 0 ? cfg.TvTimerSize : 96;
            TvClockSize = cfg.TvClockSize > 0 ? cfg.TvClockSize : 88;
            TvFooterSize = cfg.TvFooterSize > 0 ? cfg.TvFooterSize : 38;

            // Restore positions
            if (cfg.PosBannerX.HasValue) BannerX = cfg.PosBannerX.Value;
            if (cfg.PosBannerY.HasValue) BannerY = cfg.PosBannerY.Value;
            ClockX = cfg.ClockX;
            ClockY = cfg.ClockY;
            ScheduleX = cfg.ScheduleX;
            ScheduleY = cfg.ScheduleY;

            // Warning feature
            _enableWarning = cfg.EnableWarning;
            _warningSeconds = cfg.WarningSeconds > 0 ? cfg.WarningSeconds : 30;
            _timerService.SetWarningThreshold(_enableWarning ? _warningSeconds : 0);
            OnPropertyChanged(nameof(EnableWarning));
            OnPropertyChanged(nameof(WarningSeconds));

            // Layout mode
            _tvLayoutMode = cfg.TvLayoutMode > 0 ? cfg.TvLayoutMode : 1;
            OnPropertyChanged(nameof(TvLayoutMode));

            if (forceHide)
            {
                // Force hide all
                _showBanner = false;
                _showClock = false;
                _showTimer = false;
                _showSchedule = false;
                OnPropertyChanged(nameof(ShowBanner));
                OnPropertyChanged(nameof(ShowClock));
                OnPropertyChanged(nameof(ShowTimer));
                OnPropertyChanged(nameof(ShowSchedule));
            }

            VisibilityChanged?.Invoke();
            StateChanged?.Invoke();
            SaveConfig();
        }

        public void SwitchMode(string newMode)
        {
            if (newMode == _currentMode) return;
            SaveCurrentToConfig(_currentMode);
            ApplyMode(newMode);
        }

        public void SaveCurrentToConfig(string mode)
        {
            if (!_modeConfigs.ContainsKey(mode)) return;
            var cfg = _modeConfigs[mode];
            cfg.ScheduleSize = _scheduleSize;
            cfg.ScheduleWidth = _scheduleWidth;
            cfg.TextMode = _textMode;
            cfg.BannerHeight = _bannerHeight;
            cfg.ClockSize = _clockSize;
            cfg.ClockScale = _clockScale;
            cfg.BgColor = _bgColor;
            cfg.Speed = _speed;
            
            cfg.TvHeadlineSize = _tvHeadlineSize;
            cfg.TvTickerSize = _tvTickerSize;
            cfg.TvEventSize = _tvEventSize;
            cfg.TvListSize = _tvListSize;
            cfg.TvTimerSize = _tvTimerSize;
            cfg.TvClockSize = _tvClockSize;
            cfg.TvFooterSize = _tvFooterSize;

            cfg.PosBannerX = _bannerX;
            cfg.PosBannerY = _bannerY;
            cfg.ClockX = _clockX;
            cfg.ClockY = _clockY;
            cfg.ScheduleX = _scheduleX;
            cfg.ScheduleY = _scheduleY;
            cfg.EnableWarning = _enableWarning;
            cfg.WarningSeconds = _warningSeconds;
            cfg.TvLayoutMode = _tvLayoutMode;
        }

        public void SaveConfig()
        {
            SaveCurrentToConfig(_currentMode);
            var config = new AppConfig
            {
                CurrentMode = _currentMode,
                LastDay = _currentDay,
                LastTheme = GetCurrentTheme(),
                ModeConfigs = _modeConfigs,
                RemotePort = RemotePort,
                RemotePassword = RemotePassword,
                KeyBindings = _keyBindings
            };
            _dataService.SaveConfig(config);
        }

        // ==================== ITEM SELECTION ====================
        public void SelectItem(ScheduleItem item, int index)
        {
            // Deactivate warning from previous item
            if (_warningActive)
            {
                _warningActive = false;
                WarningDeactivated?.Invoke();
            }

            CurrentItem = item;
            CurrentItemIndex = index;

            // Auto-show banner
            if (!_showBanner) ShowBanner = true;
            if (!_showClock && !_showTimer)
            {
                ShowClock = true;
                ShowTimer = true;
            }

            // Start timer
            var seconds = DataService.ParseDuration(item.Duration);
            if (seconds.HasValue)
                _timerService.Start(seconds.Value);

            OnPropertyChanged(nameof(DisplayText));
            OnPropertyChanged(nameof(TextColor));
            ItemSelected?.Invoke();
            StateChanged?.Invoke();
        }

        public void AdvanceToNextItem()
        {
            var schedule = CurrentSchedule;
            if (_currentItemIndex == null)
            {
                if (schedule.Count > 0) SelectItem(schedule[0], 0);
                return;
            }
            int nextIdx = _currentItemIndex.Value + 1;
            if (nextIdx < schedule.Count)
                SelectItem(schedule[nextIdx], nextIdx);
        }

        public void PrevItem()
        {
            var schedule = CurrentSchedule;
            if (_currentItemIndex == null || _currentItemIndex <= 0) return;
            int prevIdx = _currentItemIndex.Value - 1;
            SelectItem(schedule[prevIdx], prevIdx);
        }

        public void DeleteItem(int index)
        {
            var schedule = CurrentSchedule;
            if (index < 0 || index >= schedule.Count) return;
            schedule.RemoveAt(index);
            HasUnsavedChanges = true; // _dataService.SaveSchedule(_weeklySchedule);

            if (_currentItemIndex == index)
            {
                CurrentItem = null;
                CurrentItemIndex = null;
                _timerService.Stop();
            }
            else if (_currentItemIndex != null && _currentItemIndex > index)
            {
                CurrentItemIndex = _currentItemIndex - 1;
            }

            StateChanged?.Invoke();
            ScheduleUpdated?.Invoke();
        }

        // ==================== SCHEDULE MANAGEMENT ====================
        public void ChangeDay(string day)
        {
            CurrentDay = day;
            CurrentItem = null;
            CurrentItemIndex = null;
            _timerService.Stop();
            SaveConfig(); // Proactive save
            StateChanged?.Invoke();
            ScheduleUpdated?.Invoke();
        }

        public void UpdateScheduleDay(string day, List<ScheduleItem> schedule)
        {
            _weeklySchedule[day] = schedule;
            HasUnsavedChanges = true; // _dataService.SaveSchedule(_weeklySchedule);
            if (day == _currentDay)
            {
                OnPropertyChanged(nameof(CurrentSchedule));
                StateChanged?.Invoke();
                ScheduleUpdated?.Invoke();
            }
        }

        public void SaveSchedule()
        {
            _dataService.SaveSchedule(_weeklySchedule);
            HasUnsavedChanges = false;
        }

        public void ResetSchedule()
        {
            _weeklySchedule.Clear();
            foreach (var day in DataService.DaysOfWeek)
            {
                _weeklySchedule[day] = _dataService.GetDefaultScheduleForDay(day);
            }
            CurrentItem = null; // Bug #3 fix: duplicata removida
            CurrentItemIndex = null;
            _timerService.Stop();
            // _dataService.SaveSchedule(_weeklySchedule);
            HasUnsavedChanges = true;
            RefreshScheduleTabs();
            ChangeDay(ScheduleTabs.FirstOrDefault() ?? "Culto de Celebração");
            
            OnPropertyChanged(nameof(CurrentSchedule));
            StateChanged?.Invoke();
            ScheduleUpdated?.Invoke();
        }

        private void RefreshScheduleTabs()
        {
            ScheduleTabs.Clear();
            var orderedKeys = new List<string>();
            foreach (var day in DataService.DaysOfWeek)
            {
                if (_weeklySchedule.ContainsKey(day)) orderedKeys.Add(day);
            }
            foreach (var key in _weeklySchedule.Keys)
            {
                if (!orderedKeys.Contains(key)) orderedKeys.Add(key);
            }
            foreach (var key in orderedKeys) ScheduleTabs.Add(key);
        }

        public void AddScheduleTab(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            if (_weeklySchedule.ContainsKey(name)) return;

            _weeklySchedule[name] = new List<ScheduleItem>();
            // _dataService.SaveSchedule(_weeklySchedule);
            HasUnsavedChanges = true;
            RefreshScheduleTabs();
            ChangeDay(name);
        }

        public void RemoveScheduleTab(string name)
        {
            if (!_weeklySchedule.ContainsKey(name)) return;
            if (ScheduleTabs.Count <= 1) return;

            _weeklySchedule.Remove(name);
            // _dataService.SaveSchedule(_weeklySchedule);
            HasUnsavedChanges = true;
            RefreshScheduleTabs();
            
            if (_currentDay == name)
            {
                ChangeDay(ScheduleTabs.FirstOrDefault() ?? "Culto de Celebração");
            }
        }

        public void ReorderScheduleItem(int oldIndex, int newIndex)
        {
            var schedule = CurrentSchedule;
            if (oldIndex < 0 || oldIndex >= schedule.Count) return;
            if (newIndex < 0 || newIndex >= schedule.Count) return;
            if (oldIndex == newIndex) return;

            var item = schedule[oldIndex];
            schedule.RemoveAt(oldIndex);
            schedule.Insert(newIndex, item);

            // Se o item que movemos era o selecionado, atualiza o índice
            if (CurrentItemIndex == oldIndex)
            {
                CurrentItemIndex = newIndex;
            }
            else
            {
                // Ajusta o índice do selecionado se ele foi afetado pelo movimento
                if (oldIndex < CurrentItemIndex && newIndex >= CurrentItemIndex)
                    CurrentItemIndex--;
                else if (oldIndex > CurrentItemIndex && newIndex <= CurrentItemIndex)
                    CurrentItemIndex++;
            }

            HasUnsavedChanges = true;
            ScheduleUpdated?.Invoke();
            StateChanged?.Invoke();
        }

        public void RenameScheduleTab(string oldName, string newName)
        {
             if (!_weeklySchedule.ContainsKey(oldName)) return;
             if (_weeklySchedule.ContainsKey(newName)) return; // Exists

             var items = _weeklySchedule[oldName];
             _weeklySchedule.Remove(oldName);
             _weeklySchedule[newName] = items; // Bug #2 fix: assignment duplicado removido
             
             // _dataService.SaveSchedule(_weeklySchedule);
             HasUnsavedChanges = true;
             RefreshScheduleTabs();
             
             if (_currentDay == oldName) ChangeDay(newName);
        }

        public Dictionary<string, List<ScheduleItem>> WeeklySchedule => _weeklySchedule;

        // ==================== MONITOR ====================
        public void ChangeMonitor(int index)
        {
            if (index < 0 || index >= Monitors.Count) return;
            SelectedMonitorIndex = index;
            var m = Monitors[index];
            MonitorX = m.X;
            MonitorY = m.Y;
            MonitorWidth = m.Width;
            MonitorHeight = m.Height;
            BannerX = m.X;
            BannerY = m.Y;
            ClockX = null;
            ClockY = null;
            ScheduleX = null;
            ScheduleY = null;
            VisibilityChanged?.Invoke();
            StateChanged?.Invoke();
        }

        // ==================== EMERGENCY ====================
        public void EmergencyStop()
        {
            _showBanner = false;
            _showClock = false;
            _showTimer = false;
            _showSchedule = false;
            _isContentVisible = true; // Reset content visibility for next time
            OnPropertyChanged(nameof(ShowBanner));
            OnPropertyChanged(nameof(ShowClock));
            OnPropertyChanged(nameof(ShowTimer));
            OnPropertyChanged(nameof(ShowSchedule));
            OnPropertyChanged(nameof(IsContentVisible));
            _timerService.Stop();
            VisibilityChanged?.Invoke();
            StateChanged?.Invoke();
        }

        private int _remotePort = 8080;
        public int RemotePort
        {
            get => _remotePort;
            set { _remotePort = value; OnPropertyChanged(); }
        }

        private string _remotePassword = "";
        public string RemotePassword
        {
            get => _remotePassword;
            set { _remotePassword = value; OnPropertyChanged(); }
        }



        // ==================== WEB SERVER ====================
        public void StartWebServer(bool generateRandom = false)
        {
            if (_serverRunning) return;

            string? passToUse = null;

            // Priority: Fixed Password > Random Generated
            if (!string.IsNullOrEmpty(RemotePassword))
            {
                passToUse = RemotePassword;
            }
            else if (generateRandom)
            {
                passToUse = new Random().Next(1000, 9999).ToString();
            }

            int port = RemotePort > 0 ? RemotePort : 8080;
            
            _webServerService.Start(passToUse, port); 
            ServerRunning = true;
            
            string url = $"http://{_webServerService.GetLocalIpAddress()}:{port}";
            if (!string.IsNullOrEmpty(_webServerService.Password))
            {
                url += $"?key={_webServerService.Password}";
            }
            ServerUrl = url;
        }

        public void StopWebServer()
        {
            if (!_serverRunning) return;
            _webServerService.Stop();
            ServerRunning = false;
            ServerUrl = string.Empty;
        }

        // Content Visibility (Blackout)
        private bool _isContentVisible = true;
        public bool IsContentVisible
        {
            get => _isContentVisible;
            set { SetProperty(ref _isContentVisible, value); }
        }

        // ==================== TOGGLES ====================
        public void ToggleBanner() 
        {
            if (!ShowBanner)
            {
                // If closed, open it with content visible
                IsContentVisible = true;
                ShowBanner = true;
            }
            else
            {
                // If open, just toggle content (blackout)
                IsContentVisible = !IsContentVisible;
            }
        }
        public void ToggleClock() => ShowClock = !ShowClock;
        public void ToggleTimer() => ShowTimer = !ShowTimer;
        
        public void ToggleTvMode()
        {
            // Only TV Mode allowed now
            SwitchMode("TV_MODE");
        }

        public void ToggleTimerPause()
        {
            if (_timerService.Running)
                _timerService.Pause();
            else
            {
               if (_timerService.Seconds > 0)
                   _timerService.Resume();
               else if (_currentItem != null)
               {
                   // Start new
                   var seconds = DataService.ParseDuration(_currentItem.Duration) ?? 0;
                   if (seconds > 0) _timerService.Start(seconds);
               }
            }
            OnPropertyChanged(nameof(TimerRunning));
        }

        public void SeekTimer(double progress)
        {
            if (_timerService.TotalSeconds <= 0) return;
            // Progress is 0-100, where 100 is start (TotalSeconds) and 0 is end (0)
            // But usually sliders are 0-100 where 100 is end. 
            // The timer is a countdown. So 0% progress = TotalSeconds, 100% progress = 0.
            // Let's make the slider 0-100 where 0 is the beginning and 100 is the end.
            double remainingFactor = 1.0 - (progress / 100.0);
            _timerService.Seconds = (int)Math.Round(_timerService.TotalSeconds * remainingFactor);
        }

        // ==================== TIMER EVENTS ====================
        private void OnTimerTick(object? sender, TimerTickEventArgs e)
        {
            TimerFormatted = e.Formatted;
            TimerRunning = e.Running;
            BlinkPhase = e.BlinkPhase;
            BlinkState = e.BlinkState;

            if (!_isUserSeeking && _timerService.TotalSeconds > 0)
            {
                // Progress 0% = start, 100% = end
                double progress = (1.0 - (double)e.Seconds / _timerService.TotalSeconds) * 100.0;
                TimerProgress = Math.Clamp(progress, 0, 100);
            }
        }

        private void OnTimerFinished(object? sender, EventArgs e)
        {
            // Deactivate warning when timer finishes
            if (_warningActive)
            {
                _warningActive = false;
                WarningDeactivated?.Invoke();
            }
            AdvanceToNextItem();
        }

        // ==================== HELPERS ====================
        private void NotifyAll()
        {
            OnPropertyChanged(nameof(CurrentMode));
            OnPropertyChanged(nameof(CurrentDay));
            OnPropertyChanged(nameof(CurrentSchedule));
            OnPropertyChanged(nameof(CurrentItem));
            OnPropertyChanged(nameof(CurrentItemIndex));
            OnPropertyChanged(nameof(Speed));
            OnPropertyChanged(nameof(BgColor));
            OnPropertyChanged(nameof(BgBrush));
            OnPropertyChanged(nameof(BannerHeight));
            OnPropertyChanged(nameof(ClockSize));
            OnPropertyChanged(nameof(ClockScale));
            OnPropertyChanged(nameof(ScheduleSize));
            OnPropertyChanged(nameof(ScheduleWidth));
            OnPropertyChanged(nameof(TextMode));
            OnPropertyChanged(nameof(ShowBanner));
            OnPropertyChanged(nameof(ShowClock));
            OnPropertyChanged(nameof(ShowTimer));
            OnPropertyChanged(nameof(ShowSchedule));
            OnPropertyChanged(nameof(DisplayText));
            OnPropertyChanged(nameof(TimerFormatted));
            OnPropertyChanged(nameof(TimerRunning));
            StateChanged?.Invoke();
        }

        public void RaiseStateChanged() => StateChanged?.Invoke();

        public static Brush BrushFromHex(string hex)
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(hex);
                return new SolidColorBrush(color);
            }
            catch
            {
                return Brushes.Red;
            }
        }

        public int GetClockWindowWidth() => (int)Math.Round(200 * _clockScale);
        public int GetClockWindowHeight() => (int)Math.Round(120 * _clockScale);

        public int GetClockDefaultX() => _monitorX + _monitorWidth - GetClockWindowWidth() - 20;
        public int GetClockDefaultY() => _monitorY + _monitorHeight - GetClockWindowHeight() - 20;

        public int GetScheduleDefaultX() => _monitorX + 50;
        public int GetScheduleDefaultY() => _monitorY + 150;

        // ==================== THEME & TRANSITIONS ====================
        private double _transitionDuration = 0.5;
        private string _transitionType = "Fade";
        private string _activeThemeName = "(Personalizado)";
        
        public double TransitionDuration
        {
            get => _transitionDuration;
            set => SetProperty(ref _transitionDuration, value);
        }

        public string TransitionType
        {
            get => _transitionType;
            set => SetProperty(ref _transitionType, value);
        }

        public void ApplyTheme(Theme theme)
        {
            if (theme == null) return;

            // Font sizes
            TvEventSize = theme.TitleFontSize;
            TvListSize = theme.BodyFontSize;
            TvTimerSize = theme.TvTimerSize;
            TvClockSize = theme.TvClockSize;
            TvFooterSize = theme.TvFooterSize;

            // Font families
            TitleFontFamily = theme.TitleFontFamily;
            BodyFontFamily = theme.BodyFontFamily;
            DigitalFontFamily = theme.DigitalFontFamily;
            FooterFontFamily = theme.FooterFontFamily;

            // Colors
            TitleColor = theme.TitleColor;
            BodyColor = theme.BodyColor;
            BgColor = theme.BackgroundColor;
            
            // Apply new colors
            EventLabelColor = theme.EventLabelColor;
            ScheduleHeaderColor = theme.ScheduleHeaderColor;
            SimpleLabelColor = theme.SimpleLabelColor;
            ScheduleItemActiveColor = theme.ScheduleItemActiveColor;
            ScheduleItemInactiveColor = theme.ScheduleItemInactiveColor;
            DigitalTimerColor = theme.DigitalTimerColor;
            DigitalClockColor = theme.DigitalClockColor;
            FooterBgColor = theme.FooterBgColor;
            FooterTextColor = theme.FooterTextColor;
            PanelPrimaryBgColor = theme.PanelPrimaryBgColor;
            PanelSecondaryBgColor = theme.PanelSecondaryBgColor;
            LiveIndicatorColor = theme.LiveIndicatorColor;

            // Transitions
            TransitionDuration = theme.TransitionDuration;
            TransitionType = theme.TransitionType;

            // Track theme name
            _activeThemeName = theme.Name;

            NotifyAll();
        }

        public Theme GetCurrentTheme()
        {
            return new Theme
            {
                Name = _activeThemeName,
                TitleFontFamily = _titleFontFamily,
                TitleFontSize = _tvEventSize,
                TitleColor = _titleColor,
                BodyFontFamily = _bodyFontFamily,
                BodyFontSize = _tvListSize,
                BodyColor = _bodyColor,
                DigitalFontFamily = _digitalFontFamily,
                FooterFontFamily = _footerFontFamily,
                TvTimerSize = _tvTimerSize,
                TvClockSize = _tvClockSize,
                TvFooterSize = _tvFooterSize,
                
                // Save new colors
                EventLabelColor = _eventLabelColor,
                ScheduleHeaderColor = _scheduleHeaderColor,
                SimpleLabelColor = _simpleLabelColor,
                ScheduleItemActiveColor = _scheduleItemActiveColor,
                ScheduleItemInactiveColor = _scheduleItemInactiveColor,
                DigitalTimerColor = _digitalTimerColor,
                DigitalClockColor = _digitalClockColor,
                FooterBgColor = _footerBgColor,
                FooterTextColor = _footerTextColor,
                PanelPrimaryBgColor = _panelPrimaryBgColor,
                PanelSecondaryBgColor = _panelSecondaryBgColor,
                LiveIndicatorColor = _liveIndicatorColor,

                BackgroundColor = _bgColor,
                TransitionDuration = _transitionDuration,
                TransitionType = _transitionType,
            };
        }
    }
}
