using MvvmHelpers;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Input;
using WPF_LCD_Test.Commands;
using WPF_LCD_Test.Interfaces;
using WPF_LCD_Test.Models;
using WPF_LCD_Test.Services;
using static WPF_LCD_Test.Resources.Resources;

namespace WPF_LCD_Test.ViewModels
{
    public partial class MeasurementViewModel : BaseViewModel, IDisposable
    {
        // Сервисы и модель
        private readonly IColorMeasurementService _colorMeasurementService;
        private readonly IFileService _fileService;
        private readonly IDialogService _dialogService;
        private readonly ILocalizationService _localizationService;
        private readonly IDispatcher _dispatcher;
        public DeviceUnderTest? _currentDevice;

        // Состояния и данные UI
        private string _serialNumber;
        private bool _isSerialNumberConfirmed = false;
        private int _measurementTime;
        private string _choosedDeviceConfiguration;
        private readonly string CONFIGS_DIR = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config", "device_configs");
        private bool _isMeasurementButtonsEnabled;
        private bool _isDeviceConnected;
        private bool _isDeviceCalibrated;
        public bool _isDeviceConnecting;
        public bool _isDeviceCalibrating;
        private bool _isTvCheckboxChecked;
        public event EventHandler RequestClearInputFocus;
        private const string SerialNumberPattern = "^[a-zA-Z0-9]*$";
        private const int DefaultMeasurementTime = 2;
        private string _logText = string.Empty;

        // Свойства для привязки к UI
        public bool IsTvCheckboxChecked
        {
            get => _isTvCheckboxChecked;
            set => SetProperty(ref _isTvCheckboxChecked, value);
        }
        public string SelectedDeviceConfiguration
        {
            get => _choosedDeviceConfiguration;
            set => SetProperty(ref _choosedDeviceConfiguration, value);
        }
        public string SerialNumber
        {
            get => _serialNumber;
            set
            {
                if (SetProperty(ref _serialNumber, value))
                {
                    UpdateMeasurementButtonsState();
                    UpdateCommandsCanExecute();
                }
            }
        }
        public bool IsSerialNumberConfirmed
        {
            get => _isSerialNumberConfirmed;
            set
            {
                if (SetProperty(ref _isSerialNumberConfirmed, value))
                {
                    UpdateCommandsCanExecute();
                    UpdateMeasurementButtonsState();
                }
            }
        }
        public int MeasurementTime
        {
            get => _measurementTime;
            set
            {
                if (value <= 0)
                {
                    OnPropertyChanged();
                    return;
                }
                SetProperty(ref _measurementTime, value);
            }
        }
        public string LogText
        {
            get => _logText;
            set => SetProperty(ref _logText, value);
        }
        public bool IsMeasurementButtonsEnabled
        {
            get => _isMeasurementButtonsEnabled;
            set => SetProperty(ref _isMeasurementButtonsEnabled, value);
        }
        public bool IsDeviceConnected
        {
            get => _isDeviceConnected;
            set
            {
                if (SetProperty(ref _isDeviceConnected, value))
                {
                    UpdateCommandsCanExecute();
                    UpdateMeasurementButtonsState();
                    OnPropertyChanged(nameof(DeviceConnectionStatusText));
                }
            }
        }
        public bool IsDeviceCalibrated
        {
            get => _isDeviceCalibrated;
            set
            {
                if (SetProperty(ref _isDeviceCalibrated, value))
                {
                    UpdateCommandsCanExecute();
                    UpdateMeasurementButtonsState();
                    OnPropertyChanged(nameof(DeviceCalibrationStatusText));
                }
            }
        }
        public string DeviceConnectionStatusText => _isDeviceConnected ? ConnectedCA : DisconnectedCA;
        public string DeviceCalibrationStatusText => _isDeviceCalibrated ? CalibratedCA : NotCalibratedCa;
        public ObservableCollection<string> DeviceConfigurations { get; } = new();

