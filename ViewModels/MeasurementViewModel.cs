// В папке ViewModels
// Файл MainWindowViewModel.cs

// --- Usings для доступа к другим частям проекта и библиотекам ---
using System.Collections.ObjectModel; // Для ObservableCollection (для логов, статусов)
using System.Diagnostics;
using System.Globalization; // Для CultureInfo (если нужно для форматирования в VM)
using System.IO;
using System.Text.RegularExpressions; // Для валидации серийного номера
using System.Windows;
using System.Windows.Input; // Для интерфейса ICommand
using System.Windows.Threading;
using MvvmHelpers;
using WPF_LCD_Test.Commands; // Для класса RelayCommand и BaseViewModel
using WPF_LCD_Test.Models; // Для классов Model (Measurement, DeviceUnderTest)
using WPF_LCD_Test.Services; // Для интерфейсов Services (IColorMeasurementService, IFileService, IDialogService)
using static WPF_LCD_Test.Resources.Resources;
using MeasurementStatusViewModel = WPF_LCD_Test.Services.MeasurementStatusViewModel;

// Класс ViewModel для MainWindow. Наследует от BaseViewModel для уведомлений UI.
// Реализует IDisposable для очистки ресурсов (отписка от событий).
namespace WPF_LCD_Test.ViewModels
{
    public class MeasurementViewModel : BaseViewModel, IDisposable
    {
        // --- Приватные поля для хранения экземпляров Сервисов и Модели ---
        private readonly IColorMeasurementService _colorMeasurementService; // Сервис для работы с прибором (зависимость)

        private readonly IFileService _fileService; // Сервис для работы с файлами (зависимость)
        private readonly IDialogService _dialogService; // Сервис для показа диалогов (зависимость)
        private readonly ILocalizationService _localizationService; // Сервис для локализации (зависимость)

        private DeviceUnderTest? _currentDevice; // Текущее устройство под тестированием (объект Модели)

        // --- Приватные поля для хранения данных и состояния UI (будут привязаны к View) ---
        private string _serialNumber;

        private readonly Dispatcher _dispatcher;
        private bool _isSerialNumberConfirmed = false;

        private int _measurementTime;

        //private ObservableCollection<string> _logMessages; // Коллекция сообщений для лога UI (UI ListBox/ListView)
        private bool _isMeasurementButtonsEnabled; // Флаг доступности кнопок измерений (UI IsEnabled)

        private bool _isDeviceConnected; // Флаг статуса подключения прибора (UI индикатор)
        private bool _isDeviceCalibrated; // Флаг статуса калибровки прибора (UI индикатор)
        private bool _isDeviceConnecting;
        private bool _isDeviceCalibrating;

        // private double _measurementProgress; // Если хотим показывать прогресс измерения (UI ProgressBar)

        // --- Приватные поля для хранения состояния и статусов ---
        public event EventHandler RequestClearInputFocus;

        // Список ожидаемых измерений по именам точек (из WinForms measurementButtons)
        // Этот список может быть загружен из конфигурации или констант

        // В классе MainWindowViewModel (рядом с другими свойствами)

        // Публичные свойства для статуса каждой точки измерения

        // Константы валидации и значения по умолчанию
        private const string SerialNumberPattern = "^[a-zA-Z0-9]*$"; // Pattern for using only letters and digits

        private const int DefaultMeasurementTime = 2;

        // --- Публичные Свойства ViewModel (для привязки в XAML) ---

        // Серийный номер устройства
        public string SerialNumber
        {
            get => _serialNumber;
            set
            {
                if (SetProperty(ref _serialNumber, value))
                {
                    UpdateMeasurementButtonsState(); // Ваша логика
                    UpdateCommandsCanExecute(); // Ваша логика
                }
                // Здесь НЕТ else блока, т.к. SetProperty уже вернул false, если значение не изменилось.
            }
        }

        // Статус подтверждения серийного номера
        public bool IsSerialNumberConfirmed
        {
            get => _isSerialNumberConfirmed;
            set
            {
                // !!! Правильное использование SetProperty !!!
                if (SetProperty(ref _isSerialNumberConfirmed, value))
                {
                    // Логика после изменения свойства
                    // Важно: при изменении этого статуса нужно переоценить доступность команд!
                    UpdateCommandsCanExecute();
                    UpdateMeasurementButtonsState(); // Ваша логика
                }
            }
        }

        // Время измерения в секундах
        public int MeasurementTime
        {
            get => _measurementTime;
            set
            {
                if (value <= 0)
                {
                    OnPropertyChanged(); // Уведомляем UI о текущем значении поля (_measurementTime)
                    return; // Выходим из сеттера
                }

                // Если значение валидно и отличается, используем SetProperty
                if (SetProperty(ref _measurementTime, value)) { }
            }
        }

        private string _logText = string.Empty; // Поле для хранения лога как единой строки

        public string LogText
        {
            get => _logText;
            // Используем SetProperty для уведомления View об изменении строки лога
            set => SetProperty(ref _logText, value);
        }

        // Флаг, управляющий доступностью группы кнопок измерений
        public bool IsMeasurementButtonsEnabled
        {
            get => _isMeasurementButtonsEnabled;
            // Используем SetProperty в приватном сеттере
            private set => SetProperty(ref _isMeasurementButtonsEnabled, value);
        }

        // Флаг статуса подключения прибора
        public bool IsDeviceConnected
        {
            get => _isDeviceConnected;
            private set
            {
                if (SetProperty(ref _isDeviceConnected, value))
                {
                    UpdateCommandsCanExecute(); // Уведомляем все команды о необходимости перепроверки CanExecute
                    UpdateMeasurementButtonsState(); // Обновляем доступность кнопок измерения
                    OnPropertyChanged(nameof(DeviceConnectionStatusText));
                }
            }
        }

