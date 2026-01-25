using WPF_LCD_Test.Models;

namespace WPF_LCD_Test.UnitTests.ModelsTests
{
    [TestFixture]
    public class UploadReportItemTests
    {
        [Test]
        public void Constructor_RequiredProperties_Initialized()
        {
            // Arrange
            var remoteDirectory = "SCT/Device/Config/20230115/";
            var localFiles = new List<string> { "file1.zip", "file2.html" };

            // Act
            var item = new UploadReportItem
            {
                ReportRemoteDirectory = remoteDirectory,
                LocalFilesToUpload = localFiles
            };

            // Assert
            Assert.That(item.ReportRemoteDirectory, Is.EqualTo(remoteDirectory));
            Assert.That(item.LocalFilesToUpload, Is.EqualTo(localFiles));
        }

        [Test]
        public void ReportRemoteDirectory_Setter_StoresValue()
        {
            // Arrange
            var remoteDirectory = "SCT/TestDevice/REV1/20230201/";

            // Act
            var item = new UploadReportItem
            {
                ReportRemoteDirectory = remoteDirectory,
                LocalFilesToUpload = new List<string>()
            };

            // Assert
            Assert.That(item.ReportRemoteDirectory, Is.EqualTo(remoteDirectory));
        }

        [Test]
        public void LocalFilesToUpload_DefaultInitialization_EmptyList()
        {
            // Act
            var item = new UploadReportItem
            {
                ReportRemoteDirectory = "SCT/Device/Config/Date/",
                LocalFilesToUpload = new List<string>()
            };

            // Assert
            Assert.That(item.LocalFilesToUpload, Is.Not.Null);
            Assert.That(item.LocalFilesToUpload, Is.Empty);
        }

        [Test]
        public void LocalFilesToUpload_AddFiles_MaintainsList()
        {
            // Arrange
            var item = new UploadReportItem
            {
                ReportRemoteDirectory = "SCT/Device/Config/Date/",
                LocalFilesToUpload = new List<string>()
            };

            // Act
            item.LocalFilesToUpload.Add("file1.zip");
            item.LocalFilesToUpload.Add("file2.html");
            item.LocalFilesToUpload.Add("file3.pdf");

            // Assert
            Assert.That(item.LocalFilesToUpload.Count, Is.EqualTo(3));
            Assert.That(item.LocalFilesToUpload[0], Is.EqualTo("file1.zip"));
            Assert.That(item.LocalFilesToUpload[1], Is.EqualTo("file2.html"));
            Assert.That(item.LocalFilesToUpload[2], Is.EqualTo("file3.pdf"));
        }

        [Test]
        public void LocalFilesToUpload_MultipleOperations_MaintainsState()
        {
            // Arrange
            var item = new UploadReportItem
            {
                ReportRemoteDirectory = "SCT/Device/Config/Date/",
                LocalFilesToUpload = new List<string>()
            };

            // Act
            item.LocalFilesToUpload.Add("file1.zip");
            item.LocalFilesToUpload.Add("file2.html");
            item.LocalFilesToUpload.Remove("file1.zip");
            item.LocalFilesToUpload.Add("file3.pdf");

            // Assert
            Assert.That(item.LocalFilesToUpload.Count, Is.EqualTo(2));
            Assert.That(item.LocalFilesToUpload, Does.Contain("file2.html"));
            Assert.That(item.LocalFilesToUpload, Does.Contain("file3.pdf"));
            Assert.That(item.LocalFilesToUpload, Does.Not.Contain("file1.zip"));
        }

        [Test]
        public void Constructor_WithInitializedList_StoresCorrectly()
        {
            // Arrange & Act
            var item = new UploadReportItem
            {
                ReportRemoteDirectory = "SCT/Device/Config/Date/",
                LocalFilesToUpload = new List<string> { "initial1.zip", "initial2.html" }
            };

            // Assert
            Assert.That(item.LocalFilesToUpload.Count, Is.EqualTo(2));
            Assert.That(item.LocalFilesToUpload[0], Is.EqualTo("initial1.zip"));
            Assert.That(item.LocalFilesToUpload[1], Is.EqualTo("initial2.html"));
        }

        [Test]
        public void ReportRemoteDirectory_EmptyString_Allowed()
        {
            // Act
            var item = new UploadReportItem
            {
                ReportRemoteDirectory = string.Empty,
                LocalFilesToUpload = new List<string>()
            };

            // Assert
            Assert.That(item.ReportRemoteDirectory, Is.Empty);
        }

        [Test]
        public void LocalFilesToUpload_DuplicateFiles_AllowsDuplicates()
        {
            // Arrange
            var item = new UploadReportItem
            {
                ReportRemoteDirectory = "SCT/Device/Config/Date/",
                LocalFilesToUpload = new List<string>()
            };

            // Act
            item.LocalFilesToUpload.Add("duplicate.zip");
            item.LocalFilesToUpload.Add("duplicate.zip");

            // Assert
            Assert.That(item.LocalFilesToUpload.Count, Is.EqualTo(2));
            Assert.That(item.LocalFilesToUpload, Has.All.EqualTo("duplicate.zip"));
        }
    }
}
