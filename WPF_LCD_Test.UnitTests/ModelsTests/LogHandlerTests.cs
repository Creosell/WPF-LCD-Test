using Moq;
using WPF_LCD_Test.Interfaces;
using WPF_LCD_Test.Models;

namespace WPF_LCD_Test.UnitTests.ModelsTests
{
    [TestFixture]
    public class LogHandlerTests
    {
        private Mock<ILocalizationService> _mockLocalizationService;
        private List<string> _loggedMessages;
        private LogHandler _logHandler;

        [SetUp]
        public void Setup()
        {
            _mockLocalizationService = new Mock<ILocalizationService>();
            _loggedMessages = new List<string>();

            // Setup default behavior - return keys wrapped in exclamation marks (not found)
            _mockLocalizationService.Setup(ls => ls.GetString(It.IsAny<string>()))
                .Returns((string key) => $"!{key}!");

            _logHandler = new LogHandler(
                _mockLocalizationService.Object,
                message => _loggedMessages.Add(message));
        }

        #region Constructor Tests

        [Test]
        public void Constructor_NullLocalizationService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new LogHandler(null!, message => { }));
        }

        [Test]
        public void Constructor_NullLogAction_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new LogHandler(_mockLocalizationService.Object, null!));
        }

        [Test]
        public void Constructor_ValidParameters_CreatesInstance()
        {
            // Act
            var handler = new LogHandler(_mockLocalizationService.Object, message => { });

            // Assert
            Assert.That(handler, Is.Not.Null);
        }

        #endregion

        #region Log Method Tests

        [Test]
        public void Log_WithLiteralString_UsesLiteral()
        {
            // Arrange & Act
            // When passing a string literal directly, CallerArgumentExpression will contain quotes
            // which signals LogHandler to use the literal value without translation
            _logHandler.Log("This is a literal message");

            // Assert
            Assert.That(_loggedMessages.Count, Is.EqualTo(1));
            Assert.That(_loggedMessages[0], Is.EqualTo("This is a literal message"));
            _mockLocalizationService.Verify(ls => ls.GetString(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void Log_WithResourceKey_ResolvesLocalization()
        {
            // Arrange
            var resourceKey = "ErrorMessage";
            var localizedValue = "Localized Error Message";

            _mockLocalizationService.Setup(ls => ls.GetString(resourceKey))
                .Returns(localizedValue);

            // Act
            // Simulating calling Log with a resource constant (ResourceName would be "ErrorMessage")
            _logHandler.Log(resourceKey, null, resourceKey);

            // Assert
            Assert.That(_loggedMessages.Count, Is.EqualTo(1));
            Assert.That(_loggedMessages[0], Is.EqualTo(localizedValue));
            _mockLocalizationService.Verify(ls => ls.GetString(resourceKey), Times.Once);
        }

        [Test]
        public void Log_WithArgs_FormatsCorrectly()
        {
            // Arrange
            var template = "Hello {0}, you have {1} messages";
            var args = new object[] { "User", 5 };
            var expected = "Hello User, you have 5 messages";

            // Act
            _logHandler.Log(template, args);

            // Assert
            Assert.That(_loggedMessages.Count, Is.EqualTo(1));
            Assert.That(_loggedMessages[0], Is.EqualTo(expected));
        }

        [Test]
        public void Log_WithInvalidFormat_FallsBackToArgList()
        {
            // Arrange & Act
            var args = new object[] { "arg1", "arg2" };
            // Invalid format string (unclosed placeholder) causes FormatException
            _logHandler.Log("Invalid format {0", args);

            // Assert
            Assert.That(_loggedMessages.Count, Is.EqualTo(1));
            Assert.That(_loggedMessages[0], Does.Contain("Invalid format {0"));
            Assert.That(_loggedMessages[0], Does.Contain("[Args:"));
            Assert.That(_loggedMessages[0], Does.Contain("arg1"));
            Assert.That(_loggedMessages[0], Does.Contain("arg2"));
        }

        [Test]
        public void Log_ResourceNotFound_UsesOriginalValue()
        {
            // Arrange
            var unknownKey = "UnknownResourceKey";

            // Mock returns !key! indicating not found
            _mockLocalizationService.Setup(ls => ls.GetString(unknownKey))
                .Returns($"!{unknownKey}!");

            // Act
            _logHandler.Log(unknownKey, null, unknownKey);

            // Assert - should use original value when translation not found
            Assert.That(_loggedMessages.Count, Is.EqualTo(1));
            Assert.That(_loggedMessages[0], Is.EqualTo(unknownKey));
        }

        [Test]
        public void Log_NullArgs_HandlesGracefully()
        {
            // Arrange
            var message = "Message without arguments";

            // Act
            _logHandler.Log(message, null);

            // Assert
            Assert.That(_loggedMessages.Count, Is.EqualTo(1));
            Assert.That(_loggedMessages[0], Is.EqualTo(message));
        }

        [Test]
        public void Log_EmptyArgs_HandlesGracefully()
        {
            // Arrange
            var message = "Message with empty args";
            var emptyArgs = Array.Empty<object>();

            // Act
            _logHandler.Log(message, emptyArgs);

            // Assert
            Assert.That(_loggedMessages.Count, Is.EqualTo(1));
            Assert.That(_loggedMessages[0], Is.EqualTo(message));
        }

        [Test]
        public void Log_ExceptionInLogAction_CatchesAndLogsError()
        {
            // Arrange
            var throwingHandler = new LogHandler(
                _mockLocalizationService.Object,
                message => throw new InvalidOperationException("Log action failed"));

            // Act & Assert - should not throw, should handle internally
            Assert.DoesNotThrow(() => throwingHandler.Log("Test message"));
        }

        #endregion

        #region ResolveTemplate Tests (via Log)

        [Test]
        public void ResolveTemplate_DetectsResourceKey_ViaExpression()
        {
            // Arrange
            var resourceKey = "SuccessMessage";
            var localizedValue = "Operation Successful";

            _mockLocalizationService.Setup(ls => ls.GetString(resourceKey))
                .Returns(localizedValue);

            // Act - resourceName parameter simulates CallerArgumentExpression
            _logHandler.Log("Original", null, resourceKey);

            // Assert
            Assert.That(_loggedMessages[0], Is.EqualTo(localizedValue));
            _mockLocalizationService.Verify(ls => ls.GetString(resourceKey), Times.Once);
        }

        [Test]
        public void ResolveTemplate_ExpressionWithQuotes_SkipsResolution()
        {
            // Arrange
            var literalMessage = "Literal message";
            var expressionWithQuotes = "\"Literal message\""; // Simulates string literal

            // Act
            _logHandler.Log(literalMessage, null, expressionWithQuotes);

            // Assert - should not attempt resolution due to quotes in expression
            Assert.That(_loggedMessages[0], Is.EqualTo(literalMessage));
            _mockLocalizationService.Verify(ls => ls.GetString(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void ResolveTemplate_ExpressionWithPlus_SkipsResolution()
        {
            // Arrange
            var message = "Concatenated message";
            var expressionWithPlus = "\"Part1\" + \"Part2\""; // Simulates concatenation

            // Act
            _logHandler.Log(message, null, expressionWithPlus);

            // Assert - should not attempt resolution due to plus in expression
            Assert.That(_loggedMessages[0], Is.EqualTo(message));
            _mockLocalizationService.Verify(ls => ls.GetString(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void ResolveTemplate_NullExpression_UsesOriginalValue()
        {
            // Arrange
            var message = "Message with null expression";

            // Act
            _logHandler.Log(message, null, null);

            // Assert
            Assert.That(_loggedMessages[0], Is.EqualTo(message));
            _mockLocalizationService.Verify(ls => ls.GetString(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void ResolveTemplate_EmptyExpression_UsesOriginalValue()
        {
            // Arrange
            var message = "Message with empty expression";

            // Act
            _logHandler.Log(message, null, string.Empty);

            // Assert
            Assert.That(_loggedMessages[0], Is.EqualTo(message));
            _mockLocalizationService.Verify(ls => ls.GetString(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void ResolveTemplate_QualifiedResourceKey_ExtractsLastPart()
        {
            // Arrange
            var expectedKey = "SuccessMessage";
            var localizedValue = "Operation completed successfully";
            var calledKeys = new List<string>();

            // Create a new mock and handler for this specific test to avoid Setup conflicts
            var mockLocalization = new Mock<ILocalizationService>();
            var messages = new List<string>();
            var handler = new LogHandler(mockLocalization.Object, msg => messages.Add(msg));

            // Setup behavior: track which keys are requested and return appropriate values
            // Note: LogHandler checks that translated strings don't start/end with '!' to detect missing translations
            mockLocalization.Setup(ls => ls.GetString(It.IsAny<string>()))
                .Returns((string key) =>
                {
                    calledKeys.Add(key);
                    return key == expectedKey ? localizedValue : $"!{key}!";
                });

            // Act - simulating Log(SomeResource) where SomeResource = Resources.Messages.SuccessMessage
            handler.Log("Original", null, "Resources.Messages.SuccessMessage");

            // Assert
            Assert.That(calledKeys.Count, Is.GreaterThan(0), "GetString should have been called");
            Assert.That(calledKeys, Does.Contain(expectedKey), $"Expected key '{expectedKey}' to be requested");
            Assert.That(messages.Count, Is.EqualTo(1), "Should have logged one message");
            Assert.That(messages[0], Is.EqualTo(localizedValue), "Should use localized value");
        }

        #endregion

        #region Integration Tests

        [Test]
        public void Log_MultipleMessages_AllLogged()
        {
            // Arrange & Act
            _logHandler.Log("Message 1");
            _logHandler.Log("Message 2");
            _logHandler.Log("Message 3");

            // Assert
            Assert.That(_loggedMessages.Count, Is.EqualTo(3));
            Assert.That(_loggedMessages[0], Is.EqualTo("Message 1"));
            Assert.That(_loggedMessages[1], Is.EqualTo("Message 2"));
            Assert.That(_loggedMessages[2], Is.EqualTo("Message 3"));
        }

        [Test]
        public void Log_MixedLiteralAndResourceKey_HandlesCorrectly()
        {
            // Arrange
            var resourceKey = "ErrorKey";
            var localizedValue = "Localized Error";
            _mockLocalizationService.Setup(ls => ls.GetString(resourceKey))
                .Returns(localizedValue);

            // Act
            _logHandler.Log("Literal message");
            _logHandler.Log("Original", null, resourceKey);

            // Assert
            Assert.That(_loggedMessages.Count, Is.EqualTo(2));
            Assert.That(_loggedMessages[0], Is.EqualTo("Literal message"));
            Assert.That(_loggedMessages[1], Is.EqualTo(localizedValue));
        }

        #endregion
    }
}
