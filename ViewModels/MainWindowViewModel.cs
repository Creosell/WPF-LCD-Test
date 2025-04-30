// В новом файле ViewModels
// Файл MainWindowViewModel.cs

using System;
using System.Windows.Input;
using WPF_LCD_Test.Services;

using WPF_LCD_Test.Models;
using MvvmHelpers;
using WPF_LCD_Test.Commands;
using System.Diagnostics; 


namespace WPF_LCD_Test.ViewModels
{
    // ViewModel для главного окна (оболочки приложения)
    // Он будет управлять навигацией между ViewModel "страниц"
    public class MainWindowViewModel : BaseViewModel, IDisposable // Оставляем IDisposable для очистки
    {
        // --- ЗАВИСИМОСТИ: Оставьте только те сервисы, которые нужны в оболочке или для создания других ViewModel !!! ---
        // Сервисы, которые будут переданы в конструкторы ViewModel страниц.
        // MainWindowViewModel действует как "фабрика" или композиционный корень для ViewModel страниц.
        private readonly IColorMeasurementService _colorMeasurementService;
        private readonly IFileService _fileService;
        private readonly IDialogService _dialogService; // Возможно, нужен для общеприложениевых диалогов
        private readonly ILocalizationService _localizationService; // Нужен для смены языка и подписки
        private readonly ISettingsService _settingService;
        private BaseViewModel _currentPageViewModel;
        private string _currentPageIdentifier;
        private MeasurementViewModel? _measurementViewModel; // Используем Nullable Reference Types ?
        private SettingsViewModel? _settingsViewModel;

        // Геттеры и сеттеры для текущей страницы
        public string CurrentPageIdentifier
        {
            get => _currentPageIdentifier;
            private set => SetProperty(ref _currentPageIdentifier, value); // Используем SetProperty для уведомления UI
        }


        public BaseViewModel CurrentPageViewModel
        {
            get => _currentPageViewModel;
            set
            {
                // Устанавливаем новый ViewModel страницы (из хранимых экземпляров)
                SetProperty(ref _currentPageViewModel, value);
            }
        }


        public ICommand NavigateCommand { get; }


        // --- КОНСТРУКТОР ---
        // В конструкторе инициализируем сервисы и команды оболочки.
        // Изначально устанавливаем первую страницу.
        public MainWindowViewModel(
            IColorMeasurementService colorMeasurementService,
            IFileService fileService,
            IDialogService dialogService,
            ILocalizationService localizationService,
            ISettingsService settingsService) : base()
        {
            // Инициализация зависимостей
            _colorMeasurementService = colorMeasurementService ?? throw new ArgumentNullException(nameof(colorMeasurementService));
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService)); // Сохраняем для общеприложениевых диалогов
            _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
            _settingService = settingsService ?? throw new ArgumentNullException(nameof(settingsService)); // Сохраняем для доступа к настройкам

            // Инициализация команд оболочки
            // Команда NavigateCommand принимает параметр (string pageName)
            NavigateCommand = new RelayCommand(ExecuteNavigate, CanExecuteNavigate);

            // При запуске приложения автоматически переходим на страницу измерений.
            ExecuteNavigate("Measurement");

        }


        // --- МЕТОДЫ ВЫПОЛНЕНИЯ КОМАНД ОБОЛОЧКИ (Execute... и CanExecute...) ---

        // Логика проверки возможности выполнения навигации
        private bool CanExecuteNavigate(object parameter)
        {
            return true;
        }

        // Логика выполнения навигации - Создает ViewModel нужной страницы
        private void ExecuteNavigate(object parameter)
        {
            string? pageName = parameter as string;

            if (string.IsNullOrEmpty(pageName)) return;

            // CurrentPageIdentifier = pageName; // Если используется для подсветки

            // --- Логика использования ХРАНИМЫХ экземпляров ViewModel !!! ---
            BaseViewModel? targetViewModel = null; // Используем Nullable Reference Types


            switch (pageName)
            {

                case "Measurement":

                    _measurementViewModel ??= new MeasurementViewModel(
                            _colorMeasurementService,
                            _fileService,
                            _dialogService,
                            _localizationService
                        );

                    targetViewModel = _measurementViewModel;
                    break;

                case "Settings":

                    _settingsViewModel ??= new SettingsViewModel(_settingService, _localizationService);

                    targetViewModel = _settingsViewModel;
                    break;



                default:
                    _measurementViewModel ??= new MeasurementViewModel(
                            _colorMeasurementService,
                            _fileService,
                            _dialogService,
                            _localizationService
                        );
                    break;
            }

            // !!! Устанавливаем CurrentPageViewModel в найденный или созданный экземпляр !!!
            // Сеттер CurrentPageViewModel больше НЕ вызывает Dispose().
            if (targetViewModel != null && targetViewModel != _currentPageViewModel) // Проверяем, что есть что установить и это не текущий ViewModel
            {
                CurrentPageViewModel = targetViewModel;
            }
        }

        // --- IDisposable ---
        // Важно: при уничтожении MainWindowViewModel (например, при закрытии окна),
        // нужно очистить текущий ViewModel страницы, если он реализует IDisposable.
        // Также отписаться от событий сервисов, на которые подписан ТОЛЬКО MainWindowViewModel.
        public void Dispose()
        {
            // Очищаем текущий ViewModel страницы, если он Disposable.
            // Это важно, чтобы ViewModel страницы мог отписаться от событий сервисов и освободить ресурсы.
            (CurrentPageViewModel as IDisposable)?.Dispose();

            // TODO: Если MainWindowViewModel подписывался на другие глобальные события, отпишитесь здесь.
            GC.SuppressFinalize(this); // Вызываем сборщик мусора, если нужно
        }
    }
}