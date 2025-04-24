using System.Runtime.InteropServices;
using CA200SRVRLib;
using WPF_LCD_Test.Services;
using WPF_LCD_Test.Models;

namespace WPF_LCD_Test.Services
{
    public class ColorMeasurementService : IDisposable, IColorMeasurementService
    {
        private Ca200? _objCa200 = null;
        private Ca? _objCa = null;
        private string? _portID = null;
        private bool _isConnected = false;
        private bool _isCalibrated = false;
        private readonly ILocalizationService _localizationService; // Сервис для локализации (зависимость)
        

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

        public event EventHandler<bool> ConnectionStatusChanged;

        public event EventHandler<bool> CalibrationStatusChanged;

        public event EventHandler<string> StatusMessage;

        public event EventHandler<double> MeasurementProgress;

        public bool IsConnected => _isConnected;

        public bool IsCalibrated => _isCalibrated;

        public string PortID => _portID;

        public ColorMeasurementService(ILocalizationService localizationService)
        {
            _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
        }

        private double GetMeasuredSx() => _objCa200.SingleCa.SingleProbe.sx;

        private double GetMeasuredSy() => _objCa200.SingleCa.SingleProbe.sy;

        private double GetMeasuredLv() => _objCa200.SingleCa.SingleProbe.Lv;

        private double GetMeasuredT() => _objCa200.SingleCa.SingleProbe.T;

        public async Task<bool> ConnectAsync()
        {
            // Переносим логику из твоих MinoltaConnection() и ColorAnalyzer() конструктора сюда
            // Делаем асинхронным!
            await Task.Run(() => // Выполняем потенциально блокирующий COM вызов в фоновом потоке
            {
                try
                {
                    if (_objCa200 == null)
                    {
                        _objCa200 = new Ca200();
                    }

                    if (!_isConnected)
                    {
                        StatusMessage?.Invoke(this, _localizationService.GetString("ConnectingCA")); // Отправляем сообщение в лог ViewModel через событие
                        _objCa200.AutoConnect(); // Блокирующий вызов COM
                        _objCa = _objCa200.SingleCa;
                        _portID = _objCa.PortID;
                        _isConnected = true;
                        ConnectionStatusChanged?.Invoke(this, _isConnected); // Оповещаем ViewModel об изменении статуса
                        StatusMessage?.Invoke(this, _localizationService.GetString("ConnectedCA")); // Отправляем сообщение
                    }
                }
                catch (COMException ex)
                {
                    StatusMessage?.Invoke(this, _localizationService.GetString("ConnectionError")  + $":    {ex.Message}"); // Отправляем ошибку
                    _isConnected = false; // Обновляем статус
                    ConnectionStatusChanged?.Invoke(this, _isConnected); // Оповещаем ViewModel
                                                                         // Здесь не пробрасываем исключение, Сервис сам обрабатывает ошибку подключения
                                                                         // ViewModel может проверить IsConnected после вызова ConnectAsync
                }
                catch (Exception ex) // Ловим другие возможные исключения
                {
                    StatusMessage?.Invoke(this, _localizationService.GetString("ConnectionError") + $": {ex.Message}");
                    _isConnected = false;
                    ConnectionStatusChanged?.Invoke(this, _isConnected);
                }
            });
            return _isConnected; // Возвращаем статус подключения
        }

        private void Disconnect()
        {
            Dispose();
        }

