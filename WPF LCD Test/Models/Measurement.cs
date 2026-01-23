using System.Globalization;
using System.Text.Json.Serialization;
using WPF_LCD_Test.Converters;

namespace WPF_LCD_Test.Models
    {
    /// <summary>
    /// Represents a color measurement with chromaticity coordinates, luminance, and color temperature.
    /// </summary>
    [JsonConverter(typeof(MeasurementJsonConverter))]
    public class Measurement
        {
        /// <summary>
        /// Gets or sets the measurement location identifier.
        /// </summary>
        public string Location { get; set; }

        /// <summary>
        /// Gets or sets the x chromaticity coordinate.
        /// </summary>
#pragma warning disable IDE1006
        public double x { get; set; }

        /// <summary>
        /// Gets or sets the y chromaticity coordinate.
        /// </summary>
        public double y { get; set; }
#pragma warning restore IDE1006

        /// <summary>
        /// Gets or sets the luminance value in cd/m².
        /// </summary>
        [JsonIgnore]
        public double Lv { get; set; }

        /// <summary>
        /// Gets or sets the color temperature in Kelvin.
        /// </summary>
        public double T { get; set; }

        /// <summary>
        /// Gets or sets whether the measurement is valid.
        /// </summary>
        [JsonIgnore]
        public bool IsValid { get; set; } = true;

        /// <summary>
        /// Initializes a new instance of Measurement with specified values.
        /// </summary>
        /// <param name="location">Measurement location identifier.</param>
        /// <param name="x">X chromaticity coordinate.</param>
        /// <param name="y">Y chromaticity coordinate.</param>
        /// <param name="Lv">Luminance value in cd/m².</param>
        /// <param name="T">Color temperature in Kelvin.</param>
        public Measurement(string location, double x, double y, double Lv, double T)
            {
            Location = location;
            this.x = x;
            this.y = y;
            this.Lv = Lv;
            this.T = T;
            }

        /// <summary>
        /// Initializes a new instance of Measurement with default location.
        /// </summary>
        public Measurement()
            {
            Location = "Unknown";
            }

        /// <summary>
        /// Returns formatted string representation of measurement with rounded values.
        /// </summary>
        /// <returns>Formatted measurement string.</returns>
        public override string ToString()
            {
            string formattedX = Math.Round(x, 3, MidpointRounding.AwayFromZero).ToString("F3", CultureInfo.InvariantCulture);
            string formattedY = Math.Round(y, 3, MidpointRounding.AwayFromZero).ToString("F3", CultureInfo.InvariantCulture);
            string formattedLv = Math.Round(Lv, 1, MidpointRounding.AwayFromZero).ToString("F1", CultureInfo.InvariantCulture);
            string formattedT = Math.Round(T, 0, MidpointRounding.AwayFromZero).ToString("F0", CultureInfo.InvariantCulture);
            return $"Location: {Location}, x: {formattedX}, y: {formattedY}, Lv: {formattedLv}, T: {formattedT}";
            }

        /// <summary>
        /// Converts measurement to CSV format with invariant culture.
        /// </summary>
        /// <returns>CSV formatted string.</returns>
        public string ToCsvString()
            {
            return $"{Location},{x.ToString(CultureInfo.InvariantCulture)}," +
                $"{y.ToString(CultureInfo.InvariantCulture)}," +
                $"{Lv.ToString(CultureInfo.InvariantCulture)}," +
                $"{T.ToString(CultureInfo.InvariantCulture)}";
            }
        }
    }