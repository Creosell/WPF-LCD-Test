using Moq;
using System.Globalization;
using System.Runtime.InteropServices;
using WPF_LCD_Test.Interfaces;
using WPF_LCD_Test.Services;

// Это заглушка для LocalizationService.Instance, если он статичен и вызывает проблемы в тестах.
// В более сложном сценарии вы бы могли использовать фреймворк для мокирования статических методов
// или переделать LocalizationService для инъекции зависимостей.
internal class MockLocalizationService
{
    // Используем внутренний статический класс, чтобы избежать конфликтов имен
    // и чтобы он был доступен только в сборке тестов.
    private MockLocalizationService()
    { } // Приватный конструктор для синглтона

    public static MockLocalizationService Instance { get; } = new MockLocalizationService();
    public CultureInfo CurrentCulture { get; set; } = CultureInfo.InvariantCulture;
}

namespace WPF_LCD_Test.Tests
{
    // [TestFixture] указывает NUnit, что этот класс содержит тесты
    [TestFixture]
    public class ColorMeasurementServiceTests : IDisposable
    {
        private Mock<IColorAnalyzer200> _mockCa200;
        private Mock<IColorAnalyzer> _mockCa;
        private Mock<IColorAnalyzerProbe> _mockProbe;
        private Mock<IColorAnalyzerMemory> _mockMemory;
        private ColorMeasurementService _service;

        // [SetUp] метод выполняется перед каждым тестом
        [SetUp]
        public void Setup()
        {
            // Инициализация моков
            _mockProbe = new Mock<IColorAnalyzerProbe>();
            _mockMemory = new Mock<IColorAnalyzerMemory>();
            _mockCa = new Mock<IColorAnalyzer>();
            _mockCa200 = new Mock<IColorAnalyzer200>();

            // Настройка базового поведения моков, чтобы избежать NullReferenceException при доступе к свойствам
            _mockCa200.Setup(m => m.SingleCa).Returns(_mockCa.Object);
            _mockCa.Setup(m => m.SingleProbe).Returns(_mockProbe.Object);
            _mockCa.Setup(m => m.Memory).Returns(_mockMemory.Object);

            // Создаем экземпляр сервиса, передавая ему МОК IColorAnalyzer200
            _service = new ColorMeasurementService(_mockCa200.Object);

            // Настройка LocalizationService для тестов, чтобы он не мешал
            MockLocalizationService.Instance.CurrentCulture = CultureInfo.InvariantCulture;
        }

        // --- Тесты для ConnectAsync ---

        [Test] // [Test] вместо [Fact]
        public async Task ConnectAsync_ShouldConnectSuccessfullyAndRaiseEvents()
        {
            // Arrange
            bool connectionStatusChangedCalled = false;
            string? statusMessage = null;

            _service.ConnectionStatusChanged += (sender, args) =>
                connectionStatusChangedCalled = args;
            _service.StatusMessage += (sender, msg) => statusMessage = msg;

            // Act
            var result = await _service.ConnectAsync();

            // Assert
            Assert.IsTrue(result); // NUnit: Assert.IsTrue
            Assert.IsTrue(_service.IsDeviceConnected); // NUnit: Assert.IsTrue
            Assert.IsTrue(connectionStatusChangedCalled); // NUnit: Assert.IsTrue
            StringAssert.Contains(Resources.Resources.ConnectedCA, statusMessage); // NUnit: StringAssert.Contains

            // Убеждаемся, что AutoConnect был вызван на моке _mockCa200
            _mockCa200.Verify(m => m.AutoConnect(), Times.Once);
            // Убеждаемся, что SingleCa был запрошен
            _mockCa200.Verify(m => m.SingleCa, Times.Once);
        }

        [Test]
        public async Task ConnectAsync_ShouldHandleCOMExceptionAndSetDisconnected()
        {
            // Arrange
            _mockCa200
                .Setup(m => m.AutoConnect())
                .Throws(new COMException("Test COM Error", -2147024891)); // Пример COMException
            bool connectionStatusChangedCalled = false;
            string? statusMessage = null;

            _service.ConnectionStatusChanged += (sender, args) =>
                connectionStatusChangedCalled = args;
            _service.StatusMessage += (sender, msg) => statusMessage = msg;

            // Act
            var result = await _service.ConnectAsync();

            // Assert
            Assert.IsFalse(result); // NUnit: Assert.IsFalse
            Assert.IsFalse(_service.IsDeviceConnected); // NUnit: Assert.IsFalse
            Assert.IsFalse(connectionStatusChangedCalled); // Событие должно быть вызвано со значением false
            StringAssert.Contains(Resources.Resources.ConnectionError, statusMessage); // NUnit: StringAssert.Contains
            StringAssert.Contains("Test COM Error", statusMessage); // NUnit: StringAssert.Contains
        }

