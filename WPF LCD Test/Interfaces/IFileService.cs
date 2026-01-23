using WPF_LCD_Test.Models;

namespace WPF_LCD_Test.Interfaces
    {
    /// <summary>
    /// Service interface for file operations including JSON serialization, CSV export, and external program execution.
    /// </summary>
    public interface IFileService
        {
        /// <summary>
        /// Occurs when status message needs to be reported.
        /// </summary>
        event EventHandler<string> StatusMessage;

        /// <summary>
        /// Occurs when save operation completes.
        /// </summary>
        event EventHandler<bool> SaveOperationCompleted;

        /// <summary>
        /// Gets the base folder path for storing test data.
        /// </summary>
        string BaseFolderPath { get; }

        /// <summary>
        /// Gets the working folder name.
        /// </summary>
        string WorkFolderName { get; }

        /// <summary>
        /// Initializes working directory structure, creates base folder if it doesn't exist.
        /// </summary>
        void InitializeWorkingFolders();

        /// <summary>
        /// Asynchronously saves device measurement data to JSON file.
        /// </summary>
        /// <param name="device">Device under test with measurement data.</param>
        /// <returns>True if save succeeded, false otherwise.</returns>
        Task<bool> SaveDeviceDataToJsonAsync(DeviceUnderTest device);

        /// <summary>
        /// Asynchronously saves single measurement data to CSV file in device-specific subfolder.
        /// </summary>
        /// <param name="measurementCsvString">CSV formatted measurement data.</param>
        /// <param name="measurementLocationName">Measurement location identifier.</param>
        /// <param name="serialNumber">Device serial number.</param>
        /// <returns>True if save succeeded, false otherwise.</returns>
        Task<bool> SaveMeasurementToCsvAsync(string measurementCsvString, string measurementLocationName, string serialNumber);

        /// <summary>
        /// Executes external program from application base directory.
        /// </summary>
        /// <param name="executableName">Name of executable file to run.</param>
        /// <returns>True if program started successfully, false otherwise.</returns>
        bool RunExternalProgram(string executableName);
        }
    }