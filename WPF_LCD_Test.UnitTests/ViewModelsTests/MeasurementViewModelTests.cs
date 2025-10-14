using Moq;
using System.Globalization;
using WPF_LCD_Test.Interfaces;
using WPF_LCD_Test.Models;
using WPF_LCD_Test.Services; // Добавляем using для доступа к MeasurementStatusService
using WPF_LCD_Test.ViewModels;

// Это важно: позволяет получить доступ к свойствам Resources.resx напрямую (например, IncorrectMeasTimeFormat)
using static WPF_LCD_Test.Resources.Resources;

namespace WPF_LCD_Test.UnitTests.ViewModels
{
    [TestFixture]
    public class MeasurementViewModelTests
    {
        private Mock<IColorMeasurementService> _mockColorMeasurementService;
        private Mock<IFileService> _mockFileService;
        private Mock<IDialogService> _mockDialogService;
        private Mock<ILocalizationService> _mockLocalizationService;
        private Mock<IDispatcher> _mockDispatcher;
        private MeasurementViewModel _viewModel;

        [SetUp]
        public void Setup()
        {
            _mockColorMeasurementService = new Mock<IColorMeasurementService>();
            _mockFileService = new Mock<IFileService>();
            _mockDialogService = new Mock<IDialogService>();
            _mockLocalizationService = new Mock<ILocalizationService>();
            _mockDispatcher = new Mock<IDispatcher>();

            // --- КЛЮЧЕВОЕ ИЗМЕНЕНИЕ ЗДЕСЬ: Добавляем IDisposable к мокам СРАЗУ ПОСЛЕ ИХ СОЗДАНИЯ ---
            // Делаем это, если интерфейсы IColorMeasurementService, IFileService, IDialogService
            // сами по себе НЕ реализуют IDisposable, но ViewModel пытается Dispose() их.
            _mockColorMeasurementService.As<IDisposable>();
            _mockFileService.As<IDisposable>();
            _mockDialogService.As<IDisposable>();

            // И сразу настраиваем их поведение Dispose()
            _mockColorMeasurementService.As<IDisposable>().Setup(m => m.Dispose());
            _mockFileService.As<IDisposable>().Setup(m => m.Dispose());
            _mockDialogService.As<IDisposable>().Setup(m => m.Dispose());

            // Настройка поведения мока диспетчера:
            _mockDispatcher.Setup(d => d.Invoke(It.IsAny<Action>()))
                           .Callback<Action>(action => action.Invoke());
            _mockDispatcher.Setup(d => d.BeginInvoke(It.IsAny<Action>()))
                           .Callback<Action>(action => action.Invoke());
            _mockDispatcher.Setup(d => d.CheckAccess()).Returns(true);

            _mockLocalizationService.Setup(l => l.GetString(It.IsAny<string>())).Returns((string key) => key);
            _mockLocalizationService.Setup(l => l.GetString(It.IsAny<string>(), It.IsAny<object[]>())).Returns((string key, object[] args) => key + string.Join("", args));

            _viewModel = new MeasurementViewModel(
                _mockColorMeasurementService.Object,
                _mockFileService.Object,
                _mockDialogService.Object,
                _mockLocalizationService.Object,
                _mockDispatcher.Object
            );

            // Настройка LocalizationService:
            // Мокируем сервис локализации, чтобы он возвращал реальные значения из Resources.resx
            _mockLocalizationService.Setup(l => l.GetString(It.IsAny<string>()))
                                        .Returns((string key) =>
                                        {
                                            switch (key)
                                            {
                                                case nameof(ConnectingCA): return ConnectingCA;
                                                case nameof(ConnectedCA): return ConnectedCA;
                                                case nameof(DisconnectedCA): return DisconnectedCA;
                                                case nameof(Err): return Err;
                                                case nameof(FillSN): return FillSN;
                                                case nameof(NoSNErr): return NoSNErr;
                                                case nameof(TestStartInfo): return TestStartInfo;
                                                case nameof(Result): return Result;
                                                case nameof(NoData): return NoData;
                                                case nameof(LvIsTooLow): return LvIsTooLow;
                                                case nameof(CheckProbe): return CheckProbe;
                                                case nameof(InvalidResultErr): return InvalidResultErr;
                                                case nameof(ColorServiceErr): return ColorServiceErr;
                                                case nameof(ColorAnalyzerErr): return ColorAnalyzerErr;
                                                case nameof(UnexpectedMeasurementErr): return UnexpectedMeasurementErr;
                                                case nameof(Saving): return Saving;
                                                case nameof(SaveJSONErrDeviceIsEmpty): return SaveJSONErrDeviceIsEmpty;
                                                case nameof(SavingNotFullWarning): return SavingNotFullWarning;
                                                case nameof(Warning): return Warning;
                                                case nameof(SaveCanceled): return SaveCanceled;
                                                case nameof(SaveJSONErrForSN): return SaveJSONErrForSN;
                                                case nameof(CleanFieldWarning): return CleanFieldWarning;
                                                case nameof(ClearFieldsDone): return ClearFieldsDone;
                                                case nameof(ErrMsgLangSwitchFailed): return ErrMsgLangSwitchFailed;
                                                case nameof(IncorrectFormatForSNErr): return IncorrectFormatForSNErr;
                                                case nameof(SerialNumber): return SerialNumber;
                                                case nameof(AlreadyActivated): return AlreadyActivated;
                                                case nameof(IncorrectMeasTimeFormat): return IncorrectMeasTimeFormat;
                                                case nameof(CurrentMeasurementTime): return CurrentMeasurementTime;
                                                case nameof(Seconds): return Seconds;
                                                case nameof(ViewModelClearing): return ViewModelClearing;
                                                case nameof(ViewModelCleared): return ViewModelCleared;
                                                case nameof(RunExternalAppNotFoundErr): return RunExternalAppNotFoundErr;
                                                case nameof(RunExternalAppUnexpectedErr): return RunExternalAppUnexpectedErr;
                                                case nameof(ErrAtCalibration): return ErrAtCalibration;
                                                case nameof(ErrUnexpected): return ErrUnexpected;
                                                case nameof(Measuring): return Measuring;
                                                case nameof(ResultsSaved): return ResultsSaved;
                                                case nameof(ConnectionError): return ConnectionError;
                                                case nameof(ErrReleaseConnectionCA): return ErrReleaseConnectionCA;
                                                case nameof(MeasureWihoutConnectionError): return MeasureWihoutConnectionError;
                                                case nameof(MakeZeroCalibration): return MakeZeroCalibration;
                                                case nameof(ZeroCalibratedCA): return ZeroCalibratedCA;
                                                case nameof(WorkFolderCreatedForSN): return WorkFolderCreatedForSN;
                                                case nameof(CalibratedCA): return CalibratedCA;
                                                case nameof(NotCalibratedCa): return NotCalibratedCa;
                                                case nameof(TestFormatString): return TestFormatString;
                                                case nameof(BadConnection): return BadConnection;
                                                case nameof(ZeroCalibration): return ZeroCalibration;
                                                default: return $"{key}";
                                            }
                                        });

            // Создаем ViewModel, передавая ему моки сервисов
            _viewModel = new MeasurementViewModel(
                _mockColorMeasurementService.Object,
                _mockFileService.Object,
                _mockDialogService.Object,
                _mockLocalizationService.Object,
                _mockDispatcher.Object
            );
        }

