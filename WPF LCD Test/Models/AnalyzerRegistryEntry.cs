using System.Text.Json.Serialization;

namespace WPF_LCD_Test.Models
    {
    /// <summary>
    /// Remembered state of a single color analyzer probe, keyed by its serial number.
    /// </summary>
    public class AnalyzerRegistryEntry
        {
        /// <summary>
        /// Gets or sets the probe serial number.
        /// </summary>
        public string ProbeSN { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the analyzer family the probe was last connected through.
        /// </summary>
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ColorAnalyzerModel Model { get; set; }

        /// <summary>
        /// Gets or sets the channel the inspector last confirmed for this probe.
        /// </summary>
        public int LastChannel { get; set; }

        /// <summary>
        /// Gets or sets the UTC time of the last confirmation.
        /// </summary>
        public DateTime LastSeenUtc { get; set; }
        }
    }
