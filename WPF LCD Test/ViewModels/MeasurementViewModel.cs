using MvvmHelpers;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Input;
using WPF_LCD_Test.Commands;
using WPF_LCD_Test.Interfaces;
using WPF_LCD_Test.Models;
using WPF_LCD_Test.Services;
using static WPF_LCD_Test.Resources.Resources;

// Класс ViewModel для MainWindow
namespace WPF_LCD_Test.ViewModels
{
    public partial class MeasurementViewModel : BaseViewModel, IDisposable
    {
        private readonly IColorMeasurementService _colorMeasurementService;
        private readonly IFileService _fileService;
        private readonly IDialogService _dialogService;
        private readonly ILocalizationService _localizationService;
        private readonly IDispatcher _dispatcher;

        public DeviceUnderTest? _currentDevice;

        private string _serialNumber = string.Empty;
        private bool _isSerialNumberConfirmed;
        private string _logText = string.Empty;
        private bool _isMeasurementButtonsEnabled;
        private bool _isDeviceConnected;
        private bool _isDeviceCalibrated;
        public bool _isDeviceConnecting;
        public bool _isDeviceCalibrating;
        private const string SerialNumberPattern = "^[a-zA-Z0-9]*$";

        public event EventHandler RequestClearInputFocus;

        public int MeasurementTime { get; set; }


        public string SerialNumber
        {
            get => _serialNumber;
            set
            {
                if (SetProperty(ref _serialNumber, value))
                {
                    UpdateState();
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
                    UpdateState();
                }
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
                    UpdateState();
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
                    UpdateState();
                    OnPropertyChanged(nameof(DeviceCalibrationStatusText));
                }
            }
        }

        public string DeviceConnectionStatusText => IsDeviceConnected ? ConnectedCA : DisconnectedCA;
        public string DeviceCalibrationStatusText => IsDeviceCalibrated ? CalibratedCA : NotCalibratedCa;

        public ICommand ZeroCalibrationCommand { get; }
        public ICommand SaveResultsCommand { get; }
        public ICommand ClearFieldsCommand { get; }
        public ICommand ClearLogCommand { get; }
        public ICommand SwitchLanguageCommand { get; }
        public ICommand MeasureCommand { get; }
        public ICommand ApplySerialNumberCommand { get; }
        public ICommand NewDeviceUnderTestCommand { get; }
        public ICommand LaunchExternalProgramCommand { get; }

