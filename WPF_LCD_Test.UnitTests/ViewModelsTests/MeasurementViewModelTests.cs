using Moq;
using WPF_LCD_Test.Commands;
using WPF_LCD_Test.Interfaces;
using WPF_LCD_Test.Models;
using WPF_LCD_Test.Services; // Доступ к MeasurementStatusService
using WPF_LCD_Test.ViewModels;

// Для доступа к ключам ресурсов (RunExternalAppNotFoundErr и др.)
using static WPF_LCD_Test.Resources.Resources;

namespace WPF_LCD_Test.UnitTests.ViewModelsTests
    {
    [TestFixture]
    public class MeasurementViewModelTests
        {
        private Mock<IColorMeasurementService> _mockColorMeasurementService;
        private Mock<IFileService> _mockFileService;
        private Mock<IDialogService> _mockDialogService;
        private Mock<ILocalizationService> _mockLocalizationService;
        private Mock<IDispatcher> _mockDispatcher;
        private Mock<IUploadService> _mockUploadService;
        private Mock<ISettingsService> _mockSettingsService;
        private Mock<IPathProvider> _mockPathProvider;
        private Mock<IMeasurementStatusService> _mockMeasurementStatusService;
        private MeasurementViewModel _viewModel;

        [SetUp]
        public void Setup()
            {
            _mockColorMeasurementService = new Mock<IColorMeasurementService>();
            _mockFileService = new Mock<IFileService>();
            _mockDialogService = new Mock<IDialogService>();
            _mockLocalizationService = new Mock<ILocalizationService>();
            _mockDispatcher = new Mock<IDispatcher>();
            _mockUploadService = new Mock<IUploadService>();
            _mockSettingsService = new Mock<ISettingsService>();
            _mockPathProvider = new Mock<IPathProvider>();
            _mockMeasurementStatusService = new Mock<IMeasurementStatusService>();

            // Setup PathProvider
            _mockPathProvider.Setup(p => p.BaseDirectory).Returns("C:\\TestApp");
            _mockPathProvider.Setup(p => p.ConfigDirectory).Returns("C:\\TestApp\\config");
            _mockPathProvider.Setup(p => p.DataDirectory).Returns("C:\\TestApp\\data");

            // Setup MeasurementStatusService
            var measurementStatuses = new System.Collections.ObjectModel.ObservableCollection<MeasurementStatus>();
            foreach (var location in Enum.GetValues<MeasurementLocation>())
                {
                measurementStatuses.Add(new MeasurementStatus(location.ToString()));
                }
            _mockMeasurementStatusService.Setup(m => m.AllMeasurementButtonStatuses).Returns(measurementStatuses);

            // --- ИСПРАВЛЕННАЯ НАСТРОЙКА МОКА ---

            // Мы настраиваем мок так, чтобы он возвращал строку "Loc_КЛЮЧ {invalid}".
            // 1. Префикс "Loc_" делает строку отличной от ключа (LogHandler принимает её).
            // 2. Часть "{invalid}" вызывает FormatException внутри LogHandler.
            // 3. LogHandler ловит ошибку и выводит: "Loc_КЛЮЧ {invalid} [Args: арг1, арг2]"
            // Это позволяет тестам видеть и Ключ, и Аргументы в логе.
            _mockLocalizationService
                .Setup(s => s.GetString(It.IsAny<string>()))
                .Returns((string key) => $"Loc_{key} {{invalid}}");

            // Примечание: Setup для GetString(key, args) больше не нужен,
            // так как новый LogHandler его не использует.

            _viewModel = new MeasurementViewModel(
                _mockColorMeasurementService.Object,
                _mockFileService.Object,
                _mockDialogService.Object,
                _mockLocalizationService.Object,
                _mockDispatcher.Object,
                _mockUploadService.Object,
                _mockSettingsService.Object,
                _mockPathProvider.Object,
                _mockMeasurementStatusService.Object
            );

            // Сбрасываем статический счетчик попыток измерений между тестами
            _viewModel.ClearAllAttemptCounts();
            }

        [TearDown]
        public void TearDown()
            {
            _viewModel.Dispose();
            }

        // --- Тесты для валидации статусов измерений ---

        [Test]
        public void AreAllStatusesRepresentedInMeasurements_ReturnsTrue_WhenAllRequiredMeasurementsPresent()
            {
            // Arrange
            _viewModel.ExecuteApplySerialNumber("Sn123");
            Assert.That(_viewModel.CurrentDevice, Is.Not.Null);

            // ИСПРАВЛЕНИЕ: Берем список точек из Singleton сервиса, так как во ViewModel нет публичного свойства
            var allStatuses = _mockMeasurementStatusService.Object.AllMeasurementButtonStatuses;

            foreach (var status in allStatuses)
                {
                // Добавляем измерение для каждой требуемой точки
                _viewModel.CurrentDevice.AddMeasurement(new Measurement { Location = status.Location, IsValid = true });
                }

            // Act
            bool result = _viewModel.AreAllStatusesRepresentedInMeasurements();

            // Assert
            Assert.That(result, Is.True);
            }

        [Test]
        public void AreAllStatusesRepresentedInMeasurements_ReturnsFalse_WhenNotAllPresent()
            {
            // Arrange
            _viewModel.ExecuteApplySerialNumber("SnPartial");
            Assert.That(_viewModel.CurrentDevice, Is.Not.Null);

            // Добавляем только одно измерение
            _viewModel.CurrentDevice.AddMeasurement(new Measurement { Location = "TopLeft", IsValid = true });

            // Act
            bool result = _viewModel.AreAllStatusesRepresentedInMeasurements();

            // Assert
            Assert.That(result, Is.False);
            }

        [Test]
        public void AreAllStatusesRepresentedInMeasurements_ReturnsFalse_WhenNoDevice()
            {
            _viewModel.CurrentDevice = null;
            bool result = _viewModel.AreAllStatusesRepresentedInMeasurements();
            Assert.That(result, Is.False);
            }

        [Test]
        public void AreAllStatusesRepresentedInMeasurements_ReturnsFalse_WhenDeviceHasNoMeasurements()
            {
            _viewModel.ExecuteApplySerialNumber("SnEmpty");
            Assert.That(_viewModel.CurrentDevice, Is.Not.Null);
            Assert.That(_viewModel.CurrentDevice.Measurements, Is.Empty);

            bool result = _viewModel.AreAllStatusesRepresentedInMeasurements();
            Assert.That(result, Is.False);
            }

        // --- Тесты для ReportGenerateCommand ---

        [Test]
        public void ReportGenerateCommand_ExecutableNotFound_LogsErrorAndDoesNotStartGeneration()
            {
            // Arrange
            _viewModel.IsReportGenerating = false;
            // Сбрасываем лог, чтобы проверить запись
            _viewModel.ExecuteClearLog();

            var appDir = AppDomain.CurrentDomain.BaseDirectory;
            var exeName = "ReportGenerator.exe";
            var expectedExePath = Path.Combine(appDir, exeName);

            if (File.Exists(expectedExePath))
                {
                File.Delete(expectedExePath);
                }

            // Act
            if (_viewModel.ReportGenerateCommand.CanExecute(null))
                {
                _viewModel.ReportGenerateCommand.Execute(null);
                }

            Assert.Multiple(() =>
            {
                // Assert
                Assert.That(_viewModel.IsReportGenerating, Is.False, "Flag should correspond to finished/failed state");

                // Проверяем, что в лог попал ключ ошибки (RunExternalAppNotFoundErr)
                // Текст лога берется из свойства LogText
                Assert.That(_viewModel.LogText, Does.Contain(nameof(RunExternalAppNotFoundErr)));
            });
            Assert.That(_viewModel.LogText, Does.Contain(expectedExePath));
            }

        [Test]
        public void ReportGenerateCommand_IsActive_WhenNotGenerating()
            {
            _viewModel.IsReportGenerating = false;
            var canExecute = _viewModel.ReportGenerateCommand.CanExecute(null);
            Assert.That(canExecute, Is.True);
            }

        [Test]
        public void ReportGenerateCommand_IsInactive_WhenGenerating()
            {
            _viewModel.IsReportGenerating = true;
            var canExecute = _viewModel.ReportGenerateCommand.CanExecute(null);
            Assert.That(canExecute, Is.False);
            }

        // --- Тесты для ExecuteApplySerialNumber ---

        [Test]
        public void ExecuteApplySerialNumber_InvalidFormat_ShowsErrorDialog()
            {
            // Arrange
            var invalidSerialNumber = "SN-With-Dashes!";

            // Act
            _viewModel.ExecuteApplySerialNumber(invalidSerialNumber);

            // Assert
            Assert.That(_viewModel.IsSerialNumberConfirmed, Is.False);
            _mockDialogService.Verify(d => d.ShowMessage(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            }

        [Test]
        public void ExecuteApplySerialNumber_ValidSN_CreatesNewDevice()
            {
            // Arrange
            var validSerialNumber = "SN12345";

            // Act
            _viewModel.ExecuteApplySerialNumber(validSerialNumber);

            // Assert
            Assert.That(_viewModel.IsSerialNumberConfirmed, Is.True);
            Assert.That(_viewModel.SerialNumber, Is.EqualTo(validSerialNumber));
            Assert.That(_viewModel.CurrentDevice, Is.Not.Null);
            Assert.That(_viewModel.CurrentDevice.SerialNumber, Is.EqualTo(validSerialNumber));
            }

        [Test]
        public void ExecuteApplySerialNumber_SameSN_DoesNotRecreate()
            {
            // Arrange
            var serialNumber = "SN12345";
            _viewModel.ExecuteApplySerialNumber(serialNumber);
            var originalDevice = _viewModel.CurrentDevice;

            // Act - apply same SN again
            _viewModel.ExecuteApplySerialNumber(serialNumber);

            // Assert - should be same device instance
            Assert.That(_viewModel.CurrentDevice, Is.SameAs(originalDevice));
            }

        [Test]
        public void ExecuteApplySerialNumber_ClearsInputFocus_OnSuccess()
            {
            // Arrange
            var validSerialNumber = "SN12345";
            bool eventFired = false;
            _viewModel.RequestClearInputFocus += (s, e) => eventFired = true;

            // Act
            _viewModel.ExecuteApplySerialNumber(validSerialNumber);

            // Assert
            Assert.That(eventFired, Is.True);
            }

        [Test]
        public void ExecuteApplySerialNumber_PropertyChanged_ForSerialNumberAndConfirmed()
            {
            // Arrange
            var validSerialNumber = "SN12345";
            var propertiesChanged = new List<string>();
            _viewModel.PropertyChanged += (s, e) => propertiesChanged.Add(e.PropertyName!);

            // Act
            _viewModel.ExecuteApplySerialNumber(validSerialNumber);

            // Assert
            Assert.That(propertiesChanged, Does.Contain(nameof(_viewModel.SerialNumber)));
            Assert.That(propertiesChanged, Does.Contain(nameof(_viewModel.IsSerialNumberConfirmed)));
            }

        [Test]
        public void ExecuteApplySerialNumber_LogsCurrentSN_OnSuccess()
            {
            // Arrange
            var validSerialNumber = "SN12345";
            _viewModel.ExecuteClearLog();

            // Act
            _viewModel.ExecuteApplySerialNumber(validSerialNumber);

            // Assert
            Assert.That(_viewModel.LogText, Does.Contain("CurrentSN"));
            Assert.That(_viewModel.LogText, Does.Contain(validSerialNumber));
            }

        // --- Тесты для ExecuteConnectAsync ---

        [Test]
        public async Task ExecuteConnectAsync_Successful_UpdatesConnectionStatus()
            {
            // Arrange
            _mockColorMeasurementService.Setup(s => s.ConnectAsync()).ReturnsAsync(true);

            // Act
            await ((RelayCommand)_viewModel.ZeroCalibrationCommand).ExecuteAsync(null);

            // Assert
            _mockColorMeasurementService.Verify(s => s.ConnectAsync(), Times.AtLeastOnce);
            }

        [Test]
        public async Task ExecuteConnectAsync_Failed_CallsDisconnectForCleanup()
            {
            // Arrange
            _mockColorMeasurementService.Setup(s => s.ConnectAsync()).ReturnsAsync(false);

            // Act
            await ((RelayCommand)_viewModel.ZeroCalibrationCommand).ExecuteAsync(null);

            // Assert - ExecuteDisconnect(force: true) is called to cleanup COM objects on failed connection
            _mockColorMeasurementService.Verify(s => s.Disconnect(), Times.Once);
            }

        [Test]
        public async Task ExecuteConnectAsync_Exception_CallsDisconnectForCleanup()
            {
            // Arrange
            _mockColorMeasurementService.Setup(s => s.ConnectAsync()).ThrowsAsync(new InvalidOperationException("Connection failed"));

            // Act
            await ((RelayCommand)_viewModel.ZeroCalibrationCommand).ExecuteAsync(null);

            // Assert - ExecuteDisconnect(force: true) is called to cleanup COM objects on connection exception
            _mockColorMeasurementService.Verify(s => s.Disconnect(), Times.Once);
            }

        // --- Тесты для ExecuteZeroCalibrationAsync ---

        [Test]
        public async Task ExecuteZeroCalibrationAsync_NotConnected_ConnectsFirst()
            {
            // Arrange
            _viewModel.IsDeviceConnected = false;
            _mockColorMeasurementService.Setup(s => s.ConnectAsync())
                .ReturnsAsync(true)
                .Callback(() => _viewModel.IsDeviceConnected = true); // Simulate connection success
            _mockColorMeasurementService.Setup(s => s.CalibrateZeroAsync()).ReturnsAsync(true);
            _mockColorMeasurementService.SetupGet(s => s.ProbeSN).Returns("SomeProbe");
            _mockColorMeasurementService.SetupGet(s => s.CurrentChannel).Returns(5);

            // Act
            await ((RelayCommand)_viewModel.ZeroCalibrationCommand).ExecuteAsync(null);

            // Assert
            _mockColorMeasurementService.Verify(s => s.ConnectAsync(), Times.Once);
            _mockColorMeasurementService.Verify(s => s.CalibrateZeroAsync(), Times.Once);
            }

        [Test]
        public async Task ExecuteZeroCalibrationAsync_CalibrationFails_ShowsDialog()
            {
            // Arrange
            _mockColorMeasurementService.SetupGet(s => s.IsDeviceConnected).Returns(true);
            _mockColorMeasurementService.Setup(s => s.CalibrateZeroAsync()).ReturnsAsync(false);
            _mockColorMeasurementService.SetupGet(s => s.ProbeSN).Returns("SomeProbe");

            // Act
            await ((RelayCommand)_viewModel.ZeroCalibrationCommand).ExecuteAsync(null);

            // Assert
            _mockDialogService.Verify(d => d.ShowMessage(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            // ExecuteDisconnect(force: true) is called to cleanup COM objects on calibration failure
            _mockColorMeasurementService.Verify(s => s.Disconnect(), Times.Once);
            }

        [Test]
        public async Task ExecuteZeroCalibrationAsync_QAProbe_SwitchesChannel()
            {
            // Arrange
            _mockColorMeasurementService.SetupGet(s => s.IsDeviceConnected).Returns(true);
            _mockColorMeasurementService.Setup(s => s.CalibrateZeroAsync()).ReturnsAsync(true);
            _mockColorMeasurementService.SetupGet(s => s.ProbeSN).Returns("08954195"); // QA Probe
            _mockColorMeasurementService.SetupGet(s => s.CurrentChannel).Returns(5);
            _mockColorMeasurementService.SetupSet(s => s.CurrentChannel = 1).Verifiable();

            // Act
            await ((RelayCommand)_viewModel.ZeroCalibrationCommand).ExecuteAsync(null);

            // Assert
            _mockColorMeasurementService.VerifySet(s => s.CurrentChannel = 1, Times.Once);
            _mockSettingsService.Verify(s => s.UpdateChannel(1), Times.Once);
            _mockDialogService.Verify(d => d.ShowMessage(It.Is<string>(msg => msg.Contains("QA CA-310")), It.IsAny<string>()), Times.Once);
            }

        // --- Тесты для ExecuteSaveResultsAsync ---

        [Test]
        public async Task ExecuteSaveResultsAsync_NoDevice_ReturnsFalseWithoutDialog()
            {
            // Arrange
            _viewModel.CurrentDevice = null;

            // Act
            var result = await ((RelayCommand)_viewModel.SaveResultsCommand).ExecuteAsync(null);

            // Assert - CanExecute returns false, so method returns early without showing dialog
            Assert.That(result, Is.False);
            _mockDialogService.Verify(d => d.ShowMessage(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            }

        [Test]
        public async Task ExecuteSaveResultsAsync_NoMeasurements_ReturnsFalseWithoutDialog()
            {
            // Arrange
            _viewModel.ExecuteApplySerialNumber("SN12345");
            Assert.That(_viewModel.CurrentDevice!.Measurements.Count, Is.EqualTo(0));

            // Act
            var result = await ((RelayCommand)_viewModel.SaveResultsCommand).ExecuteAsync(null);

            // Assert - CanExecute returns false when no measurements, so method returns early
            Assert.That(result, Is.False);
            _mockDialogService.Verify(d => d.ShowMessage(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            }

        [Test]
        public async Task ExecuteSaveResultsAsync_IncompleteData_ShowsWarning()
            {
            // Arrange
            _viewModel.ExecuteApplySerialNumber("SN12345");
            _viewModel.CurrentDevice!.AddMeasurement(new Measurement { Location = MeasurementLocation.RedColor.ToString(), IsValid = true });
            _mockDialogService.Setup(d => d.ShowQuestion(It.IsAny<string>(), It.IsAny<string>())).Returns(true); // User accepts

            // Act
            var result = await ((RelayCommand)_viewModel.SaveResultsCommand).ExecuteAsync(null);

            // Assert
            _mockDialogService.Verify(d => d.ShowQuestion(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            // Result depends on fileService.SaveDeviceDataToJsonAsync which we haven't mocked, so we can't assert result
            }

        [Test]
        public async Task ExecuteSaveResultsAsync_UserCancels_SaveDoesNotProceed()
            {
            // Arrange
            _viewModel.ExecuteApplySerialNumber("SN12345");
            _viewModel.CurrentDevice!.AddMeasurement(new Measurement { Location = MeasurementLocation.RedColor.ToString(), IsValid = true, x = 0.3, y = 0.3, Lv = 100, T = 6500 });

            // Verify that not all statuses are represented (this should trigger ShowQuestion)
            var allStatusesRepresented = _viewModel.AreAllStatusesRepresentedInMeasurements();
            Assert.That(allStatusesRepresented, Is.False, "Test setup error: all statuses should NOT be represented");

            // Setup incomplete data warning - user cancels
            bool questionResult = false; // User cancels
            _mockDialogService.Setup(d => d.ShowQuestion(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(() => questionResult);

            // Setup fileService - should not be called if user cancels
            _mockFileService.Setup(f => f.SaveDeviceDataToJsonAsync(It.IsAny<DeviceUnderTest>()))
                .ReturnsAsync(true);

            // Act
            var result = await ((RelayCommand)_viewModel.SaveResultsCommand).ExecuteAsync(null);

            // Assert
            // First verify that ShowQuestion was called (because not all measurements are complete)
            _mockDialogService.Verify(d => d.ShowQuestion(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            // Verify that SaveDeviceDataToJsonAsync was NOT called (because user cancelled)
            _mockFileService.Verify(f => f.SaveDeviceDataToJsonAsync(It.IsAny<DeviceUnderTest>()), Times.Never);
            // User cancelled, result should be false
            Assert.That(result, Is.False);
            }

        [Test]
        public async Task ExecuteSaveResultsAsync_Success_CallsFileService()
            {
            // Arrange
            _viewModel.ExecuteApplySerialNumber("SN12345");
            var allStatuses = _mockMeasurementStatusService.Object.AllMeasurementButtonStatuses;
            foreach (var status in allStatuses)
                {
                _viewModel.CurrentDevice!.AddMeasurement(new Measurement { Location = status.Location, IsValid = true, x = 0.3, y = 0.3, Lv = 100, T = 6500 });
                }
            _mockFileService.Setup(f => f.SaveDeviceDataToJsonAsync(It.IsAny<DeviceUnderTest>())).ReturnsAsync(true);

            // Act
            var result = await ((RelayCommand)_viewModel.SaveResultsCommand).ExecuteAsync(null);

            // Assert
            Assert.That(result, Is.True);
            _mockFileService.Verify(f => f.SaveDeviceDataToJsonAsync(_viewModel.CurrentDevice), Times.Once);
            }

        [Test]
        public async Task ExecuteSaveResultsAsync_SetsDeviceConfiguration_BeforeSave()
            {
            // Arrange
            _viewModel.ExecuteApplySerialNumber("SN12345");
            _viewModel.SelectedDeviceConfiguration = "TestConfig";
            _viewModel.IsTvCheckboxChecked = true;
            var allStatuses = _mockMeasurementStatusService.Object.AllMeasurementButtonStatuses;
            foreach (var status in allStatuses)
                {
                _viewModel.CurrentDevice!.AddMeasurement(new Measurement { Location = status.Location, IsValid = true, x = 0.3, y = 0.3, Lv = 100, T = 6500 });
                }
            _mockFileService.Setup(f => f.SaveDeviceDataToJsonAsync(It.IsAny<DeviceUnderTest>())).ReturnsAsync(true);

            // Act
            await ((RelayCommand)_viewModel.SaveResultsCommand).ExecuteAsync(null);

            // Assert
            Assert.That(_viewModel.CurrentDevice!.DeviceConfiguration, Is.EqualTo("TestConfig"));
            Assert.That(_viewModel.CurrentDevice.IsTV, Is.True);
            }

        // --- Тесты для ExecuteUploadReportsAsync ---

        [Test]
        public async Task ExecuteUploadReportsAsync_NoConfiguration_ShowsError()
            {
            // Arrange
            _viewModel.SelectedDeviceConfiguration = string.Empty;

            // Act
            await ((RelayCommand)_viewModel.UploadReportsCommand).ExecuteAsync(null);

            // Assert
            _mockDialogService.Verify(d => d.ShowMessage(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            _mockUploadService.Verify(u => u.UploadReportsAsync(), Times.Never);
            }

        [Test]
        public async Task ExecuteUploadReportsAsync_Success_LogsSuccess()
            {
            // Arrange
            _viewModel.SelectedDeviceConfiguration = "TestConfig";
            _mockUploadService.Setup(u => u.UploadReportsAsync()).ReturnsAsync(true);
            _viewModel.ExecuteClearLog();

            // Act
            await ((RelayCommand)_viewModel.UploadReportsCommand).ExecuteAsync(null);

            // Assert
            _mockUploadService.Verify(u => u.UploadReportsAsync(), Times.Once);
            Assert.That(_viewModel.LogText, Does.Contain("StartingReportUploading"));
            }

        [Test]
        public async Task ExecuteUploadReportsAsync_Failure_LogsFailure()
            {
            // Arrange
            _viewModel.SelectedDeviceConfiguration = "TestConfig";
            _mockUploadService.Setup(u => u.UploadReportsAsync()).ReturnsAsync(false);
            _viewModel.ExecuteClearLog();

            // Act
            await ((RelayCommand)_viewModel.UploadReportsCommand).ExecuteAsync(null);

            // Assert
            _mockUploadService.Verify(u => u.UploadReportsAsync(), Times.Once);
            Assert.That(_viewModel.LogText, Does.Contain("UploadFailed"));
            }

        [Test]
        public async Task ExecuteUploadReportsAsync_SetsUploadingFlag_DuringOperation()
            {
            // Arrange
            _viewModel.SelectedDeviceConfiguration = "TestConfig";
            bool flagWasSet = false;
            _mockUploadService.Setup(u => u.UploadReportsAsync())
                .Callback(() => flagWasSet = _viewModel.IsUploadingReports)
                .ReturnsAsync(true);

            // Act
            await ((RelayCommand)_viewModel.UploadReportsCommand).ExecuteAsync(null);

            // Assert
            Assert.That(flagWasSet, Is.True, "Flag should be set during operation");
            Assert.That(_viewModel.IsUploadingReports, Is.False, "Flag should be cleared after operation");
            }

        // --- Тесты для ExecuteMeasureAsync ---

        [Test]
        public async Task ExecuteMeasureAsync_DeviceNotConnected_UpdatesStatusWithError()
            {
            // Arrange
            _viewModel.IsDeviceConnected = false;
            _viewModel.IsDeviceCalibrated = false;
            var location = MeasurementLocation.RedColor.ToString();

            // Act
            await ((RelayCommand)_viewModel.MeasureCommand).ExecuteAsync(location);

            // Assert
            // Verify that measurement was not attempted due to connection/calibration guards
            _mockColorMeasurementService.Verify(s => s.MeasureAsync(It.IsAny<int>()), Times.Never);
            }

        [Test]
        public async Task ExecuteMeasureAsync_NoSerialNumber_CommandDoesNotExecute()
            {
            // Arrange
            _viewModel.SerialNumber = string.Empty;
            _viewModel.IsDeviceConnected = true;
            _viewModel.IsDeviceCalibrated = true;
            var location = MeasurementLocation.RedColor.ToString();

            // Act
            await ((RelayCommand)_viewModel.MeasureCommand).ExecuteAsync(location);

            // Assert - CanExecute returns false when SerialNumber is empty, so command doesn't execute
            _mockColorMeasurementService.Verify(s => s.MeasureAsync(It.IsAny<int>()), Times.Never);
            _mockDialogService.Verify(d => d.ShowMessage(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            }

        [Test]
        public async Task ExecuteMeasureAsync_ValidMeasurement_AddsMeasurementToDevice()
            {
            // Arrange
            _viewModel.ExecuteApplySerialNumber("SN12345");
            _viewModel.IsDeviceConnected = true;
            _viewModel.IsDeviceCalibrated = true;
            var location = MeasurementLocation.WhiteColor.ToString();
            var measurement = new Measurement
                {
                Location = location,
                x = 0.3127,
                y = 0.3290,
                Lv = 100,
                T = 6500,
                IsValid = true
                };

            _mockColorMeasurementService.Setup(s => s.MeasureAsync(It.IsAny<int>())).ReturnsAsync(measurement);

            // Act
            await ((RelayCommand)_viewModel.MeasureCommand).ExecuteAsync(location);

            // Assert
            Assert.That(_viewModel.CurrentDevice, Is.Not.Null);
            Assert.That(_viewModel.CurrentDevice.Measurements.Count, Is.EqualTo(1));
            Assert.That(_viewModel.CurrentDevice.Measurements[0].Location, Is.EqualTo(location));
            }

        [Test]
        public async Task ExecuteMeasureAsync_MeasurementException_HandlesGracefully()
            {
            // Arrange
            _viewModel.ExecuteApplySerialNumber("SN12345");
            _viewModel.IsDeviceConnected = true;
            _viewModel.IsDeviceCalibrated = true;
            var location = MeasurementLocation.RedColor.ToString();

            _mockColorMeasurementService.Setup(s => s.MeasureAsync(It.IsAny<int>()))
                .ThrowsAsync(new InvalidOperationException("Measurement device error"));

            // Act & Assert - should not throw
            Assert.DoesNotThrowAsync(async () =>
                await ((RelayCommand)_viewModel.MeasureCommand).ExecuteAsync(location));

            _mockDialogService.Verify(d => d.ShowMessage(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            }

        // --- Тесты для MeasurementValidation ---

        [Test]
        public async Task MeasurementValidation_BlackColor_LvTooHigh_ReturnsFalse()
            {
            // Arrange
            _viewModel.ExecuteApplySerialNumber("SN12345");
            _viewModel.IsDeviceConnected = true;
            _viewModel.IsDeviceCalibrated = true;
            var location = MeasurementLocation.BlackColor.ToString();
            var measurement = new Measurement
                {
                Location = location,
                x = 0.3,
                y = 0.3,
                Lv = 10, // Too high for black (should be < 5)
                T = 6500,
                IsValid = true
                };

            _mockColorMeasurementService.Setup(s => s.MeasureAsync(It.IsAny<int>())).ReturnsAsync(measurement);
            _mockDialogService.Setup(d => d.ShowMessage(It.IsAny<string>(), It.IsAny<string>()));

            // Act
            await ((RelayCommand)_viewModel.MeasureCommand).ExecuteAsync(location);

            // Assert - should show retry dialog due to validation failure
            _mockDialogService.Verify(d => d.ShowMessage(It.Is<string>(msg => msg.Contains("unexpectedly high") || msg.Contains("too high")), It.IsAny<string>()), Times.Once);
            }

        [Test]
        public async Task MeasurementValidation_BlackColor_Valid_ReturnsTrue()
            {
            // Arrange
            _viewModel.ExecuteApplySerialNumber("SN12345");
            _viewModel.IsDeviceConnected = true;
            _viewModel.IsDeviceCalibrated = true;
            var location = MeasurementLocation.BlackColor.ToString();
            var measurement = new Measurement
                {
                Location = location,
                x = 0.3,
                y = 0.3,
                Lv = 0.5, // Valid for black (< 5)
                T = 6500,
                IsValid = true
                };

            _mockColorMeasurementService.Setup(s => s.MeasureAsync(It.IsAny<int>())).ReturnsAsync(measurement);

            // Act
            await ((RelayCommand)_viewModel.MeasureCommand).ExecuteAsync(location);

            // Assert - should not show error dialog
            _mockDialogService.Verify(d => d.ShowMessage(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            Assert.That(_viewModel.CurrentDevice!.Measurements.Count, Is.EqualTo(1));
            }

        [Test]
        public async Task MeasurementValidation_NonBlackColor_LvTooLow_ReturnsFalse()
            {
            // Arrange
            _viewModel.ExecuteApplySerialNumber("SN12345");
            _viewModel.IsDeviceConnected = true;
            _viewModel.IsDeviceCalibrated = true;
            var location = MeasurementLocation.RedColor.ToString();
            var measurement = new Measurement
                {
                Location = location,
                x = 0.67,
                y = 0.33,
                Lv = 3, // Too low for non-black (should be > 5)
                T = 6500,
                IsValid = true
                };

            _mockColorMeasurementService.Setup(s => s.MeasureAsync(It.IsAny<int>())).ReturnsAsync(measurement);
            _mockDialogService.Setup(d => d.ShowMessage(It.IsAny<string>(), It.IsAny<string>()));

            // Act
            await ((RelayCommand)_viewModel.MeasureCommand).ExecuteAsync(location);

            // Assert
            _mockDialogService.Verify(d => d.ShowMessage(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            }

        [Test]
        public async Task MeasurementValidation_RedColor_OutOfRange_ReturnsFalse()
            {
            // Arrange
            _viewModel.ExecuteApplySerialNumber("SN12345");
            _viewModel.IsDeviceConnected = true;
            _viewModel.IsDeviceCalibrated = true;
            var location = MeasurementLocation.RedColor.ToString();
            var measurement = new Measurement
                {
                Location = location,
                x = 0.5, // Out of range (should be 0.67 ± 0.1)
                y = 0.5, // Out of range (should be 0.33 ± 0.1)
                Lv = 100,
                T = 6500,
                IsValid = true
                };

            _mockColorMeasurementService.Setup(s => s.MeasureAsync(It.IsAny<int>())).ReturnsAsync(measurement);
            _mockDialogService.Setup(d => d.ShowMessage(It.IsAny<string>(), It.IsAny<string>()));

            // Act
            await ((RelayCommand)_viewModel.MeasureCommand).ExecuteAsync(location);

            // Assert
            _mockDialogService.Verify(d => d.ShowMessage(It.Is<string>(msg => msg.Contains("out of expected range")), It.IsAny<string>()), Times.Once);
            }

        [Test]
        public async Task MeasurementValidation_RedColor_InRange_ReturnsTrue()
            {
            // Arrange
            _viewModel.ExecuteApplySerialNumber("SN12345");
            _viewModel.IsDeviceConnected = true;
            _viewModel.IsDeviceCalibrated = true;
            var location = MeasurementLocation.RedColor.ToString();
            var measurement = new Measurement
                {
                Location = location,
                x = 0.67, // In range (0.67 ± 0.1)
                y = 0.33, // In range (0.33 ± 0.1)
                Lv = 100,
                T = 6500,
                IsValid = true
                };

            _mockColorMeasurementService.Setup(s => s.MeasureAsync(It.IsAny<int>())).ReturnsAsync(measurement);

            // Act
            await ((RelayCommand)_viewModel.MeasureCommand).ExecuteAsync(location);

            // Assert - should not show error dialog
            _mockDialogService.Verify(d => d.ShowMessage(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            Assert.That(_viewModel.CurrentDevice!.Measurements.Count, Is.EqualTo(1));
            }

        [Test]
        public async Task MeasurementValidation_GreenColor_InRange_ReturnsTrue()
            {
            // Arrange
            _viewModel.ExecuteApplySerialNumber("SN12345");
            _viewModel.IsDeviceConnected = true;
            _viewModel.IsDeviceCalibrated = true;
            var location = MeasurementLocation.GreenColor.ToString();
            var measurement = new Measurement
                {
                Location = location,
                x = 0.21, // In range (0.21 ± 0.1)
                y = 0.71, // In range (0.71 ± 0.1)
                Lv = 100,
                T = 6500,
                IsValid = true
                };

            _mockColorMeasurementService.Setup(s => s.MeasureAsync(It.IsAny<int>())).ReturnsAsync(measurement);

            // Act
            await ((RelayCommand)_viewModel.MeasureCommand).ExecuteAsync(location);

            // Assert
            _mockDialogService.Verify(d => d.ShowMessage(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            Assert.That(_viewModel.CurrentDevice!.Measurements.Count, Is.EqualTo(1));
            }

        [Test]
        public async Task MeasurementValidation_BlueColor_InRange_ReturnsTrue()
            {
            // Arrange
            _viewModel.ExecuteApplySerialNumber("SN12345");
            _viewModel.IsDeviceConnected = true;
            _viewModel.IsDeviceCalibrated = true;
            var location = MeasurementLocation.BlueColor.ToString();
            var measurement = new Measurement
                {
                Location = location,
                x = 0.14, // In range (0.14 ± 0.1)
                y = 0.08, // In range (0.08 ± 0.1)
                Lv = 100,
                T = 6500,
                IsValid = true
                };

            _mockColorMeasurementService.Setup(s => s.MeasureAsync(It.IsAny<int>())).ReturnsAsync(measurement);

            // Act
            await ((RelayCommand)_viewModel.MeasureCommand).ExecuteAsync(location);

            // Assert
            _mockDialogService.Verify(d => d.ShowMessage(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            Assert.That(_viewModel.CurrentDevice!.Measurements.Count, Is.EqualTo(1));
            }

        [Test]
        public async Task MeasurementValidation_WhiteColor_InRange_ReturnsTrue()
            {
            // Arrange
            _viewModel.ExecuteApplySerialNumber("SN12345");
            _viewModel.IsDeviceConnected = true;
            _viewModel.IsDeviceCalibrated = true;
            var location = MeasurementLocation.WhiteColor.ToString();
            var measurement = new Measurement
                {
                Location = location,
                x = 0.3127, // In range (0.3127 ± 0.1)
                y = 0.3290, // In range (0.3290 ± 0.1)
                Lv = 100,
                T = 6500,
                IsValid = true
                };

            _mockColorMeasurementService.Setup(s => s.MeasureAsync(It.IsAny<int>())).ReturnsAsync(measurement);

            // Act
            await ((RelayCommand)_viewModel.MeasureCommand).ExecuteAsync(location);

            // Assert
            _mockDialogService.Verify(d => d.ShowMessage(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            Assert.That(_viewModel.CurrentDevice!.Measurements.Count, Is.EqualTo(1));
            }

        // --- Additional ExecuteMeasureAsync Tests ---

        [Test]
        public async Task ExecuteMeasureAsync_InvalidMeasurement_ShowsRetryDialog()
            {
            // Arrange
            _viewModel.ExecuteApplySerialNumber("SN12345");
            _viewModel.IsDeviceConnected = true;
            _viewModel.IsDeviceCalibrated = true;
            var location = MeasurementLocation.RedColor.ToString();
            var invalidMeasurement = new Measurement
                {
                Location = location,
                x = 0.5, // Out of range
                y = 0.5, // Out of range
                Lv = 100,
                T = 6500,
                IsValid = true
                };

            _mockColorMeasurementService.Setup(s => s.MeasureAsync(It.IsAny<int>())).ReturnsAsync(invalidMeasurement);
            _mockDialogService.Setup(d => d.ShowMessage(It.IsAny<string>(), It.IsAny<string>()));

            // Act
            await ((RelayCommand)_viewModel.MeasureCommand).ExecuteAsync(location);

            // Assert - first attempt should show retry dialog
            _mockDialogService.Verify(d => d.ShowMessage(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            }

        [Test]
        public async Task ExecuteMeasureAsync_SecondFailure_ShowsConfirmationDialog()
            {
            // Arrange
            _viewModel.ExecuteApplySerialNumber("SN12345");
            _viewModel.IsDeviceConnected = true;
            _viewModel.IsDeviceCalibrated = true;
            var location = MeasurementLocation.RedColor.ToString();
            var invalidMeasurement = new Measurement
                {
                Location = location,
                x = 0.5, // Out of range
                y = 0.5, // Out of range
                Lv = 100,
                T = 6500,
                IsValid = true
                };

            _mockColorMeasurementService.Setup(s => s.MeasureAsync(It.IsAny<int>())).ReturnsAsync(invalidMeasurement);
            _mockDialogService.Setup(d => d.ShowMessage(It.IsAny<string>(), It.IsAny<string>()));
            _mockDialogService.Setup(d => d.ShowQuestion(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

            // Act - first attempt
            await ((RelayCommand)_viewModel.MeasureCommand).ExecuteAsync(location);
            // Second attempt
            await ((RelayCommand)_viewModel.MeasureCommand).ExecuteAsync(location);

            // Assert - second attempt should show confirmation dialog
            _mockDialogService.Verify(d => d.ShowQuestion(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            }

        [Test]
        public async Task ExecuteMeasureAsync_UserAcceptsInvalidResult_SavesMeasurement()
            {
            // Arrange
            _viewModel.ExecuteApplySerialNumber("SN12345");
            _viewModel.IsDeviceConnected = true;
            _viewModel.IsDeviceCalibrated = true;
            var location = MeasurementLocation.RedColor.ToString();
            var invalidMeasurement = new Measurement
                {
                Location = location,
                x = 0.5, // Out of range
                y = 0.5, // Out of range
                Lv = 100,
                T = 6500,
                IsValid = true
                };

            _mockColorMeasurementService.Setup(s => s.MeasureAsync(It.IsAny<int>())).ReturnsAsync(invalidMeasurement);
            _mockDialogService.Setup(d => d.ShowMessage(It.IsAny<string>(), It.IsAny<string>()));
            _mockDialogService.Setup(d => d.ShowQuestion(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

            // Act - first attempt (retry)
            await ((RelayCommand)_viewModel.MeasureCommand).ExecuteAsync(location);
            // Second attempt (user accepts)
            await ((RelayCommand)_viewModel.MeasureCommand).ExecuteAsync(location);

            // Assert - measurement should be saved
            Assert.That(_viewModel.CurrentDevice!.Measurements.Count, Is.EqualTo(1));
            Assert.That(_viewModel.CurrentDevice.Measurements[0].Location, Is.EqualTo(location));
            }

        [Test]
        public async Task ExecuteMeasureAsync_UserRejectsInvalidResult_DoesNotSave()
            {
            // Arrange
            _viewModel.ExecuteApplySerialNumber("SN12345");
            _viewModel.IsDeviceConnected = true;
            _viewModel.IsDeviceCalibrated = true;
            var location = MeasurementLocation.RedColor.ToString();
            var invalidMeasurement = new Measurement
                {
                Location = location,
                x = 0.5, // Out of range
                y = 0.5, // Out of range
                Lv = 100,
                T = 6500,
                IsValid = true
                };

            _mockColorMeasurementService.Setup(s => s.MeasureAsync(It.IsAny<int>())).ReturnsAsync(invalidMeasurement);
            _mockDialogService.Setup(d => d.ShowMessage(It.IsAny<string>(), It.IsAny<string>()));
            _mockDialogService.Setup(d => d.ShowQuestion(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

            // Act - first attempt (retry)
            await ((RelayCommand)_viewModel.MeasureCommand).ExecuteAsync(location);
            // Second attempt (user rejects)
            await ((RelayCommand)_viewModel.MeasureCommand).ExecuteAsync(location);

            // Assert - measurement should NOT be saved
            Assert.That(_viewModel.CurrentDevice!.Measurements.Count, Is.EqualTo(0));
            }

        [Test]
        public async Task ExecuteMeasureAsync_CreatesNewDevice_WhenCurrentDeviceIsNull()
            {
            // Arrange
            _viewModel.CurrentDevice = null;
            _viewModel.SerialNumber = "SN12345";
            _viewModel.IsSerialNumberConfirmed = true;
            _viewModel.IsDeviceConnected = true;
            _viewModel.IsDeviceCalibrated = true;
            var location = MeasurementLocation.WhiteColor.ToString();
            var measurement = new Measurement
                {
                Location = location,
                x = 0.3127,
                y = 0.3290,
                Lv = 100,
                T = 6500,
                IsValid = true
                };

            _mockColorMeasurementService.Setup(s => s.MeasureAsync(It.IsAny<int>())).ReturnsAsync(measurement);

            // Act
            await ((RelayCommand)_viewModel.MeasureCommand).ExecuteAsync(location);

            // Assert
            Assert.That(_viewModel.CurrentDevice, Is.Not.Null);
            Assert.That(_viewModel.CurrentDevice.SerialNumber, Is.EqualTo("SN12345"));
            }

        // --- Тесты для иерархического дерева конфигураций и каскадного меню ---

        private string _configTestDirectory;

        [TearDown]
        public void TearDownConfigTestDirectory()
            {
            if (_configTestDirectory != null && Directory.Exists(_configTestDirectory))
                Directory.Delete(_configTestDirectory, recursive: true);
            _configTestDirectory = null;
            }

        /// <summary>
        /// Creates a fresh temp config directory and a MeasurementViewModel wired to it, so
        /// InitializeDeviceConfigurations (called from the constructor) scans real files/folders.
        /// </summary>
        private MeasurementViewModel CreateViewModelWithConfigDirectory(Action<string> populateDirectory)
            {
            _configTestDirectory = Path.Combine(Path.GetTempPath(), "WpfLcdTestConfigs_" + Guid.NewGuid());
            Directory.CreateDirectory(_configTestDirectory);
            populateDirectory(_configTestDirectory);

            _mockPathProvider.Setup(p => p.ConfigDirectory).Returns(_configTestDirectory);

            var viewModel = new MeasurementViewModel(
                _mockColorMeasurementService.Object,
                _mockFileService.Object,
                _mockDialogService.Object,
                _mockLocalizationService.Object,
                _mockDispatcher.Object,
                _mockUploadService.Object,
                _mockSettingsService.Object,
                _mockPathProvider.Object,
                _mockMeasurementStatusService.Object);
            viewModel.ClearAllAttemptCounts();
            return viewModel;
            }

        [Test]
        public void InitializeDeviceConfigurations_NoConfigSelected_ByDefault()
            {
            // Arrange & Act
            using var viewModel = CreateViewModelWithConfigDirectory(dir =>
                File.WriteAllText(Path.Combine(dir, "Config1.yaml"), string.Empty));

            // Assert - the operator must explicitly pick a configuration, nothing is auto-selected
            Assert.That(viewModel.SelectedDeviceConfiguration, Is.Null.Or.Empty);
            Assert.That(viewModel.HasSelectedDeviceConfiguration, Is.False);
            Assert.That(viewModel.SelectedDeviceConfigurationDisplayName, Is.EqualTo(ChooseConfiguration));
            }

        [Test]
        public void InitializeDeviceConfigurations_EmptyDirectory_LogsConfigDirNotFound()
            {
            // Arrange & Act
            using var viewModel = CreateViewModelWithConfigDirectory(dir => { });

            // Assert
            Assert.That(viewModel.LogText, Does.Contain(nameof(ConfigDirNotFound)));
            }

        [Test]
        public void InitializeDeviceConfigurations_DirectoryHasConfigs_DoesNotLogConfigDirNotFound()
            {
            // Arrange & Act
            using var viewModel = CreateViewModelWithConfigDirectory(dir =>
                File.WriteAllText(Path.Combine(dir, "Config1.yaml"), string.Empty));

            // Assert
            Assert.That(viewModel.LogText, Does.Not.Contain(nameof(ConfigDirNotFound)));
            }

        [Test]
        public void InitializeDeviceConfigurations_BuildsFlatFileList_OrderedAlphabetically()
            {
            // Arrange & Act
            using var viewModel = CreateViewModelWithConfigDirectory(dir =>
                {
                File.WriteAllText(Path.Combine(dir, "Zebra.yaml"), string.Empty);
                File.WriteAllText(Path.Combine(dir, "Alpha.yaml"), string.Empty);
                });

            // Assert
            Assert.That(viewModel.DeviceConfigurationTree, Has.Count.EqualTo(2));
            Assert.That(viewModel.DeviceConfigurationTree[0].DisplayName, Is.EqualTo("Alpha"));
            Assert.That(viewModel.DeviceConfigurationTree[1].DisplayName, Is.EqualTo("Zebra"));
            Assert.That(viewModel.DeviceConfigurationTree[0].IsFolder, Is.False);
            }

        [Test]
        public void InitializeDeviceConfigurations_NestedFolders_SubfoldersOrderedBeforeFiles()
            {
            // Arrange & Act
            using var viewModel = CreateViewModelWithConfigDirectory(dir =>
                {
                File.WriteAllText(Path.Combine(dir, "RootConfig.yaml"), string.Empty);
                var orderFolder = Directory.CreateDirectory(Path.Combine(dir, "OrderA")).FullName;
                File.WriteAllText(Path.Combine(orderFolder, "Nested.yaml"), string.Empty);
                });

            // Assert - folder node comes first, then the root-level file
            Assert.That(viewModel.DeviceConfigurationTree, Has.Count.EqualTo(2));
            Assert.That(viewModel.DeviceConfigurationTree[0].IsFolder, Is.True);
            Assert.That(viewModel.DeviceConfigurationTree[0].DisplayName, Is.EqualTo("OrderA"));
            Assert.That(viewModel.DeviceConfigurationTree[1].DisplayName, Is.EqualTo("RootConfig"));

            var nestedNode = viewModel.DeviceConfigurationTree[0].Children.Single();
            Assert.That(nestedNode.DisplayName, Is.EqualTo("Nested"));
            Assert.That(nestedNode.RelativePath, Is.EqualTo(Path.Combine("OrderA", "Nested")));
            }

        [Test]
        public void InitializeDeviceConfigurations_EmptySubfolder_IsExcludedFromTree()
            {
            // Arrange & Act
            using var viewModel = CreateViewModelWithConfigDirectory(dir =>
                Directory.CreateDirectory(Path.Combine(dir, "EmptyOrder")));

            // Assert - a folder with no configuration files (direct or nested) is not shown
            Assert.That(viewModel.DeviceConfigurationTree, Is.Empty);
            }

        [Test]
        public void SelectDeviceConfigurationCommand_CanExecute_TrueForNonEmptyRelativePath()
            {
            using var viewModel = CreateViewModelWithConfigDirectory(dir =>
                File.WriteAllText(Path.Combine(dir, "Config1.yaml"), string.Empty));

            Assert.That(viewModel.SelectDeviceConfigurationCommand.CanExecute("Config1"), Is.True);
            }

        [Test]
        public void SelectDeviceConfigurationCommand_CanExecute_FalseForNullOrEmptyParameter()
            {
            using var viewModel = CreateViewModelWithConfigDirectory(dir =>
                File.WriteAllText(Path.Combine(dir, "Config1.yaml"), string.Empty));

            Assert.That(viewModel.SelectDeviceConfigurationCommand.CanExecute(null), Is.False);
            Assert.That(viewModel.SelectDeviceConfigurationCommand.CanExecute(string.Empty), Is.False);
            }

        [Test]
        public void SelectDeviceConfigurationCommand_Execute_SetsSelectedDeviceConfiguration()
            {
            // Arrange
            using var viewModel = CreateViewModelWithConfigDirectory(dir =>
                File.WriteAllText(Path.Combine(dir, "Config1.yaml"), string.Empty));

            // Act
            viewModel.SelectDeviceConfigurationCommand.Execute("Config1");

            // Assert
            Assert.That(viewModel.SelectedDeviceConfiguration, Is.EqualTo("Config1"));
            Assert.That(viewModel.HasSelectedDeviceConfiguration, Is.True);
            Assert.That(viewModel.SelectedDeviceConfigurationDisplayName, Is.EqualTo("Config1"));
            }

        [Test]
        public void SelectDeviceConfigurationCommand_Execute_NestedConfig_DisplayNameShowsFileNameOnly()
            {
            // Arrange
            using var viewModel = CreateViewModelWithConfigDirectory(dir =>
                {
                var orderFolder = Directory.CreateDirectory(Path.Combine(dir, "OrderA")).FullName;
                File.WriteAllText(Path.Combine(orderFolder, "Nested.yaml"), string.Empty);
                });
            var relativePath = Path.Combine("OrderA", "Nested");

            // Act
            viewModel.SelectDeviceConfigurationCommand.Execute(relativePath);

            // Assert - button shows only the file name, not the folder path
            Assert.That(viewModel.SelectedDeviceConfigurationDisplayName, Is.EqualTo("Nested"));
            }

        [Test]
        public void SelectDeviceConfigurationCommand_Execute_UpdatesCheckmarkHighlight_AndMovesItOnReselection()
            {
            // Arrange
            using var viewModel = CreateViewModelWithConfigDirectory(dir =>
                {
                File.WriteAllText(Path.Combine(dir, "Config1.yaml"), string.Empty);
                File.WriteAllText(Path.Combine(dir, "Config2.yaml"), string.Empty);
                });
            var config1Node = viewModel.DeviceConfigurationTree.Single(n => n.DisplayName == "Config1");
            var config2Node = viewModel.DeviceConfigurationTree.Single(n => n.DisplayName == "Config2");

            // Act - select the first configuration
            viewModel.SelectDeviceConfigurationCommand.Execute("Config1");

            // Assert
            Assert.That(config1Node.IsSelected, Is.True);
            Assert.That(config2Node.IsSelected, Is.False);

            // Act - switch to the second configuration
            viewModel.SelectDeviceConfigurationCommand.Execute("Config2");

            // Assert - checkmark moves, previous selection is cleared
            Assert.That(config1Node.IsSelected, Is.False);
            Assert.That(config2Node.IsSelected, Is.True);
            }

        [Test]
        public void SelectDeviceConfigurationCommand_Execute_PropertyChanged_ForDisplayNameAndHasSelected()
            {
            // Arrange
            using var viewModel = CreateViewModelWithConfigDirectory(dir =>
                File.WriteAllText(Path.Combine(dir, "Config1.yaml"), string.Empty));
            var propertiesChanged = new List<string>();
            viewModel.PropertyChanged += (s, e) => propertiesChanged.Add(e.PropertyName!);

            // Act
            viewModel.SelectDeviceConfigurationCommand.Execute("Config1");

            // Assert
            Assert.That(propertiesChanged, Does.Contain(nameof(viewModel.SelectedDeviceConfigurationDisplayName)));
            Assert.That(propertiesChanged, Does.Contain(nameof(viewModel.HasSelectedDeviceConfiguration)));
            }
        }
    }