// WPF_LCD_Test.UnitTests/ServicesTests/MeasurementStatusServiceTests.cs

using System.Linq;
using WPF_LCD_Test.Models;
using WPF_LCD_Test.Services;
using NUnit.Framework;

namespace WPF_LCD_Test.UnitTests.ServicesTests
{
    [TestFixture]
    public class MeasurementStatusServiceTests
    {
        private MeasurementStatusService _service;

        [SetUp]
        public void Setup()
        {
            _service = MeasurementStatusService.Instance;
        }

        [Test]
        public void Instance_ReturnsSingletonInstance()
        {
            var instance1 = MeasurementStatusService.Instance;
            var instance2 = MeasurementStatusService.Instance;

            Assert.That(instance1, Is.SameAs(instance2));
            Assert.That(instance1, Is.InstanceOf<MeasurementStatusService>());
        }

        [Test]
        public void AllStatuses_AreInitialized_ForAllEnumValues()
        {
            var allLocations = Enum.GetValues<MeasurementLocation>();
            foreach (var location in allLocations)
            {
                var status = _service.GetStatus(location);
                Assert.NotNull(status);
                Assert.AreEqual(location.ToString(), status.Location);
            }
        }

        [Test]
        public void AllMeasurementButtonStatuses_ContainsAllStatuses()
        {
            var allLocations = Enum.GetValues<MeasurementLocation>().Select(l => l.ToString()).ToList();
            var collectionLocations = _service.AllMeasurementButtonStatuses.Select(s => s.Location).ToList();

            Assert.AreEqual(allLocations.Count, collectionLocations.Count);
            foreach (var loc in allLocations)
                Assert.Contains(loc, collectionLocations);
        }

        [Test]
        public void GetStatus_ReturnsCorrectStatus()
        {
            var status = _service.GetStatus(MeasurementLocation.BlueColor);
            Assert.NotNull(status);
            Assert.AreEqual(MeasurementLocation.BlueColor.ToString(), status.Location);
        }

        [Test]
        public void InitialStatus_IsPassedIsNullAndValuesAreEmpty()
        {
            var status = _service.GetStatus(MeasurementLocation.TopLeft);
            Assert.NotNull(status);
            Assert.IsNull(status.IsPassed);
            Assert.IsEmpty(status.MeasuredValuesString);
        }

        [Test]
        public void SetIsPassed_UpdatesStatus()
        {
            var status = _service.GetStatus(MeasurementLocation.Center);
            Assert.NotNull(status);

            status.IsPassed = true;
            Assert.IsTrue(status.IsPassed);

            status.IsPassed = false;
            Assert.IsFalse(status.IsPassed);

            status.IsPassed = null;
            Assert.IsNull(status.IsPassed);
        }

        [Test]
        public void SetMeasuredValuesString_UpdatesValue()
        {
            var status = _service.GetStatus(MeasurementLocation.BottomLeft);
            Assert.NotNull(status);

            string expectedValues = "1.23, 4.56, 7.89";
            status.MeasuredValuesString = expectedValues;
            Assert.AreEqual(expectedValues, status.MeasuredValuesString);
        }
    }
}