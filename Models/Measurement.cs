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

        //[JsonPropertyName("Lv")] // Указываем, что в JSON это свойство должно называться "Lv"
        //public string FormattedLvValue // Используем другое имя для свойства внутри класса
        //{
        //    get
        //    {
        //        // Применяем логику форматирования
        //        if (Location == "Black")
        //        {
        //            return Lv.ToString("F4", CultureInfo.InvariantCulture);
        //        }
        //        else
        //        {
        //            return Lv.ToString("F1", CultureInfo.InvariantCulture);
        //        }
        //    }
        //    // Сеттер не нужен, если свойство только для сериализации.
        //}


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

        // Метод для получения строкового представления (например, для лога или отладки)
        // Это ОБЩЕЕ строковое представление, не привязанное к CSV
        public override string ToString()
        {
            // Используем формат "F2" для примера, можно настроить
            // Используем InvariantCulture для точки в качестве разделителя
            return $"Location: {Location}, x: {x.ToString("F3", CultureInfo.InvariantCulture)}, y: {y.ToString("F3", CultureInfo.InvariantCulture)}, Lv: {Lv.ToString("F1", CultureInfo.InvariantCulture)}, T: {T.ToString("F0", CultureInfo.InvariantCulture)}";
        }

        // Метод для форматирования специально для CSV
        public string ToCsvString()
        {
            // Используем InvariantCulture для точки в качестве разделителя
            return $"{Location},{x.ToString(CultureInfo.InvariantCulture)},{y.ToString(CultureInfo.InvariantCulture)},{Lv.ToString(CultureInfo.InvariantCulture)},{T.ToString(CultureInfo.InvariantCulture)}";
            // Возможно, нужно явно указать форматы precisionThreeDigits и т.п. как ты делал раньше, если они важны для CSV
            // return $"{Location},{X.ToString("F3", CultureInfo.InvariantCulture)},{Y.ToString("F3", CultureInfo.InvariantCulture)},{Lv.ToString("F4", CultureInfo.InvariantCulture)},{T.ToString("F0", CultureInfo.InvariantCulture)}";
        }

        // Метод SetValues можно удалить, так как конструктор и публичные сеттеры уже позволяют установить значения.
    }
}