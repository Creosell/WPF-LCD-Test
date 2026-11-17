using System.IO.Abstractions;
using System.Text.Json;
using WPF_LCD_Test.Interfaces;
using WPF_LCD_Test.Models;

namespace WPF_LCD_Test.Services
    {
    /// <summary>
    /// Runs <c>ReportGenerator.exe --evaluate -</c> and parses its answer.
    /// </summary>
    public class SpecEvaluationService : ISpecEvaluationService
        {
        private const string GeneratorFileName = "ReportGenerator.exe";
        private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(5);

        private readonly IFileSystem _fileSystem;
        private readonly IPathProvider _pathProvider;
        private readonly IProcessRunner _processRunner;

        /// <summary>
        /// Initializes a new instance of SpecEvaluationService.
        /// </summary>
        /// <param name="fileSystem">File system abstraction.</param>
        /// <param name="pathProvider">Path provider used to locate the generator next to the application.</param>
        /// <param name="processRunner">Runner of the external generator process.</param>
        public SpecEvaluationService(IFileSystem fileSystem, IPathProvider pathProvider, IProcessRunner processRunner)
            {
            _fileSystem = fileSystem;
            _pathProvider = pathProvider;
            _processRunner = processRunner;
            }

        /// <inheritdoc/>
        public async Task<SpecEvaluationOutcome> EvaluateAsync(string deviceJson, CancellationToken cancellationToken)
            {
            var generatorPath = _fileSystem.Path.Combine(_pathProvider.BaseDirectory, GeneratorFileName);
            if (!_fileSystem.File.Exists(generatorPath))
                {
                return new(null, $"{GeneratorFileName} not found");
                }

            try
                {
                var result = await _processRunner.RunAsync(generatorPath, ["--evaluate", "-"], _pathProvider.BaseDirectory, deviceJson, _timeout, cancellationToken);
                if (result.ExitCode == 0)
                    {
                    return new(SpecEvaluation.Parse(result.StandardOutput), null);
                    }

                return new(null, ReadError(result));
                }
            catch (OperationCanceledException)
                {
                throw;
                }
            catch (Exception ex)
                {
                return new(null, ex.Message);
                }
            }

        private static string ReadError(ProcessResult result)
            {
            try
                {
                using var document = JsonDocument.Parse(result.StandardOutput);
                if (document.RootElement.TryGetProperty("error", out var error) && error.GetString() is { } message)
                    {
                    return message;
                    }
                }
            catch (JsonException)
                {
                }

            return $"{GeneratorFileName} exited with code {result.ExitCode}";
            }
        }
    }
