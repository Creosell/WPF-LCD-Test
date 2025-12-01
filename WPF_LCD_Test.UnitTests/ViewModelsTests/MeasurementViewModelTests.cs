using Moq;
using NUnit.Framework;
using System;
using System.IO;
using System.Globalization;
using WPF_LCD_Test.Interfaces;
using WPF_LCD_Test.Models;
using WPF_LCD_Test.Services; // Доступ к MeasurementStatusService
using WPF_LCD_Test.ViewModels;

// Для доступа к ключам ресурсов (RunExternalAppNotFoundErr и др.)
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
        private Mock<IUploadService> _mockUploadService;
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
                _mockUploadService.Object
            );
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
            Assert.That(_viewModel._currentDevice, Is.Not.Null);

            // ИСПРАВЛЕНИЕ: Берем список точек из Singleton сервиса, так как во ViewModel нет публичного свойства
            var allStatuses = MeasurementStatusService.Instance.AllMeasurementButtonStatuses;

            foreach (var status in allStatuses)
                {
                // Добавляем измерение для каждой требуемой точки
                _viewModel._currentDevice.AddMeasurement(new Measurement { Location = status.Location, IsValid = true });
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
            Assert.That(_viewModel._currentDevice, Is.Not.Null);

            // Добавляем только одно измерение
            _viewModel._currentDevice.AddMeasurement(new Measurement { Location = "TopLeft", IsValid = true });

            // Act
            bool result = _viewModel.AreAllStatusesRepresentedInMeasurements();

            // Assert
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

            // Assert
            Assert.That(_viewModel.IsReportGenerating, Is.False, "Flag should correspond to finished/failed state");

            // Проверяем, что в лог попал ключ ошибки (RunExternalAppNotFoundErr)
            // Текст лога берется из свойства LogText
            Assert.That(_viewModel.LogText, Does.Contain(nameof(RunExternalAppNotFoundErr)));
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
        }
    }