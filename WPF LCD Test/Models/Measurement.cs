using System.Globalization;
using System.Text.Json.Serialization;
using WPF_LCD_Test.Converters;

namespace WPF_LCD_Test.Models
{
    [JsonConverter(typeof(MeasurementJsonConverter))]
    public class Measurement
    {
        public string Location { get; set; }
#pragma warning disable IDE1006 // Naming Styles
        public double x { get; set; }
        public double y { get; set; }
#pragma warning restore IDE1006
        [JsonIgnore]
        public double Lv { get; set; }
        public double T { get; set; }
        [JsonIgnore]
        public bool IsValid { get; set; } = true;

        public Measurement(string location, double x, double y, double Lv, double T)
        {
            Location = location;
            this.x = x;
            this.y = y;
            this.Lv = Lv;
            this.T = T;
        }

        public Measurement()
        {
            Location = "Unknown";
        }

        public override string ToString()
        {
            string formattedX = Math.Round(x, 3, MidpointRounding.AwayFromZero).ToString("F3", CultureInfo.InvariantCulture);
            string formattedY = Math.Round(y, 3, MidpointRounding.AwayFromZero).ToString("F3", CultureInfo.InvariantCulture);
            string formattedLv = Math.Round(Lv, 1, MidpointRounding.AwayFromZero).ToString("F1", CultureInfo.InvariantCulture);
            string formattedT = Math.Round(T, 0, MidpointRounding.AwayFromZero).ToString("F0", CultureInfo.InvariantCulture);
            return $"Location: {Location}, x: {formattedX}, y: {formattedY}, Lv: {formattedLv}, T: {formattedT}";
        }

        public string ToCsvString()
        {
            return $"{Location},{x.ToString(CultureInfo.InvariantCulture)},{y.ToString(CultureInfo.InvariantCulture)},{Lv.ToString(CultureInfo.InvariantCulture)},{T.ToString(CultureInfo.InvariantCulture)}";
        }
    }
}