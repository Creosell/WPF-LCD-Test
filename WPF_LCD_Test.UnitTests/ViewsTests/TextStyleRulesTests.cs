using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace WPF_LCD_Test.UnitTests.ViewsTests
    {
    /// <summary>
    /// Rules the XAML of the pages must keep so that the text of all controls looks the same: colors come from the theme, sizes from one
    /// scale, fonts from a short list, and a label or a field takes its look from the shared style, not from its own attributes.
    /// A new exception to a rule has to be added here on purpose.
    /// </summary>
    [TestFixture]
    public class TextStyleRulesTests
        {
        private static readonly int[] FontSizeScale = [10, 11, 12, 13, 14, 15, 16, 18, 20];
        private static readonly string[] FontFamilies = ["Segoe UI", "Segoe UI Symbol", "Consolas"];
        private static readonly string[] ColorProperties = ["Foreground", "Background", "BorderBrush", "Fill", "Stroke"];

        /// <summary>Colors that are not from the theme on purpose: the title bar, the selected tab, the text on a colored fill.</summary>
        private static readonly Dictionary<string, int> AllowedColors = new()
            {
            ["MainWindow.xaml|Foreground|#A0A0A0"] = 1,
            ["MainWindow.xaml|Background|Purple"] = 1,
            ["MainWindow.xaml|Foreground|White"] = 2,
            ["MainWindow.xaml|Background|Black"] = 1,
            ["MeasurementView.xaml|Foreground|#1E1E1E"] = 2,
            ["MeasurementView.xaml|Foreground|White"] = 1,
            ["MeasurementView.xaml|Fill|#9575CD"] = 1
            };

        private static string ProjectFolder([CallerFilePath] string testFile = "") =>
            Path.GetFullPath(Path.Combine(Path.GetDirectoryName(testFile)!, "..", "..", "WPF LCD Test"));

        private static IEnumerable<(string File, XDocument Document)> Pages() =>
            Directory.GetFiles(Path.Combine(ProjectFolder(), "Views"), "*.xaml")
                .Select(file => (Path.GetFileName(file), XDocument.Load(file)));

        private static IEnumerable<(string File, XDocument Document)> AllXaml() =>
            Pages().Concat(Directory.GetFiles(Path.Combine(ProjectFolder(), "Resources", "Styles"), "*.xaml")
                .Select(file => (Path.GetFileName(file), XDocument.Load(file))));

        /// <summary>Every value given to a property, from an attribute or from a Setter.</summary>
        private static IEnumerable<(string File, string Property, string Value)> Values(IEnumerable<(string File, XDocument Document)> files, params string[] properties)
            {
            foreach (var (file, document) in files)
                {
                foreach (var element in document.Descendants())
                    {
                    foreach (var attribute in element.Attributes().Where(a => properties.Contains(a.Name.LocalName)))
                        {
                        yield return (file, attribute.Name.LocalName, attribute.Value);
                        }

                    if (element.Name.LocalName == "Setter" && (string?)element.Attribute("Property") is { } property && properties.Contains(property)
                        && (string?)element.Attribute("Value") is { } value)
                        {
                        yield return (file, property, value);
                        }
                    }
                }
            }

        [Test]
        public void Colors_AreFromTheThemeExceptTheKnownOnes()
            {
            var literal = new System.Text.RegularExpressions.Regex(@"^(#[0-9A-Fa-f]{3,8}|White|Black|Gray|Red|Green|Blue|Purple|Yellow|Orange)$");
            var found = Values(AllXaml(), ColorProperties)
                .Where(v => literal.IsMatch(v.Value))
                .GroupBy(v => $"{v.File}|{v.Property}|{v.Value}")
                .ToDictionary(g => g.Key, g => g.Count());

            var unexpected = found.Where(f => !AllowedColors.TryGetValue(f.Key, out int allowed) || f.Value > allowed).Select(f => f.Key);

            Assert.That(unexpected, Is.Empty, "use a brush of the theme dictionaries (DynamicResource App.../Status.../Log...) instead of a literal color");
            }

        [Test]
        public void FontSizes_FollowTheScale()
            {
            var sizes = Values(AllXaml(), "FontSize")
                .Where(v => double.TryParse(v.Value, System.Globalization.CultureInfo.InvariantCulture, out _))
                .Where(v => !FontSizeScale.Contains((int)double.Parse(v.Value, System.Globalization.CultureInfo.InvariantCulture)))
                .Select(v => $"{v.File}: {v.Value}");

            Assert.That(sizes, Is.Empty, $"the sizes of the text are {string.Join(", ", FontSizeScale)} or AppFontSize");
            }

        [Test]
        public void FontFamilies_AreFromTheShortList()
            {
            var families = Values(AllXaml(), "FontFamily")
                .Where(v => !v.Value.StartsWith('{') && !FontFamilies.Contains(v.Value))
                .Select(v => $"{v.File}: {v.Value}");

            Assert.That(families, Is.Empty, $"the fonts are {string.Join(", ", FontFamilies)}");
            }

        [Test]
        public void Labels_TakeTheirColorFromTheSharedStyle()
            {
            var labels = Pages()
                .SelectMany(p => p.Document.Descendants().Where(e => e.Name.LocalName == "Label").Select(e => (p.File, Element: e)))
                .Where(l => l.Element.Attribute("Foreground") != null || l.Element.Attribute("FontSize") != null || l.Element.Attribute("FontWeight") != null)
                .Select(l => $"{l.File}: {l.Element.Attribute("Content")?.Value}");

            Assert.That(labels, Is.Empty, "a Label gets its text look from the Label style of FieldStyles.xaml, not from its own attributes");
            }

        [Test]
        public void FieldsOfThePages_UseTheSharedStyles()
            {
            var fields = Pages()
                .SelectMany(p => p.Document.Descendants()
                    .Where(e => e.Name.LocalName is "TextBox" or "ComboBox")
                    .Where(e => e.Parent?.Name.LocalName != "ControlTemplate")
                    .Select(e => (p.File, Kind: e.Name.LocalName, Style: (string?)e.Attribute("Style") ?? "")))
                .Where(f => !f.Style.Contains(f.Kind == "TextBox" ? "UnderlinedTextBoxStyle" : "UnderlinedComboBoxStyle"))
                .Select(f => $"{f.File}: {f.Kind}");

            Assert.That(fields, Is.Empty, "every text field and drop-down list of a page uses the Underlined... style of FieldStyles.xaml");
            }

        [Test]
        public void SharedFieldStyles_SetTheTextColorThemselves()
            {
            var styles = XDocument.Load(Path.Combine(ProjectFolder(), "Resources", "Styles", "FieldStyles.xaml"));
            var textStyles = new[] { "UnderlinedTextBoxStyle", "UnderlinedComboBoxStyle", "UnderlinedDropdownButtonStyle" };

            var withoutColor = styles.Descendants().Where(e => e.Name.LocalName == "Style")
                .Where(e => textStyles.Contains(e.Attributes().FirstOrDefault(a => a.Name.LocalName == "Key")?.Value ?? ""))
                .Where(e => !e.Elements().Any(s => s.Name.LocalName == "Setter" && (string?)s.Attribute("Property") == "Foreground"))
                .Select(e => e.Attributes().First(a => a.Name.LocalName == "Key").Value);

            Assert.That(withoutColor, Is.Empty, "a page must not have to set the text color of a field");
            }
        }
    }
