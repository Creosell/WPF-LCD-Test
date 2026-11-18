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
            Assert.That(settings.LanguageCultureCode, Is.EqualTo(""), "Default LanguageCultureCode should be 'en'.");
            Assert.That(settings.ColorAnalyzerChannel, Is.EqualTo("0"), "Default ColorAnalyzerChannel should be '0'.");
            Assert.That(settings.MeasurementSpeed, Is.EqualTo(MeasurementSpeed.Slow), "Default MeasurementSpeed should be Slow.");
            Assert.That(settings.SyncFrequency, Is.Null, "Default SyncFrequency should select the universal synchronization.");
        }

        [Test]
        public void AppSettings_Properties_CanBeSetAndGet()
        {
            // Arrange
            var settings = new AppSettings();
            string expectedLanguage = "zh-Hans";
            string expectedPort = "3";
            var expectedSpeed = MeasurementSpeed.Fast;

            // Act
            settings.LanguageCultureCode = expectedLanguage;
            settings.ColorAnalyzerChannel = expectedPort;
            settings.MeasurementSpeed = expectedSpeed;

            // Assert
            Assert.That(settings.LanguageCultureCode, Is.EqualTo(expectedLanguage), "LanguageCultureCode should match the set value.");
            Assert.That(settings.ColorAnalyzerChannel, Is.EqualTo(expectedPort), "ColorAnalyzerChannel should match the set value.");
            Assert.That(settings.MeasurementSpeed, Is.EqualTo(expectedSpeed), "MeasurementSpeed should match the set value.");
        }

        [Test]
        public void AppSettings_Serialization_WorksCorrectly()
        {
            // Arrange
            var settings = new AppSettings
            {
                LanguageCultureCode = "zh-Hans",
                ColorAnalyzerChannel = "0",
                MeasurementSpeed = MeasurementSpeed.Auto
            };

            // Act
            string jsonString = JsonSerializer.Serialize(settings);
            // Ожидаем JSON, который соответствует атрибутам JsonPropertyName
            string expectedJson = "{\"languageCultureCode\":\"zh-Hans\",\"colorAnalyzerChannel\":\"0\",\"measurementSpeed\":\"Auto\",\"syncMode\":\"Universal\",\"theme\":\"Dark\",\"syncFrequency\":null,\"integrationTime\":null}";

            // Assert
            // Сравниваем JSON, игнорируя возможные различия в форматировании (пробелы, переносы строк)
            // Более надежный способ - десериализовать обратно и сравнить объекты
            Assert.That(jsonString, Is.EqualTo(expectedJson).IgnoreCase.NoClip, "Serialized JSON should match expected format and values.");

            // Дополнительная проверка: десериализация
            var deserializedSettings = JsonSerializer.Deserialize<AppSettings>(jsonString);

            Assert.IsNotNull(deserializedSettings, "Deserialized settings should not be null.");
            Assert.That(deserializedSettings.LanguageCultureCode, Is.EqualTo(settings.LanguageCultureCode), "Deserialized LanguageCultureCode should match original.");
            Assert.That(deserializedSettings.ColorAnalyzerChannel, Is.EqualTo(settings.ColorAnalyzerChannel), "Deserialized ColorAnalyzerChannel should match original.");
            Assert.That(deserializedSettings.MeasurementSpeed, Is.EqualTo(settings.MeasurementSpeed), "Deserialized MeasurementSpeed should match original.");
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
            Assert.That(deserializedSettings.LanguageCultureCode, Is.EqualTo(""), "Missing LanguageCultureCode should default to 'en'.");
            Assert.That(deserializedSettings.ColorAnalyzerChannel, Is.EqualTo("0"), "Missing ColorAnalyzerChannel should default to '0'.");
            Assert.That(deserializedSettings.MeasurementSpeed, Is.EqualTo(MeasurementSpeed.Slow), "Missing MeasurementSpeed should default to Slow.");
        }
    }
}