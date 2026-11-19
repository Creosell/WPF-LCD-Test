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
