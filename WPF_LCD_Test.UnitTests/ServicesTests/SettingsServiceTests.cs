// В WPF_LCD_Test.UnitTests/ServicesTests/SettingsServiceTests.cs

using System.Reflection; // Обязательно для рефлексии
using System.Text.Json;
using WPF_LCD_Test.Models; // Убедитесь, что эта ссылка есть
using WPF_LCD_Test.Services;

namespace WPF_LCD_Test.UnitTests.ServicesTests
{
    [TestFixture]
    public class SettingsServiceTests
    {
        private SettingsService _settingsService;
        private string _testTempFilePath; // Путь для временного файла настроек

        // Метод для сброса синглтона SettingsService с использованием рефлексии
        private void ResetSettingsServiceSingleton()
        {
            var lazyInstanceField = typeof(SettingsService)
                .GetField("_lazyInstance", BindingFlags.NonPublic | BindingFlags.Static);

            if (lazyInstanceField == null)
            {
                Assert.Fail("Приватное статическое поле '_lazyInstance' не найдено для SettingsService.");
            }

            var privateCtor = typeof(SettingsService)
                .GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null);

            if (privateCtor == null)
            {
                Assert.Fail("Приватный конструктор без параметров не найден для SettingsService.");
            }

            var newLazyInstance = new Lazy<SettingsService>(() =>
            {
                return (SettingsService)privateCtor.Invoke(null);
            }, LazyThreadSafetyMode.ExecutionAndPublication);

            lazyInstanceField.SetValue(null, newLazyInstance);
        }

        // Метод для установки приватного поля _currentSettingsFilePath в экземпляре SettingsService
        private void SetSettingsServiceFilePath(string filePath)
        {
            var pathField = typeof(SettingsService)
                .GetField("_currentSettingsFilePath", BindingFlags.NonPublic | BindingFlags.Instance);

            if (pathField == null)
            {
                Assert.Fail("Приватное поле '_currentSettingsFilePath' не найдено в SettingsService.");
            }

            pathField.SetValue(_settingsService, filePath);
        }

        [SetUp]
        public void Setup()
        {
            ResetSettingsServiceSingleton();
            _settingsService = SettingsService.Instance;

            _testTempFilePath = Path.Combine(Path.GetTempPath(), $"test_appsettings_{Guid.NewGuid()}.json");
            SetSettingsServiceFilePath(_testTempFilePath);

            if (File.Exists(_testTempFilePath))
            {
                File.Delete(_testTempFilePath);
            }
        }

        [TearDown]
        public void Teardown()
        {
            if (File.Exists(_testTempFilePath))
            {
                File.Delete(_testTempFilePath);
            }
        }

        [Test]
        public void Instance_ReturnsSingletonInstance()
        {
            var instance1 = SettingsService.Instance;
            var instance2 = SettingsService.Instance;

            Assert.That(instance1, Is.SameAs(instance2), "Instance должен возвращать один и тот же объект синглтона.");
            Assert.That(instance1, Is.InstanceOf<SettingsService>(), "Instance должен быть типа SettingsService.");
        }

        [Test]
        public void LoadSettings_FileDoesNotExist_CreatesDefaultSettingsFile()
        {
            var loadedSettings = _settingsService.LoadSettings();

            Assert.That(loadedSettings, Is.Not.Null, "Загруженные настройки не должны быть null.");
            Assert.That(File.Exists(_testTempFilePath), Is.True, "Файл настроек должен быть создан.");

            string jsonContent = File.ReadAllText(_testTempFilePath);
            var savedSettings = JsonSerializer.Deserialize<AppSettings>(jsonContent);

            Assert.That(savedSettings, Is.Not.Null);
            Assert.That(savedSettings.LanguageCultureCode, Is.EqualTo(""), "LanguageCultureCode по умолчанию должен быть 'en'.");
            Assert.That(savedSettings.ColorAnalyzerChannel, Is.EqualTo("COM1"), "DevicePort по умолчанию должен быть 'COM1'.");
            // !!! ВАЖНОЕ ИЗМЕНЕНИЕ ЗДЕСЬ !!!
            Assert.That(savedSettings.AutoConnectEnabled, Is.EqualTo(true), "AutoConnectEnabled по умолчанию должен быть 'true' согласно обновленной модели.");
        }

