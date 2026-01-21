using MvvmHelpers;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows.Input;
using System.Runtime.InteropServices;
using WPF_LCD_Test.Interfaces;
using WPF_LCD_Test.Models;
using WPF_LCD_Test.Wrappers;
using static WPF_LCD_Test.Resources.Resources;

namespace WPF_LCD_Test.Services
{
    // Service class for handling all communications and measurements with the Color Analyzer device.
    public class ColorMeasurementService : IDisposable, IColorMeasurementService
    {
        private IColorAnalyzer200? _objCa200;
        private IColorAnalyzer? _objCa = null; // Analyzer object (e.g., SingleCa property from CA-200).
        private IColorAnalyzerMemory? _objMemory = null; // Memory object (e.g., Memory property from CA-200).
        private bool _isDeviceConnected = false;
        private bool _isDeviceCalibrated = false;
        private int _channel = 0;

        // Color analyzer constants (Remote modes)
        private const int RemoteModeOFF = 0;
        private const int RemoteModeON = 1;
        private const int RemoteModeLOCKED = 2;

        // Sync modes
        private const int NtscSync = 0; // NTSC sync.
        private const int PalSync = 1; // PAL sync.
        private const int ExtSync = 2; // EXT sync.
        private const int UniverslaSyncMode = 3; // UNIV sync.

        // Measuring rate modes
        private const int SlowMeasuringMode = 0; // Slow
        private const int FastMeasuringMode = 1; // Fast
        private const int AutoMeasuringMode = 2; // Auto

        // Analog display range constant.
        private const double DefaultDisplayRange = 2.5;

        // Display and measurement modes
        private const int LvxyDisplayMode = 0; // Mode for measuring x, y, Lv
        private const int TdudvDisplayMode = 1; // Tdudv
        private const int NoDisplayMode = 2; // Analyzer mode (no display)
        private const int GDisplayMode = 3; // Analyzer mode (G standard)
        private const int RDisplayMode = 4; // Analyzer mode (R standard)
        private const int uvDisplayMode = 5; // u'v'
        private const int FmaDisplayMode = 6; // FMA flicker - Contrast flicker method
        private const int XYZDisplayMode = 7; // XYZ
        private const int JeitaDisplayMode = 8; // JEITA flicker*2 - JEITA flicker method

        public event EventHandler<bool>? ConnectionStatusChanged;
        public event EventHandler<bool>? CalibrationStatusChanged;
        public event EventHandler<string>? StatusMessage;
        public event EventHandler<double>? MeasurementProgress;

        // Use expression body for simple properties.
        public bool IsDeviceConnected => _isDeviceConnected;
        public bool IsDeviceCalibrated => _isDeviceCalibrated;

        public int CurrentChannel
            {
            get => _channel;
            set
                {
                ChangeChannel(value);
                }
            }

        public ColorMeasurementService()
        {
        }

        // Constructor for injecting a mock/wrapper dependency.
        public ColorMeasurementService(IColorAnalyzer200 ca200Wrapper)
        {
            _objCa200 = ca200Wrapper ?? throw new ArgumentNullException(nameof(ca200Wrapper));
        }

        // Asynchronously attempts to connect to the color analyzer device.
        public async Task<bool> ConnectAsync()
        {
            await Task.Run(() =>
            {
                CheckCurrentAppLanguage();

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

        // Calls the internal Dispose method for disconnection (expression-bodied member).
        private void Disconnect() => Dispose(true);

        // Asynchronously performs a zero calibration of the device.
        public async Task<bool> CalibrateZeroAsync()
        {
            var success = false;
            await Task.Run(async () =>
            {
                CheckCurrentAppLanguage();
                try
                {
                    StatusMessage?.Invoke(this, CalibratingZeroCA);
                    if (_objCa != null)
                    {
                        _objCa.CalZero();

                        // Configure the device settings after calibration.
                        _objCa.SyncMode = UniverslaSyncMode;
                        _objCa.AveragingMode = AutoMeasuringMode;
                        _objCa.SetAnalogRange(Convert.ToSingle(DefaultDisplayRange), Convert.ToSingle(DefaultDisplayRange));
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

        private void ChangeChannel(int channel)
            {
            CheckCurrentAppLanguage();
            try
                {
                if (!_isDeviceConnected)
                    {
                    _channel = channel;
                    return;
                    }
                if (_objMemory != null)
                    {
                    _objMemory.ChannelNO = channel;
                    }
                StatusMessage?.Invoke(this, string.Format(ChannelChanged, channel));
                }
            catch (COMException ex)
                {
                StatusMessage?.Invoke(this, string.Format(ErrAtChangeChannel, ex.Message));
                }
            catch (Exception ex)
                {
                StatusMessage?.Invoke(this, string.Format(ErrAtChangeChannel, ex.Message));
                }
            }


        // Asynchronously performs a series of measurements and calculates the average.
        public async Task<Measurement> MeasureAsync(int measurementTime)
        {
            var result = new Measurement();
            await Task.Run(async () =>
            {
                CheckCurrentAppLanguage();
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

                    // Initialize arrays to store measurement results.
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
                                var probe = _objCa200.SingleCa.SingleProbe; // Cache probe for conciseness.
                                xValues[i] = probe.sx;
                                yValues[i] = probe.sy;
                                LvValues[i] = probe.Lv;
                                TValues[i] = probe.T;
                            }
                            // Report progress based on completion percentage.
                            MeasurementProgress?.Invoke(this, (double)(i + 1) / measurementTime * 100);
                        }
                        catch (COMException measureEx)
                        {
                            StatusMessage?.Invoke(this, $"{ErrorAtMeasuringIteration}{i}: {measureEx.Message}");
                            continue; // Continue to the next iteration on failure.
                        }
                        catch (Exception measureEx)
                        {
                            StatusMessage?.Invoke(this, $"{ErrorAtMeasuringIteration}{i}: {measureEx.Message}");
                            continue;
                        }

                        // Delay between measurements.
                        if (i < measurementTime - 1)
                        {
                            await Task.Delay(1000);
                        }
                    }

                    // Calculate the average of all valid measurements.
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

        // IDisposable implementation (explicit interface member).
        void IDisposable.Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // The core method for resource cleanup.
        public virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                StatusMessage?.Invoke(this, DisconnectingCA);

                if (_objCa200 != null)
                {
                    _objCa200.Dispose();
                    _objCa200 = null;
                }

                // Update connection and calibration status after disposal.
                _isDeviceConnected = false;
                ConnectionStatusChanged?.Invoke(this, _isDeviceConnected);
                _isDeviceCalibrated = false;
                CalibrationStatusChanged?.Invoke(this, _isDeviceCalibrated);
                StatusMessage?.Invoke(this, DisconnectedCA);
            }
        }

        // Finalizer calls Dispose(false) if Dispose(true) was not called.
        ~ColorMeasurementService() => Dispose(false);

        // IColorMeasurementService Disconnect implementation uses the private method (expression-bodied member).
        void IColorMeasurementService.Disconnect() => Disconnect();

        // Helper method to ensure the correct culture for resource strings on background threads.
        private static void CheckCurrentAppLanguage()
        {
            var culture = LocalizationService.Instance.CurrentCulture;

            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
        }
    }
}