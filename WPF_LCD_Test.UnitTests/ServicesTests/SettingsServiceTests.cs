using System.Text.Json;
using WPF_LCD_Test.Models;
using WPF_LCD_Test.Services;

namespace WPF_LCD_Test.UnitTests.ServicesTests
    {
    [TestFixture]
    public class SettingsServiceTests
        {
        private SettingsService _settingsService;
        private string _testFilePath;
        private TestPathProvider _pathProvider;

        [SetUp]
        public void Setup()
            {
            _testFilePath = Path.Combine(Path.GetTempPath(), $"test_settings_{Guid.NewGuid()}.json");
            _pathProvider = new TestPathProvider(Path.GetDirectoryName(_testFilePath));

            _settingsService = new SettingsService(_pathProvider);
            _settingsService.SetTestFilePath(_testFilePath);

            if (File.Exists(_testFilePath))
                {
                File.Delete(_testFilePath);
                }
            }

        private class TestPathProvider : Interfaces.IPathProvider
            {
            private readonly string _baseDir;
            public TestPathProvider(string baseDir) => _baseDir = baseDir;
            public string BaseDirectory => _baseDir;
            public string ConfigDirectory => Path.Combine(_baseDir, "config");
            public string DataDirectory => Path.Combine(_baseDir, "data");
            }

        [TearDown]
        public void TearDown()
            {
            if (File.Exists(_testFilePath))
                {
                File.Delete(_testFilePath);
                }
            }


        [Test]
        public void LoadSettings_FileDoesNotExist_CreatesDefaultSettingsFile()
            {
            var loadedSettings = _settingsService.LoadSettings();

            Assert.That(loadedSettings, Is.Not.Null);
            Assert.That(File.Exists(_testFilePath), Is.True);

            string jsonContent = File.ReadAllText(_testFilePath);
            var savedSettings = JsonSerializer.Deserialize<AppSettings>(jsonContent);

            Assert.That(savedSettings, Is.Not.Null);
            Assert.That(savedSettings.LanguageCultureCode, Is.EqualTo(""));
            Assert.That(savedSettings.ColorAnalyzerChannel, Is.EqualTo("0"));
            Assert.That(savedSettings.AutoConnectEnabled, Is.True);
            }

        [Test]
        public void LoadSettings_FileExists_LoadsSettings()
            {
            var expectedSettings = new AppSettings
                {
                LanguageCultureCode = "fr",
                ColorAnalyzerChannel = "3",
                AutoConnectEnabled = true
                };
            string jsonContent = JsonSerializer.Serialize(expectedSettings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_testFilePath, jsonContent);

            var loadedSettings = _settingsService.LoadSettings();

            Assert.That(loadedSettings, Is.Not.Null);
            Assert.That(loadedSettings.LanguageCultureCode, Is.EqualTo(expectedSettings.LanguageCultureCode));
            Assert.That(loadedSettings.ColorAnalyzerChannel, Is.EqualTo(expectedSettings.ColorAnalyzerChannel));
            Assert.That(loadedSettings.AutoConnectEnabled, Is.EqualTo(expectedSettings.AutoConnectEnabled));
            }

        [Test]
        public void LoadSettings_InvalidJson_ReturnsDefaultSettings()
            {
            string invalidJson = "{ \"languageCultureCode\": \"es\", \"colorAnalyzerChannel\": \"4\", \"autoConnectEnabled\": ";
            File.WriteAllText(_testFilePath, invalidJson);

            var loadedSettings = _settingsService.LoadSettings();

            Assert.That(loadedSettings, Is.Not.Null);
            Assert.That(loadedSettings.LanguageCultureCode, Is.EqualTo(""));
            Assert.That(loadedSettings.ColorAnalyzerChannel, Is.EqualTo("0"));
            Assert.That(loadedSettings.AutoConnectEnabled, Is.True);
            }

        [Test]
        public void SaveSettings_ValidSettings_SavesCorrectly()
            {
            var settingsToSave = new AppSettings
                {
                LanguageCultureCode = "de",
                ColorAnalyzerChannel = "5",
                AutoConnectEnabled = true
                };

            _settingsService.SaveSettings(settingsToSave);

            Assert.That(File.Exists(_testFilePath), Is.True);

            string jsonContent = File.ReadAllText(_testFilePath);
            var loadedSettings = JsonSerializer.Deserialize<AppSettings>(jsonContent);

            Assert.That(loadedSettings, Is.Not.Null);
            Assert.That(loadedSettings.LanguageCultureCode, Is.EqualTo(settingsToSave.LanguageCultureCode));
            Assert.That(loadedSettings.ColorAnalyzerChannel, Is.EqualTo(settingsToSave.ColorAnalyzerChannel));
            Assert.That(loadedSettings.AutoConnectEnabled, Is.EqualTo(settingsToSave.AutoConnectEnabled));
            }

        [Test]
        public void SaveSettings_NullSettings_DoesNotCreateFile()
            {
            if (File.Exists(_testFilePath))
                {
                File.Delete(_testFilePath);
                }

            Assert.DoesNotThrow(() => _settingsService.SaveSettings(null));
            Assert.That(File.Exists(_testFilePath), Is.False);
            }

        [Test]
        public void UpdateChannel_UpdatesChannelInFile()
            {
            var initialSettings = new AppSettings { ColorAnalyzerChannel = "0" };
            _settingsService.SaveSettings(initialSettings);

            _settingsService.UpdateChannel(5);

            var loadedSettings = _settingsService.LoadSettings();
            Assert.That(loadedSettings.ColorAnalyzerChannel, Is.EqualTo("5"));
            }


        }
    }