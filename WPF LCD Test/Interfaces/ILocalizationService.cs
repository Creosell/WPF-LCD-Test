using System.Globalization;

namespace WPF_LCD_Test.Interfaces
{
    public interface ILocalizationService
    {
        // Событие, которое будет срабатывать при смене языка
        event EventHandler LanguageChanged;

        // Свойство для получения текущей культуры (языка) приложения
        CultureInfo CurrentCulture { get; }

        // Метод для установки нового языка по его коду (например, "en", "zh", "ru")
        void SetLanguage(string cultureCode);

        // Метод для получения локализованной строки по ключу
        string GetString(string key);

        // Метод для получения локализованной строки с форматированием
        string GetString(string key, params object[] args);

        // Событие для передачи сообщений об ошибках и статусах
        event EventHandler<string> StatusMessage;
    }
}