        // Флаг статуса калибровки прибора
        public bool IsDeviceCalibrated
        {
            get => _isDeviceCalibrated;
            private set
            {
                // Используем SetProperty. Если значение изменилось (SetProperty вернул true), выполняем дополнительную логику.
                if (SetProperty(ref _isDeviceCalibrated, value))
                {
                    // При изменении статуса калибровки, потенциально меняется доступность команд (Измерение, Калибровка)
                    UpdateCommandsCanExecute(); // Уведомляем все команды
                    UpdateMeasurementButtonsState(); // Обновляем доступность кнопок измерения
                    OnPropertyChanged(nameof(DeviceCalibrationStatusText));
                }
            }
        }

        public string DeviceConnectionStatusText
        {
            get { return _isDeviceConnected ? ConnectedCA : DisconnectedCA; }
        }

        public string DeviceCalibrationStatusText
        {
            get { return _isDeviceCalibrated ? CalibratedCA : NotCalibratedCa; }
        }

        //public ObservableCollection<MeasurementStatusViewModel> AllMeasurementButtonStatuses { get; set; }

        public ICommand ZeroCalibrationCommand { get; private set; }
        public ICommand SaveResultsCommand { get; private set; }
        public ICommand ClearFieldsCommand { get; private set; }
        public ICommand ClearLogCommand { get; } // Если есть команда для очистки лога
        public ICommand SwitchLanguageCommand { get; private set; } // Принимает параметр (код языка)

        public ICommand MeasureCommand { get; private set; } // Принимает параметр (имя точки измерения)

        // Новые команды для обработки ввода в текстовых полях (по Enter, например)
        public ICommand ApplySerialNumberCommand { get; private set; } // Применяет введенный SN

        public ICommand ApplyMeasurementTimeCommand { get; private set; } // Применяет введенное время

        public ICommand NewDeviceUnderTestCommand { get; private set; } // Создает новое устройство под тестирование
        public ICommand LaunchExternalProgramCommand { get; } // Запускает внешнюю программу (например, для тестирования)

        // --- Конструктор ViewModel ---
        // Получает экземпляры всех необходимых сервисов через параметры (Инъекция Зависимостей)
        public MeasurementViewModel(
            IColorMeasurementService colorMeasurementService,
            IFileService fileService,
            IDialogService dialogService,
            ILocalizationService localizationService
        )
        {
            // Проверяем, что сервисы были корректно предоставлены
            _colorMeasurementService =
                colorMeasurementService
                ?? throw new ArgumentNullException(nameof(colorMeasurementService));
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _dialogService =
                dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _localizationService =
                localizationService ?? throw new ArgumentNullException(nameof(localizationService));

            // Инициализация свойств по умолчанию (как при старте приложения)
            _measurementTime = DefaultMeasurementTime;
            _serialNumber = ""; // Пустая строка по умолчанию

            // Получаем Dispatcher UI-потока
            _dispatcher = App.Current.Dispatcher;

            // Инициализация команд, связывая их с методами Execute/CanExecute
            // Используем RelayCommand, который находится в папке Commands
            //ConnectCommand = new RelayCommand(ExecuteConnectAsync, CanExecuteConnect); // Асинхронная команда
            //DisconnectCommand = new RelayCommand(ExecuteDisconnect, CanExecuteDisconnect); // Синхронная команда (операция быстрая)
            ZeroCalibrationCommand = new RelayCommand(
                ExecuteZeroCalibrationAsync,
                CanExecuteZeroCalibration
            ); // Асинхронная команда
            SaveResultsCommand = new RelayCommand(ExecuteSaveResultsAsync, CanExecuteSaveResults); // Асинхронная команда
            ClearFieldsCommand = new RelayCommand(ExecuteClearFields, CanExecuteClearFields); // Синхронная команда, с CanExecute
            //TestCommand = new RelayCommand(ExecuteTest, CanExecuteTest); // Синхронная команда, с CanExecute
            ClearLogCommand = new RelayCommand(ExecuteClearLog, CanExecuteClearLog); // Синхронная команда, с CanExecute
            SwitchLanguageCommand = new RelayCommand(
                ExecuteSwitchLanguage,
                CanExecuteSwitchLanguage
            ); // Синхронная команда, с CanExecute
            NewDeviceUnderTestCommand = new RelayCommand(
                ExecuteNewDeviceUnderTest,
                CanExecuteNewDeviceUnderTest
            ); // Синхронная команда, с CanExecute
            LaunchExternalProgramCommand = new RelayCommand(ExecuteLaunchExternalProgramCommand); // Синхронная команда, с CanExecute

            // Команда измерения - принимает string parameter (имя точки)
            MeasureCommand = new RelayCommand(ExecuteMeasureAsync, CanExecuteMeasure); // Асинхронная команда

            // Инициализация новых команд для ввода в поля
            ApplySerialNumberCommand = new RelayCommand(
                ExecuteApplySerialNumber,
                CanExecuteApplySerialNumber
            ); // Синхронная команда
            ApplyMeasurementTimeCommand = new RelayCommand(
                ExecuteApplyMeasurementTime,
                CanExecuteApplyMeasurementTime
            ); // Синхронная команда

            // Подписка на события сервисов, чтобы ViewModel мог реагировать на их активность
            // и обновлять UI/состояние через свойства ViewModel или лог

            // Подписка на события сервисов с использованием именованных методов
            _colorMeasurementService.StatusMessage += ColorMeasurementService_StatusMessage;
            _fileService.StatusMessage += FileService_StatusMessage;
            _colorMeasurementService.ConnectionStatusChanged += (sender, isConnected) =>
                ColorMeasurementService_ConnectionStatusChanged(sender, isConnected); // Подписка на событие подключения
            _colorMeasurementService.CalibrationStatusChanged += (sender, isCalibrated) =>
                ColorMeasurementService_CalibrationStatusChanged(sender, isCalibrated); // Подписка на событие калибровки
            //_localizationService.LanguageChanged += LocalizationService_LanguageChanged; // <-- Эта строка должна быть

            // Инициализация начального состояния UI и команд
            //MeasurementButtonsStatusInit(); // Инициализация статусов для каждой точки измерения
            //RequieredMeasurementButtonsInit();
            UpdateCommandsCanExecute(); // Обновляем доступность всех команд при запуске
            UpdateMeasurementButtonsState(); // Обновляем доступность кнопок измерения при запуске
        }

