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
        private Mock<IColorMeasurementService> _mockColorMeasurementService;
        private Mock<IDialogService> _mockDialogService;
        private SettingsViewModel _viewModel;

        [SetUp]
        public void Setup()
            {
            _mockSettingsService = new Mock<ISettingsService>();
            _mockLocalizationService = new Mock<ILocalizationService>();
            _mockColorMeasurementService = new Mock<IColorMeasurementService>();
            _mockDialogService = new Mock<IDialogService>();

            _mockSettingsService.Setup(s => s.LoadSettings()).Returns(new AppSettings
                {
                LanguageCultureCode = "",
                ColorAnalyzerChannel = "1",
                AutoConnectEnabled = true
                });

            _mockColorMeasurementService.Setup(s => s.CurrentChannel).Returns(1);

            _viewModel = new SettingsViewModel(
                _mockSettingsService.Object,
                _mockDialogService.Object,
                _mockColorMeasurementService.Object,
                _mockLocalizationService.Object
            );
            }

        [TearDown]
        public void TearDown()
            {
            _viewModel.Dispose();
            }

        [Test]
        public void Constructor_InitializesCorrectly()
            {
            Assert.That(_viewModel, Is.Not.Null);
            }

        [Test]
        public void Constructor_ThrowsArgumentNullException_WhenDependencyIsNull()
            {
            Assert.Throws<ArgumentNullException>(() => new SettingsViewModel(
                null,
                _mockDialogService.Object,
                _mockColorMeasurementService.Object,
                _mockLocalizationService.Object
            ));

            Assert.Throws<ArgumentNullException>(() => new SettingsViewModel(
                _mockSettingsService.Object,
                null,
                _mockColorMeasurementService.Object,
                _mockLocalizationService.Object
            ));

            Assert.Throws<ArgumentNullException>(() => new SettingsViewModel(
                _mockSettingsService.Object,
                _mockDialogService.Object,
                null,
                _mockLocalizationService.Object
            ));

            Assert.Throws<ArgumentNullException>(() => new SettingsViewModel(
                _mockSettingsService.Object,
                _mockDialogService.Object,
                _mockColorMeasurementService.Object,
                null
            ));
            }

        [Test]
        public void Constructor_LoadsSettings()
            {
            _mockSettingsService.Verify(s => s.LoadSettings(), Times.Once());
            Assert.That(_viewModel.ColorAnalyzerChannel, Is.EqualTo("1"));
            Assert.That(_viewModel.AutoConnectEnabled, Is.True);
            Assert.That(_viewModel.LanguageCultureCode, Is.EqualTo(""));
            }

        [Test]
        public void Constructor_PopulatesAvailableLanguages()
            {
            Assert.That(_viewModel.AvailableLanguages.Count, Is.EqualTo(2));
            Assert.That(_viewModel.AvailableLanguages.Any(l => l.CultureCode == ""), Is.True);
            Assert.That(_viewModel.AvailableLanguages.Any(l => l.CultureCode == "zh-Hans"), Is.True);
            }

        [Test]
        public void Constructor_SubscribesToEvents()
            {
            _mockLocalizationService.VerifyAdd(l => l.LanguageChanged += It.IsAny<EventHandler>(), Times.Once());
            _mockColorMeasurementService.VerifyAdd(c => c.CurrentChannelChanged += It.IsAny<EventHandler<int>>(), Times.Once());
            }

        [Test]
        public void SelectedLanguage_UpdatesLanguageCultureCode()
            {
            var newLang = new SettingsViewModel.LanguageOption { DisplayName = "Chinese", CultureCode = "zh-Hans" };

            _viewModel.SelectedLanguage = newLang;

            Assert.That(_viewModel.LanguageCultureCode, Is.EqualTo("zh-Hans"));
            }

        [Test]
        public void SelectedLanguage_CallsSetLanguage()
            {
            var newLang = new SettingsViewModel.LanguageOption { DisplayName = "Chinese", CultureCode = "zh-Hans" };

            _viewModel.SelectedLanguage = newLang;

            _mockLocalizationService.Verify(l => l.SetLanguage("zh-Hans"), Times.Once());
            }

        [Test]
        public void SelectedLanguage_SavesLanguageSettings()
            {
            var newLang = new SettingsViewModel.LanguageOption { DisplayName = "Chinese", CultureCode = "zh-Hans" };

            _viewModel.SelectedLanguage = newLang;

            _mockSettingsService.Verify(s => s.SaveSettings(It.Is<AppSettings>(
                settings => settings.LanguageCultureCode == "zh-Hans")), Times.Once());
            }

        [Test]
        public void SelectedLanguage_HandlesNull()
            {
            _viewModel.SelectedLanguage = null;

            Assert.That(_viewModel.LanguageCultureCode, Is.EqualTo(""));
            }

        [Test]
        public void SaveSettingsCommand_CanExecute_ReturnsTrue()
            {
            Assert.That(_viewModel.SaveSettingsCommand.CanExecute(null), Is.True);
            }

        [Test]
        public void SaveSettingsCommand_Execute_SavesSettings()
            {
            _mockColorMeasurementService.Setup(s => s.CurrentChannel).Returns(3);
            _viewModel.AutoConnectEnabled = false;

            _viewModel.SaveSettingsCommand.Execute(null);

            _mockSettingsService.Verify(s => s.SaveSettings(It.Is<AppSettings>(
                settings => settings.ColorAnalyzerChannel == "3" &&
                            settings.AutoConnectEnabled == false
            )), Times.Once());
            }

        [Test]
        public void SaveSettingsCommand_Execute_HandlesException()
            {
            _mockSettingsService.Setup(s => s.SaveSettings(It.IsAny<AppSettings>()))
                .Throws(new InvalidOperationException("Test error"));

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
            _viewModel.AutoConnectEnabled = false;

            _viewModel.CancelSettingsCommand.Execute(null);

            _mockSettingsService.Verify(s => s.LoadSettings(), Times.Exactly(2));
            Assert.That(_viewModel.AutoConnectEnabled, Is.True);
            }

        [Test]
        public void Dispose_UnsubscribesFromEvents()
            {
            _viewModel.Dispose();

            _mockLocalizationService.VerifyRemove(l => l.LanguageChanged -= It.IsAny<EventHandler>(), Times.Once());
            _mockColorMeasurementService.VerifyRemove(c => c.CurrentChannelChanged -= It.IsAny<EventHandler<int>>(), Times.Once());
            }

        [Test]
        public void ColorAnalyzerChannel_ReturnsCurrentChannelFromService()
            {
            _mockColorMeasurementService.Setup(s => s.CurrentChannel).Returns(5);

            Assert.That(_viewModel.ColorAnalyzerChannel, Is.EqualTo("5"));
            }
        }
    }