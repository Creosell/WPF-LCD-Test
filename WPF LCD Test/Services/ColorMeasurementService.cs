using System.Runtime.InteropServices;
using WPF_LCD_Test.Interfaces;
using WPF_LCD_Test.Models;
using WPF_LCD_Test.Wrappers;
using static WPF_LCD_Test.Resources.Resources;

namespace WPF_LCD_Test.Services
    {
    /// <summary>
    /// Service for handling communications and measurements with Color Analyzer device (CA-200/CA-310).
    /// </summary>
    public class ColorMeasurementService : IDisposable, IColorMeasurementService
        {
        private IColorAnalyzer200? _objCa200;
        private IColorAnalyzer? _objCa = null;
        private IColorAnalyzerMemory? _objMemory = null;
        private bool _isDeviceConnected = false;
        private bool _isDeviceCalibrated = false;
        private int _channel = 0;
        private string _probeSN = "Unknown";

        private const int RemoteModeOFF = 0;
        private const int RemoteModeON = 1;
        private const int RemoteModeLOCKED = 2;
        private const int NtscSync = 0;
        private const int PalSync = 1;
        private const int ExtSync = 2;
        private const int UniverslaSyncMode = 3;
        private const int SlowMeasuringMode = 0;
        private const int FastMeasuringMode = 1;
        private const int AutoMeasuringMode = 2;
        private const double DefaultDisplayRange = 2.5;
        private const int LvxyDisplayMode = 0;
        private const int TdudvDisplayMode = 1;
        private const int NoDisplayMode = 2;
        private const int GDisplayMode = 3;
        private const int RDisplayMode = 4;
        private const int uvDisplayMode = 5;
        private const int FmaDisplayMode = 6;
        private const int XYZDisplayMode = 7;
        private const int JeitaDisplayMode = 8;

        /// <summary>
        /// Occurs when device connection status changes.
        /// </summary>
        public event EventHandler<bool>? ConnectionStatusChanged;

        /// <summary>
        /// Occurs when device calibration status changes.
        /// </summary>
        public event EventHandler<bool>? CalibrationStatusChanged;

        /// <summary>
        /// Occurs when status message needs to be reported.
        /// </summary>
        public event EventHandler<string>? StatusMessage;

        /// <summary>
        /// Occurs when measurement progress updates.
        /// </summary>
        public event EventHandler<double>? MeasurementProgress;

        /// <summary>
        /// Occurs when current channel changes.
        /// </summary>
        public event EventHandler<int>? CurrentChannelChanged;

        /// <summary>
        /// Gets whether device is currently connected.
        /// </summary>
        public bool IsDeviceConnected => _isDeviceConnected;

        /// <summary>
        /// Gets whether device is calibrated.
        /// </summary>
        public bool IsDeviceCalibrated => _isDeviceCalibrated;

        /// <summary>
        /// Gets the probe serial number.
        /// </summary>
        public string ProbeSN
            {
            get
                {
                if (_objCa != null)
                    {
                    _probeSN = _objCa.SingleProbe.SerialNO;
                    }
                return _probeSN;
                }
            }

        /// <summary>
        /// Gets or sets the current measurement channel (0-99).
        /// </summary>
        public int CurrentChannel
            {
            get => _channel;
            set { ChangeChannel(value); }
            }

        /// <summary>
        /// Initializes a new instance of ColorMeasurementService.
        /// </summary>
        public ColorMeasurementService()
            {
            }

        /// <summary>
        /// Initializes a new instance of ColorMeasurementService with dependency injection for testing.
        /// </summary>
        /// <param name="ca200Wrapper">Color analyzer wrapper for testing.</param>
        internal ColorMeasurementService(IColorAnalyzer200 ca200Wrapper)
            {
            _objCa200 = ca200Wrapper ?? throw new ArgumentNullException(nameof(ca200Wrapper));
            }

        /// <summary>
        /// Asynchronously attempts to connect to color analyzer device.
        /// </summary>
        /// <returns>True if connection succeeded, false otherwise.</returns>
        public async Task<bool> ConnectAsync()
            {
            await Task.Run(() =>
            {
                if (_objCa200 == null)
                    {
                    _objCa200 = new ColorAnalyzer200Wrapper();
                    }

                try
                    {
                    if (!_isDeviceConnected)
                        {
                        StatusMessage?.Invoke(this, ConnectingCA);

                        _objCa200.AutoConnect();
                        _objCa = _objCa200.SingleCa;
                        _objMemory = _objCa.Memory;

                        _isDeviceConnected = true;
                        ConnectionStatusChanged?.Invoke(this, _isDeviceConnected);
                        StatusMessage?.Invoke(this, ConnectedCA);
                        }
                    }
                catch (COMException ex)
                    {
                    StatusMessage?.Invoke(this, $"{ConnectionError}: {ex.Message}");
                    _isDeviceConnected = false;
                    ConnectionStatusChanged?.Invoke(this, _isDeviceConnected);
                    }
                catch (Exception ex)
                    {
                    StatusMessage?.Invoke(this, $"{ConnectionError}: {ex.Message}");
                    _isDeviceConnected = false;
                    ConnectionStatusChanged?.Invoke(this, _isDeviceConnected);
                    }
            });
            return _isDeviceConnected;
            }

        /// <summary>
        /// Disconnects from device by disposing resources.
        /// </summary>
        private void Disconnect() => Dispose(true);

        /// <summary>
        /// Asynchronously performs zero calibration and configures device settings.
        /// </summary>
        /// <returns>True if calibration succeeded, false otherwise.</returns>
        public async Task<bool> CalibrateZeroAsync()
            {
            var success = false;
            await Task.Run(() =>
            {
                try
                    {
                    StatusMessage?.Invoke(this, CalibratingZeroCA);
                    if (_objCa != null)
                        {
                        _objCa.CalZero();

                        _objCa.SyncMode = UniverslaSyncMode;
                        _objCa.AveragingMode = AutoMeasuringMode;
                        _objCa.SetAnalogRange(
                            Convert.ToSingle(DefaultDisplayRange),
                            Convert.ToSingle(DefaultDisplayRange)
                        );
                        _objCa.DisplayMode = LvxyDisplayMode;

                        if (_objMemory != null)
                            {
                            _objMemory.ChannelNO = CurrentChannel;
                            }
                        }

                    _isDeviceCalibrated = true;
                    CalibrationStatusChanged?.Invoke(this, _isDeviceCalibrated);
                    StatusMessage?.Invoke(this, ZeroCalibratedCA);
                    success = true;
                    }
                catch (COMException ex)
                    {
                    StatusMessage?.Invoke(this, $"{CheckConnectionCA}: {ex.Message}");
                    _isDeviceCalibrated = false;
                    CalibrationStatusChanged?.Invoke(this, _isDeviceCalibrated);
                    }
                catch (Exception ex)
                    {
                    StatusMessage?.Invoke(this, $"{ErrAtCalibration}: {ex.Message}");
                    _isDeviceCalibrated = false;
                    CalibrationStatusChanged?.Invoke(this, _isDeviceCalibrated);
                    }
            });
            return success;
            }

        /// <summary>
        /// Updates current channel and synchronizes with connected device memory if available.
        /// Channel value is stored regardless of device connection state.
        /// </summary>
        /// <param name="channel">Target channel identifier to set (0-99).</param>
        private void ChangeChannel(int channel)
            {
            if (!IsValidChannel(channel))
                return;

            _channel = channel;
            CurrentChannelChanged?.Invoke(this, _channel);

            if (!_isDeviceConnected || _objMemory == null)
                return;

            try
                {
                _objMemory.ChannelNO = channel;
                StatusMessage?.Invoke(this, string.Format(ChannelChanged, channel));
                }
            catch (Exception ex)
                {
                StatusMessage?.Invoke(this, string.Format(ErrAtChangeChannel, ex.Message));
                }
            }

        /// <summary>
        /// Validates if channel number is within acceptable range.
        /// </summary>
        /// <param name="channel">Channel number to validate.</param>
        /// <returns>True if channel is valid, false otherwise.</returns>
        private bool IsValidChannel(int channel) => channel >= AppConstants.ColorAnalyzer.MinChannel && channel <= AppConstants.ColorAnalyzer.MaxChannel;

        /// <summary>
        /// Asynchronously performs series of measurements and calculates average values.
        /// </summary>
        /// <param name="measurementTime">Number of measurements to perform.</param>
        /// <returns>Measurement object containing averaged results.</returns>
        public async Task<Measurement> MeasureAsync(int measurementTime)
            {
            var result = new Measurement();
            await Task.Run(async () =>
            {
                try
                    {
                    if (!_isDeviceConnected)
                        {
                        StatusMessage?.Invoke(this, MeasureWihoutConnectionError);
                        result.IsValid = false;
                        return;
                        }
                    if (!_isDeviceCalibrated)
                        {
                        StatusMessage?.Invoke(this, MakeZeroCalibration);
                        result.IsValid = false;
                        return;
                        }

                    StatusMessage?.Invoke(this, Measuring);

                    var xValues = new double[measurementTime];
                    var yValues = new double[measurementTime];
                    var LvValues = new double[measurementTime];
                    var TValues = new double[measurementTime];

                    for (var i = 0; i < measurementTime; i++)
                        {
                        try
                            {
                            if (_objCa200 != null)
                                {
                                _objCa200.SingleCa.Measure();
                                var probe = _objCa200.SingleCa.SingleProbe;
                                xValues[i] = probe.sx;
                                yValues[i] = probe.sy;
                                LvValues[i] = probe.Lv;
                                TValues[i] = probe.T;
                                }
                            MeasurementProgress?.Invoke(this, (double)( i + 1 ) / measurementTime * 100);
                            }
                        catch (COMException measureEx)
                            {
                            StatusMessage?.Invoke(this, $"{ErrorAtMeasuringIteration}{i}: {measureEx.Message}");
                            continue;
                            }
                        catch (Exception measureEx)
                            {
                            StatusMessage?.Invoke(this, $"{ErrorAtMeasuringIteration}{i}: {measureEx.Message}");
                            continue;
                            }

                        if (i < measurementTime - 1)
                            {
                            await Task.Delay(1000);
                            }
                        }

                    result.x = xValues.Average();
                    result.y = yValues.Average();
                    result.Lv = LvValues.Average();
                    result.T = TValues.Average();
                    result.IsValid = true;
                    }
                catch (Exception ex)
                    {
                    StatusMessage?.Invoke(this, $"{ErrUnexpected}: {ex.Message}");
                    result.IsValid = false;
                    }
            });

            return result;
            }

        /// <summary>
        /// Releases all resources used by ColorMeasurementService.
        /// </summary>
        void IDisposable.Dispose()
            {
            Dispose(true);
            GC.SuppressFinalize(this);
            }

        /// <summary>
        /// Releases unmanaged and managed resources.
        /// </summary>
        /// <param name="disposing">True to release both managed and unmanaged resources.</param>
        public virtual void Dispose(bool disposing)
            {
            if (disposing)
                {

                if (_objCa200 != null)
                    {
                    _objCa200.Dispose();
                    _objCa200 = null;
                    }

                _isDeviceConnected = false;
                ConnectionStatusChanged?.Invoke(this, _isDeviceConnected);
                _isDeviceCalibrated = false;
                CalibrationStatusChanged?.Invoke(this, _isDeviceCalibrated);
                StatusMessage?.Invoke(this, DisconnectedCA);
                }
            }

        /// <summary>
        /// Finalizer for ColorMeasurementService.
        /// </summary>
        ~ColorMeasurementService() => Dispose(false);

        /// <summary>
        /// Disconnects from color analyzer device.
        /// </summary>
        void IColorMeasurementService.Disconnect() => Disconnect();
        }
    }