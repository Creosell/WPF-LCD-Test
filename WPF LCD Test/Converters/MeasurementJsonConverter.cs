using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using WPF_LCD_Test.Models;
using WPF_LCD_Test.Services;

namespace WPF_LCD_Test.Converters
{
    public class MeasurementJsonConverter : JsonConverter<Measurement>
    {
        public override Measurement Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException("Deserialization not implemented for MeasurementJsonConverter.");
        }

        public override void Write(Utf8JsonWriter writer, Measurement value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("Location", value.Location);
            writer.WriteNumber("x", Math.Round(value.x, 3, MidpointRounding.AwayFromZero));
            writer.WriteNumber("y", Math.Round(value.y, 3, MidpointRounding.AwayFromZero));

            int LvPrecision = value.Location.Equals(MeasurementLocation.BlackColor.ToString())? 8 : 1;
            writer.WriteNumber("Lv", Math.Round(value.Lv, LvPrecision, MidpointRounding.AwayFromZero));
            writer.WriteNumber("T", Math.Round(value.T, 0, MidpointRounding.AwayFromZero));

            writer.WriteEndObject();
        }
    }
}