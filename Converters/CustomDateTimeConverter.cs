// В папке Converters
// Файл CustomDateTimeConverter.cs

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Globalization; // Для CultureInfo.InvariantCulture

namespace WPF_LCD_Test.Converters // Используйте соответствующее пространство имен вашего проекта
{
    // Пользовательский конвертер для сериализации/десериализации DateTime
    public class CustomDateTimeConverter : JsonConverter<DateTime>
    {
        // Метод для чтения (десериализации) DateTime из JSON.
        // В данном случае, если вы сохраняете DateTime в специфическом строковом формате,
        // вам нужно здесь реализовать логику парсинга этой строки обратно в DateTime.
        // Если вам сейчас не нужна десериализация из JSON с этим форматом,
        // можно оставить его выброс исключения или базовую реализацию,
        // которая может не справиться с нестандартным форматом.
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Пример парсинга строки в формате "yyyy-MM-dd HH:mm:ss" обратно в DateTime
            if (reader.TokenType == JsonTokenType.String)
            {
                string? dateString = reader.GetString();
                if (dateString != null && DateTime.TryParseExact(dateString, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
                {
                    return date;
                }
                // Обработайте случаи, когда строка не соответствует ожидаемому формату
                throw new JsonException($"Не удалось распарсить строку '{dateString}' в DateTime с ожидаемым форматом.");
            }

            // Если токен не строка, попробуйте стандартную десериализацию или выбросите ошибку
            // Если стандартная десериализация DateTime по умолчанию работает для других форматов,
            // можно вызвать ее: return reader.GetDateTime();
            throw new JsonException($"Ожидался строковый токен для DateTime, но получен {reader.TokenType}.");
        }

        // Метод для записи (сериализации) DateTime в JSON.
        // Здесь мы определяем желаемый формат вывода.
        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            // !!! Форматируем DateTime в нужный строковый формат !!!
            // Пример: "yyyy-MM-dd HH:mm:ss" (год-месяц-день час:минута:секунда)
            string formattedDate = value.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

            // Записываем отформатированную строку в JSON
            writer.WriteStringValue(formattedDate);

            // Вы можете изменить строку формата "yyyy-MM-dd HH:mm:ss" на любой другой стандартный формат DateTime.
            // Например:
            // "d" - короткая дата (напр., 08/16/03)
            // "D" - длинная дата (напр., Friday, August 16, 2003)
            // "g" - общая дата/время (короткое время)
            // "G" - общая дата/время (длинное время)
            // "s" - сортируемый формат ISO 8601 ("yyyy-MM-ddTHH:mm:ss")
            // Полный список стандартных форматов: https://learn.microsoft.com/en-us/dotnet/standard/base-types/standard-date-and-time-format-strings
            // Или использовать пользовательские строки формата: "yyyy/MM/dd HH:mm", "dd.MM.yyyy" и т.д.
        }
    }
}