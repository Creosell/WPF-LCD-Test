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
        private Mock<ISpecEvaluationService> _mockSpecEvaluationService;
        private Mock<IAnalyzerRegistryService> _mockAnalyzerRegistryService;
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
            _mockDispatcher.Setup(d => d.CheckAccess()).Returns(true);
            _mockSpecEvaluationService = new Mock<ISpecEvaluationService>();
            _mockAnalyzerRegistryService = new Mock<IAnalyzerRegistryService>();

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
                _mockMeasurementStatusService.Object,
                _mockAnalyzerRegistryService.Object,
                _mockSpecEvaluationService.Object
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
        public void ReportGenerateCommand_StaysActive_WhenGeneratingSoThatItCanBeCanceled()
            {
            _viewModel.IsReportGenerating = true;
            var canExecute = _viewModel.ReportGenerateCommand.CanExecute(null);
            Assert.That(canExecute, Is.True);
            }

        [Test]
        public async Task UploadReports_SecondClickWhileUploading_CancelsTheUpload()
            {
            _viewModel.SelectedDeviceConfiguration = "TestConfig";
            var release = new TaskCompletionSource<bool>();
            _mockUploadService.Setup(u => u.UploadReportsAsync()).Returns(release.Task);
            var command = (RelayCommand)_viewModel.UploadReportsCommand;

            var running = command.ExecuteAsync(null);
            Assert.That(_viewModel.IsUploadingReports, Is.True);
            await command.ExecuteAsync(null);

            _mockUploadService.Verify(u => u.CancelUpload(), Times.Once);
            _mockUploadService.Verify(u => u.UploadReportsAsync(), Times.Once);
            release.SetResult(true);
            await running;
            }

        [Test]
        public async Task UploadReports_Canceled_SignalsCanceledAndFreesTheButton()
            {
            _viewModel.SelectedDeviceConfiguration = "TestConfig";
            _mockUploadService.Setup(u => u.UploadReportsAsync()).ThrowsAsync(new OperationCanceledException());

            await ((RelayCommand)_viewModel.UploadReportsCommand).ExecuteAsync(null);

            Assert.That(_viewModel.UploadFeedback.Feedback, Is.EqualTo(ActionFeedback.Canceled));
            Assert.That(_viewModel.IsUploadingReports, Is.False);
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

        private void SetupCalibratedProbe(string probeSN, int deviceChannel)
            {
            _mockColorMeasurementService.SetupGet(s => s.IsDeviceConnected).Returns(true);
            _mockColorMeasurementService.Setup(s => s.CalibrateZeroAsync()).ReturnsAsync(true);
            _mockColorMeasurementService.SetupGet(s => s.ProbeSN).Returns(probeSN);
            _mockColorMeasurementService.SetupGet(s => s.CurrentChannel).Returns(deviceChannel);
            _mockColorMeasurementService.SetupGet(s => s.Model).Returns(ColorAnalyzerModel.CA410);
            }

        [Test]
        public async Task ExecuteZeroCalibrationAsync_UnknownProbe_RegistersChannelWithoutDialog()
            {
            // Arrange
            SetupCalibratedProbe("81026182", 2);
            _mockAnalyzerRegistryService.Setup(r => r.GetLastChannel("81026182")).Returns((int?)null);

            // Act
            await ((RelayCommand)_viewModel.ZeroCalibrationCommand).ExecuteAsync(null);

            // Assert
            _mockDialogService.Verify(d => d.ShowChoice(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _mockAnalyzerRegistryService.Verify(r => r.Update("81026182", ColorAnalyzerModel.CA410, 2), Times.Once);
            _mockSettingsService.Verify(s => s.UpdateChannel(2), Times.Once);
            Assert.That(_viewModel.IsChannelConfirmed, Is.True);
            StringAssert.Contains("81026182", _viewModel.DeviceProbeSerialText);
            Assert.That(_viewModel.DeviceModelName, Is.EqualTo("CA-410"));
            }

        [Test]
        public void DeviceStateText_FollowsConnectionAndCalibration()
            {
            Assert.That(_viewModel.DeviceStateText, Is.EqualTo(DeviceStateNotConnected));

            _viewModel.IsDeviceConnected = true;
            Assert.That(_viewModel.DeviceStateText, Is.EqualTo(DeviceStateConnectedNotCalibrated));

            _viewModel.IsDeviceCalibrated = true;
            Assert.That(_viewModel.DeviceStateText, Is.EqualTo(DeviceStateConnectedCalibrated));

            _viewModel.IsDeviceConnected = false;
            _viewModel.IsDeviceCalibrated = false;
            Assert.That(_viewModel.DeviceStateText, Is.EqualTo(DeviceStateNotConnected));
            }

        [Test]
        public async Task DeviceInfoLabels_WhileRecalibrating_KeepLastConfirmedValues()
            {
            // Arrange
            SetupCalibratedProbe("81026182", 2);
            _mockAnalyzerRegistryService.Setup(r => r.GetLastChannel("81026182")).Returns((int?)null);
            await ((RelayCommand)_viewModel.ZeroCalibrationCommand).ExecuteAsync(null);

            // Act: a new calibration starts, the channel has to be confirmed again
            _viewModel.IsDeviceCalibrated = true;
            _viewModel.IsDeviceCalibrated = false;

            // Assert
            Assert.That(_viewModel.IsChannelConfirmed, Is.False);
            Assert.That(_viewModel.DeviceModelName, Is.EqualTo("CA-410"));
            StringAssert.Contains("2", _viewModel.DeviceActiveChannelStatusText);
            StringAssert.Contains("81026182", _viewModel.DeviceProbeSerialText);
            }

        [Test]
        public void DeviceInfoLabels_BeforeFirstConfirmation_AreEmpty()
            {
            Assert.That(_viewModel.DeviceProbeSerialText, Is.Empty);
            Assert.That(_viewModel.DeviceActiveChannelStatusText, Is.Empty);
            Assert.That(_viewModel.DeviceModelName, Is.Empty);
            }

        [Test]
        public async Task ExecuteZeroCalibrationAsync_ConfirmedChannel_FillsColorimeterOfDevices()
            {
            // Arrange
            SetupCalibratedProbe("81026182", 2);
            _mockAnalyzerRegistryService.Setup(r => r.GetLastChannel("81026182")).Returns((int?)null);
            _viewModel.ExecuteApplySerialNumber("SN12345");

            // Act
            await ((RelayCommand)_viewModel.ZeroCalibrationCommand).ExecuteAsync(null);
            var expected = new ColorimeterInfo("CA-410", "81026182", 2);

            // Assert
            Assert.That(_viewModel.CurrentDevice!.Colorimeter, Is.EqualTo(expected), "Device created before calibration receives the colorimeter.");

            _viewModel.CurrentDevice = null;
            _viewModel.ExecuteApplySerialNumber("SN67890");
            Assert.That(_viewModel.CurrentDevice!.Colorimeter, Is.EqualTo(expected), "Device created after calibration receives the colorimeter.");
            }

        [Test]
        public async Task ExecuteZeroCalibrationAsync_SameChannelAsRegistry_DoesNotShowDialog()
            {
            // Arrange
            SetupCalibratedProbe("81026182", 2);
            _mockAnalyzerRegistryService.Setup(r => r.GetLastChannel("81026182")).Returns(2);

            // Act
            await ((RelayCommand)_viewModel.ZeroCalibrationCommand).ExecuteAsync(null);

            // Assert
            _mockDialogService.Verify(d => d.ShowChoice(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _mockAnalyzerRegistryService.Verify(r => r.Update("81026182", ColorAnalyzerModel.CA410, 2), Times.Once);
            }

        [Test]
        public async Task ExecuteZeroCalibrationAsync_ChannelMismatch_ProbeChoiceKeepsDeviceChannel()
            {
            // Arrange
            SetupCalibratedProbe("81026182", 2);
            _mockAnalyzerRegistryService.Setup(r => r.GetLastChannel("81026182")).Returns(5);
            _mockDialogService
                .Setup(d => d.ShowChoice(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(0);

            // Act
            await ((RelayCommand)_viewModel.ZeroCalibrationCommand).ExecuteAsync(null);

            // Assert
            _mockColorMeasurementService.VerifySet(s => s.CurrentChannel = It.IsAny<int>(), Times.Never);
            _mockAnalyzerRegistryService.Verify(r => r.Update("81026182", ColorAnalyzerModel.CA410, 2), Times.Once);
            Assert.That(_viewModel.IsChannelConfirmed, Is.True);
            }

        [Test]
        public async Task ExecuteZeroCalibrationAsync_ChannelMismatch_LastChoiceWritesRegistryChannelToDevice()
            {
            // Arrange
            SetupCalibratedProbe("81026182", 2);
            _mockAnalyzerRegistryService.Setup(r => r.GetLastChannel("81026182")).Returns(5);
            _mockDialogService
                .Setup(d => d.ShowChoice(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(1);

            // Act
            await ((RelayCommand)_viewModel.ZeroCalibrationCommand).ExecuteAsync(null);

            // Assert
            _mockColorMeasurementService.VerifySet(s => s.CurrentChannel = 5, Times.Once);
            _mockAnalyzerRegistryService.Verify(r => r.Update("81026182", ColorAnalyzerModel.CA410, 5), Times.Once);
            _mockSettingsService.Verify(s => s.UpdateChannel(5), Times.Once);
            }

        [Test]
        public async Task ExecuteZeroCalibrationAsync_ChannelMismatch_DialogDismissed_CancelsCalibration()
            {
            // Arrange
            SetupCalibratedProbe("81026182", 2);
            _mockAnalyzerRegistryService.Setup(r => r.GetLastChannel("81026182")).Returns(5);
            _mockDialogService
                .Setup(d => d.ShowChoice(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns((int?)null);

            // Act
            await ((RelayCommand)_viewModel.ZeroCalibrationCommand).ExecuteAsync(null);

            // Assert
            _mockAnalyzerRegistryService.Verify(r => r.Update(It.IsAny<string>(), It.IsAny<ColorAnalyzerModel>(), It.IsAny<int>()), Times.Never);
            _mockSettingsService.Verify(s => s.UpdateChannel(It.IsAny<int>()), Times.Never);
            _mockColorMeasurementService.Verify(s => s.Disconnect(), Times.Once);
            Assert.That(_viewModel.IsChannelConfirmed, Is.False);
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
            _viewModel.SelectedDeviceConfiguration = "TestConfig";
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
            _viewModel.SelectedDeviceConfiguration = "TestConfig";
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
            _viewModel.SelectedDeviceConfiguration = "TestConfig";
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
        public async Task ExecuteMeasureAsync_AcceptedMeasurement_FillsTheResultsRowAndKeepsThePointResultOutOfTheLog()
            {
            _viewModel.ExecuteApplySerialNumber("SN12345");
            _viewModel.IsDeviceConnected = true;
            _viewModel.IsDeviceCalibrated = true;
            var location = MeasurementLocation.WhiteColor.ToString();
            _mockColorMeasurementService.Setup(s => s.MeasureAsync(It.IsAny<int>()))
                .ReturnsAsync(new Measurement { Location = location, x = 0.3127, y = 0.3290, Lv = 100, T = 6500, IsValid = true });

            await ((RelayCommand)_viewModel.MeasureCommand).ExecuteAsync(location);

            var row = _viewModel.Results.Single(r => r.Location == location);
            Assert.That((row.X, row.Y, row.Lv, row.T), Is.EqualTo(("0.313", "0.329", "100.0", "6500")));
            Assert.That(_viewModel.LogText, Does.Not.Contain("x=0.313"));
            }

        [Test]
        public async Task ExecuteMeasureAsync_FailedRetry_KeepsTheResultsRowAndLogsAWarning()
            {
            _viewModel.ExecuteApplySerialNumber("SN12345");
            _viewModel.IsDeviceConnected = true;
            _viewModel.IsDeviceCalibrated = true;
            var location = MeasurementLocation.WhiteColor.ToString();
            var good = new Measurement { Location = location, x = 0.3127, y = 0.3290, Lv = 100, T = 6500, IsValid = true };
            var tooDark = new Measurement { Location = location, x = 0.3127, y = 0.3290, Lv = 1, T = 6500, IsValid = true };
            _mockColorMeasurementService.SetupSequence(s => s.MeasureAsync(It.IsAny<int>())).ReturnsAsync(good).ReturnsAsync(tooDark);

            await ((RelayCommand)_viewModel.MeasureCommand).ExecuteAsync(location);
            await ((RelayCommand)_viewModel.MeasureCommand).ExecuteAsync(location);

            var row = _viewModel.Results.Single(r => r.Location == location);
            Assert.That(row.Lv, Is.EqualTo("100.0"), "the previous accepted attempt stays");
            Assert.That(row.Status.IsPassed, Is.False, "only the status of the last measurement is red");
            Assert.That(_viewModel.LogEntries.Last().Severity, Is.EqualTo(LogSeverity.Warning));
            }

        [Test]
        public void UnreadIssues_CountWarningsWhileTheLogTabIsHiddenAndResetWhenItOpens()
            {
            _mockDispatcher.Setup(d => d.CheckAccess()).Returns(true);
            _viewModel.SelectedTabIndex = 1;
            _viewModel.SelectedTabIndex = 0;
            Assert.That(_viewModel.UnreadIssues, Is.EqualTo(0));

            _mockColorMeasurementService.Raise(s => s.StatusMessage += null, _mockColorMeasurementService.Object, "info line");
            Assert.That(_viewModel.UnreadIssues, Is.EqualTo(0), "informational lines are not counted");

            _viewModel.Spec.Show(new SpecEvaluation(SpecVerdict.Warn, false,
                [new SpecCheck("Brightness", 90, 100, 120, null, SpecStatus.Warning, ["WhiteColor"], [])]), 1, 14);
            Assert.That(_viewModel.UnreadIssues, Is.EqualTo(1));

            _viewModel.SelectedTabIndex = 1;
            Assert.That(_viewModel.UnreadIssues, Is.EqualTo(0));

            _viewModel.Spec.Show(new SpecEvaluation(SpecVerdict.Fail, false,
                [new SpecCheck("Brightness", 50, 100, 120, null, SpecStatus.Fail, ["WhiteColor"], [])]), 1, 14);
            Assert.That(_viewModel.UnreadIssues, Is.EqualTo(0), "nothing is unread while the log is open");
            }

        [Test]
        public void Log_FromAnotherThread_TouchesTheBoundCollectionOnlyThroughTheDispatcher()
            {
            _mockDispatcher.Setup(d => d.CheckAccess()).Returns(false);
            var queued = new List<Action>();
            _mockDispatcher.Setup(d => d.BeginInvoke(It.IsAny<Action>())).Callback<Action>(queued.Add);
            int before = _viewModel.LogEntries.Count;

            _viewModel.ReportGenerateCommand.Execute(null);

            Assert.That(_viewModel.LogEntries.Count, Is.EqualTo(before), "the bound collection must not change on a foreign thread");
            Assert.That(_viewModel.LogText, Does.Contain("ReportGenerator.exe"), "the text of the log is updated at once");

            queued.ForEach(action => action());
            Assert.That(_viewModel.LogEntries.Count, Is.GreaterThan(before));
            }

        [Test]
        public async Task Measure_AcceptedMeasurement_SignalsSuccessOnTheButtonOfThePoint()
            {
            await PrepareDeviceWithOneMeasurementAsync();

            var status = _mockMeasurementStatusService.Object.AllMeasurementButtonStatuses
                .First(s => s.Location == MeasurementLocation.WhiteColor.ToString());
            Assert.That(status.Feedback.Feedback, Is.EqualTo(ActionFeedback.Success));
            }

        [Test]
        public async Task SaveCommands_AreDisabledWithoutConfiguration()
            {
            await PrepareDeviceWithOneMeasurementAsync();
            Assert.That(_viewModel.NewDeviceUnderTestCommand.CanExecute(null), Is.True);

            _viewModel.SelectedDeviceConfiguration = string.Empty;

            Assert.That(_viewModel.NewDeviceUnderTestCommand.CanExecute(null), Is.False);
            Assert.That(_viewModel.SaveResultsCommand.CanExecute(null), Is.False);
            }

        [Test]
        public async Task ConfirmClose_AsksOnlyWhileMeasurementsAreUnsaved()
            {
            Assert.That(_viewModel.ConfirmClose(), Is.True, "nothing measured, no question");
            _mockDialogService.Verify(d => d.ShowQuestion(It.IsAny<string>(), It.IsAny<string>()), Times.Never);

            await PrepareDeviceWithOneMeasurementAsync();
            _mockDialogService.Setup(d => d.ShowQuestion(It.IsAny<string>(), It.IsAny<string>())).Returns(false);
            Assert.That(_viewModel.ConfirmClose(), Is.False);

            _mockDialogService.Setup(d => d.ShowQuestion(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
            _mockFileService.Setup(f => f.SaveDeviceDataToJsonAsync(It.IsAny<DeviceUnderTest>())).ReturnsAsync(true);
            await ((RelayCommand)_viewModel.SaveResultsCommand).ExecuteAsync(null);
            _mockDialogService.Invocations.Clear();
            Assert.That(_viewModel.ConfirmClose(), Is.True, "saved results can be closed without a question");
            _mockDialogService.Verify(d => d.ShowQuestion(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            }

        [Test]
        public void PendingReportsSuffix_ShowsTheCountOfReportsWaitingForUpload()
            {
            _mockUploadService.Setup(u => u.GetPendingReportCount()).Returns(3);
            using var viewModel = CreateViewModelWithConfigDirectory(_ => { });

            Assert.That(viewModel.PendingReportsSuffix, Is.EqualTo(" (3)"));
            }

        [Test]
        public void UnreadSeverity_FollowsTheMostSevereUnreadEntryAndResetsWhenTheLogIsOpened()
            {
            _viewModel.SelectedTabIndex = 1;
            _viewModel.SelectedTabIndex = 0;
            Assert.That(_viewModel.UnreadSeverity, Is.EqualTo(LogSeverity.Info));

            _viewModel.ReportGenerateCommand.Execute(null);
            Assert.That(_viewModel.UnreadSeverity, Is.EqualTo(LogSeverity.Error));

            _viewModel.SelectedTabIndex = 1;
            Assert.That(_viewModel.UnreadIssues, Is.EqualTo(0));
            Assert.That(_viewModel.UnreadSeverity, Is.EqualTo(LogSeverity.Info));
            }

        [TestCase(1, "1")]
        [TestCase(99, "99")]
        [TestCase(100, "99+")]
        [TestCase(250, "99+")]
        public void UnreadIssuesText_IsCappedAt99Plus(int errors, string expected)
            {
            _viewModel.SelectedTabIndex = 1;
            _viewModel.SelectedTabIndex = 0;
            for (int i = 0; i < errors; i++) _viewModel.ReportGenerateCommand.Execute(null);

            Assert.That(_viewModel.UnreadIssuesText, Is.EqualTo(expected));
            }

        [Test]
        public void LogEntries_HoldOnlyWarningsAndErrors()
            {
            int before = _viewModel.LogEntries.Count;
            _viewModel.ExecuteApplySerialNumber("SN12345");
            Assert.That(_viewModel.LogEntries.Count, Is.EqualTo(before), "info goes to the copyable text only");
            Assert.That(_viewModel.LogText, Does.Contain("SN12345"));

            _viewModel.ReportGenerateCommand.Execute(null);

            Assert.That(_viewModel.LogEntries.Count, Is.EqualTo(before + 1));
            }

        private async Task PrepareDeviceWithOneMeasurementAsync()
            {
            _viewModel.ExecuteApplySerialNumber("SN12345");
            _viewModel.SelectedDeviceConfiguration = "TestConfig";
            _viewModel.IsDeviceConnected = true;
            _viewModel.IsDeviceCalibrated = true;
            var location = MeasurementLocation.WhiteColor.ToString();
            _mockColorMeasurementService.Setup(s => s.MeasureAsync(It.IsAny<int>()))
                .ReturnsAsync(new Measurement { Location = location, x = 0.3127, y = 0.3290, Lv = 100, T = 6500, IsValid = true });
            await ((RelayCommand)_viewModel.MeasureCommand).ExecuteAsync(location);
            }

        [Test]
        public async Task SaveResults_Saved_SignalsSuccess()
            {
            await PrepareDeviceWithOneMeasurementAsync();
            _mockDialogService.Setup(d => d.ShowQuestion(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
            _mockFileService.Setup(f => f.SaveDeviceDataToJsonAsync(It.IsAny<DeviceUnderTest>())).ReturnsAsync(true);

            await ((RelayCommand)_viewModel.SaveResultsCommand).ExecuteAsync(null);

            Assert.That(_viewModel.SaveFeedback.Feedback, Is.EqualTo(ActionFeedback.Success));
            }

        [Test]
        public async Task SaveResults_FailedToSave_SignalsFailure()
            {
            await PrepareDeviceWithOneMeasurementAsync();
            _mockDialogService.Setup(d => d.ShowQuestion(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
            _mockFileService.Setup(f => f.SaveDeviceDataToJsonAsync(It.IsAny<DeviceUnderTest>())).ReturnsAsync(false);

            await ((RelayCommand)_viewModel.SaveResultsCommand).ExecuteAsync(null);

            Assert.That(_viewModel.SaveFeedback.Feedback, Is.EqualTo(ActionFeedback.Failure));
            }

        [Test]
        public async Task SaveResults_DeclinedByTheOperator_SignalsNothing()
            {
            await PrepareDeviceWithOneMeasurementAsync();
            _mockDialogService.Setup(d => d.ShowQuestion(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

            await ((RelayCommand)_viewModel.SaveResultsCommand).ExecuteAsync(null);

            Assert.That(_viewModel.SaveFeedback.Sequence, Is.EqualTo(0));
            }

        [Test]
        public void ClearFields_Confirmed_SignalsSuccessAndDeclinedSignalsNothing()
            {
            _mockDialogService.Setup(d => d.ShowQuestion(It.IsAny<string>(), It.IsAny<string>())).Returns(false);
            _viewModel.ClearFieldsCommand.Execute(null);
            Assert.That(_viewModel.ClearFeedback.Sequence, Is.EqualTo(0));

            _mockDialogService.Setup(d => d.ShowQuestion(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
            _viewModel.ClearFieldsCommand.Execute(null);

            Assert.That(_viewModel.ClearFeedback.Feedback, Is.EqualTo(ActionFeedback.Success));
            }

        [TestCase(true, ActionFeedback.Success)]
        [TestCase(false, ActionFeedback.Failure)]
        public async Task UploadReports_SignalsTheOutcome(bool uploaded, ActionFeedback expected)
            {
            _viewModel.SelectedDeviceConfiguration = "TestConfig";
            _mockUploadService.Setup(u => u.UploadReportsAsync()).ReturnsAsync(uploaded);

            await ((RelayCommand)_viewModel.UploadReportsCommand).ExecuteAsync(null);

            Assert.That(_viewModel.UploadFeedback.Feedback, Is.EqualTo(expected));
            }

        [Test]
        public async Task UploadReports_WithoutConfiguration_SignalsFailure()
            {
            await ((RelayCommand)_viewModel.UploadReportsCommand).ExecuteAsync(null);

            Assert.That(_viewModel.UploadFeedback.Feedback, Is.EqualTo(ActionFeedback.Failure));
            }

        [Test]
        public void StatusMessageMeasuring_IsNotWrittenToTheLog()
            {
            _mockDispatcher.Setup(d => d.CheckAccess()).Returns(true);

            _mockColorMeasurementService.Raise(s => s.StatusMessage += null, _mockColorMeasurementService.Object, Resources.Resources.Measuring);

            Assert.That(_viewModel.LogEntries.Select(e => e.Message), Does.Not.Contain(Resources.Resources.Measuring));
            }

        [Test]
        public async Task ExecuteMeasureAsync_InfiniteColorTemperature_IsShownAsNotAvailable()
            {
            _viewModel.ExecuteApplySerialNumber("SN12345");
            _viewModel.IsDeviceConnected = true;
            _viewModel.IsDeviceCalibrated = true;
            var location = MeasurementLocation.WhiteColor.ToString();
            _mockColorMeasurementService.Setup(s => s.MeasureAsync(It.IsAny<int>()))
                .ReturnsAsync(new Measurement { Location = location, x = 0.3127, y = 0.3290, Lv = 100, T = double.PositiveInfinity, IsValid = true });

            await ((RelayCommand)_viewModel.MeasureCommand).ExecuteAsync(location);

            var status = _mockMeasurementStatusService.Object.AllMeasurementButtonStatuses.First(s => s.Location == location);
            Assert.That(status.MeasuredValuesString, Does.EndWith("T=n/a"));
            }

        [Test]
        public async Task ExecuteMeasureAsync_FailedRetry_KeepsThePreviousAttemptsAndOnlyTheButtonTurnsRed()
            {
            _viewModel.ExecuteApplySerialNumber("SN12345");
            _viewModel.IsDeviceConnected = true;
            _viewModel.IsDeviceCalibrated = true;
            var location = MeasurementLocation.WhiteColor.ToString();
            var good = new Measurement { Location = location, x = 0.3127, y = 0.3290, Lv = 100, T = 6500, IsValid = true };
            var tooDark = new Measurement { Location = location, x = 0.3127, y = 0.3290, Lv = 1, T = 6500, IsValid = true };
            _mockColorMeasurementService.SetupSequence(s => s.MeasureAsync(It.IsAny<int>())).ReturnsAsync(good).ReturnsAsync(tooDark);

            await ((RelayCommand)_viewModel.MeasureCommand).ExecuteAsync(location);
            await ((RelayCommand)_viewModel.MeasureCommand).ExecuteAsync(location);

            Assert.That(_viewModel.CurrentDevice.Measurements.Single().Lv, Is.EqualTo(100));
            Assert.That(_viewModel.CurrentDevice.AttemptCount(location), Is.EqualTo(1), "a rejected measurement is not an attempt");
            var status = _mockMeasurementStatusService.Object.AllMeasurementButtonStatuses.First(s => s.Location == location);
            Assert.That(status.IsPassed, Is.False);
            }

        [Test]
        public async Task ExecuteMeasureAsync_RepeatedAcceptedMeasurements_AreKeptAsAttemptsOfThePoint()
            {
            _viewModel.ExecuteApplySerialNumber("SN12345");
            _viewModel.IsDeviceConnected = true;
            _viewModel.IsDeviceCalibrated = true;
            var location = MeasurementLocation.WhiteColor.ToString();
            Measurement Attempt(double lv) => new() { Location = location, x = 0.3127, y = 0.3290, Lv = lv, T = 6500, IsValid = true };
            _mockColorMeasurementService.SetupSequence(s => s.MeasureAsync(It.IsAny<int>()))
                .ReturnsAsync(Attempt(150)).ReturnsAsync(Attempt(180)).ReturnsAsync(Attempt(170));

            for (int i = 0; i < 3; i++)
                await ((RelayCommand)_viewModel.MeasureCommand).ExecuteAsync(location);

            var device = _viewModel.CurrentDevice;
            Assert.That(device.AttemptCount(location), Is.EqualTo(3));
            Assert.That(device.Measurements.Single().Lv, Is.EqualTo(180), "until the specification check chooses, the best is the brightest");
            Assert.That(_viewModel.Results.Single(r => r.Location == location).Lv, Is.EqualTo("180.0"));
            }

        [Test]
        public async Task ExecuteMeasureAsync_TheSpecificationCheckChoosesTheBestAttemptAndFillsTheCounter()
            {
            _mockDispatcher.Setup(d => d.CheckAccess()).Returns(true);
            _mockFileService.Setup(f => f.SerializeDevice(It.IsAny<DeviceUnderTest>())).Returns("{}");
            var evaluation = new SpecEvaluation(SpecVerdict.Warn, false,
                [new SpecCheck("Brightness", 150, 160, 190, null, SpecStatus.Fail, ["WhiteColor"], [])])
                {
                Attempts = new Dictionary<string, SpecAttemptInfo> { ["WhiteColor"] = new(2, 0, true) }
                };
            _mockSpecEvaluationService.Setup(s => s.EvaluateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SpecEvaluationOutcome(evaluation, null));
            _viewModel.SelectedDeviceConfiguration = "TestConfig";
            _viewModel.ExecuteApplySerialNumber("SN12345");
            _viewModel.IsDeviceConnected = true;
            _viewModel.IsDeviceCalibrated = true;
            var location = MeasurementLocation.WhiteColor.ToString();
            Measurement Attempt(double lv) => new() { Location = location, x = 0.3127, y = 0.3290, Lv = lv, T = 6500, IsValid = true };
            _mockColorMeasurementService.SetupSequence(s => s.MeasureAsync(It.IsAny<int>())).ReturnsAsync(Attempt(150)).ReturnsAsync(Attempt(180));

            await ((RelayCommand)_viewModel.MeasureCommand).ExecuteAsync(location);
            await ((RelayCommand)_viewModel.MeasureCommand).ExecuteAsync(location);
            await Task.Delay(50);

            var row = _viewModel.Results.Single(r => r.Location == location);
            Assert.That(_viewModel.CurrentDevice.Measurements.Single().BestAttemptIndex, Is.EqualTo(0), "the check chose the first attempt");
            Assert.That(row.Lv, Is.EqualTo("150.0"));
            Assert.That((row.Tries, row.TriesLevel), Is.EqualTo(("2/3", TriesLevel.Few)));
            }

        [Test]
        public async Task ExecuteMeasureAsync_TheCounterIsGreenWhenTheAcceptedAttemptHasNoRemarks()
            {
            _mockDispatcher.Setup(d => d.CheckAccess()).Returns(true);
            _mockFileService.Setup(f => f.SerializeDevice(It.IsAny<DeviceUnderTest>())).Returns("{}");
            var evaluation = new SpecEvaluation(SpecVerdict.Pass, false,
                [new SpecCheck("Brightness", 150, 160, 190, null, SpecStatus.Pass, ["WhiteColor"], [])])
                {
                Attempts = new Dictionary<string, SpecAttemptInfo> { ["WhiteColor"] = new(2, 0, true) }
                };
            _mockSpecEvaluationService.Setup(s => s.EvaluateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SpecEvaluationOutcome(evaluation, null));
            _viewModel.SelectedDeviceConfiguration = "TestConfig";
            _viewModel.ExecuteApplySerialNumber("SN12345");
            _viewModel.IsDeviceConnected = true;
            _viewModel.IsDeviceCalibrated = true;
            var location = MeasurementLocation.WhiteColor.ToString();
            Measurement Attempt(double lv) => new() { Location = location, x = 0.3127, y = 0.3290, Lv = lv, T = 6500, IsValid = true };
            _mockColorMeasurementService.SetupSequence(s => s.MeasureAsync(It.IsAny<int>())).ReturnsAsync(Attempt(150)).ReturnsAsync(Attempt(180));

            await ((RelayCommand)_viewModel.MeasureCommand).ExecuteAsync(location);
            await ((RelayCommand)_viewModel.MeasureCommand).ExecuteAsync(location);
            await Task.Delay(50);

            var row = _viewModel.Results.Single(r => r.Location == location);
            Assert.That(_viewModel.CurrentDevice.Measurements.Single().BestAttemptIndex, Is.EqualTo(0), "the check chose the first attempt");
            Assert.That(row.Lv, Is.EqualTo("150.0"));
            Assert.That((row.Tries, row.TriesLevel), Is.EqualTo(("2/3", TriesLevel.Enough)));
            }

        [Test]
        public async Task ExecuteMeasureAsync_ACheckResultForAnOlderStateOfThePointIsNotApplied()
            {
            _mockDispatcher.Setup(d => d.CheckAccess()).Returns(true);
            _mockFileService.Setup(f => f.SerializeDevice(It.IsAny<DeviceUnderTest>())).Returns("{}");
            var stale = new SpecEvaluation(SpecVerdict.Pass, false, [])
                {
                Attempts = new Dictionary<string, SpecAttemptInfo> { ["WhiteColor"] = new(1, 0, false) }
                };
            _mockSpecEvaluationService.Setup(s => s.EvaluateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SpecEvaluationOutcome(stale, null));
            _viewModel.SelectedDeviceConfiguration = "TestConfig";
            _viewModel.ExecuteApplySerialNumber("SN12345");
            var location = MeasurementLocation.WhiteColor.ToString();
            _viewModel.CurrentDevice.RecordAttempt(new Measurement { Location = location, x = 0.3, y = 0.3, Lv = 150, T = 6500 });
            _viewModel.CurrentDevice.RecordAttempt(new Measurement { Location = location, x = 0.3, y = 0.3, Lv = 180, T = 6500 });
            _viewModel.CurrentDevice.SelectBestAttempt(location, 1);

            _viewModel.IsTvCheckboxChecked = true;
            await Task.Delay(50);

            Assert.That(_viewModel.CurrentDevice.Measurements.Single().BestAttemptIndex, Is.EqualTo(1), "an answer about one attempt must not override two");
            }

        [Test]
        public async Task SaveResults_WithRemarks_AsksWithTheProblemsAndTheAttemptsAndDeclineCancelsTheSave()
            {
            _mockDispatcher.Setup(d => d.CheckAccess()).Returns(true);
            _mockFileService.Setup(f => f.SerializeDevice(It.IsAny<DeviceUnderTest>())).Returns("{}");
            var evaluation = new SpecEvaluation(SpecVerdict.Fail, true,
                [new SpecCheck("Brightness", 268, 290, 300, null, SpecStatus.Fail, ["WhiteColor"], [])])
                {
                Attempts = new Dictionary<string, SpecAttemptInfo> { ["WhiteColor"] = new(1, 0, true) }
                };
            _mockSpecEvaluationService.Setup(s => s.EvaluateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SpecEvaluationOutcome(evaluation, null));
            string? asked = null;
            _mockDialogService.Setup(d => d.ShowQuestion(It.IsAny<string>(), It.IsAny<string>()))
                .Callback<string, string>((message, _) => asked = message).Returns(false);
            _viewModel.SelectedDeviceConfiguration = "TestConfig";
            _viewModel.ExecuteApplySerialNumber("SN12345");
            _viewModel.IsDeviceConnected = true;
            _viewModel.IsDeviceCalibrated = true;
            var location = MeasurementLocation.WhiteColor.ToString();
            _mockColorMeasurementService.Setup(s => s.MeasureAsync(It.IsAny<int>()))
                .ReturnsAsync(new Measurement { Location = location, x = 0.3127, y = 0.3290, Lv = 268, T = 6500, IsValid = true });
            await ((RelayCommand)_viewModel.MeasureCommand).ExecuteAsync(location);
            await Task.Delay(50);

            await ((RelayCommand)_viewModel.SaveResultsCommand).ExecuteAsync(null);

            Assert.That(asked, Does.Contain("White: Brightness (cd/m²) is 268, below the minimum 290 (measured once)").And.Contain("fewer than 3 times").And.Contain("Save the results anyway?"));
            _mockFileService.Verify(f => f.SaveDeviceDataToJsonAsync(It.IsAny<DeviceUnderTest>()), Times.Never);
            }

        [Test]
        public async Task ExecuteMeasureAsync_WithConfiguration_ChecksTheSpecificationAfterEveryAcceptedMeasurement()
            {
            _mockDispatcher.Setup(d => d.CheckAccess()).Returns(true);
            _mockFileService.Setup(f => f.SerializeDevice(It.IsAny<DeviceUnderTest>())).Returns("{}");
            var evaluation = new SpecEvaluation(SpecVerdict.Warn, false,
                [new SpecCheck("Brightness", 90, 100, 120, null, SpecStatus.Warning, ["WhiteColor"], [])]);
            _mockSpecEvaluationService.Setup(s => s.EvaluateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SpecEvaluationOutcome(evaluation, null));
            _viewModel.SelectedDeviceConfiguration = "TestConfig";
            _viewModel.ExecuteApplySerialNumber("SN12345");
            _viewModel.IsDeviceConnected = true;
            _viewModel.IsDeviceCalibrated = true;
            var location = MeasurementLocation.WhiteColor.ToString();
            _mockColorMeasurementService.Setup(s => s.MeasureAsync(It.IsAny<int>()))
                .ReturnsAsync(new Measurement { Location = location, x = 0.3127, y = 0.3290, Lv = 90, T = 6500, IsValid = true });

            await ((RelayCommand)_viewModel.MeasureCommand).ExecuteAsync(location);
            await Task.Delay(50);

            _mockSpecEvaluationService.Verify(s => s.EvaluateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.AtLeast(2));
            Assert.That(_viewModel.Spec.Evaluation, Is.SameAs(evaluation));
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
                _mockMeasurementStatusService.Object,
                _mockAnalyzerRegistryService.Object,
                _mockSpecEvaluationService.Object);
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