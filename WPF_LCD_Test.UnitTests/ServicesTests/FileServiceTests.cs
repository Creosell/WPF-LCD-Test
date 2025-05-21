// В проекте WPF_LCD_Test.UnitTests
// Папка ServicesTests
// Файл FileServiceTests.cs

using NUnit.Framework;
using Moq;
using System;
using System.IO;
using System.Threading.Tasks;
using WPF_LCD_Test.Interfaces;
using WPF_LCD_Test.Models;
using WPF_LCD_Test.Interfaces; // Для FileService
using static WPF_LCD_Test.Resources.Resources;
using System.Globalization;
using System.Threading;

namespace WPF_LCD_Test.UnitTests.ServicesTests
{
    [TestFixture]
    public class FileServiceTests
    {
        private Mock<IDirectory> _mockDirectory;
        private Mock<IFile> _mockFile;
        private Mock<IPath> _mockPath;
        private FileService _fileService;
        private string _testBasePath;
        private Mock<ILocalizationService> _mockLocalizationService; 


        [SetUp]
        public void Setup()
        {
            _mockDirectory = new Mock<IDirectory>();
            _mockFile = new Mock<IFile>();
            _mockPath = new Mock<IPath>();
            _mockLocalizationService = new Mock<ILocalizationService>(); // Инициализация мока

            // Настраиваем мок LocalizationService, чтобы он возвращал инвариантную культуру
            _mockLocalizationService.SetupGet(ls => ls.CurrentCulture).Returns(CultureInfo.InvariantCulture);

            _testBasePath = "C:\\TestApp\\";

            _mockPath.Setup(p => p.Combine(It.IsAny<string[]>()))
                     .Returns((string[] paths) => System.IO.Path.Combine(paths));
        }

        // Тест для конструктора / InitializeWorkingFolders
        [Test]
        public void Constructor_InitializesWorkingFolders_CreatesDirectoryIfNotExist()
        {
            // Arrange
            string expectedBaseFolderPath = System.IO.Path.Combine(_testBasePath, "data");

            // Настраиваем, что директории не существует
            _mockDirectory.Setup(d => d.Exists(expectedBaseFolderPath)).Returns(false);

            // Act
            _fileService = new FileService(_mockDirectory.Object, _mockFile.Object, _mockPath.Object, _testBasePath, _mockLocalizationService.Object);

            // Assert
            // Проверяем, что Exists был вызван
            _mockDirectory.Verify(d => d.Exists(expectedBaseFolderPath), Times.Once);
            // Проверяем, что CreateDirectory был вызван
            _mockDirectory.Verify(d => d.CreateDirectory(expectedBaseFolderPath), Times.Once);
            // Проверяем, что BaseFolderPath установлен правильно
            Assert.That(_fileService.BaseFolderPath, Is.EqualTo(expectedBaseFolderPath));
        }

        [Test]
        public void Constructor_InitializesWorkingFolders_DoesNotCreateDirectoryIfExist()
        {
            // Arrange
            string expectedBaseFolderPath = System.IO.Path.Combine(_testBasePath, "data");

            // Настраиваем, что директория уже существует
            _mockDirectory.Setup(d => d.Exists(expectedBaseFolderPath)).Returns(true);

            // Act
            _fileService = new FileService(_mockDirectory.Object, _mockFile.Object, _mockPath.Object, _testBasePath, _mockLocalizationService.Object);

            // Assert
            // Проверяем, что Exists был вызван
            _mockDirectory.Verify(d => d.Exists(expectedBaseFolderPath), Times.Once);
            // Проверяем, что CreateDirectory НЕ был вызван
            _mockDirectory.Verify(d => d.CreateDirectory(expectedBaseFolderPath), Times.Never);
        }

        [Test]
        public void InitializeWorkingFolders_HandlesException()
        {
            // Arrange
            string expectedBaseFolderPath = System.IO.Path.Combine(_testBasePath, "data");
            _mockDirectory.Setup(d => d.Exists(expectedBaseFolderPath)).Returns(false);

            // Настраиваем, что CreateDirectory выбрасывает исключение
            _mockDirectory.Setup(d => d.CreateDirectory(expectedBaseFolderPath))
                          .Throws(new IOException("Disk full."));

            // Мок для Path.Combine
            _mockPath.Setup(p => p.Combine(It.IsAny<string[]>()))
                     .Returns((string[] paths) => System.IO.Path.Combine(paths));

            // Act
            var fileService = new FileService(_mockDirectory.Object, _mockFile.Object, _mockPath.Object, _testBasePath, _mockLocalizationService.Object);

            // Assert
            // Проверяем, что CreateDirectory был вызван ровно один раз с правильным путем
            _mockDirectory.Verify(d => d.CreateDirectory(expectedBaseFolderPath), Times.Once);

            // Проверяем, что _baseFolderPath была установлена (даже если было исключение)
            Assert.That(fileService.BaseFolderPath, Is.EqualTo(expectedBaseFolderPath));
        }


