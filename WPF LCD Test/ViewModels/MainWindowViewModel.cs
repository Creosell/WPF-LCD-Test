using MvvmHelpers;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using WPF_LCD_Test.Commands;
using WPF_LCD_Test.Interfaces;

namespace WPF_LCD_Test.ViewModels
{
    /// <summary>
    /// ViewModel for the main application window, manages navigation between pages.
    /// </summary>
    public class MainWindowViewModel : BaseViewModel, IDisposable
    {
        private readonly IServiceProvider _serviceProvider;
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
        /// Initializes a new instance of MainWindowViewModel with service provider.
        /// </summary>
        /// <param name="serviceProvider">Service provider for dependency resolution.</param>
        public MainWindowViewModel(IServiceProvider serviceProvider) : base()
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
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
                    _measurementViewModel ??= _serviceProvider.GetRequiredService<MeasurementViewModel>();
                    targetViewModel = _measurementViewModel;
                    break;
                case "Settings":
                    _settingsViewModel ??= _serviceProvider.GetRequiredService<SettingsViewModel>();
                    targetViewModel = _settingsViewModel;
                    break;
                default:
                    _measurementViewModel ??= _serviceProvider.GetRequiredService<MeasurementViewModel>();
                    targetViewModel = _measurementViewModel;
                    break;
            }
            if (targetViewModel != null && targetViewModel != _currentPageViewModel)
            {
                CurrentPageViewModel = targetViewModel;
                CurrentPageIdentifier = pageName;
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