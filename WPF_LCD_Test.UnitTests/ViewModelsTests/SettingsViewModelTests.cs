using Moq;
using WPF_LCD_Test.Interfaces;
using WPF_LCD_Test.Models;
using WPF_LCD_Test.ViewModels;

namespace WPF_LCD_Test.UnitTests.ViewModels
{
    [TestFixture]
    public class SettingsViewModelTests
    {
        private Mock<ISettingsService> _mockSettingsService;
        private Mock<ILocalizationService> _mockLocalizationService;
        private SettingsViewModel _viewModel;

        [SetUp]
        public void Setup()
        {
            _mockSettingsService = new Mock<ISettingsService>();
            _mockLocalizationService = new Mock<ILocalizationService>();
            _mockSettingsService.Setup(s => s.LoadSettings()).Returns(new AppSettings
            {
                LanguageCultureCode = "",
                DevicePort = "COM1",
                AutoConnectEnabled = true
            });
            _viewModel = new SettingsViewModel(
                _mockSettingsService.Object,
                _mockLocalizationService.Object
            );
        }

        [TearDown]
        public void Teardown()
        {
            _viewModel.Dispose();
        }

        [Test]
        public void Ctor_InitializesWithDependencies()
        {
            Assert.That(_viewModel, Is.Not.Null);
        }

        [Test]
        public void Ctor_ThrowsArgumentNullException_IfAnyDependencyIsNull()
        {
            var ex1 = Assert.Throws<ArgumentNullException>(() => new SettingsViewModel(
                null,
                _mockLocalizationService.Object
            ));
            Assert.That(ex1.ParamName, Is.EqualTo("settingsService"));
            var ex2 = Assert.Throws<ArgumentNullException>(() => new SettingsViewModel(
                _mockSettingsService.Object,
                null
            ));
            Assert.That(ex2.ParamName, Is.EqualTo("localizationService"));
        }

        [Test]
        public void Ctor_LoadsSettingsOnInitialization()
        {
            _mockSettingsService.Verify(s => s.LoadSettings(), Times.Once());
            Assert.That(_viewModel.DevicePort, Is.EqualTo("COM1"));
            Assert.That(_viewModel.AutoConnectEnabled, Is.True);
            Assert.That(_viewModel.LanguageCultureCode, Is.EqualTo(""));
            Assert.That(_viewModel.SelectedLanguage?.CultureCode, Is.EqualTo(""));
        }

        [Test]
        public void Ctor_PopulatesAvailableLanguages()
        {
            Assert.That(_viewModel.AvailableLanguages, Is.Not.Empty);
            Assert.That(_viewModel.AvailableLanguages.Count, Is.EqualTo(2));
            Assert.That(_viewModel.AvailableLanguages.Any(l => l.CultureCode == ""), Is.True);
            Assert.That(_viewModel.AvailableLanguages.Any(l => l.CultureCode == "zh-Hans"), Is.True);
        }

        [Test]
        public void Ctor_SubscribesToLanguageChangedEvent()
        {
            _mockLocalizationService.VerifyAdd(l => l.LanguageChanged += It.IsAny<EventHandler>(), Times.Once());
        }

        [Test]
        public void SelectedLanguage_UpdatesLanguageCultureCode()
        {
            var newLang = new SettingsViewModel.LanguageOption { DisplayName = "Chinese", CultureCode = "zh-Hans" };
            _viewModel.SelectedLanguage = newLang;
            Assert.That(_viewModel.LanguageCultureCode, Is.EqualTo("zh-Hans"));
        }

        [Test]
        public void SelectedLanguage_CallsSetLanguageOnLocalizationService()
        {
            var newLang = new SettingsViewModel.LanguageOption { DisplayName = "Chinese", CultureCode = "zh-Hans" };
            _viewModel.SelectedLanguage = newLang;
            _mockLocalizationService.Verify(l => l.SetLanguage("zh-Hans"), Times.Once());
        }

