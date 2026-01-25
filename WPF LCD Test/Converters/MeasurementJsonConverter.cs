using System.Text.Json;
using System.Text.Json.Serialization;
using WPF_LCD_Test.Models;

namespace WPF_LCD_Test.Converters
{
    /// <summary>
    /// Custom JSON converter for Measurement objects with precision-based rounding.
    /// </summary>
    public class MeasurementJsonConverter : JsonConverter<Measurement>
    {
        /// <summary>
        /// Reads and converts JSON to Measurement. Not implemented as deserialization is not required.
        /// </summary>
        /// <param name="reader">The JSON reader.</param>
        /// <param name="typeToConvert">The type to convert.</param>
        /// <param name="options">Serialization options.</param>
        /// <returns>Measurement instance.</returns>
        /// <exception cref="NotImplementedException">Always thrown as deserialization is not implemented.</exception>
        public override Measurement Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException("Deserialization not implemented for MeasurementJsonConverter.");
        }

        /// <summary>
        /// Writes Measurement value as JSON with appropriate precision for each field.
        /// Black color measurements use 8 decimal places for Lv, others use 1 decimal place.
        /// </summary>
        /// <param name="writer">The JSON writer.</param>
        /// <param name="value">The Measurement value to write.</param>
        /// <param name="options">Serialization options.</param>
        public override void Write(Utf8JsonWriter writer, Measurement value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("Location", value.Location);
            writer.WriteNumber("x", Math.Round(value.x, 3, MidpointRounding.AwayFromZero));
            writer.WriteNumber("y", Math.Round(value.y, 3, MidpointRounding.AwayFromZero));

            int LvPrecision = value.Location.Equals(MeasurementLocation.BlackColor.ToString()) ? 8 : 1;
            writer.WriteNumber("Lv", Math.Round(value.Lv, LvPrecision, MidpointRounding.AwayFromZero));
            writer.WriteNumber("T", Math.Round(value.T, 0, MidpointRounding.AwayFromZero));

            writer.WriteEndObject();
        }
    }
}