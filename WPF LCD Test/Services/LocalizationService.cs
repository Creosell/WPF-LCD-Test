using System.Globalization;
using WPF_LCD_Test.Interfaces;
using static WPF_LCD_Test.Resources.Resources;

namespace WPF_LCD_Test.Services
    {
    /// <summary>
    /// Service for managing application localization and culture settings. Implemented as Singleton.
    /// </summary>
    public class LocalizationService : ILocalizationService
        {
        private static readonly Lazy<ILocalizationService> _lazyInstance = new(() => new LocalizationService());
        private CultureInfo _applicationCulture;

        /// <summary>
        /// Occurs when localization status message needs to be reported.
        /// </summary>
        public event EventHandler<string> StatusMessage;

        /// <summary>
        /// Occurs when application language changes.
        /// </summary>
        public event EventHandler LanguageChanged;

        /// <summary>
        /// Gets the singleton instance of LocalizationService.
        /// </summary>
        public static ILocalizationService Instance => _lazyInstance.Value;

        /// <summary>
        /// Gets the current application culture.
        /// </summary>
        public CultureInfo CurrentCulture => _applicationCulture;

        private LocalizationService()
            {
            SetLanguage("");
            }

        /// <summary>
        /// Sets application language and culture for current and all new threads.
        /// </summary>
        /// <param name="cultureCode">Culture code (e.g., "en", "zh-Hans"). Empty string for default culture.</param>
        public void SetLanguage(string cultureCode)
            {
            try
                {
                CultureInfo culture = new CultureInfo(cultureCode);

                Thread.CurrentThread.CurrentCulture = culture;
                Thread.CurrentThread.CurrentUICulture = culture;

                CultureInfo.DefaultThreadCurrentCulture = culture;
                CultureInfo.DefaultThreadCurrentUICulture = culture;

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

        /// <summary>
        /// Retrieves localized string for specified resource key.
        /// </summary>
        /// <param name="key">Resource key identifier.</param>
        /// <returns>Localized string or placeholder if key not found.</returns>
        public string GetString(string key)
            {
            if (ResourceManager == null)
                {
                StatusMessage?.Invoke(this, $"{ErrUnexpected}: ResourceManager is null!");
                return $"!{key}!";
                }

            string result = ResourceManager.GetString(key, _applicationCulture);
            if (result == null)
                {
                StatusMessage?.Invoke(this, $"{LocalizationServiceResErr} {_applicationCulture?.Name}: {key}");
                return $"!{key}!";
                }
            return result;
            }

        /// <summary>
        /// Retrieves localized string with formatting arguments.
        /// </summary>
        /// <param name="key">Resource key identifier.</param>
        /// <param name="args">Format arguments to insert into localized string.</param>
        /// <returns>Formatted localized string or placeholder if formatting fails.</returns>
        public string GetString(string key, params object[] args)
            {
            string format = GetString(key);
            if (string.IsNullOrEmpty(format) || ( format.StartsWith("!{") && format.EndsWith("}!") ))
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

        /// <summary>
        /// Raises the LanguageChanged event.
        /// </summary>
        protected virtual void OnLanguageChanged()
            {
            LanguageChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }