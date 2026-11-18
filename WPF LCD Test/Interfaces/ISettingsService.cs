using WPF_LCD_Test.Models;

namespace WPF_LCD_Test.Interfaces
    {
    /// <summary>
    /// Service interface for managing application settings persistence.
    /// </summary>
    public interface ISettingsService
        {
        /// <summary>
        /// Gets the current application settings.
        /// </summary>
        AppSettings CurrentSettings { get; }

        /// <summary>
        /// Loads application settings from storage.
        /// </summary>
        /// <returns>Loaded settings or defaults if file doesn't exist.</returns>
        AppSettings LoadSettings();

        /// <summary>
        /// Saves application settings to storage.
        /// </summary>
        /// <param name="settings">Settings object to persist.</param>
        void SaveSettings(AppSettings settings);

        /// <summary>
        /// Updates the color analyzer channel in settings.
        /// </summary>
        /// <param name="channel">Channel number to save.</param>
        void UpdateChannel(int channel);

        /// <summary>
        /// Updates only the application language in settings and persists changes.
        /// </summary>
        /// <param name="cultureCode">Language culture code to save.</param>
        void UpdateLanguage(string cultureCode);

        /// <summary>
        /// Writes only the theme mode to the settings file.
        /// </summary>
        /// <param name="mode">The chosen theme mode.</param>
        void UpdateTheme(ThemeMode mode);
        }
    }