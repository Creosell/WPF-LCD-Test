// В проекте WPF_LCD_Test.UnitTests (папка ModelsTests)
// Файл DeviceUnderTestTests.cs

using WPF_LCD_Test.Models; // Для DeviceUnderTest и Measurement
using static WPF_LCD_Test.Resources.Resources; // Для доступа к SnCantBeEmpty

namespace WPF_LCD_Test.UnitTests.ModelsTests
{
    [TestFixture]
    public class DeviceUnderTestTests
    {
        // Тест для конструктора с валидным серийным номером
        [Test]
        public void Constructor_WithValidSerialNumber_InitializesCorrectly()
        {
            // Arrange
            string serialNumber = "SN12345";

            // Act
            DeviceUnderTest dut = new DeviceUnderTest(serialNumber);

            // Assert
            Assert.That(dut.SerialNumber, Is.EqualTo(serialNumber), "SerialNumber should be initialized correctly.");
            Assert.That(dut.Measurements, Is.Not.Null, "Measurements list should not be null.");
            Assert.That(dut.Measurements, Is.Empty, "Measurements list should be empty initially.");
            // Проверяем, что MeasurementDateTime была установлена в момент создания (примерно сейчас)
            Assert.That(dut.MeasurementDateTime, Is.EqualTo(DateTime.Now).Within(TimeSpan.FromSeconds(5)),
                        "MeasurementDateTime should be set to current time.");
        }

        // Тест для конструктора с пустым или null серийным номером

        [TestCase("")]
        [TestCase(" ")]
        public void Constructor_WithInvalidSerialNumber_ThrowsArgumentException(string invalidSerialNumber)
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => new DeviceUnderTest(invalidSerialNumber));
            Assert.That(ex.ParamName, Is.EqualTo("serialNumber"), "Exception should specify 'serialNumber' as parameter name.");
            Assert.That(ex.Message, Does.Contain(SnCantBeEmpty), $"Exception message should contain '{SnCantBeEmpty}'.");
        }

        [Test]
        public void RecordAttempt_AddsTheFirstMeasurementOfALocation()
        {
            DeviceUnderTest dut = new DeviceUnderTest("SN001");

            var entry = dut.RecordAttempt(new Measurement("Center", 1, 2, 3, 4));

            Assert.That(dut.Measurements, Has.Count.EqualTo(1), "Measurements list should contain one item.");
            Assert.That(dut.Measurements[0], Is.SameAs(entry), "The returned entry is the one the device holds.");
            Assert.That((entry.Location, entry.x, entry.y, entry.Lv, entry.T), Is.EqualTo(("Center", 1.0, 2.0, 3.0, 4.0)));
        }

        [Test]
        public void RecordAttempt_KeepsTheMeasurementsOfOtherLocations()
        {
            DeviceUnderTest dut = new DeviceUnderTest("SN002");
            dut.RecordAttempt(new Measurement("Corner", 10, 20, 30, 40));

            dut.RecordAttempt(new Measurement("Edge", 5, 6, 7, 8));

            Assert.That(dut.Measurements.Select(m => m.Location), Is.EquivalentTo(new[] { "Corner", "Edge" }));
        }

        [Test]
        public void RecordAttempt_AnotherAttemptOfALocationKeepsBothAndHoldsTheBrightest()
        {
            DeviceUnderTest dut = new DeviceUnderTest("SN003");
            dut.RecordAttempt(new Measurement("Center", 1, 2, 100, 25));
            dut.RecordAttempt(new Measurement("Edge", 5, 6, 7, 8));

            dut.RecordAttempt(new Measurement("Center", 1.1, 2.2, 101.1, 25.1));

            Assert.That(dut.Measurements, Has.Count.EqualTo(2), "one entry per location");
            var center = dut.Measurements.Single(m => m.Location == "Center");
            Assert.That((center.x, center.Lv, dut.AttemptCount("Center")), Is.EqualTo((1.1, 101.1, 2)));
            Assert.That(dut.AttemptCount("Edge"), Is.EqualTo(1), "the other locations are not touched");
        }

        [Test]
        public void RecordAttempt_WithNullMeasurement_ThrowsArgumentNullException()
        {
            DeviceUnderTest dut = new DeviceUnderTest("SN004");

            Assert.Throws<ArgumentNullException>(() => dut.RecordAttempt(null!));
        }

        // Тест для проверки, что MeasurementDateTime можно изменить
        [Test]
        public void MeasurementDateTime_CanBeSet()
        {
            // Arrange
            DeviceUnderTest dut = new DeviceUnderTest("SN005");
            DateTime newDateTime = new DateTime(2024, 1, 1, 10, 0, 0);

            // Act
            dut.MeasurementDateTime = newDateTime;

            // Assert
            Assert.That(dut.MeasurementDateTime, Is.EqualTo(newDateTime));
        }

        // Тест для проверки, что Measurements можно получить (простая проверка свойства)
        [Test]
        public void MeasurementsProperty_ReturnsTheList()
        {
            // Arrange
            DeviceUnderTest dut = new DeviceUnderTest("SN006");
            List<Measurement> externalList = new List<Measurement> { new Measurement("Test", 1, 1, 1, 1) };
            dut.Measurements = externalList; // Можно установить снаружи, хотя обычно это не делается после инициализации

            // Act
            List<Measurement> retrievedList = dut.Measurements;

            // Assert
            Assert.That(retrievedList, Is.SameAs(externalList), "The retrieved list should be the same instance as the set one.");
            Assert.That(retrievedList, Has.Count.EqualTo(1));
        }

        [Test]
        public void ToString_ReturnsNone_WhenNoMeasurements()
        {
            var device = new DeviceUnderTest("SN123");
            var result = device.ToString();
            StringAssert.Contains("Measurements: [None]", result);
        }

        [Test]
        public void ToString_ReturnsMeasurementString_WhenOneMeasurement()
        {
            var device = new DeviceUnderTest("SN123");
            device.RecordAttempt(new Measurement(MeasurementLocation.WhiteColor.ToString(), 0.1, 0.2, 100, 5000));
            var result = device.ToString();
            StringAssert.Contains("Location: WhiteColor", result);
            StringAssert.DoesNotContain("None", result);
        }

        [Test]
        public void ToString_ReturnsAllMeasurements_WhenMultipleMeasurements()
        {
            var device = new DeviceUnderTest("SN123");
            device.RecordAttempt(new Measurement(MeasurementLocation.WhiteColor.ToString(), 0.1, 0.2, 100, 5000));
            device.RecordAttempt(new Measurement(MeasurementLocation.BlackColor.ToString(), 0.3, 0.4, 50, 6000));
            var result = device.ToString();
            StringAssert.Contains("Location: WhiteColor", result);
            StringAssert.Contains("Location: BlackColor", result);
        }
    }
}