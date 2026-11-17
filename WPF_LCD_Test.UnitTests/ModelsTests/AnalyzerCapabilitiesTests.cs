using WPF_LCD_Test.Models;

namespace WPF_LCD_Test.UnitTests.ModelsTests
    {
    [TestFixture]
    public class AnalyzerCapabilitiesTests
        {
        [Test]
        public void Ca310_DoesNotSupportManualSync()
            {
            Assert.That(AnalyzerCapabilities.GetSyncModes(ColorAnalyzerModel.CA310), Does.Not.Contain(SyncMode.Manual));
            Assert.That(AnalyzerCapabilities.GetSyncModes(ColorAnalyzerModel.CA410), Does.Contain(SyncMode.Manual));
            }

        [TestCase(ColorAnalyzerModel.CA310, 39.9, false)]
        [TestCase(ColorAnalyzerModel.CA310, 40.0, true)]
        [TestCase(ColorAnalyzerModel.CA310, 200.0, true)]
        [TestCase(ColorAnalyzerModel.CA310, 200.1, false)]
        [TestCase(ColorAnalyzerModel.CA410, 0.5, true)]
        [TestCase(ColorAnalyzerModel.CA410, 240.0, true)]
        [TestCase(ColorAnalyzerModel.CA410, 240.1, false)]
        public void InternalSync_IsCheckedAgainstTheFrequencyRangeOfTheModel(ColorAnalyzerModel model, double frequency, bool expected)
            {
            Assert.That(AnalyzerCapabilities.IsSupported(model, new SyncSetting(SyncMode.Internal, frequency)), Is.EqualTo(expected));
            }

        [TestCase(3.9, false)]
        [TestCase(4.0, true)]
        [TestCase(4000.0, true)]
        [TestCase(4000.1, false)]
        public void ManualSync_IsCheckedAgainstTheIntegrationTimeRange(double time, bool expected)
            {
            Assert.That(AnalyzerCapabilities.IsSupported(ColorAnalyzerModel.CA410, new SyncSetting(SyncMode.Manual, time)), Is.EqualTo(expected));
            }

        [Test]
        public void Ca310_RejectsManualSyncEvenWithAValidTime()
            {
            Assert.That(AnalyzerCapabilities.IsSupported(ColorAnalyzerModel.CA310, new SyncSetting(SyncMode.Manual, 100)), Is.False);
            }
        }
    }
