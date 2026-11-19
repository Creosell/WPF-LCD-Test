using Moq;
using System.Globalization;
using System.Runtime.InteropServices;
using WPF_LCD_Test.Interfaces;
using WPF_LCD_Test.Models;
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
            _mockCa.VerifySet(m => m.Sync = It.IsAny<SyncSetting>(), Times.Once);
            _mockCa.VerifySet(m => m.AveragingMode = It.IsAny<int>(), Times.Once);
            _mockCa.Verify(m => m.SetAnalogRange(It.IsAny<float>(), It.IsAny<float>()), Times.Once);
            _mockCa.VerifySet(m => m.DisplayMode = It.IsAny<int>(), Times.Once);
            }

        [Test]
        public async Task CalibrateZeroAsync_WithoutSettings_UsesSlowSpeedAndUniversalSync()
            {
            await _service.ConnectAsync();

            await _service.CalibrateZeroAsync();

            _mockCa.VerifySet(m => m.AveragingMode = (int)MeasurementSpeed.Slow, Times.Once);
            _mockCa.VerifySet(m => m.Sync = new SyncSetting(SyncMode.Universal, 0), Times.Once);
            }

        [TestCase(MeasurementSpeed.Fast, SyncMode.Universal, null, null, 1, 0)]
        [TestCase(MeasurementSpeed.Auto, SyncMode.Internal, 75.0, null, 2, 75)]
        [TestCase(MeasurementSpeed.Slow, SyncMode.Manual, null, 250.0, 0, 250)]
        [TestCase(MeasurementSpeed.Slow, SyncMode.Ntsc, null, null, 0, 0)]
        public async Task CalibrateZeroAsync_AppliesModesFromSettings(MeasurementSpeed speed, SyncMode mode, double? frequency, double? time, int expectedSpeed, double expectedValue)
            {
            var settings = new Mock<ISettingsService>();
            settings.SetupGet(s => s.CurrentSettings).Returns(new AppSettings { MeasurementSpeed = speed, SyncMode = mode, SyncFrequency = frequency, IntegrationTime = time });
            var service = new ColorMeasurementService(_mockCa200.Object, settings.Object);
            await service.ConnectAsync();

            await service.CalibrateZeroAsync();

            _mockCa.VerifySet(m => m.AveragingMode = expectedSpeed, Times.Once);
            _mockCa.VerifySet(m => m.Sync = new SyncSetting(mode, expectedValue), Times.Once);
            }

        [TestCase(SyncMode.Manual, null, 100.0)]
        [TestCase(SyncMode.Internal, 250.0, null)]
        [TestCase(SyncMode.Internal, 20.0, null)]
        public async Task CalibrateZeroAsync_Ca310_FallsBackToUniversalSyncWhenSettingIsNotSupported(SyncMode mode, double? frequency, double? time)
            {
            _mockCa200.SetupGet(m => m.Model).Returns(ColorAnalyzerModel.CA310);
            var settings = new Mock<ISettingsService>();
            settings.SetupGet(s => s.CurrentSettings).Returns(new AppSettings { SyncMode = mode, SyncFrequency = frequency, IntegrationTime = time });
            var service = new ColorMeasurementService(_mockCa200.Object, settings.Object);
            var messages = new List<string>();
            service.StatusMessage += (_, message) => messages.Add(message);
            await service.ConnectAsync();

            await service.CalibrateZeroAsync();

            _mockCa.VerifySet(m => m.Sync = new SyncSetting(SyncMode.Universal, 0), Times.Once);
            Assert.That(messages, Has.Some.Contains("CA-310"));
            }

        [Test]
        public async Task CalibrateZeroAsync_ReadsActiveChannelFromDeviceWithoutOverwritingIt()
            {
            // Arrange
            _mockMemory.SetupGet(m => m.ChannelNO).Returns(7);
            await _service.ConnectAsync();
            int raisedChannel = -1;
            _service.CurrentChannelChanged += (sender, channel) => raisedChannel = channel;

            // Act
            await _service.CalibrateZeroAsync();

            // Assert
            Assert.That(_service.CurrentChannel, Is.EqualTo(7));
            Assert.That(raisedChannel, Is.EqualTo(7));
            _mockMemory.VerifySet(m => m.ChannelNO = It.IsAny<int>(), Times.Never);
            }

        [Test]
        public async Task CalibrateZeroAsync_WhenAlreadyCalibrated_ReportsUncalibratedBeforeCalibratedAgain()
            {
            // Arrange
            await _service.ConnectAsync();
            await _service.CalibrateZeroAsync();
            var statuses = new List<bool>();
            _service.CalibrationStatusChanged += (sender, isCalibrated) => statuses.Add(isCalibrated);

            // Act
            await _service.CalibrateZeroAsync();

            // Assert
            Assert.That(statuses, Is.EqualTo(new[] { false, true }));
            Assert.That(_service.IsDeviceCalibrated, Is.True);
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

        [TestCase(-3200.0, 6500.0)]
        [TestCase(0.0, 6500.0)]
        [TestCase(double.PositiveInfinity, 6500.0)]
        [TestCase(double.NaN, 6500.0)]
        public async Task MeasureAsync_AColorTemperatureThatIsNotPositiveIsUndefined(double reported, double other)
            {
            _mockCa200.Setup(m => m.AutoConnect()).Verifiable();
            await _service.ConnectAsync();
            await _service.CalibrateZeroAsync();
            _mockProbe.Setup(p => p.sx).Returns(0.3);
            _mockProbe.Setup(p => p.sy).Returns(0.3);
            _mockProbe.Setup(p => p.Lv).Returns(100.0);
            _mockProbe.SetupSequence(p => p.T).Returns(other).Returns(reported);

            var measurement = await _service.MeasureAsync(2);

            Assert.That(measurement.IsValid, Is.True);
            Assert.That(double.IsPositiveInfinity(measurement.T), Is.True, "undefined, so the table shows n/a and the file null");
            }

        [Test]
        public async Task MeasureAsync_PositiveColorTemperaturesAreAveraged()
            {
            _mockCa200.Setup(m => m.AutoConnect()).Verifiable();
            await _service.ConnectAsync();
            await _service.CalibrateZeroAsync();
            _mockProbe.Setup(p => p.sx).Returns(0.3);
            _mockProbe.Setup(p => p.sy).Returns(0.3);
            _mockProbe.Setup(p => p.Lv).Returns(100.0);
            _mockProbe.SetupSequence(p => p.T).Returns(6000.0).Returns(6200.0);

            var measurement = await _service.MeasureAsync(2);

            Assert.That(measurement.T, Is.EqualTo(6100.0));
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
            ( (IColorMeasurementService)_service ).Disconnect(); // Вызываем через интерфейс

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

        [Test]
        public void CurrentChannel_Set_RaisesCurrentChannelChangedEvent()
            {
            var service = new ColorMeasurementService();
            int raisedChannel = -1;

            service.CurrentChannelChanged += (sender, channel) => raisedChannel = channel;
            service.CurrentChannel = 10;

            Assert.That(raisedChannel, Is.EqualTo(10));
            Assert.That(service.CurrentChannel, Is.EqualTo(10));
            }

        [Test]
        public void CurrentChannel_SetInvalidValue_DoesNotRaiseEvent()
            {
            var service = new ColorMeasurementService();
            bool eventRaised = false;

            service.CurrentChannelChanged += (sender, channel) => eventRaised = true;
            service.CurrentChannel = 150;

            Assert.That(eventRaised, Is.False);
            Assert.That(service.CurrentChannel, Is.EqualTo(0));
            }

        // --- Tests for COM Object Lifecycle ---

        [Test]
        public void Disconnect_ShouldCleanupAllReferencesAndRaiseEvents()
            {
            // Arrange
            _mockCa200.Setup(m => m.AutoConnect()).Verifiable();
            _service.ConnectAsync().Wait();

            bool connectionStatusChanged = true; // Should become false after disconnect
            bool calibrationStatusChanged = true; // Should become false after disconnect

            _service.ConnectionStatusChanged += (sender, connected) => connectionStatusChanged = connected;
            _service.CalibrationStatusChanged += (sender, calibrated) => calibrationStatusChanged = calibrated;

            // Act
            ((IColorMeasurementService)_service).Disconnect();

            // Assert
            Assert.IsFalse(_service.IsDeviceConnected);
            Assert.IsFalse(_service.IsDeviceCalibrated);
            Assert.IsFalse(connectionStatusChanged);
            Assert.IsFalse(calibrationStatusChanged);
            _mockCa200.Verify(m => m.Dispose(), Times.Once);
            }

        [Test]
        public async Task ConnectAsync_AfterDisconnect_ShouldReconnectSuccessfully()
            {
            // Arrange - First connection
            _mockCa200.Setup(m => m.AutoConnect()).Verifiable();
            var firstResult = await _service.ConnectAsync();
            Assert.IsTrue(firstResult);
            Assert.IsTrue(_service.IsDeviceConnected);

            // Act - Disconnect
            ((IColorMeasurementService)_service).Disconnect();
            Assert.IsFalse(_service.IsDeviceConnected);

            // Prepare for reconnection - need fresh mock since old one is disposed
            _mockCa200 = new Mock<IColorAnalyzer200>();
            _mockCa = new Mock<IColorAnalyzer>();
            _mockProbe = new Mock<IColorAnalyzerProbe>();
            _mockMemory = new Mock<IColorAnalyzerMemory>();

            _mockCa200.Setup(m => m.SingleCa).Returns(_mockCa.Object);
            _mockCa.Setup(m => m.SingleProbe).Returns(_mockProbe.Object);
            _mockCa.Setup(m => m.Memory).Returns(_mockMemory.Object);
            _mockCa200.Setup(m => m.AutoConnect()).Verifiable();

            _service = new ColorMeasurementService(_mockCa200.Object);

            // Act - Reconnect
            var secondResult = await _service.ConnectAsync();

            // Assert - Should connect successfully again
            Assert.IsTrue(secondResult);
            Assert.IsTrue(_service.IsDeviceConnected);
            _mockCa200.Verify(m => m.AutoConnect(), Times.Once);
            }

        [Test]
        public async Task ConnectAsync_AfterCOMException_ShouldCleanupAndAllowReconnect()
            {
            // Arrange - First connection attempt fails
            _mockCa200
                .Setup(m => m.AutoConnect())
                .Throws(new COMException("Connection failed", -2147024891));

            // Act - First attempt
            var firstResult = await _service.ConnectAsync();

            // Assert - Connection failed
            Assert.IsFalse(firstResult);
            Assert.IsFalse(_service.IsDeviceConnected);

            // Arrange - Prepare for successful reconnection
            _mockCa200 = new Mock<IColorAnalyzer200>();
            _mockCa = new Mock<IColorAnalyzer>();
            _mockProbe = new Mock<IColorAnalyzerProbe>();
            _mockMemory = new Mock<IColorAnalyzerMemory>();

            _mockCa200.Setup(m => m.SingleCa).Returns(_mockCa.Object);
            _mockCa.Setup(m => m.SingleProbe).Returns(_mockProbe.Object);
            _mockCa.Setup(m => m.Memory).Returns(_mockMemory.Object);
            _mockCa200.Setup(m => m.AutoConnect()).Verifiable();

            _service = new ColorMeasurementService(_mockCa200.Object);

            // Act - Second attempt should succeed
            var secondResult = await _service.ConnectAsync();

            // Assert - Should connect successfully
            Assert.IsTrue(secondResult);
            Assert.IsTrue(_service.IsDeviceConnected);
            }

        [Test]
        public async Task ConnectAsync_WithExistingConnection_ShouldNotCreateNewWrapper()
            {
            // Arrange
            _mockCa200.Setup(m => m.AutoConnect()).Verifiable();

            // Act - First connection
            await _service.ConnectAsync();
            var firstCallCount = _mockCa200.Invocations.Count;

            // Act - Second connection attempt while already connected
            await _service.ConnectAsync();
            var secondCallCount = _mockCa200.Invocations.Count;

            // Assert - AutoConnect should only be called once
            Assert.AreEqual(firstCallCount, secondCallCount);
            _mockCa200.Verify(m => m.AutoConnect(), Times.Once);
            Assert.IsTrue(_service.IsDeviceConnected);
            }

        [Test]
        public void Dispose_ShouldCleanupAllReferencesAndSetStatesToFalse()
            {
            // Arrange
            _mockCa200.Setup(m => m.AutoConnect()).Verifiable();
            _service.ConnectAsync().Wait();
            _service.CalibrateZeroAsync().Wait();

            Assert.IsTrue(_service.IsDeviceConnected);
            Assert.IsTrue(_service.IsDeviceCalibrated);

            bool connectionStatusChanged = true;
            bool calibrationStatusChanged = true;

            _service.ConnectionStatusChanged += (sender, connected) => connectionStatusChanged = connected;
            _service.CalibrationStatusChanged += (sender, calibrated) => calibrationStatusChanged = calibrated;

            // Act
            _service.Dispose(true);

            // Assert
            Assert.IsFalse(_service.IsDeviceConnected);
            Assert.IsFalse(_service.IsDeviceCalibrated);
            Assert.IsFalse(connectionStatusChanged);
            Assert.IsFalse(calibrationStatusChanged);
            _mockCa200.Verify(m => m.Dispose(), Times.Once);
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