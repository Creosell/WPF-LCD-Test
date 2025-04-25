// В файле App.xaml.cs


using System.Windows;
using WPF_LCD_Test.Services;
using WPF_LCD_Test.Views;
using WPF_LCD_Test.ViewModels;


namespace WPF_LCD_Test
{
    public partial class App : Application
    {
        // Объявите приватные поля для сервисов, если они нужны на уровне App (например, LocalizationService)
        private ILocalizationService _localizationService;
        IColorMeasurementService colorMeasurementService = new ColorMeasurementService(); // Реализация сервиса прибора
        IFileService fileService = new FileService(); // Реализация сервиса файлов
        IDialogService dialogService = new DialogService(); // Реализация сервиса диалогов
        ILocalizationService localizationService = LocalizationService.Instance; // Получаем синглтон сервиса локализации (если он синглтон)


        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // === КОМПОЗИЦИОННЫЙ КОРЕНЬ: Здесь создаются все сервисы и главный ViewModel ===

            // 1. Создаем экземпляры всех сервисов
            

            // 2. Создаем экземпляр ГЛАВНОГО ViewModel приложения (оболочки)
            MainWindowViewModel mainWindowViewModel = new MainWindowViewModel(
                colorMeasurementService,
                fileService,
                dialogService,
                localizationService
            );

            // 3. Создаем экземпляр главного окна (View оболочки)
            MainWindow mainWindow = new MainWindow();

            // 4. Устанавливаем DataContext окна на созданный ViewModel оболочки
            mainWindow.DataContext = mainWindowViewModel;

            // 5. Опционально: Подписываемся на событие закрытия окна, чтобы очистить ГЛАВНЫЙ ViewModel при закрытии приложения
            mainWindow.Closed += (s, args) =>
            {
                // Вызываем Dispose у главного ViewModel при закрытии окна
                (mainWindow.DataContext as IDisposable)?.Dispose();
            };

            // 6. Инициализация локализации (после создания сервиса локализации)
            localizationService.LanguageChanged += LocalizationService_LanguageChanged; // Если App сам обрабатывает смену словаря, подписка здесь

            // Устанавливаем язык по умолчанию (это вызовет SetLanguage в сервисе,
            localizationService.SetLanguage("en"); // Или другой язык по умолчанию

            // 7. Показываем главное окно
            mainWindow.Show();
        }

        // Обработчик события смены языка сервиса локализации
        private void LocalizationService_LanguageChanged(object sender, EventArgs e)
        {
            // Этот код подмены словаря ресурсов
            string defaultResourcePath = "/Resources/StringResources.xaml"; // Базовый словарь по умолчанию

            ILocalizationService localizationService = LocalizationService.Instance;
            string cultureCode = localizationService.CurrentCulture.Name;

            string resourcePathToLoad = defaultResourcePath;

            // Логика выбора пути к словарю в зависимости от cultureCode
            if (cultureCode == "en")
            {
                resourcePathToLoad = "/Resources/StringResources.en.xaml"; // Или просто defaultResourcePath, если базовый - английский
            }
            else if (cultureCode == "zh-Hans")
            {
                resourcePathToLoad = "/Resources/StringResources.zh-Hans.xaml";
            }
            // ... другие языки ...

            try
            {
                // Логика удаления старого словаря и добавления нового
                // Ищем словари, которые *не* Material Design и *не* базовый (defaultResourcePath)
                var oldDictionaries = Application.Current.Resources.MergedDictionaries
                    .Where(d => d.Source != null &&
                                 !d.Source.OriginalString.Contains("MaterialDesignThemes.Wpf") &&
                                 !d.Source.OriginalString.Equals(new Uri(defaultResourcePath, UriKind.RelativeOrAbsolute).OriginalString, StringComparison.OrdinalIgnoreCase)) // Сравниваем URI
                    .ToList();

                foreach (var oldDict in oldDictionaries)
                {
                    Application.Current.Resources.MergedDictionaries.Remove(oldDict);
                }

                ResourceDictionary newLanguageDictionary = new ResourceDictionary() { Source = new Uri(resourcePathToLoad, UriKind.RelativeOrAbsolute) };
                Application.Current.Resources.MergedDictionaries.Add(newLanguageDictionary);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"App Ошибка: Не удалось загрузить или применить словарь ресурсов '{resourcePathToLoad}'. Ошибка: {ex.Message}");
            }
        }

         protected override void OnExit(ExitEventArgs e)
        {
            // Очистка сервисов, если они Disposable и создаются здесь
            (colorMeasurementService as IDisposable)?.Dispose();
            (fileService as IDisposable)?.Dispose();
            (dialogService as IDisposable)?.Dispose();
            (localizationService as IDisposable)?.Dispose();
            // ...
            base.OnExit(e);
        }
    }
}