using System.Windows;
using MaterialDesignThemes.Wpf;
using WPF_LCD_Test.Interfaces;
using WPF_LCD_Test.Models;

namespace WPF_LCD_Test.Wrappers
    {
    /// <summary>
    /// Draws the application with a theme: switches the Material Design base theme and swaps the dictionary with the application brushes.
    /// </summary>
    public sealed class WpfThemeApplier : IThemeApplier
        {
        private const string DictionaryFolder = "/Resources/Themes/Theme.";

        /// <summary>
        /// Switches the base theme of the controls and the application brushes to the theme.
        /// </summary>
        /// <param name="theme">The theme to draw with.</param>
        public void Apply(AppTheme theme)
            {
            var application = Application.Current;
            if (application == null) return;

            var palette = new PaletteHelper();
            var materialTheme = palette.GetTheme();
            materialTheme.SetBaseTheme(theme == AppTheme.Dark ? BaseTheme.Dark : BaseTheme.Light);
            palette.SetTheme(materialTheme);

            var dictionaries = application.Resources.MergedDictionaries;
            var brushes = new ResourceDictionary { Source = new Uri($"{DictionaryFolder}{theme}.xaml", UriKind.Relative) };
            int index = dictionaries.ToList().FindIndex(d => d.Source?.OriginalString.StartsWith(DictionaryFolder, StringComparison.Ordinal) == true);
            if (index >= 0) dictionaries[index] = brushes;
            else dictionaries.Add(brushes);
            }
        }
    }
