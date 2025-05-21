// В проекте WPF_LCD_Test.UnitTests (папка ModelsTests)
// Файл DeviceUnderTestTests.cs

using NUnit.Framework;
using System;
using System.Collections.Generic;
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

        // Тест для AddMeasurement - добавление в пустой список
        [Test]
        public void AddMeasurement_AddsNewMeasurementToEmptyList()
        {
            // Arrange
            DeviceUnderTest dut = new DeviceUnderTest("SN001");
            Measurement newMeasurement = new Measurement("Center", 1, 2, 3, 4);

            // Act
            dut.AddMeasurement(newMeasurement);

            // Assert
            Assert.That(dut.Measurements, Has.Count.EqualTo(1), "Measurements list should contain one item.");
            Assert.That(dut.Measurements[0], Is.SameAs(newMeasurement), "The added measurement should be the same instance.");
        }

        // Тест для AddMeasurement - добавление в непустой список
        [Test]
        public void AddMeasurement_AddsNewMeasurementToNonEmptyList()
        {
            // Arrange
            DeviceUnderTest dut = new DeviceUnderTest("SN002");
            Measurement existingMeasurement = new Measurement("Corner", 10, 20, 30, 40);
            dut.AddMeasurement(existingMeasurement); // Добавим существующее измерение

            Measurement newMeasurement = new Measurement("Edge", 5, 6, 7, 8);

            // Act
            dut.AddMeasurement(newMeasurement);

            // Assert
            Assert.That(dut.Measurements, Has.Count.EqualTo(2), "Measurements list should contain two items.");
            Assert.That(dut.Measurements.Contains(existingMeasurement), Is.True, "Existing measurement should still be in the list.");
            Assert.That(dut.Measurements.Contains(newMeasurement), Is.True, "New measurement should be added to the list.");
        }

        // Тест для AddMeasurement - замена существующего измерения
        [Test]
        public void AddMeasurement_ReplacesExistingMeasurementByLocation()
        {
            // Arrange
            DeviceUnderTest dut = new DeviceUnderTest("SN003");
            Measurement originalCenterMeasurement = new Measurement("Center", 1, 2, 100, 25);
            dut.AddMeasurement(originalCenterMeasurement);
            dut.AddMeasurement(new Measurement("Edge", 5, 6, 7, 8)); // Добавим еще одно, чтобы проверить, что другие не затрагиваются

            Measurement updatedCenterMeasurement = new Measurement("Center", 1.1, 2.2, 101.1, 25.1); // То же Location, но другие данные

            // Act
            dut.AddMeasurement(updatedCenterMeasurement);

            // Assert
            Assert.That(dut.Measurements, Has.Count.EqualTo(2), "Measurements list count should remain the same (original Center removed, new added).");
            Assert.That(dut.Measurements.Contains(originalCenterMeasurement), Is.False, "Original Center measurement should be removed.");
            Assert.That(dut.Measurements.Contains(updatedCenterMeasurement), Is.True, "Updated Center measurement should be added.");

            // Проверяем, что в списке есть только одна запись с Location="Center" и это обновленная запись
            Measurement foundMeasurement = dut.Measurements.Find(m => m.Location == "Center");
            Assert.That(foundMeasurement, Is.SameAs(updatedCenterMeasurement), "The found 'Center' measurement should be the updated one.");
            Assert.That(foundMeasurement.x, Is.EqualTo(1.1), "Updated measurement's x value is incorrect.");
        }

        // Тест для AddMeasurement - обработка null аргумента
        [Test]
        public void AddMeasurement_WithNullMeasurement_ThrowsArgumentNullException()
        {
            // Arrange
            DeviceUnderTest dut = new DeviceUnderTest("SN004");

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => dut.AddMeasurement(null), "AddMeasurement should throw ArgumentNullException for null input.");
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
    }
}