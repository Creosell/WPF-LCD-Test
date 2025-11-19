// В файле App.xaml.cs

using System.Diagnostics;
using System.Windows;
using WPF_LCD_Test.Interfaces;
using WPF_LCD_Test.Models;
using WPF_LCD_Test.Services;
using WPF_LCD_Test.ViewModels;
using WPF_LCD_Test.Views;
using System.ComponentModel;
using System.Globalization;
using System.Threading;
using System.Linq;

namespace WPF_LCD_Test
{
    public partial class App : Application
    {
        // Объявите приватные поля для сервисов, если они нужны на уровне App (например, LocalizationService)

        private IColorMeasurementService colorMeasurementService = new ColorMeasurementService(); // Реализация сервиса прибора
        private IFileService fileService = new FileService(); // Реализация сервиса файлов
        private IDialogService dialogService = new DialogService(); // Реализация сервиса диалогов
        private ISettingsService settingsService = SettingsService.Instance; // Получаем синглтон сервиса настроек (если он синглтон)
        private ILocalizationService localizationService = LocalizationService.Instance; // Получаем синглтон сервиса локализации (если он синглтон)
        private IDispatcher dispatcher = new WpfDispatcher(); // Реализация обертки для Dispatcher (если нужна)
        private IUploadService uploadService = new UploadService(); // Реализация сервиса загрузки отчетов

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // === КОМПОЗИЦИОННЫЙ КОРЕНЬ: Здесь создаются все сервисы и главный ViewModel ===

            // 1. Загружаем настройки
            AppSettings appSettings = settingsService.LoadSettings();

            // 6. Инициализация локализации (после создания сервиса локализации)
            localizationService.LanguageChanged += LocalizationService_LanguageChanged; // Если App сам обрабатывает смену словаря, подписка здесь

            // Устанавливаем язык по умолчанию (это вызовет SetLanguage в сервисе,
            localizationService.SetLanguage(appSettings.LanguageCultureCode);

            // 2. Создаем экземпляр ГЛАВНОГО ViewModel приложения (оболочки)
            MainWindowViewModel mainWindowViewModel = new MainWindowViewModel(
                colorMeasurementService,
                fileService,
                dialogService,
                localizationService,
                settingsService,
                uploadService,
                dispatcher
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


            // 7. Показываем главное окно
            mainWindow.Show();
        }

        public static void CheckCurrentAppLanguage()
        {
            // Устанавливаем эту культуру для текущего потока из пула
            Thread.CurrentThread.CurrentCulture = LocalizationService.Instance.CurrentCulture; ;
            Thread.CurrentThread.CurrentUICulture = LocalizationService.Instance.CurrentCulture; ;
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
            if (cultureCode == "")
            {
                resourcePathToLoad = "/Resources/StringResources.xaml"; // Или просто defaultResourcePath, если базовый - английский
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
                Debug.WriteLine($"App Ошибка: Не удалось загрузить или применить словарь ресурсов '{resourcePathToLoad}'. Ошибка: {ex.Message}");
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