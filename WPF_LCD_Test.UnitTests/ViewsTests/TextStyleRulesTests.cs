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
        public void Labels_AreNotSizedByAFixedWidth()
            {
            var labels = Pages()
                .SelectMany(p => p.Document.Descendants().Where(e => e.Name.LocalName == "Label" && e.Attribute("Width") != null).Select(e => (p.File, Element: e)))
                .Select(l => $"{l.File}: {l.Element.Attribute("Content")?.Value}");

            Assert.That(labels, Is.Empty, "a label column is as wide as its widest label: put the labels into one SharedSizeGroup instead of a fixed Width");
            }

        [Test]
        public void TemplatesThatReserveRoomForSeveralTexts_CenterTheShownText()
            {
            // A button that is as wide as its longest text holds the invisible copies of every text and the shown one in one cell:
            // the shown one must be centered, or a shorter text sticks to the left edge.
            var uncentered = Pages()
                .SelectMany(p => p.Document.Descendants().Where(e => e.Name.LocalName == "DataTemplate").Select(e => (p.File, Template: e)))
                .Where(t => t.Template.Descendants().Any(e => (string?)e.Attribute("Visibility") == "Hidden"))
                .SelectMany(t => t.Template.Descendants().Where(e => e.Parent?.Name.LocalName == "Grid" && e.Parent.Parent == t.Template
                    && (string?)e.Attribute("Visibility") != "Hidden"
                    && (string?)e.Attribute("HorizontalAlignment") != "Center")
                    .Select(e => $"{t.File}: {e.Name.LocalName} {e.Attribute("Text")?.Value}"));

            Assert.That(uncentered, Is.Empty, "the shown text of a template with reserved room needs HorizontalAlignment=\"Center\"");
            }

        /// <summary>The resource keys of the invisible texts a template reserves room for.</summary>
        private static HashSet<string> ReservedKeys(XElement template) =>
            template.Descendants()
                .Where(e => (string?)e.Attribute("Visibility") == "Hidden")
                .SelectMany(e => e.DescendantsAndSelf().Select(x => (string?)x.Attribute("Text")))
                .Select(text => System.Text.RegularExpressions.Regex.Match(text ?? "", @"^\{DynamicResource (\w+)\}$"))
                .Where(m => m.Success)
                .Select(m => m.Groups[1].Value)
                .ToHashSet();

        /// <summary>The content template a button uses: its own, or the one of its style.</summary>
        private static XElement? ContentTemplateOf(XDocument page, XElement button)
            {
            static string? Key(string? reference) =>
                System.Text.RegularExpressions.Regex.Match(reference ?? "", @"^\{StaticResource (\w+)\}$") is { Success: true } m ? m.Groups[1].Value : null;

            XElement? Resource(string? key) => key == null ? null : page.Descendants().FirstOrDefault(e => e.Attributes().Any(a => a.Name.LocalName == "Key" && a.Value == key));

            if (Resource(Key((string?)button.Attribute("ContentTemplate"))) is { } own) return own;

            var style = Resource(Key((string?)button.Attribute("Style")));
            return style?.Descendants().FirstOrDefault(e => e.Name.LocalName == "Setter" && (string?)e.Attribute("Property") == "ContentTemplate")
                ?.Descendants().FirstOrDefault(e => e.Name.LocalName == "DataTemplate");
            }

        [Test]
        public void ButtonsThatShowAnOutcome_ReserveRoomForEveryTextTheyCanShow()
            {
            // The label, the outcome that follows a success and the failure and the cancel texts: the width of the button must not change
            // when the text does, so the button is as wide as the longest of them.
            var missing = new List<string>();
            foreach (var (file, document) in Pages())
                {
                foreach (var button in document.Descendants().Where(e => e.Name.LocalName == "Button" && e.Attributes().Any(a => a.Name.LocalName == "ActionFeedbackBehavior.SuccessText")))
                    {
                    string success = button.Attributes().First(a => a.Name.LocalName == "ActionFeedbackBehavior.SuccessText").Value;
                    string name = (string?)button.Attributes().FirstOrDefault(a => a.Name.LocalName == "Name") ?? success;
                    var template = ContentTemplateOf(document, button);
                    var reserved = template == null ? [] : ReservedKeys(template);

                    var expected = new List<string> { success, "FeedbackFailed", "FeedbackCanceled" };
                    string? label = System.Text.RegularExpressions.Regex.Match((string?)button.Attribute("Content") ?? "", @"^\{DynamicResource (\w+)\}$") is { Success: true } m ? m.Groups[1].Value : null;
                    if (label != null) expected.Add(label);

                    missing.AddRange(expected.Where(key => !reserved.Contains(key)).Select(key => $"{file}: {name} does not reserve room for {key}"));
                    }
                }

            Assert.That(missing, Is.Empty, "give the button a ContentTemplate with an invisible copy of every text it can show");
            }

        [Test]
        public void LabelsOfFields_ShareOneColumnWidthPerPanel()
            {
            // The fields of a panel start at one x: its labels sit in a column whose width is shared, so it is as wide as the widest label.
            var loose = Pages()
                .SelectMany(p => p.Document.Descendants().Where(e => e.Name.LocalName == "Label").Select(e => (p.File, Element: e)))
                .Where(l => ((string?)l.Element.Attribute("Content") ?? "").StartsWith('{') && !((string?)l.Element.Attribute("Content"))!.Contains("DynamicResource Unit"))  // a unit after a field is not a label
                .Where(l => l.Element.Parent is not { Name.LocalName: "Grid" } grid
                    || !grid.Descendants().Any(d => d.Name.LocalName == "ColumnDefinition" && d.Attribute("SharedSizeGroup") != null))
                .Select(l => $"{l.File}: {l.Element.Attribute("Content")?.Value}");

            Assert.That(loose, Is.Empty, "wrap the label in a Grid with a ColumnDefinition SharedSizeGroup (and Grid.IsSharedSizeScope on the panel)");
            }

        [Test]
        public void DropDownLists_AreNotSizedByAFixedWidth()
            {
            var fixedWidth = Pages()
                .SelectMany(p => p.Document.Descendants().Where(e => e.Name.LocalName == "ComboBox" && e.Attribute("Width") != null).Select(e => (p.File, Element: e)))
                .Select(l => $"{l.File}: {l.Element.Attribute("ItemsSource")?.Value}");

            Assert.That(fixedWidth, Is.Empty, "a drop-down list is as wide as its widest item: leave its Width alone");
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
