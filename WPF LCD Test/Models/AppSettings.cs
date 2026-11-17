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
        /// Gets or sets the measurement speed applied to the color analyzer during zero calibration.
        /// </summary>
        [JsonPropertyName("measurementSpeed")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public MeasurementSpeed MeasurementSpeed { get; set; } = MeasurementSpeed.Slow;

        /// <summary>
        /// Gets or sets the internal synchronization frequency in Hz; null selects the universal (100 ms) synchronization.
        /// </summary>
        [JsonPropertyName("syncFrequency")]
        public int? SyncFrequency { get; set; }
        }
    }