        [Test]
        public void LoadSettings_FileExists_LoadsSettings()
        {
            // Arrange
            var expectedSettings = new AppSettings
            {
                LanguageCultureCode = "fr",
                ColorAnalyzerChannel = "COM3",
                AutoConnectEnabled = true
            };
            string jsonContent = JsonSerializer.Serialize(expectedSettings, new JsonSerializerOptions { WriteIndented = true });

            File.WriteAllText(_testTempFilePath, jsonContent);

            // Act
            AppSettings loadedSettings = _settingsService.LoadSettings();

            // Assert
            Assert.That(loadedSettings, Is.Not.Null, "Загруженные настройки не должны быть null.");
            Assert.That(loadedSettings.LanguageCultureCode, Is.EqualTo(expectedSettings.LanguageCultureCode), "Загруженный LanguageCultureCode должен совпадать.");
            Assert.That(loadedSettings.ColorAnalyzerChannel, Is.EqualTo(expectedSettings.ColorAnalyzerChannel), "Загруженный DevicePort должен совпадать.");
            Assert.That(loadedSettings.AutoConnectEnabled, Is.EqualTo(expectedSettings.AutoConnectEnabled), "Загруженный AutoConnectEnabled должен совпадать.");
        }

        [Test]
        public void LoadSettings_FileExists_InvalidJson_ReturnsDefaultSettings()
        {
            // Arrange
            string invalidJson = "{ \"languageCultureCode\": \"es\", \"devicePort\": \"COMX\", \"autoConnectEnabled\": "; // Некорректный JSON (обрезан)
            File.WriteAllText(_testTempFilePath, invalidJson);

            // Act
            AppSettings loadedSettings = _settingsService.LoadSettings();

            // Assert
            Assert.That(loadedSettings, Is.Not.Null, "Загруженные настройки не должны быть null.");
            // Проверяем, что вернулись значения по умолчанию из-за ошибки JSON
            Assert.That(loadedSettings.LanguageCultureCode, Is.EqualTo(""), "LanguageCultureCode должен быть 'en' из-за ошибки JSON.");
            Assert.That(loadedSettings.ColorAnalyzerChannel, Is.EqualTo("COM1"), "DevicePort должен быть 'COM1' из-за ошибки JSON.");
            Assert.That(loadedSettings.AutoConnectEnabled, Is.EqualTo(true), "AutoConnectEnabled должен быть 'true' из-за ошибки JSON.");
        }

        [Test]
        public void SaveSettings_SavesProvidedSettingsToFile()
        {
            // Arrange
            var settingsToSave = new AppSettings
            {
                LanguageCultureCode = "de",
                ColorAnalyzerChannel = "COM5",
                AutoConnectEnabled = true
            };

            // Act
            _settingsService.SaveSettings(settingsToSave);

            // Assert
            Assert.That(File.Exists(_testTempFilePath), Is.True, "Файл настроек должен существовать после сохранения.");

            string jsonContent = File.ReadAllText(_testTempFilePath);
            var loadedSettings = JsonSerializer.Deserialize<AppSettings>(jsonContent);

            Assert.That(loadedSettings, Is.Not.Null);
            Assert.That(loadedSettings.LanguageCultureCode, Is.EqualTo(settingsToSave.LanguageCultureCode), "Сохраненный LanguageCultureCode должен совпадать.");
            Assert.That(loadedSettings.ColorAnalyzerChannel, Is.EqualTo(settingsToSave.ColorAnalyzerChannel), "Сохраненный DevicePort должен совпадать.");
            Assert.That(loadedSettings.AutoConnectEnabled, Is.EqualTo(settingsToSave.AutoConnectEnabled), "Сохраненный AutoConnectEnabled должен совпадать.");
        }

        [Test]
        public void SaveSettings_NullSettings_DoesNotThrowAndFileNotCreatedOrModified()
        {
            if (File.Exists(_testTempFilePath)) File.Delete(_testTempFilePath);

            Assert.DoesNotThrow(() => _settingsService.SaveSettings(null), "Вызов SaveSettings с null не должен бросать исключение.");
            Assert.That(File.Exists(_testTempFilePath), Is.False, "Файл не должен быть создан/изменен при сохранении null настроек.");
        }
    }
}