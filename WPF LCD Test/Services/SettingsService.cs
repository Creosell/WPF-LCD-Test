using System.Diagnostics;
using System.IO;
using System.Text.Json;
using WPF_LCD_Test.Interfaces;
using WPF_LCD_Test.Models;

namespace WPF_LCD_Test.Services
    {
    /// <summary>
    /// Service for loading and saving application settings to JSON file.
    /// </summary>
    public class SettingsService : ISettingsService
        {
        private readonly IPathProvider _pathProvider;
        private string _currentSettingsFilePath;
        private const string _settingsFileName = "appsettings.json";

        private static readonly JsonSerializerOptions _loadJsonSerializerOptions = new()
            {
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            PropertyNameCaseInsensitive = true
            };

        private static readonly JsonSerializerOptions _saveJsonSerializerOptions = new()
            {
            WriteIndented = true
            };

        /// <summary>
        /// Gets the current application settings.
        /// </summary>
        public AppSettings CurrentSettings => LoadSettings();

        /// <summary>
        /// Initializes a new instance of SettingsService.
        /// </summary>
        /// <param name="pathProvider">Path provider for resolving file paths.</param>
        public SettingsService(IPathProvider pathProvider)
            {
            _pathProvider = pathProvider;
            _currentSettingsFilePath = Path.Combine(_pathProvider.BaseDirectory, _settingsFileName);
            LoadSettings();
            }

        /// <summary>
        /// Sets custom file path for testing purposes.
        /// </summary>
        /// <param name="testFilePath">Path to test settings file.</param>
        public void SetTestFilePath(string testFilePath)
            {
            _currentSettingsFilePath = testFilePath;
            }

        /// <summary>
        /// Loads application settings from JSON file. Creates file with defaults if not exists.
        /// </summary>
        /// <returns>AppSettings object with loaded or default values.</returns>
        public AppSettings LoadSettings()
            {
            AppSettings settings = new();

            if (!File.Exists(_currentSettingsFilePath))
                {
                Debug.WriteLine("Settings file not found. Creating with defaults.");

                try
                    {
                    SaveSettings(settings);
                    }
                catch (Exception ex)
                    {
                    Debug.WriteLine($"Failed to create settings file: {ex.Message}");
                    }

                return settings;
                }

            try
                {
                string jsonString = File.ReadAllText(_currentSettingsFilePath);
                var loadedSettings = JsonSerializer.Deserialize<AppSettings>(jsonString, _loadJsonSerializerOptions);

                if (loadedSettings != null)
                    {
                    settings = loadedSettings;
                    }

                Debug.WriteLine("Settings loaded successfully.");
                }
            catch (Exception ex)
                {
                Debug.WriteLine($"Failed to load settings: {ex.Message}");
                }

            return settings;
            }

        /// <summary>
        /// Saves application settings to JSON file.
        /// </summary>
        /// <param name="settings">Settings object to save.</param>
        public void SaveSettings(AppSettings settings)
            {
            if (settings == null)
                {
                Debug.WriteLine("Cannot save null settings.");
                return;
                }

            try
                {
                string jsonString = JsonSerializer.Serialize(settings, _saveJsonSerializerOptions);
                File.WriteAllText(_currentSettingsFilePath, jsonString);
                Debug.WriteLine("Settings saved successfully.");
                }
            catch (Exception ex)
                {
                Debug.WriteLine($"Failed to save settings: {ex.Message}");
                }
            }

        /// <summary>
        /// Updates the color analyzer channel in settings and persists changes.
        /// </summary>
        /// <param name="channel">Channel number to save.</param>
        public void UpdateChannel(int channel)
            {
            var settings = LoadSettings();
            settings.ColorAnalyzerChannel = channel.ToString();
            SaveSettings(settings);
            }
        }
    }