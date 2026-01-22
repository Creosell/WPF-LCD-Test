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
        }
    }