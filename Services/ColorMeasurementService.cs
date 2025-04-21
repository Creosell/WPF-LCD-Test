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

        private long ErrorInterpretation = -2147221504;

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

        public ColorMeasurementService()
        {
            //_objCa200 = new Ca200();
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
                        StatusMessage?.Invoke(this, "Подключение к CA-310..."); // Отправляем сообщение в лог ViewModel через событие
                        _objCa200.AutoConnect(); // Блокирующий вызов COM
                        _objCa = _objCa200.SingleCa;
                        _portID = _objCa.PortID;
                        _isConnected = true;
                        ConnectionStatusChanged?.Invoke(this, _isConnected); // Оповещаем ViewModel об изменении статуса
                        StatusMessage?.Invoke(this, "CA-310 подключен успешно."); // Отправляем сообщение
                    }
                }
                catch (COMException ex)
                {
                    StatusMessage?.Invoke(this, $"Ошибка подключения к CA-310: {ex.Message}"); // Отправляем ошибку
                    _isConnected = false; // Обновляем статус
                    ConnectionStatusChanged?.Invoke(this, _isConnected); // Оповещаем ViewModel
                                                                         // Здесь не пробрасываем исключение, Сервис сам обрабатывает ошибку подключения
                                                                         // ViewModel может проверить IsConnected после вызова ConnectAsync
                }
                catch (Exception ex) // Ловим другие возможные исключения
                {
                    StatusMessage?.Invoke(this, $"Неожиданная ошибка при подключении: {ex.Message}");
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

        private void Measure()
        {
            _objCa200.SingleCa.Measure();
        }

        public async Task<bool> CalibrateZeroAsync()
        {
            bool success = false;
            await Task.Run(() =>
            {
                try
                {
                    if (!_isConnected)
                    {
                        // Попытка подключения синхронно или вызвать ConnectAsync().Wait() (осторожно!)
                        // Лучше убедиться в ViewModel, что подключен, прежде чем вызывать калибровку
                        StatusMessage?.Invoke(this, "Попытка калибровки без подключения. Подключение...");
                        if (!ConnectAsync().Result) // Осторожно: .Result блокирует! Лучше обрабатывать в ViewModel последовательность
                        {
                            StatusMessage?.Invoke(this, "Не удалось подключиться для калибровки.");
                            return; // Выходим из лямбды Task.Run
                        }
                    }

                    StatusMessage?.Invoke(this, "Выполнение нулевой калибровки..."); // Сообщение
                    _objCa200.SingleCa.CalZero(); // Блокирующий вызов COM

                    _objCa200.SingleCa.SyncMode = (int)UniverslaSyncMode;
                    _objCa200.SingleCa.AveragingMode = (int)AutoMeasuringMode;
                    _objCa200.SingleCa.SetAnalogRange(Convert.ToSingle(DefaultDisplayRange), Convert.ToSingle(DefaultDisplayRange));
                    _objCa200.SingleCa.DisplayMode = (int)LvxyDisplayMode;
                    _objCa200.SingleCa.Memory.ChannelNO = (int)ZeroChannel;

                    _isCalibrated = true; // Обновляем статус
                    CalibrationStatusChanged?.Invoke(this, _isCalibrated); // Оповещаем
                    StatusMessage?.Invoke(this, "Нулевая калибровка выполнена."); // Сообщение
                    success = true; // Успех
                }
                catch (COMException ex) // Ловим ошибки COM
                {
                    StatusMessage?.Invoke(this, $"Ошибка COM при калибровке: {ex.Message}");
                    _isCalibrated = false; // Обновляем статус
                    CalibrationStatusChanged?.Invoke(this, _isCalibrated); // Оповещаем
                                                                           // Не пробрасываем исключение, обрабатываем внутри сервиса
                }
                catch (Exception ex) // Ловим другие ошибки
                {
                    StatusMessage?.Invoke(this, $"Неожиданная ошибка при калибровке: {ex.Message}");
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
                _isCalibrated = false;
            }
        }

        ~ColorMeasurementService()
        {
            Dispose(false);
        }

        public async Task<Measurement> MeasureAsync()
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
                        StatusMessage?.Invoke(this, "Ошибка: Попытка измерения без подключения.");
                        result.IsValid = false; // Отмечаем результат как невалидный
                        return; // Выходим из лямбды
                    }
                    if (!_isCalibrated) // Проверяем калибровку
                    {
                        StatusMessage?.Invoke(this, "Ошибка: Попытка измерения без калибровки.");
                        result.IsValid = false; // Отмечаем результат как невалидный
                        return; // Выходим из лямбды
                    }

                    StatusMessage?.Invoke(this, "Выполнение измерений..."); // Сообщение

                    // Переносим цикл измерений
                    int measurmentTime = 2; // Это значение должно приходить из ViewModel!
                    double[] xValues = new double[measurmentTime];
                    double[] yValues = new double[measurmentTime];
                    double[] LvValues = new double[measurmentTime];
                    double[] TValues = new double[measurmentTime];

                    for (int i = 0; i < measurmentTime; i++)
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
                            StatusMessage?.Invoke(this, $"Ошибка COM при измерении {i}: {measureEx.Message}");
                            continue;
                        }
                        catch (Exception measureEx)
                        {
                            StatusMessage?.Invoke(this, $"Неожиданная ошибка при измерении {i}: {measureEx.Message}");
                            continue;
                        }

                        if (i < measurmentTime - 1)
                        {
                            await Task.Delay(1000); // Асинхронная задержка между измерениями
                        }
                    }

                    // Расчет средних значений
                    double xAverage = xValues.Where(v => v != 0).DefaultIfEmpty(0).Average(); // Учет возможных ошибок, если измерение пропущено
                    double yAverage = yValues.Where(v => v != 0).DefaultIfEmpty(0).Average();
                    double LvAverage = LvValues.Where(v => v != 0).DefaultIfEmpty(0).Average();
                    double TAverage = TValues.Where(v => v != 0).DefaultIfEmpty(0).Average();

                    // Заполнение объекта Measurement - здесь только сырые средние значения
                    // Форматирование для отображения или специфическая валидация (вроде Lv < 10)
                    // лучше делать в ViewModel или методе ViewModel, который вызывает MeasureAsync
                    // result.SetValues("Измерение", xAverage, yAverage, LvAverage, TAverage); // ЭТУ строку удаляем

                    result.x = xAverage;          // Устанавливаем X
                    result.y = yAverage;          // Устанавливаем Y
                    result.Lv = LvAverage;        // Устанавливаем Lv
                    result.T = TAverage;          // Устанавливаем T
                    result.IsValid = true;        // Помечаем как валидное (если выполнение дошло до сюда)

                    StatusMessage?.Invoke(this, "Измерения завершены."); // Сообщение
                }
                catch (COMException ex)
                {
                    StatusMessage?.Invoke(this, $"Глобальная ошибка COM при выполнении измерений: {ex.Message}");
                    result.IsValid = false;
                }
                catch (Exception ex)
                {
                    StatusMessage?.Invoke(this, $"Неожиданная ошибка при выполнении измерений: {ex.Message}");
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