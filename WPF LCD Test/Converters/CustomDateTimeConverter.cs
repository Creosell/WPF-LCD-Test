using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WPF_LCD_Test.Converters
{
    public class CustomDateTimeConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                string? dateString = reader.GetString();
                if (dateString != null && DateTime.TryParseExact(dateString, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
                    return date;
                throw new JsonException($"Unable to parse '{dateString}' to DateTime with expected format.");
            }
            throw new JsonException($"Expected string token for DateTime, got {reader.TokenType}.");
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            string formattedDate = value.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
            writer.WriteStringValue(formattedDate);
        }
    }
}