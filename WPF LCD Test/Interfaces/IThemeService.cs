using WPF_LCD_Test.Models;

namespace WPF_LCD_Test.Interfaces
    {
    /// <summary>
    /// Applies and remembers the theme of the application.
    /// </summary>
    public interface IThemeService
        {
        /// <summary>
        /// Gets the theme mode chosen by the operator.
        /// </summary>
        ThemeMode Mode { get; }

        /// <summary>
        /// Gets the theme the interface is drawn with now (the system theme is resolved for <see cref="ThemeMode.System"/>).
        /// </summary>
        AppTheme EffectiveTheme { get; }

        /// <summary>
        /// Raised after a theme has been applied.
        /// </summary>
        event EventHandler? ThemeChanged;

        /// <summary>
        /// Applies the theme stored in the settings; called once at startup before the first window is shown.
        /// </summary>
        void Initialize();

        /// <summary>
        /// Applies the theme mode at once and stores it in the settings.
        /// </summary>
        /// <param name="mode">The theme mode to apply.</param>
        void SetMode(ThemeMode mode);
        }
    }
