// В новом файле ViewModels
// Файл MainWindowViewModel.cs

// Оставьте только необходимые using директивы для оболочки
using System;
using System.Windows.Input; // Для ICommand
using WPF_LCD_Test.Services; // Для сервисов, которые нужны в оболочке или для создания других ViewModel
using WPF_LCD_Test.Models;
using MvvmHelpers;
using WPF_LCD_Test.Commands; // Если DeviceUnderTest все еще управляется тут напрямую
// using System.Collections.ObjectModel; // Удалите, если логи лога и статусы точек перенесены

// using static WPF_LCD_Test.Resources.Resources; // Удалите, если прямой доступ к ресурсам не используется в этом VM
// using System.Globalization; // Удалите, если не используется

// Убедитесь, что ваш базовый класс ViewModel страниц доступен
// using WPF_LCD_Test.ViewModels;

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

        // Геттеры и сеттеры для текущей страницы
        public string CurrentPageIdentifier
        {
            get => _currentPageIdentifier;
            private set => SetProperty(ref _currentPageIdentifier, value); // Используем SetProperty для уведомления UI
        }


        public BaseViewModel CurrentPageViewModel // Используйте PageViewModelBase, если он создан
        // public object CurrentPageViewModel // Или используйте object
        {
            get => _currentPageViewModel;
            set
            {
                // Опционально: Вызвать метод при уходе со страницы (OnNavigatedFrom), если он есть в PageViewModelBase
                // (_currentPageViewModel as PageViewModelBase)?.OnNavigatedFrom();

                // !!! Важно: очищаем старый ViewModel страницы, если он реализует IDisposable !!!
                (_currentPageViewModel as IDisposable)?.Dispose();

                // Устанавливаем новый ViewModel страницы
                SetProperty(ref _currentPageViewModel, value);

                // Опционально: Вызвать метод при переходе на новую страницу (OnNavigatedTo), если он есть в PageViewModelBase
                // (value as PageViewModelBase)?.OnNavigatedTo();
            }
        }

        // --- КОМАНДЫ НАВИГАЦИИ: Команда для кнопок в боковом меню !!! ---
        // Эта команда будет принимать параметр (например, строку с именем страницы).
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
            _fileService = fileService ?? throw new ArgumentNullException(nameof(_fileService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(_dialogService)); // Сохраняем для общеприложениевых диалогов
            _localizationService = localizationService ?? throw new ArgumentNullException(nameof(_localizationService));
            _settingService = settingsService ?? throw new ArgumentNullException(nameof(settingsService)); // Сохраняем для доступа к настройкам


            // Инициализация команд оболочки
            // Команда NavigateCommand принимает параметр (string pageName)
            NavigateCommand = new RelayCommand(ExecuteNavigate, CanExecuteNavigate);


            // !!! НАЧАЛЬНАЯ НАВИГАЦИЯ: Устанавливаем страницу, которая будет показана при запуске !!!
            // При запуске приложения автоматически переходим на страницу измерений.
            ExecuteNavigate("Measurement");


            // !!! ОБРАБОТЧИК СМЕНЫ ЯЗЫКА: Оставляем, если он нужен для ОБНОВЛЕНИЯ ЛОКАЛИЗУЕМЫХ СВОЙСТВ В ЭТОМ MAINWINDOWVIEWMODEL !!!
            // Если в MainWindowViewModel больше нет локализуемых свойств (например, заголовка окна),
            // которые должны меняться при смене языка, этот обработчик может быть пустым или удален.
            // ViewModel каждой "страницы" (например, MeasurementWindowViewModel) должен сам подписаться на LanguageChanged,
            // если он содержит локализуемые свойства, которые должны обновляться.
            _localizationService.LanguageChanged += LocalizationService_LanguageChanged;

            // Обновление локализуемых свойств оболочки (если они есть) при старте
            // UpdateLocalizedViewModelStrings(); // Если MainWindowViewModel имеет локализуемые свойства


            // Остальные инициализации, специфичные для оболочки приложения.
            // Например, создание DeviceUnderTest, если его жизненный цикл управляется здесь и передается в ViewModel страниц.
            // Или DeviceUnderTest создается внутри ViewModel страницы (например, MeasurementWindowViewModel).
        }


        // --- МЕТОДЫ ВЫПОЛНЕНИЯ КОМАНД ОБОЛОЧКИ (Execute... и CanExecute...) ---

        // Логика проверки возможности выполнения навигации
        private bool CanExecuteNavigate(object parameter)
        {
            // Здесь можно добавить логику, запрещающую навигацию во время определенных операций
            // (например, если MeasurementWindowViewModel занят долгим измерением).
            // Пока просто разрешаем всегда.
            return true;
        }

        // Логика выполнения навигации - Создает ViewModel нужной страницы
        private void ExecuteNavigate(object parameter)
        {
            string? pageName = parameter as string; // Получаем имя страницы из параметра команды
            CurrentPageIdentifier = pageName; // Сохраняем идентификатор текущей страницы (если нужно)

            // Создаем соответствующий ViewModel для выбранной страницы.
            switch (pageName)
            {
                case "Measurement":

                    if (!(CurrentPageViewModel is MeasurementViewModel))
                    {
                        // Перед созданием нового ViewModel, очищаем предыдущий, если он IDisposable
                        (CurrentPageViewModel as IDisposable)?.Dispose();

                        // !!! Здесь создается НОВЫЙ экземпляр MeasurementViewModel !!!
                        CurrentPageViewModel = new MeasurementViewModel(
                            _colorMeasurementService,
                            _fileService,
                            _dialogService,
                            _localizationService // Передаем сервисы
                        );
                    }
                    break;
                case "Settings":
                    if (!(CurrentPageViewModel is SettingsViewModel))
                    {
                        (CurrentPageViewModel as IDisposable)?.Dispose();
                        CurrentPageViewModel = new SettingsViewModel(_settingService, _localizationService); // Создаем экземпляр SettingsViewModel
                    }
                    break;
                // TODO: Добавьте case для других страниц (например, About, Help и т.д.)

                default:
                    if (!(CurrentPageViewModel is MeasurementViewModel))
                    {
                        CurrentPageViewModel = new MeasurementViewModel( // Переходим на MeasurementWindowViewModel по умолчанию
                         _colorMeasurementService, _fileService, _dialogService, _localizationService);
                    }
                    break;

            }
            // Свойство CurrentPageViewModel вызывает SetProperty и OnPropertyChanged (из BaseViewModel),
            // что заставляет ContentControl в MainWindow.xaml обновить свое содержимое,
            // отобразив View (UserControl), соответствующий новому типу ViewModel.
        }

        // !!! ОБРАБОТЧИК СМЕНЫ ЯЗЫКА В ОБОЛОЧКЕ (если нужен) !!!
        // Этот обработчик теперь нужен ТОЛЬКО для обновления локализуемых свойств,
        // которые находятся непосредственно в MainWindowViewModel (например, заголовок окна).
        // Если все локализуемые свойства перенесены в ViewModel страниц,
        // и ViewModel страниц сами подписаны на LanguageChanged, то этот обработчик в оболочке может быть пустым
        // или удален.

        private void LocalizationService_LanguageChanged(object sender, EventArgs e)
        {
            // Если в этом ViewModel (оболочки) есть локализуемые свойства, обновите их здесь.
            // Например:
            // MainWindowTitle = Resources.Resources.AppTitle; // Пример обновления свойства для заголовка окна
            // OnPropertyChanged(nameof(MainWindowTitle));

            // ViewModel активной страницы должен сам обновить свои локализуемые свойства,
            // если он подписан на событие LanguageChanged от LocalizationService.
            // Если ViewModel страниц не подписываются сами, то здесь можно было бы
            // вызвать какой-то метод обновления у текущего CurrentPageViewModel.
            // Например: (CurrentPageViewModel as PageViewModelBase)?.UpdateLocalizedContent(); // Требует метода в PageViewModelBase
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

            // Отписываемся от событий сервисов, на которые подписан ТОЛЬКО MainWindowViewModel.
            if (_localizationService != null)
            {
                _localizationService.LanguageChanged -= LocalizationService_LanguageChanged;
            }
            // TODO: Если MainWindowViewModel подписывался на другие глобальные события, отпишитесь здесь.
            
            Console.WriteLine("MainWindowViewModel Dispose Called.");
        }

        // TODO: Возможно, вам понадобится публичное свойство в MainWindowViewModel для привязки Title окна,
        // если заголовок должен меняться динамически при смене языка.
        // private string _mainWindowTitle;
        // public string MainWindowTitle
        // {
        //     get => _mainWindowTitle;
        //     set => SetProperty(ref _mainWindowTitle, value);
        // }
        // И обновлять его в конструкторе и LocalizationService_LanguageChanged.
    }
}