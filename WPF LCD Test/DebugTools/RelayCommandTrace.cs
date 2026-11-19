#if DEBUG
using System.Diagnostics;

namespace WPF_LCD_Test.DebugTools
    {
    /// <summary>
    /// Traces one run of a <see cref="Commands.RelayCommand"/>: what it runs, with which parameter, how long it took and how it ended.
    /// </summary>
    public readonly struct RelayCommandTrace
        {
        private readonly string _name;
        private readonly Stopwatch _clock;

        private RelayCommandTrace(string name)
            {
            _name = name;
            _clock = Stopwatch.StartNew();
            }

        /// <summary>
        /// Notes that the command started.
        /// </summary>
        /// <param name="code">The delegate the command runs.</param>
        /// <param name="parameter">The parameter of the command.</param>
        /// <returns>The trace to finish when the command ends.</returns>
        public static RelayCommandTrace Begin(Delegate? code, object? parameter)
            {
            var trace = new RelayCommandTrace(DebugTrace.DescribeDelegate(code));
            DebugTrace.Write("command", $"{trace._name}({DebugTrace.Format(parameter)}) started");
            return trace;
            }

        /// <summary>
        /// Notes that the command was refused because it cannot run now.
        /// </summary>
        /// <param name="code">The delegate the command runs.</param>
        /// <param name="parameter">The parameter of the command.</param>
        public static void Refused(Delegate? code, object? parameter) =>
            DebugTrace.Write("command", $"{DebugTrace.DescribeDelegate(code)}({DebugTrace.Format(parameter)}) ignored: it cannot run now");

        /// <summary>
        /// Notes that the command ended.
        /// </summary>
        /// <param name="error">The exception that ended it, if any.</param>
        public void End(Exception? error = null) =>
            DebugTrace.Write(error == null ? "command" : "error", $"{_name} {(error == null ? "finished" : $"failed: {DebugTrace.Format(error)}")} in {_clock.ElapsedMilliseconds} ms");
        }
    }
#endif
