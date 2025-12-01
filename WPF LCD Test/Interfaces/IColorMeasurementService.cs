using WPF_LCD_Test.Models;

namespace WPF_LCD_Test.Interfaces
{
    public interface IColorMeasurementService : IDisposable
    {
        bool IsDeviceConnected { get; }
        bool IsDeviceCalibrated { get; }
        Task<bool> ConnectAsync();
        void Disconnect();
        Task<bool> CalibrateZeroAsync();
        Task<Measurement> MeasureAsync(int measurmentTime);
        event EventHandler<bool> ConnectionStatusChanged;
        event EventHandler<bool> CalibrationStatusChanged;
        event EventHandler<string> StatusMessage;
        event EventHandler<double> MeasurementProgress;
    }
}