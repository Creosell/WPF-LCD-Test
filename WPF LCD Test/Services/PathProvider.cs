using System.IO;
using WPF_LCD_Test.Interfaces;

namespace WPF_LCD_Test.Services
{
    /// <summary>
    /// Provides application directory paths for centralized path management.
    /// </summary>
    public class PathProvider : IPathProvider
    {
        private readonly string _baseDirectory;

        /// <summary>
        /// Initializes a new instance of PathProvider with specified base directory.
        /// </summary>
        /// <param name="baseDirectory">Base directory path. If null, uses AppDomain.CurrentDomain.BaseDirectory.</param>
        public PathProvider(string baseDirectory = null)
        {
            _baseDirectory = baseDirectory ?? AppDomain.CurrentDomain.BaseDirectory;
        }

        /// <summary>
        /// Gets the application base directory path.
        /// </summary>
        public string BaseDirectory => _baseDirectory;

        /// <summary>
        /// Gets the device configuration files directory path.
        /// </summary>
        public string ConfigDirectory => Path.Combine(_baseDirectory, "config", "device_configs");

        /// <summary>
        /// Gets the measurement data directory path.
        /// </summary>
        public string DataDirectory => Path.Combine(_baseDirectory, "data");
    }
}
