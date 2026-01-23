using System.Globalization;

namespace WPF_LCD_Test.Interfaces
    {
    /// <summary>
    /// Service interface for managing application localization and retrieving localized strings.
    /// </summary>
    public interface ILocalizationService
        {
        /// <summary>
        /// Occurs when application language changes.
        /// </summary>
        event EventHandler LanguageChanged;

        /// <summary>
        /// Occurs when status message needs to be reported.
        /// </summary>
        event EventHandler<string> StatusMessage;

        /// <summary>
        /// Gets the current application culture.
        /// </summary>
        CultureInfo CurrentCulture { get; }

        /// <summary>
        /// Sets application language by culture code.
        /// </summary>
        /// <param name="cultureCode">Culture code (e.g., "en", "zh-Hans"). Empty string for default culture.</param>
        void SetLanguage(string cultureCode);

        /// <summary>
        /// Retrieves localized string for specified resource key.
        /// </summary>
        /// <param name="key">Resource key identifier.</param>
        /// <returns>Localized string or placeholder if key not found.</returns>
        string GetString(string key);

        /// <summary>
        /// Retrieves localized string with formatting arguments.
        /// </summary>
        /// <param name="key">Resource key identifier.</param>
        /// <param name="args">Format arguments to insert into localized string.</param>
        /// <returns>Formatted localized string or placeholder if formatting fails.</returns>
        string GetString(string key, params object[] args);
        }
    }