using System.Globalization;
using System.Reflection;
using WPF_LCD_Test.Interfaces;
using WPF_LCD_Test.Services;

namespace WPF_LCD_Test.UnitTests.ServicesTests
{
    [TestFixture]
    public class LocalizationServiceTests
    {
        private ILocalizationService _localizationService;
        private CultureInfo _initialCulture;
        private string _capturedErrorMessage; // Поле для захвата сообщения StatusMessage

        // Метод для обработки события StatusMessage
        private void TestStatusMessageEventHandler(object sender, string message)
        {
            _capturedErrorMessage = message;
        }

        // Метод для сброса синглтона LocalizationService с использованием рефлексии
        private void ResetLocalizationServiceSingleton()
        {
            var lazyInstanceField = typeof(LocalizationService).GetField(
                "_lazyInstance",
                BindingFlags.NonPublic | BindingFlags.Static
            );

            if (lazyInstanceField == null)
            {
                // Это должно быть невозможно, но добавим для устойчивости
                return;
            }

            // Находим приватный конструктор LocalizationService
            var privateCtor = typeof(LocalizationService)
                .GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(c => c.GetParameters().Length == 0); // Ищем конструктор без параметров

            if (privateCtor == null)
            {
                return;
            }

            // Создаем новый Lazy<ILocalizationService>, используя рефлексию для вызова приватного конструктора
            // Это "обходной путь", чтобы Lazy мог создать экземпляр.
            var newLazyInstance = new Lazy<ILocalizationService>(() =>
            {
                // Вызываем приватный конструктор через рефлексию
                return (ILocalizationService)privateCtor.Invoke(null);
            });

            // Устанавливаем _lazyInstance в новый Lazy<T> объект
            lazyInstanceField.SetValue(null, newLazyInstance);
        }

        [SetUp]
        public void Setup()
        {
            _initialCulture = Thread.CurrentThread.CurrentUICulture;

            // !!! СБРОС СИНГЛТОНА ПЕРЕД КАЖДЫМ ТЕСТОМ !!!
            ResetLocalizationServiceSingleton();

            // Получаем (новый) экземпляр синглтона.
            _localizationService = LocalizationService.Instance;

            // Сбрасываем захваченное сообщение
            _capturedErrorMessage = null;
        }

        [TearDown]
        public void Teardown()
        {
            // Восстанавливаем начальную культуру потока
            if (_initialCulture != null)
            {
                Thread.CurrentThread.CurrentCulture = _initialCulture;
                Thread.CurrentThread.CurrentUICulture = _initialCulture;
            }

            // Отписываемся от обработчика только если _localizationService был инициализирован
            if (_localizationService != null)
            {
                _localizationService.StatusMessage -= TestStatusMessageEventHandler;
            }
        }

        [Test]
        public void Instance_ReturnsSingletonInstance()
        {
            // Act
            var instance1 = LocalizationService.Instance;
            var instance2 = LocalizationService.Instance;

            // Assert
            Assert.That(
                instance1,
                Is.SameAs(instance2),
                "Instance should return the same singleton object."
            );
            Assert.That(
                instance1,
                Is.InstanceOf<ILocalizationService>(),
                "Instance should be of type ILocalizationService."
            );
        }

        [Test]
        public void SetLanguage_ChangesCurrentCulture()
        {
            // Arrange
            string cultureCode = "zh-Hans"; // Китайский (упрощенный)
            CultureInfo expectedCulture = new CultureInfo(cultureCode);

            // Act
            _localizationService.SetLanguage(cultureCode);

            // Assert
            Assert.That(
                _localizationService.CurrentCulture,
                Is.EqualTo(expectedCulture),
                "Service's CurrentCulture should be updated."
            );
            Assert.That(
                Thread.CurrentThread.CurrentCulture,
                Is.EqualTo(expectedCulture),
                "Thread.CurrentCulture should be updated."
            );
            Assert.That(
                Thread.CurrentThread.CurrentUICulture,
                Is.EqualTo(expectedCulture),
                "Thread.CurrentUICulture should be updated."
            );
        }

