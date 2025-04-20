// В папке Models
// Файл Measurement.cs

using System;
using System.Globalization; // Для InvariantCulture
using System.Text.Json.Serialization;

namespace WPF_LCD_Test.Models
{
    // Делаем класс публичным
    public class Measurement
    {

        public double x { get; set; }
        public double y { get; set; }
        public double Lv { get; set; }
        public double T { get; set; } 

    
        public string Location { get; set; } 

        [JsonIgnore]
        public bool IsValid { get; set; } = true; // Устанавливаем по умолчанию true



        public Measurement(string location, double x, double y, double lv, double t)
        {
            Location = location;
            x = x;
            y = y;
            Lv = lv;
            T = t;
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