        [Test]
        public void SelectedLanguage_CallsSaveLanguageToSettings()
        {
            var newLang = new SettingsViewModel.LanguageOption { DisplayName = "Chinese", CultureCode = "zh-Hans" };
            _mockSettingsService.Setup(s => s.SaveSettings(It.IsAny<AppSettings>()));
            _viewModel.SelectedLanguage = newLang;
            _mockSettingsService.Verify(s => s.SaveSettings(It.Is<AppSettings>(
                appSettings => appSettings.LanguageCultureCode == "zh-Hans")), Times.Once());
        }

        [Test]
        public void SelectedLanguage_HandlesNullSelectedLanguageGracefully()
        {
            _viewModel.SelectedLanguage = new SettingsViewModel.LanguageOption { DisplayName = "Chinese", CultureCode = "zh-Hans" };
            _mockLocalizationService.Verify(l => l.SetLanguage("zh-Hans"), Times.Once());
            _viewModel.SelectedLanguage = null;
            Assert.That(_viewModel.LanguageCultureCode, Is.EqualTo(""));
            _mockLocalizationService.Verify(l => l.SetLanguage(""), Times.Once());
        }

        [Test]
        public void SaveSettingsCommand_CanExecute_ReturnsTrue()
        {
            Assert.That(_viewModel.SaveSettingsCommand.CanExecute(null), Is.True);
        }

        [Test]
        public void SaveSettingsCommand_Execute_SavesCurrentSettings()
        {
            _viewModel.DevicePort = "COM3";
            _viewModel.AutoConnectEnabled = false;
            var newLang = new SettingsViewModel.LanguageOption { DisplayName = "Chinese", CultureCode = "zh-Hans" };
            _viewModel.SelectedLanguage = newLang;
            _viewModel.SaveSettingsCommand.Execute(null);
            _mockSettingsService.Verify(s => s.SaveSettings(It.Is<AppSettings>(
                settings => settings.DevicePort == "COM3" &&
                            settings.AutoConnectEnabled == false &&
                            settings.LanguageCultureCode == "zh-Hans"
            )), Times.Once());
        }

        [Test]
        public void SaveSettingsCommand_Execute_HandlesSaveSettingsException()
        {
            _mockSettingsService.Setup(s => s.SaveSettings(It.IsAny<AppSettings>()))
                .Throws(new InvalidOperationException("Test save error"));
            Assert.DoesNotThrow(() => _viewModel.SaveSettingsCommand.Execute(null));
        }

        [Test]
        public void CancelSettingsCommand_CanExecute_ReturnsTrue()
        {
            Assert.That(_viewModel.CancelSettingsCommand.CanExecute(null), Is.True);
        }

        [Test]
        public void CancelSettingsCommand_Execute_ReloadsSettings()
        {
            _viewModel.DevicePort = "COM_CHANGED";
            _viewModel.AutoConnectEnabled = false;
            _viewModel.SelectedLanguage = new SettingsViewModel.LanguageOption { DisplayName = "Chinese", CultureCode = "zh-Hans" };
            _mockSettingsService.Setup(s => s.LoadSettings()).Returns(new AppSettings
            {
                LanguageCultureCode = "",
                DevicePort = "COM1",
                AutoConnectEnabled = true
            });
            _viewModel.CancelSettingsCommand.Execute(null);
            _mockSettingsService.Verify(s => s.LoadSettings(), Times.Exactly(2));
            Assert.That(_viewModel.DevicePort, Is.EqualTo("COM1"));
            Assert.That(_viewModel.AutoConnectEnabled, Is.True);
            Assert.That(_viewModel.LanguageCultureCode, Is.EqualTo(""));
            Assert.That(_viewModel.SelectedLanguage?.CultureCode, Is.EqualTo(""));
        }

        [Test]
        public void Dispose_UnsubscribesFromLanguageChangedEvent()
        {
            _viewModel.Dispose();
            _mockLocalizationService.VerifyRemove(l => l.LanguageChanged -= It.IsAny<EventHandler>(), Times.Once());
        }
    }
}