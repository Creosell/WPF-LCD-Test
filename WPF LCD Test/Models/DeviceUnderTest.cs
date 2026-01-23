using System.Text.Json.Serialization;
using WPF_LCD_Test.Converters;
using static WPF_LCD_Test.Resources.Resources;

namespace WPF_LCD_Test.Models
    {
    /// <summary>
    /// Represents a device under test with its measurements and configuration.
    /// </summary>
    public class DeviceUnderTest
        {
        /// <summary>
        /// Gets or sets the device serial number.
        /// </summary>
        public string SerialNumber { get; set; }

        /// <summary>
        /// Gets or sets the device configuration identifier.
        /// </summary>
        public string DeviceConfiguration { get; set; } = "DefaultConfig";

        /// <summary>
        /// Gets or sets whether the device is a TV.
        /// </summary>
        public bool IsTV { get; set; }

        /// <summary>
        /// Gets or sets the measurement timestamp.
        /// </summary>
        [JsonConverter(typeof(CustomDateTimeConverter))]
        public DateTime MeasurementDateTime { get; set; }

        /// <summary>
        /// Gets or sets the collection of measurements for this device.
        /// </summary>
        public List<Measurement> Measurements { get; set; }

        /// <summary>
        /// Initializes a new instance of DeviceUnderTest with specified serial number.
        /// </summary>
        /// <param name="serialNumber">Device serial number.</param>
        /// <exception cref="ArgumentException">Thrown when serial number is null or whitespace.</exception>
        public DeviceUnderTest(string serialNumber)
            {
            if (string.IsNullOrWhiteSpace(serialNumber))
                throw new ArgumentException($"{SnCantBeEmpty}", nameof(serialNumber));
            SerialNumber = serialNumber;
            MeasurementDateTime = DateTime.Now;
            Measurements = [];
            }

        /// <summary>
        /// Adds or replaces measurement for specific location.
        /// </summary>
        /// <param name="newMeasurement">Measurement to add.</param>
        /// <exception cref="ArgumentNullException">Thrown when measurement is null.</exception>
        public void AddMeasurement(Measurement newMeasurement)
            {
            ArgumentNullException.ThrowIfNull(newMeasurement);
            Measurements.RemoveAll(m => m.Location == newMeasurement.Location);
            Measurements.Add(newMeasurement);
            }

        /// <summary>
        /// Returns formatted string representation of device and its measurements.
        /// </summary>
        /// <returns>Formatted device information string.</returns>
        public override string ToString()
            {
            var measurementsStr = ( Measurements.Count>0 ) == true
                ? string.Join("; ", Measurements.Select(m => m.ToString()))
                : "None";
            return $"Device SN: {SerialNumber}, Config: {DeviceConfiguration}, IsTV: {IsTV}, Measured at: {MeasurementDateTime}, Measurements: [{measurementsStr}]";
            }
        }
    }