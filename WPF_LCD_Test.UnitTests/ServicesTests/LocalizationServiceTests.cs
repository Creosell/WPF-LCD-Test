using System.Globalization;
using WPF_LCD_Test.Interfaces;
using WPF_LCD_Test.Services;

namespace WPF_LCD_Test.UnitTests.ServicesTests
    {
    [TestFixture]
    public class LocalizationServiceTests
        {
        private ILocalizationService _localizationService;
        private CultureInfo _initialCulture;
        private string _capturedStatusMessage;

        [SetUp]
        public void Setup()
            {
            _initialCulture = Thread.CurrentThread.CurrentUICulture;
            _localizationService = LocalizationService.Instance;
            _capturedStatusMessage = null;
            }

        [TearDown]
        public void TearDown()
            {
            if (_initialCulture != null)
                {
                Thread.CurrentThread.CurrentCulture = _initialCulture;
                Thread.CurrentThread.CurrentUICulture = _initialCulture;
                CultureInfo.DefaultThreadCurrentCulture = _initialCulture;
                CultureInfo.DefaultThreadCurrentUICulture = _initialCulture;
                }

            if (_localizationService != null)
                {
                _localizationService.StatusMessage -= CaptureStatusMessage;
                }
            }

        private void CaptureStatusMessage(object sender, string message)
            {
            _capturedStatusMessage = message;
            }

        [Test]
        public void Instance_ReturnsSingletonInstance()
            {
            var instance1 = LocalizationService.Instance;
            var instance2 = LocalizationService.Instance;

            Assert.That(instance1, Is.SameAs(instance2));
            Assert.That(instance1, Is.InstanceOf<ILocalizationService>());
            }

        [Test]
        public void SetLanguage_ChangesCurrentCulture()
            {
            string cultureCode = "zh-Hans";
            CultureInfo expectedCulture = new CultureInfo(cultureCode);

            _localizationService.SetLanguage(cultureCode);

            Assert.That(_localizationService.CurrentCulture, Is.EqualTo(expectedCulture));
            Assert.That(Thread.CurrentThread.CurrentCulture, Is.EqualTo(expectedCulture));
            Assert.That(Thread.CurrentThread.CurrentUICulture, Is.EqualTo(expectedCulture));
            }

        [Test]
        public void SetLanguage_RaisesLanguageChangedEvent()
            {
            bool eventRaised = false;
            _localizationService.LanguageChanged += (sender, args) => eventRaised = true;

            _localizationService.SetLanguage("fr");

            Assert.That(eventRaised, Is.True);
            }

        [Test]
        public void SetLanguage_HandlesCultureNotFoundException()
            {
            string invalidCultureCode = "invalid-culture-xxx";
            _localizationService.StatusMessage += CaptureStatusMessage;

            _localizationService.SetLanguage(invalidCultureCode);

            Assert.That(_capturedStatusMessage, Is.Not.Null.And.Contains("Culture is not supported"));
            }

        [Test]
        public void GetString_ReturnsLocalizedValue()
            {
            _localizationService.SetLanguage("");
            string expectedEnglish = Resources.Resources.ResourceManager.GetString("Err", new CultureInfo(""));
            string actualEnglish = _localizationService.GetString("Err");

            Assert.That(actualEnglish, Is.EqualTo(expectedEnglish));

            _localizationService.SetLanguage("zh-Hans");
            string expectedChinese = Resources.Resources.ResourceManager.GetString("Err", new CultureInfo("zh-Hans"));
            string actualChinese = _localizationService.GetString("Err");

            Assert.That(actualChinese, Is.EqualTo(expectedChinese));
            }

        [Test]
        public void GetString_ReturnsFormattedLocalizedValue()
            {
            _localizationService.SetLanguage("");
            string format = Resources.Resources.ResourceManager.GetString("TestFormatString", new CultureInfo(""));
            string expected = string.Format(new CultureInfo(""), format, "MyValue");

            string actual = _localizationService.GetString("TestFormatString", "MyValue");

            Assert.That(actual, Is.EqualTo(expected));
            }

        [Test]
        public void GetString_HandlesMissingKey()
            {
            string nonExistentKey = "NonExistentKey";
            _localizationService.StatusMessage += CaptureStatusMessage;

            string result = _localizationService.GetString(nonExistentKey);

            Assert.That(result, Is.EqualTo($"!{nonExistentKey}!"));
            Assert.That(_capturedStatusMessage, Is.Not.Null.And.Contains("LocalisationService Warning"));
            }
        }
    }