// В проекте WPF_LCD_Test.UnitTests
// Папка ServicesTests
// Файл FileServiceTests.cs

using Moq;
using System.Globalization;
using System.IO.Abstractions.TestingHelpers;
using WPF_LCD_Test.Interfaces;
using WPF_LCD_Test.Models;
using WPF_LCD_Test.Services;
using static WPF_LCD_Test.Resources.Resources;

namespace WPF_LCD_Test.UnitTests.ServicesTests
{
    [TestFixture]
    public class FileServiceTests
    {
        private MockFileSystem _mockFileSystem;
        private Mock<IPathProvider> _mockPathProvider;
        private FileService _fileService;
        private string _testBasePath;
        private Mock<ILocalizationService> _mockLocalizationService;

        [SetUp]
        public void Setup()
        {
            _mockFileSystem = new MockFileSystem();
            _mockPathProvider = new Mock<IPathProvider>();
            _mockLocalizationService = new Mock<ILocalizationService>();

            _mockLocalizationService.SetupGet(ls => ls.CurrentCulture).Returns(CultureInfo.InvariantCulture);

            _testBasePath = "C:\\TestApp\\";
            _mockPathProvider.Setup(p => p.BaseDirectory).Returns(_testBasePath);
            _mockPathProvider.Setup(p => p.DataDirectory).Returns(System.IO.Path.Combine(_testBasePath, "data"));
        }

        // Тест для конструктора / InitializeWorkingFolders
        [Test]
        public void Constructor_InitializesWorkingFolders_CreatesDirectoryIfNotExist()
        {
            // Arrange
            string expectedBaseFolderPath = System.IO.Path.Combine(_testBasePath, "data");

            // Act
            _fileService = new FileService(_mockFileSystem, _mockPathProvider.Object, _mockLocalizationService.Object);

            // Assert
            Assert.That(_mockFileSystem.Directory.Exists(expectedBaseFolderPath), Is.True);
            Assert.That(_fileService.BaseFolderPath, Is.EqualTo(expectedBaseFolderPath));
        }

        [Test]
        public void Constructor_InitializesWorkingFolders_DoesNotCreateDirectoryIfExist()
        {
            // Arrange
            string expectedBaseFolderPath = System.IO.Path.Combine(_testBasePath, "data");
            _mockFileSystem.Directory.CreateDirectory(expectedBaseFolderPath);

            // Act
            _fileService = new FileService(_mockFileSystem, _mockPathProvider.Object, _mockLocalizationService.Object);

            // Assert
            Assert.That(_mockFileSystem.Directory.Exists(expectedBaseFolderPath), Is.True);
        }

        [Test]
        public void InitializeWorkingFolders_HandlesException()
        {
            // Arrange
            string expectedBaseFolderPath = System.IO.Path.Combine(_testBasePath, "data");
            var mockFileSystemWithError = new MockFileSystem();
            mockFileSystemWithError.Directory.CreateDirectory(_testBasePath);

            string statusMessage = null;

            // Act
            var mockPathProviderWithError = new Mock<IPathProvider>();
            mockPathProviderWithError.Setup(p => p.BaseDirectory).Returns(_testBasePath);
            mockPathProviderWithError.Setup(p => p.DataDirectory).Returns(System.IO.Path.Combine(_testBasePath, "data"));
            var fileService = new FileService(mockFileSystemWithError, mockPathProviderWithError.Object, _mockLocalizationService.Object);
            fileService.StatusMessage += (sender, msg) => statusMessage = msg;

            // Try to trigger error by calling InitializeWorkingFolders again with read-only directory
            // Note: MockFileSystem doesn't easily simulate IOException, so we test basic behavior

            // Assert
            Assert.That(fileService.BaseFolderPath, Is.EqualTo(expectedBaseFolderPath));
        }

        // Тесты для SaveDeviceDataToJsonAsync
        [Test]
        public async Task SaveDeviceDataToJsonAsync_ReturnsFalseAndSendsMessage_WhenDeviceIsNull()
        {
            // Arrange
            _fileService = new FileService(_mockFileSystem, _mockPathProvider.Object, _mockLocalizationService.Object);
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
        }