        private bool AreAllStatusesRepresentedInMeasurements()
        {
            // Сначала проверяем, что есть устройство и у него есть коллекция измерений
            if (_currentDevice?.Measurements == null || !_currentDevice.Measurements.Any())
            {
                // Если нет устройства или у него нет измерений, то условие не выполнено
                return false;
            }

            // Получаем коллекцию всех статусов точек измерения из менеджера статусов
            var allStatusPoints = MeasurementStatusManager.Instance.AllMeasurementButtonStatuses;

            // Проверяем, что менеджер статусов содержит точки (хотя он должен быть инициализирован с ними)
            if (allStatusPoints == null || !allStatusPoints.Any())
            {
                // Если в менеджере статусов нет точек, это может указывать на проблему инициализации
                // или если такая ситуация допустима, возможно, здесь нужно вернуть true.
                // Предполагаем, что менеджер статусов всегда должен содержать точки.
                return false;
            }


            // Для КАЖДОЙ точки, представленной в менеджере статусов (AllStatuses),
            // проверяем, есть ли соответствующее измерение в коллекции Measurements текущего устройства.
            foreach (var status in allStatusPoints)
            {
                // Для текущей точки статуса (например, "TopLeft") ищем измерение в коллекции устройства
                // с таким же Location.
                if (!_currentDevice.Measurements.Any(measurement => measurement.Location == status.Location))
                {
                    // Если мы нашли точку в менеджере статусов, для которой НЕТ измерения в коллекции устройства,
                    // значит, не все статусы представлены измерениями. Возвращаем false.
                    return false;
                }
               
            }
            return true;
        }

        // Асинхронная команда подключения
        private async Task ExecuteConnectAsync(object parameter)
        {
            if (!CanExecuteConnect(parameter))
                return; // Проверка доступности

            try
            {
                _isDeviceConnecting = true;
                // Вызываем асинхронный метод Сервиса. Результат и статус придут через события.
                await _colorMeasurementService.ConnectAsync();
            }
            catch (Exception)
            {
                // Обработка непредвиденных
                ExecuteDisconnect(parameter);
            }
            finally
            {
                _isDeviceConnecting = false;
            }
        }

        // Реализация синхронной команды отключения
        private void ExecuteDisconnect(object parameter)
        {
            if (!CanExecuteDisconnect(parameter))
                return;

            try
            {
                _colorMeasurementService.Disconnect(); // Вызываем метод Сервиса
            }
            catch (Exception ex)
            {
                _dialogService.ShowMessage($"{ErrUnexpected}: {ex.Message}", $"{Err}");
            }

            // Обновляем доступность команд и кнопок
            UpdateCommandsCanExecute();
            UpdateMeasurementButtonsState();
        }

        // Реализация асинхронной команды калибровки нуля
        private async Task ExecuteZeroCalibrationAsync(object parameter) // Возвращаем Task
        {
            CheckCurrentAppLanguage();
            try
            {
                if (!IsDeviceConnected)
                {
                    // Если прибор не подключен, то сначала подключаем его
                    await ExecuteConnectAsync(parameter);
                }

                if (IsDeviceConnected)
                {
                    _isDeviceCalibrating = true;
                    await _colorMeasurementService.CalibrateZeroAsync();
                }
            }
            catch (Exception)
            {
                ExecuteDisconnect(parameter); //Если калибровка была неуспешной, отключаем прибор
                _dialogService.ShowMessage($"{ErrAtCalibration}", $"{Err}");
            }
            finally
            {
                _isDeviceCalibrating = false;
            }

            // Обновляем доступность команд и кнопок (непосредственно после завершения калибровки)
            UpdateCommandsCanExecute();
            UpdateMeasurementButtonsState();
        }

        // Реализация асинхронной команды сохранения результатов
        private async Task ExecuteSaveResultsAsync(object parameter)
        {
            CheckCurrentAppLanguage();
            if (!CanExecuteSaveResults(parameter))
                return;

            AddLogMessage($"{Saving}");

            try
            {
                // Проверяем, есть ли данные для сохранения и SN
                if (
                    _currentDevice == null
                    || string.IsNullOrWhiteSpace(_currentDevice.SerialNumber)
                    || _currentDevice.Measurements.Count == 0
                )
                {
                    _dialogService.ShowMessage($"{SaveJSONErrDeviceIsEmpty}", $"{Err}");
                    return;
                }

               

                // Проверяем полноту измерений и запрашиваем подтверждение, если не все собраны
                if (!AreAllStatusesRepresentedInMeasurements())
                {
                    bool confirmSave = _dialogService.ShowQuestion(
                        $"{SavingNotFullWarning}",
                        $"{Warning}"
                    );
                    if (!confirmSave)
                    {
                        AddLogMessage($"{SaveCanceled}");
                        return;
                    }
                }

                // Вызываем асинхронный метод Сервиса Файлов для сохранения в JSON
                // FileService сам отправит сообщения в лог через StatusMessage
                bool saveSuccess = await _fileService.SaveDeviceDataToJsonAsync(_currentDevice);

                //if (saveSuccess)
                //{
                //    _dialogService.ShowMessage($"{ResultsSaved}", $"{Saved}");
                //}
                //else
                //{
                //    _dialogService.ShowMessage($"{SaveJSONErrForSN}", $"{Err}");
                //}
            }
            catch (Exception ex)
            {
                AddLogMessage($"{ErrUnexpected}: {ex.Message}");
                _dialogService.ShowMessage($"{SaveJSONErrForSN}: {ex.Message}", $"{Err}");
            }
            // Доступность команды Сохранить обновится в ExecuteClearFields или по UpdateCommandsCanExecute
        }

