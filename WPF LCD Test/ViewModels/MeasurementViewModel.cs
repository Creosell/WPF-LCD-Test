using MvvmHelpers;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
        private readonly IColorMeasurementService _colorMeasurementService;
        private readonly IFileService _fileService;
        private readonly IDialogService _dialogService;
        private readonly ILocalizationService _localizationService;
        private readonly IDispatcher _dispatcher;
        public DeviceUnderTest? _currentDevice;
        private string _serialNumber = "";
        private bool _isSerialNumberConfirmed;
        private int _measurementTime = 2;
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
        private string _logText = string.Empty;


        // Keys and values for Measurement Validation
        private const int MaxMeasurementAttemptsBeforeConfirm = 1;
        private const double ColorCoordinatesTolerance = 0.1;
        private static readonly Dictionary<string, (double x, double y)> primariesNTSC = new()
        {
         {MeasurementLocation.RedColor.ToString(), (x: 0.67, y: 0.33)},
         {MeasurementLocation.GreenColor.ToString(), (x: 0.21, y: 0.71)},
         {MeasurementLocation.BlueColor.ToString(), (x: 0.14, y: 0.08)},
         {MeasurementLocation.WhiteColor.ToString(), (x: 0.3127, y: 0.3290)}
         };
        private static readonly Dictionary<string, int> _measurementAttemptCountersMap = new();

        private int GetAttemptCount(string location)
        {
            _measurementAttemptCountersMap.TryGetValue(location, out int count);
            return count;
        }

        private void IncrementAttemptCount(string location)
        {
            if (!_measurementAttemptCountersMap.TryAdd(location, 1))
            {
                _measurementAttemptCountersMap[location]++;
            }
        }

        private void ResetAttemptCount(string location)
        {
            _measurementAttemptCountersMap[location] = 0;
        }



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
                if (value > 0)
                    SetProperty(ref _measurementTime, value);
                else
                    OnPropertyChanged();
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

        public ICommand ZeroCalibrationCommand { get; }
        public ICommand SaveResultsCommand { get; }
        public ICommand ClearFieldsCommand { get; }
        public ICommand ClearLogCommand { get; }
        public ICommand SwitchLanguageCommand { get; }
        public ICommand MeasureCommand { get; }
        public ICommand ApplySerialNumberCommand { get; }
        public ICommand ApplyMeasurementTimeCommand { get; }
        public ICommand NewDeviceUnderTestCommand { get; }
        public ICommand LaunchExternalProgramCommand { get; }

        public MeasurementViewModel(
            IColorMeasurementService colorMeasurementService,
            IFileService fileService,
            IDialogService dialogService,
            ILocalizationService localizationService,
            IDispatcher dispatcher)
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



        private void InitializeDeviceConfigurations()
        {
            try
            {
                if (!Directory.Exists(CONFIGS_DIR))
                    Directory.CreateDirectory(CONFIGS_DIR);

                var configFiles = Directory.GetFiles(CONFIGS_DIR, "*.yaml");
                DeviceConfigurations.Clear();
                foreach (var file in configFiles)
                    DeviceConfigurations.Add(Path.GetFileNameWithoutExtension(file));

                if (DeviceConfigurations.Any())
                    SelectedDeviceConfiguration = DeviceConfigurations.First();
                else
                    AddLogMessage($"{ConfigDirNotFound}: {CONFIGS_DIR}");
            }
            catch (Exception ex)
            {
                AddLogMessage($"{ErrUnexpected}: {ex.Message}");
            }
        }

        public bool AreAllStatusesRepresentedInMeasurements()
        {
            if (_currentDevice?.Measurements == null || _currentDevice.Measurements.Count == 0)
                return false;

            var allStatusPoints = MeasurementStatusService.Instance.AllMeasurementButtonStatuses;
            if (allStatusPoints == null || !allStatusPoints.Any())
                return false;

            return allStatusPoints.All(status => status.Location == MeasurementLocation.WhiteColor.ToString() && !_currentDevice.IsTV ||
                _currentDevice.Measurements.Any(measurement => measurement.Location == status.Location));
        }

        private async Task ExecuteConnectAsync()
        {
            if (!CanExecuteConnect())
                return;

            try
            {
                _isDeviceConnecting = true;
                if (!await _colorMeasurementService.ConnectAsync())
                    ExecuteDisconnect();
            }
            catch
            {
                ExecuteDisconnect();
            }
            finally
            {
                _isDeviceConnecting = false;
            }
        }

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
                        ExecuteDisconnect();
                        _dialogService.ShowMessage($"{ErrAtCalibration}", $"{Err}");
                    }
                }
            }
            catch
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

        private async Task<bool> ExecuteSaveResultsAsync(object parameter)
        {
            CheckCurrentAppLanguage();
            if (!CanExecuteSaveResults(parameter))
                return false;

            AddLogMessage($"{Saving}");
            try
            {
                if (_currentDevice == null || string.IsNullOrWhiteSpace(_currentDevice.SerialNumber) || _currentDevice.Measurements.Count == 0)
                {
                    _dialogService.ShowMessage($"{SaveJSONErrDeviceIsEmpty}", $"{Err}");
                    return false;
                }

                _currentDevice.DeviceConfiguration = SelectedDeviceConfiguration;
                _currentDevice.IsTV = IsTvCheckboxChecked;

                if (!AreAllStatusesRepresentedInMeasurements())
                {
                    if (!_dialogService.ShowQuestion($"{SavingNotFullWarning}", $"{Warning}"))
                    {
                        AddLogMessage($"{SaveCanceled}");
                        return false;
                    }
                }

                return await _fileService.SaveDeviceDataToJsonAsync(_currentDevice);
            }
            catch (Exception ex)
            {
                AddLogMessage($"{ErrUnexpected}: {ex.Message}");
                _dialogService.ShowMessage($"{SaveJSONErrForSN}: {ex.Message}", $"{Err}");
                return false;
            }
        }

        private void ExecuteClearFields(object parameter)
        {
            CheckCurrentAppLanguage();
            if (!CanExecuteClearFields(parameter))
                return;

            string approveQuestion = parameter?.Equals("CalledFromNewDeviceMethod") == true
                ? CleanFieldWarningAfterSave
                : CleanFieldWarning;

            if (_dialogService.ShowQuestion(approveQuestion, $"{Warning}"))
            {
                SerialNumber = "";
                MeasurementTime = 2;
                _currentDevice = null;
                IsSerialNumberConfirmed = false;
                ResetMeasurementStatuses();
                AddLogMessage($"{ClearFieldsDone}");
                UpdateCommandsCanExecute();
                UpdateMeasurementButtonsState();
            }
        }

        private void ExecuteSwitchLanguage(object parameter)
        {
            if (!CanExecuteSwitchLanguage(parameter))
                return;

            if (parameter is string languageCode && !string.IsNullOrWhiteSpace(languageCode))
            {
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
        }

        private static void CheckCurrentAppLanguage()
        {
            var culture = LocalizationService.Instance.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
        }

        private async Task ExecuteMeasureAsync(object parameter)
        {
            if (parameter.ToString() is not string measurementLocation || !CanExecuteMeasure(parameter))
                return;

            bool isMeasurementSuccess = false;
            string messageWithMeasuredValues = $"{NoData}";
            CheckCurrentAppLanguage();
            UpdateMeasurementStatus(measurementLocation, null, $"{Measuring}");

            void ApplyMeasurementResult(Measurement measurement)
            {
                var (xFormatted, yFormatted, LvFormatted, TFormatted) = FormatMeasurement(measurement);
                messageWithMeasuredValues = $"x={xFormatted}, y={yFormatted}, Lv={LvFormatted}, T={TFormatted}";
                AddLogMessage($"{Result} '{measurement.Location}': {messageWithMeasuredValues}");
                _currentDevice.AddMeasurement(measurement);
                isMeasurementSuccess = true;
            }

            try
            {
                if (_currentDevice == null || _currentDevice.SerialNumber != SerialNumber)
                {
                    if (string.IsNullOrWhiteSpace(SerialNumber))
                    {
                        _dialogService.ShowMessage($"{FillSN}", $"{Err}");
                        messageWithMeasuredValues = $"{NoSNErr}";
                        UpdateMeasurementStatus(measurementLocation, isMeasurementSuccess, messageWithMeasuredValues);
                        return;
                    }

                    _currentDevice = new DeviceUnderTest(SerialNumber);
                    AddLogMessage($"{TestStartInfo}: {_currentDevice.SerialNumber}");
                    ResetMeasurementStatuses();
                    UpdateMeasurementStatus(measurementLocation, null, $"{Measuring}");
                }

                var measurement = await _colorMeasurementService.MeasureAsync(_measurementTime);

                if (measurement != null && measurement.IsValid)
                {
                    measurement.Location = measurementLocation;
                    var (MeasurementValidationPassed, MeasurementValidationMessage) = MeasurementValidation(measurement);
                    int currentAttempt = GetAttemptCount(measurement.Location);


                    if (MeasurementValidationPassed)
                    {
                        ApplyMeasurementResult(measurement);
                        ResetAttemptCount(measurement.Location);
                    }
                    else if (currentAttempt < MaxMeasurementAttemptsBeforeConfirm)
                    {
                        string retryMessage = $"{MeasurementValidationMessage} \n{PleaseTryToMeasureAgain}";

                        IncrementAttemptCount(measurementLocation);
                        _dialogService.ShowMessage(retryMessage, $"{Warning}");
                        messageWithMeasuredValues = $"Failed measurement for location: {measurementLocation}.\n (Attempt {currentAttempt + 1}). {PleaseTryToMeasureAgain}";
                        AddLogMessage(messageWithMeasuredValues);
                    }
                    else
                    {
                        bool confirmed = _dialogService.ShowQuestion($"{SaveNotCorrectResultQuestion}\n" +
                            $"{MeasurementValidationMessage}.\n", $"{Warning}");

                        if (confirmed)
                        {
                            ApplyMeasurementResult(measurement);
                            messageWithMeasuredValues = $"{ResultsSaved} '{measurement.Location}'. \nValidation message: {MeasurementValidationMessage}";
                        }
                        else
                        {
                            messageWithMeasuredValues = $"{SaveCanceled}";
                        }
                        AddLogMessage(messageWithMeasuredValues);
                        ResetAttemptCount(measurement.Location);
                    }
                }
                else
                {
                    messageWithMeasuredValues = measurement != null && !measurement.IsValid
                        ? $"{InvalidResultErr}"
                        : $"{ColorAnalyzerErr}";
                    AddLogMessage($"{ColorServiceErr} '{measurementLocation}'.");
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowMessage($"{UnexpectedMeasurementErr} '{measurementLocation}': {ex.Message}", $"{Err}");
                isMeasurementSuccess = false;
                messageWithMeasuredValues = $"{Err}: {ex.Message}";
            }
            finally
            {
                UpdateMeasurementStatus(measurementLocation, isMeasurementSuccess, messageWithMeasuredValues);
                UpdateCommandsCanExecute();
            }
        }

        private static (string xFormatted, string yFormatted, string LvFormatted, string TFormatted) FormatMeasurement(Measurement measurement)
        {
            string xFormatted = measurement.x.ToString("F3", CultureInfo.InvariantCulture);
            string yFormatted = measurement.y.ToString("F3", CultureInfo.InvariantCulture);
            string LvFormatted = measurement.Location.Equals(MeasurementLocation.BlackColor.ToString())
                ? measurement.Lv.ToString("F6", CultureInfo.InvariantCulture)
                : measurement.Lv.ToString("F1", CultureInfo.InvariantCulture);
            string TFormatted = measurement.T.ToString("F0", CultureInfo.InvariantCulture);

            return (xFormatted, yFormatted, LvFormatted, TFormatted);
        }

        private static (bool result, string message) MeasurementValidation(Measurement measurement)
        {
            bool result = false;
            string message = $"{ErrMeasurementValidation}";

            // Brightness check for measurement
            if (!measurement.Location.Equals(MeasurementLocation.BlackColor.ToString()) && measurement.Lv <= 5)
            {
                message = $"{LvIsTooLow}: {measurement.Lv:F1}. {CheckProbe}";
                return (result, message);
            }
            else if (measurement.Location.Equals(MeasurementLocation.BlackColor.ToString()) && measurement.Lv >= 5)
            {
                message = $"{LvIsTooHigh}. \nBrightness: {measurement.Lv:F1}";
                return (result, message);
            }


            // Skip non-target measurements
            if (!primariesNTSC.TryGetValue(measurement.Location, out var target))
            {
                target = primariesNTSC[MeasurementLocation.WhiteColor.ToString()];
            }

            double minX = target.x - ColorCoordinatesTolerance;
            double maxX = target.x + ColorCoordinatesTolerance;
            double minY = target.y - ColorCoordinatesTolerance;
            double maxY = target.y + ColorCoordinatesTolerance;

            result = measurement.x >= minX && measurement.x <= maxX &&
                   measurement.y >= minY && measurement.y <= maxY;

            if (result is false)
            {
                message = $"{ErrMeasurementOutOfRange}: '{measurement.Location}'.\n" +
                          $"Got x: {measurement.x:F3}, y: {measurement.y:F3}";
            }
            else
            {
                message = MeasurementValidationPassed;
            }

            return (result, message);
        }

        public void ExecuteApplySerialNumber(object parameter)
        {
            CheckCurrentAppLanguage();
            if (!CanExecuteApplySerialNumber(parameter) || parameter is not string enteredSerialNumber)
                return;

            if (SerialNumberRegex().IsMatch(enteredSerialNumber))
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
                    _currentDevice = new DeviceUnderTest(SerialNumber);
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

        private void ExecuteApplyMeasurementTime(object parameter)
        {
            CheckCurrentAppLanguage();
            if (parameter is not string enteredMeasurementTime || string.IsNullOrWhiteSpace(enteredMeasurementTime))
            {
                _dialogService.ShowMessage($"{IncorrectMeasTimeFormat}", $"{Err}");
                return;
            }

            try
            {
                int measurementTime = int.Parse(enteredMeasurementTime);
                if (measurementTime <= 0)
                    _dialogService.ShowMessage($"{IncorrectMeasTimeFormat}", $"{Err}");
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

        private async Task ExecuteNewDeviceUnderTest(object parameter)
        {
            if (await ExecuteSaveResultsAsync(parameter))
                ExecuteClearFields("CalledFromNewDeviceMethod");
            UpdateMeasurementButtonsState();
        }

        public void ExecuteClearLog() => LogText = string.Empty;

        private void ExecuteLaunchExternalProgramCommand(object parameter)
        {
            if (parameter is not string executableName || string.IsNullOrWhiteSpace(executableName))
            {
                _dialogService.ShowMessage(_localizationService.GetString(RunExternalAppNotFoundErr), _localizationService.GetString(Err));
                AddLogMessage($"{_localizationService.GetString(RunExternalAppNotFoundErr)}");
                return;
            }

            try
            {
                if (!_fileService.RunExternalProgram(executableName))
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

        private bool CanExecuteConnect() => !IsDeviceConnected && !_isDeviceCalibrating && !_isDeviceConnecting;
        private bool CanExecuteDisconnect() => !_isDeviceConnecting && !_isDeviceCalibrating;
        private bool CanExecuteZeroCalibration(object parameter) => !_isDeviceConnecting && !_isDeviceCalibrating;
        private bool CanExecuteSaveResults(object parameter) => _currentDevice != null && !string.IsNullOrWhiteSpace(_currentDevice.SerialNumber) && _currentDevice.Measurements.Count > 0;
        private bool CanExecuteNewDeviceUnderTest(object parameter) => _currentDevice != null && !string.IsNullOrWhiteSpace(_currentDevice.SerialNumber) && _currentDevice.Measurements.Count > 0;
        private bool CanExecuteClearFields(object parameter) => true;
        private bool CanExecuteSwitchLanguage(object parameter) => parameter is string languageCode && !string.IsNullOrWhiteSpace(languageCode);
        private bool CanExecuteMeasure(object parameter) => IsDeviceConnected && !_isDeviceCalibrating && !_isDeviceConnecting && IsDeviceCalibrated && !string.IsNullOrWhiteSpace(SerialNumber) && MeasurementTime > 0;
        private bool CanExecuteApplySerialNumber(object parameter) => !string.IsNullOrWhiteSpace(parameter as string);
        private bool CanExecuteApplyMeasurementTime(object parameter) => MeasurementTime > 0;

        public void AddLogMessage(string message)
        {
            if (!string.IsNullOrEmpty(message))
            {
                string timestamp = DateTime.Now.ToString("HH:mm:ss");
                LogText += $"{timestamp} {message}{Environment.NewLine}";
            }
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
            ((RelayCommand)ApplyMeasurementTimeCommand)?.RaiseCanExecuteChanged();
            ((RelayCommand)NewDeviceUnderTestCommand)?.RaiseCanExecuteChanged();
        }

        private void UpdateMeasurementStatus(string location, bool? isPassed, string measuredValuesString)
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
            foreach (var status in MeasurementStatusService.Instance.AllMeasurementButtonStatuses)
            {
                status.IsPassed = null;
                status.MeasuredValuesString = "";
            }
        }

        private void ColorMeasurementService_StatusMessage(object? sender, string message) =>
            ExecuteThreadInUI(() => AddLogMessage(message));

        private void FileService_StatusMessage(object? sender, string message) =>
            ExecuteThreadInUI(() => AddLogMessage(message));

        private void ColorMeasurementService_CalibrationStatusChanged(object? sender, bool isCalibrated) =>
            ExecuteThreadInUI(() => { IsDeviceCalibrated = isCalibrated; UpdateCommandsCanExecute(); });

        private void ColorMeasurementService_ConnectionStatusChanged(object? sender, bool isConnected) =>
            ExecuteThreadInUI(() => { IsDeviceConnected = isConnected; UpdateCommandsCanExecute(); });

        public void Dispose()
        {
            AddLogMessage($"{ViewModelClearing}");
            _colorMeasurementService.StatusMessage -= ColorMeasurementService_StatusMessage;
            _fileService.StatusMessage -= FileService_StatusMessage;
            _colorMeasurementService.ConnectionStatusChanged -= ColorMeasurementService_ConnectionStatusChanged;
            _colorMeasurementService.CalibrationStatusChanged -= ColorMeasurementService_CalibrationStatusChanged;

            (_colorMeasurementService as IDisposable)?.Dispose();
            (_fileService as IDisposable)?.Dispose();
            (_dialogService as IDisposable)?.Dispose();

            ExecuteClearLog();
            _currentDevice = null;
            _serialNumber = string.Empty;
            _measurementTime = 2;
            _isDeviceConnected = false;
            _isDeviceCalibrated = false;
            _isSerialNumberConfirmed = false;
            _isMeasurementButtonsEnabled = false;
            GC.SuppressFinalize(this);
            AddLogMessage($"{ViewModelCleared}");
        }

        private void ExecuteThreadInUI(Action action)
        {
            if (_dispatcher.CheckAccess())
                action.Invoke();
            else
                _dispatcher.BeginInvoke(action);
        }

        [GeneratedRegex(SerialNumberPattern)]
        public static partial Regex SerialNumberRegex();
    }
}