        [TearDown]
        public void Teardown()
        {
            _viewModel.Dispose();
        }

        [Test]
        public void InitializeViewModel_SetsInitialStates()
        {
            Assert.That(_viewModel.IsDeviceConnected, Is.False);
            Assert.That(_viewModel.IsDeviceCalibrated, Is.False);
            Assert.That(_viewModel.IsSerialNumberConfirmed, Is.False);
            Assert.That(_viewModel.IsMeasurementButtonsEnabled, Is.False);
        }

        // --- Тесты для ZeroCalibrationCommand ---
        [Test]
        public async Task ZeroCalibrationCommand_Execute_CalibratesAndLogs()
        {
            // Arrange
            // 1. Имитируем успешное подключение устройства, чтобы CanExecute команды был true.
            _mockColorMeasurementService.Raise(
                s => s.ConnectionStatusChanged += null, // Событие, которое нужно вызвать
                _mockColorMeasurementService.Object,    // Отправитель события
                true                                    // Аргумент события (статус подключения = true)
            );

            // Убедимся, что ViewModel обновил свой статус подключения
            Assert.That(_viewModel.IsDeviceConnected, Is.True, "ViewModel.IsDeviceConnected should be true after mocking ConnectionStatusChanged event.");

            // 2. Мокируем CalibrateZeroAsync И имитируем вызовы событий StatusMessage и CalibrationStatusChanged
            _mockColorMeasurementService.Setup(s => s.CalibrateZeroAsync())
                .Returns(Task.FromResult(true)) // Возвращаем успешный результат операции
                .Callback(() =>
                {
                    // Имитируем вызов события StatusMessage в начале калибровки
                    _mockColorMeasurementService.Raise(
                        s => s.StatusMessage += null,
                        _mockColorMeasurementService.Object,
                        _mockLocalizationService.Object.GetString(CalibratingZeroCA) // ПЕРВОЕ СООБЩЕНИЕ
                    );

                    // Имитируем вызов события CalibrationStatusChanged при завершении калибровки
                    _mockColorMeasurementService.Raise(
                        s => s.CalibrationStatusChanged += null,
                        _mockColorMeasurementService.Object,
                        true
                    );

                    // Имитируем вызов события StatusMessage после успешной калибровки
                    _mockColorMeasurementService.Raise(
                        s => s.StatusMessage += null,
                        _mockColorMeasurementService.Object,
                        _mockLocalizationService.Object.GetString(ZeroCalibratedCA) // ВТОРОЕ СООБЩЕНИЕ
                    );
                });

            // Act
            _viewModel.ZeroCalibrationCommand.Execute(null);

            // Ждем завершения асинхронной операции и обработки события ViewModel'ом.
            // Возможно, потребуется небольшая задержка, если логика ViewModel асинхронна после события.
            await Task.Delay(50);

            // Assert
            // Проверяем, что мокированный метод был вызван.
            _mockColorMeasurementService.Verify(s => s.CalibrateZeroAsync(), Times.Once);

            // Проверяем, что ViewModel обновил статус калибровки.
            Assert.That(_viewModel.IsDeviceCalibrated, Is.True, "ViewModel.IsDeviceCalibrated should be true after successful calibration.");

            // Проверяем, что в лог добавлено ОБА сообщения о калибровке.
            // Используем Does.Contain дважды для обоих сообщений.
            Assert.That(_viewModel.LogText, Does.Contain(_mockLocalizationService.Object.GetString(CalibratingZeroCA)),
                        "Log should contain the 'Calibrating Zero' message.");
            Assert.That(_viewModel.LogText, Does.Contain(_mockLocalizationService.Object.GetString(ZeroCalibratedCA)),
                        "Log should contain the 'Zero is calibrated' message.");
        }

        [Test]
        public async Task ZeroCalibrationCommand_Execute_HandlesCalibrationError()
        {
            // Arrange
            _mockColorMeasurementService.Setup(s => s.IsDeviceConnected).Returns(true);
            _viewModel.IsDeviceConnected = true;

            // Мокируем CalibrateZeroAsync И имитируем вызовы событий StatusMessage и CalibrationStatusChanged
            _mockColorMeasurementService.Setup(s => s.CalibrateZeroAsync())
                .Returns(Task.FromResult(false)) // Возвращаем успешный результат операции
                .Callback(() =>
                {
                    // Имитируем вызов события StatusMessage в начале калибровки
                    _mockColorMeasurementService.Raise(
                        s => s.StatusMessage += null,
                        _mockColorMeasurementService.Object,
                        _mockLocalizationService.Object.GetString(CalibratingZeroCA)
                    );

                    // Имитируем вызов события StatusMessage после успешной калибровки
                    _mockColorMeasurementService.Raise(
                        s => s.StatusMessage += null,
                        _mockColorMeasurementService.Object,
                        _mockLocalizationService.Object.GetString(CheckConnectionCA)
                    );
                });

            // Act
            _viewModel.ZeroCalibrationCommand.Execute(null);
            await Task.Delay(50);

            // Assert
            _mockColorMeasurementService.Verify(s => s.CalibrateZeroAsync(), Times.Once);
            Assert.That(_viewModel.IsDeviceCalibrated, Is.False);
            Assert.That(_viewModel.LogText.Contains(CheckConnectionCA), Is.True);
        }

        // --- Тесты для SaveResultsCommand ---

