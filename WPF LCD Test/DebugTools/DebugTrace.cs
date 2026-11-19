#if DEBUG
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace WPF_LCD_Test.DebugTools
    {
    /// <summary>
    /// Debug-build trace of everything that happens in the application: one timestamped line per event, written to
    /// <c>logs/debug_yyyyMMdd_HHmmss.log</c> next to the executable and to the debugger output. Compiled only in the Debug
    /// configuration. The events come from a few cross-cutting hooks (<see cref="UiEventTracer"/>, <see cref="ViewModelTracer"/>,
    /// <see cref="TracingProxy"/>, <see cref="RelayCommandTrace"/>), so the code of the application carries no log calls of its own.
    /// </summary>
    public static class DebugTrace
        {
        private const int KeptFiles = 10;
        private const int MaxValueLength = 200;
        private static readonly object Gate = new();
        private static StreamWriter? _file;

        /// <summary>
        /// Gets the path of the trace file of this run, or null before <see cref="Start"/>.
        /// </summary>
        public static string? FilePath { get; private set; }

        /// <summary>
        /// Opens the trace file of this run and removes the oldest ones.
        /// </summary>
        public static void Start()
            {
            lock (Gate)
                {
                if (_file != null) return;

                try
                    {
                    string folder = Path.Combine(AppContext.BaseDirectory, "logs");
                    Directory.CreateDirectory(folder);
                    FilePath = Path.Combine(folder, $"debug_{DateTime.Now:yyyyMMdd_HHmmss}.log");
                    _file = new StreamWriter(FilePath, append: false, new UTF8Encoding(false)) { AutoFlush = true };

                    foreach (var old in Directory.GetFiles(folder, "debug_*.log").OrderByDescending(f => f).Skip(KeptFiles))
                        {
                        try { File.Delete(old); } catch (IOException) { }
                        }
                    }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                    {
                    Debug.WriteLine($"[trace] cannot open the trace file: {ex.Message}");
                    }
                }

            Write("app", $"trace started: {FilePath}");
            }

        /// <summary>
        /// Writes one line of the trace.
        /// </summary>
        /// <param name="category">Short kind of the event (ui, command, call, state, binding, error, app).</param>
        /// <param name="message">What happened.</param>
        public static void Write(string category, string message)
            {
            string line = $"{DateTime.Now:HH:mm:ss.fff} [T{Environment.CurrentManagedThreadId:00}] {category,-7} {message}";
            Debug.WriteLine(line);
            lock (Gate)
                {
                try { _file?.WriteLine(line); }
                catch (IOException) { }
                }
            }

        /// <summary>
        /// Shortens a value to one readable token for a trace line: text in quotes, a collection by its size, a plain object by its type.
        /// </summary>
        /// <param name="value">The value to show.</param>
        /// <returns>The text of the value.</returns>
        public static string Format(object? value)
            {
            string text = value switch
                {
                null => "null",
                string s => $"\"{s}\"",
                Exception e => $"{e.GetType().Name}: {e.Message}",
                IDictionary d => $"{value.GetType().Name}[{d.Count}]",
                ICollection c => $"{value.GetType().Name}[{c.Count}]",
                Delegate d => DescribeDelegate(d),
                _ when value.GetType().IsPrimitive || value is Enum || value is decimal || value is DateTime || value is TimeSpan => Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? "",
                _ => HasOwnToString(value.GetType()) ? value.ToString() ?? "" : value.GetType().Name
                };

            text = text.Replace("\r", "\\r").Replace("\n", "\\n");
            return text.Length > MaxValueLength ? text[..MaxValueLength] + "…" : text;
            }

        /// <summary>
        /// Names the code a delegate runs: the type and the method, without the names the compiler makes for lambdas.
        /// </summary>
        /// <param name="handler">The delegate.</param>
        /// <returns>Text like <c>MeasurementViewModel.ExecuteMeasureAsync</c>.</returns>
        public static string DescribeDelegate(Delegate? handler)
            {
            if (handler == null) return "null";

            var method = handler.Method;
            var type = method.DeclaringType;
            while (type is { IsNested: true } && type.Name.Contains('<'))
                {
                type = type.DeclaringType;
                }

            string name = method.Name;
            int open = name.IndexOf('<');
            int close = name.IndexOf('>');
            if (open == 0 && close > 1) name = name[1..close];
            return name.StartsWith('.') ? $"{type?.Name} (lambda in {name})" : $"{type?.Name}.{name}";
            }

        private static bool HasOwnToString(Type type) =>
            type.GetMethod(nameof(ToString), Type.EmptyTypes)?.DeclaringType != typeof(object);
        }
    }
#endif
