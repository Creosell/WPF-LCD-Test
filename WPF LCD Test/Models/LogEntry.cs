namespace WPF_LCD_Test.Models
    {
    /// <summary>
    /// Importance of a log entry, used to color it and to count the entries the operator has not seen.
    /// </summary>
    public enum LogSeverity
        {
        /// <summary>A regular event.</summary>
        Info,

        /// <summary>Something needs attention (a rejected measurement, a value below the typical one).</summary>
        Warning,

        /// <summary>An operation failed or a value is outside the specification.</summary>
        Error
        }

    /// <summary>
    /// One line of the application log.
    /// </summary>
    /// <param name="Time">When the event happened.</param>
    /// <param name="Message">Localized text.</param>
    /// <param name="Severity">Importance of the entry.</param>
    public sealed record LogEntry(DateTime Time, string Message, LogSeverity Severity)
        {
        /// <summary>
        /// Gets the time of the entry as shown in the log.
        /// </summary>
        public string TimeText => Time.ToString("HH:mm:ss");
        }
    }