        [Test]
        public async Task SaveResultsCommand_Execute_SavesResultsAndLogs()
        {
            // Arrange
            string testSn = "TESTSN";
            _viewModel.ExecuteApplySerialNumber(testSn);

            // Убедимся, что _currentDevice содержит ВСЕ необходимые измерения,
            // чтобы CanExecuteSaveResults вернул true и AreAllStatusesRepresentedInMeasurements() вернул true.
            foreach (var status in MeasurementStatusService.Instance.AllMeasurementButtonStatuses)
            {
                _viewModel._currentDevice.AddMeasurement(new Measurement { Location = status.Location, IsValid = true });
            }

            // Настройка моков для успешного сохранения
            // Мокируем SaveDeviceDataToJsonAsync так, чтобы он возвращал true и вызывал StatusMessage
            _mockFileService.Setup(f => f.SaveDeviceDataToJsonAsync(
                It.IsAny<DeviceUnderTest>()
            ))
            .ReturnsAsync((DeviceUnderTest device) =>
            {
                // Имитируем вызов StatusMessage
                _mockFileService.Raise(f => f.StatusMessage += null, _mockFileService.Object,
                    _mockLocalizationService.Object.GetString(ResultsForSN, device.SerialNumber) + $" {SavedToJSON}: mock/path");
                return true;
            });

            // Мокируем SaveMeasurementToCsvAsync, чтобы он не мешал
            _mockFileService.Setup(f => f.SaveMeasurementToCsvAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()
            )).ReturnsAsync(true);

            // Act
            _viewModel.SaveResultsCommand.Execute(null); // Вызываем команду

            // Assert
            // 1. Проверяем, что SaveDeviceDataToJsonAsync был вызван.
            _mockFileService.Verify(f => f.SaveDeviceDataToJsonAsync(
                It.Is<DeviceUnderTest>(d => d.SerialNumber == testSn)
            ), Times.Once);

            // 2. Проверяем, что лог содержит сообщение об успешном сохранении из FileService.
            // Используем Contains, так как может быть дополнительная информация (дата, путь).
            // Добавим ожидание, чтобы асинхронное добавление в лог успело произойти
            await Task.Delay(100); // Небольшая задержка для UI-потока, чтобы обновить ObservableCollection

            // Проверяем, что лог содержит сообщение об успешном сохранении
            Assert.That(_viewModel.LogText, Does.Contain(_mockLocalizationService.Object.GetString(ResultsForSN, testSn)),
                        $"Log should contain the successful save message from FileService. Current logs: {_viewModel.LogText}");
            Assert.That(_viewModel.LogText, Does.Contain(SavedToJSON),
                        $"Log should contain the successful save message from FileService. Current logs: {_viewModel.LogText}");

            // Проверяем, что также есть сообщения "Current SN is: TESTSN" и "Saving..."
            Assert.That(_viewModel.LogText, Does.Contain(string.Format(_mockLocalizationService.Object.GetString(CurrentSN), testSn)),
                        "Log should contain 'Current SN is: TESTSN' message.");
            Assert.That(_viewModel.LogText, Does.Contain(_mockLocalizationService.Object.GetString(Saving)),
                        "Log should contain 'Saving...' message.");