        public async Task<bool> CalibrateZeroAsync()
        {
            bool success = false;
            await Task.Run(() =>
            {
                try
                {
                    //if (!_isConnected)
                    //{
                    //    // Попытка подключения синхронно или вызвать ConnectAsync().Wait() (осторожно!)
                    //    // Лучше убедиться в ViewModel, что подключен, прежде чем вызывать калибровку
                    //    StatusMessage?.Invoke(this, "Попытка калибровки без подключения. Подключение...");
                    //    if (!ConnectAsync().Result) // Осторожно: .Result блокирует! Лучше обрабатывать в ViewModel последовательность
                    //    {
                    //        StatusMessage?.Invoke(this, "Не удалось подключиться для калибровки.");
                    //        return; // Выходим из лямбды Task.Run
                    //    }
                    //}

                    StatusMessage?.Invoke(this, _localizationService.GetString("CalibratingZeroCA")); // Сообщение
                    _objCa200.SingleCa.CalZero(); // Блокирующий вызов COM

                    _objCa200.SingleCa.SyncMode = (int)UniverslaSyncMode;
                    _objCa200.SingleCa.AveragingMode = (int)AutoMeasuringMode;
                    _objCa200.SingleCa.SetAnalogRange(Convert.ToSingle(DefaultDisplayRange), Convert.ToSingle(DefaultDisplayRange));
                    _objCa200.SingleCa.DisplayMode = (int)LvxyDisplayMode;
                    _objCa200.SingleCa.Memory.ChannelNO = (int)ZeroChannel;

                    _isCalibrated = true; // Обновляем статус
                    CalibrationStatusChanged?.Invoke(this, _isCalibrated); // Оповещаем
                    StatusMessage?.Invoke(this, _localizationService.GetString("ZeroCalibratedCA")); // Сообщение
                    success = true; // Успех
                }
                catch (COMException ex) // Ловим ошибки COM
                {
                    StatusMessage?.Invoke(this, _localizationService.GetString("CheckConnectionCA") + $": {ex.Message}");
                    _isCalibrated = false; // Обновляем статус
                    CalibrationStatusChanged?.Invoke(this, _isCalibrated); // Оповещаем
                                                                           // Не пробрасываем исключение, обрабатываем внутри сервиса
                }
                catch (Exception ex) {  // Ловим другие ошибки
                
                    StatusMessage?.Invoke(this, _localizationService.GetString("ErrAtCalibration") + $": {ex.Message}");
                    _isCalibrated = false;
                    CalibrationStatusChanged?.Invoke(this, _isCalibrated);
                }
            }); // Конец Task.Run
            return success;
        }

        private void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                StatusMessage?.Invoke(this, _localizationService.GetString("DisconnectingCA")); // Сообщение
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

                _isConnected = false;
                ConnectionStatusChanged?.Invoke(this, _isConnected); // Оповещаем
                _isCalibrated = false;
                CalibrationStatusChanged?.Invoke(this, _isCalibrated); // Оповещаем
                StatusMessage?.Invoke(this, _localizationService.GetString("DisconnectedCA")); // Сообщение
            }
        }

        ~ColorMeasurementService()
        {
            Dispose(false);
        }

        public async Task<Measurement> MeasureAsync(int measurementTime)
        {
            // Переносим логику из твоих PerformMeasurements() и части ColorMeasure()
            // Делаем асинхронным!
            Measurement result = new Measurement(); // Создаем объект результата
            await Task.Run(async () => // Выполняем в фоновом потоке
            {
                try
                {
                    if (!_isConnected)
                    {
                        StatusMessage?.Invoke(this, _localizationService.GetString("MeasureWihoutConnectionError"));
                        result.IsValid = false; // Отмечаем результат как невалидный
                        return; // Выходим из лямбды
                    }
                    if (!_isCalibrated) // Проверяем калибровку
                    {
                        StatusMessage?.Invoke(this, _localizationService.GetString("MakeZeroCalibration"));
                        result.IsValid = false; // Отмечаем результат как невалидный
                        return; // Выходим из лямбды
                    }

                    StatusMessage?.Invoke(this, _localizationService.GetString("Measuring")); // Сообщение

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
                        }
                        catch (COMException measureEx)
                        {
                            StatusMessage?.Invoke(this, _localizationService.GetString("ErrorAtMeasuringIteration") + $"{ i}: {measureEx.Message}");
                            continue;
                        }
                        catch (Exception measureEx)
                        {
                            StatusMessage?.Invoke(this, _localizationService.GetString("ErrorAtMeasuringIteration") + $"{i}: {measureEx.Message}");
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
                    StatusMessage?.Invoke(this, _localizationService.GetString("ErrUnexpected") + $": {ex.Message}");
                    result.IsValid = false;
                }
            });

            return result;
        }

        void IDisposable.Dispose()
        {
            Dispose();
        }

        void IColorMeasurementService.Disconnect()
        {
            Disconnect();
        }
    }
}