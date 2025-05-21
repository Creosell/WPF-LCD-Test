// В папке Models
// Файл Measurement.cs

using System.Globalization; // Для InvariantCulture
using System.Text.Json.Serialization;
using WPF_LCD_Test.Converters;

namespace WPF_LCD_Test.Models
{
    // Делаем класс публичным
    [JsonConverter(typeof(MeasurementJsonConverter))]
    public class Measurement
    {
        public string Location { get; set; }
#pragma warning disable IDE1006 // Naming Styles
        public double x { get; set; }

#pragma warning disable IDE1006 // Naming Styles
        public double y { get; set; }

                               //[JsonIgnore]
        public double Lv { get; set; }
        public double T { get; set; }

        [JsonIgnore]
        public bool IsValid { get; set; } = true; // Устанавливаем по умолчанию true


        public Measurement(string location, double x, double y, double Lv, double T)
        {
            Location = location;
            this.x = x;
            this.y = y;
            this.Lv = Lv;
            this.T = T;
            IsValid = true; // По умолчанию считаем валидным при создании через этот конструктор
        }

        public Measurement()
        {
            Location = "Unknown"; // Значение по умолчанию
            IsValid = true;
        }

        // Метод для получения строкового представления
        public override string ToString()
        {
            // Применяем Math.Round с MidpointRounding.AwayFromZero перед форматированием
            // Это обеспечит округление .5 вверх, как в MeasurementJsonConverter
            string formattedX = Math.Round(x, 3, MidpointRounding.AwayFromZero).ToString("F3", CultureInfo.InvariantCulture);
            string formattedY = Math.Round(y, 3, MidpointRounding.AwayFromZero).ToString("F3", CultureInfo.InvariantCulture);
            string formattedLv = Math.Round(Lv, 1, MidpointRounding.AwayFromZero).ToString("F1", CultureInfo.InvariantCulture);
            string formattedT = Math.Round(T, 0, MidpointRounding.AwayFromZero).ToString("F0", CultureInfo.InvariantCulture);

            return $"Location: {Location}, x: {formattedX}, y: {formattedY}, Lv: {formattedLv}, T: {formattedT}";
        }

        // Метод для форматирования специально для CSV
        public string ToCsvString()
        {
            // Используем InvariantCulture для точки в качестве разделителя
            return $"{Location},{x.ToString(CultureInfo.InvariantCulture)},{y.ToString(CultureInfo.InvariantCulture)},{Lv.ToString(CultureInfo.InvariantCulture)},{T.ToString(CultureInfo.InvariantCulture)}";
            // Возможно, нужно явно указать форматы precisionThreeDigits и т.п. как ты делал раньше, если они важны для CSV
            // return $"{Location},{X.ToString("F3", CultureInfo.InvariantCulture)},{Y.ToString("F3", CultureInfo.InvariantCulture)},{Lv.ToString("F4", CultureInfo.InvariantCulture)},{T.ToString("F0", CultureInfo.InvariantCulture)}";
        }

    }
}