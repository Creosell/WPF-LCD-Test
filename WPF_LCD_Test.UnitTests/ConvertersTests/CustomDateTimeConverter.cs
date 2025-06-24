// В проекте WPF_LCD_Test.UnitTests (создайте новую папку, например, ConvertersTests)
// Файл CustomDateTimeConverterTests.cs

using System.Text.Json;
using WPF_LCD_Test.Converters; // Используем пространство имен вашего конвертера

namespace WPF_LCD_Test.UnitTests.ConvertersTests
{
    [TestFixture]
    public class CustomDateTimeConverterTests
    {
        // Вспомогательный класс для тестирования конвертера
        // JsonSerializer работает с целыми объектами, поэтому нам нужна "оболочка"
        private class TestObjectWithDateTime
        {
            // Применяем CustomDateTimeConverter к этому свойству
            [System.Text.Json.Serialization.JsonConverter(typeof(CustomDateTimeConverter))]
            public DateTime TestDateTime { get; set; }
        }

        [Test]
        public void Write_CorrectlyFormatsDateTimeToString()
        {
            // Arrange
            var dateTime = new DateTime(2023, 5, 20, 10, 30, 45); // Год, месяц, день, час, минута, секунда
            var testObject = new TestObjectWithDateTime { TestDateTime = dateTime };
            var options = new JsonSerializerOptions { WriteIndented = false }; // Не добавляем отступы для простой проверки

            // Act
            string jsonString = JsonSerializer.Serialize(testObject, options);

            // Assert
            string expectedJson = "{\"TestDateTime\":\"20230520_103045\"}";
            Assert.That(jsonString, Is.EqualTo(expectedJson), "DateTime should be serialized to 'yyyyMMdd_HHmmss' format.");
        }

        [Test]
        public void Read_CorrectlyParsesStringToDateTime()
        {
            // Arrange
            string jsonString = "{\"TestDateTime\":\"2023-05-20 10:30:45\"}";
            var expectedDateTime = new DateTime(2023, 5, 20, 10, 30, 45);
            var options = new JsonSerializerOptions();

            // Act
            var deserializedObject = JsonSerializer.Deserialize<TestObjectWithDateTime>(jsonString, options);

            // Assert
            Assert.IsNotNull(deserializedObject, "Deserialized object should not be null.");
            // Сравниваем даты с некоторой допустимой погрешностью, чтобы избежать проблем с точностью DateTime (хотя здесь это не очень вероятно)
            Assert.That(deserializedObject.TestDateTime, Is.EqualTo(expectedDateTime), "Deserialized DateTime should match the original.");
        }

        [Test]
        public void Read_ThrowsJsonException_ForInvalidStringFormat()
        {
            // Arrange
            string invalidJsonString = "{\"TestDateTime\":\"Invalid Date String\"}";
            var options = new JsonSerializerOptions();

            // Act & Assert
            // Проверяем, что метод Read выбрасывает JsonException при неверном формате
            Assert.Throws<JsonException>(() =>
            {
                JsonSerializer.Deserialize<TestObjectWithDateTime>(invalidJsonString, options);
            }, "Should throw JsonException for invalid date string format.");
        }

        [Test]
        public void Read_ThrowsJsonException_ForNonStringToken()
        {
            // Arrange
            // Передаем число, а не строку, чтобы проверить обработку нестроковых токенов
            string jsonWithNonStringToken = "{\"TestDateTime\":12345}";
            var options = new JsonSerializerOptions();

            // Act & Assert
            Assert.Throws<JsonException>(() =>
            {
                JsonSerializer.Deserialize<TestObjectWithDateTime>(jsonWithNonStringToken, options);
            }, "Should throw JsonException for non-string token for DateTime.");
        }
    }
}