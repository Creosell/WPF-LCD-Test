// В файле App.xaml.cs

using System;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Markup; // Для XmlLanguage
using System.Collections.Generic; // Для List
using System.Linq; // Для Linq
using WPF_LCD_Test.Services; // Добавь using для твоего сервиса

namespace WPF_LCD_Test
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        // Удали переопределение protected override void OnStartup(...)

        // Добавь обработчик события Startup приложения
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // === Эта логика перенесена из старого OnStartup ===

            // 1. Получаем экземпляр сервиса локализации (первое обращение, создает синглтон)
            ILocalizationService localizationService = LocalizationService.Instance;

            // 2. Подписываемся на событие смены языка сервиса локализации
            localizationService.LanguageChanged += LocalizationService_LanguageChanged;

            // 3. Устанавливаем язык по умолчанию (это вызовет SetLanguage в сервисе,
            //    который вызовет OnLanguageChanged, который вызовет наш обработчик)
            localizationService.SetLanguage("en"); // Или другой язык по умолчанию

            // 4. Явно создаем и показываем главное окно, т.к. убрали StartupUri
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();

            // === Конец перенесенной логики ===
    }

        // Оставь этот метод обработчика события как есть (он теперь будет вызываться)
        private void LocalizationService_LanguageChanged(object sender, EventArgs e)
        {

            string defaultResourcePath = "/Resources/StringResources.xaml";
            // Получаем текущую культуру из сервиса (это новая выбранная культура)
            ILocalizationService localizationService = LocalizationService.Instance; // Можно использовать _instance напрямую в сервисе, но Instance тоже работает
            string cultureCode = localizationService.CurrentCulture.Name; // Например, "en" или "zh-Hans"

            // Формируем URI к файлу словаря ресурсов для данного языка
            // Убедись, что имена файлов ресурсов соответствуют культурам
            string resourceFileName = $"StringResources.{cultureCode}.xaml";
            string resourcePath = $"/Resources/{resourceFileName}";

            // Если нужен английский (или язык по умолчанию), используем базовый файл без кода культуры
            if (cultureCode == "en") // Замени "en" на твой язык по умолчанию, если он другой
            {
                resourcePath = defaultResourcePath; // "/Resources/StringResources.xaml"
                                                    // Дополнительная проверка для китайского упрощенного, если файл назван по-другому
            }
            else if (cultureCode == "zh-Hans") // Пример для китайского упрощенного, если твой файл назван "StringResources.zh-Hans.xaml"
            {
                resourcePath = $"/Resources/StringResources.zh-Hans.xaml";
}
            // ... добавь else if для других языков

            try
            {
                // Находим старый словарь языка и удаляем его (если он есть)
                // Ищем словари, которые не являются Material Design или базовым (defaultResourcePath)
                var oldDictionaries = Application.Current.Resources.MergedDictionaries
                    .Where(d => d.Source != null &&
                                !d.Source.OriginalString.Contains("MaterialDesignThemes.Wpf") &&
                                !d.Source.OriginalString.EndsWith(defaultResourcePath, StringComparison.OrdinalIgnoreCase)) // Исключаем базовый словарь
                    .ToList(); // Копируем в список, чтобы можно было удалять из оригинальной коллекции

                foreach (var oldDict in oldDictionaries)
                {
                    Application.Current.Resources.MergedDictionaries.Remove(oldDict);
                }

                // Создаем и загружаем новый словарь ресурсов
                // Используем UriKind.Relative для ресурсов в сборке приложения
                ResourceDictionary newLanguageDictionary = new ResourceDictionary() { Source = new Uri(resourcePath, UriKind.RelativeOrAbsolute) }; // UriKind.Relative обычно достаточно

                // Добавляем новый словарь в MergedDictionaries приложения
                Application.Current.Resources.MergedDictionaries.Add(newLanguageDictionary);


                // Опционально: Обновить привязки, если они не обновляются автоматически.
                // Обычно DynamicResource должен автоматически обновиться после изменения MergedDictionaries,
                // но если есть проблемы, может потребоваться принудительное обновление,
                // например, через событие LanguageChanged в ViewModel и OnPropertyChanged.

            }
            catch (Exception ex)
            {
                Console.WriteLine($"App Ошибка: Не удалось загрузить или применить словарь ресурсов '{resourcePath}'. Ошибка: {ex.Message}"); // Для отладки
                // Возможно, стоит использовать CultureInfo.InvariantCulture или другой fallback язык/словарь в случае ошибки
            }

            // Уведомляем ViewModel (если он подписан и нужно обновить свойства)
            // ViewModel уже подписан в MainWindowViewModel.cs, поэтому это сработает
            // Console.WriteLine("App: Событие LanguageChanged завершено."); // Для отладки
        }
        // ... другие методы App.xaml.cs ...
    }
}