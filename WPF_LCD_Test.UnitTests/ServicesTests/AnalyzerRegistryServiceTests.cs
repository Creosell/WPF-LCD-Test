using System.IO.Abstractions.TestingHelpers;
using Moq;
using WPF_LCD_Test.Interfaces;
using WPF_LCD_Test.Models;
using WPF_LCD_Test.Services;

namespace WPF_LCD_Test.UnitTests.ServicesTests
    {
    [TestFixture]
    public class AnalyzerRegistryServiceTests
        {
        private const string BaseDirectory = @"C:\App";
        private const string RegistryPath = @"C:\App\analyzers.json";

        private MockFileSystem _fileSystem;
        private AnalyzerRegistryService _service;

        [SetUp]
        public void Setup()
            {
            _fileSystem = new MockFileSystem();
            _fileSystem.AddDirectory(BaseDirectory);
            var pathProvider = new Mock<IPathProvider>();
            pathProvider.SetupGet(p => p.BaseDirectory).Returns(BaseDirectory);
            _service = new AnalyzerRegistryService(_fileSystem, pathProvider.Object);
            }

        [Test]
        public void GetLastChannel_NoRegistryFile_ReturnsNull()
            {
            Assert.That(_service.GetLastChannel("81026182"), Is.Null);
            }

        [Test]
        public void Update_NewProbe_CreatesFileAndStoresChannel()
            {
            _service.Update("81026182", ColorAnalyzerModel.CA410, 2);

            Assert.That(_fileSystem.File.Exists(RegistryPath), Is.True);
            Assert.That(_service.GetLastChannel("81026182"), Is.EqualTo(2));
            StringAssert.Contains("CA410", _fileSystem.File.ReadAllText(RegistryPath));
            }

        [Test]
        public void Update_ExistingProbe_OverwritesChannelWithoutDuplicates()
            {
            _service.Update("81026182", ColorAnalyzerModel.CA410, 2);
            _service.Update("81026182", ColorAnalyzerModel.CA410, 5);

            Assert.That(_service.GetLastChannel("81026182"), Is.EqualTo(5));
            StringAssert.DoesNotContain("\"LastChannel\": 2", _fileSystem.File.ReadAllText(RegistryPath));
            }

        [Test]
        public void GetLastChannel_SerialNumberCaseIsIgnored()
            {
            _service.Update("abc123", ColorAnalyzerModel.CA310, 1);

            Assert.That(_service.GetLastChannel("ABC123"), Is.EqualTo(1));
            }

        [Test]
        public void GetLastChannel_CorruptFile_ReturnsNullAndUpdateRecovers()
            {
            _fileSystem.AddFile(RegistryPath, new MockFileData("{ not json"));

            Assert.That(_service.GetLastChannel("81026182"), Is.Null);

            _service.Update("81026182", ColorAnalyzerModel.CA410, 3);
            Assert.That(_service.GetLastChannel("81026182"), Is.EqualTo(3));
            }
        }
    }
