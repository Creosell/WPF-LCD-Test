using MvvmHelpers;
using System.Collections.Concurrent;
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
        private readonly IMeasurementStatusService _measurementStatusService;
        private readonly ISpecEvaluationService _specEvaluationService;
        private readonly LogHandler _logHandler;
        private CancellationTokenSource? _specCts;
        private Task _specTask = Task.CompletedTask;
        private string? _lastSpecError;

        private DeviceUnderTest? _currentDevice;
        private string _serialNumber = string.Empty;
        private bool _isSerialNumberConfirmed;
        private bool _isChannelConfirmed;
        private ColorimeterInfo? _colorimeter;
        private readonly IAnalyzerRegistryService _analyzerRegistryService;
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
        private int _logLineCount = 0;
        private LogSeverity _pendingLogSeverity = LogSeverity.Info;
        private int _selectedTabIndex;
        private int _unreadIssues;

        private readonly Dictionary<string, DeviceConfigTreeNode> _configNodesByPath = [];
        private DeviceConfigTreeNode? _selectedConfigNode;

        private const string SERIAL_NUMBER_PATTERN = "^[a-zA-Z0-9]*$";
        private const int MAX_MEASUREMENT_ATTEMPTS_BEFORE_CONFIRM = 1;
        private const double COLOR_COORDINATES_TOLERANCE = 0.15;
        private const string PreviewSerialNumber = "preview";
        private const int LogTabIndex = 1;

        private static readonly Dictionary<string, (double x, double y)> primariesNTSC = new()
        {
            { MeasurementLocation.RedColor.ToString(), (0.67, 0.33) },
            { MeasurementLocation.GreenColor.ToString(), (0.21, 0.71) },
            { MeasurementLocation.BlueColor.ToString(), (0.14, 0.08) },
            { MeasurementLocation.WhiteColor.ToString(), (0.3127, 0.3290) }
        };

        private static readonly ConcurrentDictionary<string, int> _measurementAttemptCountersMap = new();

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

        /// <summary>
        /// Gets the device info: the specification check of the measurements taken so far.
        /// </summary>
        public SpecSummaryViewModel Spec { get; }

        /// <summary>
        /// Gets the signal the Save results button breathes on (green: saved, red: failed).
        /// </summary>
        public ActionFeedbackSignal SaveFeedback { get; } = new();

        /// <summary>
        /// Gets the signal the Clear results button breathes on.
        /// </summary>
        public ActionFeedbackSignal ClearFeedback { get; } = new();

        /// <summary>
        /// Gets the signal the Create report button breathes on.
        /// </summary>
        public ActionFeedbackSignal ReportFeedback { get; } = new();

        /// <summary>
        /// Gets the signal the Upload reports button breathes on.
        /// </summary>
        public ActionFeedbackSignal UploadFeedback { get; } = new();

        /// <summary>
        /// Gets the signal the serial number field breathes on (green: applied, red: rejected).
        /// </summary>
        public ActionFeedbackSignal SerialFeedback { get; } = new();

        /// <summary>
        /// Gets the signal the device configuration selector breathes on when a configuration is chosen.
        /// </summary>
        public ActionFeedbackSignal ConfigFeedback { get; } = new();

        /// <summary>
        /// Gets the suffix of the Upload reports button label with the number of reports waiting for upload (empty when none).
        /// </summary>
        public string PendingReportsSuffix => _pendingReportsCount > 0 ? $" ({_pendingReportsCount})" : string.Empty;

        public bool IsTvCheckboxChecked
            {
            get => _isTvCheckboxChecked;
            set
                {
                if (SetProperty(ref _isTvCheckboxChecked, value))
                    {
                    RefreshResults();
                    RequestSpecCheck();
                    }
                }
            }

        public string SelectedDeviceConfiguration
            {
            get => _selectedDeviceConfiguration;
            set
                {
                if (SetProperty(ref _selectedDeviceConfiguration, value))
                    {
                    OnPropertyChanged(nameof(SelectedDeviceConfigurationDisplayName));
                    OnPropertyChanged(nameof(HasSelectedDeviceConfiguration));
                    UpdateSelectedConfigNodeHighlight();
                    RequestSpecCheck();
                    }
                }
            }

        /// <summary>
        /// Gets the file name (without folder path) of the currently selected device configuration,
        /// shown on the closed cascading configuration menu button, or a placeholder when none is selected.
        /// </summary>
        public string SelectedDeviceConfigurationDisplayName =>
            string.IsNullOrEmpty(_selectedDeviceConfiguration) ? ChooseConfiguration : Path.GetFileName(_selectedDeviceConfiguration);

        /// <summary>
        /// Gets whether a device configuration is currently selected, used to dim the
        /// configuration button text while it shows the "choose configuration" placeholder.
        /// </summary>
        public bool HasSelectedDeviceConfiguration => !string.IsNullOrEmpty(_selectedDeviceConfiguration);

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

        private bool _isSavingDevice;
        private bool _hasUnsavedMeasurements;
        private int _pendingReportsCount;
        private Process? _reportProcess;
        private bool _reportCancelRequested;

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

        /// <summary>
        /// Gets the log entries shown in the Log tab (the text of the same entries is <see cref="LogText"/>).
        /// </summary>
        public ObservableCollection<LogEntry> LogEntries { get; } = [];

        /// <summary>
        /// Gets the per-point values of the Results tab.
        /// </summary>
        public ObservableCollection<MeasurementResultRow> Results { get; }

        /// <summary>
        /// Gets or sets the selected tab of the left block: 0 - Results, 1 - Log. Opening the log clears the unread counter.
        /// </summary>
        public int SelectedTabIndex
            {
            get => _selectedTabIndex;
            set
                {
                if (SetProperty(ref _selectedTabIndex, value) && value == LogTabIndex)
                    UnreadIssues = 0;
                }
            }

        /// <summary>
        /// Gets the number of warnings and errors added to the log while the Log tab was not shown.
        /// </summary>
        public int UnreadIssues
            {
            get => _unreadIssues;
            private set => SetProperty(ref _unreadIssues, value);
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
                    OnPropertyChanged(nameof(DeviceStateText));
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
                    if (!value) IsChannelConfirmed = false;
                    UpdateUIState();
                    OnPropertyChanged(nameof(DeviceStateText));
                    }
                }
            }

        /// <summary>
        /// Gets the full description of the analyzer state shown as the tooltip of the state indicator.
        /// </summary>
        public string DeviceStateText => !_isDeviceConnected ? DeviceStateNotConnected
            : _isDeviceCalibrated ? DeviceStateConnectedCalibrated
            : DeviceStateConnectedNotCalibrated;

        /// <summary>
        /// Gets the analyzer model name: the live value while the channel is confirmed, the last confirmed one
        /// while it is being re-confirmed, empty before the first confirmation (the card does not show it then).
        /// </summary>
        public string DeviceModelName =>
            _isChannelConfirmed ? _colorMeasurementService.Model.ToDisplayName() : _colorimeter?.Model ?? string.Empty;

        /// <summary>
        /// Gets the active measurement channel label: the live value while the channel is confirmed, the last confirmed one
        /// while it is being re-confirmed, empty before the first confirmation (the card does not show it then).
        /// </summary>
        public string DeviceActiveChannelStatusText =>
            _isChannelConfirmed ? string.Format(ActiveChannelStatus, _colorMeasurementService.CurrentChannel)
            : _colorimeter != null ? string.Format(ActiveChannelStatus, _colorimeter.Channel)
            : string.Empty;

        /// <summary>
        /// Gets the probe serial number label: the last confirmed probe, empty before the first confirmation.
        /// </summary>
        public string DeviceProbeSerialText =>
            _colorimeter != null ? string.Format(ProbeSerialNumberLabel, _colorimeter.ProbeSN) : string.Empty;

        /// <summary>
        /// Gets or sets whether the inspector has confirmed the active channel after calibration.
        /// Model and channel labels stay hidden until then.
        /// </summary>
        public bool IsChannelConfirmed
            {
            get => _isChannelConfirmed;
            set
                {
                if (SetProperty(ref _isChannelConfirmed, value))
                    {
                    OnPropertyChanged(nameof(DeviceModelName));
                    OnPropertyChanged(nameof(DeviceActiveChannelStatusText));
                    OnPropertyChanged(nameof(DeviceProbeSerialText));
                    }
                }
            }
        public ObservableCollection<DeviceConfigTreeNode> DeviceConfigurationTree { get; } = [];
        public IMeasurementStatusService MeasurementStatusService => _measurementStatusService;

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
        public ICommand SelectDeviceConfigurationCommand { get; }

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
        /// <param name="measurementStatusService">Service for managing measurement statuses.</param>
        /// <param name="analyzerRegistryService">Registry of the last confirmed channel per probe.</param>
        /// <param name="specEvaluationService">Service checking the measurements against the device specification.</param>
        public MeasurementViewModel(
            IColorMeasurementService colorMeasurementService,
            IFileService fileService,
            IDialogService dialogService,
            ILocalizationService localizationService,
            IDispatcher dispatcher,
            IUploadService uploadService,
            ISettingsService settingsService,
            IPathProvider pathProvider,
            IMeasurementStatusService measurementStatusService,
            IAnalyzerRegistryService analyzerRegistryService,
            ISpecEvaluationService specEvaluationService)
            {
            _colorMeasurementService = colorMeasurementService ?? throw new ArgumentNullException(nameof(colorMeasurementService));
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            _uploadService = uploadService ?? throw new ArgumentNullException(nameof(uploadService));
            _pathProvider = pathProvider ?? throw new ArgumentNullException(nameof(pathProvider));
            _measurementStatusService = measurementStatusService ?? throw new ArgumentNullException(nameof(measurementStatusService));
            _analyzerRegistryService = analyzerRegistryService ?? throw new ArgumentNullException(nameof(analyzerRegistryService));
            _specEvaluationService = specEvaluationService ?? throw new ArgumentNullException(nameof(specEvaluationService));
            _logHandler = new LogHandler(_localizationService, AddLogMessage);
            Spec = new SpecSummaryViewModel(_measurementStatusService, AppendToLog);
            Results = new(( _measurementStatusService.AllMeasurementButtonStatuses ?? [] ).Select(status => new MeasurementResultRow(status)));

            ZeroCalibrationCommand = new RelayCommand(ExecuteZeroCalibrationAsync, CanExecuteZeroCalibration);
            SaveResultsCommand = new RelayCommand(ExecuteSaveResultsAsync, CanExecuteSaveResults);
            ClearFieldsCommand = new RelayCommand(ExecuteClearFields, CanExecuteClearFields);
            ClearLogCommand = new RelayCommand(ExecuteClearLog);
            SwitchLanguageCommand = new RelayCommand(ExecuteSwitchLanguage, CanExecuteSwitchLanguage);
            NewDeviceUnderTestCommand = new RelayCommand(ExecuteNewDeviceUnderTest, CanExecuteNewDeviceUnderTest, allowConcurrentExecution: true);
            ReportGenerateCommand = new RelayCommand(ExecuteReportGenerateCommand, CanExecuteReportGenerateCommand);
            MeasureCommand = new RelayCommand(ExecuteMeasureAsync, CanExecuteMeasure);
            ApplySerialNumberCommand = new RelayCommand(ExecuteApplySerialNumber, CanExecuteApplySerialNumber);
            UploadReportsCommand = new RelayCommand(ExecuteUploadReportsAsync, CanExecuteUploadReportsAsync, allowConcurrentExecution: true);
            RefreshPendingReports();
            SelectDeviceConfigurationCommand = new RelayCommand(ExecuteSelectDeviceConfiguration, CanExecuteSelectDeviceConfiguration);

            _colorMeasurementService.StatusMessage += ColorMeasurementService_StatusMessage;
            _fileService.StatusMessage += FileService_StatusMessage;
            _colorMeasurementService.ConnectionStatusChanged += ColorMeasurementService_ConnectionStatusChanged;
            _colorMeasurementService.CalibrationStatusChanged += ColorMeasurementService_CalibrationStatusChanged;
            _colorMeasurementService.CurrentChannelChanged += ColorMeasurementService_CurrentChannelChanged;
            _uploadService.StatusMessage += UploadService_StatusMessage;
            _localizationService.LanguageChanged += LocalizationService_LanguageChanged;

            InitializeDeviceConfigurations();
            RefreshResults();
            RequestSpecCheck();
            UpdateUIState();
            }

        #endregion

        #region Methods

        /// <summary>
        /// Loads device configuration files from the disk, including any nested subfolders,
        /// and populates the hierarchical selection tree for cascading menu display.
        /// </summary>
        private void InitializeDeviceConfigurations()
            {
            try
                {
                if (!Directory.Exists(_pathProvider.ConfigDirectory))
                    Directory.CreateDirectory(_pathProvider.ConfigDirectory);

                DeviceConfigurationTree.Clear();
                _configNodesByPath.Clear();
                _selectedConfigNode = null;
                foreach (var node in BuildDeviceConfigurationTree(_pathProvider.ConfigDirectory))
                    DeviceConfigurationTree.Add(node);

                if (FindFirstConfiguration(DeviceConfigurationTree) == null)
                    Log(ConfigDirNotFound, [_pathProvider.ConfigDirectory], LogSeverity.Error);
                }
            catch (Exception ex)
                {
                Log(ErrUnexpected, [ex.Message], LogSeverity.Error);
                }
            }

        /// <summary>
        /// Recursively builds device configuration tree nodes for a directory: subfolders
        /// (containing at least one configuration file, direct or nested) ordered alphabetically
        /// first, followed by the directory's own configuration files ordered alphabetically.
        /// </summary>
        /// <param name="directoryPath">Directory to scan.</param>
        /// <returns>Sequence of child nodes for the directory.</returns>
        private IEnumerable<DeviceConfigTreeNode> BuildDeviceConfigurationTree(string directoryPath)
            {
            foreach (var subDirectory in Directory.GetDirectories(directoryPath).OrderBy(d => d, StringComparer.OrdinalIgnoreCase))
                {
                var folderNode = new DeviceConfigTreeNode { DisplayName = Path.GetFileName(subDirectory), IsFolder = true };
                foreach (var childNode in BuildDeviceConfigurationTree(subDirectory))
                    folderNode.Children.Add(childNode);

                if (folderNode.Children.Any())
                    yield return folderNode;
                }

            foreach (var file in Directory.GetFiles(directoryPath, "*.yaml").OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
                {
                var relativePath = Path.GetRelativePath(_pathProvider.ConfigDirectory, file);
                var relativePathWithoutExtension = Path.Combine(
                    Path.GetDirectoryName(relativePath) ?? string.Empty,
                    Path.GetFileNameWithoutExtension(relativePath));

                var fileNode = new DeviceConfigTreeNode
                    {
                    DisplayName = Path.GetFileNameWithoutExtension(file),
                    RelativePath = relativePathWithoutExtension,
                    IsFolder = false,
                    SelectCommand = SelectDeviceConfigurationCommand
                    };
                _configNodesByPath[relativePathWithoutExtension] = fileNode;
                yield return fileNode;
                }
            }

        /// <summary>
        /// Moves the checkmark shown in the cascading configuration menu to the node matching
        /// the currently selected device configuration.
        /// </summary>
        private void UpdateSelectedConfigNodeHighlight()
            {
            if (_selectedConfigNode != null)
                _selectedConfigNode.IsSelected = false;

            _selectedConfigNode = !string.IsNullOrEmpty(_selectedDeviceConfiguration) &&
                _configNodesByPath.TryGetValue(_selectedDeviceConfiguration, out var node)
                    ? node
                    : null;

            if (_selectedConfigNode != null)
                _selectedConfigNode.IsSelected = true;
            }

        /// <summary>
        /// Finds the identifier of the first configuration file in the tree, following display order
        /// (subfolders before files at every level), for use as the default selection.
        /// </summary>
        /// <param name="nodes">Nodes to search, in display order.</param>
        /// <returns>Relative path of the first configuration file found, or null if none exist.</returns>
        private static string? FindFirstConfiguration(IEnumerable<DeviceConfigTreeNode> nodes)
            {
            foreach (var node in nodes)
                {
                if (!node.IsFolder)
                    return node.RelativePath;

                var foundInChildren = FindFirstConfiguration(node.Children);
                if (foundInChildren != null)
                    return foundInChildren;
                }
            return null;
            }

        /// <summary>
        /// Applies the device configuration selected from the cascading configuration menu.
        /// </summary>
        /// <param name="parameter">Relative path identifier of the selected configuration file.</param>
        private void ExecuteSelectDeviceConfiguration(object parameter)
            {
            SelectedDeviceConfiguration = (string)parameter;
            Flash(ConfigFeedback, true);
            }

        /// <summary>
        /// Determines whether a configuration menu entry represents a selectable configuration file
        /// rather than a folder node.
        /// </summary>
        /// <param name="parameter">Relative path identifier, or null for folder nodes.</param>
        /// <returns>True if the entry is a configuration file; otherwise, false.</returns>
        private bool CanExecuteSelectDeviceConfiguration(object parameter)
            {
            return parameter is string relativePath && !string.IsNullOrEmpty(relativePath);
            }

        /// <summary>
        /// Verifies if all required measurement statuses are present in the current device data.
        /// </summary>
        /// <returns>True if all statuses are represented; otherwise, false.</returns>
        public bool AreAllStatusesRepresentedInMeasurements()
            {
            if (_currentDevice?.Measurements == null || _currentDevice.Measurements.Count == 0)
                return false;

            var allStatusPoints = _measurementStatusService.AllMeasurementButtonStatuses;
            if (allStatusPoints == null || !allStatusPoints.Any())
                return false;

            return allStatusPoints.All(status =>
                ( status.Location == MeasurementLocation.WhiteColor.ToString() && !_currentDevice.IsTV ) ||
                _currentDevice.Measurements.Any(measurement => measurement.Location == status.Location));
            }

        /// <summary>
        /// Attempts to establish a connection with the measurement device.
        /// </summary>
        /// <returns>True if connection was successful, false otherwise.</returns>
        private async Task<bool> ExecuteConnectAsync()
            {
            if (!CanExecuteConnect()) return false;

            try
                {
                _isDeviceConnecting = true;
                bool connected = await _colorMeasurementService.ConnectAsync();
                if (!connected)
                    ExecuteDisconnect(force: true);
                return connected;
                }
            catch
                {
                ExecuteDisconnect(force: true);
                return false;
                }
            finally
                {
                _isDeviceConnecting = false;
                }
            }

        /// <summary>
        /// Disconnects the measurement device and updates the UI state.
        /// </summary>
        /// <param name="force">If true, bypasses state checks for error recovery scenarios.</param>
        private void ExecuteDisconnect(bool force = false)
            {
            if (!force && !CanExecuteDisconnect()) return;

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
                // Use return value directly to avoid race condition with event-based property updates
                bool isConnected = _colorMeasurementService.IsDeviceConnected;
                if (!isConnected)
                    isConnected = await ExecuteConnectAsync();

                if (isConnected)
                    {
                    _isDeviceCalibrating = true;
                    if (!await _colorMeasurementService.CalibrateZeroAsync())
                        {
                        ExecuteDisconnect(force: true);
                        _dialogService.ShowMessage($"{ErrAtCalibration}", $"{Err}");
                        return;
                        }
                    if (!ConfirmActiveChannel())
                        {
                        ExecuteDisconnect(force: true);
                        return;
                        }
                    }
                }
            catch
                {
                // Force disconnect on error to cleanup COM objects
                if (_colorMeasurementService.IsDeviceConnected)
                    ExecuteDisconnect(force: true);
                _dialogService.ShowMessage($"{ErrAtCalibration}", $"{Err}");
                }
            finally
                {
                _isDeviceCalibrating = false;
                UpdateUIState();
                }
            }

        /// <summary>
        /// Reconciles the channel currently set on the probe with the last channel registered for it.
        /// On mismatch the inspector picks one of the two; the confirmed channel is stored in the registry and in the settings.
        /// </summary>
        /// <returns>True when a channel was confirmed; false when the inspector dismissed the dialog.</returns>
        private bool ConfirmActiveChannel()
            {
            string probeSN = _colorMeasurementService.ProbeSN;
            int channel = _colorMeasurementService.CurrentChannel;

            if (_analyzerRegistryService.GetLastChannel(probeSN) is int lastChannel && lastChannel != channel)
                {
                int? choice = _dialogService.ShowChoice(
                    string.Format(ChannelMismatchMessage, lastChannel, channel),
                    ChannelMismatchTitle,
                    string.Format(UseProbeChannel, channel),
                    string.Format(UseLastChannel, lastChannel));

                if (choice is null) return false;

                if (choice == 1)
                    {
                    _colorMeasurementService.CurrentChannel = lastChannel;
                    channel = lastChannel;
                    }
                }

            _analyzerRegistryService.Update(probeSN, _colorMeasurementService.Model, channel);
            _settingsService.UpdateChannel(channel);
            _colorimeter = new ColorimeterInfo(_colorMeasurementService.Model.ToDisplayName(), probeSN, channel);
            if (_currentDevice != null) _currentDevice.Colorimeter = _colorimeter;
            IsChannelConfirmed = true;
            return true;
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
                    Flash(SaveFeedback, false);
                    return false;
                    }

                if (string.IsNullOrEmpty(SelectedDeviceConfiguration))
                    {
                    Log(SaveNeedsConfiguration, severity: LogSeverity.Warning);
                    _dialogService.ShowMessage(ResourceManager.GetString("SaveNeedsConfiguration")!, $"{Err}");
                    Flash(SaveFeedback, false);
                    return false;
                    }

                _currentDevice.DeviceConfiguration = SelectedDeviceConfiguration;
                _currentDevice.IsTV = IsTvCheckboxChecked;

                await _specTask;
                var problems = Spec.DescribeProblems();
                bool incomplete = !AreAllStatusesRepresentedInMeasurements();
                if ((problems.Count > 0 || incomplete) && !_dialogService.ShowQuestion(BuildSaveWarning(problems, incomplete), $"{Warning}"))
                    {
                    Log(SaveCanceled);
                    return false;
                    }

                bool saved = await _fileService.SaveDeviceDataToJsonAsync(_currentDevice);
                if (saved) _hasUnsavedMeasurements = false;
                Flash(SaveFeedback, saved);
                return saved;
                }
            catch (Exception ex)
                {
                Log(ErrUnexpected, [ex.Message], LogSeverity.Error);
                _dialogService.ShowMessage($"{SaveJSONErrForSN}: {ex.Message}", $"{Err}");
                Flash(SaveFeedback, false);
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
                _hasUnsavedMeasurements = false;
                if (parameter?.Equals("CalledFromNewDeviceMethod") != true) Flash(ClearFeedback, true);
                Spec.Reset();
                RequestSpecCheck();
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
                    Log(ColorServiceErr, [measurementLocation], LogSeverity.Error);
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
                RefreshResults();
                RequestSpecCheck();
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
                Flash(SerialFeedback, true);
                RequestClearInputFocus?.Invoke(this, EventArgs.Empty);
                }
            else
                {
                IsSerialNumberConfirmed = false;
                Flash(SerialFeedback, false);
                _dialogService.ShowMessage($"{IncorrectFormatForSNErr}", $"{Err}");
                return;
                }

            if (_currentDevice == null || _currentDevice.SerialNumber != SerialNumber)
                {
                try
                    {
                    _currentDevice = new DeviceUnderTest(SerialNumber) { Colorimeter = _colorimeter };
                    ResetMeasurementStatuses();
                    RequestSpecCheck();
                    }
                catch (ArgumentException ex)
                    {
                    IsSerialNumberConfirmed = false;
                    Log(ErrWithArg, [ex.Message], LogSeverity.Error);
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
            if (_isSavingDevice) return;

            _isSavingDevice = true;
            try
                {
                if (await ExecuteSaveResultsAsync(parameter))
                    ExecuteClearFields("CalledFromNewDeviceMethod");
                }
            finally
                {
                _isSavingDevice = false;
                }
            UpdateUIState();
            }

        /// <summary>
        /// Clears the log text.
        /// </summary>
        public void ExecuteClearLog()
            {
            LogText = string.Empty;
            LogEntries.Clear();
            UnreadIssues = 0;
            _logLineCount = 0;
            }

        /// <summary>
        /// Executes the external report generator application.
        /// </summary>
        private void ExecuteReportGenerateCommand(object parameter)
            {
            if (IsReportGenerating)
                {
                CancelReportGeneration();
                return;
                }

            try
                {
                var appDir = AppDomain.CurrentDomain.BaseDirectory;
                var exePath = Path.Combine(appDir, "ReportGenerator.exe");

                if (!File.Exists(exePath))
                    {
                    Log(RunExternalAppNotFoundErr, [exePath], LogSeverity.Error);
                    Flash(ReportFeedback, false);
                    return;
                    }

                Log(StartingReportGeneration);
                _reportCancelRequested = false;
                IsReportGenerating = true;

                Task.Run(() =>
                {
                    using var process = new Process
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

                    try
                        {
                        process.Start();
                        _reportProcess = process;
                        process.BeginOutputReadLine();
                        process.BeginErrorReadLine();
                        process.WaitForExit();
                        if (_reportCancelRequested)
                            {
                            Log(ReportGenerationCanceled, severity: LogSeverity.Warning);
                            FlashCanceled(ReportFeedback);
                            }
                        else
                            {
                            HandleReportGenerationResult((ReportExitCode)process.ExitCode);
                            }
                        }
                    catch (Exception ex)
                        {
                        Log(ErrorInBackgroundTask, [ex.Message], LogSeverity.Error);
                        }
                    finally
                        {
                        _reportProcess = null;
                        ExecuteThreadInUI(() => IsReportGenerating = false);
                        }
                });
                }
            catch (Exception ex)
                {
                Log(RunExternalAppUnexpectedErr, [ex.Message], LogSeverity.Error);
                IsReportGenerating = false;
                }
            }

        /// <summary>
        /// Cancels the running report generation by killing the generator process.
        /// </summary>
        private void CancelReportGeneration()
            {
            _reportCancelRequested = true;
            try
                {
                _reportProcess?.Kill(true);
                }
            catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
                {
                }
            }

        /// <summary>
        /// Uploads the generated reports using the upload service; calling it again while the upload runs cancels it.
        /// </summary>
        private async Task ExecuteUploadReportsAsync(object parameter)
            {
            if (IsUploadingReports)
                {
                _uploadService.CancelUpload();
                return;
                }

            if (string.IsNullOrEmpty(_selectedDeviceConfiguration))
                {
                _dialogService.ShowMessage($"{CantStartUpload}", $"{Err}");
                Flash(UploadFeedback, false);
                return;
                }

            Log(StartingReportUploading);
            IsUploadingReports = true;

            try
                {
                bool uploaded = await _uploadService.UploadReportsAsync();
                RefreshPendingReports();
                if (!uploaded)
                    {
                    Log(UploadFailed, severity: LogSeverity.Error);
                    }
                Flash(UploadFeedback, uploaded);
                }
            catch (OperationCanceledException)
                {
                Log(UploadCanceled, severity: LogSeverity.Warning);
                RefreshPendingReports();
                FlashCanceled(UploadFeedback);
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

                _currentDevice = new DeviceUnderTest(SerialNumber) { Colorimeter = _colorimeter };
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
                string logMessage = $"{FailedMeasurementForLocation}: {measurement.Location}. {validationMessage.Replace('\n', ' ')}";
                Log(logMessage, severity: LogSeverity.Warning);
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
            _currentDevice!.RecordAttempt(measurement);
            _hasUnsavedMeasurements = true;
            }

        internal static (string x, string y, string Lv, string T) FormatMeasurement(Measurement measurement)
            {
            return (
                measurement.x.ToString("F3", CultureInfo.InvariantCulture),
                measurement.y.ToString("F3", CultureInfo.InvariantCulture),
                measurement.Location == MeasurementLocation.BlackColor.ToString()
                    ? measurement.Lv.ToString("F6", CultureInfo.InvariantCulture)
                    : measurement.Lv.ToString("F1", CultureInfo.InvariantCulture),
                double.IsFinite(measurement.T) ? measurement.T.ToString("F0", CultureInfo.InvariantCulture) : "n/a"
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
            Flash(ReportFeedback, exitCode == ReportExitCode.Success);
            ExecuteThreadInUI(RefreshPendingReports);
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

        private void Log(string messageOrKey, object[]? args = null, LogSeverity severity = LogSeverity.Info, [CallerArgumentExpression(nameof(messageOrKey))] string? resourceName = null)
            {
            _pendingLogSeverity = severity;
            try
                {
                _logHandler.Log(messageOrKey, args, resourceName);
                }
            finally
                {
                _pendingLogSeverity = LogSeverity.Info;
                }
            }

        private void AddLogMessage(string message) => AppendToLog(message, _pendingLogSeverity);

        /// <summary>
        /// Appends a message to the log with automatic cleanup of old entries.
        /// Limits log to last 500 lines to prevent memory issues.
        /// </summary>
        private void AppendToLog(string message, LogSeverity severity = LogSeverity.Info)
            {
            if (string.IsNullOrEmpty(message)) return;

            const int maxLines = 500;
            _logLineCount++;

            // If we exceed the limit, trim old lines
            if (_logLineCount > maxLines)
                {
                var lines = LogText.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
                var linesToKeep = lines.Skip(lines.Length - maxLines + 1);
                LogText = string.Join(Environment.NewLine, linesToKeep) + Environment.NewLine;
                _logLineCount = maxLines;
                }

            var entry = new LogEntry(DateTime.Now, message, severity);
            if (severity != LogSeverity.Info) ExecuteThreadInUI(() => AddLogEntry(entry));

            LogText += $"{entry.Time:HH:mm:ss} {message}{Environment.NewLine}";
            }

        /// <summary>
        /// Adds an entry to the collection the Log tab shows; must run on the UI thread because the collection is bound
        /// (messages may be logged from background tasks, e.g. when the report generator finishes).
        /// </summary>
        private void AddLogEntry(LogEntry entry)
            {
            const int maxEntries = 500;
            if (LogEntries.Count >= maxEntries) LogEntries.RemoveAt(0);
            LogEntries.Add(entry);
            if (SelectedTabIndex != LogTabIndex) UnreadIssues++;
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
            var status = _measurementStatusService.AllMeasurementButtonStatuses.FirstOrDefault(s => s.Location == location);
            if (status != null)
                {
                status.IsPassed = isPassed;
                status.MeasuredValuesString = measuredValuesString;
                }
            else
                {
                Log(MeasButStatusErr, [location], LogSeverity.Error);
                }
            }

        /// <summary>
        /// Starts the specification check of the current measurements; the previous request is abandoned.
        /// </summary>
        private void RequestSpecCheck() => _specTask = CheckSpecAsync();

        /// <summary>
        /// Evaluates the measurements taken so far against the selected configuration and shows the result.
        /// Without a device, an empty one is evaluated so the table lists the checked metrics as pending.
        /// </summary>
        private async Task CheckSpecAsync()
            {
            _specCts?.Cancel();
            var cts = _specCts = new CancellationTokenSource();

            if (string.IsNullOrEmpty(SelectedDeviceConfiguration))
                {
                ExecuteThreadInUI(() => Spec.ShowMessage(ResourceManager.GetString("SpecSelectConfig")!));
                return;
                }

            var device = _currentDevice ?? new DeviceUnderTest(PreviewSerialNumber);
            device.DeviceConfiguration = SelectedDeviceConfiguration;
            device.IsTV = IsTvCheckboxChecked;
            string deviceJson = _fileService.SerializeDevice(device);
            int measuredPoints = device.Measurements.Count;
            int totalPoints = _measurementStatusService.AllMeasurementButtonStatuses.Count - (device.IsTV ? 0 : 1);

            SpecEvaluationOutcome outcome;
            try
                {
                outcome = await _specEvaluationService.EvaluateAsync(deviceJson, cts.Token);
                }
            catch (OperationCanceledException)
                {
                return;
                }
            if (cts.IsCancellationRequested) return;

            ExecuteThreadInUI(() =>
            {
                if (outcome.Evaluation != null)
                    {
                    _lastSpecError = null;
                    ApplyAttempts(device, outcome.Evaluation);
                    Spec.Show(outcome.Evaluation, measuredPoints, totalPoints);
                    return;
                    }

                string error = outcome.Error ?? "?";
                Spec.ShowMessage(string.Format(ResourceManager.GetString("SpecUnavailable")!, error));
                if (error != _lastSpecError)
                    {
                    _lastSpecError = error;
                    AppendToLog(string.Format(ResourceManager.GetString("SpecUnavailable")!, error), LogSeverity.Warning);
                    }
            });
            }

        /// <summary>
        /// Takes over the choice of the best attempt of every point from the specification check and shows the attempts counters.
        /// A result computed for an older state of the device (a point was measured meanwhile) is not applied.
        /// </summary>
        private void ApplyAttempts(DeviceUnderTest device, SpecEvaluation evaluation)
            {
            foreach (var (location, info) in evaluation.Attempts)
                {
                if (info.Count > 1 && device.AttemptCount(location) == info.Count)
                    device.SelectBestAttempt(location, info.Best);
                }

            RefreshResults();
            foreach (var row in Results)
                {
                row.ShowAttempts(evaluation.Attempts.GetValueOrDefault(row.Location));
                }
            }

        /// <summary>
        /// Builds the confirmation text for saving a device that has remarks against the specification.
        /// </summary>
        private static string BuildSaveWarning(IReadOnlyList<SpecProblem> problems, bool incomplete)
            {
            bool fewAttempts = problems.Any(problem => problem.FewAttempts);
            var lines = new List<string>();
            if (incomplete)
                {
                lines.Add(ResourceManager.GetString("SaveNotFullNote")!);
                if (problems.Count > 0) lines.Add(string.Empty);
                }

            if (problems.Count > 0)
                {
                lines.Add(ResourceManager.GetString(fewAttempts ? "SaveSpecHeaderFewAttempts" : "SaveSpecHeader")!);
                lines.AddRange(problems.Select(problem => problem.Text));
                }

            if (fewAttempts)
                {
                lines.Add(string.Empty);
                lines.Add(ResourceManager.GetString("SaveSpecRemeasureHint")!);
                }

            lines.Add(string.Empty);
            lines.Add(ResourceManager.GetString("SaveSpecQuestion")!);
            return string.Join(Environment.NewLine, lines);
            }

        private void LocalizationService_LanguageChanged(object? sender, EventArgs e)
            {
            Spec.Refresh();
            RefreshResults();
            }

        /// <summary>
        /// Updates the results table from the measurements of the current device.
        /// </summary>
        private void RefreshResults()
            {
            foreach (var row in Results)
                {
                row.IsAvailable = row.Location != nameof(MeasurementLocation.WhiteColor) || IsTvCheckboxChecked;
                row.Show(_currentDevice?.Measurements.FirstOrDefault(m => m.Location == row.Location));
                row.RefreshName();
                }
            }

        private void ClearAttempts()
            {
            foreach (var row in Results) row.ShowAttempts(null);
            }

        private void ResetMeasurementStatuses()
            {
            foreach (var status in _measurementStatusService.AllMeasurementButtonStatuses)
                {
                status.IsPassed = null;
                status.MeasuredValuesString = "";
                status.SpecRemark = SpecStatus.Pass;
                status.SpecTooltip = "";
                }
            RefreshResults();
            ClearAttempts();
            }

        /// <summary>
        /// Signals the outcome of a button action to the view (from any thread).
        /// </summary>
        private void Flash(ActionFeedbackSignal signal, bool success) =>
            ExecuteThreadInUI(() =>
            {
                if (success) signal.Succeed();
                else signal.Fail();
            });

        /// <summary>
        /// Recounts the reports waiting for upload for the label of the Upload reports button.
        /// </summary>
        private void RefreshPendingReports()
            {
            _pendingReportsCount = _uploadService.GetPendingReportCount();
            OnPropertyChanged(nameof(PendingReportsSuffix));
            }

        /// <summary>
        /// Asks the operator to confirm closing the application when the measurements of the current device are not saved.
        /// </summary>
        /// <returns>True if the application may close.</returns>
        public bool ConfirmClose() =>
            !_hasUnsavedMeasurements
            || _dialogService.ShowQuestion(ResourceManager.GetString("CloseUnsavedQuestion")!, $"{Warning}");

        /// <summary>
        /// Signals that the operator canceled a button action (from any thread).
        /// </summary>
        private void FlashCanceled(ActionFeedbackSignal signal) => ExecuteThreadInUI(signal.Cancel);

        private void ExecuteThreadInUI(Action action)
            {
            if (_dispatcher.CheckAccess()) action.Invoke();
            else _dispatcher.BeginInvoke(action);
            }

        // CanExecute predicates
        private bool CanExecuteConnect() => !IsDeviceConnected && !_isDeviceCalibrating && !_isDeviceConnecting;
        private bool CanExecuteDisconnect() => !_isDeviceCalibrating && !_isDeviceConnecting;
        private bool CanExecuteZeroCalibration(object parameter) => !_isDeviceConnecting && !_isDeviceCalibrating;
        private bool CanExecuteSaveResults(object parameter) => _currentDevice != null && !string.IsNullOrWhiteSpace(_currentDevice.SerialNumber) && _currentDevice.Measurements.Count > 0;
        private bool CanExecuteNewDeviceUnderTest(object parameter) => CanExecuteSaveResults(parameter);
        private bool CanExecuteClearFields(object parameter) => true;
        private bool CanExecuteSwitchLanguage(object parameter) => parameter is string code && !string.IsNullOrWhiteSpace(code);
        private bool CanExecuteMeasure(object parameter) => IsDeviceConnected && !_isDeviceCalibrating && !_isDeviceConnecting && IsDeviceCalibrated && !string.IsNullOrWhiteSpace(SerialNumber) && MeasurementTime > 0;
        private bool CanExecuteApplySerialNumber(object parameter) => !string.IsNullOrWhiteSpace(parameter as string);
        private bool CanExecuteReportGenerateCommand(object parameter) => true;
        private bool CanExecuteUploadReportsAsync(object parameter) => true;

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
            _colorMeasurementService.CurrentChannelChanged -= ColorMeasurementService_CurrentChannelChanged;
            _uploadService.StatusMessage -= UploadService_StatusMessage;
            _localizationService.LanguageChanged -= LocalizationService_LanguageChanged;
            _specCts?.Cancel();

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

        private void ColorMeasurementService_StatusMessage(object? sender, string message)
            {
            if (message == Measuring) return;
            ExecuteThreadInUI(() => AppendToLog(message, ServiceMessageSeverity(message)));
            }

        private void FileService_StatusMessage(object? sender, string message) =>
            ExecuteThreadInUI(() => AppendToLog(message, ServiceMessageSeverity(message)));

        private void UploadService_StatusMessage(object? sender, string message) =>
            ExecuteThreadInUI(() => AppendToLog(message, ServiceMessageSeverity(message)));

        /// <summary>
        /// Tells whether a message coming from a service reports a failure, by the localized prefix of its error texts.
        /// </summary>
        private static LogSeverity ServiceMessageSeverity(string message)
            {
            string[] errorPrefixes =
                [
                ConnectionError, CheckConnectionCA, ErrAtCalibration, ErrorAtMeasuringIteration, MeasureWihoutConnectionError,
                SaveJSONErrDeviceIsEmpty, SaveJSONErrForSN, WorkingFolderInitErr, CsvSnErr, CsvDataErr, CsvLocationErr, ErrCSV, UploadFailed
                ];
            return errorPrefixes.Any(prefix => !string.IsNullOrEmpty(prefix) && message.StartsWith(prefix, StringComparison.Ordinal))
                ? LogSeverity.Error
                : LogSeverity.Info;
            }

        private void ColorMeasurementService_CalibrationStatusChanged(object? sender, bool isCalibrated) =>
            ExecuteThreadInUI(() => { IsDeviceCalibrated = isCalibrated; UpdateUIState(); });

        private void ColorMeasurementService_ConnectionStatusChanged(object? sender, bool isConnected) =>
            ExecuteThreadInUI(() => { IsDeviceConnected = isConnected; UpdateUIState(); });

        private void ColorMeasurementService_CurrentChannelChanged(object? sender, int channel) =>
            ExecuteThreadInUI(() => OnPropertyChanged(nameof(DeviceActiveChannelStatusText)));

        #endregion

        [GeneratedRegex(SERIAL_NUMBER_PATTERN)]
        public static partial Regex SerialNumberRegex();
        }
    }