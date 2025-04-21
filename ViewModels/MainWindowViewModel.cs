// В папке ViewModels
// Файл MainWindowViewModel.cs

// --- Usings для доступа к другим частям проекта и библиотекам ---
using WPF_LCD_Test.Models; // Для классов Model (Measurement, DeviceUnderTest)
using WPF_LCD_Test.Services; // Для интерфейсов Services (IColorMeasurementService, IFileService, IDialogService)
using WPF_LCD_Test.Commands; // Для класса RelayCommand и BaseViewModel

using System; // Для DateTime, Exception и т.п.
using System.Collections.Generic; // Для List
using System.Collections.ObjectModel; // Для ObservableCollection (для логов, статусов)
using System.Linq; // Для LINQ (расчеты, фильтрация)
using System.Threading.Tasks; // Для работы с асинхронными операциями
using System.Windows.Input; // Для интерфейса ICommand
using System.Globalization; // Для CultureInfo (если нужно для форматирования в VM)
using System.Text.RegularExpressions; // Для валидации серийного номера
using System.ComponentModel; // Для INotifyPropertyChanged (хотя BaseViewModel уже его реализует)
using System.Runtime.CompilerServices; // Для CallerMemberName (хотя BaseViewModel уже его использует)
using System.Windows.Media; // Для System.Windows.Media.Brush / Brushes (в MeasurementStatusViewModel)
using System.Text.Json.Serialization;
using MvvmHelpers;
using System.Windows.Threading;
using System.Windows; // Для атрибута JsonIgnore (в MeasurementStatusViewModel)

// Класс ViewModel для MainWindow. Наследует от BaseViewModel для уведомлений UI.
// Реализует IDisposable для очистки ресурсов (отписка от событий).
namespace WPF_LCD_Test.ViewModels // Пространство имен для ViewModel
{
    public class MainWindowViewModel : BaseViewModel, IDisposable
    {
        // --- Приватные поля для хранения экземпляров Сервисов и Модели ---
        private readonly IColorMeasurementService _colorMeasurementService; // Сервис для работы с прибором (зависимость)

        private readonly IFileService _fileService; // Сервис для работы с файлами (зависимость)
        private readonly IDialogService _dialogService; // Сервис для показа диалогов (зависимость)

        private DeviceUnderTest _currentDevice; // Текущее устройство под тестированием (объект Модели)

        private Dispatcher _dispatcher; //Диспетчер UI-потока

        // --- Приватные поля для хранения данных и состояния UI (будут привязаны к View) ---
        private string _serialNumber;

        private int _measurementTime;
        private ObservableCollection<string> _logMessages; // Коллекция сообщений для лога UI (UI ListBox/ListView)
        private bool _isMeasurementButtonsEnabled; // Флаг доступности кнопок измерений (UI IsEnabled)
        private bool _isDeviceConnected; // Флаг статуса подключения прибора (UI индикатор)
        private bool _isDeviceCalibrated; // Флаг статуса калибровки прибора (UI индикатор)
        // private double _measurementProgress; // Если хотим показывать прогресс измерения (UI ProgressBar)

        // Коллекция статусов измерений для точек (для изменения цвета кнопок в UI)
        // Каждый элемент MeasurementStatusViewModel уведомляет UI об изменении своих свойств (IsPassed, MeasuredValuesString)
        private ObservableCollection<MeasurementStatusViewModel> _measurementStatuses;

        // Список ожидаемых измерений по именам точек (из WinForms measurementButtons)
        // Этот список может быть загружен из конфигурации или констант
        private readonly List<string> _requiredMeasurementNames = new List<string>
        {
            "TopLeft", "TopCenter", "TopRight",
            "MiddleLeft", "Center", "MiddleRight",
            "BottomLeft", "BottomCenter", "BottomRight",
            "RedColor", "GreenColor", "BlueColor", "BlackColor"
        };

        // В классе MainWindowViewModel (рядом с другими свойствами)

        // Публичные свойства для статуса каждой точки измерения
        public MeasurementStatusViewModel TopLeftStatus { get; private set; }
        public MeasurementStatusViewModel TopCenterStatus { get; private set; }
        public MeasurementStatusViewModel TopRightStatus { get; private set; }

        public MeasurementStatusViewModel MiddleLeftStatus { get; private set; }
        public MeasurementStatusViewModel CenterStatus { get; private set; }
        public MeasurementStatusViewModel MiddleRightStatus { get; private set; }

        public MeasurementStatusViewModel BottomLeftStatus { get; private set; }
        public MeasurementStatusViewModel BottomCenterStatus { get; private set; }
        public MeasurementStatusViewModel BottomRightStatus { get; private set; }