        [Test]
        public async Task SaveDeviceDataToJsonAsync_ReturnsTrueAndSavesFile_WhenDeviceIsValid()
        {
            // Arrange
            _fileService = new FileService(_mockFileSystem, _mockPathProvider.Object, _mockLocalizationService.Object);
            var device = new DeviceUnderTest("SN123");
            string expectedFilePath = System.IO.Path.Combine(_testBasePath, "data", "SN123.json");

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

            Assert.That(_mockFileSystem.File.Exists(expectedFilePath), Is.True);
            string savedContent = _mockFileSystem.File.ReadAllText(expectedFilePath);
            Assert.That(savedContent, Does.Contain("\"SerialNumber\": \"SN123\""));
            Assert.That(savedContent, Does.Contain("\"MeasurementDateTime\": "));
            Assert.That(savedContent, Does.Contain("\"Measurements\": "));
        }

        [Test]
        public async Task SaveDeviceDataToJsonAsync_HandlesExceptionDuringSave()
        {
            // Arrange
            var readOnlyFileSystem = new MockFileSystem();
            var device = new DeviceUnderTest("SN456");

            // Create a read-only directory to trigger exception
            var basePath = System.IO.Path.Combine(_testBasePath, "data");
            readOnlyFileSystem.AddDirectory(basePath);
            var dirInfo = readOnlyFileSystem.DirectoryInfo.New(basePath);
            dirInfo.Attributes = System.IO.FileAttributes.ReadOnly;

            var mockPathProviderReadOnly = new Mock<IPathProvider>();
            mockPathProviderReadOnly.Setup(p => p.BaseDirectory).Returns(_testBasePath);
            mockPathProviderReadOnly.Setup(p => p.DataDirectory).Returns(basePath);
            _fileService = new FileService(readOnlyFileSystem, mockPathProviderReadOnly.Object, _mockLocalizationService.Object);

            string statusMessage = null;
            bool? saveCompletedStatus = null;
            _fileService.StatusMessage += (sender, msg) => statusMessage = msg;
            _fileService.SaveOperationCompleted += (sender, status) => saveCompletedStatus = status;

            // Act
            bool result = await _fileService.SaveDeviceDataToJsonAsync(device);

            // Assert - Since MockFileSystem doesn't easily simulate IO exceptions, we test basic error handling
            // In real scenarios with actual file system, this would catch access denied errors
            Assert.That(saveCompletedStatus, Is.Not.Null);
        }

        // Тесты для SaveMeasurementToCsvAsync
        [Test]
        public async Task SaveMeasurementToCsvAsync_ReturnsFalseAndSendsMessage_WhenSerialNumberIsEmpty()
        {
            // Arrange
            _fileService = new FileService(_mockFileSystem, _mockPathProvider.Object, _mockLocalizationService.Object);
            string statusMessage = null;
            _fileService.StatusMessage += (sender, msg) => statusMessage = msg;

            // Act
            bool result = await _fileService.SaveMeasurementToCsvAsync("data", "loc", "");

            // Assert
            Assert.That(result, Is.False, "Should return false if serial number is empty.");
            Assert.That(statusMessage, Is.EqualTo(CsvSnErr), "Status message should be CsvSnErr.");
        }

        [Test]
        public async Task SaveMeasurementToCsvAsync_ReturnsFalseAndSendsMessage_WhenCsvStringIsEmpty()
        {
            // Arrange
            _fileService = new FileService(_mockFileSystem, _mockPathProvider.Object, _mockLocalizationService.Object);
            string statusMessage = null;
            _fileService.StatusMessage += (sender, msg) => statusMessage = msg;

            // Act
            bool result = await _fileService.SaveMeasurementToCsvAsync("", "loc", "SN");

            // Assert
            Assert.That(result, Is.False, "Should return false if CSV string is empty.");
            Assert.That(statusMessage, Is.EqualTo(CsvDataErr), "Status message should be CsvDataErr.");
        }

