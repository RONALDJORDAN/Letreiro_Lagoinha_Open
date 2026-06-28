using System.Text.Json.Serialization;

namespace LetreiroDigital.Models
{
    public class ScheduleItem
    {
        [JsonPropertyName("time")]
        public string Time { get; set; } = "00:00";

        [JsonPropertyName("sigla")]
        public string Sigla { get; set; } = "";

        [JsonPropertyName("content")]
        public string Content { get; set; } = "";

        [JsonPropertyName("duration")]
        public string Duration { get; set; } = "5min";

        [JsonPropertyName("lead")]
        public string Lead { get; set; } = "";

        [JsonPropertyName("color")]
        public string Color { get; set; } = "#FFEB3B";

        public ScheduleItem Clone()
        {
            return new ScheduleItem
            {
                Time = Time,
                Sigla = Sigla,
                Content = Content,
                Duration = Duration,
                Lead = Lead,
                Color = Color
            };
        }
    }
}
