// В папке Interfaces
// Файл ColorMeasurementService.cs (реализация IColorMeasurementService)

using System.Globalization;
using System.Runtime.InteropServices;
using CA200SRVRLib;
using WPF_LCD_Test.Models;
using static WPF_LCD_Test.Resources.Resources;
using WPF_LCD_Test.Interfaces;

namespace WPF_LCD_Test.Services
{
    public class ColorMeasurementService : IDisposable, IColorMeasurementService
    {
        private Ca200? _objCa200 = null;
        private Ca? _objCa = null;
        private bool _isDeviceConnected = false;
        private bool _isDeviceCalibrated = false;

        //Color analyzer constants
        //Remote modes
        private const int RemoteModeOFF = 0;

        private const int RemoteModeON = 1;
        private const int RemoteModeLOCKED = 2;

        //Sync modes
        private const int NtscSync = 0; //NTSC sync.

        private const int PalSync = 1; //PAL sync.
        private const int ExtSync = 2; //EXT sync.
        private const int UniverslaSyncMode = 3; //UNIV sync.

        //Measuring rate modes
        private const int SlowMeasuringMode = 0; //

        private const int FastMeasuringMode = 1; //Fast
        private const int AutoMeasuringMode = 2; //Auto

        //Analog display range
        //This value sets the display range of the targeted CA-310 unit's analog display
        //Available to set the range every 1 steps among 10 to 99 and every 0.1 steps among 0.1 to 9.9
        private const double DefaultDisplayRange = 2.5; // Analog display range 2.5% units.

        //Display and measurement modes
        private const int LvxyDisplayMode = 0; //Mode for measuring x, y, Lv

        private const int TdudvDisplayMode = 1; //Tdudv
        private const int NoDisplayMode = 2; //Analyzer mode (no display)
        private const int GDisplayMode = 3; //Analyzer mode (G standard)
        private const int RDisplayMode = 4; //Analyzer mode (R standard)
        private const int uvDisplayMode = 5; //u'v'
        private const int FmaDisplayMode = 6; //FMA flicker - Contrast flicker method
        private const int XYZDisplayMode = 7; //XYZ
        private const int JeitaDisplayMode = 8; //JEITA flicker*2 - JEITA flicker method

        //Memory channels
        //This property selects a memory channel for the CA-200 unit, or returns the current selection
        //Note that the property's channel argument identifies the channel by its channel number on the CA-310 unit.
        //Range setting:0~99ch
        private const int ZeroChannel = 0; //Konica Minolta calibration memory channel

        public event EventHandler<bool>? ConnectionStatusChanged;

        public event EventHandler<bool>? CalibrationStatusChanged;

        public event EventHandler<string>? StatusMessage;

        public event EventHandler<double>? MeasurementProgress;

        public bool IsDeviceConnected => _isDeviceConnected;

        public bool IsDeviceCalibrated => _isDeviceCalibrated;

        public ColorMeasurementService()
        {
            
        }

        private double? GetMeasuredSx() => _objCa200?.SingleCa.SingleProbe.sx;

        private double? GetMeasuredSy() => _objCa200?.SingleCa.SingleProbe.sy;

        private double? GetMeasuredLv() => _objCa200?.SingleCa.SingleProbe.Lv;

        private double? GetMeasuredT() => _objCa200?.SingleCa.SingleProbe.T;

        public async Task<bool> ConnectAsync()

        {
            await Task.Run(() => // Выполняем потенциально блокирующий COM вызов в фоновом потоке
            {
                CheckCurrentAppLanguage(); // Проверяем текущую культуру приложения

                try
                {
                    _objCa200 ??= new Ca200();

                    if (!_isDeviceConnected)
                    {
                        StatusMessage?.Invoke(this, ConnectingCA);

                        _objCa200.AutoConnect(); // Блокирующий вызов COM
                        _objCa = _objCa200.SingleCa;

                        _isDeviceConnected = true;
                        ConnectionStatusChanged?.Invoke(this, _isDeviceConnected); // Оповещаем ViewModel об изменении статуса
                        StatusMessage?.Invoke(this, ConnectedCA); // Отправляем сообщение
                    }
                }
                catch (COMException ex)
                {
                    StatusMessage?.Invoke(this, $"{ConnectionError}:    {ex.Message}"); // Отправляем ошибку
                    _isDeviceConnected = false; // Обновляем статус
                    ConnectionStatusChanged?.Invoke(this, _isDeviceConnected); // Оповещаем ViewModel
                }
                catch (Exception ex) // Ловим другие возможные исключения
                {
                    StatusMessage?.Invoke(this, $"{ConnectionError}: {ex.Message}");
                    _isDeviceConnected = false;
                    ConnectionStatusChanged?.Invoke(this, _isDeviceConnected);
                }
                ///////TEST
                //finally
                //{

                //    _isDeviceConnected = true;
                //    ConnectionStatusChanged?.Invoke(this, _isDeviceConnected);
                //}
            });
            return _isDeviceConnected; // Возвращаем статус подключения
        }

        private void Disconnect()
        {
            Dispose(true);
        }

        private static void CheckCurrentAppLanguage()
        {
            CultureInfo culture = LocalizationService.Instance.CurrentCulture; // Получаем текущую культуру из сервиса локализации

            // Устанавливаем эту культуру для текущего потока из пула
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
        }

