namespace WPF_LCD_Test.Interfaces
{
    /// <summary>
    /// Provides application directory paths for centralized path management.
    /// </summary>
    public interface IPathProvider
    {
        /// <summary>
        /// Gets the application base directory path.
        /// </summary>
        string BaseDirectory { get; }

        /// <summary>
        /// Gets the device configuration files directory path.
        /// </summary>
        string ConfigDirectory { get; }

        /// <summary>
        /// Gets the measurement data directory path.
        /// </summary>
        string DataDirectory { get; }
    }
}