        // Реализация синхронной команды очистки полей
        private void ExecuteClearFields(object parameter)
        {
            CheckCurrentAppLanguage();
            if (!CanExecuteClearFields(parameter))
                return; // Хотя обычно всегда true

            // Запрашиваем подтверждение очистки
            bool confirm = _dialogService.ShowQuestion($"{CleanFieldWarning}", $"{Warning}");

            if (confirm)
            {
                // Очистка свойств ViewModel
                SerialNumber = ""; // Сеттер обновит UI и вызовет UpdateMeasurementButtonsState/UpdateCommandsCanExecute
                MeasurementTime = DefaultMeasurementTime; // Сеттер обновит UI и запишет в лог

                // Сброс текущего объекта Модели DeviceUnderTest
                _currentDevice = null;
                IsSerialNumberConfirmed = false;

                // Очистка коллекций ViewModel
                //ExecuteClearLog(parameter); // Очистка лога (вызываем команду очистки лога)
                ResetMeasurementStatuses(); // Сбрасываем статусы всех точек измерения (обновит UI через MeasurementStatusViewModel)

                AddLogMessage($"{ClearFieldsDone}");

                // Явно обновляем доступность команд и кнопок, так как состояние сброшено
                UpdateCommandsCanExecute();
                UpdateMeasurementButtonsState();
            }
        }

        // Реализация синхронной команды смены языка
        private void ExecuteSwitchLanguage(object parameter)
        {
            if (!CanExecuteSwitchLanguage(parameter))
                return;

            string? languageCode = parameter as string; // Получаем код языка из параметра команды
            if (string.IsNullOrWhiteSpace(languageCode))
            {
                return;
            }

            try
            {
                _localizationService.SetLanguage(languageCode);
                CheckCurrentAppLanguage();
            }
            catch (Exception ex)
            {
                _dialogService.ShowMessage($"{ErrMsgLangSwitchFailed}: {ex.Message}", $"{Err}");
            }
        }

        private void CheckCurrentAppLanguage()
        {
            CultureInfo culture = LocalizationService.Instance.CurrentCulture; // Получаем текущую культуру из сервиса локализации

            // Устанавливаем эту культуру для текущего потока из пула
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
        }

