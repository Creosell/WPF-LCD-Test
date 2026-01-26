using MvvmHelpers;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
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
        #region Fields & Constants

        private readonly IColorMeasurementService _colorMeasurementService;
        private readonly IFileService _fileService;
        private readonly IDialogService _dialogService;
        private readonly ILocalizationService _localizationService;
        private readonly IDispatcher _dispatcher;
        private readonly IUploadService _uploadService;
        private readonly ISettingsService _settingsService;
        private readonly IPathProvider _pathProvider;
        private readonly LogHandler _logHandler;

        private DeviceUnderTest? _currentDevice;
        private string _serialNumber = string.Empty;
        private bool _isSerialNumberConfirmed;
        private int _measurementTime = 2;
        private string _selectedDeviceConfiguration;
        private bool _isMeasurementButtonsEnabled;
        private bool _isDeviceConnected;
        private bool _isDeviceCalibrated;
        private bool _isDeviceConnecting;
        private bool _isDeviceCalibrating;
        private bool _isTvCheckboxChecked;
        private bool _isReportGenerating;
        private bool _isUploadingReports;
        private string _logText = string.Empty;

        private const string SERIAL_NUMBER_PATTERN = "^[a-zA-Z0-9]*$";
        private const string QA_PROBE_SN = "08954195";
        private const int QA_PROBE_CHANNEL = 1;
        private const int MAX_MEASUREMENT_ATTEMPTS_BEFORE_CONFIRM = 1;
        private const double COLOR_COORDINATES_TOLERANCE = 0.1;

        private static readonly Dictionary<string, (double x, double y)> primariesNTSC = new()
        {
            { MeasurementLocation.RedColor.ToString(), (0.67, 0.33) },
            { MeasurementLocation.GreenColor.ToString(), (0.21, 0.71) },
            { MeasurementLocation.BlueColor.ToString(), (0.14, 0.08) },
            { MeasurementLocation.WhiteColor.ToString(), (0.3127, 0.3290) }
        };

        private static readonly Dictionary<string, int> _measurementAttemptCountersMap = [];

        public event EventHandler RequestClearInputFocus;

        private enum ReportExitCode
            {
            Success = 0,
            GeneralError = 1,
            NoDataFound = 2,
            ConfigError = 3
            }

        #endregion

        #region Properties

        public DeviceUnderTest? CurrentDevice
            {
            get => _currentDevice;
            set => SetProperty(ref _currentDevice, value);
            }

        public bool IsTvCheckboxChecked
            {
            get => _isTvCheckboxChecked;
            set => SetProperty(ref _isTvCheckboxChecked, value);
            }

        public string SelectedDeviceConfiguration
            {
            get => _selectedDeviceConfiguration;
            set => SetProperty(ref _selectedDeviceConfiguration, value);
            }

        public string SerialNumber
            {
            get => _serialNumber;
            set
                {
                if (SetProperty(ref _serialNumber, value))
                    {
                    UpdateUIState();
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
                    UpdateUIState();
                    }
                }
            }

        public bool IsReportGenerating
            {
            get => _isReportGenerating;
            set
                {
                if (SetProperty(ref _isReportGenerating, value))
                    UpdateCommandsCanExecute();
                }
            }

        public bool IsUploadingReports
            {
            get => _isUploadingReports;
            set
                {
                if (SetProperty(ref _isUploadingReports, value))
                    UpdateCommandsCanExecute();
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
                    UpdateUIState();
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
                    UpdateUIState();
                    OnPropertyChanged(nameof(DeviceCalibrationStatusText));
                    }
                }
            }

        public string DeviceConnectionStatusText => _isDeviceConnected ? ConnectedCA : DisconnectedCA;
        public string DeviceCalibrationStatusText => _isDeviceCalibrated ? CalibratedCA : NotCalibratedCa;
        public ObservableCollection<string> DeviceConfigurations { get; } = [];

        #endregion

        #region Commands

        public ICommand ZeroCalibrationCommand { get; }
        public ICommand SaveResultsCommand { get; }
        public ICommand ClearFieldsCommand { get; }
        public ICommand ClearLogCommand { get; }
        public ICommand SwitchLanguageCommand { get; }
        public ICommand MeasureCommand { get; }
        public ICommand ApplySerialNumberCommand { get; }
        public ICommand ApplyMeasurementTimeCommand { get; }
        public ICommand NewDeviceUnderTestCommand { get; }
        public ICommand ReportGenerateCommand { get; }
        public ICommand UploadReportsCommand { get; }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="MeasurementViewModel"/> class.
        /// Configures services, commands, and event subscriptions.
        /// </summary>
        /// <param name="colorMeasurementService">Service for colorimeter interaction.</param>
        /// <param name="fileService">Service for file operations.</param>
        /// <param name="dialogService">Service for displaying dialogs.</param>
        /// <param name="localizationService">Service for localization management.</param>
        /// <param name="dispatcher">Dispatcher for UI thread marshaling.</param>
        /// <param name="uploadService">Service for uploading reports.</param>
        /// <param name="settingsService">Service for application settings.</param>
        public MeasurementViewModel(
            IColorMeasurementService colorMeasurementService,
            IFileService fileService,
            IDialogService dialogService,
            ILocalizationService localizationService,
            IDispatcher dispatcher,
            IUploadService uploadService,
            ISettingsService settingsService,
            IPathProvider pathProvider)
            {
            _colorMeasurementService = colorMeasurementService ?? throw new ArgumentNullException(nameof(colorMeasurementService));
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            _uploadService = uploadService ?? throw new ArgumentNullException(nameof(uploadService));
            _pathProvider = pathProvider ?? throw new ArgumentNullException(nameof(pathProvider));
            _logHandler = new LogHandler(_localizationService, AddLogMessage);

            ZeroCalibrationCommand = new RelayCommand(ExecuteZeroCalibrationAsync, CanExecuteZeroCalibration);
            SaveResultsCommand = new RelayCommand(ExecuteSaveResultsAsync, CanExecuteSaveResults);
            ClearFieldsCommand = new RelayCommand(ExecuteClearFields, CanExecuteClearFields);
            ClearLogCommand = new RelayCommand(ExecuteClearLog);
            SwitchLanguageCommand = new RelayCommand(ExecuteSwitchLanguage, CanExecuteSwitchLanguage);
            NewDeviceUnderTestCommand = new RelayCommand(ExecuteNewDeviceUnderTest, CanExecuteNewDeviceUnderTest);
            ReportGenerateCommand = new RelayCommand(ExecuteReportGenerateCommand, CanExecuteReportGenerateCommand);
            MeasureCommand = new RelayCommand(ExecuteMeasureAsync, CanExecuteMeasure);
            ApplySerialNumberCommand = new RelayCommand(ExecuteApplySerialNumber, CanExecuteApplySerialNumber);
            UploadReportsCommand = new RelayCommand(ExecuteUploadReportsAsync, CanExecuteUploadReportsAsync);

            _colorMeasurementService.StatusMessage += ColorMeasurementService_StatusMessage;
            _fileService.StatusMessage += FileService_StatusMessage;
            _colorMeasurementService.ConnectionStatusChanged += ColorMeasurementService_ConnectionStatusChanged;
            _colorMeasurementService.CalibrationStatusChanged += ColorMeasurementService_CalibrationStatusChanged;
            _uploadService.StatusMessage += (sender, message) => Log(message);

            Log(WelcomeMessage);
            InitializeDeviceConfigurations();
            UpdateUIState();
            }

        #endregion

        #region Methods

        /// <summary>
        /// Loads device configuration files from the disk and populates the selection list.
        /// </summary>
        private void InitializeDeviceConfigurations()
            {
            try
                {
                if (!Directory.Exists(_pathProvider.ConfigDirectory))
                    Directory.CreateDirectory(_pathProvider.ConfigDirectory);

                var configFiles = Directory.GetFiles(_pathProvider.ConfigDirectory, "*.yaml");
                DeviceConfigurations.Clear();

                foreach (var file in configFiles)
                    DeviceConfigurations.Add(Path.GetFileNameWithoutExtension(file));

                if (DeviceConfigurations.Any())
                    SelectedDeviceConfiguration = DeviceConfigurations.First();
                else
                    Log(ConfigDirNotFound, [_pathProvider.ConfigDirectory]);
                }
            catch (Exception ex)
                {
                Log(ErrUnexpected, [ex.Message]);
                }
            }

        /// <summary>
        /// Verifies if all required measurement statuses are present in the current device data.
        /// </summary>
        /// <returns>True if all statuses are represented; otherwise, false.</returns>
        public bool AreAllStatusesRepresentedInMeasurements()
            {
            if (_currentDevice?.Measurements == null || _currentDevice.Measurements.Count == 0)
                return false;

            var allStatusPoints = MeasurementStatusService.Instance.AllMeasurementButtonStatuses;
            if (allStatusPoints == null || !allStatusPoints.Any())
                return false;

            return allStatusPoints.All(status =>
                ( status.Location == MeasurementLocation.WhiteColor.ToString() && !_currentDevice.IsTV ) ||
                _currentDevice.Measurements.Any(measurement => measurement.Location == status.Location));
            }

        /// <summary>
        /// Attempts to establish a connection with the measurement device.
        /// </summary>
        private async Task ExecuteConnectAsync()
            {
            if (!CanExecuteConnect()) return;

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

        /// <summary>
        /// Disconnects the measurement device and updates the UI state.
        /// </summary>
        private void ExecuteDisconnect()
            {
            if (!CanExecuteDisconnect()) return;

            try
                {
                _colorMeasurementService.Disconnect();
                }
            catch (Exception ex)
                {
                _dialogService.ShowMessage($"{ErrUnexpected}: {ex.Message}", $"{Err}");
                }
            UpdateUIState();
            }

        /// <summary>
        /// Performs zero calibration on the connected device.
        /// </summary>
        /// <param name="parameter">Command parameter (unused).</param>
        private async Task ExecuteZeroCalibrationAsync(object parameter)
            {
            if (!CanExecuteZeroCalibration(parameter)) return;

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
                    // QA Probe specific logic
                    if (_colorMeasurementService.ProbeSN.Equals(QA_PROBE_SN) && _colorMeasurementService.CurrentChannel != QA_PROBE_CHANNEL)
                        {
                        _settingsService.UpdateChannel(_colorMeasurementService.CurrentChannel = QA_PROBE_CHANNEL);
                        _dialogService.ShowMessage("Detected QA CA-310. Changed channel to CH01", Warning);
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
                UpdateUIState();
                }
            }

        /// <summary>
        /// Saves the measurement results of the current device to a JSON file.
        /// </summary>
        /// <param name="parameter">Command parameter (unused).</param>
        /// <returns>True if save was successful; otherwise, false.</returns>
        private async Task<bool> ExecuteSaveResultsAsync(object parameter)
            {
            if (!CanExecuteSaveResults(parameter)) return false;

            Log(Saving);
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
                        Log(SaveCanceled);
                        return false;
                        }
                    }

                return await _fileService.SaveDeviceDataToJsonAsync(_currentDevice);
                }
            catch (Exception ex)
                {
                Log(ErrUnexpected, [ex.Message]);
                _dialogService.ShowMessage($"{SaveJSONErrForSN}: {ex.Message}", $"{Err}");
                return false;
                }
            }

        /// <summary>
        /// Clears the input fields and resets the current device state.
        /// </summary>
        /// <param name="parameter">Optional context string triggering the clear action.</param>
        private void ExecuteClearFields(object parameter)
            {
            string approveQuestion = parameter?.Equals("CalledFromNewDeviceMethod") == true
                ? CleanFieldWarningAfterSave
                : CleanFieldWarning;

            if (_dialogService.ShowQuestion(approveQuestion, Warning))
                {
                SerialNumber = "";
                MeasurementTime = 2;
                _currentDevice = null;
                IsSerialNumberConfirmed = false;
                ResetMeasurementStatuses();
                Log(ClearFieldsDone);
                UpdateUIState();
                }
            }

        /// <summary>
        /// Switches the application language.
        /// </summary>
        /// <param name="parameter">The language code (e.g., "en-US").</param>
        private void ExecuteSwitchLanguage(object parameter)
            {
            if (parameter is string languageCode && !string.IsNullOrWhiteSpace(languageCode))
                {
                try
                    {
                    _localizationService.SetLanguage(languageCode);
                    }
                catch (Exception ex)
                    {
                    _dialogService.ShowMessage($"{ErrMsgLangSwitchFailed}: {ex.Message}", $"{Err}");
                    }
                }
            }

        /// <summary>
        /// Orchestrates the measurement process, including validation, retries, and UI updates.
        /// </summary>
        /// <param name="parameter">The measurement location (e.g., "RedColor").</param>
        private async Task ExecuteMeasureAsync(object parameter)
            {
            if (parameter?.ToString() is not string measurementLocation || !CanExecuteMeasure(parameter)) return;

            bool isMeasurementSuccess = false;
            string messageWithMeasuredValues = $"{NoData}";
            UpdateMeasurementStatus(measurementLocation, null, $"{Measuring}");

            try
                {
                if (!EnsureDeviceInitialized(measurementLocation, ref messageWithMeasuredValues))
                    return;

                var measurement = await _colorMeasurementService.MeasureAsync(_measurementTime);

                if (measurement != null && measurement.IsValid)
                    {
                    measurement.Location = measurementLocation;
                    ProcessValidMeasurement(measurement, ref isMeasurementSuccess, ref messageWithMeasuredValues);
                    }
                else
                    {
                    messageWithMeasuredValues = measurement != null ? $"{InvalidResultErr}" : $"{ColorAnalyzerErr}";
                    Log(ColorServiceErr, [measurementLocation]);
                    }
                }
            catch (Exception ex)
                {
                _dialogService.ShowMessage($"{UnexpectedMeasurementErr} '{measurementLocation}': {ex.Message}", $"{Err}");
                messageWithMeasuredValues = $"{Err}: {ex.Message}";
                }
            finally
                {
                UpdateMeasurementStatus(measurementLocation, isMeasurementSuccess, messageWithMeasuredValues);
                UpdateCommandsCanExecute();
                }
            }

        /// <summary>
        /// Applies the serial number entered by the user.
        /// </summary>
        /// <param name="parameter">The serial number string.</param>
        public void ExecuteApplySerialNumber(object parameter)
            {
            if (parameter is not string enteredSerialNumber || string.IsNullOrWhiteSpace(enteredSerialNumber)) return;

            if (SerialNumberRegex().IsMatch(enteredSerialNumber))
                {
                SerialNumber = enteredSerialNumber;
                IsSerialNumberConfirmed = true;
                Log(CurrentSN, [SerialNumber]);
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
                    Log(ErrWithArg, [ex.Message]);
                    _dialogService.ShowMessage(ex.Message, $"{Err} SN");
                    SerialNumber = "";
                    _currentDevice = null;
                    }
                }
            else
                {
                Log(SerialNumber, [AlreadyActivated]);
                }
            UpdateUIState();
            }

        /// <summary>
        /// Saves current results and prepares for a new device under test.
        /// </summary>
        private async Task ExecuteNewDeviceUnderTest(object parameter)
            {
            if (await ExecuteSaveResultsAsync(parameter))
                ExecuteClearFields("CalledFromNewDeviceMethod");
            UpdateUIState();
            }

        /// <summary>
        /// Clears the log text.
        /// </summary>
        public void ExecuteClearLog() => LogText = string.Empty;

        /// <summary>
        /// Executes the external report generator application.
        /// </summary>
        private void ExecuteReportGenerateCommand(object parameter)
            {
            try
                {
                var appDir = AppDomain.CurrentDomain.BaseDirectory;
                var exePath = Path.Combine(appDir, "ReportGenerator.exe");

                if (!File.Exists(exePath))
                    {
                    Log(RunExternalAppNotFoundErr, [exePath]);
                    return;
                    }

                Log(StartingReportGeneration);
                IsReportGenerating = true;

                var process = new Process
                    {
                    StartInfo = new ProcessStartInfo
                        {
                        FileName = exePath,
                        WorkingDirectory = appDir,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                        }
                    };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                Task.Run(() =>
                {
                    try
                        {
                        process.WaitForExit();
                        var exitCode = (ReportExitCode)process.ExitCode;
                        process.Dispose();
                        HandleReportGenerationResult(exitCode);
                        }
                    catch (Exception ex)
                        {
                        Log(ErrorInBackgroundTask, [ex.Message]);
                        }
                    finally
                        {
                        ExecuteThreadInUI(() => IsReportGenerating = false);
                        }
                });
                }
            catch (Exception ex)
                {
                Log(RunExternalAppUnexpectedErr, [ex.Message]);
                IsReportGenerating = false;
                }
            }

        /// <summary>
        /// Uploads the generated reports using the upload service.
        /// </summary>
        private async Task ExecuteUploadReportsAsync(object parameter)
            {
            if (string.IsNullOrEmpty(_selectedDeviceConfiguration))
                {
                _dialogService.ShowMessage($"{CantStartUpload}", $"{Err}");
                return;
                }

            Log(StartingReportUploading);
            IsUploadingReports = true;

            try
                {
                if (!await _uploadService.UploadReportsAsync())
                    {
                    Log(UploadFailed);
                    }
                }
            finally
                {
                IsUploadingReports = false;
                }
            }

        #endregion

        #region Helpers

        private bool EnsureDeviceInitialized(string location, ref string message)
            {
            if (_currentDevice == null || _currentDevice.SerialNumber != SerialNumber)
                {
                if (string.IsNullOrWhiteSpace(SerialNumber))
                    {
                    _dialogService.ShowMessage($"{FillSN}", $"{Err}");
                    message = $"{NoSNErr}";
                    UpdateMeasurementStatus(location, false, message);
                    return false;
                    }

                _currentDevice = new DeviceUnderTest(SerialNumber);
                Log(TestStartInfo, [_currentDevice.SerialNumber]);
                ResetMeasurementStatuses();
                UpdateMeasurementStatus(location, null, $"{Measuring}");
                }
            return true;
            }

        private void ProcessValidMeasurement(Measurement measurement, ref bool isSuccess, ref string message)
            {
            var (validationPassed, validationMessage) = MeasurementValidation(measurement);
            int currentAttempt = GetAttemptCount(measurement.Location);

            if (validationPassed)
                {
                ApplyMeasurementResult(measurement, ref message);
                isSuccess = true;
                ResetAttemptCount(measurement.Location);
                }
            else if (currentAttempt < MAX_MEASUREMENT_ATTEMPTS_BEFORE_CONFIRM)
                {
                IncrementAttemptCount(measurement.Location);
                string retryMsg = $"{validationMessage} \n{PleaseTryToMeasureAgain}";
                _dialogService.ShowMessage(retryMsg, $"{Warning}");
                message = $"{FailedMeasurementForLocation}: {measurement.Location}.\n ({Attempt} {currentAttempt + 1}). {PleaseTryToMeasureAgain}";
                Log(message);
                }
            else
                {
                if (_dialogService.ShowQuestion($"{SaveNotCorrectResultQuestion}\n{validationMessage}.\n", $"{Warning}"))
                    {
                    ApplyMeasurementResult(measurement, ref message);
                    message = $"{ResultsSaved} '{measurement.Location}'. \n {ValidationMessage}: {validationMessage}";
                    isSuccess = true;
                    }
                else
                    {
                    message = $"{SaveCanceled}";
                    }
                Log(message);
                ResetAttemptCount(measurement.Location);
                }
            }

        private void ApplyMeasurementResult(Measurement measurement, ref string message)
            {
            var (x, y, Lv, T) = FormatMeasurement(measurement);
            message = $"x={x}, y={y}, Lv={Lv}, T={T}";
            Log(MeasurementResult, [measurement.Location, message]);
            _currentDevice!.AddMeasurement(measurement);
            }

        private static (string x, string y, string Lv, string T) FormatMeasurement(Measurement measurement)
            {
            return (
                measurement.x.ToString("F3", CultureInfo.InvariantCulture),
                measurement.y.ToString("F3", CultureInfo.InvariantCulture),
                measurement.Location == MeasurementLocation.BlackColor.ToString()
                    ? measurement.Lv.ToString("F6", CultureInfo.InvariantCulture)
                    : measurement.Lv.ToString("F1", CultureInfo.InvariantCulture),
                measurement.T.ToString("F0", CultureInfo.InvariantCulture)
            );
            }

        private static (bool result, string message) MeasurementValidation(Measurement measurement)
            {
            // Brightness check
            if (!measurement.Location.Equals(MeasurementLocation.BlackColor.ToString()))
                {
                if (measurement.Lv <= 5) return (false, $"{LvIsTooLow}: {measurement.Lv:F1}. {CheckProbe}");
                }
            else
                {
                // Black color logic
                if (measurement.Lv >= 5) return (false, $"{LvIsTooHigh}. \n {Brightness}: {measurement.Lv:F1}");
                return (true, MeasurementValidationPassed);
                }

            // Coordinates check (White only for uniformity, but logic implies general check)
            if (!primariesNTSC.TryGetValue(measurement.Location, out var target))
                target = primariesNTSC[MeasurementLocation.WhiteColor.ToString()];

            bool inRange = measurement.x >= target.x - COLOR_COORDINATES_TOLERANCE &&
                           measurement.x <= target.x + COLOR_COORDINATES_TOLERANCE &&
                           measurement.y >= target.y - COLOR_COORDINATES_TOLERANCE &&
                           measurement.y <= target.y + COLOR_COORDINATES_TOLERANCE;

            return inRange
                ? (true, MeasurementValidationPassed)
                : (false, $"{ErrMeasurementOutOfRange}: '{measurement.Location}'.\n{Actual} x: {measurement.x:F3}, y: {measurement.y:F3}");
            }

        private void HandleReportGenerationResult(ReportExitCode exitCode)
            {
            string message = exitCode switch
                {
                    ReportExitCode.Success => $"{ReportGeneratedSuccessfully}",
                    ReportExitCode.NoDataFound => $"{Warning}: {NoDataFoundForReportGenerator}",
                    ReportExitCode.ConfigError => $"{Err}: {InvalidReportConfiguration}",
                    _ => $"{ReportGenerationFailed} ({Code}: {(int)exitCode})."
                    };
            Log(message);
            }

        private static int GetAttemptCount(string location)
            {
            _measurementAttemptCountersMap.TryGetValue(location, out int count);
            return count;
            }

        private static void IncrementAttemptCount(string location)
            {
            if (!_measurementAttemptCountersMap.TryAdd(location, 1))
                _measurementAttemptCountersMap[location]++;
            }

        private static void ResetAttemptCount(string location) => _measurementAttemptCountersMap[location] = 0;

        /// <summary>
        /// Clears all measurement attempt counts. Used for testing purposes.
        /// </summary>
        public void ClearAllAttemptCounts() => _measurementAttemptCountersMap.Clear();

        private void Log(string messageOrKey, object[]? args = null, [CallerArgumentExpression(nameof(messageOrKey))] string? resourceName = null) =>
            _logHandler.Log(messageOrKey, args, resourceName);

        private void AddLogMessage(string message) => AppendToLog(message);

        private void AppendToLog(string message)
            {
            if (!string.IsNullOrEmpty(message))
                LogText += $"{DateTime.Now:HH:mm:ss} {message}{Environment.NewLine}";
            }

        private void UpdateUIState()
            {
            IsMeasurementButtonsEnabled = IsDeviceConnected && IsDeviceCalibrated && !string.IsNullOrWhiteSpace(SerialNumber);
            UpdateCommandsCanExecute();
            }

        private void UpdateCommandsCanExecute()
            {
            ( ZeroCalibrationCommand as RelayCommand )?.RaiseCanExecuteChanged();
            ( MeasureCommand as RelayCommand )?.RaiseCanExecuteChanged();
            ( SaveResultsCommand as RelayCommand )?.RaiseCanExecuteChanged();
            ( ApplySerialNumberCommand as RelayCommand )?.RaiseCanExecuteChanged();
            ( NewDeviceUnderTestCommand as RelayCommand )?.RaiseCanExecuteChanged();
            ( ReportGenerateCommand as RelayCommand )?.RaiseCanExecuteChanged();
            ( UploadReportsCommand as RelayCommand )?.RaiseCanExecuteChanged();
            }

        private void UpdateMeasurementStatus(string location, bool? isPassed, string measuredValuesString)
            {
            var status = MeasurementStatusService.Instance.AllMeasurementButtonStatuses.FirstOrDefault(s => s.Location == location);
            if (status != null)
                {
                status.IsPassed = isPassed;
                status.MeasuredValuesString = measuredValuesString;
                }
            else
                {
                Log(MeasButStatusErr, [location]);
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

        private void ExecuteThreadInUI(Action action)
            {
            if (_dispatcher.CheckAccess()) action.Invoke();
            else _dispatcher.BeginInvoke(action);
            }

        // CanExecute predicates
        private bool CanExecuteConnect() => !IsDeviceConnected && !_isDeviceCalibrating && !_isDeviceConnecting;
        private bool CanExecuteDisconnect() => !_isDeviceConnecting && !_isDeviceCalibrating;
        private bool CanExecuteZeroCalibration(object parameter) => !_isDeviceConnecting && !_isDeviceCalibrating;
        private bool CanExecuteSaveResults(object parameter) => _currentDevice != null && !string.IsNullOrWhiteSpace(_currentDevice.SerialNumber) && _currentDevice.Measurements.Count > 0;
        private bool CanExecuteNewDeviceUnderTest(object parameter) => CanExecuteSaveResults(parameter);
        private bool CanExecuteClearFields(object parameter) => true;
        private bool CanExecuteSwitchLanguage(object parameter) => parameter is string code && !string.IsNullOrWhiteSpace(code);
        private bool CanExecuteMeasure(object parameter) => IsDeviceConnected && !_isDeviceCalibrating && !_isDeviceConnecting && IsDeviceCalibrated && !string.IsNullOrWhiteSpace(SerialNumber) && MeasurementTime > 0;
        private bool CanExecuteApplySerialNumber(object parameter) => !string.IsNullOrWhiteSpace(parameter as string);
        private bool CanExecuteReportGenerateCommand(object parameter) => !IsReportGenerating;
        private bool CanExecuteUploadReportsAsync(object parameter) => !IsUploadingReports;

        #endregion

        #region IDisposable

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
            {
            Log(ViewModelClearing);
            _colorMeasurementService.StatusMessage -= ColorMeasurementService_StatusMessage;
            _fileService.StatusMessage -= FileService_StatusMessage;
            _colorMeasurementService.ConnectionStatusChanged -= ColorMeasurementService_ConnectionStatusChanged;
            _colorMeasurementService.CalibrationStatusChanged -= ColorMeasurementService_CalibrationStatusChanged;

            if (_uploadService != null)
                _uploadService.StatusMessage -= (sender, message) => AddLogMessage(message);

            ( _colorMeasurementService as IDisposable )?.Dispose();
            ( _fileService as IDisposable )?.Dispose();
            ( _dialogService as IDisposable )?.Dispose();

            ExecuteClearLog();
            _currentDevice = null;
            _serialNumber = string.Empty;
            _measurementTime = 2;
            _isDeviceConnected = false;
            _isDeviceCalibrated = false;
            _isSerialNumberConfirmed = false;
            _isMeasurementButtonsEnabled = false;

            GC.SuppressFinalize(this);
            Log(ViewModelCleared);
            }

        #endregion

        #region Event Handlers

        private void ColorMeasurementService_StatusMessage(object? sender, string message) =>
            ExecuteThreadInUI(() => AddLogMessage(message));

        private void FileService_StatusMessage(object? sender, string message) =>
            ExecuteThreadInUI(() => AddLogMessage(message));

        private void ColorMeasurementService_CalibrationStatusChanged(object? sender, bool isCalibrated) =>
            ExecuteThreadInUI(() => { IsDeviceCalibrated = isCalibrated; UpdateUIState(); });

        private void ColorMeasurementService_ConnectionStatusChanged(object? sender, bool isConnected) =>
            ExecuteThreadInUI(() => { IsDeviceConnected = isConnected; UpdateUIState(); });

        #endregion

        [GeneratedRegex(SERIAL_NUMBER_PATTERN)]
        public static partial Regex SerialNumberRegex();
        }
    }