            // 3. Проверяем, что диалоговое окно НЕ было показано при успешном сохранении
            _mockDialogService.Verify(d => d.ShowMessage(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task SaveResultsCommand_Execute_HandlesSaveError()
        {
            // Arrange
            string testSnErr = "TESTSNERR";
            _viewModel.ExecuteApplySerialNumber(testSnErr);

            // Убедимся, что _currentDevice содержит ВСЕ необходимые измерения,
            // чтобы CanExecuteSaveResults вернул true и AreAllStatusesRepresentedInMeasurements() вернул true.
            // Это предотвратит ShowQuestion и обеспечит вызов SaveDeviceDataToJsonAsync.
            foreach (var status in MeasurementStatusService.Instance.AllMeasurementButtonStatuses)
            {
                _viewModel._currentDevice.AddMeasurement(new Measurement { Location = status.Location, IsValid = true });
            }

            // Мокируем SaveDeviceDataToJsonAsync так, чтобы он ВЫБРОСИЛ ИСКЛЮЧЕНИЕ.
            // Это заставит код войти в блок catch в ExecuteSaveResultsAsync.
            var commandFinishedEvent = new ManualResetEventSlim(false);

            _mockFileService.Setup(f => f.SaveDeviceDataToJsonAsync(It.IsAny<DeviceUnderTest>()))
                            .Returns(async (DeviceUnderTest device) =>
                            {
                                await Task.Yield(); // Имитируем асинхронность
                                commandFinishedEvent.Set(); // Сигнализируем о завершении после "бросания" исключения
                                throw new IOException("Simulated save error for test."); // Бросаем исключение
                            });

            // Act
            _viewModel.SaveResultsCommand.Execute(null); // Вызываем команду

            // Ждем, пока команда завершит свою асинхронную часть
            bool finished = commandFinishedEvent.Wait(TimeSpan.FromSeconds(5));
            Assert.That(finished, Is.True, "Command did not complete its async operation within the timeout.");

            // Assert
            // 1. Проверяем, что _mockFileService.SaveDeviceDataToJsonAsync был вызван ровно один раз.
            _mockFileService.Verify(f => f.SaveDeviceDataToJsonAsync(It.IsAny<DeviceUnderTest>()), Times.Once);

            // 2. Проверяем, что в лог добавлено сообщение "Saving...".
            Assert.That(_viewModel.LogText, Does.Contain(_mockLocalizationService.Object.GetString(Saving)));

            // 3. Проверяем, что в лог добавлено сообщение об ОШИБКЕ (из catch блока).
            // Ожидаем увидеть "ErrUnexpected" и сообщение исключения.
            Assert.That(_viewModel.LogText, Does.Contain(_mockLocalizationService.Object.GetString(ErrUnexpected)), "Log should contain the unexpected error prefix.");
            // Проверка на полное сообщение из лога, включая текст исключения
            Assert.That(_viewModel.LogText, Does.Contain("Simulated save error for test."), "Log should contain the simulated exception message.");

            // 4. Проверяем, что ShowMessage для ошибки сохранения был вызван с правильными параметрами.
            _mockDialogService.Verify(d => d.ShowMessage(
                It.Is<string>(msg => msg.Contains(_mockLocalizationService.Object.GetString(SaveJSONErrForSN))), // Проверяем, что сообщение диалога содержит SaveJSONErrForSN
                It.Is<string>(title => title == _mockLocalizationService.Object.GetString(Err)) // Проверяем заголовок диалога
            ), Times.Once);
            // Проверяем, что сообщение диалога также содержит текст исключения
            _mockDialogService.Verify(d => d.ShowMessage(
                It.Is<string>(msg => msg.Contains("Simulated save error for test.")),
                It.IsAny<string>()
            ), Times.Once);
        }

        [Test]
        public void SaveResultsCommand_CanExecute_ReturnsFalse_WhenEmptyDevice()
        {
            // Arrange
            _viewModel._currentDevice = null; // Устройство null

            // Act
            bool canExecute = _viewModel.SaveResultsCommand.CanExecute(null);

            // Assert
            Assert.That(canExecute, Is.False);
            // Никакие сервисы не должны быть вызваны, если команда неактивна.
            _mockFileService.Verify(f => f.SaveDeviceDataToJsonAsync(It.IsAny<DeviceUnderTest>()), Times.Never);
            _mockDialogService.Verify(d => d.ShowMessage(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void SaveResultsCommand_CanExecute_ReturnsFalse_WhenDeviceHasNoMeasurements()
        {
            // Arrange
            _viewModel.ExecuteApplySerialNumber("TestSN"); // Создаем устройство, но без измерений
            Assert.That(_viewModel._currentDevice.Measurements, Is.Empty);

            // Act
            bool canExecute = _viewModel.SaveResultsCommand.CanExecute(null);

            // Assert
            Assert.That(canExecute, Is.False);
            _mockFileService.Verify(f => f.SaveDeviceDataToJsonAsync(It.IsAny<DeviceUnderTest>()), Times.Never);
            _mockDialogService.Verify(d => d.ShowMessage(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task SaveResultsCommand_Execute_HandlesSaveCanceled() // Метод должен быть async
        {
            // Arrange
            _viewModel.ExecuteApplySerialNumber("TESTSNCANCEL"); // Убедимся, что SN применен

            // _viewModel._currentDevice будет содержать только одно измерение,
            // что приведет к тому, что _viewModel.AreAllStatusesRepresentedInMeasurements() вернет false.
            // Это активирует диалог подтверждения.
            _viewModel._currentDevice.AddMeasurement(new Measurement { Location = "Test", IsValid = true });

            // Мокируем, что пользователь отменил сохранение (нажал "Нет" в диалоге подтверждения)
            _mockDialogService.Setup(d => d.ShowQuestion(
                It.Is<string>(msg => msg.Contains(SavingNotFullWarning)),
                It.Is<string>(title => title == Warning)
            )).Returns(false);

            // Act
            _viewModel.SaveResultsCommand.Execute(null);
            await Task.Delay(100); // Даем небольшую задержку для завершения асинхронной операции

            // Assert
            // Проверяем, что асинхронный метод сохранения НЕ вызывался
            _mockFileService.Verify(f => f.SaveDeviceDataToJsonAsync(It.IsAny<DeviceUnderTest>()), Times.Never);
            _mockDialogService.Verify(d => d.ShowQuestion(
                It.Is<string>(msg => msg.Contains(SavingNotFullWarning)),
                It.Is<string>(title => title == Warning)
            ), Times.Once);
            Assert.That(_viewModel.LogText.Contains(SaveCanceled), Is.True);
        }

        // --- Тесты для ClearFieldsCommand ---
        [Test]
        public void ClearFieldsCommand_Execute_ClearsFieldsAndLogs()
        {
            // Arrange
            _viewModel.AddLogMessage("Some log message");
            _viewModel.SerialNumber = "123";
            _viewModel.MeasurementTime = 5;
            _viewModel.IsDeviceConnected = true;
            _viewModel.IsDeviceCalibrated = true;
            _viewModel.IsSerialNumberConfirmed = true;
            _viewModel.IsMeasurementButtonsEnabled = true;

            _mockDialogService.Setup(d => d.ShowQuestion(
                It.Is<string>(msg => msg.Contains(CleanFieldWarning)),
                It.Is<string>(title => title == Warning)
            )).Returns(true);

            // Act
            _viewModel.ClearFieldsCommand.Execute(null);

            // Assert
            Assert.That(_viewModel.SerialNumber, Is.Empty);
            Assert.That(_viewModel.MeasurementTime, Is.EqualTo(2));
            Assert.That(_viewModel.IsDeviceConnected, Is.True);
            Assert.That(_viewModel.IsDeviceCalibrated, Is.True);
            Assert.That(_viewModel.IsSerialNumberConfirmed, Is.False);
            Assert.That(_viewModel.IsMeasurementButtonsEnabled, Is.False);
            Assert.That(_viewModel.LogText.Contains(ClearFieldsDone), Is.True);
            _mockDialogService.Verify(d => d.ShowQuestion(
                It.Is<string>(msg => msg.Contains(CleanFieldWarning)),
                It.Is<string>(title => title == Warning)
            ), Times.Once);
        }

        [Test]
        public void ClearFieldsCommand_Execute_DoesNotClearIfCanceled()
        {
            // Arrange
            _viewModel.AddLogMessage("Some log message");
            _viewModel.SerialNumber = "123";
            _viewModel.MeasurementTime = 5;
            _viewModel.IsDeviceConnected = true;
            _viewModel.IsDeviceCalibrated = true;
            _viewModel.IsSerialNumberConfirmed = true;
            _viewModel.IsMeasurementButtonsEnabled = true;

            _mockDialogService.Setup(d => d.ShowQuestion(
                It.Is<string>(msg => msg.Contains(CleanFieldWarning)),
                It.Is<string>(title => title == Warning)
            )).Returns(false);

            // Act
            _viewModel.ClearFieldsCommand.Execute(null);

            // Assert
            Assert.That(_viewModel.SerialNumber, Is.EqualTo("123"));
            Assert.That(_viewModel.MeasurementTime, Is.EqualTo(5));
            Assert.That(_viewModel.IsDeviceConnected, Is.True);
            Assert.That(_viewModel.IsDeviceCalibrated, Is.True);
            Assert.That(_viewModel.IsSerialNumberConfirmed, Is.True);
            Assert.That(_viewModel.IsMeasurementButtonsEnabled, Is.True);
            Assert.That(_viewModel.LogText.Contains(ClearFieldsDone), Is.False);
            _mockDialogService.Verify(d => d.ShowQuestion(
                It.Is<string>(msg => msg.Contains(CleanFieldWarning)),
                It.Is<string>(title => title == Warning)
            ), Times.Once);
        }

        // --- Тесты для ClearLogCommand ---
        [Test]
        public void ExecuteClearLog_ClearsLogs()
        {
            // Arrange
            _viewModel.AddLogMessage("Log entry 1");
            _viewModel.AddLogMessage("Log entry 2");

            // Act
            _viewModel.ExecuteClearLog();

            // Assert
            Assert.That(_viewModel.LogText, Is.Empty);
        }

        // --- Тесты для ApplySerialNumberCommand (применяется через ExecuteApplySerialNumber) ---
        [Test]
        public void ApplySerialNumberCommand_Execute_SetsSerialNumberAndConfirms()
        {
            // Arrange
            string testSn = "VALIDSNXYZ";

            // Убраны моки для CreateFolderForSN и GetOrCreateWorkFolder,
            // так как этих методов нет в предоставленном коде IFileService/FileService.
            // Если ViewModel действительно вызывает эти методы, то это указывает на несоответствие
            // между ViewModel и IFileService.

            // Act
            _viewModel.ExecuteApplySerialNumber(testSn);

            // Assert
            Assert.That(_viewModel.SerialNumber, Is.EqualTo(testSn));
            Assert.That(_viewModel.IsSerialNumberConfirmed, Is.True);
            // Удалена верификация для CreateFolderForSN, так как метода нет
            Assert.That(_viewModel.LogText.Contains($"{CurrentSN}: {testSn}"), Is.True);
        }

        [Test]
        public void ApplySerialNumberCommand_Execute_HandlesInvalidSerialNumberFormat()
        {
            // Arrange
            string invalidSn = "SN!@#";

            // Act
            _viewModel.ExecuteApplySerialNumber(invalidSn);

            // Assert
            Assert.That(_viewModel.SerialNumber, Is.Not.EqualTo(invalidSn));
            _mockDialogService.Verify(d => d.ShowMessage(
                It.Is<string>(msg => msg.Contains(IncorrectFormatForSNErr)),
                It.Is<string>(title => title == Err)
            ), Times.Once);
            Assert.That(_viewModel.IsSerialNumberConfirmed, Is.False);
        }

        // --- Тесты для ApplyMeasurementTimeCommand ---
        [Test]
        public void ApplyMeasurementTimeCommand_Execute_AppliesValidTime()
        {
            // Arrange
            // Команда ожидает строковый параметр, поэтому устанавливаем его здесь.
            string expectedTime = "10"; // <--- Это было ключевое изменение
            _viewModel._currentDevice = new DeviceUnderTest("123"); // Убедимся, что _currentDevice не null

            // Act
            // Передаем строковое значение времени в качестве параметра команды
            _viewModel.ApplyMeasurementTimeCommand.Execute(expectedTime); // <--- Это было ключевое изменение

            // Assert
            // Проверяем, что свойство MeasurementTime было обновлено командой до 10 (int).
            Assert.That(_viewModel.MeasurementTime, Is.EqualTo(10));

            // С учетом вашей реализации AddLogMessage и мока ILocalizationService (который возвращает ключи ресурсов),
            // ожидаемая строка в логе будет "CurrentMeasurementTime: 10 Seconds".
            // Проверяем наличие всей ожидаемой строки в логе.
            Assert.That(_viewModel.LogText.Contains($"{CurrentMeasurementTime}: 10 {Seconds}"), Is.True);

            // Можно также проверить, что диалог НЕ был показан для этого случая
            _mockDialogService.Verify(d => d.ShowMessage(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        // Дополнительные тесты для обработки некорректного ввода, основанные на вашей реализации:
        [Test]
        public void ApplyMeasurementTimeCommand_Execute_EmptyInput()
        {
            // Arrange
            string emptyInput = "";

            // Act
            _viewModel.ApplyMeasurementTimeCommand.Execute(emptyInput);

            // Assert
            _mockDialogService.Verify(d => d.ShowMessage(
                It.Is<string>(msg => msg.Contains(IncorrectMeasTimeFormat)),
                It.Is<string>(title => title.Contains(Err))
            ), Times.Once);
            // Проверяем, что лог не содержит сообщения об успешном применении времени
            Assert.That(_viewModel.LogText.Contains($"{CurrentMeasurementTime}:"), Is.False);
        }

        [Test]
        public void ApplyMeasurementTimeCommand_Execute_HandlesNegativeInput()
        {
            // Arrange
            string negativeInput = "-5";

            // Act
            _viewModel.ApplyMeasurementTimeCommand.Execute(negativeInput);

            // Assert
            _mockDialogService.Verify(d => d.ShowMessage(
                It.Is<string>(msg => msg.Contains(IncorrectMeasTimeFormat)),
                It.Is<string>(title => title.Contains(Err))
            ), Times.Once);
            Assert.That(_viewModel.LogText.Contains(CurrentMeasurementTime), Is.False);
        }

        [Test]
        public void ApplyMeasurementTimeCommand_Execute_HandlesZeroInput()
        {
            // Arrange
            string zeroInput = "0";

            // Act
            _viewModel.ApplyMeasurementTimeCommand.Execute(zeroInput);

            // Assert
            _mockDialogService.Verify(d => d.ShowMessage(
                It.Is<string>(msg => msg.Contains(IncorrectMeasTimeFormat)),
                It.Is<string>(title => title.Contains(Err))
            ), Times.Once);
            Assert.That(_viewModel.LogText.Contains(CurrentMeasurementTime), Is.False);
        }

        [Test]
        public void ApplyMeasurementTimeCommand_Execute_HandlesNonNumericInput()
        {
            // Arrange
            string nonNumericInput = "abc";

            // Act
            _viewModel.ApplyMeasurementTimeCommand.Execute(nonNumericInput);

            // Assert
            _mockDialogService.Verify(d => d.ShowMessage(
                It.Is<string>(msg => msg.Contains(IncorrectMeasTimeFormat)),
                It.Is<string>(title => title.Contains(Err))
            ), Times.Once);
            Assert.That(_viewModel.LogText.Contains(CurrentMeasurementTime), Is.False);
        }

        // --- Тесты для MeasureCommand ---

        [Test]
        public void MeasureCommand_Execute_DoesNothing_WhenNoConnection()
        {
            // Arrange
            _viewModel.IsDeviceConnected = false; // Ключевое условие для CanExecute: false
            _viewModel.IsDeviceCalibrated = true;
            _viewModel.SerialNumber = "SomeSN";
            _viewModel.IsSerialNumberConfirmed = true; // Убедимся, что SN подтвержден, если IsSerialNumberConfirmed используется в CanExecute
            _viewModel.MeasurementTime = 10; // Убедимся, что другие условия CanExecute выполнены

            // Act
            // Выполняем команду. Так как CanExecuteMeasure вернет false,
            // метод ExecuteMeasure не должен быть вызван.
            _viewModel.MeasureCommand.Execute(null);

            // Assert
            // 1. Проверяем, что _colorMeasurementService.MeasureAsync НИКОГДА не был вызван.
            _mockColorMeasurementService.Verify(s => s.MeasureAsync(It.IsAny<int>()), Times.Never);

            // 2. Проверяем, что _dialogService.ShowMessage НИКОГДА не был вызван.
            _mockDialogService.Verify(d => d.ShowMessage(It.IsAny<string>(), It.IsAny<string>()), Times.Never);

            // 3. Проверяем, что лог НЕ содержит сообщений, связанных с выполнением этой команды.
            Assert.That(_viewModel.LogText.Contains("MeasureWihoutConnectionError"), Is.False);
            Assert.That(_viewModel.LogText.Contains("Measuring"), Is.False); // Также, чтобы не было сообщений об успешном измерении
        }

        [Test]
        public void MeasureCommand_Execute_DoesNothing_WhenNotCalibrated()
        {
            // Arrange
            _viewModel.IsDeviceConnected = true;
            _viewModel.IsDeviceCalibrated = false; // Ключевое условие для CanExecute: false
            _viewModel.SerialNumber = "SomeSN";
            _viewModel.IsSerialNumberConfirmed = true;
            _viewModel.MeasurementTime = 10;

            // Act
            _viewModel.MeasureCommand.Execute(null);

            // Assert
            // 1. Проверяем, что _colorMeasurementService.MeasureAsync НИКОГДА не был вызван.
            _mockColorMeasurementService.Verify(s => s.MeasureAsync(It.IsAny<int>()), Times.Never);

            // 2. Проверяем, что _dialogService.ShowMessage НИКОГДА не был вызван.
            _mockDialogService.Verify(d => d.ShowMessage(It.IsAny<string>(), It.IsAny<string>()), Times.Never);

            // 3. Проверяем, что лог НЕ содержит сообщений, связанных с выполнением этой команды.
            Assert.That(_viewModel.LogText.Contains("MakeZeroCalibration"), Is.False); // Если раньше здесь был этот лог
            Assert.That(_viewModel.LogText.Contains("Measuring"), Is.False);
        }

        [Test]
        public void MeasureCommand_Execute_DoesNothing_WhenNoSerialNumber()
        {
            // Arrange
            _viewModel.IsDeviceConnected = true;
            _viewModel.IsDeviceCalibrated = true;
            _viewModel.SerialNumber = ""; // Ключевое условие для CanExecute: false
            _viewModel.IsSerialNumberConfirmed = false; // Убедимся, что это свойство также отражает пустой SN
            _viewModel.MeasurementTime = 10;

            // Act
            _viewModel.MeasureCommand.Execute(null);

            // Assert
            // 1. Проверяем, что _colorMeasurementService.MeasureAsync НИКОГДА не был вызван.
            _mockColorMeasurementService.Verify(s => s.MeasureAsync(It.IsAny<int>()), Times.Never);

            // 2. Проверяем, что _dialogService.ShowMessage НИКОГДА не был вызван.
            _mockDialogService.Verify(d => d.ShowMessage(It.IsAny<string>(), It.IsAny<string>()), Times.Never);

            // 3. Проверяем, что лог НЕ содержит сообщений, связанных с выполнением этой команды.
            Assert.That(_viewModel.LogText.Contains("FillSN"), Is.False); // Если раньше здесь был этот лог
            Assert.That(_viewModel.LogText.Contains("Measuring"), Is.False);
        }

        [Test]
        public async Task MeasureCommand_Execute_PerformsMeasurementAndLogs()
        {
            // Arrange
            // Устанавливаем все условия для CanExecute в true, чтобы команда МОГЛА быть выполнена.
            _viewModel.IsDeviceConnected = true;
            _viewModel.IsDeviceCalibrated = true;
            _viewModel.SerialNumber = "TESTSN";
            _viewModel.MeasurementTime = 1;
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

            // Убедимся, что серийный номер подтвержден, так как это часть логики CanExecute.
            // Если IsSerialNumberConfirmed устанавливается через метод, вызываем его.
            // Если это свойство, то просто устанавливаем его в true, если оно не связано с SerialNumber напрямую.
            // Лучше всего, чтобы SerialNumber устанавливал IsSerialNumberConfirmed при валидации.
            // Если ExecuteApplySerialNumber устанавливает IsSerialNumberConfirmed, то нужно вызвать его.
            // _viewModel.ExecuteApplySerialNumber(_viewModel.SerialNumber); // Если этот метод устанавливает IsSerialNumberConfirmed

            // Предполагаем, что IsSerialNumberConfirmed зависит от SerialNumber != "".
            // Если это не так, и IsSerialNumberConfirmed - отдельное свойство, то установим его здесь:
            _viewModel.IsSerialNumberConfirmed = true;

            // Настраиваем мок ColorMeasurementService для возврата тестовых данных
            _mockColorMeasurementService.Setup(s => s.MeasureAsync(It.IsAny<int>()))
                                         .Returns(Task.FromResult(new Measurement { Location = MeasurementStatusService.CenterLocationName, IsValid = true, x = 0.331, y = 0.322, Lv = 200, T = 6000 }));

            // Act
            _viewModel.MeasureCommand.Execute(MeasurementStatusService.CenterLocationName); // Передаем параметр (например, Location)
            await Task.Delay(100); // Даем время для завершения асинхронной операции

            // Assert
            // 1. Проверяем, что MeasureAsync был вызван ровно один раз.
            _mockColorMeasurementService.Verify(s => s.MeasureAsync(It.IsAny<int>()), Times.Once);

            // 2. Проверяем, что никаких диалоговых окон об ошибке не было показано.
            _mockDialogService.Verify(d => d.ShowMessage(It.IsAny<string>(), It.IsAny<string>()), Times.Never);

            // 3. Проверяем, что лог содержит сообщение об успешном измерении.
            Assert.That(_viewModel.LogText, Does.Contain($"{Result} '{MeasurementStatusService.CenterLocationName}': x=0.331, y=0.322, Lv=200.0, T=6000"));

            // 4. Проверяем, что измерения были добавлены в CurrentDevice.
            Assert.That(_viewModel._currentDevice.Measurements.Count, Is.GreaterThan(0));
            Assert.That(_viewModel._currentDevice.Measurements.Any(m => m.Location == MeasurementStatusService.CenterLocationName), Is.True);
        }

        [Test]
        public async Task MeasureCommand_Execute_HandlesMeasurementFailure()
        {
            // Arrange
            _viewModel.IsDeviceConnected = true;
            _viewModel.IsDeviceCalibrated = true;
            _viewModel.SerialNumber = "FAILSN";
            _viewModel.MeasurementTime = 1;
            _viewModel.ExecuteApplySerialNumber(_viewModel.SerialNumber); // Устанавливаем SN и подтверждаем

            _mockColorMeasurementService.Setup(s => s.MeasureAsync(It.IsAny<int>()))
                                        .Returns(Task.FromResult((Measurement)null));

            // Act
            _viewModel.MeasureCommand.Execute(MeasurementStatusService.CenterLocationName);
            await Task.Delay(100); // Ждем завершения асинхронной операции

            // Assert
            _mockColorMeasurementService.Verify(s => s.MeasureAsync(It.IsAny<int>()), Times.Once);
            Assert.That(_viewModel.LogText.Contains(ColorServiceErr), Is.True);
            Assert.That(_viewModel._currentDevice?.Measurements.Count, Is.EqualTo(0));
        }

        // --- Тесты для SwitchLanguageCommand ---
        [Test]
        public void SwitchLanguageCommand_Execute_ChangesLanguageAndLogs()
        {
            // Arrange
            string langCode = "en-US";
            // Мокируем вызов SetLanguage, так как это void-метод.
            _mockLocalizationService.Setup(l => l.SetLanguage(langCode));

            // Act
            _viewModel.SwitchLanguageCommand.Execute(langCode);

            // Assert
            // Проверяем, что метод SetLanguage был вызван один раз.
            _mockLocalizationService.Verify(l => l.SetLanguage(langCode), Times.Once);
            // Для успешного переключения языка обычно не ожидается сообщение в логе (если только ViewModel не логирует успех)
            // Если ViewModel логирует успех, добавьте: Assert.That(_viewModel.LogText.Contains("Language changed to en-US"), Is.True);
        }

        [Test]
        public void SwitchLanguageCommand_Execute_HandlesLanguageSwitchFailure()
        {
            // Arrange
            string langCode = "invalidcode";

            // Мокируем SetLanguage так, чтобы он ВЫБРОСИЛ ИСКЛЮЧЕНИЕ
            _mockLocalizationService
                .Setup(l => l.SetLanguage(langCode))
                .Throws(new InvalidOperationException("Simulated language switch error.")); // Имитируем выброс исключения

            // Act
            _viewModel.SwitchLanguageCommand.Execute(langCode);

            // Assert
            // Проверяем, что метод SetLanguage был вызван один раз.
            _mockLocalizationService.Verify(l => l.SetLanguage(langCode), Times.Once);

            // Проверяем, что DialogService показал сообщение об ошибке, содержащее ErrMsgLangSwitchFailed.
            _mockDialogService.Verify(d => d.ShowMessage(
                It.Is<string>(msg => msg.Contains(_mockLocalizationService.Object.GetString(ErrMsgLangSwitchFailed))),
                It.Is<string>(title => title == _mockLocalizationService.Object.GetString(Err))
            ), Times.Once);
        }

        // --- Тесты для NewDeviceUnderTestCommand ---
        [Test]
        public void NewDeviceUnderTestCommand_Execute_CreatesNewDevice()
        {
            // Arrange
            _viewModel.ExecuteApplySerialNumber("OLDSN"); // Создаем старое устройство
            Assert.That(_viewModel._currentDevice, Is.Not.Null);
            var oldDevice = _viewModel._currentDevice;
            oldDevice.AddMeasurement(new Models.Measurement { Location = MeasurementStatusService.CenterLocationName, IsValid = true, x = 0.331, y = 0.322, Lv = 200, T = 6000 });

            // ДОБАВЛЕНО: Мокируем ShowQuestion, чтобы он вернул true (подтверждаем очистку)
            _mockDialogService.Setup(d => d.ShowQuestion(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

            _mockFileService.Setup(d => d.SaveDeviceDataToJsonAsync(oldDevice).Result).Returns(true); // Мокируем успешное сохранение старого устройства

            // Act
            _viewModel.NewDeviceUnderTestCommand.Execute(null);

            // Assert
            Assert.That(_viewModel._currentDevice, Is.Null);
            Assert.That(_viewModel.SerialNumber, Is.Empty); // SN должен быть сброшен
            Assert.That(_viewModel.IsSerialNumberConfirmed, Is.False); // SN не подтвержден
        }

        // --- Тесты для LaunchExternalProgramCommand ---
        [Test]
        public void LaunchExternalProgramCommand_Execute_LaunchesProgram()
        {
            // Arrange
            string programPath = "notepad.exe";
            // Теперь мокируем новый метод RunExternalProgram в IFileService
            _mockFileService.Setup(f => f.RunExternalProgram(It.IsAny<string>())).Returns(true);

            // Act
            _viewModel.LaunchExternalProgramCommand.Execute(programPath);

            // Assert
            // Теперь проверяем, что ViewModel вызвал RunExternalProgram у IFileService
            _mockFileService.Verify(f => f.RunExternalProgram(It.Is<string>(p => p.Contains(programPath))), Times.Once);
            // Проверяем, что в логе было сообщение об успехе, если ViewModel его добавляет
            // Assert.That(_viewModel.LogText.Contains($"Launched program: {programPath}"), Is.True);
        }

        [Test]
        public void LaunchExternalProgramCommand_Execute_HandlesProgramNotFound()
        {
            // Arrange
            // Передаем только имя программы, как теперь ожидает ViewModel
            string programName = "nonexistent.exe";

            // Мокируем, что RunExternalProgram будет вызван с этим именем и вернет false.
            // Теперь Moq будет ожидать именно "nonexistent.exe", а не полный путь.
            _mockFileService.Setup(f => f.RunExternalProgram(programName)).Returns(false);

            // Act
            // Передаем название программы
            _viewModel.LaunchExternalProgramCommand.Execute(programName);

            // Assert
            // Проверяем, что ViewModel вызвал RunExternalProgram у IFileService с ПРАВИЛЬНЫМ НАЗВАНИЕМ ПРОГРАММЫ.
            _mockFileService.Verify(f => f.RunExternalProgram(programName), Times.Once);

            // Проверяем, что DialogService показал сообщение об ошибке.
            // Используем мок ILocalizationService для получения ожидаемых строк.
            // (Как мы выяснили ранее, если ресурсы возвращают локализованные строки, то Verify должен ожидать их).
            _mockDialogService.Verify(d => d.ShowMessage(
                It.Is<string>(msg => msg == _mockLocalizationService.Object.GetString(RunExternalAppNotFoundErr)),
                It.Is<string>(title => title == _mockLocalizationService.Object.GetString(Err))
            ), Times.Once);

            // Проверяем, что в лог было добавлено сообщение об ошибке.
            // Если вы добавили имя программы в лог, то ожидаем его здесь:
            Assert.That(_viewModel.LogText.Contains($"{_mockLocalizationService.Object.GetString(RunExternalAppNotFoundErr)}: {programName}"), Is.True);
            // Если просто сообщение об ошибке без имени программы:
            // Assert.That(_viewModel.LogText.Contains(_mockLocalizationService.Object.GetString(RunExternalAppNotFoundErr)), Is.True);
        }

        [Test]
        public void LaunchExternalProgramCommand_Execute_HandlesUnexpectedError()
        {
            // Arrange
            string programPath = "some_program.exe";
            string errorMessage = "Access denied.";
            // Мокируем, что RunExternalProgram выбросит исключение или вернет false
            _mockFileService.Setup(f => f.RunExternalProgram(It.IsAny<string>())).Throws(new Exception(errorMessage));

            // Act
            _viewModel.LaunchExternalProgramCommand.Execute(programPath);

            // Assert
            _mockFileService.Verify(f => f.RunExternalProgram(It.Is<string>(p => p.Contains(programPath))), Times.Once);
            _mockDialogService.Verify(d => d.ShowMessage(
                It.Is<string>(msg => msg.Contains(RunExternalAppUnexpectedErr) && msg.Contains(errorMessage)),
                It.Is<string>(title => title == Err)
            ), Times.Once);
            Assert.That(_viewModel.LogText.Contains(RunExternalAppUnexpectedErr), Is.True);
            Assert.That(_viewModel.LogText.Contains(errorMessage), Is.True);
        }

        // --- Тесты для Dispose ---
        [Test]
        public void Dispose_CleansUpResources()
        {
            // Arrange
            _viewModel.AddLogMessage("Test log");
            _viewModel.SerialNumber = "DisposeTest";
            _viewModel.IsDeviceConnected = true;
            _viewModel.IsDeviceCalibrated = true;
            _viewModel.IsSerialNumberConfirmed = true;
            _viewModel.IsMeasurementButtonsEnabled = true;

            // Act
            _viewModel.Dispose();

            // Assert
            Assert.That(_viewModel.LogText, Is.Not.Empty);
            Assert.That(_viewModel.LogText.Contains(ViewModelCleared), Is.True);
            Assert.That(_viewModel.SerialNumber, Is.Empty);
            Assert.That(_viewModel.IsDeviceConnected, Is.False);
            Assert.That(_viewModel.IsDeviceCalibrated, Is.False);
            Assert.That(_viewModel.IsSerialNumberConfirmed, Is.False);
            Assert.That(_viewModel.IsMeasurementButtonsEnabled, Is.False);

            _mockColorMeasurementService.As<IDisposable>().Verify(d => d.Dispose(), Times.Once);
            _mockFileService.As<IDisposable>().Verify(d => d.Dispose(), Times.Once);
        }

        // --- Тесты для CanExecute методов команд (примеры) ---
        [Test]
        public void ZeroCalibrationCommand_CanExecute_ReturnsTrue_WhenConnected()
        {
            _viewModel.IsDeviceConnected = true;
            Assert.That(_viewModel.ZeroCalibrationCommand.CanExecute(null), Is.True);
        }

        [Test]
        public void MeasureCommand_CanExecute_ReturnsTrue_WhenConnectedCalibratedAndSNPresentAndConfirmed()
        {
            _viewModel.IsDeviceConnected = true;
            _viewModel.IsDeviceCalibrated = true;
            _viewModel.SerialNumber = "SomeSN";
            _viewModel.IsSerialNumberConfirmed = true;
            // Проверяем, что ExecuteMeasureAsync не выполняется в данный момент.
            // Это можно сделать, например, через флаг IsMeasuring, если бы он был,
            // или косвенно, если кнопки блокируются через IsMeasurementButtonsEnabled
            // Если MeasureCommand.CanExecute зависит от _viewModel.IsMeasurementButtonsEnabled, то
            // Assert.That(_viewModel.IsMeasurementButtonsEnabled, Is.True);
            Assert.That(_viewModel.MeasureCommand.CanExecute(null), Is.True);
        }

        [Test]
        public void MeasureCommand_CanExecute_ReturnsFalse_WhenNotConnected()
        {
            _viewModel.IsDeviceConnected = false;
            _viewModel.IsDeviceCalibrated = true;
            _viewModel.SerialNumber = "SomeSN";
            _viewModel.IsSerialNumberConfirmed = true;
            Assert.That(_viewModel.MeasureCommand.CanExecute(null), Is.False);
        }

        [Test]
        public void MeasureCommand_CanExecute_ReturnsFalse_WhenNotCalibrated()
        {
            _viewModel.IsDeviceConnected = true;
            _viewModel.IsDeviceCalibrated = false;
            _viewModel.SerialNumber = "SomeSN";
            _viewModel.IsSerialNumberConfirmed = true;
            Assert.That(_viewModel.MeasureCommand.CanExecute(null), Is.False);
        }

        [Test]
        public void MeasureCommand_CanExecute_ReturnsFalse_WhenSerialNumberIsEmpty()
        {
            _viewModel.IsDeviceConnected = true;
            _viewModel.IsDeviceCalibrated = true;
            _viewModel.SerialNumber = "";
            _viewModel.IsSerialNumberConfirmed = false;
            Assert.That(_viewModel.MeasureCommand.CanExecute(null), Is.False);
        }

        // --- Вспомогательные тесты ---
        [Test]
        public void AreAllStatusesRepresentedInMeasurements_ReturnsTrue_WhenAllPresent()
        {
            _viewModel.ExecuteApplySerialNumber("SnFull");
            Assert.That(_viewModel._currentDevice, Is.Not.Null);

            var allExpectedLocations = MeasurementStatusService.Instance.AllMeasurementButtonStatuses.Select(s => s.Location).ToList();

            foreach (var location in allExpectedLocations)
            {
                _viewModel._currentDevice.AddMeasurement(new Measurement { Location = location, IsValid = true });
            }

            bool result = _viewModel.AreAllStatusesRepresentedInMeasurements();

            Assert.That(result, Is.True);
        }

        [Test]
        public void AreAllStatusesRepresentedInMeasurements_ReturnsFalse_WhenNotAllPresent()
        {
            _viewModel.ExecuteApplySerialNumber("SnPartial");
            Assert.That(_viewModel._currentDevice, Is.Not.Null);

            _viewModel._currentDevice.AddMeasurement(new Measurement { Location = "TopLeft", IsValid = true });

            bool result = _viewModel.AreAllStatusesRepresentedInMeasurements();

            Assert.That(result, Is.False);
        }

        [Test]
        public void AreAllStatusesRepresentedInMeasurements_ReturnsFalse_WhenNoDevice()
        {
            _viewModel._currentDevice = null;
            bool result = _viewModel.AreAllStatusesRepresentedInMeasurements();

            Assert.That(result, Is.False);
        }

        [Test]
        public void AreAllStatusesRepresentedInMeasurements_ReturnsFalse_WhenDeviceHasNoMeasurements()
        {
            _viewModel.ExecuteApplySerialNumber("SnEmpty");
            Assert.That(_viewModel._currentDevice, Is.Not.Null);
            Assert.That(_viewModel._currentDevice.Measurements, Is.Empty);

            bool result = _viewModel.AreAllStatusesRepresentedInMeasurements();

            Assert.That(result, Is.False);
        }
    }
}