        // Реализация асинхронной команды измерения (вызывается для каждой точки измерения)
        private async Task ExecuteMeasureAsync(object parameter)
        {
            CheckCurrentAppLanguage();
            // Получаем имя точки измерения из параметра команды
            var measurementName = parameter as string;
            if (string.IsNullOrWhiteSpace(measurementName))
            {
                return;
            }

            if (!CanExecuteMeasure(parameter)) // Проверка доступности
            {
                return;
            }

            // Обновляем статус этой точки в UI на "измерение в процессе" (опционально)
            UpdateMeasurementStatus(measurementName, null, $"{Measuring}"); // null или кастомный статус

            try
            {
                // 1. Убеждаемся, что есть объект DeviceUnderTest с серийным номером
                // Эту логику можно вынести или обрабатывать в ExecuteApplySerialNumber
                if (
                    _currentDevice == null
                    || string.IsNullOrWhiteSpace(_currentDevice.SerialNumber)
                    || _currentDevice.SerialNumber != SerialNumber
                )
                {
                    // Проверяем SN из свойства ViewModel, т.к. оно привязано к TextBox
                    if (string.IsNullOrWhiteSpace(SerialNumber))
                    {
                        _dialogService.ShowMessage($"{FillSN}", $"{Err}");
                        UpdateMeasurementStatus(measurementName, false, $"{NoSNErr}"); // Сбрасываем статус точки на ошибку
                        return;
                    }

                    // Создаем новый объект DeviceUnderTest, если его нет или SN изменился
                    _currentDevice = new DeviceUnderTest(SerialNumber);
                    AddLogMessage($"{TestStartInfo}: {_currentDevice.SerialNumber}");
                    // Сбрасываем статусы измерений для нового устройства
                    ResetMeasurementStatuses();
                    // Обновляем статус текущей точки измерения (если она была измерена ранее с другим устройством)
                    UpdateMeasurementStatus(measurementName, null, $"{Measuring}"); // Статус "в процессе" для новой точки
                }

                // 2. Вызываем асинхронный метод измерения у Сервиса
                // Передаем время измерения из свойства ViewModel
                // Сервис выполнит усреднение и вернет Measurement
                Measurement resultMeasurement = await _colorMeasurementService.MeasureAsync(
                    _measurementTime
                );

                // 3. Обработка результата измерения, валидация, форматирование
                bool isMeasurmentSuccess = false;
                string measuredValuesDisplay = $"{NoData}"; // Строка для отображения результата в UI

                // Проверяем результат от сервиса: не null и IsValid == true
                if (resultMeasurement != null && resultMeasurement.IsValid)
                {
                    // Логика валидации Lv < 10 (для всех, кроме BlackColor)
                    bool lvValidationPassed = true;

                    if (
                        measurementName
                            != MeasurementStatusManager.Instance.BlackColorStatus.Location
                        && resultMeasurement.Lv < 10
                    )
                    {
                        AddLogMessage($"{LvIsTooLow}: {resultMeasurement.Lv:F1}. {CheckProbe}");
                        lvValidationPassed = false; // Валидация по Lv не пройдена
                        resultMeasurement.IsValid = false; // Отмечаем измерение как невалидное в модели, если не прошло Lv валидацию здесь
                    }

                    if (lvValidationPassed) // Если валидация по Lv пройдена (и сервис вернул Valid=true)
                    {
                        // Логика форматирования для вывода в лог/UI
                        string LvFormatted =
                            (
                                measurementName
                                == MeasurementStatusManager.Instance.BlackColorStatus.Location
                            )
                                ? resultMeasurement.Lv.ToString("F6", CultureInfo.InvariantCulture)
                                : resultMeasurement.Lv.ToString("F1", CultureInfo.InvariantCulture);
                        string TFormatted = resultMeasurement.T.ToString(
                            "F0",
                            CultureInfo.InvariantCulture
                        );

                        measuredValuesDisplay =
                            $"x={resultMeasurement.x:F3}, y={resultMeasurement.y:F3}, Lv={LvFormatted}, T={TFormatted}";

                        AddLogMessage($"{Result} '{measurementName}': {measuredValuesDisplay}");

                        // Добавляем измерение (объект Модели) в коллекцию устройства (объект Модели)
                        // Метод AddMeasurement в DeviceUnderTest позаботится о замене по Location
                        resultMeasurement.Location = measurementName; // Устанавливаем имя точки в объекте измерения перед добавлением
                        _currentDevice.AddMeasurement(resultMeasurement);

                        // Логика сохранения в CSV - это задача Сервиса Файлов
                        // Форматируем CSV строку здесь или в Measurement
                        string csvString =
                            $"{resultMeasurement.Location},{resultMeasurement.x.ToString(CultureInfo.InvariantCulture)},{resultMeasurement.y.ToString(CultureInfo.InvariantCulture)},{resultMeasurement.Lv.ToString(CultureInfo.InvariantCulture)},{resultMeasurement.T.ToString(CultureInfo.InvariantCulture)}";
                        // Возможно, форматирование должно быть более точным, как в WinForms

                        // Вызываем асинхронный метод сохранения CSV у Сервиса Файлов
                        // await _fileService.SaveMeasurementToCsvAsync(csvString, resultMeasurement.Location, _currentDevice.SerialNumber);

                        isMeasurmentSuccess = true; // Измерение успешно выполнено и обработано
                    }
                    else // Не прошло Lv валидацию
                    {
                        isMeasurmentSuccess = false; // Считаем измерение неуспешным
                        measuredValuesDisplay = $"{LvIsTooLow}: {resultMeasurement.Lv:F1}"; // Сообщение для UI
                    }
                }
                else // Сервис вернул null или resultMeasurement.IsValid == false
                {
                    if (resultMeasurement != null && !resultMeasurement.IsValid)
                    {
                        measuredValuesDisplay = $"{InvalidResultErr}";
                    }
                    else // resultMeasurement == null
                    {
                        AddLogMessage($"{ColorServiceErr} '{measurementName}'.");
                        measuredValuesDisplay = $"{ColorAnalyzerErr}";
                    }
                    isMeasurmentSuccess = false; // Считаем измерение неуспешным
                }

                // 4. Обновление статуса измерения для данной точки в коллекции ViewModel (для обновления UI)
                UpdateMeasurementStatus(
                    measurementName,
                    isMeasurmentSuccess,
                    measuredValuesDisplay
                );

                // После измерения, возможно, команда Сохранить стала доступной (если собраны все измерения)
                UpdateCommandsCanExecute(); // Проверяем доступность команд
            }
            catch (Exception ex)
            {
                // Обработка непредвиденных ошибок при выполнении команды измерения
                _dialogService.ShowMessage(
                    $"{UnexpectedMeasurementErr} '{measurementName}': {ex.Message}",
                    $"{Err}"
                );

                // Помечаем статус точки как ошибочный в ViewModel
                UpdateMeasurementStatus(measurementName, false, $"{Err}: {ex.Message}");
                UpdateCommandsCanExecute(); // Проверяем доступность команд
            }
        }

        // Реализация команды для применения введенного Серийного номера (например, по Enter)
        private void ExecuteApplySerialNumber(object parameter)
        {
            CheckCurrentAppLanguage();
            var enteredSerialNumber = parameter as string;
            // Логика из SerialNumberTextBox_KeyDown
            if (!CanExecuteApplySerialNumber(parameter))
                return;

            if (
                !string.IsNullOrWhiteSpace(enteredSerialNumber)
                && Regex.IsMatch(enteredSerialNumber, SerialNumberPattern)
            )
            {
                // Если введен текст, обновляем свойство SerialNumber в ViewModel
                // ЭТО НЕ вызовет сеттер SerialNumber, так как UpdateSourceTrigger=Explicit
                // Вместо этого, нам нужно было бы вызвать UpdateSource() если бы CommandParameter не передавал текст
                // Но раз мы передаем текст, мы можем просто присвоить его свойству SerialNumber
                SerialNumber = enteredSerialNumber; // <-- Присваиваем подтвержденное значение свойству ViewModel
                IsSerialNumberConfirmed = true; // Устанавливаем флаг подтверждения
                AddLogMessage($"{CurrentSN}: {SerialNumber}");
                RequestClearInputFocus?.Invoke(this, EventArgs.Empty); // Запрос на очистку фокуса ввода серийного номера
            }
            else
            {
                // Если подтверждается пустое поле
                //SerialNumber = string.Empty; // Очищаем свойство ViewModel
                IsSerialNumberConfirmed = false; // Сбрасываем флаг
                _dialogService.ShowMessage($"{IncorrectFormatForSNErr}", $"{Err}");
                return;
            }

            // Здесь можно добавить более сложную логику, связанную с применением SN:
            // Например, создание или сброс объекта DeviceUnderTest
            // Убедимся, что _currentDevice соответствует SerialNumber из ViewModel

            if (_currentDevice == null || _currentDevice.SerialNumber != SerialNumber)
            {
                // Если устройство еще не создано или SN изменился, создаем новое
                try
                {
                    IsSerialNumberConfirmed = true;
                    _currentDevice = new DeviceUnderTest(SerialNumber);
                    // Сбрасываем все предыдущие измерения и статусы при смене устройства
                    ResetMeasurementStatuses();
                }
                catch (ArgumentException ex)
                {
                    // Ошибка валидации в конструкторе Модели
                    IsSerialNumberConfirmed = false;
                    AddLogMessage($"{Err}: {ex.Message}");
                    _dialogService.ShowMessage(ex.Message, $"{Err} SN");
                    // Сбрасываем SerialNumber ViewModel на пустую строку, если он невалиден
                    SerialNumber = ""; // Это вызовет OnPropertyChanged и обновит UI
                    _currentDevice = null; // Сбрасываем объект Модели
                    UpdateMeasurementButtonsState(); // Сбрасываем статусы
                }
            }
            else
            {
                // Если устройство уже соответствует SN, возможно, ничего не нужно делать,
                // или просто логируем подтверждение.
                AddLogMessage(
                    $"{Resources.Resources.SerialNumber} {SerialNumber}' {AlreadyActivated}"
                );
            }

            // Обновляем доступность кнопок измерения (зависит от наличия SN)
            UpdateMeasurementButtonsState();
            // Обновляем доступность других команд, которые зависят от наличия SN (например, Сохранить, Измерение)
            UpdateCommandsCanExecute();
        }

