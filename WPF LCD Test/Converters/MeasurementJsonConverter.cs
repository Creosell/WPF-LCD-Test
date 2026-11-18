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
            WriteRounded(writer, "x", value.x, 3);
            WriteRounded(writer, "y", value.y, 3);

            int LvPrecision = value.Location.Equals(MeasurementLocation.BlackColor.ToString()) ? 8 : 1;
            WriteRounded(writer, "Lv", value.Lv, LvPrecision);
            WriteRounded(writer, "T", value.T, 0);

            if (value.Attempts.Count > 1)
            {
                WriteAttempts(writer, value, LvPrecision);
            }

            writer.WriteEndObject();
        }

        /// <summary>
        /// Writes every attempt of the point with its time and the flag of the one the measurement holds.
        /// </summary>
        /// <param name="writer">The JSON writer.</param>
        /// <param name="value">The measurement with the attempts.</param>
        /// <param name="lvPrecision">Number of fractional digits of the luminance.</param>
        private static void WriteAttempts(Utf8JsonWriter writer, Measurement value, int lvPrecision)
        {
            writer.WriteStartArray("Attempts");
            for (int i = 0; i < value.Attempts.Count; i++)
            {
                var attempt = value.Attempts[i];
                writer.WriteStartObject();
                writer.WriteString("Time", attempt.Time.ToString("yyyyMMdd_HHmmss", System.Globalization.CultureInfo.InvariantCulture));
                WriteRounded(writer, "x", attempt.x, 3);
                WriteRounded(writer, "y", attempt.y, 3);
                WriteRounded(writer, "Lv", attempt.Lv, lvPrecision);
                WriteRounded(writer, "T", attempt.T, 0);
                writer.WriteBoolean("Best", i == value.BestAttemptIndex);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }

        /// <summary>
        /// Writes a rounded number property; non-finite values (e.g. undefined color temperature reported as Infinity) are written as null, as JSON cannot represent them.
        /// </summary>
        /// <param name="writer">The JSON writer.</param>
        /// <param name="name">Property name.</param>
        /// <param name="value">Value to write.</param>
        /// <param name="digits">Number of fractional digits to round to.</param>
        private static void WriteRounded(Utf8JsonWriter writer, string name, double value, int digits)
        {
            if (double.IsFinite(value))
                writer.WriteNumber(name, Math.Round(value, digits, MidpointRounding.AwayFromZero));
            else
                writer.WriteNull(name);
        }
    }
}