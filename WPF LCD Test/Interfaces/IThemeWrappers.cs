using WPF_LCD_Test.Models;

namespace WPF_LCD_Test.Interfaces
    {
    /// <summary>
    /// Reads the theme of the operating system.
    /// </summary>
    public interface ISystemThemeProvider
        {
        /// <summary>
        /// Gets the theme Windows uses for applications.
        /// </summary>
        /// <returns>The system theme; dark when it cannot be read.</returns>
        AppTheme GetSystemTheme();
        }

    /// <summary>
    /// Draws the whole interface with a theme.
    /// </summary>
    public interface IThemeApplier
        {
        /// <summary>
        /// Switches the base theme of the controls and the application brushes to the theme.
        /// </summary>
        /// <param name="theme">The theme to draw with.</param>
        void Apply(AppTheme theme);
        }
    }