        // Реализация команды для применения введенного Времени измерения (например, по Enter)
        private void ExecuteApplyMeasurementTime(object parameter)
        {
            CheckCurrentAppLanguage();
            try
            {
                var enteredMeasurementTime = parameter as string;
                int measurementTime = int.Parse(s: enteredMeasurementTime); // Пробуем преобразовать строку в число
                if (measurementTime <= 0)
                {
                    // Сообщение уже было добавлено в сеттере, можно показать диалог
                    _dialogService.ShowMessage($"{IncorrectMeasTimeFormat}", $"{Err}");
                }
                else
                {
                    MeasurementTime = measurementTime;
                    // Время успешно применено, сообщение в логе уже есть из сеттера свойства
                    AddLogMessage($"{CurrentMeasurementTime}: {MeasurementTime} {Seconds}");
                    RequestClearInputFocus?.Invoke(this, EventArgs.Empty); // Запрос на очистку фокуса ввода времени измерения
                }
            }
            catch
            {
                _dialogService.ShowMessage($"{IncorrectMeasTimeFormat}", $"{Err}");
            }

            // Применение времени измерения обычно не влияет на доступность команд,
            // но если влияет, нужно вызвать UpdateCommandsCanExecute();
        }

        private async Task ExecuteNewDeviceUnderTest(object parameter)
        {
            // Здесь можно добавить логику для создания нового устройства
            // Например, сбросить все статусы и очистить лог

            if (CanExecuteSaveResults(parameter))
            {
                await ExecuteSaveResultsAsync(parameter); // Сохраняем результаты, если команда доступна
            }
            if (CanExecuteClearFields(parameter))
            {
                ExecuteClearFields(parameter);
            }
            ResetMeasurementStatuses();
        }

        private void ExecuteClearLog(object parameter)
        {
            LogText = string.Empty; // Просто устанавливаем строку лога в пустую
            // Свойство LogText вызывает SetProperty.
        }

        private void ExecuteLaunchExternalProgramCommand(object parameter) // Parameter может быть null, если не используется
        {
            try
            {
                // !!! Определение пути к внешнему исполняемому файлу !!!
                // Получаем директорию, где находится ваше приложение.
                string appDirectory = AppDomain.CurrentDomain.BaseDirectory;

                //string exeFileFolder = "Tools"; // Папка с exe файлом (если есть)

                // Определите путь к вашему exe файлу относительно директории приложения.
                // Пример 1: exe находится прямо в папке с приложением
                string executableName = "ReportGenerator.exe";
                string executablePath = Path.Combine(appDirectory, executableName);
                //string executablePath = Path.Combine(appDirectory, exeFileFolder, executableName);

                // !!! Опционально: проверка существования файла !!!
                if (File.Exists(executablePath))
                {
                    Process.Start(executablePath);
                }
                else
                {
                    // Если файл не найден, логируем ошибку и, возможно, показываем сообщение пользователю
                    AddLogMessage($"{RunExternalAppNotFoundErr}: {executablePath}");
                    // Предполагаем, что у вас есть сервис диалогов _dialogService
                }
            }
            catch (Exception ex)
            {
                // Обработка любых ошибок, которые могут возникнуть при запуске процесса
                // Например, если у пользователя нет прав на запуск или произошла другая системная ошибка.
                AddLogMessage($"{RunExternalAppUnexpectedErr}: {ex.Message}");
            }
        }

        // --- Методы ViewModel, проверяющие доступность команд (CanExecute...) ---
        // Эти методы возвращают true, если команда доступна, и false, если нет.
        // WPF вызывает эти методы, чтобы определить, должны ли элементы UI (например, кнопки) быть активными.
        // Они должны быть "чистыми" - не менять состояние, только возвращать bool на основе текущих свойств ViewModel.

        private bool CanExecuteClearLog(object parameter)
        {
            return true; // Команда очистки лога всегда доступна
        }

        // Проверка доступности команды Подключить: доступна, если прибор НЕ подключен
        private bool CanExecuteConnect(object parameter)
        {
            return !IsDeviceConnected && !_isDeviceCalibrating && !_isDeviceConnecting;
        }

        // Проверка доступности команды Отключить: доступна, если прибор ПОДКЛЮЧЕН
        private bool CanExecuteDisconnect(object parameter)
        {
            return !_isDeviceConnecting && !_isDeviceCalibrating;
        }

