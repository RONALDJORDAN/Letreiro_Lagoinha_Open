using System.Text.Json.Serialization;

namespace LetreiroDigital.Models
{
    public class ModeConfig
    {
        [JsonPropertyName("show_clock")]
        public bool ShowClock { get; set; }

        [JsonPropertyName("show_timer")]
        public bool ShowTimer { get; set; }

        [JsonPropertyName("show_banner")]
        public bool ShowBanner { get; set; }

        [JsonPropertyName("show_schedule")]
        public bool ShowSchedule { get; set; }

        [JsonPropertyName("clock_size")]
        public int ClockSize { get; set; } = 30;

        [JsonPropertyName("clock_scale")]
        public double ClockScale { get; set; } = 1.0;

        [JsonPropertyName("banner_height")]
        public int BannerHeight { get; set; } = 80;

        [JsonPropertyName("schedule_size")]
        public int ScheduleSize { get; set; } = 12;

        [JsonPropertyName("schedule_width")]
        public int ScheduleWidth { get; set; } = 300;

        [JsonPropertyName("text_mode")]
        public string TextMode { get; set; } = "full";

        [JsonPropertyName("bg_color")]
        public string BgColor { get; set; } = "#CC0000";

        [JsonPropertyName("speed")]
        public int Speed { get; set; } = 2;

        [JsonPropertyName("tv_headline_size")]
        public int TvHeadlineSize { get; set; } = 22;

        [JsonPropertyName("tv_ticker_size")]
        public int TvTickerSize { get; set; } = 14;

        [JsonPropertyName("tv_event_size")]
        public int TvEventSize { get; set; } = 72;

        [JsonPropertyName("tv_list_size")]
        public int TvListSize { get; set; } = 24;

        [JsonPropertyName("tv_timer_size")]
        public int TvTimerSize { get; set; } = 96;

        [JsonPropertyName("tv_clock_size")]
        public int TvClockSize { get; set; } = 88;

        [JsonPropertyName("tv_footer_size")]
        public int TvFooterSize { get; set; } = 38;

        // Saved positions
        [JsonPropertyName("pos_banner_x")]
        public int? PosBannerX { get; set; }

        [JsonPropertyName("pos_banner_y")]
        public int? PosBannerY { get; set; }

        [JsonPropertyName("clock_x")]
        public int? ClockX { get; set; }

        [JsonPropertyName("clock_y")]
        public int? ClockY { get; set; }

        [JsonPropertyName("schedule_x")]
        public int? ScheduleX { get; set; }

        [JsonPropertyName("schedule_y")]
        public int? ScheduleY { get; set; }

        [JsonPropertyName("enable_warning")]
        public bool EnableWarning { get; set; } = false;

        [JsonPropertyName("warning_seconds")]
        public int WarningSeconds { get; set; } = 30;

        [JsonPropertyName("tv_layout_mode")]
        public int TvLayoutMode { get; set; } = 1;

        public ModeConfig Clone()
        {
            return new ModeConfig
            {
                ShowClock = ShowClock,
                ShowTimer = ShowTimer,
                ShowBanner = ShowBanner,
                ShowSchedule = ShowSchedule,
                ClockSize = ClockSize,
                ClockScale = ClockScale,
                BannerHeight = BannerHeight,
                ScheduleSize = ScheduleSize,
                ScheduleWidth = ScheduleWidth,
                TextMode = TextMode,
                BgColor = BgColor,
                Speed = Speed,
                TvHeadlineSize = TvHeadlineSize,
                TvTickerSize = TvTickerSize,
                TvEventSize = TvEventSize,
                TvListSize = TvListSize,
                TvTimerSize = TvTimerSize,
                TvClockSize = TvClockSize,
                TvFooterSize = TvFooterSize,
                PosBannerX = PosBannerX,
                PosBannerY = PosBannerY,
                ClockX = ClockX,
                ClockY = ClockY,
                ScheduleX = ScheduleX,
                ScheduleY = ScheduleY,
                EnableWarning = EnableWarning,
                WarningSeconds = WarningSeconds,
                TvLayoutMode = TvLayoutMode,
            };
        }
    }
}
