using System.Globalization;
using WPF_LCD_Test.Interfaces;
using static WPF_LCD_Test.Resources.Resources;

namespace WPF_LCD_Test.Services
{
    public class LocalizationService : ILocalizationService
    {
        public event EventHandler<string> StatusMessage;
        private static Lazy<ILocalizationService> _lazyInstance = new(() => new LocalizationService());
        private CultureInfo _applicationCulture;
        private LocalizationService()
        {
            SetLanguage("");
        }
        public static ILocalizationService Instance => _lazyInstance.Value;
        public event EventHandler LanguageChanged;
        public CultureInfo CurrentCulture => _applicationCulture;

        public void SetLanguage(string cultureCode)
        {
            try
            {
                CultureInfo culture = new CultureInfo(cultureCode);
                Thread.CurrentThread.CurrentCulture = culture;
                Thread.CurrentThread.CurrentUICulture = culture;
                _applicationCulture = culture;
                OnLanguageChanged();
            }
            catch (CultureNotFoundException ex)
            {
                StatusMessage?.Invoke(this, $"{CultureNotFoundErr}: {ex.Message}");
            }
            catch (Exception ex)
            {
                StatusMessage?.Invoke(this, $"{ErrUnexpected}: {ex.Message}");
            }
        }

        public string GetString(string key)
        {
            if (ResourceManager == null)
            {
                StatusMessage?.Invoke(this, $"{ErrUnexpected}:  null!");
                return $"!{key}!";
            }

            string result = ResourceManager.GetString(key, _applicationCulture);
            if (result == null)
            {
                StatusMessage?.Invoke(this, $"{LocalizationServiceResErr} {_applicationCulture?.Name} : {key}");
                return $"!{key}!";
            }
            return result;
        }

        public string GetString(string key, params object[] args)
        {
            string format = GetString(key);
            if (string.IsNullOrEmpty(format) || (format.StartsWith("!{") && format.EndsWith("}!")))
            {
                return format;
            }
            try
            {
                return string.Format(_applicationCulture, format, args);
            }
            catch (FormatException ex)
            {
                StatusMessage?.Invoke(this, $"{LocalizationServiceFormatErr} {key} {Err}: {ex.Message}");
                return format;
            }
        }

        protected virtual void OnLanguageChanged()
        {
            LanguageChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}