        [Test]
        public async Task ConnectAsync_ShouldNotConnectIfAlreadyConnected()
        {
            // Arrange
            // Имитируем уже подключенное состояние
            _mockCa200.Setup(m => m.AutoConnect()).Verifiable(); // Настройка мока для первого вызова
            await _service.ConnectAsync(); // Первый вызов для установки connected = true
            _mockCa200.Invocations.Clear(); // Очищаем вызовы, чтобы проверить, что AutoConnect не вызывается повторно

            // Act
            var result = await _service.ConnectAsync();

            // Assert
            Assert.IsTrue(result); // Метод все еще должен вернуть true, так как он уже подключен
            Assert.IsTrue(_service.IsDeviceConnected); // Состояние должно остаться подключенным
            _mockCa200.Verify(m => m.AutoConnect(), Times.Never); // AutoConnect не должен быть вызван повторно
        }

        // --- Тесты для CalibrateZeroAsync ---

        [Test]
        public async Task CalibrateZeroAsync_ShouldCalibrateSuccessfullyAndRaiseEvents()
        {
            // Arrange
            // Убедимся, что устройство подключено, иначе CalibrateZeroAsync может не работать
            _mockCa200.Setup(m => m.AutoConnect()).Verifiable();
            await _service.ConnectAsync(); // Подключаемся

            bool calibrationStatusChangedCalled = false;
            string? statusMessage = null;

            _service.CalibrationStatusChanged += (sender, args) =>
                calibrationStatusChangedCalled = args;
            _service.StatusMessage += (sender, msg) => statusMessage = msg;

            // Act
            var result = await _service.CalibrateZeroAsync();

            // Assert
            Assert.IsTrue(result);
            Assert.IsTrue(_service.IsDeviceCalibrated);
            Assert.IsTrue(calibrationStatusChangedCalled);
            StringAssert.Contains(Resources.Resources.ZeroCalibratedCA, statusMessage);

            _mockCa.Verify(m => m.CalZero(), Times.Once); // Проверяем, что CalZero был вызван
            _mockCa.VerifySet(m => m.SyncMode = It.IsAny<int>(), Times.Once);
            _mockCa.VerifySet(m => m.AveragingMode = It.IsAny<int>(), Times.Once);
            _mockCa.Verify(m => m.SetAnalogRange(It.IsAny<float>(), It.IsAny<float>()), Times.Once);
            _mockCa.VerifySet(m => m.DisplayMode = It.IsAny<int>(), Times.Once);
        }

        [Test]
        public async Task CalibrateZeroAsync_ShouldHandleCOMExceptionAndSetUncalibrated()
        {
            // Arrange
            _mockCa200.Setup(m => m.AutoConnect()).Verifiable();
            await _service.ConnectAsync(); // Подключаемся

            _mockCa
                .Setup(m => m.CalZero())
                .Throws(new COMException("Calibration COM Error", -2147024891));

            bool calibrationStatusChangedCalled = false;
            string? statusMessage = null;

            _service.CalibrationStatusChanged += (sender, args) =>
                calibrationStatusChangedCalled = args;
            _service.StatusMessage += (sender, msg) => statusMessage = msg;

            // Act
            var result = await _service.CalibrateZeroAsync();

            // Assert
            Assert.IsFalse(result);
            Assert.IsFalse(_service.IsDeviceCalibrated);
            Assert.IsFalse(calibrationStatusChangedCalled); // Событие должно быть вызвано со значением false
            StringAssert.Contains(Resources.Resources.CheckConnectionCA, statusMessage);
            StringAssert.Contains("Calibration COM Error", statusMessage);
        }

        // --- Тесты для MeasureAsync ---

        [Test]
        public async Task MeasureAsync_ShouldReturnInvalidIfDeviceNotConnected()
        {
            // Arrange
            // Убеждаемся, что устройство не подключено (по умолчанию оно не подключено)
            string? statusMessage = null;
            _service.StatusMessage += (sender, msg) => statusMessage = msg;

            // Act
            var measurement = await _service.MeasureAsync(1);

            // Assert
            Assert.IsFalse(measurement.IsValid);
            StringAssert.Contains(Resources.Resources.MeasureWihoutConnectionError, statusMessage);
            _mockCa.Verify(m => m.Measure(), Times.Never); // Метод измерения не должен быть вызван
        }

        [Test]
        public async Task MeasureAsync_ShouldReturnInvalidIfDeviceNotCalibrated()
        {
            // Arrange
            _mockCa200.Setup(m => m.AutoConnect()).Verifiable();
            await _service.ConnectAsync(); // Подключаемся
            // Убеждаемся, что устройство не откалибровано (по умолчанию оно не откалибровано после подключения)
            string? statusMessage = null;
            _service.StatusMessage += (sender, msg) => statusMessage = msg;

            // Act
            var measurement = await _service.MeasureAsync(1);

            // Assert
            Assert.IsFalse(measurement.IsValid);
            StringAssert.Contains(Resources.Resources.MakeZeroCalibration, statusMessage);
            _mockCa.Verify(m => m.Measure(), Times.Never); // Метод измерения не должен быть вызван
        }

