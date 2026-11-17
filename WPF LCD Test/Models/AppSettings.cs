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
        /// Gets or sets the synchronization mode applied to the color analyzer during zero calibration.
        /// </summary>
        [JsonPropertyName("syncMode")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public SyncMode SyncMode { get; set; } = SyncMode.Universal;

        /// <summary>
        /// Gets or sets the internal synchronization frequency in Hz; null selects the default.
        /// </summary>
        [JsonPropertyName("syncFrequency")]
        public double? SyncFrequency { get; set; }

        /// <summary>
        /// Gets or sets the manual integration time in ms; null selects the default.
        /// </summary>
        [JsonPropertyName("integrationTime")]
        public double? IntegrationTime { get; set; }

        /// <summary>
        /// Builds the synchronization setting (mode with the parameter it needs) from the stored values.
        /// </summary>
        public SyncSetting ToSyncSetting() => SyncMode switch
            {
            SyncMode.Internal => new(SyncMode, SyncFrequency ?? AnalyzerCapabilities.DefaultFrequency),
            SyncMode.Manual => new(SyncMode, IntegrationTime ?? AnalyzerCapabilities.DefaultIntegrationTime),
            _ => new(SyncMode)
            };
        }
    }