        // Тесты для SaveDeviceDataToJsonAsync
        [Test]
        public async Task SaveDeviceDataToJsonAsync_ReturnsFalseAndSendsMessage_WhenDeviceIsNull()
        {
            // Arrange
            _fileService = new FileService(_mockDirectory.Object, _mockFile.Object, _mockPath.Object, _testBasePath, _mockLocalizationService.Object);
            string statusMessage = null;
            bool? saveCompletedStatus = null;

            _fileService.StatusMessage += (sender, msg) => statusMessage = msg;
            _fileService.SaveOperationCompleted += (sender, status) => saveCompletedStatus = status;

            // Act
            bool result = await _fileService.SaveDeviceDataToJsonAsync(null);

            // Assert
            Assert.That(result, Is.False, "Should return false when device is null.");
            Assert.That(statusMessage, Is.EqualTo(SaveJSONErrDeviceIsEmpty), "Status message should indicate null device.");
            Assert.That(saveCompletedStatus, Is.False, "SaveOperationCompleted should be false.");
            _mockFile.VerifyNoOtherCalls(); // Убеждаемся, что никаких операций с файлами не было
        }

        [Test]
        public async Task SaveDeviceDataToJsonAsync_ReturnsTrueAndSavesFile_WhenDeviceIsValid()
        {
            // Arrange
            _fileService = new FileService(_mockDirectory.Object, _mockFile.Object, _mockPath.Object, _testBasePath, _mockLocalizationService.Object);
            var device = new DeviceUnderTest ("SN123");
            string expectedFilePath = System.IO.Path.Combine(_testBasePath, "data", "SN123.json");
            string capturedJsonContent = null;

            // Настраиваем мок File.WriteAllTextAsync для захвата содержимого
            _mockFile.Setup(f => f.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>()))
                     .Callback<string, string>((path, content) =>
                     {
                         Assert.That(path, Is.EqualTo(expectedFilePath), "File path should be correct.");
                         capturedJsonContent = content;
                     })
                     .Returns(Task.CompletedTask);

            string statusMessage = null;
            bool? saveCompletedStatus = null;
            _fileService.StatusMessage += (sender, msg) => statusMessage = msg;
            _fileService.SaveOperationCompleted += (sender, status) => saveCompletedStatus = status;

            // Act
            bool result = await _fileService.SaveDeviceDataToJsonAsync(device);

            // Assert
            Assert.That(result, Is.True, "Should return true on successful save.");
            Assert.That(saveCompletedStatus, Is.True, "SaveOperationCompleted should be true.");
            Assert.That(statusMessage, Does.Contain(ResultsForSN), "Status message should indicate success.");
            Assert.That(statusMessage, Does.Contain(expectedFilePath), "Status message should contain file path.");

            // Проверяем, что метод WriteAllTextAsync был вызван ровно один раз
            _mockFile.Verify(f => f.WriteAllTextAsync(expectedFilePath, It.IsAny<string>()), Times.Once);

            // Проверяем, что JSON-содержимое корректно сериализовано
            Assert.That(capturedJsonContent, Is.Not.Null);
            Assert.That(capturedJsonContent, Does.Contain("\"SerialNumber\": \"SN123\""));
            Assert.That(capturedJsonContent, Does.Contain("\"MeasurementDateTime\": "));
            Assert.That(capturedJsonContent, Does.Contain("\"Measurements\": "));
        }

        [Test]
        public async Task SaveDeviceDataToJsonAsync_HandlesExceptionDuringSave()
        {
            // Arrange
            _fileService = new FileService(_mockDirectory.Object, _mockFile.Object, _mockPath.Object, _testBasePath, _mockLocalizationService.Object);
            var device = new DeviceUnderTest ("SN456");
            string expectedFilePath = System.IO.Path.Combine(_testBasePath, "data", "SN456.json");

            // Имитируем исключение при записи файла
            _mockFile.Setup(f => f.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>()))
                     .ThrowsAsync(new IOException("Access Denied."));

