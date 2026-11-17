using System.Diagnostics;
using System.IO;
using System.Text;
using WPF_LCD_Test.Interfaces;

namespace WPF_LCD_Test.Wrappers
    {
    /// <summary>
    /// <see cref="IProcessRunner"/> implementation over <see cref="Process"/>.
    /// </summary>
    public class ProcessRunner : IProcessRunner
        {
        /// <inheritdoc/>
        public async Task<ProcessResult> RunAsync(string fileName, IReadOnlyList<string> arguments, string workingDirectory, string standardInput, TimeSpan timeout, CancellationToken cancellationToken)
            {
            var startInfo = new ProcessStartInfo(fileName)
                {
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardInputEncoding = new UTF8Encoding(false),
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
                };
            foreach (var argument in arguments)
                {
                startInfo.ArgumentList.Add(argument);
                }

            using var process = new Process { StartInfo = startInfo };
            process.Start();

            using var timeoutSource = new CancellationTokenSource(timeout);
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutSource.Token);
            try
                {
                var output = process.StandardOutput.ReadToEndAsync(linked.Token);
                var error = process.StandardError.ReadToEndAsync(linked.Token);
                await process.StandardInput.WriteAsync(standardInput.AsMemory(), linked.Token);
                process.StandardInput.Close();
                await process.WaitForExitAsync(linked.Token);
                return new ProcessResult(process.ExitCode, await output, await error);
                }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                Kill(process);
                throw new TimeoutException($"{Path.GetFileName(fileName)} did not finish within {timeout.TotalSeconds:0.#} s.");
                }
            catch (OperationCanceledException)
                {
                Kill(process);
                throw;
                }
            }

        private static void Kill(Process process)
            {
            try
                {
                process.Kill(entireProcessTree: true);
                }
            catch (InvalidOperationException)
                {
                }
            }
        }
    }
