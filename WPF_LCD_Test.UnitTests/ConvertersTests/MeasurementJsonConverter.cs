// В проекте WPF_LCD_Test.UnitTests (папка ConvertersTests)
// Файл MeasurementJsonConverterTests.cs

using NUnit.Framework;
using System;
using System.Text.Json;
using WPF_LCD_Test.Converters;
using WPF_LCD_Test.Models; // Для класса Measurement

namespace WPF_LCD_Test.UnitTests.ConvertersTests
{
    [TestFixture]
    public class MeasurementJsonConverterTests
    {
        // Вспомогательный класс для тестирования конвертера
        private class TestObjectWithMeasurement
        {
            [System.Text.Json.Serialization.JsonConverter(typeof(MeasurementJsonConverter))]
            public Measurement TestMeasurement { get; set; }
        }

        // Жестко задаем значение, чтобы не зависеть от MeasurementStatusService в тесте
        private const string BlackColorLocationName = "BlackColor"; // Используем значение из вашего кода

        [Test]
        public void Write_CorrectlyFormatsAllProperties()
        {
            // Arrange
            var measurement = new Measurement("Center", 1.2345, 6.7891, 100.12345, 25.6);
            var testObject = new TestObjectWithMeasurement { TestMeasurement = measurement };
            var options = new JsonSerializerOptions { WriteIndented = false };

            // Act
            string jsonString = JsonSerializer.Serialize(testObject, options);

            // Assert
            // x и y округлены до 3 знаков
            // Lv округлено до 1 знака (т.к. Location не "BlackColor")
            // T округлено до 0 знаков
            string expectedJson = "{\"TestMeasurement\":{\"Location\":\"Center\",\"x\":1.235,\"y\":6.789,\"Lv\":100.1,\"T\":26}}";
            Assert.That(jsonString, Is.EqualTo(expectedJson), "JSON should contain all properties with correct rounding and Lv precision for non-black location.");
        }

        [Test]
        public void Write_LvPrecision_Is8_ForBlackColorLocation()
        {
            // Arrange
            var measurement = new Measurement(BlackColorLocationName, 1.234567, 2.345678, 0.000000123, 20.9);
            var testObject = new TestObjectWithMeasurement { TestMeasurement = measurement };
            var options = new JsonSerializerOptions { WriteIndented = false };

            // Act
            string jsonString = JsonSerializer.Serialize(testObject, options);

            // Assert
            // x, y округлены до 3 знаков
            // Lv округлено до 8 знаков (т.к. Location = BlackColorLocationName)
            // T округлено до 0 знаков
            string expectedJson = "{\"TestMeasurement\":{\"Location\":\"BlackColor\",\"x\":1.235,\"y\":2.346,\"Lv\":1.2E-07,\"T\":21}}";
            Assert.That(jsonString, Is.EqualTo(expectedJson), "Lv should be rounded to 8 decimal places for BlackColorLocation.");
        }

        [Test]
        public void Write_LvPrecision_Is1_ForOtherLocations()
        {
            // Arrange
            var measurement1 = new Measurement("Red", 0.655343434, 0.342542, 123.456, 2313.12312312);
            var measurement2 = new Measurement("White", 0.2636746346, 0.436643346, 123.891, 6500.8231230);

            var testObject1 = new TestObjectWithMeasurement { TestMeasurement = measurement1 };
            var testObject2 = new TestObjectWithMeasurement { TestMeasurement = measurement2 };

            var options = new JsonSerializerOptions { WriteIndented = false };

            // Act
            string jsonString1 = JsonSerializer.Serialize(testObject1, options);
            string jsonString2 = JsonSerializer.Serialize(testObject2, options);

            // Assert
            string expectedJson1 = "{\"TestMeasurement\":{\"Location\":\"Red\",\"x\":0.655,\"y\":0.343,\"Lv\":123.5,\"T\":2313}}";
            string expectedJson2 = "{\"TestMeasurement\":{\"Location\":\"White\",\"x\":0.264,\"y\":0.437,\"Lv\":123.9,\"T\":6501}}";

            Assert.That(jsonString1, Is.EqualTo(expectedJson1), "Lv should be rounded to 1 decimal place for 'Red' location.");
            Assert.That(jsonString2, Is.EqualTo(expectedJson2), "Lv should be rounded to 1 decimal place for 'White' location.");
        }

        [Test]
        public void Write_IsValidProperty_IsNotSerialized()
        {
            // Arrange
            var measurement = new Measurement("Test", 1.0, 1.0, 1.0, 1.0);
            measurement.IsValid = false; // Устанавливаем, чтобы убедиться, что она не попадает в JSON
            var testObject = new TestObjectWithMeasurement { TestMeasurement = measurement };
            var options = new JsonSerializerOptions { WriteIndented = false };

            // Act
            string jsonString = JsonSerializer.Serialize(testObject, options);

            // Assert
            // Проверяем, что строка не содержит "IsValid"
            Assert.That(jsonString, Does.Not.Contain("\"IsValid\""), "IsValid property should not be serialized.");
            Assert.That(jsonString, Does.Not.Contain("true"), "Boolean 'true' from IsValid should not be in JSON.");
            Assert.That(jsonString, Does.Not.Contain("false"), "Boolean 'false' from IsValid should not be in JSON.");
        }


        [Test]
        public void Read_ThrowsNotImplementedException()
        {
            // Arrange
            // Любая строка JSON, так как Read все равно выбросит исключение
            string jsonString = "{\"TestMeasurement\":{\"Location\":\"Some\",\"x\":1,\"y\":1,\"Lv\":1,\"T\":1}}";
            var options = new JsonSerializerOptions();

            // Act & Assert
            Assert.Throws<NotImplementedException>(() =>
            {
                JsonSerializer.Deserialize<TestObjectWithMeasurement>(jsonString, options);
            }, "Read method should throw NotImplementedException as it is not implemented.");
        }
    }
}