            string statusMessage = null;
            bool? saveCompletedStatus = null;
            _fileService.StatusMessage += (sender, msg) => statusMessage = msg;
            _fileService.SaveOperationCompleted += (sender, status) => saveCompletedStatus = status;

            // Act
            bool result = await _fileService.SaveDeviceDataToJsonAsync(device);

            // Assert
            Assert.That(result, Is.False, "Should return false on exception.");
            Assert.That(saveCompletedStatus, Is.False, "SaveOperationCompleted should be false.");
            Assert.That(statusMessage, Does.Contain(SaveJSONErrForSN), "Status message should indicate an error.");
            Assert.That(statusMessage, Does.Contain("Access Denied."), "Status message should contain exception message.");
            _mockFile.Verify(f => f.WriteAllTextAsync(expectedFilePath, It.IsAny<string>()), Times.Once);
        }

        // Тесты для SaveMeasurementToCsvAsync
        [Test]
        public async Task SaveMeasurementToCsvAsync_ReturnsFalseAndSendsMessage_WhenSerialNumberIsEmpty()
        {
            // Arrange
            _fileService = new FileService(_mockDirectory.Object, _mockFile.Object, _mockPath.Object, _testBasePath, _mockLocalizationService.Object);
            string statusMessage = null;
            _fileService.StatusMessage += (sender, msg) => statusMessage = msg;

            // Act
            bool result = await _fileService.SaveMeasurementToCsvAsync("data", "loc", "");

            // Assert
            Assert.That(result, Is.False, "Should return false if serial number is empty.");
            Assert.That(statusMessage, Is.EqualTo(CsvSnErr), "Status message should be CsvSnErr.");
            _mockFile.VerifyNoOtherCalls();
        }

        [Test]
        public async Task SaveMeasurementToCsvAsync_ReturnsFalseAndSendsMessage_WhenCsvStringIsEmpty()
        {
            // Arrange
            _fileService = new FileService(_mockDirectory.Object, _mockFile.Object, _mockPath.Object, _testBasePath, _mockLocalizationService.Object);
            string statusMessage = null;
            _fileService.StatusMessage += (sender, msg) => statusMessage = msg;

            // Act
            bool result = await _fileService.SaveMeasurementToCsvAsync("", "loc", "SN");

            // Assert
            Assert.That(result, Is.False, "Should return false if CSV string is empty.");
            Assert.That(statusMessage, Is.EqualTo(CsvDataErr), "Status message should be CsvDataErr.");
            _mockFile.VerifyNoOtherCalls();
        }

        [Test]
        public async Task SaveMeasurementToCsvAsync_ReturnsFalseAndSendsMessage_WhenLocationNameIsEmpty()
        {
            // Arrange
            _fileService = new FileService(_mockDirectory.Object, _mockFile.Object, _mockPath.Object, _testBasePath, _mockLocalizationService.Object);
            string statusMessage = null;
            _fileService.StatusMessage += (sender, msg) => statusMessage = msg;

            // Act
            bool result = await _fileService.SaveMeasurementToCsvAsync("data", "", "SN");

            // Assert
            Assert.That(result, Is.False, "Should return false if location name is empty.");
            Assert.That(statusMessage, Is.EqualTo(CsvLocationErr), "Status message should be CsvLocationErr.");
            _mockFile.VerifyNoOtherCalls();
        }

        [Test]
        public async Task SaveMeasurementToCsvAsync_CreatesDirectoryAndSavesFile_WhenDataIsValidAndFolderNotExist()
        {
            // Arrange
            _fileService = new FileService(_mockDirectory.Object, _mockFile.Object, _mockPath.Object, _testBasePath, _mockLocalizationService.Object);
            string serialNumber = "SN789";
            string measurementLocation = "Point1";
            string csvContent = "header\n1,2,3";
            string expectedFolderPath = System.IO.Path.Combine(_testBasePath, "data", serialNumber);
            string expectedFilePath = System.IO.Path.Combine(expectedFolderPath, $"{measurementLocation}.csv");

            // Настраиваем мок, что папки не существует
            _mockDirectory.Setup(d => d.Exists(expectedFolderPath)).Returns(false);

            // Настраиваем мок File.WriteAllTextAsync
            _mockFile.Setup(f => f.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>()))
                     .Callback<string, string>((path, content) =>
                     {
                         Assert.That(path, Is.EqualTo(expectedFilePath), "CSV file path should be correct.");
                         Assert.That(content, Is.EqualTo(csvContent), "CSV content should be correct.");
                     })
                     .Returns(Task.CompletedTask);

            string statusMessage = null;
            _fileService.StatusMessage += (sender, msg) => statusMessage = msg;

            // Act
            bool result = await _fileService.SaveMeasurementToCsvAsync(csvContent, measurementLocation, serialNumber);

            // Assert
            Assert.That(result, Is.True, "Should return true on successful save.");
            _mockDirectory.Verify(d => d.Exists(expectedFolderPath), Times.Once);
            _mockDirectory.Verify(d => d.CreateDirectory(expectedFolderPath), Times.Once); // Должна быть создана
            _mockFile.Verify(f => f.WriteAllTextAsync(expectedFilePath, csvContent), Times.Once);
            Assert.That(statusMessage, Is.EqualTo($"{WorkFolderCreatedForSN}: {expectedFolderPath}"), "Status message should indicate folder creation.");
        }

        [Test]
        public async Task SaveMeasurementToCsvAsync_SavesFile_WhenDataIsValidAndFolderAlreadyExists()
        {
            // Arrange
            _fileService = new FileService(_mockDirectory.Object, _mockFile.Object, _mockPath.Object, _testBasePath, _mockLocalizationService.Object);
            string serialNumber = "SN999";
            string measurementLocation = "PointA";
            string csvContent = "some,csv,data";
            string expectedFolderPath = System.IO.Path.Combine(_testBasePath, "data", serialNumber);
            string expectedFilePath = System.IO.Path.Combine(expectedFolderPath, $"{measurementLocation}.csv");

            // Настраиваем мок, что папка уже существует
            _mockDirectory.Setup(d => d.Exists(expectedFolderPath)).Returns(true);

            // Настраиваем мок File.WriteAllTextAsync
            _mockFile.Setup(f => f.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>()))
                     .Callback<string, string>((path, content) =>
                     {
                         Assert.That(path, Is.EqualTo(expectedFilePath));
                         Assert.That(content, Is.EqualTo(csvContent));
                     })
                     .Returns(Task.CompletedTask);

            string statusMessage = null;
            _fileService.StatusMessage += (sender, msg) => statusMessage = msg; // Подписываемся на события

            // Act
            bool result = await _fileService.SaveMeasurementToCsvAsync(csvContent, measurementLocation, serialNumber);

            // Assert
            Assert.That(result, Is.True, "Should return true on successful save.");
            _mockDirectory.Verify(d => d.Exists(expectedFolderPath), Times.Once);
            _mockDirectory.Verify(d => d.CreateDirectory(expectedFolderPath), Times.Never); // Не должна быть создана
            _mockFile.Verify(f => f.WriteAllTextAsync(expectedFilePath, csvContent), Times.Once);
            Assert.That(statusMessage, Is.Null, "No status message about folder creation expected if folder exists."); // Проверяем, что не было сообщения о создании папки
        }

        [Test]
        public async Task SaveMeasurementToCsvAsync_HandlesExceptionDuringSave()
        {
            // Arrange
            _fileService = new FileService(_mockDirectory.Object, _mockFile.Object, _mockPath.Object, _testBasePath, _mockLocalizationService.Object);
            string serialNumber = "SN101";
            string measurementLocation = "FailurePoint";
            string csvContent = "bad,data";
            string expectedFolderPath = System.IO.Path.Combine(_testBasePath, "data", serialNumber);
            string expectedFilePath = System.IO.Path.Combine(expectedFolderPath, $"{measurementLocation}.csv");

            _mockDirectory.Setup(d => d.Exists(It.IsAny<string>())).Returns(true); // Папка существует
            // Имитируем исключение при записи файла
            _mockFile.Setup(f => f.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>()))
                     .ThrowsAsync(new UnauthorizedAccessException("Permission denied."));

            string statusMessage = null;
            _fileService.StatusMessage += (sender, msg) => statusMessage = msg;

            // Act
            bool result = await _fileService.SaveMeasurementToCsvAsync(csvContent, measurementLocation, serialNumber);

            // Assert
            Assert.That(result, Is.False, "Should return false on exception.");
            Assert.That(statusMessage, Does.Contain(ErrCSV), "Status message should indicate an error.");
            Assert.That(statusMessage, Does.Contain("Permission denied."), "Status message should contain exception message.");
            _mockFile.Verify(f => f.WriteAllTextAsync(expectedFilePath, csvContent), Times.Once);
        }
    }
}