        // Команды
        public ICommand ZeroCalibrationCommand { get; private set; }
        public ICommand SaveResultsCommand { get; private set; }
        public ICommand ClearFieldsCommand { get; private set; }
        public ICommand ClearLogCommand { get; }
        public ICommand SwitchLanguageCommand { get; private set; }
        public ICommand MeasureCommand { get; private set; }
        public ICommand ApplySerialNumberCommand { get; private set; }
        public ICommand ApplyMeasurementTimeCommand { get; private set; }
        public ICommand NewDeviceUnderTestCommand { get; private set; }
        public ICommand LaunchExternalProgramCommand { get; }

        // Конструктор: инициализация сервисов, команд, начального состояния
        public MeasurementViewModel(
            IColorMeasurementService colorMeasurementService,
            IFileService fileService,
            IDialogService dialogService,
            ILocalizationService localizationService,
            IDispatcher dispatcher
        )
        {
            _colorMeasurementService = colorMeasurementService ?? throw new ArgumentNullException(nameof(colorMeasurementService));
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            _measurementTime = DefaultMeasurementTime;
            _serialNumber = "";
            ZeroCalibrationCommand = new RelayCommand(ExecuteZeroCalibrationAsync, CanExecuteZeroCalibration);
            SaveResultsCommand = new RelayCommand(ExecuteSaveResultsAsync, CanExecuteSaveResults);
            ClearFieldsCommand = new RelayCommand(ExecuteClearFields, CanExecuteClearFields);
            ClearLogCommand = new RelayCommand(ExecuteClearLog);
            SwitchLanguageCommand = new RelayCommand(ExecuteSwitchLanguage, CanExecuteSwitchLanguage);
            NewDeviceUnderTestCommand = new RelayCommand(ExecuteNewDeviceUnderTest, CanExecuteNewDeviceUnderTest);
            LaunchExternalProgramCommand = new RelayCommand(ExecuteLaunchExternalProgramCommand);
            MeasureCommand = new RelayCommand(ExecuteMeasureAsync, CanExecuteMeasure);
            ApplySerialNumberCommand = new RelayCommand(ExecuteApplySerialNumber, CanExecuteApplySerialNumber);
            ApplyMeasurementTimeCommand = new RelayCommand(ExecuteApplyMeasurementTime, CanExecuteApplyMeasurementTime);
            _colorMeasurementService.StatusMessage += ColorMeasurementService_StatusMessage;
            _fileService.StatusMessage += FileService_StatusMessage;
            _colorMeasurementService.ConnectionStatusChanged += ColorMeasurementService_ConnectionStatusChanged;
            _colorMeasurementService.CalibrationStatusChanged += ColorMeasurementService_CalibrationStatusChanged;
            AddLogMessage(WelcomeMessage);
            InitializeDeviceConfigurations();
            UpdateCommandsCanExecute();
            UpdateMeasurementButtonsState();
        }

        // Загружает конфигурации устройств
        private void InitializeDeviceConfigurations()
        {
            try
            {
                if (!Directory.Exists(CONFIGS_DIR))
                {
                    Directory.CreateDirectory(CONFIGS_DIR);
                }
                if (Directory.Exists(CONFIGS_DIR))
                {
                    var configFiles = Directory.GetFiles(CONFIGS_DIR, "*.yaml");
                    DeviceConfigurations.Clear();
                    foreach (var file in configFiles)
                    {
                        DeviceConfigurations.Add(Path.GetFileNameWithoutExtension(file));
                    }
                }
                else
                {
                    AddLogMessage($"{ConfigDirNotFound}: {CONFIGS_DIR}");
                }
                if (DeviceConfigurations.Any())
                {
                    SelectedDeviceConfiguration = DeviceConfigurations.First();
                }
            }
            catch (Exception ex)
            {
                AddLogMessage($"{ErrUnexpected}: {ex.Message}");
            }
        }

