using System.Globalization;
using WPF_LCD_Test.Models;
using WPF_LCD_Test.Services;
using WPF_LCD_Test.ViewModels;

namespace WPF_LCD_Test.UnitTests.ViewModelsTests
    {
    [TestFixture]
    public class SpecSummaryViewModelTests
        {
        private MeasurementStatusService _statuses;
        private List<string> _log;
        private List<LogSeverity> _severities;
        private SpecSummaryViewModel _summary;
        private CultureInfo _culture;

        [SetUp]
        public void Setup()
            {
            _culture = CultureInfo.CurrentUICulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
            _statuses = new MeasurementStatusService();
            _log = [];
            _severities = [];
            _summary = new SpecSummaryViewModel(_statuses, (message, severity) =>
                {
                _log.Add(message);
                _severities.Add(severity);
                });
            }

        [TearDown]
        public void TearDown() => CultureInfo.CurrentUICulture = _culture;

        private static SpecCheck Check(string key, double? value, SpecStatus status, double? min = null, double? typ = null, double? max = null, params string[] locations) =>
            new(key, value, min, typ, max, status, locations, status == SpecStatus.Pending ? locations : []);

        private static SpecEvaluation Evaluation(SpecVerdict verdict, bool complete, params SpecCheck[] checks) => new(verdict, complete, checks);

        private MeasurementStatus Status(MeasurementLocation location) => _statuses.GetStatus(location);

        [Test]
        public void Show_ListsMetricsWithoutCoordinatesAndFormatsThem()
            {
            _summary.Show(Evaluation(SpecVerdict.Warn, false,
                Check("Brightness", 348, SpecStatus.Warning, 320, 360, null, "WhiteColor"),
                Check("Contrast", null, SpecStatus.Pending, 4125, 5500, null, "WhiteColor", "BlackColor"),
                Check("Red_x", 0.701, SpecStatus.Pass, 0.612, 0.672, 0.732, "RedColor")), 3, 14);

            Assert.That(_summary.Rows.Select(r => r.Name), Is.EqualTo(new[] { "Brightness (cd/m²)", "Contrast ratio" }));
            Assert.That(_summary.Rows[0].ValueText, Is.EqualTo("348"));
            Assert.That(_summary.Rows[0].SpecText, Is.EqualTo("min 320 · typ 360"));
            Assert.That(_summary.Rows[1].ValueText, Is.EqualTo("—"));
            Assert.That(_summary.Rows[1].ToolTip, Does.Contain("White").And.Contain("Black"));
            Assert.That(_summary.ProgressText, Is.EqualTo("3 of 14 points"));
            }

        [Test]
        public void Show_RowsWithNothingToAddHaveNoToolTip()
            {
            _summary.Show(Evaluation(SpecVerdict.Pass, true,
                Check("Brightness", 400, SpecStatus.Pass, 320, 360, null, "Center"),
                Check("Temperature", null, SpecStatus.Unavailable, 7300, null, 11300, "Center")), 1, 13);

            Assert.That(_summary.Rows.Select(r => r.ToolTip), Is.All.Null);
            }

        [TestCase(SpecVerdict.Fail, true, "Spec: FAIL")]
        [TestCase(SpecVerdict.Warn, false, "Spec: WARN")]
        [TestCase(SpecVerdict.Pass, true, "Spec: PASS")]
        [TestCase(SpecVerdict.Pass, false, "Spec: no issues yet")]
        public void Show_BuildsTheVerdictText(SpecVerdict verdict, bool complete, string expected)
            {
            _summary.Show(Evaluation(verdict, complete), 0, 13);

            Assert.That(_summary.VerdictText, Is.EqualTo(expected));
            Assert.That(_summary.Verdict, Is.EqualTo(verdict));
            }

        [Test]
        public void Show_NamesGamutMetricsBySpaceMethodAndDiagram()
            {
            _summary.Show(Evaluation(SpecVerdict.Pass, true,
                Check("Cg_rgb_area", 104.4, SpecStatus.Pass, 98, 100, null, "RedColor", "GreenColor", "BlueColor"),
                Check("Cg_dcip3_uv", 90, SpecStatus.Pass, 88, null, null, "RedColor", "GreenColor", "BlueColor")), 3, 13);

            Assert.That(_summary.Rows.Select(r => r.Name), Is.EqualTo(new[] { "sRGB gamut area, CIE 1931", "DCI-P3 gamut coverage, CIE 1976" }));
            }

        [Test]
        public void Show_PutsBadgesOnSinglePointChecksOnly()
            {
            Status(MeasurementLocation.WhiteColor).IsPassed = true;
            Status(MeasurementLocation.RedColor).IsPassed = true;
            Status(MeasurementLocation.BlackColor).IsPassed = true;

            _summary.Show(Evaluation(SpecVerdict.Fail, true,
                Check("Brightness", 348, SpecStatus.Warning, 320, 360, null, "WhiteColor"),
                Check("Red_x", 0.701, SpecStatus.Fail, 0.612, 0.672, 0.689, "RedColor"),
                Check("Contrast", 4789, SpecStatus.Fail, 5000, null, null, "WhiteColor", "BlackColor")), 3, 14);

            Assert.That(Status(MeasurementLocation.WhiteColor).SpecBadge, Is.EqualTo(SpecStatus.Warning));
            Assert.That(Status(MeasurementLocation.RedColor).SpecBadge, Is.EqualTo(SpecStatus.Fail));
            Assert.That(Status(MeasurementLocation.BlackColor).SpecBadge, Is.EqualTo(SpecStatus.Pass), "a check of two points is shown in the table only");
            }

        [Test]
        public void Show_BadgeTooltipListsFailuresFirstAndSaysWhatIsWrong()
            {
            Status(MeasurementLocation.Center).IsPassed = true;

            _summary.Show(Evaluation(SpecVerdict.Fail, true,
                Check("Brightness", 348, SpecStatus.Warning, 320, 360, null, "Center"),
                Check("Temperature", 11500, SpecStatus.Fail, 7300, null, 11300, "Center")), 1, 13);

            var status = Status(MeasurementLocation.Center);
            Assert.That(status.SpecRemark, Is.EqualTo(SpecStatus.Fail));
            Assert.That(status.SpecTooltip.Split(Environment.NewLine), Is.EqualTo(new[]
                {
                "Color temperature (K) 11500 > max 11300",
                "Brightness (cd/m²) 348 < typ 360"
                }));
            }

        [Test]
        public void Badge_IsHiddenWhileThePointHoldsNoAcceptedMeasurement()
            {
            var center = Status(MeasurementLocation.Center);
            center.IsPassed = true;
            _summary.Show(Evaluation(SpecVerdict.Fail, true, Check("Brightness", 1, SpecStatus.Fail, 320, null, null, "Center")), 1, 13);
            Assert.That(center.SpecBadge, Is.EqualTo(SpecStatus.Fail));

            center.IsPassed = null;
            Assert.That(center.SpecBadge, Is.EqualTo(SpecStatus.Pass), "while measuring");

            center.IsPassed = false;
            Assert.That(center.SpecBadge, Is.EqualTo(SpecStatus.Pass), "after a failed measurement");

            center.IsPassed = true;
            Assert.That(center.SpecBadge, Is.EqualTo(SpecStatus.Fail), "after an accepted one the remark is shown again");
            }

        [Test]
        public void Show_ClearsBadgesOfPointsThatNoLongerHaveRemarks()
            {
            Status(MeasurementLocation.Center).IsPassed = true;
            _summary.Show(Evaluation(SpecVerdict.Fail, true, Check("Brightness", 1, SpecStatus.Fail, 320, null, null, "Center")), 1, 13);

            _summary.Show(Evaluation(SpecVerdict.Pass, true, Check("Brightness", 400, SpecStatus.Pass, 320, null, null, "Center")), 1, 13);

            Assert.That(Status(MeasurementLocation.Center).SpecRemark, Is.EqualTo(SpecStatus.Pass));
            Assert.That(Status(MeasurementLocation.Center).SpecTooltip, Is.Empty);
            }

        [Test]
        public void Show_LogsADeviationOnlyWhenItIsNewOrChanged()
            {
            var warning = Evaluation(SpecVerdict.Warn, false, Check("Brightness", 348, SpecStatus.Warning, 320, 360, null, "Center"));
            var failure = Evaluation(SpecVerdict.Fail, false, Check("Brightness", 300, SpecStatus.Fail, 320, 360, null, "Center"));

            _summary.Show(warning, 1, 13);
            _summary.Show(warning, 2, 13);
            _summary.Show(failure, 2, 13);

            Assert.That(_log, Is.EqualTo(new[] { "Spec: Brightness (cd/m²) 348 < typ 360", "Spec: Brightness (cd/m²) 300 < min 320. Repeating the measurement is recommended" }));
            }

        [Test]
        public void Show_LogsWarningsAndFailuresWithTheirSeverity()
            {
            _summary.Show(Evaluation(SpecVerdict.Fail, false,
                Check("Brightness", 348, SpecStatus.Warning, 320, 360, null, "Center"),
                Check("Red_x", 0.701, SpecStatus.Fail, 0.612, 0.672, 0.689, "RedColor")), 2, 13);

            Assert.That(_severities, Is.EqualTo(new[] { LogSeverity.Warning, LogSeverity.Error }));
            }

        [Test]
        public void Show_DoesNotLogPassingOrPendingChecks()
            {
            _summary.Show(Evaluation(SpecVerdict.Pass, false,
                Check("Brightness", 400, SpecStatus.Pass, 320, 360, null, "Center"),
                Check("Contrast", null, SpecStatus.Pending, 4000, null, null, "WhiteColor", "BlackColor")), 1, 13);

            Assert.That(_log, Is.Empty);
            }

        [Test]
        public void ShowMessage_ReplacesTheTableAndTheBadges()
            {
            Status(MeasurementLocation.Center).IsPassed = true;
            _summary.Show(Evaluation(SpecVerdict.Fail, true, Check("Brightness", 1, SpecStatus.Fail, 320, null, null, "Center")), 1, 13);

            _summary.ShowMessage("Select a configuration");

            Assert.That(_summary.Rows, Is.Empty);
            Assert.That(_summary.VerdictText, Is.Empty);
            Assert.That(_summary.HintText, Is.EqualTo("Select a configuration"));
            Assert.That(Status(MeasurementLocation.Center).SpecRemark, Is.EqualTo(SpecStatus.Pass));
            Assert.That(_summary.Evaluation, Is.Null);
            }

        [Test]
        public void Reset_ForgetsLoggedDeviationsSoTheyAreReportedAgain()
            {
            var failure = Evaluation(SpecVerdict.Fail, false, Check("Brightness", 300, SpecStatus.Fail, 320, null, null, "Center"));
            _summary.Show(failure, 1, 13);

            _summary.Reset();
            _summary.Show(failure, 1, 13);

            Assert.That(_log, Has.Count.EqualTo(2));
            }

        [Test]
        public void DescribeProblems_ListsFailuresBeforeWarningsAndNamesThePointsWithTheirAttempts()
            {
            _summary.Show(Evaluation(SpecVerdict.Fail, true,
                Check("Brightness", 348, SpecStatus.Warning, 320, 360, null, "Center"),
                Check("Red_x", 0.701, SpecStatus.Fail, 0.612, 0.672, 0.689, "RedColor"),
                Check("Contrast", 4789, SpecStatus.Warning, 4125, 5500, null, "WhiteColor", "BlackColor"),
                Check("Brightness_uniformity", 61, SpecStatus.Pass, 65, 70, null, "TopLeft")) with
                {
                Attempts = new Dictionary<string, SpecAttemptInfo>
                    {
                    ["Center"] = new(2, 1, true),
                    ["RedColor"] = new(3, 0, true)
                    }
                }, 2, 13);

            var problems = _summary.DescribeProblems();

            Assert.That(problems.Select(p => p.Text), Is.EqualTo(new[]
                {
                "Red (x) 0.701 > max 0.689   (Red, 3 of 3)",
                "Brightness (cd/m²) 348 < typ 360   (Center, 2 of 3)",
                "Contrast ratio 4789 < typ 5500"
                }));
            Assert.That(problems.Select(p => p.FewAttempts), Is.EqualTo(new[] { false, true, false }));
            Assert.That(problems.Select(p => p.Status), Is.EqualTo(new[] { SpecStatus.Fail, SpecStatus.Warning, SpecStatus.Warning }));
            }

        [Test]
        public void DescribeProblems_WithoutAttemptInformationDoesNotInventAttempts()
            {
            _summary.Show(Evaluation(SpecVerdict.Warn, true, Check("Brightness", 348, SpecStatus.Warning, 320, 360, null, "Center")), 1, 13);

            var problem = _summary.DescribeProblems().Single();

            Assert.That(problem.Text, Is.EqualTo("Brightness (cd/m²) 348 < typ 360"));
            Assert.That(problem.FewAttempts, Is.False);
            }

        [Test]
        public void Show_LogLineOfASinglePointCheckNamesThePointAndTheAttempt()
            {
            _summary.Show(Evaluation(SpecVerdict.Fail, false, Check("Brightness", 251, SpecStatus.Fail, 290, 300, null, "Center")) with
                {
                Attempts = new Dictionary<string, SpecAttemptInfo> { ["Center"] = new(1, 0, true) }
                }, 1, 13);

            Assert.That(_log.Single(), Is.EqualTo("Spec: Center: Brightness (cd/m²) 251 < min 290 (attempt 1). Repeating the measurement is recommended"));
            }

        [Test]
        public void Show_LogsTheSameRemarkAgainWhenThePointGetsAnotherAttempt()
            {
            var first = Evaluation(SpecVerdict.Warn, false, Check("Brightness", 280, SpecStatus.Warning, 250, 300, null, "Center")) with
                {
                Attempts = new Dictionary<string, SpecAttemptInfo> { ["Center"] = new(1, 0, true) }
                };
            var second = Evaluation(SpecVerdict.Warn, false, Check("Brightness", 285, SpecStatus.Warning, 250, 300, null, "Center")) with
                {
                Attempts = new Dictionary<string, SpecAttemptInfo> { ["Center"] = new(2, 1, true) }
                };

            _summary.Show(first, 1, 13);
            _summary.Show(first, 1, 13);
            _summary.Show(second, 1, 13);

            Assert.That(_log, Is.EqualTo(new[]
                {
                "Spec: Center: Brightness (cd/m²) 280 < typ 300 (attempt 1)",
                "Spec: Center: Brightness (cd/m²) 285 < typ 300 (attempt 2)"
                }));
            }

        [Test]
        public void LowerIsBetterMetric_WarnsAboveTyp()
            {
            _summary.Show(Evaluation(SpecVerdict.Warn, true, Check("Delta_e", 3.5, SpecStatus.Warning, null, 3, 4, "Center")), 1, 13);

            Assert.That(_log.Single(), Is.EqualTo("Spec: ΔE 3.5 > typ 3"));
            }
        }
    }