        // Проверка доступности команды Калибровка нуля: доступна, если прибор ПОДКЛЮЧЕН И НЕ КАЛИБРОВАН
        private bool CanExecuteZeroCalibration(object parameter)
        {
            return !_isDeviceConnecting && !_isDeviceCalibrating;
        }

        // Проверка доступности команды Сохранить: есть объект устройства, серийный номер и измерения
        private bool CanExecuteSaveResults(object parameter)
        {
            // Команда доступна, если объект _currentDevice создан И у него есть SN И в нем есть хотя бы 1 измерение
            return _currentDevice != null
                && !string.IsNullOrWhiteSpace(_currentDevice.SerialNumber)
                && _currentDevice.Measurements.Count > 0;
        }

        // Проверка доступности команды Очистить: всегда доступна
        private bool CanExecuteClearFields(object parameter)
        {
            return true; // Команда очистки всегда доступна
        }

        // Проверка доступности команды Смена языка: всегда доступна (или зависит от списка доступных языков)
        private bool CanExecuteSwitchLanguage(object parameter)
        {
            // Команда требует параметр - код языка. Проверяем, что параметр передан и он строка.
            return parameter is string languageCode && !string.IsNullOrWhiteSpace(languageCode);
            // Если список доступных языков динамический, можно добавить проверку, что languageCode есть в этом списке.
        }

        private bool CanExecuteMeasure(object parameter)
        {
            // Проверка основных условий доступности
            return IsDeviceConnected // Прибор подключен
                && !_isDeviceCalibrating // Прибор калибруется
                && !_isDeviceConnecting // Прибор подключается
                && IsDeviceCalibrated // Прибор откалиброван
                && SerialNumber != "" // Серийный номер подтвержден
                && (MeasurementTime > 0); // Время измерения больше нуля
        }

        // Проверка доступности команды ApplySerialNumber: доступна, если Серийный номер в поле не пустой
        private bool CanExecuteApplySerialNumber(object parameter)
        {
            return true;
            //return !string.IsNullOrWhiteSpace(SerialNumber);
        }

        // Проверка доступности команды ApplyMeasurementTime: доступна, если время в поле валидно (например, > 0)
        private bool CanExecuteApplyMeasurementTime(object parameter)
        {
            // Команда доступна, если свойство MeasurementTime (которое привязано к TextBox) > 0
            // Валидация уже происходит в сеттере свойства.
            //return MeasurementTime > 0; // Используем публичное свойство
            return true;
        }

        private bool CanExecuteNewDeviceUnderTest(object paramater)
        {
            return _currentDevice != null
                && !string.IsNullOrWhiteSpace(_currentDevice.SerialNumber)
                && _currentDevice.Measurements.Count > 0;
        }

        // --- Вспомогательные методы ViewModel (для внутренней логики ViewModel) ---
        // Эти методы помогают организовать код внутри ViewModel, но не привязаны напрямую к UI.

        // Метод для добавления сообщения в коллекцию логов
        // Используем Dispatcher для потокобезопасного доступа к ObservableCollection
        private void AddLogMessage(string message)
        {
            // App.Current.Dispatcher.Invoke выполнит действие в UI потоке
            _dispatcher.Invoke(() =>
            {
                //LogMessages.Add($"{DateTime.Now:HH:mm:ss} - {message}");
                //// Опционально: ограничить количество сообщений в логе
                //if (LogMessages.Count > 500) // Например, держать не более 500 сообщений
                //{
                //    LogMessages.RemoveAt(0); // Удалить самое старое сообщение
                //}
                if (!string.IsNullOrEmpty(message))
                {
                    string timestamp = DateTime.Now.ToString("HH:mm:ss");
                    LogText += $"{timestamp} {message}{Environment.NewLine}";
                }
            });
        }

        // Метод для обновления состояния доступности кнопок измерения
        // Управляет свойством IsMeasurementButtonsEnabled
        // Вызывается, когда изменяются свойства, от которых зависит доступность (Connected, Calibrated, SerialNumber)
        private void UpdateMeasurementButtonsState()
        {
            IsMeasurementButtonsEnabled =
                IsDeviceConnected
                && IsDeviceCalibrated
                && !string.IsNullOrWhiteSpace(SerialNumber)
                && SerialNumber != "";

            // Важно: После обновления состояния кнопок, уведомляем команду MeasureCommand
            // о возможном изменении ее доступности, чтобы UI (кнопки) обновился.
            (MeasureCommand as RelayCommand)?.RaiseCanExecuteChanged(); // Используем безопасное приведение и ?.
        }

        // Метод для уведомления ВСЕХ команд о возможном изменении их состояния CanExecute
        // Вызывается, когда меняются свойства, которые влияют на доступность многих команд (например, IsDeviceConnected)
        private void UpdateCommandsCanExecute()
        {
            ((RelayCommand)ZeroCalibrationCommand)?.RaiseCanExecuteChanged();
            ((RelayCommand)MeasureCommand)?.RaiseCanExecuteChanged();
            ((RelayCommand)SaveResultsCommand)?.RaiseCanExecuteChanged();
            ((RelayCommand)ApplySerialNumberCommand)?.RaiseCanExecuteChanged();
            ((RelayCommand)ApplyMeasurementTimeCommand)?.RaiseCanExecuteChanged();
            ((RelayCommand)NewDeviceUnderTestCommand)?.RaiseCanExecuteChanged();
        }