        public async Task<bool> CalibrateZeroAsync()
        {
            bool success = false;
            await Task.Run(() =>
            {
                CheckCurrentAppLanguage(); // Проверяем текущую культуру приложения
                try
                {
                    StatusMessage?.Invoke(this, (CalibratingZeroCA)); // Сообщение
                    if (_objCa200 != null)
                    {
                        _objCa200.SingleCa.CalZero(); // Блокирующий вызов COM

                        _objCa200.SingleCa.SyncMode = (int)UniverslaSyncMode;
                        _objCa200.SingleCa.AveragingMode = (int)AutoMeasuringMode;
                        _objCa200.SingleCa.SetAnalogRange(Convert.ToSingle(DefaultDisplayRange), Convert.ToSingle(DefaultDisplayRange));
                        _objCa200.SingleCa.DisplayMode = (int)LvxyDisplayMode;
                        _objCa200.SingleCa.Memory.ChannelNO = (int)ZeroChannel;
                    }
                    _isDeviceCalibrated = true; // Обновляем статус
                    CalibrationStatusChanged?.Invoke(this, _isDeviceCalibrated); // Оповещаем
                    StatusMessage?.Invoke(this, (ZeroCalibratedCA)); // Сообщение
                    success = true; // Успех
                }
                catch (COMException ex) // Ловим ошибки COM
                {
                    StatusMessage?.Invoke(this, (CheckConnectionCA) + $": {ex.Message}");
                    _isDeviceCalibrated = false; // Обновляем статус
                    CalibrationStatusChanged?.Invoke(this, _isDeviceCalibrated); // Оповещаем
                                                                           // Не пробрасываем исключение, обрабатываем внутри сервиса
                }
                catch (Exception ex)
                {  // Ловим другие ошибки
                    StatusMessage?.Invoke(this, (ErrAtCalibration) + $": {ex.Message}");
                    _isDeviceCalibrated = false;
                    CalibrationStatusChanged?.Invoke(this, _isDeviceCalibrated);
                }
                //finally //////////TEST
                //{
                //    _isDeviceCalibrated = true;
                //    CalibrationStatusChanged?.Invoke(this, _isDeviceCalibrated);
                //}
            }); // Конец Task.Run
            return success;
        }

        void IDisposable.Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                StatusMessage?.Invoke(this, (DisconnectingCA)); // Сообщение
                if (_objCa != null)
                {
                    try { Marshal.ReleaseComObject(_objCa); } catch { }
                    _objCa = null;
                }

                if (_objCa200 != null)
                {
                    try
                    { Marshal.ReleaseComObject(_objCa200); }
                    catch { }
                    _objCa200 = null;
                }

                _isDeviceConnected = false;
                ConnectionStatusChanged?.Invoke(this, _isDeviceConnected); // Оповещаем
                _isDeviceCalibrated = false;
                CalibrationStatusChanged?.Invoke(this, _isDeviceCalibrated); // Оповещаем
                StatusMessage?.Invoke(this, (DisconnectedCA)); // Сообщение
            }
            
        }

        ~ColorMeasurementService()
        {
            Dispose(false);
        }

        public async Task<Measurement> MeasureAsync(int measurementTime)
        {
            Measurement result = new(); // Создаем объект результата
            await Task.Run(async () => // Выполняем в фоновом потоке
            {
                CheckCurrentAppLanguage(); // Проверяем текущую культуру приложения
                try
                {
                    if (!_isDeviceConnected)
                    {
                        StatusMessage?.Invoke(this, (MeasureWihoutConnectionError));
                        result.IsValid = false; // Отмечаем результат как невалидный
                        return; // Выходим из лямбды
                    }
                    if (!_isDeviceCalibrated) // Проверяем калибровку
                    {
                        StatusMessage?.Invoke(this, (MakeZeroCalibration));
                        result.IsValid = false; // Отмечаем результат как невалидный
                        return; // Выходим из лямбды
                    }

                    StatusMessage?.Invoke(this, (Measuring)); // Сообщение

                    // Переносим цикл
                    double[] xValues = new double[measurementTime];
                    double[] yValues = new double[measurementTime];
                    double[] LvValues = new double[measurementTime];
                    double[] TValues = new double[measurementTime];

                    for (int i = 0; i < measurementTime; i++)
                    {
                        try
                        {
                            if (_objCa200 != null)
                            {
                                _objCa200.SingleCa.Measure(); // Блокирующий вызов COM
                                xValues[i] = _objCa200.SingleCa.SingleProbe.sx;
                                yValues[i] = _objCa200.SingleCa.SingleProbe.sy;
                                LvValues[i] = _objCa200.SingleCa.SingleProbe.Lv;
                                TValues[i] = _objCa200.SingleCa.SingleProbe.T;
                            }
                            MeasurementProgress?.Invoke(this, (double)(i + 1) / measurementTime * 100); // Прогресс в процентах
                        }
                        catch (COMException measureEx)
                        {
                            StatusMessage?.Invoke(this, (ErrorAtMeasuringIteration) + $"{i}: {measureEx.Message}");
                            continue;
                        }
                        catch (Exception measureEx)
                        {
                            StatusMessage?.Invoke(this, (ErrorAtMeasuringIteration) + $"{i}: {measureEx.Message}");
                            continue;
                        }

                        if (i < measurementTime - 1)
                        {
                            await Task.Delay(1000); // Асинхронная задержка между измерениями
                        }
                    }

                    result.x = xValues.Average();           // Устанавливаем X
                    result.y = yValues.Average();          // Устанавливаем Y
                    result.Lv = LvValues.Average();     // Устанавливаем Lv
                    result.T = TValues.Average();        // Устанавливаем T
                    result.IsValid = true;        // Помечаем как валидное (если выполнение дошло до сюда)

                    //StatusMessage?.Invoke(this, "Измерения завершены."); // Сообщение
                }
                catch (Exception ex)
                {
                    StatusMessage?.Invoke(this, (ErrUnexpected) + $": {ex.Message}");
                    result.IsValid = false;
                }
            });

            return result;
        }

        void IColorMeasurementService.Disconnect()
        {
            Disconnect();
        }
    }
}