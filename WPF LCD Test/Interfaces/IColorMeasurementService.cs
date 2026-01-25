using WPF_LCD_Test.Models;

namespace WPF_LCD_Test.Interfaces
{
    /// <summary>
    /// Interface for color measurement device operations with Konica Minolta CA-200/CA-310 analyzers.
    /// </summary>
    public interface IColorMeasurementService : IDisposable
    {
        /// <summary>
        /// Gets whether the color measurement device is connected.
        /// </summary>
        bool IsDeviceConnected { get; }

        /// <summary>
        /// Gets whether the color measurement device is calibrated.
        /// </summary>
        bool IsDeviceCalibrated { get; }

        /// <summary>
        /// Gets or sets the current measurement channel (0-99).
        /// </summary>
        int CurrentChannel { get; set; }

        /// <summary>
        /// Gets the probe serial number.
        /// </summary>
        string ProbeSN { get; }

        /// <summary>
        /// Asynchronously connects to the color measurement device.
        /// </summary>
        /// <returns>True if connection successful; otherwise, false.</returns>
        Task<bool> ConnectAsync();

        /// <summary>
        /// Disconnects from the color measurement device.
        /// </summary>
        void Disconnect();

        /// <summary>
        /// Asynchronously performs zero calibration on the device.
        /// </summary>
        /// <returns>True if calibration successful; otherwise, false.</returns>
        Task<bool> CalibrateZeroAsync();

        /// <summary>
        /// Asynchronously measures color values.
        /// </summary>
        /// <param name="measurmentTime">Measurement duration in seconds.</param>
        /// <returns>Measurement result containing chromaticity and luminance data.</returns>
        Task<Measurement> MeasureAsync(int measurmentTime);

        /// <summary>
        /// Occurs when device connection status changes.
        /// </summary>
        event EventHandler<bool> ConnectionStatusChanged;

        /// <summary>
        /// Occurs when device calibration status changes.
        /// </summary>
        event EventHandler<bool> CalibrationStatusChanged;

        /// <summary>
        /// Occurs when a status message is generated.
        /// </summary>
        event EventHandler<string> StatusMessage;

        /// <summary>
        /// Occurs during measurement to report progress percentage.
        /// </summary>
        event EventHandler<double> MeasurementProgress;

        /// <summary>
        /// Occurs when the current channel number changes.
        /// </summary>
        event EventHandler<int> CurrentChannelChanged;
    }
}