using System.Globalization;

namespace WPF_LCD_Test.Interfaces
{
    public interface ILocalizationService
    {
        event EventHandler LanguageChanged;
        CultureInfo CurrentCulture { get; }
        void SetLanguage(string cultureCode);
        string GetString(string key);
        string GetString(string key, params object[] args);
        event EventHandler<string> StatusMessage;
    }
}