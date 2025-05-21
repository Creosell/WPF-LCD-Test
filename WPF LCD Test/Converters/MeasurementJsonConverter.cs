using System.Text.Json;
using System.Text.Json.Serialization;
using WPF_LCD_Test.Models; // Убедись, что пространство имен класса Measurement доступно
using WPF_LCD_Test.Services;

namespace WPF_LCD_Test.Converters // Используй соответствующее пространство имен
{
    public class MeasurementJsonConverter : JsonConverter<Measurement>
    {
        public override Measurement Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException("Десериализация пока не реализована для MeasurementJsonConverter.");
        }

        public override void Write(Utf8JsonWriter writer, Measurement value, JsonSerializerOptions options)
        {
            // Логика сериализации (записи объекта Measurement в JSON)

            writer.WriteStartObject(); // Начинаем объект JSON

            // Записываем свойство Location (всегда как строка)
            writer.WriteString("Location", value.Location);

            // Записываем свойство x (с стандартным форматированием для double, или примени DoubleJsonConverter)
            // Если ты хочешь форматирование для x и y одинаково, можно использовать DoubleJsonConverter
            // Примененный к свойству x и y в классе Measurement, если этот конвертер НЕ применен к самому классу Measurement.
            // Но если этот конвертер применяется к классу Measurement, то логика форматирования x и y должна быть здесь.
            writer.WriteNumber("x", Math.Round(value.x, 3, MidpointRounding.AwayFromZero));

            // Записываем свойство y
            writer.WriteNumber("y", Math.Round(value.y, 3, MidpointRounding.AwayFromZero));

            // --- Условное форматирование для Lv ---
            int LvPrecision = 1; // Точность по умолчанию (для F1)

            if (value.Location == MeasurementStatusManager.BlackColorLocationName) // Или "K. Black"
            {
                LvPrecision = 8; // Точность 4 знака для Black (для F4)
            }

            // Записываем свойство Lv с выбранным форматированием
            // Записываем как строку, чтобы гарантировать точное количество знаков
            writer.WriteNumber("Lv", Math.Round(value.Lv, LvPrecision, MidpointRounding.AwayFromZero));

            // Записываем свойство T (предполагаем, что это int или double без специфического форматирования)
            // Если T - double, и ему нужно стандартное форматирование, используй writer.WriteNumber или writer.WriteString с F2
            writer.WriteNumber("T", Math.Round(value.T, 0, MidpointRounding.AwayFromZero)); // Используем ToString() для получения строкового представления значения


            // TODO: Запиши остальные свойства Measurement, если они должны быть в JSON
            // Например: writer.WriteString("Timestamp", value.Timestamp);
            //          if (value.ErrorMessage != null) writer.WriteString("ErrorMessage", value.ErrorMessage);

            writer.WriteEndObject(); // Заканчиваем объект JSON
        }
    }
}