        // Метод для обновления статуса конкретной точки измерения по ее имени
        // Вызывается из ExecuteMeasureAsync после получения результата
        private void UpdateMeasurementStatus(
            string location,
            bool? isPassed,
            string measuredValuesString = null
        )
        {
            // 1. Ищем нужный объект MeasurementStatusViewModel в коллекции по его Location
            //    Используем LINQ FirstOrDefault(). Он вернет первый найденный элемент или null, если не найден.
            MeasurementStatusViewModel statusToUpdate =
                MeasurementStatusManager.Instance.AllMeasurementButtonStatuses.FirstOrDefault(s =>
                    s.Location == location
                );

            // 2. Проверяем, был ли найден объект статуса
            if (statusToUpdate != null)
            {
                // 3. Если объект найден, обновляем его свойства напрямую
                //    Поскольку statusToUpdate ссылается на тот же объект, что и публичное свойство
                //    (например, TopLeftStatus), обновление здесь также обновит объект,
                //    к которому привязано XAML.
                statusToUpdate.IsPassed = isPassed;
                statusToUpdate.MeasuredValuesString = measuredValuesString;

                // StatusMessage?.Invoke(this, $"Статус для '{location}' обновлен. IsPassed: {isPassed}\r\n"); // Опционально: лог об успешном обновлении
            }
            else
            {
                // 4. Если объект с таким Location не найден в коллекции (например, пришла некорректная строка)
                AddLogMessage($"{MeasButStatusErr}: '{location}'");
                // Возможно, нужно показать диалог пользователю, если это критическая ошибка
                // _dialogService.ShowMessage($"Получена неизвестная точка измерения: {location}", "Ошибка обновления статуса");
            }
        }

        // Метод для сброса всех статусов измерений (например, при очистке полей
        private void ResetMeasurementStatuses()
        {
            // Сбрасываем свойства у каждого публичного объекта статуса
            foreach (
                MeasurementStatusViewModel measurementStatusViewModel in MeasurementStatusManager
                    .Instance
                    .AllMeasurementButtonStatuses
            )
            {
                measurementStatusViewModel.IsPassed = null;
                measurementStatusViewModel.MeasuredValuesString = null;
            }
        }

        // --- Метод для обновления всех локализуемых текстов в ViewModel ---
        // Пример правильного обновления статусов в UpdateLocalizedTexts() или в обработчиках событий сервиса

        // Метод-обработчик для события StatusMessage от _colorMeasurementService
        private void ColorMeasurementService_StatusMessage(object sender, string message)
        {
            ExecuteThreadInUI(() =>
            {
                AddLogMessage(message); // Получаем сообщения от сервиса прибора
            });
        }

        // Метод-обработчик для события StatusMessage от _fileService
        private void FileService_StatusMessage(object sender, string message)
        {
            ExecuteThreadInUI(() =>
            {
                AddLogMessage(message); // Получаем сообщения от сервиса файлов
            });
        }

        private void ColorMeasurementService_CalibrationStatusChanged(
            object sender,
            bool isCalibrated
        )
        {
            // Обработка смены статуса калибровки
            ExecuteThreadInUI(() =>
            {
                IsDeviceCalibrated = isCalibrated;
                UpdateCommandsCanExecute();
            });
        }

        private void ColorMeasurementService_ConnectionStatusChanged(
            object sender,
            bool isConnected
        )
        {
            // Обработка смены статуса подключения
            ExecuteThreadInUI(() =>
            {
                IsDeviceConnected = isConnected;
                UpdateCommandsCanExecute();
            });
        }

        // Если у вас есть другие подписки через лямбды, создайте для них аналогичные именованные методы.
        // Например, для ConnectionStatusChanged:
        // private void ColorMeasurementService_ConnectionStatusChanged(object sender, bool isConnected)
        // {
        //     // Логика обработки смены статуса подключения
        //     IsDeviceConnected = isConnected;
        //     UpdateDeviceConnectionStatusText();
        //     UpdateCommandsCanExecute();
        // }

        public void Dispose()
        {
            AddLogMessage($"{ViewModelClearing}");

            // Отписываемся от событий сервисов

            if (_colorMeasurementService != null)
            {
                _colorMeasurementService.StatusMessage -= ColorMeasurementService_StatusMessage;
                _colorMeasurementService.ConnectionStatusChanged -=
                    ColorMeasurementService_ConnectionStatusChanged;
                _colorMeasurementService.CalibrationStatusChanged -=
                    ColorMeasurementService_CalibrationStatusChanged;
            }

            if (_fileService != null)
            {
                _fileService.StatusMessage -= FileService_StatusMessage;
                // _fileService.SaveOperationCompleted -= OnFileServiceSaveOperationCompleted;
            }

            // Вызываем Dispose у сервисов, если они реализуют IDisposable
            // Это важно, чтобы сервисы освободили свои ресурсы (COM-объекты, файловые потоки и т.п.)
            (_colorMeasurementService as IDisposable)?.Dispose();
            (_fileService as IDisposable)?.Dispose();
            (_dialogService as IDisposable)?.Dispose(); // Если DialogService тоже IDisposable

            ExecuteClearLog(null); // Очищаем лог, если нужно

            // Сбрасываем ссылки на объекты Модели
            _currentDevice = null;

            // Ссылки на сервисы, если они были инжектированы, обычно не сбрасываются здесь,
            // их жизненным циклом управляет контейнер DI.
            // Если ViewModel сам создавал сервисы (что не рекомендуется), тогда их нужно сбросить.
            GC.SuppressFinalize(this);
            AddLogMessage($"{ViewModelCleared}");
        }

        private void ExecuteThreadInUI(Action action)
        {
            // Проверяем, находимся ли мы уже в потоке пользовательского интерфейса.
            // Если да, выполняем действие напрямую.
            if (_dispatcher.CheckAccess())
            {
                action.Invoke(); // Или просто action();
            }
            else
            {
                // Если мы в фоновом потоке, используем BeginInvoke для выполнения действия
                // в потоке пользовательского интерфейса асинхронно.
                _dispatcher.BeginInvoke(action);
            }
        }
    }
}
