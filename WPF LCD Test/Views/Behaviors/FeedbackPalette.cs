using System.Windows;
using System.Windows.Media;
using WPF_LCD_Test.Models;

namespace WPF_LCD_Test.Views.Behaviors
{
    /// <summary>
    /// The colors of the feedback animations (green: success, red: failure or cancel). They come from the theme dictionaries
    /// (<c>FeedbackOk</c>, <c>FeedbackErr</c> for lines and fills, <c>FeedbackGlowOk</c>, <c>FeedbackGlowErr</c> for glows), so the
    /// feedback keeps its contrast on both the dark and the light page; the colors of the dark theme are the fallback.
    /// </summary>
    internal static class FeedbackPalette
    {
        private static readonly Color DarkOk = Color.FromRgb(0x43, 0xA0, 0x47);
        private static readonly Color DarkErr = Color.FromRgb(0xE5, 0x39, 0x35);
        private static readonly Color DarkGlowOk = Color.FromRgb(0x2E, 0xD1, 0x5C);
        private static readonly Color DarkGlowErr = Color.FromRgb(0xFF, 0x45, 0x3A);

        /// <summary>
        /// Gets the color of an underline or a fill for the outcome in the theme the element is drawn with.
        /// </summary>
        /// <param name="element">The element that shows the feedback.</param>
        /// <param name="feedback">The outcome.</param>
        public static Color Line(FrameworkElement element, ActionFeedback feedback) =>
            feedback == ActionFeedback.Success ? Read(element, "FeedbackOk", DarkOk) : Read(element, "FeedbackErr", DarkErr);

        /// <summary>
        /// Gets the color of a glow for the outcome in the theme the element is drawn with.
        /// </summary>
        /// <param name="element">The element that shows the feedback.</param>
        /// <param name="feedback">The outcome.</param>
        public static Color Glow(FrameworkElement element, ActionFeedback feedback) =>
            feedback == ActionFeedback.Success ? Read(element, "FeedbackGlowOk", DarkGlowOk) : Read(element, "FeedbackGlowErr", DarkGlowErr);

        private static Color Read(FrameworkElement element, string key, Color fallback) =>
            element.TryFindResource(key) is SolidColorBrush brush ? brush.Color : fallback;
    }
}