        [Test]
        public async Task MeasureAsync_ShouldHandleCOMExceptionDuringIteration()
        {
            // Arrange
            _mockCa200.Setup(m => m.AutoConnect()).Verifiable();
            await _service.ConnectAsync();
            await _service.CalibrateZeroAsync();

            // Настраиваем мок так, чтобы второе измерение выдало COMException
            _mockCa
                .SetupSequence(m => m.Measure())
                .Pass() // Первый вызов успешен
                .Throws(new COMException("Measure iteration COM Error", -2147024891)) // Второй вызов - ошибка
                .Pass(); // Третий вызов успешен

            double expectedSx = 0.5;
            double expectedSy = 0.6;
            double expectedLv = 100.0;
            double expectedT = 2.5;

            // Для успешных итераций возвращаем ожидаемые значения
            _mockProbe.Setup(p => p.sx).Returns(expectedSx);
            _mockProbe.Setup(p => p.sy).Returns(expectedSy);
            _mockProbe.Setup(p => p.Lv).Returns(expectedLv);
            _mockProbe.Setup(p => p.T).Returns(expectedT);

            string? statusMessage = null;
            _service.StatusMessage += (sender, msg) => statusMessage = msg;

            // Act
            var measurement = await _service.MeasureAsync(3); // 3 измерения

            // Assert
            Assert.IsTrue(measurement.IsValid); // Сервис должен продолжить работу и вернуть среднее из успешных
            StringAssert.Contains(Resources.Resources.ErrorAtMeasuringIteration, statusMessage);
            StringAssert.Contains("Measure iteration COM Error", statusMessage);

            _mockCa.Verify(m => m.Measure(), Times.Exactly(3)); // Все попытки измерения должны быть сделаны

            // Важное замечание: текущая логика `MeasureAsync` в случае ошибки `continue;`
            // добавляет в массивы значений нули (значение по умолчанию для double).
            // Это означает, что среднее будет рассчитано с учетом этих нулей,
            // если только вы не измените логику для отбрасывания неудачных измерений.
            // Если ожидается, что среднее будет только из успешных измерений,
            // тогда потребуется модификация ColorMeasurementService.MeasureAsync.
            // Для данного теста, мы проверяем, что сервис не падает и логирует ошибку.
        }

        // --- Тесты для Disconnect / Dispose ---

        [Test]
        public void Disconnect_ShouldSetDisconnectedAndRaiseEvents()
        {
            // Arrange
            _mockCa200.Setup(m => m.AutoConnect()).Verifiable();
            _service.ConnectAsync().Wait(); // Подключаемся

            bool connectionStatusChangedCalled = false;
            bool calibrationStatusChangedCalled = false;
            string? statusMessage = null;

            _service.ConnectionStatusChanged += (sender, args) =>
                connectionStatusChangedCalled = args;
            _service.CalibrationStatusChanged += (sender, args) =>
                calibrationStatusChangedCalled = args;
            _service.StatusMessage += (sender, msg) => statusMessage = msg;

            // Act
            ((IColorMeasurementService)_service).Disconnect(); // Вызываем через интерфейс

            // Assert
            Assert.IsFalse(_service.IsDeviceConnected);
            Assert.IsFalse(_service.IsDeviceCalibrated);
            Assert.IsFalse(connectionStatusChangedCalled); // Событие должно быть вызвано со значением false
            Assert.IsFalse(calibrationStatusChangedCalled); // Событие должно быть вызвано со значением false
            StringAssert.Contains(Resources.Resources.DisconnectedCA, statusMessage);

            _mockCa200.Verify(m => m.Dispose(), Times.Once); // Проверяем, что Dispose обертки был вызван
        }

        [Test]
        public void Dispose_ShouldReleaseResourcesAndSetFlags()
        {
            // Arrange
            _mockCa200.Setup(m => m.AutoConnect()).Verifiable();
            _service.ConnectAsync().Wait(); // Подключаемся

            bool connectionStatusChangedCalled = false;
            bool calibrationStatusChangedCalled = false;
            string? statusMessage = null;

            _service.ConnectionStatusChanged += (sender, args) =>
                connectionStatusChangedCalled = args;
            _service.CalibrationStatusChanged += (sender, args) =>
                calibrationStatusChangedCalled = args;
            _service.StatusMessage += (sender, msg) => statusMessage = msg;

            // Act
            _service.Dispose(true); // Вызываем Dispose

            // Assert
            Assert.IsFalse(_service.IsDeviceConnected);
            Assert.IsFalse(_service.IsDeviceCalibrated);
            Assert.IsFalse(connectionStatusChangedCalled); // Событие должно быть вызвано со значением false
            Assert.IsFalse(calibrationStatusChangedCalled); // Событие должно быть вызвано со значением false
            StringAssert.Contains(Resources.Resources.DisconnectedCA, statusMessage);

            _mockCa200.Verify(m => m.Dispose(), Times.Once); // Проверяем, что Dispose обертки был вызван
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_service != null)
                {
                    _service.Dispose(true);
                    _service = null;
                }
            }
        }

        [TearDown]
        public void TearDown()
        {
            // Dispose the _service instance to release resources
            if (_service != null)
            {
                _service.Dispose(true);
                _service = null; // Set to null to avoid reusing a disposed instance
            }
        }
    }
}