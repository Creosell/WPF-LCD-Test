
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using WPF_LCD_Test.Models; // Убедись, что пространство имен класса Measurement доступно

namespace WPF_LCD_Test.Converters // Используй соответствующее пространство имен
{
    public class MeasurementJsonConverter : JsonConverter<Measurement>
    {
        public override Measurement Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Логика десериализации (чтения JSON обратно в объект Measurement)
            // Для простоты, если JSON соответствует структуре класса Measurement,
            // можно использовать стандартную десериализацию, если форматирование в Write не сильно меняет структуру.
            // Если Write записывает числа как строки, Read должен уметь их читать как строки и парсить в числа.

            // В данном случае, так как мы форматируем double как строку,
            // стандартный десериализатор может не справиться.
            // Более надежный Read метод должен вручную прочитать каждый ключ-значение из JSON
            // и распарсить строковые представления чисел обратно в double/int.
            // Это может быть довольно многословным.

            // Для начала попробуем использовать стандартную десериализацию,
            // предполагая, что Write записывает в формате, который Read может прочитать.
            // Если возникнут ошибки при чтении, этот метод придется доработать.

            // Читаем весь объект как стандартный JSON и позволяем стандартному сериализатору
            // (или другим примененным конвертерам свойств, если они есть)
            // обработать внутренности.
            // НО! Если этот конвертер применяется к самому классу Measurement,
            // стандартная десериализация внутри него НЕ БУДЕТ использовать его же.

            // Лучше всего реализовать Read вручную или использовать обходные пути.
            // Реализация Read требует вручную читать JSON токены.
            // Это выходит за рамки простой демонстрации форматирования.

            // Если десериализация не является твоей текущей задачей,
            // можно пока оставить базовую реализацию (которая может не работать корректно
            // для JSON, сгенерированного этим же Write методом с строковым форматированием чисел).

            throw new NotImplementedException("Десериализация пока не реализована для MeasurementJsonConverter.");

            // Пример очень упрощенной ручной десериализации (требует доработки):
            // if (reader.TokenType != JsonTokenType.StartObject) { throw new JsonException(); }
            // var measurement = new Measurement();
            // while (reader.Read())
            // {
            //     if (reader.TokenType == JsonTokenType.EndObject) break;
            //     if (reader.TokenType != JsonTokenType.PropertyName) { throw new JsonException(); }
            //     string propertyName = reader.GetString();
            //     reader.Read(); // Читаем значение
            //     switch (propertyName)
            //     {
            //         case "Location": measurement.Location = reader.GetString(); break;
            //         case "x": measurement.x = double.Parse(reader.GetString()); break; // Нужен парсинг, если Write пишет как строку
            //         // ... и так далее для других свойств ...
            //     }
            // }
            // return measurement;
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
            writer.WriteString("x", value.x.ToString("F3")); 
            // Записываем свойство y
            writer.WriteString("y", value.y.ToString("F3"));

            // --- Условное форматирование для Lv ---
            string LvFormat = "F1"; // Формат по умолчанию для Lv (2 знака)

            // Проверяем значение Location
            if (value.Location == "0. Black") // Или "K. Black", зависит от того, как ты его инициализируешь
            {
                LvFormat = "F4"; // Устанавливаем формат 4 знака для Black
            }

            // Записываем свойство Lv с выбранным форматированием
            // Записываем как строку, чтобы гарантировать точное количество знаков
            writer.WriteString("Lv", value.Lv.ToString(LvFormat));

            // Записываем свойство T (предполагаем, что это int или double без специфического форматирования)
            // Если T - double, и ему нужно стандартное форматирование, используй writer.WriteNumber или writer.WriteString с F2
            writer.WriteNumber("T", value.T);

            // TODO: Запиши остальные свойства Measurement, если они должны быть в JSON
            // Например: writer.WriteString("Timestamp", value.Timestamp);
            //          if (value.ErrorMessage != null) writer.WriteString("ErrorMessage", value.ErrorMessage);

            writer.WriteEndObject(); // Заканчиваем объект JSON
        }
    }
}