// В папке ViewModels
// Файл MainWindowViewModel.cs

using MvvmHelpers;
using System.Windows.Input;
using WPF_LCD_Test.Commands;
using WPF_LCD_Test.Interfaces;

namespace WPF_LCD_Test.ViewModels
{
    // ViewModel для главного окна приложения, управляет навигацией между страницами
    public class MainWindowViewModel : BaseViewModel, IDisposable
    {
        // Сервисы для передачи в ViewModel страниц
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

        // Текущий идентификатор страницы
        public string CurrentPageIdentifier
        {
            get => _currentPageIdentifier;
            private set => SetProperty(ref _currentPageIdentifier, value);
        }

        // Текущий ViewModel страницы
        public BaseViewModel CurrentPageViewModel
        {
            get => _currentPageViewModel;
            set => SetProperty(ref _currentPageViewModel, value);
        }

        public ICommand NavigateCommand { get; }

        // Конструктор: инициализация сервисов, команд и стартовой страницы
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

        // Проверяет возможность навигации
        private bool CanExecuteNavigate(object parameter) => true;

        // Выполняет навигацию между страницами
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

        // Освобождает ресурсы текущей страницы
        public void Dispose()
        {
            (CurrentPageViewModel as IDisposable)?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}