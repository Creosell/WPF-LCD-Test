namespace WPF_LCD_Test.Interfaces
    {
    /// <summary>
    /// Result of a finished external process.
    /// </summary>
    /// <param name="ExitCode">Process exit code.</param>
    /// <param name="StandardOutput">Everything the process wrote to stdout.</param>
    /// <param name="StandardError">Everything the process wrote to stderr.</param>
    public readonly record struct ProcessResult(int ExitCode, string StandardOutput, string StandardError);

    /// <summary>
    /// Runs an external console program and collects its output.
    /// </summary>
    public interface IProcessRunner
        {
        /// <summary>
        /// Runs the program, feeds it the standard input and waits for it to exit.
        /// </summary>
        /// <param name="fileName">Executable path.</param>
        /// <param name="arguments">Command line arguments.</param>
        /// <param name="workingDirectory">Working directory of the process.</param>
        /// <param name="standardInput">Text written to the stdin of the process (UTF-8, no BOM).</param>
        /// <param name="timeout">Maximum run time; the process tree is killed when it is exceeded.</param>
        /// <param name="cancellationToken">Token that kills the process when cancelled.</param>
        /// <returns>The exit code and the captured output.</returns>
        /// <exception cref="TimeoutException">Thrown when the process does not exit within the timeout.</exception>
        Task<ProcessResult> RunAsync(string fileName, IReadOnlyList<string> arguments, string workingDirectory, string standardInput, TimeSpan timeout, CancellationToken cancellationToken);
        }
    }
