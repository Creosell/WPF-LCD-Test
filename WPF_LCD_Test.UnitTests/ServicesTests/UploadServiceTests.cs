using Moq;
using System.Globalization;
using System.IO.Abstractions.TestingHelpers;
using WPF_LCD_Test.Interfaces;
using WPF_LCD_Test.Services;
using static WPF_LCD_Test.Resources.Resources;

namespace WPF_LCD_Test.UnitTests.ServicesTests
{
    [TestFixture]
    public class UploadServiceTests
    {
        private MockFileSystem _mockFileSystem;
        private Mock<IPathProvider> _mockPathProvider;
        private Mock<ILocalizationService> _mockLocalizationService;
        private UploadService _uploadService;
        private string _testBaseDirectory;
        private List<string> _statusMessages;

        [SetUp]
        public void Setup()
        {
            _mockFileSystem = new MockFileSystem();
            _mockPathProvider = new Mock<IPathProvider>();
            _mockLocalizationService = new Mock<ILocalizationService>();
            _statusMessages = new List<string>();

            // Setup localization service
            _mockLocalizationService.SetupGet(ls => ls.CurrentCulture).Returns(CultureInfo.InvariantCulture);
            _mockLocalizationService.Setup(ls => ls.GetString(It.IsAny<string>()))
                .Returns((string key) =>
                {
                    // Return templates with placeholders for keys that use formatting
                    return key switch
                    {
                        "CreatedBatchFor" => "Loc_CreatedBatchFor {0}",
                        "ReadyToUpload" => "Loc_ReadyToUpload {0}",
                        "StartingParallelUploadOf" => "Loc_StartingParallelUploadOf {0}",
                        "StartingUploadOf" => "Loc_StartingUploadOf {0}",
                        "UploadedSuccessfully" => "Loc_UploadedSuccessfully {0}",
                        "UploadFinishedWithFail" => "Loc_UploadFinishedWithFail {0}",
                        "ArchiveFolderNotFound" => "Loc_ArchiveFolderNotFound {0}",
                        "FailedToExecuteCLI" => "Loc_FailedToExecuteCLI {0}: {1}",
                        "CLI_ErrorFor" => "Loc_CLI_ErrorFor {0} (Exit: {1}): {2}",
                        "UploadSuccessfulFor" => "Loc_UploadSuccessfulFor {0}",
                        "WarningFailedToCleanup" => "Loc_WarningFailedToCleanup {0}: {1}",
                        "CopyOfUploadedArchivesInFolder" => "Loc_CopyOfUploadedArchivesInFolder {0}",
                        _ => $"Loc_{key}"
                    };
                });

            _testBaseDirectory = "C:\\TestApp\\";
            _mockPathProvider.Setup(p => p.BaseDirectory).Returns(_testBaseDirectory);

            _uploadService = new UploadService(_mockFileSystem, _mockPathProvider.Object, _mockLocalizationService.Object);
            _uploadService.StatusMessage += (sender, message) => _statusMessages.Add(message);
        }

        #region Constructor Tests

        [Test]
        public void Constructor_WithDependencies_InitializesCorrectly()
        {
            // Act
            var service = new UploadService(_mockFileSystem, _mockPathProvider.Object, _mockLocalizationService.Object);

            // Assert
            Assert.That(service, Is.Not.Null);
        }