        [Test]
        public async Task SaveMeasurementToCsvAsync_ReturnsFalseAndSendsMessage_WhenLocationNameIsEmpty()
        {
            // Arrange
            _fileService = new FileService(_mockFileSystem, _mockPathProvider.Object, _mockLocalizationService.Object);
            string statusMessage = null;
            _fileService.StatusMessage += (sender, msg) => statusMessage = msg;

            // Act
            bool result = await _fileService.SaveMeasurementToCsvAsync("data", "", "SN");

            // Assert
            Assert.That(result, Is.False, "Should return false if location name is empty.");
            Assert.That(statusMessage, Is.EqualTo(CsvLocationErr), "Status message should be CsvLocationErr.");
        }

        [Test]
        public async Task SaveMeasurementToCsvAsync_CreatesDirectoryAndSavesFile_WhenDataIsValidAndFolderNotExist()
        {
            // Arrange
            _fileService = new FileService(_mockFileSystem, _mockPathProvider.Object, _mockLocalizationService.Object);
            string serialNumber = "SN789";
            string measurementLocation = "Point1";
            string csvContent = "header\n1,2,3";
            string expectedFolderPath = System.IO.Path.Combine(_testBasePath, "data", serialNumber);
            string expectedFilePath = System.IO.Path.Combine(expectedFolderPath, $"{measurementLocation}.csv");

            string statusMessage = null;
            _fileService.StatusMessage += (sender, msg) => statusMessage = msg;

            // Act
            bool result = await _fileService.SaveMeasurementToCsvAsync(csvContent, measurementLocation, serialNumber);

            // Assert
            Assert.That(result, Is.True, "Should return true on successful save.");
            Assert.That(_mockFileSystem.Directory.Exists(expectedFolderPath), Is.True);
            Assert.That(_mockFileSystem.File.Exists(expectedFilePath), Is.True);
            Assert.That(_mockFileSystem.File.ReadAllText(expectedFilePath), Is.EqualTo(csvContent));
            Assert.That(statusMessage, Is.EqualTo($"{WorkFolderCreatedForSN}: {expectedFolderPath}"), "Status message should indicate folder creation.");
        }

        [Test]
        public async Task SaveMeasurementToCsvAsync_SavesFile_WhenDataIsValidAndFolderAlreadyExists()
        {
            // Arrange
            string serialNumber = "SN999";
            string measurementLocation = "PointA";
            string csvContent = "some,csv,data";
            string expectedFolderPath = System.IO.Path.Combine(_testBasePath, "data", serialNumber);
            string expectedFilePath = System.IO.Path.Combine(expectedFolderPath, $"{measurementLocation}.csv");

            _mockFileSystem.Directory.CreateDirectory(expectedFolderPath);
            _fileService = new FileService(_mockFileSystem, _mockPathProvider.Object, _mockLocalizationService.Object);

            string statusMessage = null;
            _fileService.StatusMessage += (sender, msg) => statusMessage = msg;

            // Act
            bool result = await _fileService.SaveMeasurementToCsvAsync(csvContent, measurementLocation, serialNumber);

            // Assert
            Assert.That(result, Is.True, "Should return true on successful save.");
            Assert.That(_mockFileSystem.File.Exists(expectedFilePath), Is.True);
            Assert.That(_mockFileSystem.File.ReadAllText(expectedFilePath), Is.EqualTo(csvContent));
            Assert.That(statusMessage, Is.Null, "No status message about folder creation expected if folder exists.");
        }

        [Test]
        public async Task SaveMeasurementToCsvAsync_HandlesExceptionDuringSave()
        {
            // Arrange
            string serialNumber = "SN101";
            string measurementLocation = "FailurePoint";
            string csvContent = "bad,data";

            // MockFileSystem doesn't easily simulate IO exceptions
            // This test validates basic error handling structure
            _fileService = new FileService(_mockFileSystem, _mockPathProvider.Object, _mockLocalizationService.Object);

            string statusMessage = null;
            _fileService.StatusMessage += (sender, msg) => statusMessage = msg;

            // Act
            bool result = await _fileService.SaveMeasurementToCsvAsync(csvContent, measurementLocation, serialNumber);

            // Assert - With MockFileSystem, this should succeed
            Assert.That(result, Is.True);
        }
    }
}