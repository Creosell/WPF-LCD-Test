using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WPF_LCD_Test.Converters
{
    /// <summary>
    /// Custom JSON converter for DateTime serialization with specific format (yyyyMMdd_HHmmss).
    /// </summary>
    public class CustomDateTimeConverter : JsonConverter<DateTime>
    {
        /// <summary>
        /// Reads and converts JSON to DateTime.
        /// </summary>
        /// <param name="reader">The JSON reader.</param>
        /// <param name="typeToConvert">The type to convert.</param>
        /// <param name="options">Serialization options.</param>
        /// <returns>Parsed DateTime value.</returns>
        /// <exception cref="JsonException">Thrown when the JSON format is invalid.</exception>
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

        /// <summary>
        /// Writes DateTime value as JSON string in format yyyyMMdd_HHmmss.
        /// </summary>
        /// <param name="writer">The JSON writer.</param>
        /// <param name="value">The DateTime value to write.</param>
        /// <param name="options">Serialization options.</param>
        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            string formattedDate = value.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
            writer.WriteStringValue(formattedDate);
        }
    }
}