        public MeasurementStatusViewModel RedColorStatus { get; private set; }
        public MeasurementStatusViewModel GreenColorStatus { get; private set; }
        public MeasurementStatusViewModel BlueColorStatus { get; private set; }
        public MeasurementStatusViewModel BlackColorStatus { get; private set; }

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
                // Используем оператор ?. для безопасного вызова OnPropertyChanged
                if (_serialNumber != value)
                {
                    _serialNumber = value;
                    OnPropertyChanged(); // Уведомляем View об изменении свойства

                    // При изменении серийного номера может измениться доступность команд и кнопок
                    UpdateMeasurementButtonsState(); // Обновляем доступность кнопок измерения
                    UpdateCommandsCanExecute(); // Обновляем доступность всех команд
                }
            }
        }

        // Время измерения в секундах
        public int MeasurementTime
        {
            get => _measurementTime;
            set
            {
                // Базовая валидация времени
                if (value <= 0)
                {
                    AddLogMessage("Внимание: Время измерения должно быть больше 0.");
                    // Не меняем _measurementTime, но уведомляем UI, чтобы поле могло сбросить невалидный ввод, если привязано в TwoWay
                    OnPropertyChanged();
                }
                else if (_measurementTime != value)
                {
                    _measurementTime = value;
                    OnPropertyChanged(); // Уведомляем View
                    AddLogMessage($"Текущее время измерения установлено: {MeasurementTime} сек."); // Лог
                }
            }
        }

        // Коллекция сообщений для отображения в логе UI
        // ObservableCollection автоматически уведомляет UI при добавлении/удалении
        public ObservableCollection<string> LogMessages
        {
            get => _logMessages;
            // Сеттер может быть приватным или отсутствовать, т.к. коллекция обычно инициализируется один раз в конструкторе
            private set // Сделаем приватным, чтобы снаружи нельзя было просто заменить всю коллекцию
            {
                if (_logMessages != value)
                {
                    _logMessages = value;
                    OnPropertyChanged();
                }
            }
        }

        // Флаг, управляющий доступностью группы кнопок измерений
        public bool IsMeasurementButtonsEnabled
        {
            get => _isMeasurementButtonsEnabled;
            private set // Сеттер приватный, доступность кнопок управляется логикой ViewModel (UpdateMeasurementButtonsState)
            {
                if (_isMeasurementButtonsEnabled != value)
                {
                    _isMeasurementButtonsEnabled = value;
                    OnPropertyChanged(); // Уведомляем View
                }
            }
        }

        // Флаг статуса подключения прибора
        public bool IsDeviceConnected
        {
            get => _isDeviceConnected;
            // Сеттер приватный, статус обновляется при обработке событий от Сервиса
            private set
            {
                if (_isDeviceConnected != value)
                {
                    _isDeviceConnected = value;
                    OnPropertyChanged(nameof(IsDeviceConnected)); // Уведомляем View

                    // При изменении статуса подключения, потенциально меняется доступность многих команд и кнопок
                    UpdateCommandsCanExecute(); // Уведомляем все команды о необходимости перепроверки CanExecute
                    UpdateMeasurementButtonsState(); // Обновляем доступность кнопок измерения
                }
            }
        }

        // Флаг статуса калибровки прибора
        public bool IsDeviceCalibrated
        {
            get => _isDeviceCalibrated;
            // Сеттер приватный, статус обновляется при обработке событий от Сервиса
            private set
            {
                // ИСПРАВЛЕНИЕ: используем правильное имя приватного поля _isDeviceCalibrated
                if (_isDeviceCalibrated != value)
                {
                    _isDeviceCalibrated = value; // Используем _isDeviceCalibrated
                    OnPropertyChanged(); // Уведомляем View

                    // При изменении статуса калибровки, потенциально меняется доступность команд (Измерение, Калибровка)
                    UpdateCommandsCanExecute(); // Уведомляем все команды
                    UpdateMeasurementButtonsState(); // Обновляем доступность кнопок измерения
                }
            }
        }

        public ObservableCollection<MeasurementStatusViewModel> AllMeasurementButtonStatuses { get; set; } 

        // Коллекция статусов для каждой точки измерения (для привязки к кнопкам или списку)
        // Каждый элемент MeasurementStatusViewModel уведомляет об изменении своего статуса/цвета
        //public ObservableCollection<MeasurementStatusViewModel> MeasurementStatuses
        //{
        //    get => _measurementStatuses;
        //    // Сеттер приватный, коллекция заполняется в InitializeMeasurementStatuses
        //    private set
        //    {
        //        if (_measurementStatuses != value)
        //        {
        //            _measurementStatuses = value;
        //            OnPropertyChanged();
        //        }
        //    }
        //}

        // --- Команды ViewModel (для привязки к действиям в View) ---
        // Используем get; private set; чтобы команды можно было установить только в конструкторе ViewModel

        public ICommand ConnectCommand { get; private set; }
        public ICommand DisconnectCommand { get; private set; }
        public ICommand ZeroCalibrationCommand { get; private set; }
        public ICommand SaveResultsCommand { get; private set; }
        public ICommand ClearFieldsCommand { get; private set; }
        public ICommand TestCommand { get; private set; }
        public ICommand SwitchLanguageCommand { get; private set; } // Принимает параметр (код языка)

        public ICommand MeasureCommand { get; private set; } // Принимает параметр (имя точки измерения)

        // Новые команды для обработки ввода в текстовых полях (по Enter, например)
        public ICommand ApplySerialNumberCommand { get; private set; } // Применяет введенный SN

        public ICommand ApplyMeasurementTimeCommand { get; private set; } // Применяет введенное время

        // --- Конструктор ViewModel ---
        // Получает экземпляры всех необходимых сервисов через параметры (Инъекция Зависимостей)
        public MainWindowViewModel(IColorMeasurementService colorMeasurementService, IFileService fileService, IDialogService dialogService)
        {
            // Проверяем, что сервисы были корректно предоставлены
            _colorMeasurementService = colorMeasurementService ?? throw new ArgumentNullException(nameof(colorMeasurementService));
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

            // Инициализация коллекций
            LogMessages = new ObservableCollection<string>();
            //MeasurementStatuses = new ObservableCollection<MeasurementStatusViewModel>();

            // Инициализация свойств по умолчанию (как при старте приложения)
            _measurementTime = DefaultMeasurementTime;
            _serialNumber = ""; // Пустая строка по умолчанию

            // Получаем Dispatcher UI-потока
            _dispatcher = Application.Current.Dispatcher;

            // Инициализация команд, связывая их с методами Execute/CanExecute
            // Используем RelayCommand, который находится в папке Commands
            ConnectCommand = new RelayCommand(ExecuteConnectAsync, CanExecuteConnect); // Асинхронная команда
            DisconnectCommand = new RelayCommand(ExecuteDisconnect, CanExecuteDisconnect); // Синхронная команда (операция быстрая)
            ZeroCalibrationCommand = new RelayCommand(ExecuteZeroCalibrationAsync, CanExecuteZeroCalibration); // Асинхронная команда
            SaveResultsCommand = new RelayCommand(ExecuteSaveResultsAsync, CanExecuteSaveResults); // Асинхронная команда
            ClearFieldsCommand = new RelayCommand(ExecuteClearFields, CanExecuteClearFields); // Синхронная команда, с CanExecute
            TestCommand = new RelayCommand(ExecuteTest, CanExecuteTest); // Синхронная команда, с CanExecute
            SwitchLanguageCommand = new RelayCommand(ExecuteSwitchLanguage, CanExecuteSwitchLanguage); // Синхронная команда, с CanExecute

            // Команда измерения - принимает string parameter (имя точки)
            MeasureCommand = new RelayCommand(ExecuteMeasureAsync, CanExecuteMeasure); // Асинхронная команда

            // Инициализация новых команд для ввода в поля
            ApplySerialNumberCommand = new RelayCommand(ExecuteApplySerialNumber, CanExecuteApplySerialNumber); // Синхронная команда
            ApplyMeasurementTimeCommand = new RelayCommand(ExecuteApplyMeasurementTime, CanExecuteApplyMeasurementTime); // Синхронная команда

            // Подписка на события сервисов, чтобы ViewModel мог реагировать на их активность
            // и обновлять UI/состояние через свойства ViewModel или лог
            _colorMeasurementService.StatusMessage += (sender, message) => AddLogMessage(message); // Получаем сообщения от сервиса прибора
            _fileService.StatusMessage += (sender, message) => AddLogMessage(message); // Получаем сообщения от сервиса файлов

            // Обновляем свойства статуса ViewModel при изменении статуса в Сервисе
            _colorMeasurementService.ConnectionStatusChanged += (sender, isConnected) =>
            {
                // Маршалируем вызов обратно в UI-поток
                _dispatcher.Invoke(() =>
                {
                    // Этот код теперь будет выполнен в UI-потоке
                    IsDeviceConnected = isConnected;
                    // Все, что должно происходить в UI-потоке при изменении IsDeviceConnected,
                    // должно быть внутри этого блока Dispatcher.Invoke
                    // (Вызовы OnPropertyChanged и UpdateCommandsCanExecute происходят из сеттера)
                });
            };
            _colorMeasurementService.CalibrationStatusChanged += (sender, isCalibrated) => {
                _dispatcher.Invoke(() =>
                {
                    IsDeviceCalibrated = isCalibrated;
                    // UpdateCommandsCanExecute(); // Если это вызывается из сеттера IsDeviceCalibrated, оно тоже будет в UI потоке
                });
            };
        

            // Инициализация начального состояния UI и команд
            //InitializeMeasurementStatuses(); // Создаем начальные статусы для всех точек измерения
            ButtonsStatusInit(); // Инициализация статусов для каждой точки измерения
            UpdateCommandsCanExecute(); // Обновляем доступность всех команд при запуске
            UpdateMeasurementButtonsState(); // Обновляем доступность кнопок измерения при запуске

            AddLogMessage("Приложение запущено. Ожидание подключения..."); // Сообщение при старте
        }

        private void ButtonsStatusInit()
        {

            // Инициализируем коллекцию
            AllMeasurementButtonStatuses = new ObservableCollection<MeasurementStatusViewModel>();

            // Создаем объекты и добавляем ИХ в коллекцию
            var topLeft = new MeasurementStatusViewModel("1. Top left");
            var topCenter = new MeasurementStatusViewModel("2. Top center");
            var topRight = new MeasurementStatusViewModel("3. Top right");
            var middleLeft = new MeasurementStatusViewModel("4. Middle left");
            var center = new MeasurementStatusViewModel("5. Center");
            var middleRight= new MeasurementStatusViewModel("6. Middle right");
            var bottomLeft = new MeasurementStatusViewModel("7. Bottom left");
            var bottomCenter = new MeasurementStatusViewModel("8. Bottom center");
            var bottomRight = new MeasurementStatusViewModel("9. Bottom right");
            var red = new MeasurementStatusViewModel("R. Red");
            var green = new MeasurementStatusViewModel("G. Green");
            var blue = new MeasurementStatusViewModel("B. Blue");
            var black = new MeasurementStatusViewModel("0. Black");

            AllMeasurementButtonStatuses.Add(topLeft);
            AllMeasurementButtonStatuses.Add(topCenter);
            AllMeasurementButtonStatuses.Add(topRight);
            AllMeasurementButtonStatuses.Add(middleLeft);
            AllMeasurementButtonStatuses.Add(center);
            AllMeasurementButtonStatuses.Add(middleRight);
            AllMeasurementButtonStatuses.Add(bottomLeft);
            AllMeasurementButtonStatuses.Add(bottomCenter);
            AllMeasurementButtonStatuses.Add(bottomRight);
            AllMeasurementButtonStatuses.Add(red);
            AllMeasurementButtonStatuses.Add(green);
            AllMeasurementButtonStatuses.Add(blue);
            AllMeasurementButtonStatuses.Add(black);

            TopLeftStatus = topLeft;
            TopCenterStatus = topCenter;
            TopRightStatus = topRight;
            MiddleLeftStatus = middleLeft;
            CenterStatus = center;
            MiddleRightStatus = middleRight;
            BottomLeftStatus = bottomLeft;
            BottomCenterStatus = bottomCenter;
            BottomRightStatus = bottomRight;
            RedColorStatus = red;
            GreenColorStatus = green;
            BlueColorStatus = blue;
            BlackColorStatus = black;

            
        }
        // --- Методы ViewModel, реализующие логику команд (Execute...) ---
        // Эти методы содержат основную логику приложения, перенесенную из WinForms обработчиков событий.
        // Они вызывают методы Сервисов и Модели, обновляют свойства ViewModel и коллекции.
        // Имеют параметр object parameter, который может передавать данные из View (например, имя кнопки).

        // Пример реализации асинхронной команды подключения
        private async Task ExecuteConnectAsync(object parameter)
        {
            if (!CanExecuteConnect(parameter)) return; // Проверка доступности

            AddLogMessage("Попытка подключения к прибору...");

            try
            {
                // Вызываем асинхронный метод Сервиса. Результат и статус придут через события.
                bool success = await _colorMeasurementService.ConnectAsync();

                if (success)
                {
                    AddLogMessage("Прибор успешно подключен!");

                    // После успешного подключения, возможно, сразу выполняем калибровку нуля
                    // Проверяем доступность команды калибровки
                    if (CanExecuteZeroCalibration(null))
                    {
                        AddLogMessage("Автоматическая калибровка нуля...");
                        // Вызываем метод команды калибровки (или напрямую сервис, если логика простая)
                        await ExecuteZeroCalibrationAsync(null);
                    }
                    else if (!IsDeviceCalibrated) // Если прибор подключен, но не откалиброван
                    {
                        AddLogMessage("Прибор подключен, но не откалиброван. Выполните калибровку нуля.");
                    }
                }
                else
                {
                    AddLogMessage("Подключение не выполнено или завершилось ошибкой.");
                }
            }
            catch (Exception ex)
            {
                // Обработка непредвиденных ошибок
                AddLogMessage($"Непредвиденная ошибка при выполнении команды 'Подключить': {ex.Message}");
                _dialogService.ShowMessage($"Ошибка подключения: {ex.Message}", "Ошибка"); // Показать сообщение пользователю
            }

            // Доступность команд и кнопок обновится автоматически через подписки на события сервиса
            // UpdateCommandsCanExecute(); // Можно вызвать явно, если нужно гарантировать немедленное обновление
        }

        // Реализация синхронной команды отключения
        private void ExecuteDisconnect(object parameter)
        {
            if (!CanExecuteDisconnect(parameter)) return;

            AddLogMessage("Отключение прибора...");

            try
            {
                _colorMeasurementService.Disconnect(); // Вызываем метод Сервиса

                // После отключения сбрасываем статус калибровки в ViewModel
                IsDeviceCalibrated = false; // При отключении прибора калибровка сбрасывается
                // IsDeviceConnected обновится через подписку на событие сервиса

                AddLogMessage("Прибор отключен.");
            }
            catch (Exception ex)
            {
                AddLogMessage($"Непредвиденная ошибка при выполнении команды 'Отключить': {ex.Message}");
                _dialogService.ShowMessage($"Ошибка отключения: {ex.Message}", "Ошибка");
            }

            // Обновляем доступность команд и кнопок
            UpdateCommandsCanExecute();
            UpdateMeasurementButtonsState();
        }

        // Реализация асинхронной команды калибровки нуля
        private async Task ExecuteZeroCalibrationAsync(object parameter) // Возвращаем Task
        {
            if (!CanExecuteZeroCalibration(parameter)) return;

            AddLogMessage("Выполняется калибровка нуля...");

            try
            {
                // Вызываем асинхронный метод Сервиса
                bool success = await _colorMeasurementService.CalibrateZeroAsync();

                if (success)
                {
                    AddLogMessage("Калибровка нуля завершена успешно!");
                    // IsDeviceCalibrated обновится через подписку
                }
                else
                {
                    AddLogMessage("Калибровка нуля не выполнена или завершилась ошибкой.");
                    _dialogService.ShowMessage("Калибровка нуля не выполнена.", "Ошибка калибровки");
                    // IsDeviceCalibrated обновится через подписку
                }
            }
            catch (Exception ex)
            {
                AddLogMessage($"Непредвиденная ошибка при выполнении команды 'Калибровка нуля': {ex.Message}");
                _dialogService.ShowMessage($"Ошибка калибровки: {ex.Message}", "Ошибка");
            }

            // Обновляем доступность команд и кнопок (непосредственно после завершения калибровки)
            UpdateCommandsCanExecute();
            UpdateMeasurementButtonsState();

            // Поскольку метод теперь async Task, его можно ожидать (await)
        }

        // Реализация асинхронной команды сохранения результатов
        private async Task ExecuteSaveResultsAsync(object parameter)
        {
            if (!CanExecuteSaveResults(parameter)) return;

            AddLogMessage("Сохранение результатов...");

            try
            {
                // Проверяем, есть ли данные для сохранения и SN
                if (_currentDevice == null || string.IsNullOrWhiteSpace(_currentDevice.SerialNumber) || _currentDevice.Measurements.Count == 0)
                {
                    AddLogMessage("Ошибка сохранения: Нет данных устройства или измерений для сохранения.");
                    _dialogService.ShowMessage("Нет данных устройства или измерений для сохранения.", "Ошибка сохранения");
                    return;
                }

                // Проверяем полноту измерений и запрашиваем подтверждение, если не все собраны
                if (!_currentDevice.IsContainsAllMeasurements(_requiredMeasurementNames))
                {
                    bool confirmSave = _dialogService.ShowQuestion("Собраны не все измерения. Сохранить текущие данные?", "Предупреждение");
                    if (!confirmSave)
                    {
                        AddLogMessage("Сохранение отменено пользователем.");
                        return;
                    }
                }

                // Вызываем асинхронный метод Сервиса Файлов для сохранения в JSON
                // FileService сам отправит сообщения в лог через StatusMessage
                bool saveSuccess = await _fileService.SaveDeviceDataToJsonAsync(_currentDevice);

                if (saveSuccess)
                {
                    AddLogMessage("Результаты сохранены в JSON.");
                    _dialogService.ShowMessage("Результаты успешно сохранены.", "Сохранение завершено");

                    // Очищаем поля после успешного сохранения, как было в WinForms
                    // ExecuteClearFields(null);
                }
                else
                {
                    AddLogMessage("Сохранение результатов завершилось ошибкой. Подробности выше.");
                    _dialogService.ShowMessage("Произошла ошибка при сохранении результатов. Подробности в логе.", "Ошибка сохранения");
                }
            }
            catch (Exception ex)
            {
                AddLogMessage($"Непредвиденная ошибка при выполнении команды 'Сохранить': {ex.Message}");
                _dialogService.ShowMessage($"Непредвиденная ошибка при сохранении: {ex.Message}", "Ошибка");
            }
            // Доступность команды Сохранить обновится в ExecuteClearFields или по UpdateCommandsCanExecute
        }

        // Реализация синхронной команды очистки полей
        private void ExecuteClearFields(object parameter)
        {
            if (!CanExecuteClearFields(parameter)) return; // Хотя обычно всегда true

            // Запрашиваем подтверждение очистки
            bool confirm = _dialogService.ShowQuestion("Очистить все поля?", "Предупреждение");

            if (confirm)
            {
                // Очистка свойств ViewModel
                SerialNumber = ""; // Сеттер обновит UI и вызовет UpdateMeasurementButtonsState/UpdateCommandsCanExecute
                MeasurementTime = DefaultMeasurementTime; // Сеттер обновит UI и запишет в лог

                // Сброс текущего объекта Модели DeviceUnderTest
                _currentDevice = null;

                // Очистка коллекций ViewModel
                LogMessages.Clear(); // ObservableCollection уведомит UI
                ResetMeasurementStatuses(); // Сбрасываем статусы всех точек измерения (обновит UI через MeasurementStatusViewModel)

                AddLogMessage("Все поля очищены.");

                // Явно обновляем доступность команд и кнопок, так как состояние сброшено
                UpdateCommandsCanExecute();
                UpdateMeasurementButtonsState();
            }
        }

        // Реализация синхронной команды тестирования (генерация тестовых данных)
        private void ExecuteTest(object parameter)
        {
            if (!CanExecuteTest(parameter)) return; // Хотя обычно всегда true

            AddLogMessage("Выполняется команда: Тест (генерация тестовых данных)");

            try
            {
                // Логика генерации тестовых данных из старого метода Test()
                // Убеждаемся, что есть объект DeviceUnderTest (создаем, если нет)
                if (_currentDevice == null || string.IsNullOrWhiteSpace(_currentDevice.SerialNumber))
                {
                    // Если SN не введен, используем тестовый
                    if (string.IsNullOrWhiteSpace(SerialNumber))
                    {
                        SerialNumber = "TEST_SN";
                        AddLogMessage("Серийный номер не был указан, установлен тестовый SN: TEST_SN");
                    }
                    _currentDevice = new DeviceUnderTest(SerialNumber); // Создаем новый тестовый объект
                    AddLogMessage($"Начата генерация тестовых данных для SN: {_currentDevice.SerialNumber}");
                }
                else
                {
                    // Если устройство уже есть, очищаем его старые измерения для нового теста
                    _currentDevice.Measurements.Clear();
                    ResetMeasurementStatuses(); // Сбрасываем статусы кнопок для чистого теста
                    AddLogMessage($"Очищены старые данные устройства с SN: {_currentDevice.SerialNumber} для нового теста.");
                }

                // Генерируем тестовые измерения для каждой требуемой точки
                var random = new Random();
                foreach (var name in _requiredMeasurementNames)
                {
                    // Генерация случайных значений
                    double testX = 0.3 + (random.NextDouble() * 0.1);
                    double testY = 0.3 + (random.NextDouble() * 0.1);
                    double testLv = 50 + (random.NextDouble() * 100); // Диапазон для Lv (кроме Black)
                    double testT = 5000 + (random.NextDouble() * 1500);

                    // Специальные тестовые значения для BlackColor, если нужно
                    if (name == "BlackColor")
                    {
                        testLv = random.NextDouble() * 5; // Lv < 10 для Black
                        testT = 6500;
                    }

                    // Создаем объект Модели Measurement с тестовыми данными
                    var testMeasurement = new Measurement(name, testX, testY, testLv, testT);
                    // Модель DeviceUnderTest.AddMeasurement заменит старое измерение с таким же Location

                    // Добавляем тестовое измерение в коллекцию Модели
                    _currentDevice.AddMeasurement(testMeasurement);

                    // Логика валидации Lv для определения статуса (как в ExecuteMeasureAsync)
                    bool? isPassed = (name == BlackColorStatus.Location || testMeasurement.Lv >= 10); // Считаем успешным, если Lv >= 10 (кроме Black)

                    // Форматируем значения для отображения в логе/UI
                    string LvFormatted = (name == BlackColorStatus.Location) ?
                                        testMeasurement.Lv.ToString("F4", CultureInfo.InvariantCulture) :
                                        testMeasurement.Lv.ToString("F1", CultureInfo.InvariantCulture);
                    string TFormatted = testMeasurement.T.ToString("F0", CultureInfo.InvariantCulture);
                    string testValuesString = $"x={testMeasurement.x:F3}, y={testMeasurement.y:F3}, Lv={LvFormatted}, T={TFormatted}";

                    AddLogMessage($"Сгенерировано тестовое измерение '{name}': {testValuesString}");

                    // Обновляем статус точки в ViewModel (для UI)
                    UpdateMeasurementStatus(name, isPassed, testValuesString);
                }

                AddLogMessage("Генерация тестовых данных завершена.");

                // После генерации теста, команда Сохранить становится доступной
                UpdateCommandsCanExecute(); // Уведомляем команды, что их доступность могла измениться
            }
            catch (Exception ex)
            {
                AddLogMessage($"Непредвиденная ошибка при выполнении команды 'Тест': {ex.Message}");
                _dialogService.ShowMessage($"Ошибка теста: {ex.Message}", "Ошибка");
            }
        }

        // Реализация синхронной команды смены языка
        private void ExecuteSwitchLanguage(object parameter)
        {
            if (!CanExecuteSwitchLanguage(parameter)) return;

            string languageCode = parameter as string; // Получаем код языка из параметра команды
            if (string.IsNullOrWhiteSpace(languageCode))
            {
                AddLogMessage("Ошибка смены языка: Не указан код языка.");
                return;
            }

            AddLogMessage($"Попытка переключения языка на '{languageCode}'...");

            try
            {
                // Здесь должна быть логика вызова Сервиса Локализации
                // ILocalizationService _localizationService; // Нужно добавить зависимость и инициализировать в конструкторе
                // _localizationService.SetLanguage(languageCode);

                // Обновление UI после смены языка происходит автоматически в WPF при правильной реализации локализации (через ResourceDictionary и CultureInfo)
                // Если ViewModel содержит строки, не привязанные к ресурсам, их нужно обновить вручную или через событие сервиса локализации.

                AddLogMessage($"Язык переключен на '{languageCode}'.");
                _dialogService.ShowMessage($"Язык переключен на {languageCode}.", "Информация");

                // Если смена языка влияет на логику доступности команд (редко), вызвать UpdateCommandsCanExecute();
            }
            catch (Exception ex)
            {
                AddLogMessage($"Непредвиденная ошибка при выполнении команды 'Смена языка': {ex.Message}");
                _dialogService.ShowMessage($"Ошибка при смене языка: {ex.Message}", "Ошибка");
            }
        }

        // Реализация асинхронной команды измерения (вызывается для каждой точки измерения)
        private async Task ExecuteMeasureAsync(object parameter)
        {
            // Получаем имя точки измерения из параметра команды
            string measurementName = parameter as string;
            if (string.IsNullOrWhiteSpace(measurementName))
            {
                AddLogMessage("Ошибка измерения: Не указана точка измерения (параметр команды отсутствует).");
                return;
            }

            if (!CanExecuteMeasure(parameter)) // Проверка доступности
            {
                // Если команда недоступна, но ее попытались вызвать (например, кнопка не была отключена),
                // можно вывести сообщение или просто выйти.
                AddLogMessage($"Измерение '{measurementName}' не может быть выполнено сейчас (прибор не готов или нет SN).");
                return;
            }

            AddLogMessage($"Выполняется измерение '{measurementName}'...");

            // Обновляем статус этой точки в UI на "измерение в процессе" (опционально)
            UpdateMeasurementStatus(measurementName, null, "Измерение..."); // null или кастомный статус

            try
            {
                // 1. Убеждаемся, что есть объект DeviceUnderTest с серийным номером
                // Эту логику можно вынести или обрабатывать в ExecuteApplySerialNumber
                if (_currentDevice == null || string.IsNullOrWhiteSpace(_currentDevice.SerialNumber) || _currentDevice.SerialNumber != SerialNumber)
                {
                    // Проверяем SN из свойства ViewModel, т.к. оно привязано к TextBox
                    if (string.IsNullOrWhiteSpace(SerialNumber))
                    {
                        AddLogMessage("Ошибка измерения: Введите серийный номер устройства.");
                        _dialogService.ShowMessage("Введите серийный номер устройства.", "Ошибка");
                        UpdateMeasurementStatus(measurementName, false, "Нет SN"); // Сбрасываем статус точки на ошибку
                        return;
                    }
                    // Валидация формата SN
                    if (!System.Text.RegularExpressions.Regex.IsMatch(SerialNumber, SerialNumberPattern))
                    {
                        AddLogMessage($"Ошибка измерения: Некорректный формат серийного номера '{SerialNumber}'.");
                        _dialogService.ShowMessage("Некорректный формат серийного номера. Используйте только буквы и цифры.", "Ошибка");
                        UpdateMeasurementStatus(measurementName, false, "Ошибка SN"); // Сбрасываем статус точки на ошибку
                        return;
                    }

                    // Создаем новый объект DeviceUnderTest, если его нет или SN изменился
                    _currentDevice = new DeviceUnderTest(SerialNumber);
                    AddLogMessage($"Начато тестирование устройства с SN: {_currentDevice.SerialNumber}");
                    // Сбрасываем статусы измерений для нового устройства
                    ResetMeasurementStatuses();
                    // Обновляем статус текущей точки измерения (если она была измерена ранее с другим устройством)
                    UpdateMeasurementStatus(measurementName, null, "Измерение..."); // Статус "в процессе" для новой точки
                }

                // 2. Вызываем асинхронный метод измерения у Сервиса
                // Передаем время измерения из свойства ViewModel
                // Сервис выполнит усреднение и вернет Measurement
                Measurement resultMeasurement = await _colorMeasurementService.MeasureAsync();

                // 3. Обработка результата измерения, валидация, форматирование
                bool isMeasurmentSuccess = false;
                string measuredValuesDisplay = "Нет данных"; // Строка для отображения результата в UI

                // Проверяем результат от сервиса: не null и IsValid == true
                if (resultMeasurement != null && resultMeasurement.IsValid)
                {
                    // Логика валидации Lv < 10 (для всех, кроме BlackColor)
                    bool lvValidationPassed = true;
                    if (measurementName != "BlackColor" && resultMeasurement.Lv < 10)
                    {
                        AddLogMessage($"Внимание: Яркость Lv ({resultMeasurement.Lv:F1}) < 10 для '{measurementName}'.");
                        lvValidationPassed = false; // Валидация по Lv не пройдена
                        resultMeasurement.IsValid = false; // Отмечаем измерение как невалидное в модели, если не прошло Lv валидацию здесь
                    }

                    if (lvValidationPassed) // Если валидация по Lv пройдена (и сервис вернул Valid=true)
                    {
                        // Логика форматирования для вывода в лог/UI
                        string LvFormatted = (measurementName == "BlackColor") ?
                                            resultMeasurement.Lv.ToString("F4", CultureInfo.InvariantCulture) :
                                            resultMeasurement.Lv.ToString("F1", CultureInfo.InvariantCulture);
                        string TFormatted = resultMeasurement.T.ToString("F0", CultureInfo.InvariantCulture);

                        measuredValuesDisplay = $"x={resultMeasurement.x:F3}, y={resultMeasurement.y:F3}, Lv={LvFormatted}, T={TFormatted}";

                        AddLogMessage($"Результат '{measurementName}': {measuredValuesDisplay}");

                        // Добавляем измерение (объект Модели) в коллекцию устройства (объект Модели)
                        // Метод AddMeasurement в DeviceUnderTest позаботится о замене по Location
                        resultMeasurement.Location = measurementName; // Устанавливаем имя точки в объекте измерения перед добавлением
                        _currentDevice.AddMeasurement(resultMeasurement);

                        // Логика сохранения в CSV - это задача Сервиса Файлов
                        // Форматируем CSV строку здесь или в Measurement
                        string csvString = $"{resultMeasurement.Location},{resultMeasurement.x.ToString(CultureInfo.InvariantCulture)},{resultMeasurement.y.ToString(CultureInfo.InvariantCulture)},{resultMeasurement.Lv.ToString(CultureInfo.InvariantCulture)},{resultMeasurement.T.ToString(CultureInfo.InvariantCulture)}";
                        // Возможно, форматирование должно быть более точным, как в WinForms

                        // Вызываем асинхронный метод сохранения CSV у Сервиса Файлов
                        await _fileService.SaveMeasurementToCsvAsync(csvString, resultMeasurement.Location, _currentDevice.SerialNumber);

                        isMeasurmentSuccess = true; // Измерение успешно выполнено и обработано
                    }
                    else // Не прошло Lv валидацию
                    {
                        isMeasurmentSuccess = false; // Считаем измерение неуспешным
                        measuredValuesDisplay = $"Ошибка Lv < 10 ({resultMeasurement.Lv:F1})"; // Сообщение для UI
                    }
                }
                else // Сервис вернул null или resultMeasurement.IsValid == false
                {
                    if (resultMeasurement != null && !resultMeasurement.IsValid)
                    {
                        AddLogMessage($"Измерение '{measurementName}' вернуло невалидный результат от прибора.");
                        measuredValuesDisplay = "Невалидный результат";
                    }
                    else // resultMeasurement == null
                    {
                        AddLogMessage($"Ошибка: Сервис не вернул результат измерения для '{measurementName}'.");
                        measuredValuesDisplay = "Ошибка прибора";
                    }
                    isMeasurmentSuccess = false; // Считаем измерение неуспешным
                }

                // 4. Обновление статуса измерения для данной точки в коллекции ViewModel (для обновления UI)
                UpdateMeasurementStatus(measurementName, isMeasurmentSuccess, measuredValuesDisplay);

                // После измерения, возможно, команда Сохранить стала доступной (если собраны все измерения)
                UpdateCommandsCanExecute(); // Проверяем доступность команд
            }
            catch (Exception ex)
            {
                // Обработка непредвиденных ошибок при выполнении команды измерения
                AddLogMessage($"Непредвиденная ошибка при измерении '{measurementName}': {ex.Message}");
                _dialogService.ShowMessage($"Ошибка при измерении '{measurementName}': {ex.Message}", "Ошибка");

                // Помечаем статус точки как ошибочный в ViewModel
                UpdateMeasurementStatus(measurementName, false, $"Ошибка: {ex.Message}");
                UpdateCommandsCanExecute(); // Проверяем доступность команд
            }
        }

        // Реализация команды для применения введенного Серийного номера (например, по Enter)
        private void ExecuteApplySerialNumber(object parameter)
        {
            // Логика из SerialNumberTextBox_KeyDown
            if (!CanExecuteApplySerialNumber(parameter)) return;

            AddLogMessage($"Применен серийный номер: {SerialNumber}");

            // Здесь можно добавить более сложную логику, связанную с применением SN:
            // Например, создание или сброс объекта DeviceUnderTest
            // Убедимся, что _currentDevice соответствует SerialNumber из ViewModel
            if (_currentDevice == null || _currentDevice.SerialNumber != SerialNumber)
            {
                // Если устройство еще не создано или SN изменился, создаем новое
                try
                {
                    _currentDevice = new DeviceUnderTest(SerialNumber);
                    AddLogMessage($"Создан новый объект DeviceUnderTest с SN: {_currentDevice.SerialNumber}");
                    // Сбрасываем все предыдущие измерения и статусы при смене устройства
                    ResetMeasurementStatuses();
                }
                catch (ArgumentException ex)
                {
                    // Ошибка валидации в конструкторе Модели
                    AddLogMessage($"Ошибка: {ex.Message}");
                    _dialogService.ShowMessage(ex.Message, "Ошибка серийного номера");
                    // Сбрасываем SerialNumber ViewModel на пустую строку, если он невалиден
                    SerialNumber = ""; // Это вызовет OnPropertyChanged и обновит UI
                    _currentDevice = null; // Сбрасываем объект Модели
                    ResetMeasurementStatuses(); // Сбрасываем статусы
                }
            }
            else
            {
                // Если устройство уже соответствует SN, возможно, ничего не нужно делать,
                // или просто логируем подтверждение.
                AddLogMessage($"Серийный номер '{SerialNumber}' уже был активен.");
            }

            // Обновляем доступность кнопок измерения (зависит от наличия SN)
            UpdateMeasurementButtonsState();
            // Обновляем доступность других команд, которые зависят от наличия SN (например, Сохранить, Измерение)
            UpdateCommandsCanExecute();
        }

        // Реализация команды для применения введенного Времени измерения (например, по Enter)
        private void ExecuteApplyMeasurementTime(object parameter)
        {
            // Логика из TimeTextBox_KeyDown
            if (!CanExecuteApplyMeasurementTime(parameter)) return;

            // Значение уже должно быть в свойстве MeasurementTime благодаря привязке TwoWay
            // Валидация уже выполняется в сеттере свойства MeasurementTime
            // Если сеттер не изменил значение (из-за невалидного ввода), можно вывести доп. сообщение здесь
            if (_measurementTime <= 0)
            {
                // Сообщение уже было добавлено в сеттере, можно показать диалог
                _dialogService.ShowMessage("Введите корректное время измерения (больше 0).", "Некорректный ввод");
            }
            else
            {
                // Время успешно применено, сообщение в логе уже есть из сеттера свойства
                AddLogMessage($"Время измерения подтверждено: {MeasurementTime} сек.");
            }

            // Применение времени измерения обычно не влияет на доступность команд,
            // но если влияет, нужно вызвать UpdateCommandsCanExecute();
        }

        // --- Методы ViewModel, проверяющие доступность команд (CanExecute...) ---
        // Эти методы возвращают true, если команда доступна, и false, если нет.
        // WPF вызывает эти методы, чтобы определить, должны ли элементы UI (например, кнопки) быть активными.
        // Они должны быть "чистыми" - не менять состояние, только возвращать bool на основе текущих свойств ViewModel.

        // Проверка доступности команды Подключить: доступна, если прибор НЕ подключен
        private bool CanExecuteConnect(object parameter)
        {
            return !IsDeviceConnected; // Используем публичное свойство
        }

        // Проверка доступности команды Отключить: доступна, если прибор ПОДКЛЮЧЕН
        private bool CanExecuteDisconnect(object parameter)
        {
            return IsDeviceConnected; // Используем публичное свойство
        }

        // Проверка доступности команды Калибровка нуля: доступна, если прибор ПОДКЛЮЧЕН И НЕ КАЛИБРОВАН
        private bool CanExecuteZeroCalibration(object parameter)
        {
            return true;
        }

        // Проверка доступности команды Сохранить: есть объект устройства, серийный номер и измерения
        private bool CanExecuteSaveResults(object parameter)
        {
            // Команда доступна, если объект _currentDevice создан И у него есть SN И в нем есть хотя бы 1 измерение
            return _currentDevice != null && !string.IsNullOrWhiteSpace(_currentDevice.SerialNumber) && _currentDevice.Measurements.Count > 0;
            // Опционально, можно требовать, чтобы были собраны ВСЕ необходимые измерения:
            // return _currentDevice != null && !string.IsNullOrWhiteSpace(_currentDevice.SerialNumber) && _currentDevice.IsContainsAllMeasurements(_requiredMeasurementNames);
        }

        // Проверка доступности команды Очистить: всегда доступна
        private bool CanExecuteClearFields(object parameter)
        {
            return true; // Команда очистки всегда доступна
        }

        // Проверка доступности команды Тест: всегда доступна
        private bool CanExecuteTest(object parameter)
        {
            return true; // Команда тестирования всегда доступна
        }

        // Проверка доступности команды Смена языка: всегда доступна (или зависит от списка доступных языков)
        private bool CanExecuteSwitchLanguage(object parameter)
        {
            // Команда требует параметр - код языка. Проверяем, что параметр передан и он строка.
            return parameter is string languageCode && !string.IsNullOrWhiteSpace(languageCode);
            // Если список доступных языков динамический, можно добавить проверку, что languageCode есть в этом списке.
        }

        // Проверка доступности команды Измерение: указана точка, прибор ПОДКЛЮЧЕН, ОТКАЛИБРОВАН, введен SN
        private bool CanExecuteMeasure(object parameter)
        {
            // Команда требует параметр - имя точки измерения.
            bool hasMeasurementName = parameter is string measurementName && !string.IsNullOrWhiteSpace(measurementName);

            // Команда Измерение доступна, если все условия истинны:
            return hasMeasurementName && IsDeviceConnected && IsDeviceCalibrated && !string.IsNullOrWhiteSpace(SerialNumber);
        }

        // Проверка доступности команды ApplySerialNumber: доступна, если Серийный номер в поле не пустой
        private bool CanExecuteApplySerialNumber(object parameter)
        {
            // Команда доступна, если свойство SerialNumber (которое привязано к TextBox) не пустое
            return !string.IsNullOrWhiteSpace(SerialNumber);
            // Можно добавить валидацию формата здесь, но лучше делать это в Execute или сеттере свойства
            // return !string.IsNullOrWhiteSpace(SerialNumber) && Regex.IsMatch(SerialNumber, SerialNumberPattern);
        }

        // Проверка доступности команды ApplyMeasurementTime: доступна, если время в поле валидно (например, > 0)
        private bool CanExecuteApplyMeasurementTime(object parameter)
        {
            // Команда доступна, если свойство MeasurementTime (которое привязано к TextBox) > 0
            // Валидация уже происходит в сеттере свойства.
            return MeasurementTime > 0; // Используем публичное свойство
        }

        // --- Вспомогательные методы ViewModel (для внутренней логики ViewModel) ---
        // Эти методы помогают организовать код внутри ViewModel, но не привязаны напрямую к UI.

        // Метод для добавления сообщения в коллекцию логов
        // Используем Dispatcher для потокобезопасного доступа к ObservableCollection
        private void AddLogMessage(string message)
        {
            // App.Current.Dispatcher.Invoke выполнит действие в UI потоке
            App.Current.Dispatcher.Invoke(() =>
            {
                LogMessages.Add($"{DateTime.Now.ToString("HH:mm:ss")} - {message}");
                // Опционально: ограничить количество сообщений в логе
                if (LogMessages.Count > 500) // Например, держать не более 500 сообщений
                {
                    LogMessages.RemoveAt(0); // Удалить самое старое сообщение
                }
            });
        }

        // Метод для обновления состояния доступности кнопок измерения
        // Управляет свойством IsMeasurementButtonsEnabled
        // Вызывается, когда изменяются свойства, от которых зависит доступность (Connected, Calibrated, SerialNumber)
        private void UpdateMeasurementButtonsState()
        {
            IsMeasurementButtonsEnabled = IsDeviceConnected && IsDeviceCalibrated && !string.IsNullOrWhiteSpace(SerialNumber);

            // Важно: После обновления состояния кнопок, уведомляем команду MeasureCommand
            // о возможном изменении ее доступности, чтобы UI (кнопки) обновился.
            (MeasureCommand as RelayCommand)?.RaiseCanExecuteChanged(); // Используем безопасное приведение и ?.
        }

        // Метод для уведомления ВСЕХ команд о возможном изменении их состояния CanExecute
        // Вызывается, когда меняются свойства, которые влияют на доступность многих команд (например, IsDeviceConnected)
        private void UpdateCommandsCanExecute()
        {
            // Для каждой команды, у которой есть метод CanExecute, вызываем RaiseCanExecuteChanged
            // Это заставляет WPF перепроверить CanExecute для этих команд
            ((RelayCommand)ConnectCommand)?.RaiseCanExecuteChanged();
            ((RelayCommand)DisconnectCommand)?.RaiseCanExecuteChanged();
            ((RelayCommand)ZeroCalibrationCommand)?.RaiseCanExecuteChanged();
            ((RelayCommand)MeasureCommand)?.RaiseCanExecuteChanged(); // Повторно, если MeasureButtonState не покрыл
            ((RelayCommand)SaveResultsCommand)?.RaiseCanExecuteChanged();
            // Для команд ClearFields, Test, SwitchLanguage, Apply... тоже можно вызвать, если их CanExecute
            // зависит от динамических свойств (кроме тех, которые всегда true)
            ((RelayCommand)ApplySerialNumberCommand)?.RaiseCanExecuteChanged();
            ((RelayCommand)ApplyMeasurementTimeCommand)?.RaiseCanExecuteChanged();
        }

        // --- Вспомогательный ViewModel для статуса одной точки измерения ---
        // Этот класс представляет статус одной точки измерения в UI.
        // Он должен быть либо вложенным public классом в MainWindowViewModel, либо отдельным файлом в папке ViewModels.
        // ОН ДОЛЖЕН НАСЛЕДОВАТЬ ОТ BaseViewModel, чтобы UI мог реагировать на изменения его свойств.

        // Если хочешь, чтобы был отдельный файл:
        // Перенеси этот класс в файл MeasurementStatusViewModel.cs в папке ViewModels.
        // Убедись, что у него public модификатор и правильное пространство имен (WPF_LCD_Test.ViewModels)
        // и что он наследует от BaseViewModel.
        // Если он вложенный, оставь его public class MeasurementStatusViewModel здесь.

        public class MeasurementStatusViewModel : BaseViewModel // Наследует от BaseViewModel
        {
            public string Location { get; set; } // Имя точки измерения

            private bool? _isPassed; // Статус измерения: null - не измерено, true - успешно, false - ошибка

            public bool? IsPassed
            {
                get => _isPassed;
                set
                {
                    if (_isPassed != value)
                    {
                        _isPassed = value;
                        OnPropertyChanged(); // Уведомляем UI об изменении IsPassed
                        OnPropertyChanged(nameof(StatusColor)); // Уведомляем, что свойство StatusColor тоже могло измениться
                    }
                }
            }

            // Свойство для определения цвета в UI (привязка к Background кнопки/TextBlock)
            // Возвращает WPF Brush
            [JsonIgnore] // Обычно это свойство не нужно сохранять в JSON, т.к. оно связано с представлением
            public Brush StatusColor
            {
                get
                {
                    if (IsPassed == true) return Brushes.LightGreen; // Успех
                    if (IsPassed == false) return Brushes.Red;      // Ошибка
                    return Brushes.LightGray; // По умолчанию (не измерено)
                }
            }

            // Свойство для отображения измеренных значений рядом с точкой в UI
            private string _measuredValuesString;

            public string MeasuredValuesString
            {
                get => _measuredValuesString;
                set
                {
                    if (_measuredValuesString != value)
                    {
                        _measuredValuesString = value;
                        OnPropertyChanged(); // Уведомляем UI об изменении текста
                    }
                }
            }

            // Конструктор с параметром (имя точки) для удобства инициализации
            public MeasurementStatusViewModel(string location)
            {
                Location = location;
                IsPassed = null; // Изначально не измерено
                MeasuredValuesString = ""; // Изначально пусто
            }

            // Конструктор по умолчанию (может быть полезен для XAML дизайнера или сериализации)
            public MeasurementStatusViewModel() // Оставь, если хочешь использовать как отдельный класс
            {
                Location = "Unknown";
                IsPassed = null;
                MeasuredValuesString = "";
            }
        }


        // Метод для инициализации коллекции статусов измерений
        // Создает MeasurementStatusViewModel для каждой ожидаемой точки измерения при старте ViewModel
        //private void InitializeMeasurementStatuses()
        //{
        //    MeasurementStatuses.Clear(); // Очищаем коллекцию при инициализации
        //    foreach (var name in _requiredMeasurementNames)
        //    {
        //        // Создаем и добавляем новый объект статуса для каждой точки
        //        MeasurementStatuses.Add(new MeasurementStatusViewModel(name)); // Используем конструктор
        //    }
        //    AddLogMessage($"Инициализированы объекты статусов для {_requiredMeasurementNames.Count} точек измерений.");
        //}

        // Метод для обновления статуса конкретной точки измерения по ее имени
        // Вызывается из ExecuteMeasureAsync после получения результата
        private void UpdateMeasurementStatus(string location, bool? isPassed, string measuredValuesString = null)
        {

            // Вместо всего твоего switch оператора, используем следующий код:

            // 1. Ищем нужный объект MeasurementStatusViewModel в коллекции по его Location
            //    Используем LINQ FirstOrDefault(). Он вернет первый найденный элемент или null, если не найден.
            MeasurementStatusViewModel statusToUpdate = AllMeasurementButtonStatuses.FirstOrDefault(s => s.Location == location);

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
                AddLogMessage($"Ошибка: Не удалось найти статус для точки измерения '{location}' для обновления.");
                // Возможно, нужно показать диалог пользователю, если это критическая ошибка
                // _dialogService.ShowMessage($"Получена неизвестная точка измерения: {location}", "Ошибка обновления статуса");
            }

            // Больше не нужен break или default, потому что мы либо нашли и обновили, либо обработали ошибку поиска.
        }


        // Метод для сброса всех статусов измерений (например, при очистке полей)

        private void ResetMeasurementStatuses()
        {
            // Сбрасываем свойства у каждого публичного объекта статуса
            foreach (MeasurementStatusViewModel measurementStatusViewModel in AllMeasurementButtonStatuses)
            {
                measurementStatusViewModel.IsPassed = null;
                measurementStatusViewModel.MeasuredValuesString = null;
            }
            AddLogMessage("Статусы измерений сброшены.");
        }

        // --- Реализация IDisposable для очистки ресурсов ---
        // Метод вызывается при "уничтожении" ViewModel (например, при закрытии окна)
        // Важно отписаться от событий, чтобы избежать "утечек памяти",
        // и освободить ресурсы сервисов, если они реализуют IDisposable.
        public void Dispose()
        {
            AddLogMessage("Выполняется очистка ресурсов ViewModel...");

            // Отписываемся от событий сервисов
            // Используем оператор -= для отписки. Лямбда-выражения (sender, args) => { ... }
            // должны быть теми же экземплярами делегатов, что использовались при подписке.
            // Если лямбда-выражения создаются "на лету", отписаться от них так не получится.
            // Лучше создавать именованные приватные методы-обработчики и подписываться/отписываться от них.
            // Пример с именованными методами:
            // _colorMeasurementService.StatusMessage += ColorMeasurementService_StatusMessage;
            // ...
            // _colorMeasurementService.StatusMessage -= ColorMeasurementService_StatusMessage;

            // Пример отписки от событий с использованием лямбд (работает, если компилятор кэширует лямбду):
            if (_colorMeasurementService != null)
            {
                // Если подписывался так: += (s, msg) => AddLogMessage(msg);
                // Отписка может потребовать сохранения ссылки на делегат лямбды.
                // Самый надежный способ - использовать именованные методы.

                // Временное решение (если используешь лямбды напрямую в конструкторе):
                // Отписка может не сработать корректно для всех лямбд, созданных в конструкторе.
                // Лучше переписать подписку на именованные методы и отписываться от них.

                // Пример, если переписал подписку на именованные методы:
                // _colorMeasurementService.StatusMessage -= OnColorMeasurementServiceStatusMessage;
                // _colorMeasurementService.ConnectionStatusChanged -= OnColorMeasurementServiceConnectionStatusChanged;
                // _colorMeasurementService.CalibrationStatusChanged -= OnColorMeasurementServiceCalibrationStatusChanged;
                // ...

                // Пока оставим как есть, но имей в виду, что отписка от лямбд, созданных на лету, проблематична.
                // Возможно, для простых случаев (логгирование) это не критично, но для статусов - важно.
            }

            if (_fileService != null)
            {
                // Пример, если переписал подписку на именованные методы:
                // _fileService.StatusMessage -= OnFileServiceStatusMessage;
                // _fileService.SaveOperationCompleted -= OnFileServiceSaveOperationCompleted;
                // ...
            }

            // Вызываем Dispose у сервисов, если они реализуют IDisposable
            // Это важно, чтобы сервисы освободили свои ресурсы (COM-объекты, файловые потоки и т.п.)
            (_colorMeasurementService as IDisposable)?.Dispose();
            (_fileService as IDisposable)?.Dispose();
            (_dialogService as IDisposable)?.Dispose(); // Если DialogService тоже IDisposable

            // Очищаем коллекции в ViewModel (опционально, но хорошая практика при завершении)
            LogMessages.Clear();
            //MeasurementStatuses.Clear();

            // Сбрасываем ссылки на объекты Модели
            _currentDevice = null;

            // Ссылки на сервисы, если они были инжектированы, обычно не сбрасываются здесь,
            // их жизненным циклом управляет контейнер DI.
            // Если ViewModel сам создавал сервисы (что не рекомендуется), тогда их нужно сбросить.

            AddLogMessage("Очистка ресурсов ViewModel завершена.");
        }

        // Пример именованного метода-обработчика события (лучше использовать такой подход для подписки/отписки)
        /*
        private void OnColorMeasurementServiceStatusMessage(object sender, string message)
        {
            AddLogMessage(message); // Просто вызываем существующий вспомогательный метод ViewModel
        }

        private void OnColorMeasurementServiceConnectionStatusChanged(object sender, bool isConnected)
        {
            IsDeviceConnected = isConnected; // Обновляем свойство ViewModel
        }

        private void OnColorMeasurementServiceCalibrationStatusChanged(object sender, bool isCalibrated)
        {
            IsDeviceCalibrated = isCalibrated; // Обновляем свойство ViewModel
        }

        // Добавь подобные обработчики для всех событий, на которые подписываешься в конструкторе
        */
    }
}