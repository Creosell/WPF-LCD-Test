// В проекте WPF_LCD_Test.UnitTests (создайте новую папку, например, ModelsTests)
// Файл AppSettingsTests.cs

using System.Text.Json; // Для тестов сериализации
using WPF_LCD_Test.Models; // Используем пространство имен вашей модели

namespace WPF_LCD_Test.UnitTests.ModelsTests
{
    [TestFixture] // Атрибут NUnit, указывающий, что это класс с тестами
    public class AppSettingsTests
    {
        [Test] // Атрибут NUnit, помечающий метод как тест
        public void AppSettings_DefaultValues_AreCorrect()
        {
            // Arrange (Подготовка)
            var settings = new AppSettings();

            // Act (Действие) - здесь нет явных действий, просто проверка состояния объекта

            // Assert (Проверка)
            Assert.That(settings.LanguageCultureCode, Is.EqualTo(""), "Default LanguageCultureCode should be ''.");
            Assert.That(settings.MeasurementTime, Is.EqualTo(2), "Default Measurement time '2'.");
            Assert.IsTrue(settings.AutoConnectEnabled, "Default AutoConnectEnabled should be true.");
        }

        [Test]
        public void AppSettings_Properties_CanBeSetAndGet()
        {
            // Arrange
            var settings = new AppSettings();
            string expectedLanguage = "zh-Hans";
            int expectedMeasurementTime = 3;
            bool expectedAutoConnect = false;

            // Act
            settings.LanguageCultureCode = expectedLanguage;
            settings.MeasurementTime = expectedMeasurementTime;
            settings.AutoConnectEnabled = expectedAutoConnect;

            // Assert
            Assert.That(settings.LanguageCultureCode, Is.EqualTo(expectedLanguage), "LanguageCultureCode should match the set value.");
            Assert.That(settings.MeasurementTime, Is.EqualTo(expectedMeasurementTime), "Measurement time should match the set value.");
            Assert.That(settings.AutoConnectEnabled, Is.EqualTo(expectedAutoConnect), "AutoConnectEnabled should match the set value.");
        }

        [Test]
        public void AppSettings_Serialization_WorksCorrectly()
        {
            // Arrange
            var settings = new AppSettings
            {
                LanguageCultureCode = "zh-Hans",
                MeasurementTime = 3,
                AutoConnectEnabled = false
            };

            // Act
            string jsonString = JsonSerializer.Serialize(settings);
            // Ожидаем JSON, который соответствует атрибутам JsonPropertyName
            string expectedJson = "{\"languageCultureCode\":\"zh-Hans\",\"measurementTime\":3,\"autoConnectEnabled\":false}";

            // Assert
            // Сравниваем JSON, игнорируя возможные различия в форматировании (пробелы, переносы строк)
            // Более надежный способ - десериализовать обратно и сравнить объекты
            Assert.That(jsonString, Is.EqualTo(expectedJson).IgnoreCase.NoClip, "Serialized JSON should match expected format and values.");

            // Дополнительная проверка: десериализация
            var deserializedSettings = JsonSerializer.Deserialize<AppSettings>(jsonString);

            Assert.IsNotNull(deserializedSettings, "Deserialized settings should not be null.");
            Assert.That(deserializedSettings.LanguageCultureCode, Is.EqualTo(settings.LanguageCultureCode), "Deserialized LanguageCultureCode should match original.");
            Assert.That(deserializedSettings.MeasurementTime, Is.EqualTo(settings.MeasurementTime), "Deserialized DevicePort should match original.");
            Assert.That(deserializedSettings.AutoConnectEnabled, Is.EqualTo(settings.AutoConnectEnabled), "Deserialized AutoConnectEnabled should match original.");
        }

        [Test]
        public void AppSettings_Deserialization_WithMissingProperties_UsesDefaults()
        {
            // Arrange
            // JSON, в котором отсутствуют некоторые свойства
            string jsonWithMissingProps = "{}";

            // Act
            var deserializedSettings = JsonSerializer.Deserialize<AppSettings>(jsonWithMissingProps);

            // Assert
            Assert.IsNotNull(deserializedSettings, "Deserialized settings should not be null.");
            // Проверяем, что свойства, отсутствующие в JSON, приняли значения по умолчанию
            Assert.That(deserializedSettings.LanguageCultureCode, Is.EqualTo(""), "Missing LanguageCultureCode should default to ''.");
            Assert.That(deserializedSettings.MeasurementTime, Is.EqualTo(2), "Missing MeasurementTime should default to '2'.");
            Assert.IsTrue(deserializedSettings.AutoConnectEnabled, "Missing AutoConnectEnabled should default to true.");
        }
    }
}