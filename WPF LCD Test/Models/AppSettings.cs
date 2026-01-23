using System.Text.Json.Serialization;

namespace WPF_LCD_Test.Models
    {
    /// <summary>
    /// Represents application settings for persistence in JSON format.
    /// </summary>
    public class AppSettings
        {
        /// <summary>
        /// Gets or sets the application language culture code (e.g., "en", "zh-Hans").
        /// Empty string represents default culture.
        /// </summary>
        [JsonPropertyName("languageCultureCode")]
        public string LanguageCultureCode { get; set; } = "";

        /// <summary>
        /// Gets or sets the color analyzer channel number (0-99).
        /// </summary>
        [JsonPropertyName("colorAnalyzerChannel")]
        public string ColorAnalyzerChannel { get; set; } = "0";

        /// <summary>
        /// Gets or sets whether automatic connection to color analyzer is enabled on startup.
        /// </summary>
        [JsonPropertyName("autoConnectEnabled")]
        public bool AutoConnectEnabled { get; set; } = true;
        }
    }