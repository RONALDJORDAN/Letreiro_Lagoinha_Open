using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LetreiroDigital.Models
{
    public class PresetData
    {
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
    }

    public class AppConfig
    {
        [JsonPropertyName("current_mode")]
        public string CurrentMode { get; set; } = "PROJECTION";

        [JsonPropertyName("last_day")]
        public string LastDay { get; set; } = "Culto Fé";

        [JsonPropertyName("last_theme")]
        public Theme? LastTheme { get; set; }

        [JsonPropertyName("mode_configs")]
        public Dictionary<string, ModeConfig> ModeConfigs { get; set; } = new();

        [JsonPropertyName("remote_port")]
        public int RemotePort { get; set; } = 8080;

        [JsonPropertyName("remote_password")]
        public string RemotePassword { get; set; } = "";


        [JsonPropertyName("key_bindings")]
        public Dictionary<string, string> KeyBindings { get; set; } = new Dictionary<string, string>();
    }
}
