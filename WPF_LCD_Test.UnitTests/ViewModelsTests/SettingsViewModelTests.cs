using NUnit.Framework;
using Moq;
using System;
using System.Linq;
using System.Threading.Tasks;
using WPF_LCD_Test.Interfaces;
using WPF_LCD_Test.Models;
using WPF_LCD_Test.ViewModels;
using System.Collections.ObjectModel; // Для ObservableCollection

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

            // Настроим мок SettingsService для возврата настроек по умолчанию
            _mockSettingsService.Setup(s => s.LoadSettings()).Returns(new AppSettings
            {
                LanguageCultureCode = "en",
                DevicePort = "COM1",
                AutoConnectEnabled = true
            });

            // Для LocalizationService_LanguageChanged
            // Это событие должно быть виртуальным в Mock<ILocalizationService>
            // или мы должны вручную вызывать его из теста.
            // Предположим, что ILocalizationService.LanguageChanged - это обычное событие.

            _viewModel = new SettingsViewModel(
                _mockSettingsService.Object,
                _mockLocalizationService.Object
            );
        }

        [TearDown]
        public void Teardown()
        {
            _viewModel.Dispose(); // Убедимся, что Dispose вызывается после каждого теста
        }

        // --- Тесты конструктора и инициализации ---

        [Test]
        public void Ctor_InitializesWithDependencies()
        {
            Assert.That(_viewModel, Is.Not.Null);
            // Проверка, что зависимости не null, уже делается в конструкторе ArgumentNullException.ThrowIfNull
        }

        [Test]
        public void Ctor_ThrowsArgumentNullException_IfAnyDependencyIsNull() // УДАЛИТЕ ПАРАМЕТР string paramName
        {
            // Тест для settingsService
            var ex1 = Assert.Throws<ArgumentNullException>(() => new SettingsViewModel(
                null,
                _mockLocalizationService.Object
            ));
            // Добавьте проверку ParamName, если это необходимо
            Assert.That(ex1.ParamName, Is.EqualTo("settingsService"), "ParamName should be 'settingsService'");


            // Тест для localizationService
            var ex2 = Assert.Throws<ArgumentNullException>(() => new SettingsViewModel(
                _mockSettingsService.Object,
                null
            ));
            // Добавьте проверку ParamName, если это необходимо
            Assert.That(ex2.ParamName, Is.EqualTo("localizationService"), "ParamName should be 'localizationService'");
        }

        [Test]
        public void Ctor_LoadsSettingsOnInitialization()
        {
            // Проверяем, что LoadSettings был вызван при создании ViewModel
            _mockSettingsService.Verify(s => s.LoadSettings(), Times.Once());

            // Проверяем, что свойства ViewModel правильно обновлены
            Assert.That(_viewModel.DevicePort, Is.EqualTo("COM1"));
            Assert.That(_viewModel.AutoConnectEnabled, Is.True);
            Assert.That(_viewModel.LanguageCultureCode, Is.EqualTo("en"));
            Assert.That(_viewModel.SelectedLanguage?.CultureCode, Is.EqualTo("en"));
        }

        [Test]
        public void Ctor_PopulatesAvailableLanguages()
        {
            Assert.That(_viewModel.AvailableLanguages, Is.Not.Empty);
            Assert.That(_viewModel.AvailableLanguages.Count, Is.EqualTo(2)); // "en" и "zh-Hans"
            Assert.That(_viewModel.AvailableLanguages.Any(l => l.CultureCode == "en"), Is.True);
            Assert.That(_viewModel.AvailableLanguages.Any(l => l.CultureCode == "zh-Hans"), Is.True);
        }

        [Test]
        public void Ctor_SubscribesToLanguageChangedEvent()
        {
            // Проверяем, что подписка на событие была сделана
            _mockLocalizationService.VerifyAdd(l => l.LanguageChanged += It.IsAny<EventHandler>(), Times.Once());
        }

        // --- Тесты свойств ---

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

            // Настроим мок, чтобы он не выбрасывал исключений при SaveSettings
            _mockSettingsService.Setup(s => s.SaveSettings(It.IsAny<AppSettings>()));

            _viewModel.SelectedLanguage = newLang;

            // Verify, что SaveSettings был вызван с нужным кодом языка
            _mockSettingsService.Verify(s => s.SaveSettings(It.Is<AppSettings>(
                appSettings => appSettings.LanguageCultureCode == "zh-Hans")), Times.Once());
        }

        [Test]
        public void SelectedLanguage_HandlesNullSelectedLanguageGracefully()
        {
            // Установим другой язык сначала
            _viewModel.SelectedLanguage = new SettingsViewModel.LanguageOption { DisplayName = "Chinese", CultureCode = "zh-Hans" };
            _mockLocalizationService.Verify(l => l.SetLanguage("zh-Hans"), Times.Once()); // Проверим, что вызвался

            // Сбрасываем выбранный язык в null
            _viewModel.SelectedLanguage = null;

            // Проверим, что LanguageCultureCode сброшен на "en" (из вашего кода)
            Assert.That(_viewModel.LanguageCultureCode, Is.EqualTo("en"));
            // Убедимся, что SetLanguage НЕ вызывается с null или пустым значением, если ваша логика этого требует
            // В вашем коде: _localizationService.SetLanguage(LanguageCultureCode); будет вызван с "en"
            _mockLocalizationService.Verify(l => l.SetLanguage("en"), Times.Once());
        }

        // --- Тесты команд ---

        [Test]
        public void SaveSettingsCommand_CanExecute_ReturnsTrue()
        {
            Assert.That(_viewModel.SaveSettingsCommand.CanExecute(null), Is.True);
        }

        [Test]
        public void SaveSettingsCommand_Execute_SavesCurrentSettings()
        {
            // Arrange
            _viewModel.DevicePort = "COM3";
            _viewModel.AutoConnectEnabled = false;
            var newLang = new SettingsViewModel.LanguageOption { DisplayName = "Chinese", CultureCode = "zh-Hans" };
            _viewModel.SelectedLanguage = newLang; // Это уже вызовет SaveLanguageToSettings

            // Act
            _viewModel.SaveSettingsCommand.Execute(null);

            // Assert
            // Проверим, что SaveSettings был вызван сервисом
            _mockSettingsService.Verify(s => s.SaveSettings(It.Is<AppSettings>(
                settings => settings.DevicePort == "COM3" &&
                            settings.AutoConnectEnabled == false &&
                            settings.LanguageCultureCode == "zh-Hans"
            )), Times.Once());
            // Обратите внимание, что SaveLanguageToSettings также вызывается из SelectedLanguage setter,
            // поэтому здесь может быть Times.Exactly(2) для SaveSettings, если SelectedLanguage изменился.
            // Если вы хотите тестировать только вызов из ExecuteSaveSettings,
            // можно настроить мок _settingsService.Setup(s => s.SaveSettings(It.IsAny<AppSettings>()));
            // и сбросить счетчики вызовов (но это уже усложняет).
            // Для этого теста достаточно Times.AtLeastOnce(), чтобы убедиться, что он вообще был вызван.
        }

        [Test]
        public void SaveSettingsCommand_Execute_HandlesSaveSettingsException()
        {
            // Arrange
            _mockSettingsService.Setup(s => s.SaveSettings(It.IsAny<AppSettings>()))
                .Throws(new InvalidOperationException("Test save error"));

            // Act & Assert
            // Убеждаемся, что исключение перехватывается и не выбрасывается из ViewModel
            Assert.DoesNotThrow(() => _viewModel.SaveSettingsCommand.Execute(null));
            // В реальном приложении здесь можно было бы проверить, что DialogService вызвал сообщение об ошибке
            // _mockDialogService.Verify(d => d.ShowMessage(It.IsAny<string>(), It.IsAny<string>()), Times.Once());
        }

        [Test]
        public void CancelSettingsCommand_CanExecute_ReturnsTrue()
        {
            Assert.That(_viewModel.CancelSettingsCommand.CanExecute(null), Is.True);
        }

        [Test]
        public void CancelSettingsCommand_Execute_ReloadsSettings()
        {
            // Arrange
            // Изменяем настройки в ViewModel
            _viewModel.DevicePort = "COM_CHANGED";
            _viewModel.AutoConnectEnabled = false;
            _viewModel.SelectedLanguage = new SettingsViewModel.LanguageOption { DisplayName = "Chinese", CultureCode = "zh-Hans" };

            // Настраиваем мок для возврата исходных настроек
            _mockSettingsService.Setup(s => s.LoadSettings()).Returns(new AppSettings
            {
                LanguageCultureCode = "en",
                DevicePort = "COM1",
                AutoConnectEnabled = true
            });

            // Act
            _viewModel.CancelSettingsCommand.Execute(null);

            // Assert
            // Проверяем, что LoadSettings был вызван (уже вызывался в конструкторе)
            _mockSettingsService.Verify(s => s.LoadSettings(), Times.Exactly(2)); // Один раз в конструкторе, один раз при отмене

            // Проверяем, что свойства ViewModel вернулись к исходным
            Assert.That(_viewModel.DevicePort, Is.EqualTo("COM1"));
            Assert.That(_viewModel.AutoConnectEnabled, Is.True);
            Assert.That(_viewModel.LanguageCultureCode, Is.EqualTo("en"));
            Assert.That(_viewModel.SelectedLanguage?.CultureCode, Is.EqualTo("en"));
        }

        // --- Тесты IDisposable ---

        [Test]
        public void Dispose_UnsubscribesFromLanguageChangedEvent()
        {
            // Arrange
            // Act
            _viewModel.Dispose();

            // Assert
            // VerifyNoOtherCalls() здесь может быть слишком строгим,
            // но VerifyRemove указывает на успешную отписку
            _mockLocalizationService.VerifyRemove(l => l.LanguageChanged -= It.IsAny<EventHandler>(), Times.Once());
        }
    }
}