        public MeasurementViewModel(IColorMeasurementService colorMeasurementService, IFileService fileService, IDialogService dialogService, ILocalizationService localizationService, IDispatcher dispatcher)
        {
            _colorMeasurementService = colorMeasurementService ?? throw new ArgumentNullException(nameof(colorMeasurementService));
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));

            ZeroCalibrationCommand = new RelayCommand(ExecuteZeroCalibrationAsync, CanExecuteZeroCalibration);
            SaveResultsCommand = new RelayCommand(ExecuteSaveResultsAsync, CanExecuteSaveResults);
            ClearFieldsCommand = new RelayCommand(ExecuteClearFields, CanExecuteClearFields);
            ClearLogCommand = new RelayCommand(ExecuteClearLog);
            SwitchLanguageCommand = new RelayCommand(ExecuteSwitchLanguage, CanExecuteSwitchLanguage);
            NewDeviceUnderTestCommand = new RelayCommand(ExecuteNewDeviceUnderTest, CanExecuteNewDeviceUnderTest);
            LaunchExternalProgramCommand = new RelayCommand(ExecuteLaunchExternalProgramCommand);
            MeasureCommand = new RelayCommand(ExecuteMeasureAsync, CanExecuteMeasure);
            ApplySerialNumberCommand = new RelayCommand(ExecuteApplySerialNumber, CanExecuteApplySerialNumber);

            var settings = SettingsService.Instance.LoadSettings();
            MeasurementTime = settings.MeasurementTime;
            SettingsService.MeasurementTimeChanged += OnMeasurementTimeChanged;

            SubscribeToServices();
            AddLogMessage(WelcomeMessage);
            UpdateState();
        }

        private void OnMeasurementTimeChanged(object? sender, int newMeasurementTime)
        {
            if (MeasurementTime != newMeasurementTime)
            {
                MeasurementTime = newMeasurementTime;
                OnPropertyChanged(nameof(MeasurementTime));
            }
        }

        public bool AreAllStatusesRepresentedInMeasurements()
        {
            if (_currentDevice?.Measurements is null || !_currentDevice.Measurements.Any() || !MeasurementStatusService.Instance.AllMeasurementButtonStatuses.Any())
            {
                return false;
            }
            return !MeasurementStatusService.Instance.AllMeasurementButtonStatuses.Any(status => !_currentDevice.Measurements.Any(measurement => measurement.Location == status.Location));
        }

        private async Task ExecuteConnectAsync()
        {
            if (!CanExecuteConnect()) return;
            try
            {
                _isDeviceConnecting = true;
                if (!await _colorMeasurementService.ConnectAsync())
                {
                    ExecuteDisconnect();
                }
            }
            catch
            {
                ExecuteDisconnect();
            }
            finally
            {
                _isDeviceConnecting = false;
                UpdateState();
            }
        }

        private void ExecuteDisconnect()
        {
            if (!CanExecuteDisconnect()) return;
            try
            {
                _colorMeasurementService.Disconnect();
            }
            catch (Exception ex)
            {
                _dialogService.ShowMessage($"{ErrUnexpected}: {ex.Message}", Err);
            }
            UpdateState();
        }

        private async Task ExecuteZeroCalibrationAsync(object parameter)
        {
            CheckCurrentAppLanguage();
            if (!CanExecuteZeroCalibration(parameter)) return;
            try
            {
                if (!IsDeviceConnected)
                {
                    await ExecuteConnectAsync();
                }
                if (IsDeviceConnected)
                {
                    _isDeviceCalibrating = true;
                    if (!await _colorMeasurementService.CalibrateZeroAsync())
                    {
                        _dialogService.ShowMessage(ErrAtCalibration, Err);
                        ExecuteDisconnect();
                    }
                }
            }
            catch
            {
                _dialogService.ShowMessage(ErrAtCalibration, Err);
                ExecuteDisconnect();
            }
            finally
            {
                _isDeviceCalibrating = false;
                UpdateState();
            }
        }

        private async Task<bool> ExecuteSaveResultsAsync(object parameter)
        {
            CheckCurrentAppLanguage();
            if (!CanExecuteSaveResults(parameter)) return false;

            AddLogMessage(Saving);

            if (_currentDevice is null || string.IsNullOrWhiteSpace(_currentDevice.SerialNumber) || _currentDevice.Measurements.Count == 0)
            {
                _dialogService.ShowMessage(SaveJSONErrDeviceIsEmpty, Err);
                return false;
            }

            if (!AreAllStatusesRepresentedInMeasurements() && !_dialogService.ShowQuestion(SavingNotFullWarning, Warning))
            {
                AddLogMessage(SaveCanceled);
                return false;
            }

            try
            {
                return await _fileService.SaveDeviceDataToJsonAsync(_currentDevice);
            }
            catch (Exception ex)
            {
                AddLogMessage($"{ErrUnexpected}: {ex.Message}");
                _dialogService.ShowMessage($"{SaveJSONErrForSN}: {ex.Message}", Err);
                return false;
            }
        }

        private void ExecuteClearFields(object parameter)
        {
            CheckCurrentAppLanguage();
            if (!CanExecuteClearFields(parameter)) return;

            string approveQuestion = parameter?.Equals("CalledFromNewDeviceMethod") == true ? CleanFieldWarningAfterSave : CleanFieldWarning;
            if (!_dialogService.ShowQuestion(approveQuestion, Warning)) return;

            SerialNumber = string.Empty;
            _currentDevice = null;
            IsSerialNumberConfirmed = false;
            ResetMeasurementStatuses();
            AddLogMessage(ClearFieldsDone);
            UpdateState();
        }

        private void ExecuteSwitchLanguage(object parameter)
        {
            if (!CanExecuteSwitchLanguage(parameter)) return;

            string? languageCode = parameter as string;
            if (string.IsNullOrWhiteSpace(languageCode)) return;

            try
            {
                _localizationService.SetLanguage(languageCode);
                CheckCurrentAppLanguage();
            }
            catch (Exception ex)
            {
                _dialogService.ShowMessage($"{ErrMsgLangSwitchFailed}: {ex.Message}", Err);
            }
        }

        private static void CheckCurrentAppLanguage()
        {
            var culture = LocalizationService.Instance.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
        }

        private async Task ExecuteMeasureAsync(object parameter)
        {
            CheckCurrentAppLanguage();
            if (parameter is not string measurementName || string.IsNullOrWhiteSpace(measurementName) || !CanExecuteMeasure(parameter)) return;

            UpdateMeasurementStatus(measurementName, null, Measuring);

            try
            {
                if (_currentDevice is null || _currentDevice.SerialNumber != SerialNumber)
                {
                    if (string.IsNullOrWhiteSpace(SerialNumber))
                    {
                        _dialogService.ShowMessage(FillSN, Err);
                        UpdateMeasurementStatus(measurementName, false, NoSNErr);
                        return;
                    }
                    _currentDevice = new DeviceUnderTest(SerialNumber);
                    AddLogMessage($"{TestStartInfo}: {_currentDevice.SerialNumber}");
                    ResetMeasurementStatuses();
                    UpdateMeasurementStatus(measurementName, null, Measuring);
                }

                var resultMeasurement = await _colorMeasurementService.MeasureAsync(MeasurementTime);

                if (resultMeasurement != null && resultMeasurement.IsValid)
                {
                    if (measurementName != MeasurementStatusService.Instance.BlackColorStatus.Location && resultMeasurement.Lv < 10)
                    {
                        AddLogMessage($"{LvIsTooLow}: {resultMeasurement.Lv:F1}. {CheckProbe}");
                        resultMeasurement.IsValid = false;
                    }
                    if (resultMeasurement.IsValid)
                    {
                        var measuredValuesDisplay = $"x={resultMeasurement.x:F3}, y={resultMeasurement.y:F3}, Lv={resultMeasurement.Lv:F1}, T={resultMeasurement.T:F0}";
                        AddLogMessage($"{Result} '{measurementName}': {measuredValuesDisplay}");
                        resultMeasurement.Location = measurementName;
                        _currentDevice.AddMeasurement(resultMeasurement);
                        UpdateMeasurementStatus(measurementName, true, measuredValuesDisplay);
                    }
                    else
                    {
                        UpdateMeasurementStatus(measurementName, false, $"{LvIsTooLow}: {resultMeasurement.Lv:F1}");
                    }
                }
                else
                {
                    var displayMessage = (resultMeasurement != null && !resultMeasurement.IsValid) ? InvalidResultErr : ColorAnalyzerErr;
                    AddLogMessage($"{ColorServiceErr} '{measurementName}'.");
                    UpdateMeasurementStatus(measurementName, false, displayMessage);
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowMessage($"{UnexpectedMeasurementErr} '{measurementName}': {ex.Message}", Err);
                UpdateMeasurementStatus(measurementName, false, $"{Err}: {ex.Message}");
            }
            finally
            {
                UpdateState();
            }
        }

        public void ExecuteApplySerialNumber(object parameter)
        {
            CheckCurrentAppLanguage();
            var enteredSerialNumber = parameter as string;
            if (!CanExecuteApplySerialNumber(parameter)) return;
            if (!string.IsNullOrWhiteSpace(enteredSerialNumber) && SerialNumberRegex().IsMatch(enteredSerialNumber))
            {
                SerialNumber = enteredSerialNumber;
                IsSerialNumberConfirmed = true;
                AddLogMessage($"{CurrentSN}: {SerialNumber}");
                RequestClearInputFocus?.Invoke(this, EventArgs.Empty);

                if (_currentDevice is null || _currentDevice.SerialNumber != SerialNumber)
                {
                    try
                    {
                        _currentDevice = new DeviceUnderTest(SerialNumber);
                        ResetMeasurementStatuses();
                    }
                    catch (ArgumentException ex)
                    {
                        IsSerialNumberConfirmed = false;
                        AddLogMessage($"{Err}: {ex.Message}");
                        _dialogService.ShowMessage(ex.Message, $"{Err} SN");
                        SerialNumber = string.Empty;
                        _currentDevice = null;
                        UpdateMeasurementButtonsState();
                    }
                }
                else
                {
                    AddLogMessage($"{Resources.Resources.SerialNumber} {SerialNumber}' {AlreadyActivated}");
                }
            }
            else
            {
                IsSerialNumberConfirmed = false;
                _dialogService.ShowMessage(IncorrectFormatForSNErr, Err);
            }
            UpdateState();
        }

        private async Task ExecuteNewDeviceUnderTest(object parameter)
        {
            if (await ExecuteSaveResultsAsync(parameter))
            {
                ExecuteClearFields("CalledFromNewDeviceMethod");
            }
            UpdateMeasurementButtonsState();
        }

        public void ExecuteClearLog()
        {
            LogText = string.Empty;
        }

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

        // Методы CanExecute
        private bool CanExecuteConnect() => !IsDeviceConnected && !_isDeviceCalibrating && !_isDeviceConnecting;

        private bool CanExecuteDisconnect() => !_isDeviceConnecting && !_isDeviceCalibrating;

        private bool CanExecuteZeroCalibration(object parameter) => !_isDeviceConnecting && !_isDeviceCalibrating;

        private bool CanExecuteSaveResults(object parameter) => _currentDevice is not null && !string.IsNullOrWhiteSpace(_currentDevice.SerialNumber) && _currentDevice.Measurements.Count > 0;

        private bool CanExecuteClearFields(object parameter) => true;

        private bool CanExecuteSwitchLanguage(object parameter) => parameter is string languageCode && !string.IsNullOrWhiteSpace(languageCode);

        private bool CanExecuteMeasure(object parameter) => IsDeviceConnected && !_isDeviceCalibrating && !_isDeviceConnecting && IsDeviceCalibrated && !string.IsNullOrEmpty(SerialNumber) && MeasurementTime > 0;

        private bool CanExecuteApplySerialNumber(object parameter) => true;

        private bool CanExecuteNewDeviceUnderTest(object paramater) => _currentDevice is not null && !string.IsNullOrWhiteSpace(_currentDevice.SerialNumber) && _currentDevice.Measurements.Count > 0;

        public void AddLogMessage(string message)
        {
            if (!string.IsNullOrEmpty(message))
            {
                var timestamp = DateTime.Now.ToString("HH:mm:ss");
                LogText += $"{timestamp} {message}{Environment.NewLine}";
            }
        }

        private void UpdateState()
        {
            UpdateMeasurementButtonsState();
            UpdateCommandsCanExecute();
        }

        private void UpdateMeasurementButtonsState()
        {
            IsMeasurementButtonsEnabled = IsDeviceConnected && IsDeviceCalibrated && !string.IsNullOrWhiteSpace(SerialNumber);
            (MeasureCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        private void UpdateCommandsCanExecute()
        {
            ((RelayCommand)ZeroCalibrationCommand)?.RaiseCanExecuteChanged();
            ((RelayCommand)MeasureCommand)?.RaiseCanExecuteChanged();
            ((RelayCommand)SaveResultsCommand)?.RaiseCanExecuteChanged();
            ((RelayCommand)ApplySerialNumberCommand)?.RaiseCanExecuteChanged();
            ((RelayCommand)NewDeviceUnderTestCommand)?.RaiseCanExecuteChanged();
        }

        private void UpdateMeasurementStatus(string location, bool? isPassed, string? measuredValuesString = null)
        {
            var statusToUpdate = MeasurementStatusService.Instance.AllMeasurementButtonStatuses.FirstOrDefault(s => s.Location == location);
            if (statusToUpdate is not null)
            {
                statusToUpdate.IsPassed = isPassed;
                statusToUpdate.MeasuredValuesString = measuredValuesString;
            }
            else
            {
                AddLogMessage($"{MeasButStatusErr}: '{location}'");
            }
        }

        private static void ResetMeasurementStatuses()
        {
            foreach (var measurementStatusViewModel in MeasurementStatusService.Instance.AllMeasurementButtonStatuses)
            {
                measurementStatusViewModel.IsPassed = null;
                measurementStatusViewModel.MeasuredValuesString = string.Empty;
            }
        }

        private void SubscribeToServices()
        {
            _colorMeasurementService.StatusMessage += ColorMeasurementService_StatusMessage;
            _fileService.StatusMessage += FileService_StatusMessage;
            _colorMeasurementService.ConnectionStatusChanged += (sender, isConnected) => ColorMeasurementService_ConnectionStatusChanged(sender, isConnected);
            _colorMeasurementService.CalibrationStatusChanged += (sender, isCalibrated) => ColorMeasurementService_CalibrationStatusChanged(sender, isCalibrated);
        }

        private void ColorMeasurementService_StatusMessage(object? sender, string message) => AddLogMessage(message);

        private void FileService_StatusMessage(object? sender, string message) => AddLogMessage(message);

        private void ColorMeasurementService_CalibrationStatusChanged(object? sender, bool isCalibrated)
        {
            IsDeviceCalibrated = isCalibrated;
            UpdateCommandsCanExecute();
        }

        private void ColorMeasurementService_ConnectionStatusChanged(object? sender, bool isConnected)
        {
            IsDeviceConnected = isConnected;
            UpdateCommandsCanExecute();
        }

        public void Dispose()
        {
            AddLogMessage(ViewModelClearing);
            if (_colorMeasurementService is not null)
            {
                _colorMeasurementService.StatusMessage -= ColorMeasurementService_StatusMessage;
                _colorMeasurementService.ConnectionStatusChanged -= ColorMeasurementService_ConnectionStatusChanged;
                _colorMeasurementService.CalibrationStatusChanged -= ColorMeasurementService_CalibrationStatusChanged;
            }
            if (_fileService is not null)
            {
                _fileService.StatusMessage -= FileService_StatusMessage;
            }

            SettingsService.MeasurementTimeChanged -= OnMeasurementTimeChanged;
            (_colorMeasurementService as IDisposable)?.Dispose();
            (_fileService as IDisposable)?.Dispose();
            (_dialogService as IDisposable)?.Dispose();
            ExecuteClearLog();
            _currentDevice = null;
            _serialNumber = string.Empty;
            _isDeviceConnected = false;
            _isDeviceCalibrated = false;
            _isSerialNumberConfirmed = false;
            _isMeasurementButtonsEnabled = false;
            GC.SuppressFinalize(this);
            AddLogMessage(ViewModelCleared);
        }

        private void ExecuteThreadInUI(Action action)
        {
            if (_dispatcher is not null)
            {
                if (_dispatcher.CheckAccess())
                {
                    action.Invoke();
                }
                else
                {
                    _dispatcher.BeginInvoke(action);
                }
            }
        }

        [GeneratedRegex(SerialNumberPattern)]
        public static partial Regex SerialNumberRegex();
    }
}