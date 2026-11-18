using WPF_LCD_Test.Models;

namespace WPF_LCD_Test.UnitTests.ModelsTests
    {
    [TestFixture]
    public class SpecEvaluationTests
        {
        private const string Json = """
            {"schema": 1, "device": "WARN", "complete": false, "checks": [
              {"key": "Brightness", "min": 320, "typ": 360, "max": null, "locations": ["WhiteColor"],
               "value": 348, "status": "warning", "missing": []},
              {"key": "Contrast", "min": 4125, "typ": 5500, "max": null, "locations": ["WhiteColor", "BlackColor"],
               "value": null, "status": "pending", "missing": ["BlackColor"]},
              {"key": "Red_x", "min": 0.612, "typ": 0.672, "max": 0.732, "locations": ["RedColor"],
               "value": 0.701, "status": "pass", "missing": []},
              {"key": "Temperature", "min": 7300, "typ": null, "max": 11300, "locations": ["Center"],
               "value": null, "status": "n/a", "missing": []}
            ]}
            """;

        [Test]
        public void Parse_ReadsVerdictCompletenessAndChecks()
            {
            var evaluation = SpecEvaluation.Parse(Json);

            Assert.That(evaluation.Verdict, Is.EqualTo(SpecVerdict.Warn));
            Assert.That(evaluation.Complete, Is.False);
            Assert.That(evaluation.Checks.Select(c => c.Key), Is.EqualTo(new[] { "Brightness", "Contrast", "Red_x", "Temperature" }));
            Assert.That(evaluation.Checks.Select(c => c.Status), Is.EqualTo(new[] { SpecStatus.Warning, SpecStatus.Pending, SpecStatus.Pass, SpecStatus.Unavailable }));
            }

        [Test]
        public void Parse_ReadsBoundsValuesAndLocations()
            {
            var checks = SpecEvaluation.Parse(Json).Checks;

            Assert.That(checks[0], Has.Property("Value").EqualTo(348.0).And.Property("Min").EqualTo(320.0).And.Property("Typ").EqualTo(360.0));
            Assert.That(checks[0].Max, Is.Null);
            Assert.That(checks[1].Value, Is.Null);
            Assert.That(checks[1].Locations, Is.EqualTo(new[] { "WhiteColor", "BlackColor" }));
            Assert.That(checks[1].Missing, Is.EqualTo(new[] { "BlackColor" }));
            Assert.That(checks[2].Value, Is.EqualTo(0.701));
            }

        [Test]
        public void Parse_ReadsTheAttemptSummaryOfThePoints()
            {
            var evaluation = SpecEvaluation.Parse("""
                {"schema": 1, "device": "PASS", "complete": true, "checks": [],
                 "attempts": {"Center": {"count": 3, "best": 1, "deviated": true}, "RedColor": {"count": 1, "best": 0, "deviated": false}}}
                """);

            Assert.That(evaluation.Attempts["Center"], Is.EqualTo(new SpecAttemptInfo(3, 1, true)));
            Assert.That(evaluation.Attempts["RedColor"], Is.EqualTo(new SpecAttemptInfo(1, 0, false)));
            }

        [Test]
        public void Parse_WithoutAttemptsLeavesThemEmpty()
            {
            Assert.That(SpecEvaluation.Parse(Json).Attempts, Is.Empty);
            }

        [Test]
        public void Parse_ErrorObject_Throws()
            {
            var ex = Assert.Throws<FormatException>(() => SpecEvaluation.Parse("""{"schema": 1, "error": "spec not found: X"}"""));

            Assert.That(ex!.Message, Is.EqualTo("spec not found: X"));
            }

        [TestCase("not json")]
        [TestCase("{}")]
        [TestCase("""{"device": "PASS", "complete": true, "checks": [{"key": "A", "status": "bogus"}]}""")]
        [TestCase("""{"device": "MAYBE", "complete": true, "checks": []}""")]
        public void Parse_UnexpectedContent_ThrowsFormatException(string json)
            {
            Assert.Throws<FormatException>(() => SpecEvaluation.Parse(json));
            }
        }
    }