        [Test]
        public void SetLanguage_RaisesLanguageChangedEvent()
        {
            // Arrange
            string cultureCode = "fr"; // Французский
            bool eventRaised = false;
            // Подписываемся на событие LanguageChanged
            _localizationService.LanguageChanged += (sender, args) => eventRaised = true;

            // Act
            _localizationService.SetLanguage(cultureCode);

            // Assert
            Assert.That(
                eventRaised,
                Is.True,
                "LanguageChanged event should be raised when language is set."
            );

            // Отписываемся, чтобы не влиять на другие тесты (для LanguageChanged тоже)
            _localizationService.LanguageChanged -= (sender, args) => eventRaised = true; // Отписаться от лямбды сложно, но здесь для примера
        }

        [Test]
        public void SetLanguage_HandlesCultureNotFoundException()
        {
            // Arrange
            string invalidCultureCode = "xPxPasdPPPP"; // Несуществующий код культуры
            _capturedErrorMessage = null; // Сбрасываем перед использованием

            // Подписываемся на обработчик StatusMessage
            _localizationService.StatusMessage += TestStatusMessageEventHandler;

            // Act
            _localizationService.SetLanguage(invalidCultureCode);

            // Assert
            Assert.That(
                _capturedErrorMessage,
                Is.Not.Null.And.Contains("Culture is not supported"),
                "StatusMessage should be invoked with CultureNotFoundException message."
            );

            // Отписываемся после проверки
            _localizationService.StatusMessage -= TestStatusMessageEventHandler;
        }

        [Test]
        public void GetString_ReturnsLocalizedValue()
        {
            // Arrange
            _localizationService.SetLanguage("en"); // Устанавливаем английский
            string expectedEnglish = Resources.Resources.ResourceManager.GetString(
                "Err",
                new CultureInfo("en")
            );

            // Actually, we are using the key "Err" for the test, which is a different key in the .resx file.
            string actualEnglish = _localizationService.GetString("Err");

            // Assert
            Assert.That(
                actualEnglish,
                Is.EqualTo(expectedEnglish),
                "GetString should return the correct localized string for English."
            );

            _localizationService.SetLanguage("zh-Hans");
            string expectedChinese = WPF_LCD_Test.Resources.Resources.ResourceManager.GetString(
                "Err",
                new CultureInfo("zh-Hans")
            );
            string actualChinese = _localizationService.GetString("Err");
            Assert.That(
                actualChinese,
                Is.EqualTo(expectedChinese),
                "GetString should return the correct localized string for Chinese."
            );
        }

        [Test]
        public void GetString_ReturnsFormattedLocalizedValue()
        {
            // Arrange
            // Используем ключ "TestFormatString" из .resx
            _localizationService.SetLanguage("en");
            string expectedFormatted = string.Format(
                new CultureInfo("en"),
                WPF_LCD_Test.Resources.Resources.ResourceManager.GetString(
                    "TestFormatString",
                    new CultureInfo("en")
                ),
                "MyValue"
            );

            // Act
            string actualFormatted = _localizationService.GetString("TestFormatString", "MyValue");

            // Assert
            Assert.That(
                actualFormatted,
                Is.EqualTo(expectedFormatted),
                "GetString with arguments should return correctly formatted localized string."
            );
        }

        [Test]
        public void GetString_HandlesMissingKey()
        {
            // Arrange
            string nonExistentKey = "NonExistentStringKey";
            _capturedErrorMessage = null; // Сбрасываем перед использованием

            // Подписываемся на обработчик StatusMessage
            _localizationService.StatusMessage += TestStatusMessageEventHandler;

            // Act
            string result = _localizationService.GetString(nonExistentKey);

            // Assert
            Assert.That(
                result,
                Is.EqualTo($"!{nonExistentKey}!"),
                "GetString should return a fallback string for a missing key."
            );
            Assert.That(
                _capturedErrorMessage,
                Is.Not.Null.And.Contains("LocalisationService Warning"),
                "StatusMessage should be invoked for a missing key."
            );

            // Отписываемся после проверки
            _localizationService.StatusMessage -= TestStatusMessageEventHandler;
        }
    }
}