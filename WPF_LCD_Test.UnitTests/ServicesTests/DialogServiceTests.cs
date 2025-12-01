// В проекте WPF_LCD_Test.UnitTests
// Папка ServicesTests
// Файл DialogServiceTests.cs

using Moq; // Для использования Moq
using System.Windows; // Для MessageBoxResult и других enum
using WPF_LCD_Test.Interfaces; // Для IMessageBox
using WPF_LCD_Test.Services; // Для DialogService

namespace WPF_LCD_Test.UnitTests.ServicesTests
{
    [TestFixture]
    public class DialogServiceTests
    {
        private Mock<IMessageBox> _mockMessageBox;
        private DialogService _dialogService;

        [SetUp]
        public void Setup()
        {
            _mockMessageBox = new Mock<IMessageBox>();
            _dialogService = new DialogService(_mockMessageBox.Object);
        }

        [Test]
        public void ShowQuestion_ReturnsTrue_WhenMessageBoxReturnsYes()
        {
            // Arrange
            string testMessage = "Test question?";
            string testCaption = "Test Caption";

            // Настраиваем мок: когда будет вызван метод Show с любыми аргументами,
            // он должен вернуть MessageBoxResult.Yes
            _mockMessageBox.Setup(m => m.Show(
                It.IsAny<string>(), // Любая строка для messageBoxText
                It.IsAny<string>(), // Любая строка для caption
                MessageBoxButton.YesNo, // Должна быть кнопка YesNo
                MessageBoxImage.Question // Должна быть иконка Question
            )).Returns(MessageBoxResult.Yes);

            // Act
            bool result = _dialogService.ShowQuestion(testMessage, testCaption);

            // Assert
            Assert.That(result, Is.True, "ShowQuestion should return true if MessageBoxResult is Yes.");

            // Проверяем, что метод Show был вызван ровно один раз с нужными аргументами
            _mockMessageBox.Verify(m => m.Show(
                testMessage,
                testCaption,
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            ), Times.Once);
        }

        [Test]
        public void ShowQuestion_ReturnsFalse_WhenMessageBoxReturnsNo()
        {
            // Arrange
            string testMessage = "Test question?";
            string testCaption = "Test Caption";

            // Настраиваем мок для возврата No
            _mockMessageBox.Setup(m => m.Show(
                It.IsAny<string>(), It.IsAny<string>(), MessageBoxButton.YesNo, MessageBoxImage.Question
            )).Returns(MessageBoxResult.No);

            // Act
            bool result = _dialogService.ShowQuestion(testMessage, testCaption);

            // Assert
            Assert.That(result, Is.False, "ShowQuestion should return false if MessageBoxResult is No.");
            _mockMessageBox.Verify(m => m.Show(
                testMessage, testCaption, MessageBoxButton.YesNo, MessageBoxImage.Question
            ), Times.Once);
        }

        [Test]
        public void ShowMessage_CallsMessageBoxShowWithCorrectParameters()
        {
            // Arrange
            string testMessage = "Test info message.";
            string testCaption = "Info";

            // Настраиваем мок: когда Show будет вызван, он ничего не должен возвращать (void метод),
            // но мы хотим проверить, что он был вызван с правильными параметрами.
            // Moq автоматически ничего не делает для void методов, поэтому просто настраиваем Verify.

            // Act
            _dialogService.ShowMessage(testMessage, testCaption);

            // Assert
            // Проверяем, что метод Show был вызван ровно один раз с нужными аргументами
            _mockMessageBox.Verify(m => m.Show(
                testMessage,
                testCaption,
                MessageBoxButton.OK,
                MessageBoxImage.Information
            ), Times.Once);

            // Также можно убедиться, что не было других вызовов Show с другими параметрами
            _mockMessageBox.VerifyNoOtherCalls();
        }

        // Можно добавить тесты для других кнопок (например, MessageBoxButton.Cancel)
        // или других иконок, если методы ShowQuestion/ShowMessage были бы более гибкими.
    }
}