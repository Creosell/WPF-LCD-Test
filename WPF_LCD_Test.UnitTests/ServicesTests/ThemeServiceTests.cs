using Moq;
using WPF_LCD_Test.Interfaces;
using WPF_LCD_Test.Models;
using WPF_LCD_Test.Services;

namespace WPF_LCD_Test.UnitTests.ServicesTests
    {
    [TestFixture]
    public class ThemeServiceTests
        {
        private Mock<ISettingsService> _settings;
        private Mock<ISystemThemeProvider> _system;
        private Mock<IThemeApplier> _applier;
        private ThemeService _service;

        [SetUp]
        public void SetUp()
            {
            _settings = new Mock<ISettingsService>();
            _settings.Setup(s => s.LoadSettings()).Returns(new AppSettings());
            _system = new Mock<ISystemThemeProvider>();
            _applier = new Mock<IThemeApplier>();
            _service = new ThemeService(_settings.Object, _system.Object, _applier.Object);
            }

        [Test]
        public void Initialize_WithoutAStoredThemeAppliesDark()
            {
            _service.Initialize();

            _applier.Verify(a => a.Apply(AppTheme.Dark), Times.Once);
            Assert.That((_service.Mode, _service.EffectiveTheme), Is.EqualTo((ThemeMode.Dark, AppTheme.Dark)));
            }

        [Test]
        public void Initialize_AppliesTheStoredLightTheme()
            {
            _settings.Setup(s => s.LoadSettings()).Returns(new AppSettings { Theme = ThemeMode.Light });

            _service.Initialize();

            _applier.Verify(a => a.Apply(AppTheme.Light), Times.Once);
            }

        [TestCase(AppTheme.Dark)]
        [TestCase(AppTheme.Light)]
        public void SystemMode_FollowsTheThemeOfWindows(AppTheme system)
            {
            _system.Setup(s => s.GetSystemTheme()).Returns(system);

            _service.SetMode(ThemeMode.System);

            Assert.That((_service.Mode, _service.EffectiveTheme), Is.EqualTo((ThemeMode.System, system)));
            _applier.Verify(a => a.Apply(system), Times.Once);
            }

        [Test]
        public void SetMode_AppliesAtOnceStoresTheModeAndRaisesTheEvent()
            {
            int raised = 0;
            _service.ThemeChanged += (_, _) => raised++;

            _service.SetMode(ThemeMode.Light);

            _applier.Verify(a => a.Apply(AppTheme.Light), Times.Once);
            _settings.Verify(s => s.UpdateTheme(ThemeMode.Light), Times.Once);
            Assert.That(raised, Is.EqualTo(1));
            }

        [Test]
        public void Initialize_DoesNotWriteTheSettings()
            {
            _service.Initialize();

            _settings.Verify(s => s.UpdateTheme(It.IsAny<ThemeMode>()), Times.Never);
            }
        }
    }
