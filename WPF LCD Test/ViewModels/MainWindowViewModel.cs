using MvvmHelpers;
using System.Windows.Input;
using WPF_LCD_Test.Commands;
using WPF_LCD_Test.Interfaces;

namespace WPF_LCD_Test.ViewModels
{
    /// <summary>
    /// ViewModel for the main application window, manages navigation between pages.
    /// </summary>
    public class MainWindowViewModel : BaseViewModel, IDisposable
    {
        private readonly IColorMeasurementService _colorMeasurementService;
        private readonly IFileService _fileService;
        private readonly IDialogService _dialogService;
        private readonly ILocalizationService _localizationService;
        private readonly ISettingsService _settingService;
        private readonly IDispatcher _dispatcher;
        private readonly IUploadService _uploadService;
        private BaseViewModel _currentPageViewModel;
        private string _currentPageIdentifier;
        private MeasurementViewModel? _measurementViewModel;
        private SettingsViewModel? _settingsViewModel;

        /// <summary>
        /// Gets the current page identifier.
        /// </summary>
        public string CurrentPageIdentifier
        {
            get => _currentPageIdentifier;
            private set => SetProperty(ref _currentPageIdentifier, value);
        }

        /// <summary>
        /// Gets or sets the current page ViewModel.
        /// </summary>
        public BaseViewModel CurrentPageViewModel
        {
            get => _currentPageViewModel;
            set => SetProperty(ref _currentPageViewModel, value);
        }

        /// <summary>
        /// Gets the command for navigating between pages.
        /// </summary>
        public ICommand NavigateCommand { get; }

        /// <summary>
        /// Initializes a new instance of MainWindowViewModel with required services.
        /// </summary>
        /// <param name="colorMeasurementService">Service for color measurement operations.</param>
        /// <param name="fileService">Service for file operations.</param>
        /// <param name="dialogService">Service for dialog operations.</param>
        /// <param name="localizationService">Service for localization.</param>
        /// <param name="settingsService">Service for application settings.</param>
        /// <param name="uploadService">Service for upload operations.</param>
        /// <param name="dispatcher">Dispatcher for thread marshalling.</param>
        public MainWindowViewModel(
            IColorMeasurementService colorMeasurementService,
            IFileService fileService,
            IDialogService dialogService,
            ILocalizationService localizationService,
            ISettingsService settingsService,
            IUploadService uploadService,
            IDispatcher dispatcher) : base()
        {
            _colorMeasurementService = colorMeasurementService ?? throw new ArgumentNullException(nameof(colorMeasurementService));
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
            _settingService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            _uploadService = uploadService ?? throw new ArgumentNullException(nameof(uploadService));
            NavigateCommand = new RelayCommand(ExecuteNavigate, CanExecuteNavigate);
            ExecuteNavigate("Measurement");
        }

        /// <summary>
        /// Determines whether navigation can be executed.
        /// </summary>
        /// <param name="parameter">Navigation parameter.</param>
        /// <returns>True if navigation is allowed.</returns>
        private bool CanExecuteNavigate(object parameter) => true;

        /// <summary>
        /// Executes navigation to the specified page.
        /// </summary>
        /// <param name="parameter">Page name as string ("Measurement" or "Settings").</param>
        private void ExecuteNavigate(object parameter)
        {
            string? pageName = parameter as string;
            if (string.IsNullOrEmpty(pageName)) return;
            BaseViewModel? targetViewModel = null;
            switch (pageName)
            {
                case "Measurement":
                    _measurementViewModel ??= new MeasurementViewModel(
                        _colorMeasurementService,
                        _fileService,
                        _dialogService,
                        _localizationService,
                        _dispatcher,
                        _uploadService,
                        _settingService
                    );
                    targetViewModel = _measurementViewModel;
                    break;
                case "Settings":
                    _settingsViewModel ??= new SettingsViewModel(_settingService, _dialogService, _colorMeasurementService, _localizationService);
                    targetViewModel = _settingsViewModel;
                    break;
                default:
                    _measurementViewModel ??= new MeasurementViewModel(
                        _colorMeasurementService,
                        _fileService,
                        _dialogService,
                        _localizationService,
                        _dispatcher,
                        _uploadService,
                        _settingService
                    );
                    targetViewModel = _measurementViewModel;
                    break;
            }
            if (targetViewModel != null && targetViewModel != _currentPageViewModel)
            {
                CurrentPageViewModel = targetViewModel;
            }
        }

        /// <summary>
        /// Releases resources used by the current page ViewModel.
        /// </summary>
        public void Dispose()
        {
            (CurrentPageViewModel as IDisposable)?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}