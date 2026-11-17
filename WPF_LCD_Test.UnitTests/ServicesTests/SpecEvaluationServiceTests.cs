using System.IO.Abstractions.TestingHelpers;
using Moq;
using WPF_LCD_Test.Interfaces;
using WPF_LCD_Test.Models;
using WPF_LCD_Test.Services;

namespace WPF_LCD_Test.UnitTests.ServicesTests
    {
    [TestFixture]
    public class SpecEvaluationServiceTests
        {
        private const string BaseDirectory = @"C:\App";
        private const string GeneratorPath = @"C:\App\ReportGenerator.exe";
        private const string Result = """{"schema": 1, "device": "PASS", "complete": true, "checks": []}""";

        private MockFileSystem _fileSystem;
        private Mock<IProcessRunner> _runner;
        private SpecEvaluationService _service;

        [SetUp]
        public void Setup()
            {
            _fileSystem = new MockFileSystem();
            _fileSystem.AddFile(GeneratorPath, new MockFileData(string.Empty));
            _runner = new Mock<IProcessRunner>();
            var pathProvider = new Mock<IPathProvider>();
            pathProvider.SetupGet(p => p.BaseDirectory).Returns(BaseDirectory);
            _service = new SpecEvaluationService(_fileSystem, pathProvider.Object, _runner.Object);
            }

        private void SetupRun(ProcessResult result) =>
            _runner.Setup(r => r.RunAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(result);

        [Test]
        public async Task EvaluateAsync_RunsTheGeneratorInEvaluateModeAndFeedsTheJsonThroughStdin()
            {
            SetupRun(new ProcessResult(0, Result, string.Empty));

            var outcome = await _service.EvaluateAsync("{\"SerialNumber\":\"1\"}", CancellationToken.None);

            Assert.That(outcome.Error, Is.Null);
            Assert.That(outcome.Evaluation!.Verdict, Is.EqualTo(SpecVerdict.Pass));
            _runner.Verify(r => r.RunAsync(GeneratorPath, It.Is<IReadOnlyList<string>>(a => a.SequenceEqual(new[] { "--evaluate", "-" })),
                BaseDirectory, "{\"SerialNumber\":\"1\"}", It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Once);
            }

        [Test]
        public async Task EvaluateAsync_GeneratorMissing_ReportsItWithoutRunningAnything()
            {
            _fileSystem.RemoveFile(GeneratorPath);

            var outcome = await _service.EvaluateAsync("{}", CancellationToken.None);

            Assert.That(outcome.Evaluation, Is.Null);
            Assert.That(outcome.Error, Does.Contain("ReportGenerator.exe"));
            _runner.VerifyNoOtherCalls();
            }

        [Test]
        public async Task EvaluateAsync_NonZeroExit_ReturnsTheGeneratorError()
            {
            SetupRun(new ProcessResult(3, """{"schema": 1, "error": "spec not found: X"}""", string.Empty));

            var outcome = await _service.EvaluateAsync("{}", CancellationToken.None);

            Assert.That(outcome.Evaluation, Is.Null);
            Assert.That(outcome.Error, Is.EqualTo("spec not found: X"));
            }

        [Test]
        public async Task EvaluateAsync_NonZeroExitWithoutJson_ReturnsTheExitCode()
            {
            SetupRun(new ProcessResult(1, "Traceback...", "boom"));

            var outcome = await _service.EvaluateAsync("{}", CancellationToken.None);

            Assert.That(outcome.Error, Does.Contain("code 1"));
            }

        [Test]
        public async Task EvaluateAsync_UnreadableAnswer_ReturnsAnError()
            {
            SetupRun(new ProcessResult(0, "garbage", string.Empty));

            var outcome = await _service.EvaluateAsync("{}", CancellationToken.None);

            Assert.That(outcome.Evaluation, Is.Null);
            Assert.That(outcome.Error, Is.Not.Null.And.Not.Empty);
            }

        [Test]
        public async Task EvaluateAsync_Timeout_ReturnsAnErrorInsteadOfThrowing()
            {
            _runner.Setup(r => r.RunAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new TimeoutException("ReportGenerator.exe did not finish within 5 s."));

            var outcome = await _service.EvaluateAsync("{}", CancellationToken.None);

            Assert.That(outcome.Error, Does.Contain("did not finish"));
            }

        [Test]
        public void EvaluateAsync_Cancellation_Propagates()
            {
            _runner.Setup(r => r.RunAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            Assert.ThrowsAsync<OperationCanceledException>(() => _service.EvaluateAsync("{}", new CancellationToken(true)));
            }
        }
    }