        // Проверяет полноту измерений для устройства
        public bool AreAllStatusesRepresentedInMeasurements()
        {
            if (_currentDevice?.Measurements == null || _currentDevice.Measurements.Count == 0)
                return false;
            var allStatusPoints = MeasurementStatusService.Instance.AllMeasurementButtonStatuses;
            if (allStatusPoints == null || !allStatusPoints.Any())
                return false;
            foreach (var status in allStatusPoints)
            {
                if (status.Location == "WhiteColor" && !_currentDevice.IsTV)
                    continue;
                if (!_currentDevice.Measurements.Any(measurement => measurement.Location == status.Location))
                    return false;
            }
            return true;
        }

        // Асинхронная команда подключения
        private async Task ExecuteConnectAsync()
        {
            if (!CanExecuteConnect())
                return;
            try
            {
                _isDeviceConnecting = true;
                if (!await _colorMeasurementService.ConnectAsync())
                {
                    _isDeviceConnecting = false;
                    ExecuteDisconnect();
                }
            }
            catch (Exception)
            {
                ExecuteDisconnect();
            }
            finally
            {
                _isDeviceConnecting = false;
            }
        }

        // Синхронная команда отключения
        private void ExecuteDisconnect()
        {
            if (!CanExecuteDisconnect())
                return;
            try
            {
                _colorMeasurementService.Disconnect();
            }
            catch (Exception ex)
            {
                _dialogService.ShowMessage($"{ErrUnexpected}: {ex.Message}", $"{Err}");
            }
            UpdateCommandsCanExecute();
            UpdateMeasurementButtonsState();
        }

        // Асинхронная команда калибровки нуля
        private async Task ExecuteZeroCalibrationAsync(object parameter)
        {
            CheckCurrentAppLanguage();
            if (!CanExecuteZeroCalibration(parameter))
                return;
            try
            {
                if (!IsDeviceConnected)
                    await ExecuteConnectAsync();
                if (IsDeviceConnected)
                {
                    _isDeviceCalibrating = true;
                    if (!await _colorMeasurementService.CalibrateZeroAsync())
                    {
                        _isDeviceCalibrating = false;
                        ExecuteDisconnect();
                        _dialogService.ShowMessage($"{ErrAtCalibration}", $"{Err}");
                    }
                }
            }
            catch (Exception)
            {
                ExecuteDisconnect();
                _dialogService.ShowMessage($"{ErrAtCalibration}", $"{Err}");
            }
            finally
            {
                _isDeviceCalibrating = false;
            }
            UpdateCommandsCanExecute();
            UpdateMeasurementButtonsState();
        }

        // Асинхронная команда сохранения результатов
        private async Task<bool> ExecuteSaveResultsAsync(object parameter)
        {
            bool result = false;
            CheckCurrentAppLanguage();
            if (!CanExecuteSaveResults(parameter))
                return result;
            AddLogMessage($"{Saving}");
            try
            {
                if (_currentDevice == null || string.IsNullOrWhiteSpace(_currentDevice.SerialNumber) || _currentDevice.Measurements.Count == 0)
                {
                    _dialogService.ShowMessage($"{SaveJSONErrDeviceIsEmpty}", $"{Err}");
                    return result;
                }
                _currentDevice.DeviceConfiguration = SelectedDeviceConfiguration;
                _currentDevice.IsTV = IsTvCheckboxChecked;
                if (!AreAllStatusesRepresentedInMeasurements())
                {
                    bool confirmSave = _dialogService.ShowQuestion($"{SavingNotFullWarning}", $"{Warning}");
                    if (!confirmSave)
                    {
                        AddLogMessage($"{SaveCanceled}");
                        return result;
                    }
                }
                if (await _fileService.SaveDeviceDataToJsonAsync(_currentDevice))
                {
                    result = true;
                }
            }
            catch (Exception ex)
            {
                AddLogMessage($"{ErrUnexpected}: {ex.Message}");
                _dialogService.ShowMessage($"{SaveJSONErrForSN}: {ex.Message}", $"{Err}");
            }
            return result;
        }

