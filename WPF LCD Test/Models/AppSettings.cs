using System.Text.Json.Serialization;

namespace WPF_LCD_Test.Models
{
    public class AppSettings
    {
        [JsonPropertyName("languageCultureCode")]
        public string LanguageCultureCode { get; set; } = "";
        [JsonPropertyName("devicePort")]
        public string DevicePort { get; set; } = "COM1";
        [JsonPropertyName("autoConnectEnabled")]
        public bool AutoConnectEnabled { get; set; } = true;
    }
}