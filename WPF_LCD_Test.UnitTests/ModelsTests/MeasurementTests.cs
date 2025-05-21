// В проекте WPF_LCD_Test.UnitTests (папка ModelsTests)
// Файл MeasurementTests.cs

using NUnit.Framework;
using WPF_LCD_Test.Models; // Для класса Measurement
using System.Globalization; // Для CultureInfo

namespace WPF_LCD_Test.UnitTests.ModelsTests
{
    [TestFixture]
    public class MeasurementTests
    {
        // Тест для конструктора с параметрами
        [Test]
        public void Constructor_InitializesPropertiesCorrectly()
        {
            // Arrange
            string location = "TestLocation";
            double x = 1.234;
            double y = 5.678;
            double lv = 90.123;
            double t = 25.5;

            // Act
            Measurement measurement = new Measurement(location, x, y, lv, t);

            // Assert
            Assert.That(measurement.Location, Is.EqualTo(location));
            Assert.That(measurement.x, Is.EqualTo(x));
            Assert.That(measurement.y, Is.EqualTo(y));
            Assert.That(measurement.Lv, Is.EqualTo(lv));
            Assert.That(measurement.T, Is.EqualTo(t));
            Assert.That(measurement.IsValid, Is.True, "IsValid should be true by default in parameterized constructor.");
        }

        // Тест для конструктора без параметров
        [Test]
        public void DefaultConstructor_InitializesWithDefaultValues()
        {
            // Act
            Measurement measurement = new Measurement();

            // Assert
            Assert.That(measurement.Location, Is.EqualTo("Unknown"));
            Assert.That(measurement.x, Is.EqualTo(0.0)); // Default for double
            Assert.That(measurement.y, Is.EqualTo(0.0)); // Default for double
            Assert.That(measurement.Lv, Is.EqualTo(0.0)); // Default for double
            Assert.That(measurement.T, Is.EqualTo(0.0)); // Default for double
            Assert.That(measurement.IsValid, Is.True, "IsValid should be true by default in default constructor.");
        }

        // Тесты для геттеров и сеттеров (проверка, что свойства работают)
        [Test]
        public void Properties_CanBeSetAndGet()
        {
            // Arrange
            Measurement measurement = new Measurement();
            string newLocation = "NewLocation";
            double newX = 10.1;
            double newY = 20.2;
            double newLv = 300.3;
            double newT = 40.4;
            bool newIsValid = false;

            // Act
            measurement.Location = newLocation;
            measurement.x = newX;
            measurement.y = newY;
            measurement.Lv = newLv;
            measurement.T = newT;
            measurement.IsValid = newIsValid;

            // Assert
            Assert.That(measurement.Location, Is.EqualTo(newLocation));
            Assert.That(measurement.x, Is.EqualTo(newX));
            Assert.That(measurement.y, Is.EqualTo(newY));
            Assert.That(measurement.Lv, Is.EqualTo(newLv));
            Assert.That(measurement.T, Is.EqualTo(newT));
            Assert.That(measurement.IsValid, Is.EqualTo(newIsValid));
        }

        // Тест для метода ToString()
        [Test]
        public void ToString_ReturnsCorrectlyFormattedString()
        {
            // Arrange
            // Используем значения, которые проверят форматирование
            Measurement measurement = new Measurement("Center", 1.2345, 6.7891, 100.12345, 25.6);

            // Act
            string result = measurement.ToString();

            // Assert
            // x: 1.2345 -> F3 -> 1.235 (rounding away from zero)
            // y: 6.7891 -> F3 -> 6.789
            // Lv: 100.12345 -> F1 -> 100.1
            // T: 25.6 -> F0 -> 26
            string expected = "Location: Center, x: 1.235, y: 6.789, Lv: 100.1, T: 26";
            Assert.That(result, Is.EqualTo(expected), "ToString() output is not correctly formatted.");
        }

        // Тест для метода ToCsvString()
        [Test]
        public void ToCsvString_ReturnsCorrectlyFormattedCsvString()
        {
            // Arrange
            // Используем значения, которые проверят форматирование (без явного Fx)
            // По умолчанию double.ToString() с InvariantCulture не добавляет нули после запятой, если они не нужны,
            // и использует экспоненциальную нотацию для очень маленьких/больших чисел.
            Measurement measurement = new Measurement("Report", 12.345, 67.8, 9.123456789, 30.0);

            // Act
            string result = measurement.ToCsvString();

            // Assert
            // В вашем коде ToCsvString использует просто ToString(CultureInfo.InvariantCulture)
            // Это будет давать:
            // x: 12.345
            // y: 67.8
            // Lv: 9.123456789 (или 9.123456789E+00 если число очень мало)
            // T: 30
            //
            // Если вы хотите точное форматирование для CSV, как в закомментированной строке в ToCsvString(),
            // то нужно активировать ее и обновить ожидаемый результат.
            // Например, для Lv с F4: 9.1235
            // Для T с F0: 30
            string expected = $"Report,{12.345.ToString(CultureInfo.InvariantCulture)},{67.8.ToString(CultureInfo.InvariantCulture)},{9.123456789.ToString(CultureInfo.InvariantCulture)},{30.0.ToString(CultureInfo.InvariantCulture)}";
            Assert.That(result, Is.EqualTo(expected), "ToCsvString() output is not correctly formatted.");
        }

        // Тест для IsValid свойства
        [Test]
        public void IsValid_CanBeSetToFalse()
        {
            // Arrange
            Measurement measurement = new Measurement(); // По умолчанию IsValid = true

            // Act
            measurement.IsValid = false;

            // Assert
            Assert.That(measurement.IsValid, Is.False);
        }

        // Дополнительный тест для ToString, чтобы убедиться в правильном округлении для .5
        [Test]
        public void ToString_HandlesMidpointRoundingCorrectly()
        {
            // Arrange
            // x: 1.5 -> F0 -> 2 (AwayFromZero)
            // y: 2.5 -> F0 -> 3 (AwayFromZero)
            // Lv: 10.5 -> F0 -> 11 (AwayFromZero)
            // T: 20.5 -> F0 -> 21 (AwayFromZero)
            Measurement measurement = new Measurement("Midpoint", 1.2345, 6.7895, 100.15, 25.5);

            // Act
            string result = measurement.ToString();

            // Assert
            // x: 1.2345 -> F3 -> 1.235
            // y: 6.7895 -> F3 -> 6.790 (Rounding .5 AwayFromZero)
            // Lv: 100.15 -> F1 -> 100.2 (Rounding .5 AwayFromZero)
            // T: 25.5 -> F0 -> 26 (Rounding .5 AwayFromZero)
            string expected = "Location: Midpoint, x: 1.235, y: 6.790, Lv: 100.2, T: 26";
            Assert.That(result, Is.EqualTo(expected), "ToString() midpoint rounding is incorrect.");
        }
    }
}