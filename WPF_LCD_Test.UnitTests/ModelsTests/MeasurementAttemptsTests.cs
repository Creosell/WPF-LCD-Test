using System.Text.Json;
using WPF_LCD_Test.Models;

namespace WPF_LCD_Test.UnitTests.ModelsTests
    {
    [TestFixture]
    public class MeasurementAttemptsTests
        {
        private static Measurement Attempt(string location, double lv, double t = 6500) => new(location, 0.3127, 0.3290, lv, t);

        [Test]
        public void RecordAttempt_FirstMeasurementIsTheOnlyAttempt()
            {
            var device = new DeviceUnderTest("SN1");

            device.RecordAttempt(Attempt("Center", 100));

            Assert.That(device.Measurements.Single().Lv, Is.EqualTo(100));
            Assert.That(device.AttemptCount("Center"), Is.EqualTo(1));
            Assert.That(device.AttemptCount("TopLeft"), Is.EqualTo(0));
            }

        [Test]
        public void RecordAttempt_KeepsEveryAttemptAndTheBrightestIsBestUntilTheCheckChooses()
            {
            var device = new DeviceUnderTest("SN1");

            device.RecordAttempt(Attempt("Center", 150));
            device.RecordAttempt(Attempt("Center", 180));
            device.RecordAttempt(Attempt("Center", 170));

            var entry = device.Measurements.Single();
            Assert.That(entry.Attempts.Select(a => a.Lv), Is.EqualTo(new[] { 150.0, 180.0, 170.0 }));
            Assert.That((entry.Lv, entry.BestAttemptIndex), Is.EqualTo((180.0, 1)));
            }

        [Test]
        public void RecordAttempt_AfterALegacyMeasurementCountsItAsTheFirstAttempt()
            {
            var device = new DeviceUnderTest("SN1");
            device.AddMeasurement(Attempt("Center", 100));

            device.RecordAttempt(Attempt("Center", 90));

            Assert.That(device.AttemptCount("Center"), Is.EqualTo(2));
            }

        [Test]
        public void SelectBestAttempt_SwitchesTheValuesOfThePointAndIgnoresABadIndex()
            {
            var device = new DeviceUnderTest("SN1");
            device.RecordAttempt(Attempt("Center", 150));
            device.RecordAttempt(Attempt("Center", 180));

            device.SelectBestAttempt("Center", 0);
            Assert.That(device.Measurements.Single().Lv, Is.EqualTo(150));

            device.SelectBestAttempt("Center", 5);
            device.SelectBestAttempt("Missing", 0);
            Assert.That(device.Measurements.Single().Lv, Is.EqualTo(150));
            Assert.That(device.AttemptCount("Center"), Is.EqualTo(2));
            }

        [Test]
        public void Json_WritesAttemptsOnlyForPointsMeasuredSeveralTimes()
            {
            var device = new DeviceUnderTest("SN1");
            device.RecordAttempt(Attempt("TopLeft", 100));
            device.RecordAttempt(Attempt("Center", 150));
            device.RecordAttempt(Attempt("Center", 180, t: double.PositiveInfinity));
            device.SelectBestAttempt("Center", 0);

            using var json = JsonDocument.Parse(JsonSerializer.Serialize(device));
            var measurements = json.RootElement.GetProperty("Measurements");

            Assert.That(measurements[0].TryGetProperty("Attempts", out _), Is.False);
            var center = measurements[1];
            Assert.That(center.GetProperty("Lv").GetDouble(), Is.EqualTo(150.0));
            var attempts = center.GetProperty("Attempts");
            Assert.That(attempts.GetArrayLength(), Is.EqualTo(2));
            Assert.That(attempts[0].GetProperty("Best").GetBoolean(), Is.True);
            Assert.That(attempts[1].GetProperty("Best").GetBoolean(), Is.False);
            Assert.That(attempts[1].GetProperty("T").ValueKind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(attempts[0].GetProperty("Time").GetString(), Does.Match(@"^\d{8}_\d{6}$"));
            }
        }
    }
