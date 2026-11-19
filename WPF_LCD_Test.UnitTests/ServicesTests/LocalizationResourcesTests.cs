using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace WPF_LCD_Test.UnitTests.ServicesTests
    {
    /// <summary>
    /// Keeps the three languages of the interface (English, Chinese, Russian) in step: the same keys, the same placeholders,
    /// no text that is written into a page instead of a resource.
    /// </summary>
    [TestFixture]
    public class LocalizationResourcesTests
        {
        private static readonly string[] Languages = ["zh-Hans", "ru"];
        private static readonly Regex Placeholder = new(@"\{\d+\}");
        private static readonly XNamespace XamlKey = "http://schemas.microsoft.com/winfx/2006/xaml";

        /// <summary>Texts that are the same in every language on purpose (a name of a language, a unit, a label made of numbers).</summary>
        private static readonly HashSet<string> SameEverywhere = ["English", "Chinese", "Russian", "DialogOk", "ProbeSerialNumberLabel", "SpecMetric_Delta_e", "UnitHz", "UnitMs", "ErrCSV"];

        private static string ProjectFolder([CallerFilePath] string testFile = "") =>
            Path.GetFullPath(Path.Combine(Path.GetDirectoryName(testFile)!, "..", "..", "WPF LCD Test"));

        private static string ResourceFile(string name) => Path.Combine(ProjectFolder(), "Resources", name);

        private static Dictionary<string, string> Resx(string language)
            {
            var file = ResourceFile(language == "" ? "Resources.resx" : $"Resources.{language}.resx");
            return XDocument.Load(file).Root!.Elements("data").ToDictionary(d => (string)d.Attribute("name")!, d => (string)d.Element("value")!);
            }

        private static Dictionary<string, string> Xaml(string language)
            {
            var file = ResourceFile(language == "" ? "StringResources.xaml" : $"StringResources.{language}.xaml");
            return XDocument.Load(file).Root!.Elements().Where(e => e.Attribute(XamlKey + "Key") != null)
                .ToDictionary(e => (string)e.Attribute(XamlKey + "Key")!, e => e.Value.Trim());
            }

        private static string Placeholders(string text) => string.Join(",", Placeholder.Matches(text).Select(m => m.Value).Distinct().OrderBy(p => p));

        [TestCaseSource(nameof(Languages))]
        public void Resx_HasTheKeysOfTheNeutralFile(string language)
            {
            var neutral = Resx("");
            var translated = Resx(language);

            Assert.That(neutral.Keys.Except(translated.Keys), Is.Empty, $"{language}: keys without a translation");
            Assert.That(translated.Keys.Except(neutral.Keys), Is.Empty, $"{language}: keys the neutral file does not have");
            }

        [TestCaseSource(nameof(Languages))]
        public void Xaml_HasTheKeysOfTheEnglishDictionary(string language)
            {
            var english = Xaml("");
            var translated = Xaml(language);

            Assert.That(english.Keys.Except(translated.Keys), Is.Empty, $"{language}: keys without a translation");
            Assert.That(translated.Keys.Except(english.Keys), Is.Empty, $"{language}: keys the English dictionary does not have");
            }

        [TestCaseSource(nameof(Languages))]
        public void Resx_TranslationsKeepThePlaceholdersOfTheEnglishText(string language)
            {
            var neutral = Resx("");
            var translated = Resx(language);

            var broken = neutral.Where(e => translated.TryGetValue(e.Key, out var text) && Placeholders(text) != Placeholders(e.Value)).Select(e => e.Key);

            Assert.That(broken, Is.Empty, $"{language}: a format placeholder is missing, new or in the wrong set");
            }

        [TestCaseSource(nameof(Languages))]
        public void Resx_EveryTextIsTranslated(string language)
            {
            var neutral = Resx("");
            var translated = Resx(language);

            var same = neutral
                .Where(e => translated.TryGetValue(e.Key, out var text) && text == e.Value && !SameEverywhere.Contains(e.Key) && e.Value.Any(char.IsLetter) && e.Value.Length > 3)
                .Select(e => e.Key);

            Assert.That(same, Is.Empty, $"{language}: texts that are still English");
            }

        [Test]
        public void Russian_IsFoundByItsCultureCode()
            {
            string russian = Resources.Resources.ResourceManager.GetString("Err", new CultureInfo("ru"))!;
            string english = Resources.Resources.ResourceManager.GetString("Err", CultureInfo.InvariantCulture)!;

            Assert.That(russian, Is.EqualTo("Ошибка"));
            Assert.That(english, Is.EqualTo("Error"));
            }

        [Test]
        public void Pages_HaveNoTextWrittenIntoThem()
            {
            // A visible text of a page is a resource; what stays is a brand, the symbols of the values and a tick mark.
            var allowed = new HashSet<string> { "Screen Checker", "x", "y", "Lv", "T", "✓" };
            var literal = new Regex(@"^[^{]*[\p{L}✓]");
            var visible = new[] { "Text", "Content", "ToolTip", "Header", "Title", "Watermark" };
            var found = Directory.GetFiles(Path.Combine(ProjectFolder(), "Views"), "*.xaml")
                .SelectMany(file => XDocument.Load(file).Descendants()
                    .SelectMany(e => e.Attributes().Where(a => a.Name.NamespaceName == "" && visible.Contains(a.Name.LocalName))
                        .Select(a => (File: Path.GetFileName(file), a.Name.LocalName, a.Value))))
                .Where(a => literal.IsMatch(a.Value) && !allowed.Contains(a.Value))
                .Select(a => $"{a.File}: {a.LocalName}=\"{a.Value}\"");

            Assert.That(found, Is.Empty, "put the text into StringResources.xaml (and its Chinese and Russian versions) and bind it with DynamicResource");
            }
        }
    }
