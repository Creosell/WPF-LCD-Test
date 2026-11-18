namespace WPF_LCD_Test.Models
    {
    /// <summary>
    /// Theme setting chosen by the operator.
    /// </summary>
    public enum ThemeMode
        {
        /// <summary>Dark theme.</summary>
        Dark,

        /// <summary>Light theme.</summary>
        Light,

        /// <summary>The theme of Windows, read at startup and when this mode is chosen.</summary>
        System
        }

    /// <summary>
    /// Concrete theme the interface is drawn with.
    /// </summary>
    public enum AppTheme
        {
        /// <summary>Dark theme.</summary>
        Dark,

        /// <summary>Light theme.</summary>
        Light
        }
    }