        // Синхронная команда очистки полей
        private void ExecuteClearFields(object parameter)
        {
            CheckCurrentAppLanguage();
            if (!CanExecuteClearFields(parameter))
                return;
            string approveQuestion = CleanFieldWarning;
            if (parameter!=null && parameter.Equals("CalledFromNewDeviceMethod"))
                approveQuestion = CleanFieldWarningAfterSave;
            bool confirm = _dialogService.ShowQuestion(approveQuestion, $"{Warning}");
            if (confirm)
            {
                SerialNumber = "";
                MeasurementTime = DefaultMeasurementTime;
                _currentDevice = null;
                IsSerialNumberConfirmed = false;
                ResetMeasurementStatuses();
                AddLogMessage($"{ClearFieldsDone}");
                UpdateCommandsCanExecute();
                UpdateMeasurementButtonsState();
            }
        }

        // Синхронная команда смены языка
        private void ExecuteSwitchLanguage(object parameter)
        {
            if (!CanExecuteSwitchLanguage(parameter))
                return;
            string? languageCode = parameter as string;
            if (string.IsNullOrWhiteSpace(languageCode))
                return;
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

        private static void CheckCurrentAppLanguage()
        {
            CultureInfo culture = LocalizationService.Instance.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
        }

        // Асинхронная команда измерения
        private async Task ExecuteMeasureAsync(object parameter)
        {
            CheckCurrentAppLanguage();
            var measurementName = parameter as string;
            if (string.IsNullOrWhiteSpace(measurementName))
                return;
            if (!CanExecuteMeasure(parameter))
                return;
            UpdateMeasurementStatus(measurementName, null, $"{Measuring}");
            try
            {
                if (_currentDevice == null || string.IsNullOrWhiteSpace(_currentDevice.SerialNumber) || _currentDevice.SerialNumber != SerialNumber)
                {
                    if (string.IsNullOrWhiteSpace(SerialNumber))
                    {
                        _dialogService.ShowMessage($"{FillSN}", $"{Err}");
                        UpdateMeasurementStatus(measurementName, false, $"{NoSNErr}");
                        return;
                    }
                    _currentDevice = new DeviceUnderTest(SerialNumber);
                    AddLogMessage($"{TestStartInfo}: {_currentDevice.SerialNumber}");
                    ResetMeasurementStatuses();
                    UpdateMeasurementStatus(measurementName, null, $"{Measuring}");
                }
                Measurement resultMeasurement = await _colorMeasurementService.MeasureAsync(_measurementTime);
                bool isMeasurmentSuccess = false;
                string measuredValuesDisplay = $"{NoData}";
                if (resultMeasurement != null && resultMeasurement.IsValid)
                {
                    bool lvValidationPassed = true;
                    if (measurementName != MeasurementStatusService.Instance.BlackColorStatus.Location && resultMeasurement.Lv < 10)
                    {
                        AddLogMessage($"{LvIsTooLow}: {resultMeasurement.Lv:F1}. {CheckProbe}");
                        lvValidationPassed = false;
                        resultMeasurement.IsValid = false;
                    }
                    if (lvValidationPassed)
                    {
                        string xFormatted = resultMeasurement.x.ToString("F3", CultureInfo.InvariantCulture);
                        string yFormatted = resultMeasurement.y.ToString("F3", CultureInfo.InvariantCulture);
                        string LvFormatted = (measurementName == MeasurementStatusService.Instance.BlackColorStatus.Location)
                            ? resultMeasurement.Lv.ToString("F6", CultureInfo.InvariantCulture)
                            : resultMeasurement.Lv.ToString("F1", CultureInfo.InvariantCulture);
                        string TFormatted = resultMeasurement.T.ToString("F0", CultureInfo.InvariantCulture);
                        measuredValuesDisplay = $"x={xFormatted}, y={yFormatted}, Lv={LvFormatted}, T={TFormatted}";
                        AddLogMessage($"{Result} '{measurementName}': {measuredValuesDisplay}");
                        resultMeasurement.Location = measurementName;
                        _currentDevice.AddMeasurement(resultMeasurement);
                        isMeasurmentSuccess = true;
                    }
                    else
                    {
                        isMeasurmentSuccess = false;
                        measuredValuesDisplay = $"{LvIsTooLow}: {resultMeasurement.Lv:F1}";
                    }
                }
                else
                {
                    if (resultMeasurement != null && !resultMeasurement.IsValid)
                        measuredValuesDisplay = $"{InvalidResultErr}";
                    else
                    {
                        AddLogMessage($"{ColorServiceErr} '{measurementName}'.");
                        measuredValuesDisplay = $"{ColorAnalyzerErr}";
                    }
                    isMeasurmentSuccess = false;
                }
                UpdateMeasurementStatus(measurementName, isMeasurmentSuccess, measuredValuesDisplay);
                UpdateCommandsCanExecute();
            }
            catch (Exception ex)
            {
                _dialogService.ShowMessage($"{UnexpectedMeasurementErr} '{measurementName}': {ex.Message}", $"{Err}");
                UpdateMeasurementStatus(measurementName, false, $"{Err}: {ex.Message}");
                UpdateCommandsCanExecute();
            }
        }

        // Применяет введённый серийный номер
        public void ExecuteApplySerialNumber(object parameter)
        {
            CheckCurrentAppLanguage();
            var enteredSerialNumber = parameter as string;
            if (!CanExecuteApplySerialNumber(parameter))
                return;
            if (!string.IsNullOrWhiteSpace(enteredSerialNumber) && SerialNumberRegex().IsMatch(enteredSerialNumber))
            {
                SerialNumber = enteredSerialNumber;
                IsSerialNumberConfirmed = true;
                AddLogMessage($"{CurrentSN}: {SerialNumber}");
                RequestClearInputFocus?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                IsSerialNumberConfirmed = false;
                _dialogService.ShowMessage($"{IncorrectFormatForSNErr}", $"{Err}");
                return;
            }
            if (_currentDevice == null || _currentDevice.SerialNumber != SerialNumber)
            {
                try
                {
                    IsSerialNumberConfirmed = true;
                    _currentDevice = new DeviceUnderTest(SerialNumber);
                    Debug.WriteLine(_currentDevice.ToString());
                    ResetMeasurementStatuses();
                }
                catch (ArgumentException ex)
                {
                    IsSerialNumberConfirmed = false;
                    AddLogMessage($"{Err}: {ex.Message}");
                    _dialogService.ShowMessage(ex.Message, $"{Err} SN");
                    SerialNumber = "";
                    _currentDevice = null;
                    UpdateMeasurementButtonsState();
                }
            }
            else
            {
                AddLogMessage($"{Resources.Resources.SerialNumber} {SerialNumber}' {AlreadyActivated}");
            }
            UpdateMeasurementButtonsState();
            UpdateCommandsCanExecute();
        }

        // Применяет введённое время измерения
        private void ExecuteApplyMeasurementTime(object parameter)
        {
            CheckCurrentAppLanguage();
            try
            {
                var enteredMeasurementTime = parameter as string;
                if (string.IsNullOrWhiteSpace(enteredMeasurementTime))
                {
                    _dialogService.ShowMessage($"{IncorrectMeasTimeFormat}", $"{Err}");
                    return;
                }
                int measurementTime = int.Parse(enteredMeasurementTime);
                if (measurementTime <= 0)
                {
                    _dialogService.ShowMessage($"{IncorrectMeasTimeFormat}", $"{Err}");
                }
                else
                {
                    MeasurementTime = measurementTime;
                    AddLogMessage($"{CurrentMeasurementTime}: {MeasurementTime} {Seconds}");
                    RequestClearInputFocus?.Invoke(this, EventArgs.Empty);
                }
            }
            catch
            {
                _dialogService.ShowMessage($"{IncorrectMeasTimeFormat}", $"{Err}");
            }
        }

        // Создаёт новое устройство после сохранения
        private async Task ExecuteNewDeviceUnderTest(object parameter)
        {
            if (await ExecuteSaveResultsAsync(parameter))
                ExecuteClearFields("CalledFromNewDeviceMethod");
            UpdateMeasurementButtonsState();
        }

        public void ExecuteClearLog()
        {
            LogText = string.Empty;
        }

        // Запускает внешнюю программу
        private void ExecuteLaunchExternalProgramCommand(object parameter)
        {
            try
            {
                string executableName = parameter as string;
                if (string.IsNullOrWhiteSpace(executableName))
                {
                    _dialogService.ShowMessage(_localizationService.GetString(RunExternalAppNotFoundErr), _localizationService.GetString(Err));
                    AddLogMessage($"{_localizationService.GetString(RunExternalAppNotFoundErr)}");
                    return;
                }
                bool success = _fileService.RunExternalProgram(executableName);
                if (!success)
                {
                    _dialogService.ShowMessage(_localizationService.GetString(RunExternalAppNotFoundErr), _localizationService.GetString(Err));
                    AddLogMessage($"{_localizationService.GetString(RunExternalAppNotFoundErr)}: {executableName}");
                }
            }
            catch (Exception ex)
            {
                AddLogMessage($"{_localizationService.GetString(RunExternalAppUnexpectedErr)}: {ex.Message}");
                _dialogService.ShowMessage($"{_localizationService.GetString(RunExternalAppUnexpectedErr)}: {ex.Message}", _localizationService.GetString(Err));
            }
        }

        // Методы CanExecute для команд
        private bool CanExecuteConnect() => !IsDeviceConnected && !_isDeviceCalibrating && !_isDeviceConnecting;
        private bool CanExecuteDisconnect() => !_isDeviceConnecting && !_isDeviceCalibrating;
        private bool CanExecuteZeroCalibration(object parameter) => !_isDeviceConnecting && !_isDeviceCalibrating;
        private bool CanExecuteSaveResults(object parameter) => _currentDevice != null && !string.IsNullOrWhiteSpace(_currentDevice.SerialNumber) && _currentDevice.Measurements.Count > 0;
        private bool CanExecuteNewDeviceUnderTest(object paramater) => _currentDevice != null && !string.IsNullOrWhiteSpace(_currentDevice.SerialNumber) && _currentDevice.Measurements.Count > 0;
        private bool CanExecuteClearFields(object parameter) => true;
        private bool CanExecuteSwitchLanguage(object parameter) => parameter is string languageCode && !string.IsNullOrWhiteSpace(languageCode);
        private bool CanExecuteMeasure(object parameter) => IsDeviceConnected && !_isDeviceCalibrating && !_isDeviceConnecting && IsDeviceCalibrated && SerialNumber != "" && (MeasurementTime > 0);
        private bool CanExecuteApplySerialNumber(object parameter) => !string.IsNullOrWhiteSpace(parameter as string) && Regex.IsMatch(parameter as string, SerialNumberPattern);
        private bool CanExecuteApplyMeasurementTime(object parameter) => MeasurementTime > 0;

        // Добавляет сообщение в лог
        public void AddLogMessage(string message)
        {
            if (!string.IsNullOrEmpty(message))
            {
                if (_dispatcher != null)
                {
                    _dispatcher.Invoke(() =>
                    {
                        string timestamp = DateTime.Now.ToString("HH:mm:ss");
                        LogText += $"{timestamp} {message}{Environment.NewLine}";
                    });
                }
                else
                {
                    string timestamp = DateTime.Now.ToString("HH:mm:ss");
                    LogText += $"{timestamp} {message}{Environment.NewLine}";
                }
            }
        }

        // Обновляет доступность кнопок измерения
        private void UpdateMeasurementButtonsState()
        {
            IsMeasurementButtonsEnabled = IsDeviceConnected && IsDeviceCalibrated && !string.IsNullOrWhiteSpace(SerialNumber) && SerialNumber != "";
            (MeasureCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        // Обновляет доступность всех команд
        private void UpdateCommandsCanExecute()
        {
            ((RelayCommand)ZeroCalibrationCommand)?.RaiseCanExecuteChanged();
            ((RelayCommand)MeasureCommand)?.RaiseCanExecuteChanged();
            ((RelayCommand)SaveResultsCommand)?.RaiseCanExecuteChanged();
            ((RelayCommand)ApplySerialNumberCommand)?.RaiseCanExecuteChanged();
            ((RelayCommand)ApplyMeasurementTimeCommand)?.RaiseCanExecuteChanged();
            ((RelayCommand)NewDeviceUnderTestCommand)?.RaiseCanExecuteChanged();
        }

        // Обновляет статус конкретной точки измерения
        private void UpdateMeasurementStatus(string location, bool? isPassed, string measuredValuesString = null)
        {
            MeasurementStatus statusToUpdate = MeasurementStatusService.Instance.AllMeasurementButtonStatuses.FirstOrDefault(s => s.Location == location);
            if (statusToUpdate != null)
            {
                statusToUpdate.IsPassed = isPassed;
                statusToUpdate.MeasuredValuesString = measuredValuesString;
            }
            else
            {
                AddLogMessage($"{MeasButStatusErr}: '{location}'");
            }
        }

        // Сбрасывает статусы всех точек измерения
        private static void ResetMeasurementStatuses()
        {
            foreach (MeasurementStatus measurementStatusViewModel in MeasurementStatusService.Instance.AllMeasurementButtonStatuses)
            {
                measurementStatusViewModel.IsPassed = null;
                measurementStatusViewModel.MeasuredValuesString = "";
            }
        }

        // Обработчики событий сервисов
        private void ColorMeasurementService_StatusMessage(object? sender, string message)
        {
            ExecuteThreadInUI(() => { AddLogMessage(message); });
        }
        private void FileService_StatusMessage(object? sender, string message)
        {
            ExecuteThreadInUI(() => { AddLogMessage(message); });
        }
        private void ColorMeasurementService_CalibrationStatusChanged(object? sender, bool isCalibrated)
        {
            ExecuteThreadInUI(() => { IsDeviceCalibrated = isCalibrated; UpdateCommandsCanExecute(); });
        }
        private void ColorMeasurementService_ConnectionStatusChanged(object? sender, bool isConnected)
        {
            ExecuteThreadInUI(() => { IsDeviceConnected = isConnected; UpdateCommandsCanExecute(); });
        }

        public void Dispose()
        {
            AddLogMessage($"{ViewModelClearing}");
            if (_colorMeasurementService != null)
            {
                _colorMeasurementService.StatusMessage -= ColorMeasurementService_StatusMessage;
                _colorMeasurementService.ConnectionStatusChanged -= ColorMeasurementService_ConnectionStatusChanged;
                _colorMeasurementService.CalibrationStatusChanged -= ColorMeasurementService_CalibrationStatusChanged;
            }
            if (_fileService != null)
            {
                _fileService.StatusMessage -= FileService_StatusMessage;
            }
            (_colorMeasurementService as IDisposable)?.Dispose();
            (_fileService as IDisposable)?.Dispose();
            (_dialogService as IDisposable)?.Dispose();
            ExecuteClearLog();
            _currentDevice = null;
            _serialNumber = string.Empty;
            _measurementTime = DefaultMeasurementTime;
            _isDeviceConnected = false;
            _isDeviceCalibrated = false;
            _isSerialNumberConfirmed = false;
            _isMeasurementButtonsEnabled = false;
            GC.SuppressFinalize(this);
            AddLogMessage($"{ViewModelCleared}");
        }

        private void ExecuteThreadInUI(Action action)
        {
            if (_dispatcher != null)
            {
                if (_dispatcher.CheckAccess())
                    action.Invoke();
                else
                    _dispatcher.BeginInvoke(action);
            }
        }

        [GeneratedRegex(SerialNumberPattern)]
        public static partial Regex SerialNumberRegex();
    }
}