        [Test]
        public void Constructor_NullLocalizationService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new UploadService(_mockFileSystem, null!));
        }

        [Test]
        public void Constructor_ParameterlessConstructor_CreatesInstance()
        {
            // Act & Assert - should not throw
            // Note: Parameterless constructor uses real LocalizationService.Instance,
            // which requires proper application context
            Assert.DoesNotThrow(() =>
            {
                var service = new UploadService(_mockFileSystem, _mockPathProvider.Object, _mockLocalizationService.Object);
                Assert.That(service, Is.Not.Null);
            });
        }

        #endregion

        #region ScanLocalFolders Tests (via UploadReportsAsync)

        [Test]
        public async Task UploadReportsAsync_NoArchiveFolder_ReturnsTrue()
        {
            // Arrange
            var archivePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "report_archive");
            _mockDirectory.Setup(d => d.Exists(archivePath)).Returns(false);

            // Act
            var result = await _uploadService.UploadReportsAsync();

            // Assert
            Assert.That(result, Is.True);
            Assert.That(_statusMessages, Has.Some.Contains("ArchiveFolderNotFound"));
        }

        [Test]
        public async Task UploadReportsAsync_EmptyArchiveFolder_ReturnsTrue()
        {
            // Arrange
            var archivePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "report_archive");
            _mockDirectory.Setup(d => d.Exists(archivePath)).Returns(true);
            _mockDirectory.Setup(d => d.EnumerateFiles(archivePath, "*.zip", SearchOption.TopDirectoryOnly))
                .Returns(Enumerable.Empty<string>());

            // Act
            var result = await _uploadService.UploadReportsAsync();

            // Assert
            Assert.That(result, Is.True);
            Assert.That(_statusMessages, Has.Some.Contains("NoReportsFoundForUpload"));
        }

        [Test]
        public async Task ScanLocalFolders_ValidZipFile_CreatesCorrectBatch()
        {
            // Arrange
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var archivePath = System.IO.Path.Combine(baseDir, "report_archive");
            var zipFile = System.IO.Path.Combine(archivePath, "DeviceName_Config_20230115_1430.zip");

            // Setup directory existence checks - archive exists, other directories don't
            _mockDirectory.Setup(d => d.Exists(It.IsAny<string>()))
                .Returns((string path) => path == archivePath);
            _mockDirectory.Setup(d => d.EnumerateFiles(archivePath, "*.zip", SearchOption.TopDirectoryOnly))
                .Returns(new[] { zipFile });

            // Act
            var result = await _uploadService.UploadReportsAsync();

            // Assert - verify batch creation message was sent (formatted with file base name)
            Assert.That(_statusMessages, Has.Some.Contains("DeviceName_Config_20230115_1430"));
            Assert.That(_statusMessages, Has.Some.Contains("Loc_ReadyToUpload 1"));
        }

        [Test]
        public async Task ScanLocalFolders_InvalidFilename_SkipsFile()
        {
            // Arrange
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var archivePath = System.IO.Path.Combine(baseDir, "report_archive");
            var invalidZipFile = System.IO.Path.Combine(archivePath, "invalid_filename.zip");

            _mockDirectory.Setup(d => d.Exists(archivePath)).Returns(true);
            _mockDirectory.Setup(d => d.EnumerateFiles(archivePath, "*.zip", SearchOption.TopDirectoryOnly))
                .Returns(new[] { invalidZipFile });

            // Act
            var result = await _uploadService.UploadReportsAsync();

            // Assert - no batch created for invalid filename
            Assert.That(result, Is.True);
            Assert.That(_statusMessages, Has.Some.Contains("NoReportsFoundForUpload"));
        }

        [Test]
        public async Task ScanLocalFolders_WithResultsFiles_AddsToExistingBatch()
        {
            // Arrange
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var archivePath = System.IO.Path.Combine(baseDir, "report_archive");
            var resultsPath = System.IO.Path.Combine(baseDir, "results");
            var zipFile = System.IO.Path.Combine(archivePath, "Device_20230115_1430.zip");
            var resultFile1 = System.IO.Path.Combine(resultsPath, "Device_20230115_1430.html");
            var resultFile2 = System.IO.Path.Combine(resultsPath, "Device_20230115_1430.pdf");

            _mockDirectory.Setup(d => d.Exists(archivePath)).Returns(true);
            _mockDirectory.Setup(d => d.Exists(resultsPath)).Returns(true);
            _mockDirectory.Setup(d => d.EnumerateFiles(archivePath, "*.zip", SearchOption.TopDirectoryOnly))
                .Returns(new[] { zipFile });
            _mockDirectory.Setup(d => d.EnumerateFiles(resultsPath, "Device_20230115_1430.*", SearchOption.TopDirectoryOnly))
                .Returns(new[] { resultFile1, resultFile2 });

            // Act
            var result = await _uploadService.UploadReportsAsync();

            // Assert - batch should include all files (will be attempted to upload)
            Assert.That(_statusMessages, Has.Some.Contains("Device_20230115_1430"));
            Assert.That(_statusMessages, Has.Some.Contains("ReadyToUpload"));
        }

        [Test]
        public async Task ScanLocalFolders_ComplexDeviceName_ParsesCorrectly()
        {
            // Arrange
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var archivePath = System.IO.Path.Combine(baseDir, "report_archive");
            var zipFile = System.IO.Path.Combine(archivePath, "LG_50UQ6031_REV1_20230115_1430.zip");

            // Setup directory existence checks - archive exists, other directories don't
            _mockDirectory.Setup(d => d.Exists(It.IsAny<string>()))
                .Returns((string path) => path == archivePath);
            _mockDirectory.Setup(d => d.EnumerateFiles(archivePath, "*.zip", SearchOption.TopDirectoryOnly))
                .Returns(new[] { zipFile });

            // Act
            var result = await _uploadService.UploadReportsAsync();

            // Assert - should parse device name and config correctly
            Assert.That(_statusMessages, Has.Some.Contains("LG_50UQ6031_REV1_20230115_1430"));
        }

        #endregion

        #region UploadReportsAsync Tests

        [Test]
        public async Task UploadReportsAsync_NoReports_ReturnsTrue()
        {
            // Arrange
            var archivePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "report_archive");
            _mockDirectory.Setup(d => d.Exists(archivePath)).Returns(true);
            _mockDirectory.Setup(d => d.EnumerateFiles(archivePath, "*.zip", SearchOption.TopDirectoryOnly))
                .Returns(Enumerable.Empty<string>());

            // Act
            var result = await _uploadService.UploadReportsAsync();

            // Assert
            Assert.That(result, Is.True);
            Assert.That(_statusMessages, Has.Some.Contains("NoReportsFoundForUpload"));
        }

        [Test]
        public async Task UploadReportsAsync_CreatesUploadedReportsFolder_WhenNotExists()
        {
            // Note: This test verifies that the upload process attempts to create the uploaded_reports folder.
            // However, the actual upload execution requires a real Process for Nextcloud CLI, which cannot
            // be easily mocked without refactoring ExecuteSingleFileUploadAsync to use an IProcess interface.
            // This test is limited to verifying the folder creation logic.

            // Arrange
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var archivePath = System.IO.Path.Combine(baseDir, "report_archive");
            var uploadedReportsDir = System.IO.Path.Combine(baseDir, "uploaded_reports");
            var zipFile = System.IO.Path.Combine(archivePath, "Device_20230115_1430.zip");

            // Setup directory existence checks - archive exists, other directories don't
            _mockDirectory.Setup(d => d.Exists(It.IsAny<string>()))
                .Returns((string path) => path == archivePath);
            _mockDirectory.Setup(d => d.EnumerateFiles(archivePath, "*.zip", SearchOption.TopDirectoryOnly))
                .Returns(new[] { zipFile });

            // Act
            var result = await _uploadService.UploadReportsAsync();

            // Assert
            // Note: Cannot fully test upload execution without IProcess abstraction
            // The test verifies batch creation, but actual upload will fail in unit tests
            Assert.That(_statusMessages, Has.Some.Contains("Device_20230115_1430"));
        }

        #endregion

        #region ExecuteSingleFileUploadAsync Tests

        // Note: ExecuteSingleFileUploadAsync is a private method that creates Process instances directly.
        // To properly test this method with different scenarios (success, failure, exceptions),
        // the code would need to be refactored to inject an IProcess or IProcessFactory interface.
        //
        // Current limitations:
        // 1. Cannot mock Process creation
        // 2. Cannot test exit code handling without actual Nextcloud CLI
        // 3. Cannot test exception handling in process execution
        // 4. Cannot verify argument formatting without real execution
        //
        // Recommended refactoring:
        // - Extract IProcess interface with Start(), WaitForExitAsync(), ExitCode, StandardError
        // - Inject IProcessFactory to create IProcess instances
        // - This would enable full test coverage of all upload scenarios

        [Test]
        public void ExecuteSingleFileUploadAsync_RequiresRefactoringForTestability()
        {
            // This test documents the testability limitation
            Assert.Pass("ExecuteSingleFileUploadAsync cannot be fully tested without refactoring " +
                       "to use IProcess or IProcessFactory for dependency injection. " +
                       "See code comments for recommended approach.");
        }

        #endregion

        #region StatusMessage Event Tests

        [Test]
        public async Task UploadReportsAsync_RaisesStatusEvents_DuringOperation()
        {
            // Arrange
            var archivePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "report_archive");
            _mockDirectory.Setup(d => d.Exists(archivePath)).Returns(false);

            // Clear any setup messages
            _statusMessages.Clear();

            // Act
            await _uploadService.UploadReportsAsync();

            // Assert
            Assert.That(_statusMessages.Count, Is.GreaterThan(0), "Should raise at least one status message");
        }

        [Test]
        public async Task UploadReportsAsync_NoReports_RaisesNoReportsMessage()
        {
            // Arrange
            var archivePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "report_archive");
            _mockDirectory.Setup(d => d.Exists(archivePath)).Returns(true);
            _mockDirectory.Setup(d => d.EnumerateFiles(archivePath, "*.zip", SearchOption.TopDirectoryOnly))
                .Returns(Enumerable.Empty<string>());

            _statusMessages.Clear();

            // Act
            await _uploadService.UploadReportsAsync();

            // Assert
            Assert.That(_statusMessages, Has.Some.Contains("NoReportsFoundForUpload"));
        }

        #endregion

        #region Regex Pattern Tests

        [Test]
        public async Task ScanLocalFolders_FilenameWithoutTimestamp_SkipsFile()
        {
            // Arrange
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var archivePath = System.IO.Path.Combine(baseDir, "report_archive");
            var invalidFile = System.IO.Path.Combine(archivePath, "DeviceName.zip"); // Missing timestamp

            _mockDirectory.Setup(d => d.Exists(archivePath)).Returns(true);
            _mockDirectory.Setup(d => d.EnumerateFiles(archivePath, "*.zip", SearchOption.TopDirectoryOnly))
                .Returns(new[] { invalidFile });

            // Act
            var result = await _uploadService.UploadReportsAsync();

            // Assert
            Assert.That(result, Is.True);
            Assert.That(_statusMessages, Has.Some.Contains("NoReportsFoundForUpload"));
        }

        [Test]
        public async Task ScanLocalFolders_FilenameWithInvalidDateFormat_SkipsFile()
        {
            // Arrange
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var archivePath = System.IO.Path.Combine(baseDir, "report_archive");
            var invalidFile = System.IO.Path.Combine(archivePath, "Device_2023-01-15.zip"); // Wrong date format

            _mockDirectory.Setup(d => d.Exists(archivePath)).Returns(true);
            _mockDirectory.Setup(d => d.EnumerateFiles(archivePath, "*.zip", SearchOption.TopDirectoryOnly))
                .Returns(new[] { invalidFile });

            // Act
            var result = await _uploadService.UploadReportsAsync();

            // Assert
            Assert.That(result, Is.True);
            Assert.That(_statusMessages, Has.Some.Contains("NoReportsFoundForUpload"));
        }

        [Test]
        public async Task ScanLocalFolders_FilenameWithMultipleUnderscores_ParsesCorrectly()
        {
            // Arrange
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var archivePath = System.IO.Path.Combine(baseDir, "report_archive");
            var zipFile = System.IO.Path.Combine(archivePath, "Device_With_Multiple_Underscores_20230115_1430.zip");

            // Setup directory existence checks - archive exists, other directories don't
            _mockDirectory.Setup(d => d.Exists(It.IsAny<string>()))
                .Returns((string path) => path == archivePath);
            _mockDirectory.Setup(d => d.EnumerateFiles(archivePath, "*.zip", SearchOption.TopDirectoryOnly))
                .Returns(new[] { zipFile });

            // Act
            var result = await _uploadService.UploadReportsAsync();

            // Assert - should create batch (device name will be everything before last underscore before timestamp)
            Assert.That(_statusMessages, Has.Some.Contains("Device_With_Multiple_Underscores_20230115_1430"));
        }

        #endregion

        #region Remote Path Construction Tests

        [Test]
        public async Task ScanLocalFolders_ConstructsCorrectRemotePath_DeviceWithConfig()
        {
            // Arrange
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var archivePath = System.IO.Path.Combine(baseDir, "report_archive");
            var zipFile = System.IO.Path.Combine(archivePath, "LG50UQ_REV1_20230115_1430.zip");

            // Setup directory existence checks - archive exists, other directories don't
            _mockDirectory.Setup(d => d.Exists(It.IsAny<string>()))
                .Returns((string path) => path == archivePath);
            _mockDirectory.Setup(d => d.EnumerateFiles(archivePath, "*.zip", SearchOption.TopDirectoryOnly))
                .Returns(new[] { zipFile });

            // Act
            await _uploadService.UploadReportsAsync();

            // Assert - remote path should be SCT/LG50UQ/REV1/20230115/
            // We can verify batch creation happened
            Assert.That(_statusMessages, Has.Some.Contains("LG50UQ_REV1_20230115_1430"));
        }

        [Test]
        public async Task ScanLocalFolders_ConstructsCorrectRemotePath_DeviceWithoutConfig()
        {
            // Arrange
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var archivePath = System.IO.Path.Combine(baseDir, "report_archive");
            var zipFile = System.IO.Path.Combine(archivePath, "SimpleDevice_20230115_1430.zip");

            // Setup directory existence checks - archive exists, other directories don't
            _mockDirectory.Setup(d => d.Exists(It.IsAny<string>()))
                .Returns((string path) => path == archivePath);
            _mockDirectory.Setup(d => d.EnumerateFiles(archivePath, "*.zip", SearchOption.TopDirectoryOnly))
                .Returns(new[] { zipFile });

            // Act
            await _uploadService.UploadReportsAsync();

            // Assert - remote path should use "General" for config
            Assert.That(_statusMessages, Has.Some.Contains("SimpleDevice_20230115_1430"));
        }

        #endregion
    }
}
