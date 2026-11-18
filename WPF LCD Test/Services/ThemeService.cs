using WPF_LCD_Test.Interfaces;
using WPF_LCD_Test.Models;

namespace WPF_LCD_Test.Services
    {
    /// <summary>
    /// Resolves the theme mode (the system theme included), applies it through <see cref="IThemeApplier"/> and stores it in the settings.
    /// </summary>
    public sealed class ThemeService : IThemeService
        {
        private readonly ISettingsService _settingsService;
        private readonly ISystemThemeProvider _systemThemeProvider;
        private readonly IThemeApplier _themeApplier;

        public ThemeMode Mode { get; private set; } = ThemeMode.Dark;

        public AppTheme EffectiveTheme { get; private set; } = AppTheme.Dark;

        public event EventHandler? ThemeChanged;

        /// <summary>
        /// Initializes the service.
        /// </summary>
        /// <param name="settingsService">Settings storage.</param>
        /// <param name="systemThemeProvider">Reader of the Windows theme.</param>
        /// <param name="themeApplier">Applier of the resolved theme to the interface.</param>
        public ThemeService(ISettingsService settingsService, ISystemThemeProvider systemThemeProvider, IThemeApplier themeApplier)
            {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _systemThemeProvider = systemThemeProvider ?? throw new ArgumentNullException(nameof(systemThemeProvider));
            _themeApplier = themeApplier ?? throw new ArgumentNullException(nameof(themeApplier));
            }

        /// <summary>
        /// Applies the theme stored in the settings.
        /// </summary>
        public void Initialize() => Apply(_settingsService.LoadSettings().Theme);

        /// <summary>
        /// Applies the theme mode at once and stores it in the settings.
        /// </summary>
        /// <param name="mode">The theme mode to apply.</param>
        public void SetMode(ThemeMode mode)
            {
            Apply(mode);
            _settingsService.UpdateTheme(mode);
            }

        private void Apply(ThemeMode mode)
            {
            Mode = mode;
            EffectiveTheme = mode switch
                {
                ThemeMode.Light => AppTheme.Light,
                ThemeMode.System => _systemThemeProvider.GetSystemTheme(),
                _ => AppTheme.Dark
                };

            _themeApplier.Apply(EffectiveTheme);
            ThemeChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }
