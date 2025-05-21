using Moq;
using MvvmHelpers;
using NUnit.Framework;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using WPF_LCD_Test.Interfaces;
using WPF_LCD_Test.Models;
using WPF_LCD_Test.ViewModels;

namespace WPF_LCD_Test.UnitTests.ViewModels
{
    [TestFixture]
    public class MeasurementViewModelTests
    {
        private Mock<IColorMeasurementService> _mockColorMeasurementService;
        private Mock<IFileService> _mockFileService;
        private Mock<IDialogService> _mockDialogService;
        private Mock<ILocalizationService> _mockLocalizationService;
        private MeasurementViewModel _viewModel;

        public class TestDispatcher
        {
            public bool CheckAccess() => true;
            public void Invoke(Action action) => action();
            public void BeginInvoke(Action action) => action();
        }

        [SetUp]
        public void Setup()
        {
            _mockColorMeasurementService = new Mock<IColorMeasurementService>();
            _mockFileService = new Mock<IFileService>();
            _mockDialogService = new Mock<IDialogService>();
            _mockLocalizationService = new Mock<ILocalizationService>();

            _mockColorMeasurementService.SetupGet(s => s.IsConnected).Returns(false);
            _mockColorMeasurementService.SetupGet(s => s.IsCalibrated).Returns(false);

            // Настройка LocalizationService
            _mockLocalizationService.Setup(l => l.GetString("strStartMeasurement")).Returns("Начать измерение");
            _mockLocalizationService.Setup(l => l.GetString("strStopMeasurement")).Returns("Остановить измерение");
            _mockLocalizationService.Setup(l => l.GetString("strConnecting")).Returns("Подключение...");
            _mockLocalizationService.Setup(l => l.GetString("strConnected")).Returns("Подключено");
            _mockLocalizationService.Setup(l => l.GetString("strDisconnected")).Returns("Отключено");
            _mockLocalizationService.Setup(l => l.GetString("strMeasurementInProgress")).Returns("Измерение в процессе...");
            _mockLocalizationService.Setup(l => l.GetString("strMeasurementCompleted")).Returns("Измерение завершено.");
            _mockLocalizationService.Setup(l => l.GetString("strMeasurementCancelled")).Returns("Измерение отменено.");
            _mockLocalizationService.Setup(l => l.GetString("strMeasurementFailed")).Returns("Измерение не удалось!");
            _mockLocalizationService.Setup(l => l.GetString("strError")).Returns("Ошибка");
            _mockLocalizationService.Setup(l => l.GetString("strColorDataFile")).Returns("Файл данных цвета");
            _mockLocalizationService.Setup(l => l.GetString("strAllFiles")).Returns("Все файлы");
            _mockLocalizationService.Setup(l => l.GetString("strInvalidColorDataFormat")).Returns("Неверный формат данных цвета.");
            _mockLocalizationService.Setup(l => l.GetString("strFillSN")).Returns("Введите серийный номер");
            _mockLocalizationService.Setup(l => l.GetString("strNoSNErr")).Returns("Нет SN");
            _mockLocalizationService.Setup(l => l.GetString("strTestStartInfo")).Returns("Тест начат для SN:");
            _mockLocalizationService.Setup(l => l.GetString("strResult")).Returns("Результат");
            _mockLocalizationService.Setup(l => l.GetString("strNoData")).Returns("Нет данных");
            _mockLocalizationService.Setup(l => l.GetString("strLvIsTooLow")).Returns("Значение Lv слишком низкое");
            _mockLocalizationService.Setup(l => l.GetString("strCheckProbe")).Returns("Проверьте щуп");
            _mockLocalizationService.Setup(l => l.GetString("strInvalidResultErr")).Returns("Недопустимый результат");
            _mockLocalizationService.Setup(l => l.GetString("strColorServiceErr")).Returns("Ошибка службы цвета");
            _mockLocalizationService.Setup(l => l.GetString("strColorAnalyzerErr")).Returns("Ошибка анализатора цвета");
            _mockLocalizationService.Setup(l => l.GetString("strUnexpectedMeasurementErr")).Returns("Непредвиденная ошибка измерения");
            _mockLocalizationService.Setup(l => l.GetString("strSaving")).Returns("Сохранение...");
            _mockLocalizationService.Setup(l => l.GetString("strSaveJSONErrDeviceIsEmpty")).Returns("Устройство пустое");
            _mockLocalizationService.Setup(l => l.GetString("strSavingNotFullWarning")).Returns("Не все измерения собраны");
            _mockLocalizationService.Setup(l => l.GetString("strWarning")).Returns("Предупреждение");
            _mockLocalizationService.Setup(l => l.GetString("strSaveCanceled")).Returns("Сохранение отменено");
            _mockLocalizationService.Setup(l => l.GetString("strSaveJSONErrForSN")).Returns("Ошибка сохранения для SN");
            _mockLocalizationService.Setup(l => l.GetString("strCleanFieldWarning")).Returns("Очистить все поля?");
            _mockLocalizationService.Setup(l => l.GetString("strClearFieldsDone")).Returns("Поля очищены.");
            _mockLocalizationService.Setup(l => l.GetString("strErrMsgLangSwitchFailed")).Returns("Ошибка смены языка");
            _mockLocalizationService.Setup(l => l.GetString("strIncorrectFormatForSNErr")).Returns("Неверный формат SN");
            _mockLocalizationService.Setup(l => l.GetString("strSerialNumber")).Returns("Серийный номер");
            _mockLocalizationService.Setup(l => l.GetString("strAlreadyActivated")).Returns("уже активирован");
            _mockLocalizationService.Setup(l => l.GetString("strIncorrectMeasTimeFormat")).Returns("Неверный формат времени измерения");
            _mockLocalizationService.Setup(l => l.GetString("strCurrentMeasurementTime")).Returns("Текущее время измерения");
            _mockLocalizationService.Setup(l => l.GetString("strSeconds")).Returns("секунд");
            _mockLocalizationService.Setup(l => l.GetString("strViewModelClearing")).Returns("Очистка ViewModel");
            _mockLocalizationService.Setup(l => l.GetString("strViewModelCleared")).Returns("ViewModel очищен");
            _mockLocalizationService.Setup(l => l.GetString("strRunExternalAppNotFoundErr")).Returns("Внешнее приложение не найдено");
            _mockLocalizationService.Setup(l => l.GetString("strRunExternalAppUnexpectedErr")).Returns("Непредвиденная ошибка при запуске внешнего приложения");
            _mockLocalizationService.Setup(l => l.GetString("strErrAtCalibration")).Returns("Ошибка при калибровке");
            _mockLocalizationService.Setup(l => l.GetString("strErrUnexpected")).Returns("Непредвиденная ошибка");
            _mockLocalizationService.Setup(l => l.GetString("strErr")).Returns("Ошибка");
            _mockLocalizationService.Setup(l => l.GetString("strMeasuring")).Returns("Измерение...");

            if (App.Current == null)
            {
                new App();
            }
            var testDispatcher = new TestDispatcher();
            typeof(App)
                .GetProperty("Current")
                .GetValue(null)
                .GetType()
                .GetField("_dispatcher", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(App.Current, testDispatcher);

            _viewModel = new MeasurementViewModel(
                _mockColorMeasurementService.Object,
                _mockFileService.Object,
                _mockDialogService.Object,
                _mockLocalizationService.Object
            );
        }

        [TearDown]
        public void Teardown()
        {
            _viewModel.Dispose();
            if (App.Current != null)
            {
                typeof(App)
                    .GetProperty("Current")
                    .GetValue(null)
                    .GetType()
                    .GetField("_dispatcher", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    .SetValue(App.Current, null);
            }
        }

        // --- Тесты конструктора ---

        [Test]
        public void Ctor_InitializesWithDependencies()
        {
            Assert.That(_viewModel, Is.Not.Null);
        }

        [Test]
        public void Ctor_InitializesPropertiesToDefaultValues()
        {
            Assert.That(_viewModel.IsDeviceConnected, Is.False);
            Assert.That(_viewModel.IsMeasurementButtonsEnabled, Is.False);
            Assert.That(_viewModel.MeasurementTime, Is.EqualTo(2));
            Assert.That(_viewModel.SerialNumber, Is.Empty);
            Assert.That(_viewModel.LogText, Is.Empty);
            Assert.That(_viewModel.IsSerialNumberConfirmed, Is.False);
            Assert.That(_viewModel.IsDeviceCalibrated, Is.False);
            Assert.That(_viewModel.DeviceConnectionStatusText, Is.EqualTo("Отключено"));
            Assert.That(_viewModel.DeviceCalibrationStatusText, Is.EqualTo("Не откалибровано"));
        }

        [Test]
        public void Ctor_SubscribesToConnectionStatusChangedEvent()
        {
            _mockColorMeasurementService.VerifyAdd(s => s.ConnectionStatusChanged += It.IsAny<EventHandler<bool>>(), Times.Once());
        }

        [Test]
        public void Ctor_SubscribesToCalibrationStatusChangedEvent()
        {
            _mockColorMeasurementService.VerifyAdd(s => s.CalibrationStatusChanged += It.IsAny<EventHandler<bool>>(), Times.Once());
        }

        [Test]
        public void Ctor_SubscribesToServiceStatusMessages()
        {
            _mockColorMeasurementService.VerifyAdd(s => s.StatusMessage += It.IsAny<EventHandler<string>>(), Times.Once());
            _mockFileService.VerifyAdd(s => s.StatusMessage += It.IsAny<EventHandler<string>>(), Times.Once());
        }

        [Test]
        public void Ctor_PopulatesLocalizedStrings()
        {
            _mockLocalizationService.Verify(l => l.GetString("strStartMeasurement"), Times.AtLeastOnce());
            _mockLocalizationService.Verify(l => l.GetString("strStopMeasurement"), Times.AtLeastOnce());
        }

        // --- Тесты статуса соединения/калибровки ---

        [Test]
        public void ConnectionStatusChanged_UpdatesIsDeviceConnectedPropertyAndText()
        {
            _mockColorMeasurementService.Raise(s => s.ConnectionStatusChanged += null, null, true);
            Assert.That(_viewModel.IsDeviceConnected, Is.True);
            Assert.That(_viewModel.DeviceConnectionStatusText, Is.EqualTo("Подключено"));

            _mockColorMeasurementService.Raise(s => s.ConnectionStatusChanged += null, null, false);
            Assert.That(_viewModel.IsDeviceConnected, Is.False);
            Assert.That(_viewModel.DeviceConnectionStatusText, Is.EqualTo("Отключено"));
        }

        [Test]
        public void CalibrationStatusChanged_UpdatesIsDeviceCalibratedPropertyAndText()
        {
            _mockColorMeasurementService.Raise(s => s.CalibrationStatusChanged += null, null, true);
            Assert.That(_viewModel.IsDeviceCalibrated, Is.True);
            Assert.That(_viewModel.DeviceCalibrationStatusText, Is.EqualTo("Откалибровано"));

            _mockColorMeasurementService.Raise(s => s.CalibrationStatusChanged += null, null, false);
            Assert.That(_viewModel.IsDeviceCalibrated, Is.False);
            Assert.That(_viewModel.DeviceCalibrationStatusText, Is.EqualTo("Не откалибровано"));
        }

        [Test]
        public void ConnectionStatusChanged_UpdatesCommandCanExecuteStates()
        {
            // Initial state (disconnected)
            Assert.That(_viewModel.MeasureCommand.CanExecute(null), Is.False);
            Assert.That(_viewModel.ZeroCalibrationCommand.CanExecute(null), Is.False);

            // Connect
            _mockColorMeasurementService.SetupGet(s => s.IsConnected).Returns(true);
            _viewModel.IsDeviceConnected = true;
            _mockColorMeasurementService.Raise(s => s.ConnectionStatusChanged += null, null, true);

            Assert.That(_viewModel.MeasureCommand.CanExecute(null), Is.False);
            Assert.That(_viewModel.ZeroCalibrationCommand.CanExecute(null), Is.True);

            // Disconnect
            _mockColorMeasurementService.SetupGet(s => s.IsConnected).Returns(false);
            _viewModel.IsDeviceConnected = false;
            _mockColorMeasurementService.Raise(s => s.ConnectionStatusChanged += null, null, false);

            Assert.That(_viewModel.MeasureCommand.CanExecute(null), Is.False);
            Assert.That(_viewModel.ZeroCalibrationCommand.CanExecute(null), Is.False);
        }

        [Test]
        public void CalibrationStatusChanged_UpdatesCommandCanExecuteStates()
        {
            // Initial state (disconnected, not calibrated)
            Assert.That(_viewModel.MeasureCommand.CanExecute(null), Is.False);

            // Connect and then calibrate
            _mockColorMeasurementService.SetupGet(s => s.IsConnected).Returns(true);
            _viewModel.IsDeviceConnected = true;
            _viewModel.SerialNumber = "TESTSN";
            _viewModel.IsSerialNumberConfirmed = true;

            Assert.That(_viewModel.MeasureCommand.CanExecute(null), Is.False);

            _mockColorMeasurementService.SetupGet(s => s.IsCalibrated).Returns(true);
            _viewModel.IsDeviceCalibrated = true;
            _mockColorMeasurementService.Raise(s => s.CalibrationStatusChanged += null, null, true);

            Assert.That(_viewModel.MeasureCommand.CanExecute(null), Is.True);
        }

        // --- Тесты CanExecute команд ---

        [Test]
        public void ZeroCalibrationCommand_CanExecute_ReturnsTrueWhenConnectedAndNotCalibrating()
        {
            _mockColorMeasurementService.SetupGet(s => s.IsConnected).Returns(true);
            _viewModel.IsDeviceConnected = true;
            Assert.That(_viewModel.ZeroCalibrationCommand.CanExecute(null), Is.True);
        }

        [Test]
        public void ZeroCalibrationCommand_CanExecute_ReturnsFalseWhenNotConnected()
        {
            _mockColorMeasurementService.SetupGet(s => s.IsConnected).Returns(false);
            _viewModel.IsDeviceConnected = false;
            Assert.That(_viewModel.ZeroCalibrationCommand.CanExecute(null), Is.False);
        }

        [Test]
        public void MeasureCommand_CanExecute_ReturnsTrueWhenConnectedCalibratedAndSNConfirmed()
        {
            _mockColorMeasurementService.SetupGet(s => s.IsConnected).Returns(true);
            _mockColorMeasurementService.SetupGet(s => s.IsCalibrated).Returns(true);
            _viewModel.IsDeviceConnected = true;
            _viewModel.IsDeviceCalibrated = true;
            _viewModel.SerialNumber = "TESTSN";
            _viewModel.IsSerialNumberConfirmed = true;
            Assert.That(_viewModel.MeasureCommand.CanExecute(null), Is.True);
        }

        [Test]
        public void MeasureCommand_CanExecute_ReturnsFalseWhenDisconnected()
        {
            _mockColorMeasurementService.SetupGet(s => s.IsConnected).Returns(false);
            _viewModel.IsDeviceConnected = false;
            _viewModel.IsDeviceCalibrated = true;
            _viewModel.SerialNumber = "TESTSN";
            _viewModel.IsSerialNumberConfirmed = true;
            Assert.That(_viewModel.MeasureCommand.CanExecute(null), Is.False);
        }

        [Test]
        public void MeasureCommand_CanExecute_ReturnsFalseWhenNotCalibrated()
        {
            _mockColorMeasurementService.SetupGet(s => s.IsConnected).Returns(true);
            _mockColorMeasurementService.SetupGet(s => s.IsCalibrated).Returns(false);
            _viewModel.IsDeviceConnected = true;
            _viewModel.IsDeviceCalibrated = false;
            _viewModel.SerialNumber = "TESTSN";
            _viewModel.IsSerialNumberConfirmed = true;
            Assert.That(_viewModel.MeasureCommand.CanExecute(null), Is.False);
        }

        [Test]
        public void MeasureCommand_CanExecute_ReturnsFalseWhenSerialNumberNotConfirmed()
        {
            _mockColorMeasurementService.SetupGet(s => s.IsConnected).Returns(true);
            _mockColorMeasurementService.SetupGet(s => s.IsCalibrated).Returns(true);
            _viewModel.IsDeviceConnected = true;
            _viewModel.IsDeviceCalibrated = true;
            _viewModel.SerialNumber = "";
            _viewModel.IsSerialNumberConfirmed = false;
            Assert.That(_viewModel.MeasureCommand.CanExecute(null), Is.False);
        }

        [Test]
        public void SaveResultsCommand_CanExecute_ReturnsTrueWhenDeviceHasMeasurements()
        {
            _viewModel.ExecuteApplySerialNumber("SN123");
            _viewModel._currentDevice.AddMeasurement(new Measurement { Location = "Test", IsValid = true });
            Assert.That(_viewModel.SaveResultsCommand.CanExecute(null), Is.True);
        }

        [Test]
        public void SaveResultsCommand_CanExecute_ReturnsFalseWhenNoDeviceOrNoMeasurements()
        {
            Assert.That(_viewModel.SaveResultsCommand.CanExecute(null), Is.False);

            _viewModel.ExecuteApplySerialNumber("SN123");
            Assert.That(_viewModel.SaveResultsCommand.CanExecute(null), Is.False);
        }

        [Test]
        public void ClearFieldsCommand_CanExecute_ReturnsTrueAlways()
        {
            Assert.That(_viewModel.ClearFieldsCommand.CanExecute(null), Is.True);
        }

        [Test]
        public void SwitchLanguageCommand_CanExecute_ReturnsTrueWhenParameterIsString()
        {
            Assert.That(_viewModel.SwitchLanguageCommand.CanExecute("en-US"), Is.True);
            Assert.That(_viewModel.SwitchLanguageCommand.CanExecute(null), Is.False);
            Assert.That(_viewModel.SwitchLanguageCommand.CanExecute(123), Is.False);
        }

        [Test]
        public void ApplySerialNumberCommand_CanExecute_ReturnsTrueAlways()
        {
            Assert.That(_viewModel.ApplySerialNumberCommand.CanExecute("ABC123"), Is.True);
            Assert.That(_viewModel.ApplySerialNumberCommand.CanExecute(""), Is.True);
        }

        [Test]
        public void ApplyMeasurementTimeCommand_CanExecute_ReturnsTrueAlways()
        {
            Assert.That(_viewModel.ApplyMeasurementTimeCommand.CanExecute("10"), Is.True);
            Assert.That(_viewModel.ApplyMeasurementTimeCommand.CanExecute("invalid"), Is.True);
        }

        [Test]
        public void NewDeviceUnderTestCommand_CanExecute_ReturnsTrueWhenDeviceExistsWithMeasurements()
        {
            _viewModel.ExecuteApplySerialNumber("SN123");
            _viewModel._currentDevice.AddMeasurement(new Measurement { Location = "Test", IsValid = true });
            Assert.That(_viewModel.NewDeviceUnderTestCommand.CanExecute(null), Is.True);
        }

        [Test]
        public void NewDeviceUnderTestCommand_CanExecute_ReturnsFalseWhenNoDeviceOrNoMeasurements()
        {
            Assert.That(_viewModel.NewDeviceUnderTestCommand.CanExecute(null), Is.False);

            _viewModel.ExecuteApplySerialNumber("SN123");
            Assert.That(_viewModel.NewDeviceUnderTestCommand.CanExecute(null), Is.False);
        }

        [Test]
        public void LaunchExternalProgramCommand_CanExecute_ReturnsTrueAlways()
        {
            Assert.That(_viewModel.LaunchExternalProgramCommand.CanExecute(null), Is.True);
        }

        [Test]
        public void ClearLogCommand_CanExecute_ReturnsTrueAlways()
        {
            Assert.That(_viewModel.ClearLogCommand.CanExecute(null), Is.True);
        }

        // --- Тесты команды ZeroCalibration ---

        [Test]
        public async Task ZeroCalibrationCommand_Execute_CallsCalibrateZeroAsyncAndUpdatesStatus()
        {
            _mockColorMeasurementService.SetupGet(s => s.IsConnected).Returns(true);
            _viewModel.IsDeviceConnected = true;

            _mockColorMeasurementService.Setup(s => s.CalibrateZeroAsync()).Returns(Task.CompletedTask);

            await _viewModel.ZeroCalibrationCommand.ExecuteAsync(null);

            _mockColorMeasurementService.Verify(s => s.CalibrateZeroAsync(), Times.Once());
            _mockColorMeasurementService.Raise(s => s.CalibrationStatusChanged += null, null, true);
            Assert.That(_viewModel.IsDeviceCalibrated, Is.True);
            Assert.That(_viewModel.LogText, Does.Contain("Очистка ViewModel"));
        }

        [Test]
        public async Task ZeroCalibrationCommand_Execute_ConnectsIfDisconnectedThenCalibrates()
        {
            _mockColorMeasurementService.SetupGet(s => s.IsConnected).Returns(false);
            _viewModel.IsDeviceConnected = false;

            _mockColorMeasurementService.Setup(s => s.ConnectAsync()).Returns(Task.CompletedTask);
            _mockColorMeasurementService.Setup(s => s.CalibrateZeroAsync()).Returns(Task.CompletedTask);

            await _viewModel.ZeroCalibrationCommand.ExecuteAsync(null);

            _mockColorMeasurementService.Verify(s => s.ConnectAsync(), Times.Once());
            _mockColorMeasurementService.Verify(s => s.CalibrateZeroAsync(), Times.Once());
        }

        [Test]
        public async Task ZeroCalibrationCommand_Execute_HandlesException()
        {
            _mockColorMeasurementService.SetupGet(s => s.IsConnected).Returns(true);
            _viewModel.IsDeviceConnected = true;

            _mockColorMeasurementService.Setup(s => s.CalibrateZeroAsync()).ThrowsAsync(new Exception("Calibration error"));
            _mockDialogService.Setup(d => d.ShowMessage(It.IsAny<string>(), It.IsAny<string>()));

            await _viewModel.ZeroCalibrationCommand.ExecuteAsync(null);

            _mockDialogService.Verify(d => d.ShowMessage(
                It.Is<string>(msg => msg.Contains("Ошибка при калибровке")),
                It.Is<string>(title => title == "Ошибка")), Times.Once());
            _mockColorMeasurementService.Verify(s => s.Disconnect(), Times.Once());
        }

        // --- Тесты команды SaveResults ---

        [Test]
        public async Task SaveResultsCommand_Execute_SavesDeviceDataToFile()
        {
            _viewModel.ExecuteApplySerialNumber("TESTSN");
            var measurement = new Measurement { Location = "Top", x = 0.3, y = 0.3, Lv = 100, T = 6500, IsValid = true };
            _viewModel._currentDevice.AddMeasurement(measurement);

            // ИСПРАВЛЕНИЕ: Изменено с ReturnsAsync(true) на Returns(Task.CompletedTask)
            _mockFileService.Setup(f => f.SaveDeviceDataToJsonAsync(It.IsAny<DeviceUnderTest>())).Returns(Task.CompletedTask);
            _mockDialogService.Setup(d => d.ShowQuestion(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

            await _viewModel.SaveResultsCommand.ExecuteAsync(null);

            _mockFileService.Verify(f => f.SaveDeviceDataToJsonAsync(_viewModel._currentDevice), Times.Once());
            Assert.That(_viewModel.LogText, Does.Contain("Сохранение..."));
        }

        [Test]
        public async Task SaveResultsCommand_Execute_ShowsWarningAndCanBeCanceledIfNotFull()
        {
            _viewModel.ExecuteApplySerialNumber("TESTSN");
            var measurement = new Measurement { Location = "Top", x = 0.3, y = 0.3, Lv = 100, T = 6500, IsValid = true };
            _viewModel._currentDevice.AddMeasurement(measurement);
            _mockDialogService.Setup(d => d.ShowQuestion(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

            await _viewModel.SaveResultsCommand.ExecuteAsync(null);

            _mockDialogService.Verify(d => d.ShowQuestion("Не все измерения собраны", "Предупреждение"), Times.Once());
            // ИСПРАВЛЕНИЕ: Изменено с ReturnsAsync(true) на Returns(Task.CompletedTask)
            _mockFileService.Verify(f => f.SaveDeviceDataToJsonAsync(It.IsAny<DeviceUnderTest>()), Times.Never());
            Assert.That(_viewModel.LogText, Does.Contain("Сохранение отменено"));
        }

        [Test]
        public async Task SaveResultsCommand_Execute_HandlesFileServiceException()
        {
            _viewModel.ExecuteApplySerialNumber("TESTSN");
            var measurement = new Measurement { Location = "Top", x = 0.3, y = 0.3, Lv = 100, T = 6500, IsValid = true };
            _viewModel._currentDevice.AddMeasurement(measurement);
            _mockFileService.Setup(f => f.SaveDeviceDataToJsonAsync(It.IsAny<DeviceUnderTest>()))
                .ThrowsAsync(new IOException("Disk error"));
            _mockDialogService.Setup(d => d.ShowMessage(It.IsAny<string>(), It.IsAny<string>()));
            _mockDialogService.Setup(d => d.ShowQuestion(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

            await _viewModel.SaveResultsCommand.ExecuteAsync(null);

            _mockDialogService.Verify(d => d.ShowMessage(
                It.Is<string>(msg => msg.Contains("Disk error")),
                It.Is<string>(title => title == "Ошибка")), Times.Once());
            Assert.That(_viewModel.LogText, Does.Contain("Непредвиденная ошибка: Disk error"));
        }

        // --- Тесты команды ClearFields ---

        [Test]
        public void ClearFieldsCommand_Execute_ResetsPropertiesAndStatuses()
        {
            _viewModel.SerialNumber = "OLD_SN";
            _viewModel.IsSerialNumberConfirmed = true;
            _viewModel.MeasurementTime = 10;
            _viewModel.LogText = "Some old log message";
            _viewModel.ExecuteApplySerialNumber("SN123");
            _viewModel._currentDevice.AddMeasurement(new Measurement { Location = "Test", IsValid = true });

            var status = MeasurementStatusManager.Instance.AllMeasurementButtonStatuses.FirstOrDefault();
            if (status != null)
            {
                status.IsPassed = true;
                status.MeasuredValuesString = "x=0.1, y=0.2";
            }

            _mockDialogService.Setup(d => d.ShowQuestion(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

            _viewModel.ClearFieldsCommand.Execute(null);

            Assert.That(_viewModel.SerialNumber, Is.Empty);
            Assert.That(_viewModel.IsSerialNumberConfirmed, Is.False);
            Assert.That(_viewModel.MeasurementTime, Is.EqualTo(2));
            Assert.That(_viewModel.LogText, Does.Contain("Поля очищены."));
            Assert.That(_viewModel._currentDevice, Is.Null);

            foreach (var s in MeasurementStatusManager.Instance.AllMeasurementButtonStatuses)
            {
                Assert.That(s.IsPassed, Is.Null);
                Assert.That(s.MeasuredValuesString, Is.Empty);
            }
        }

        [Test]
        public void ClearFieldsCommand_Execute_HandlesCancellation()
        {
            _viewModel.SerialNumber = "OLD_SN";
            _mockDialogService.Setup(d => d.ShowQuestion(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

            _viewModel.ClearFieldsCommand.Execute(null);

            Assert.That(_viewModel.SerialNumber, Is.EqualTo("OLD_SN"));
            Assert.That(_viewModel.LogText, Does.Not.Contain("Поля очищены."));
        }

        // --- Тесты команды SwitchLanguage ---

        [Test]
        public void SwitchLanguageCommand_Execute_SetsLanguage()
        {
            _mockLocalizationService.Setup(l => l.SetLanguage("en-US"));
            _mockLocalizationService.Setup(l => l.CurrentCulture).Returns(new System.Globalization.CultureInfo("en-US"));

            _viewModel.SwitchLanguageCommand.Execute("en-US");

            _mockLocalizationService.Verify(l => l.SetLanguage("en-US"), Times.Once());
            Assert.That(System.Threading.Thread.CurrentThread.CurrentCulture.Name, Is.EqualTo("en-US"));
            Assert.That(System.Threading.Thread.CurrentThread.CurrentUICulture.Name, Is.EqualTo("en-US"));
        }

        [Test]
        public void SwitchLanguageCommand_Execute_HandlesException()
        {
            _mockLocalizationService.Setup(l => l.SetLanguage(It.IsAny<string>())).Throws(new Exception("Language switch failed"));
            _mockDialogService.Setup(d => d.ShowMessage(It.IsAny<string>(), It.IsAny<string>()));

            _viewModel.SwitchLanguageCommand.Execute("invalid-code");

            _mockDialogService.Verify(d => d.ShowMessage(
                It.Is<string>(msg => msg.Contains("Ошибка смены языка: Language switch failed")),
                It.Is<string>(title => title == "Ошибка")), Times.Once());
        }

        // --- Тесты команды MeasureCommand ---

        [Test]
        public async Task MeasureCommand_Execute_PerformsMeasurementAndUpdatesStatus()
        {
            _mockColorMeasurementService.SetupGet(s => s.IsConnected).Returns(true);
            _mockColorMeasurementService.SetupGet(s => s.IsCalibrated).Returns(true);
            _viewModel.IsDeviceConnected = true;
            _viewModel.IsDeviceCalibrated = true;
            _viewModel.SerialNumber = "TESTSN";
            _viewModel.IsSerialNumberConfirmed = true;
            _viewModel.MeasurementTime = 5;

            var mockMeasurement = new Measurement { x = 0.3, y = 0.35, Lv = 123.45, T = 6500, IsValid = true };
            _mockColorMeasurementService.Setup(s => s.MeasureAsync(5))
                .ReturnsAsync(mockMeasurement);

            string measurementLocation = "TopLeft";
            MeasurementStatusManager.Instance.AllMeasurementButtonStatuses.Add(new MeasurementStatusViewModel { Location = measurementLocation });

            await _viewModel.MeasureCommand.ExecuteAsync(measurementLocation);

            _mockColorMeasurementService.Verify(s => s.MeasureAsync(5), Times.Once());

            Assert.That(_viewModel.LogText, Does.Contain($"Тест начат для SN: TESTSN"));
            Assert.That(_viewModel.LogText, Does.Contain($"Измерение..."));
            Assert.That(_viewModel.LogText, Does.Contain($"Результат '{measurementLocation}': x=0.300, y=0.350, Lv=123.5, T=6500"));

            Assert.That(_viewModel._currentDevice, Is.Not.Null);
            Assert.That(_viewModel._currentDevice.SerialNumber, Is.EqualTo("TESTSN"));
            Assert.That(_viewModel._currentDevice.Measurements.Count, Is.EqualTo(1));
            Assert.That(_viewModel._currentDevice.Measurements.First().Location, Is.EqualTo(measurementLocation));
            Assert.That(_viewModel._currentDevice.Measurements.First().Lv, Is.EqualTo(123.45));

            var status = MeasurementStatusManager.Instance.AllMeasurementButtonStatuses.FirstOrDefault(s => s.Location == measurementLocation);
            Assert.That(status, Is.Not.Null);
            Assert.That(status.IsPassed, Is.True);
            Assert.That(status.MeasuredValuesString, Is.EqualTo("x=0.300, y=0.350, Lv=123.5, T=6500"));
        }

        [Test]
        public async Task MeasureCommand_Execute_HandlesLvTooLowValidation()
        {
            _mockColorMeasurementService.SetupGet(s => s.IsConnected).Returns(true);
            _mockColorMeasurementService.SetupGet(s => s.IsCalibrated).Returns(true);
            _viewModel.IsDeviceConnected = true;
            _viewModel.IsDeviceCalibrated = true;
            _viewModel.SerialNumber = "TESTSN";
            _viewModel.IsSerialNumberConfirmed = true;
            _viewModel.MeasurementTime = 5;

            var mockMeasurement = new Measurement { x = 0.3, y = 0.35, Lv = 5.0, T = 6500, IsValid = true };
            _mockColorMeasurementService.Setup(s => s.MeasureAsync(5))
                .ReturnsAsync(mockMeasurement);

            string measurementLocation = "TopLeft";
            MeasurementStatusManager.Instance.AllMeasurementButtonStatuses.Add(new MeasurementStatusViewModel { Location = measurementLocation });

            await _viewModel.MeasureCommand.ExecuteAsync(measurementLocation);

            _mockColorMeasurementService.Verify(s => s.MeasureAsync(5), Times.Once());

            Assert.That(_viewModel.LogText, Does.Contain($"Значение Lv слишком низкое: 5.0. Проверьте щуп"));
            Assert.That(_viewModel.LogText, Does.Not.Contain($"Результат '{measurementLocation}':"));

            Assert.That(_viewModel._currentDevice.Measurements.Count, Is.EqualTo(0));

            var status = MeasurementStatusManager.Instance.AllMeasurementButtonStatuses.FirstOrDefault(s => s.Location == measurementLocation);
            Assert.That(status, Is.Not.Null);
            Assert.That(status.IsPassed, Is.False);
            Assert.That(status.MeasuredValuesString, Is.EqualTo("Значение Lv слишком низкое: 5.0"));
        }

        [Test]
        public async Task MeasureCommand_Execute_HandlesMeasurementException()
        {
            _mockColorMeasurementService.SetupGet(s => s.IsConnected).Returns(true);
            _mockColorMeasurementService.SetupGet(s => s.IsCalibrated).Returns(true);
            _viewModel.IsDeviceConnected = true;
            _viewModel.IsDeviceCalibrated = true;
            _viewModel.SerialNumber = "TESTSN";
            _viewModel.IsSerialNumberConfirmed = true;
            _viewModel.MeasurementTime = 5;

            _mockColorMeasurementService.Setup(s => s.MeasureAsync(It.IsAny<int>()))
                .ThrowsAsync(new InvalidOperationException("Device communication error"));

            string measurementLocation = "BottomRight";
            MeasurementStatusManager.Instance.AllMeasurementButtonStatuses.Add(new MeasurementStatusViewModel { Location = measurementLocation });

            _mockDialogService.Setup(d => d.ShowMessage(It.IsAny<string>(), It.IsAny<string>()));

            await _viewModel.MeasureCommand.ExecuteAsync(measurementLocation);

            _mockColorMeasurementService.Verify(s => s.MeasureAsync(It.IsAny<int>()), Times.Once());

            _mockDialogService.Verify(d => d.ShowMessage(
                It.Is<string>(msg => msg.Contains("Непредвиденная ошибка измерения 'BottomRight': Device communication error")),
                It.Is<string>(title => title == "Ошибка")), Times.Once());

            var status = MeasurementStatusManager.Instance.AllMeasurementButtonStatuses.FirstOrDefault(s => s.Location == measurementLocation);
            Assert.That(status, Is.Not.Null);
            Assert.That(status.IsPassed, Is.False);
            Assert.That(status.MeasuredValuesString, Is.EqualTo("Ошибка: Device communication error"));
            Assert.That(_viewModel.LogText, Does.Not.Contain("Результат"));
        }

        // --- Тесты команды ApplySerialNumber ---

        [Test]
        public void ApplySerialNumberCommand_Execute_SetsSerialNumberAndConfirms()
        {
            string newSerialNumber = "ABC123XYZ";
            _mockLocalizationService.Setup(l => l.GetString("strCurrentSN")).Returns("Текущий SN:");

            _viewModel.ApplySerialNumberCommand.Execute(newSerialNumber);

            Assert.That(_viewModel.SerialNumber, Is.EqualTo(newSerialNumber));
            Assert.That(_viewModel.IsSerialNumberConfirmed, Is.True);
            Assert.That(_viewModel._currentDevice, Is.Not.Null);
            Assert.That(_viewModel._currentDevice.SerialNumber, Is.EqualTo(newSerialNumber));
            Assert.That(_viewModel.LogText, Does.Contain($"Текущий SN: {newSerialNumber}"));
        }

        [Test]
        public void ApplySerialNumberCommand_Execute_HandlesInvalidSerialNumber()
        {
            string invalidSerialNumber = "SN-123!";
            _mockDialogService.Setup(d => d.ShowMessage(It.IsAny<string>(), It.IsAny<string>()));

            _viewModel.ApplySerialNumberCommand.Execute(invalidSerialNumber);

            Assert.That(_viewModel.SerialNumber, Is.Empty);
            Assert.That(_viewModel.IsSerialNumberConfirmed, Is.False);
            Assert.That(_viewModel._currentDevice, Is.Null);
            _mockDialogService.Verify(d => d.ShowMessage(
                It.Is<string>(msg => msg.Contains("Неверный формат SN")),
                It.Is<string>(title => title == "Ошибка")), Times.Once());
        }

        // --- Тесты команды ApplyMeasurementTime ---

        [Test]
        public void ApplyMeasurementTimeCommand_Execute_SetsMeasurementTime()
        {
            string newTime = "15";
            _mockLocalizationService.Setup(l => l.GetString("strCurrentMeasurementTime")).Returns("Текущее время измерения:");
            _mockLocalizationService.Setup(l => l.GetString("strSeconds")).Returns("секунд");

            _viewModel.ApplyMeasurementTimeCommand.Execute(newTime);

            Assert.That(_viewModel.MeasurementTime, Is.EqualTo(15));
            Assert.That(_viewModel.LogText, Does.Contain("Текущее время измерения: 15 секунд"));
        }

        [Test]
        public void ApplyMeasurementTimeCommand_Execute_HandlesInvalidTime()
        {
            string invalidTime = "abc";
            _mockDialogService.Setup(d => d.ShowMessage(It.IsAny<string>(), It.IsAny<string>()));

            _viewModel.ApplyMeasurementTimeCommand.Execute(invalidTime);

            Assert.That(_viewModel.MeasurementTime, Is.EqualTo(2));
            _mockDialogService.Verify(d => d.ShowMessage(
                It.Is<string>(msg => msg.Contains("Неверный формат времени измерения")),
                It.Is<string>(title => title == "Ошибка")), Times.Once());
        }

        [Test]
        public void ApplyMeasurementTimeCommand_Execute_HandlesZeroOrNegativeTime()
        {
            _mockDialogService.Setup(d => d.ShowMessage(It.IsAny<string>(), It.IsAny<string>()));

            _viewModel.ApplyMeasurementTimeCommand.Execute("0");
            Assert.That(_viewModel.MeasurementTime, Is.EqualTo(2));
            _mockDialogService.Verify(d => d.ShowMessage(
                It.Is<string>(msg => msg.Contains("Неверный формат времени измерения")),
                It.Is<string>(title => title == "Ошибка")), Times.Once());

            _mockDialogService.Invocations.Clear();
            _viewModel.ApplyMeasurementTimeCommand.Execute("-5");
            Assert.That(_viewModel.MeasurementTime, Is.EqualTo(2));
            _mockDialogService.Verify(d => d.ShowMessage(
                It.Is<string>(msg => msg.Contains("Неверный формат времени измерения")),
                It.Is<string>(title => title == "Ошибка")), Times.Once());
        }

        // --- Тесты команды NewDeviceUnderTest ---

        [Test]
        public async Task NewDeviceUnderTestCommand_Execute_ClearsAndOffersSaveIfDataExists()
        {
            _viewModel.SerialNumber = "OLD_SN";
            _viewModel.IsSerialNumberConfirmed = true;
            _viewModel.ExecuteApplySerialNumber("OLD_SN");
            _viewModel._currentDevice.AddMeasurement(new Measurement { Location = "Test", IsValid = true });

            _mockDialogService.Setup(d => d.ShowQuestion(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
            // ИСПРАВЛЕНИЕ: Изменено с ReturnsAsync(true) на Returns(Task.CompletedTask)
            _mockFileService.Setup(f => f.SaveDeviceDataToJsonAsync(It.IsAny<DeviceUnderTest>())).Returns(Task.CompletedTask);

            await _viewModel.NewDeviceUnderTestCommand.ExecuteAsync(null);

            _mockFileService.Verify(f => f.SaveDeviceDataToJsonAsync(_viewModel._currentDevice), Times.Once());
            Assert.That(_viewModel.SerialNumber, Is.Empty);
            Assert.That(_viewModel._currentDevice, Is.Null);
            Assert.That(_viewModel.IsSerialNumberConfirmed, Is.False);
            Assert.That(_viewModel.LogText, Does.Contain("Поля очищены."));
        }

        [Test]
        public async Task NewDeviceUnderTestCommand_Execute_ClearsWithoutSaveIfNoData()
        {
            _viewModel.SerialNumber = "NEW_SN";
            _viewModel.IsSerialNumberConfirmed = true;
            _mockDialogService.Setup(d => d.ShowQuestion(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

            await _viewModel.NewDeviceUnderTestCommand.ExecuteAsync(null);

            _mockFileService.Verify(f => f.SaveDeviceDataToJsonAsync(It.IsAny<DeviceUnderTest>()), Times.Never());
            Assert.That(_viewModel.SerialNumber, Is.Empty);
            Assert.That(_viewModel.IsSerialNumberConfirmed, Is.False);
            Assert.That(_viewModel._currentDevice, Is.Null);
            Assert.That(_viewModel.LogText, Does.Contain("Поля очищены."));
        }

        // --- Тесты команды ClearLog ---

        [Test]
        public void ClearLogCommand_Execute_ClearsLogText()
        {
            _viewModel.LogText = "Some log message to clear.";

            _viewModel.ClearLogCommand.Execute(null);

            Assert.That(_viewModel.LogText, Is.Empty);
        }

        // --- Тесты IDisposable ---

        [Test]
        public void Dispose_UnsubscribesFromEvents()
        {
            _viewModel.Dispose();

            _mockColorMeasurementService.VerifyRemove(s => s.StatusMessage -= It.IsAny<EventHandler<string>>(), Times.Once());
            _mockColorMeasurementService.VerifyRemove(s => s.ConnectionStatusChanged -= It.IsAny<EventHandler<bool>>(), Times.Once());
            _mockColorMeasurementService.VerifyRemove(s => s.CalibrationStatusChanged -= It.IsAny<EventHandler<bool>>(), Times.Once());
            _mockFileService.VerifyRemove(s => s.StatusMessage -= It.IsAny<EventHandler<string>>(), Times.Once());

            Assert.That(_viewModel.LogText, Does.Contain("Очистка ViewModel"));
            Assert.That(_viewModel.LogText, Does.Contain("ViewModel очищен"));
        }

        [Test]
        public void Dispose_CallsDisposeOnServicesIfDisposable()
        {
            var mockColorServiceDisposable = _mockColorMeasurementService.As<IDisposable>();
            var mockFileServiceDisposable = _mockFileService.As<IDisposable>();
            var mockDialogServiceDisposable = _mockDialogService.As<IDisposable>();

            _viewModel.Dispose();

            mockColorServiceDisposable.Verify(d => d.Dispose(), Times.Once());
            mockFileServiceDisposable.Verify(d => d.Dispose(), Times.Once());
            mockDialogServiceDisposable.Verify(d => d.Dispose(), Times.Once());
        }

        // --- Тесты AreAllStatusesRepresentedInMeasurements ---

        [Test]
        public void AreAllStatusesRepresentedInMeasurements_ReturnsTrue_WhenAllPresent()
        {
            _viewModel.ExecuteApplySerialNumber("SN_FULL");
            foreach (var status in MeasurementStatusManager.Instance.AllMeasurementButtonStatuses)
            {
                _viewModel._currentDevice.AddMeasurement(new Measurement { Location = status.Location, IsValid = true });
            }

            bool result = _viewModel.AreAllStatusesRepresentedInMeasurements();

            Assert.That(result, Is.True);
        }

        [Test]
        public void AreAllStatusesRepresentedInMeasurements_ReturnsFalse_WhenNotAllPresent()
        {
            _viewModel.ExecuteApplySerialNumber("SN_PARTIAL");
            _viewModel._currentDevice.AddMeasurement(new Measurement { Location = "TopLeft", IsValid = true });

            bool result = _viewModel.AreAllStatusesRepresentedInMeasurements();

            Assert.That(result, Is.False);
        }

        [Test]
        public void AreAllStatusesRepresentedInMeasurements_ReturnsFalse_WhenNoDevice()
        {
            bool result = _viewModel.AreAllStatusesRepresentedInMeasurements();

            Assert.That(result, Is.False);
        }

        [Test]
        public void AreAllStatusesRepresentedInMeasurements_ReturnsFalse_WhenDeviceHasNoMeasurements()
        {
            _viewModel.ExecuteApplySerialNumber("SN_EMPTY");

            bool result = _viewModel.AreAllStatusesRepresentedInMeasurements();

            Assert.That(result, Is.False);
        }
    }
}