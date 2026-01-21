using System.Text.Json.Serialization;

namespace WPF_LCD_Test.Models
{
    public class AppSettings
    {
        [JsonPropertyName("languageCultureCode")]
        public string LanguageCultureCode { get; set; } = "";
        [JsonPropertyName("colorAnalyzerChannel")]
        public string ColorAnalyzerChannel { get; set; } = "0";
        [JsonPropertyName("autoConnectEnabled")]
        public bool AutoConnectEnabled { get; set; } = true;
    }
}