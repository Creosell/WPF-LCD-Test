using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace WPF_LCD_Test.UnitTests.ModelsTests
    {
    [TestFixture]
    public class ThemeDictionariesTests
        {
        private static readonly XNamespace Xaml = "http://schemas.microsoft.com/winfx/2006/xaml";

        private static string ThemeFile(string theme, [CallerFilePath] string testFile = "") =>
            Path.GetFullPath(Path.Combine(Path.GetDirectoryName(testFile)!, "..", "..", "WPF LCD Test", "Resources", "Themes", $"Theme.{theme}.xaml"));

        private static SortedSet<string> Keys(string theme) =>
            [.. XDocument.Load(ThemeFile(theme)).Descendants().Select(e => (string?)e.Attribute(Xaml + "Key")).OfType<string>()];

        [Test]
        public void DarkAndLightDefineTheSameKeys()
            {
            Assert.That(Keys("Light"), Is.EquivalentTo(Keys("Dark")));
            }

        private static double Luminance(System.Windows.Media.Color color)
            {
            static double Channel(byte value)
                {
                double c = value / 255.0;
                return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
                }

            return 0.2126 * Channel(color.R) + 0.7152 * Channel(color.G) + 0.0722 * Channel(color.B);
            }

        private static System.Windows.Media.Color ColorOf(string theme, string key)
            {
            var brush = XDocument.Load(ThemeFile(theme)).Descendants().First(e => (string?)e.Attribute(Xaml + "Key") == key);
            return (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString((string)brush.Attribute("Color")!);
            }

        [TestCase("Light", 3.0)]
        [TestCase("Dark", 2.0)]
        public void FeedbackColors_KeepTheirContrastOnThePageOfTheTheme(string theme, double minimum)
            {
            var page = Luminance(ColorOf(theme, "AppPageBackground"));
            var low = new List<string>();
            foreach (var key in new[] { "FeedbackOk", "FeedbackErr", "FeedbackGlowOk", "FeedbackGlowErr" })
                {
                double color = Luminance(ColorOf(theme, key));
                double ratio = (Math.Max(color, page) + 0.05) / (Math.Min(color, page) + 0.05);
                if (ratio < minimum) low.Add($"{key} {ratio:0.0}:1");
                }

            Assert.That(low, Is.Empty, $"the breathing and the glow must be seen on the {theme} page (at least {minimum}:1)");
            }

        [Test]
        public void EveryFeedbackKeyOfThePaletteIsDefined()
            {
            var palette = Path.Combine(Path.GetDirectoryName(ThemeFile("Dark"))!, "..", "..", "Views", "Behaviors", "FeedbackPalette.cs");
            var used = System.Text.RegularExpressions.Regex.Matches(File.ReadAllText(palette), "\"(Feedback(?:Glow)?(?:Ok|Err))\"").Select(m => m.Groups[1].Value).ToHashSet();

            Assert.That(used, Is.Not.Empty);
            Assert.That(used.Except(Keys("Dark")), Is.Empty);
            }

        [Test]
        public void EveryThemeKeyUsedByTheViewsIsDefined()
            {
            var defined = Keys("Dark");
            var viewFolder = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(ThemeFile("Dark"))!, "..", "..", "Views"));
            var styleFolder = Path.Combine(viewFolder, "..", "Resources", "Styles");
            var used = Directory.GetFiles(viewFolder, "*.xaml").Concat(Directory.GetFiles(styleFolder, "*.xaml"))
                .SelectMany(file => System.Text.RegularExpressions.Regex.Matches(File.ReadAllText(file), @"DynamicResource (App(?:Page|Panel|Inset|Card|Text|Log|Tab|Control|Hover|Field|Nav|Window)\w*|Status(?:Ok|Warn|Err)\w+|Result(?:Ok|Warn)Dot|Log(?:Warn|Err)\w+|LogBarThickness)").Select(m => m.Groups[1].Value))
                .ToHashSet();

            Assert.That(used.Except(defined), Is.Empty, "keys used by the views but missing in the theme dictionaries");
            }
        }
    }
