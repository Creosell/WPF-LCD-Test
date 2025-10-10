using System;
using System.Text.Json.Serialization;
using WPF_LCD_Test.Converters;
using static WPF_LCD_Test.Resources.Resources;

namespace WPF_LCD_Test.Models
{
    public class DeviceUnderTest
    {
        public string SerialNumber { get; set; }
        public string DeviceConfiguration { get; set; } = "DefaultConfig";
        public bool IsTV { get; set; }
        [JsonConverter(typeof(CustomDateTimeConverter))]
        public DateTime MeasurementDateTime { get; set; }
        public List<Measurement> Measurements { get; set; }

        public DeviceUnderTest(string serialNumber)
        {
            if (string.IsNullOrWhiteSpace(serialNumber))
                throw new ArgumentException($"{SnCantBeEmpty}", nameof(serialNumber));
            SerialNumber = serialNumber;
            MeasurementDateTime = DateTime.Now;
            Measurements = new List<Measurement>();
        }

        public void AddMeasurement(Measurement newMeasurement)
        {
            ArgumentNullException.ThrowIfNull(newMeasurement);
            Measurements.RemoveAll(m => m.Location == newMeasurement.Location);
            Measurements.Add(newMeasurement);
        }

        public override string ToString()
        {
            var measurementsStr = Measurements?.Any() == true
                ? string.Join("; ", Measurements.Select(m => m.ToString()))
                : "None";
            return $"Device SN: {SerialNumber}, Config: {DeviceConfiguration}, IsTV: {IsTV}, Measured at: {MeasurementDateTime}, Measurements: [{measurementsStr}]";
        }
    }
}