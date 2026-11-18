using System.IO;
using Microsoft.Win32;
using WPF_LCD_Test.Interfaces;
using WPF_LCD_Test.Models;

namespace WPF_LCD_Test.Wrappers
    {
    /// <summary>
    /// Reads the application theme of Windows from the registry.
    /// </summary>
    public sealed class WindowsSystemThemeProvider : ISystemThemeProvider
        {
        private const string PersonalizeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
        private const string AppsUseLightTheme = "AppsUseLightTheme";

        /// <summary>
        /// Gets the theme Windows uses for applications.
        /// </summary>
        /// <returns>The system theme; dark when the value cannot be read.</returns>
        public AppTheme GetSystemTheme()
            {
            try
                {
                using var key = Registry.CurrentUser.OpenSubKey(PersonalizeKey);
                return key?.GetValue(AppsUseLightTheme) is int light && light != 0 ? AppTheme.Light : AppTheme.Dark;
                }
            catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException or IOException)
                {
                return AppTheme.Dark;
                }
            }
        }
    }
