// В папке Models
// Файл AppSettings.cs

using System.Text.Json.Serialization; // Для атрибута JsonPropertyName

namespace WPF_LCD_Test.Models // Или WPF_LCD_Test.Settings
{
    /// <summary>
    /// Модель, представляющая настройки приложения для сериализации/десериализации.
    /// </summary>
    public class AppSettings
    {
        /// <summary>

        [JsonPropertyName("languageCultureCode")]
        public string LanguageCultureCode { get; set; } = "";

        [JsonPropertyName("measurementTime")]
        public int MeasurementTime { get; set; } = 2;

        [JsonPropertyName("autoConnectEnabled")]
        public bool AutoConnectEnabled { get; set; } = true;
    }
}