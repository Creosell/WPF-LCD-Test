using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;
using System.Text.Json;
using WPF_LCD_Test.Interfaces;
using WPF_LCD_Test.Models;
using static WPF_LCD_Test.Resources.Resources;

namespace WPF_LCD_Test.Services
    {
    /// <summary>
    /// Service for handling file operations including JSON serialization, CSV export, and external program execution.
    /// </summary>
    public class FileService : IFileService
        {
        private string _baseFolderPath;

        private readonly IFileSystem _fileSystem;
        private readonly IPathProvider _pathProvider;
        private readonly ILocalizationService _localizationService;

        private static readonly JsonSerializerOptions _saveSerializerOptions = new()
            {
            WriteIndented = true
            };

        /// <summary>
        /// Gets the base folder path for storing test data.
        /// </summary>
        public string BaseFolderPath => _baseFolderPath;

        /// <summary>
        /// Gets the working folder name.
        /// </summary>
        public string WorkFolderName => "data";

        /// <summary>
        /// Occurs when status message needs to be reported.
        /// </summary>
        public event EventHandler<string>? StatusMessage;

        /// <summary>
        /// Occurs when save operation completes.
        /// </summary>
        public event EventHandler<bool>? SaveOperationCompleted;

        /// <summary>
        /// Initializes a new instance of FileService with dependency injection.
        /// </summary>
        /// <param name="fileSystem">File system abstraction for file operations.</param>
        /// <param name="pathProvider">Path provider for application directories.</param>
        /// <param name="localizationService">Service for localized messages.</param>
        public FileService(IFileSystem fileSystem, IPathProvider pathProvider, ILocalizationService localizationService)
            {
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _pathProvider = pathProvider ?? throw new ArgumentNullException(nameof(pathProvider));
            _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
            InitializeWorkingFolders();
            }

        /// <summary>
        /// Initializes working directory structure, creates base folder if it doesn't exist.
        /// </summary>
        public void InitializeWorkingFolders()
            {
            try
                {
                _baseFolderPath = _pathProvider.DataDirectory;

                if (!_fileSystem.Directory.Exists(_baseFolderPath))
                    {
                    _fileSystem.Directory.CreateDirectory(_baseFolderPath);
                    StatusMessage?.Invoke(this, $"{WorkFolderCreated}: {_baseFolderPath}");
                    }
                }
            catch (Exception ex)
                {
                StatusMessage?.Invoke(this, $"{WorkingFolderInitErr}: {ex.Message}");
                }
            }

        /// <summary>
        /// Asynchronously saves device measurement data to JSON file.
        /// </summary>
        /// <param name="device">Device under test with measurement data.</param>
        /// <returns>True if save succeeded, false otherwise.</returns>
        public async Task<bool> SaveDeviceDataToJsonAsync(DeviceUnderTest? device)
            {
            if (device == null)
                {
                StatusMessage?.Invoke(this, $"{SaveJSONErrDeviceIsEmpty}");
                SaveOperationCompleted?.Invoke(this, false);
                return false;
                }

            try
                {
                string fileName = $"{device.SerialNumber}.json";
                string filePath = _fileSystem.Path.Combine(BaseFolderPath, fileName);

                string jsonString = JsonSerializer.Serialize(device, _saveSerializerOptions);

                await _fileSystem.File.WriteAllTextAsync(filePath, jsonString);

                StatusMessage?.Invoke(this, $"{ResultsForSN} {device.SerialNumber} {SavedToJSON}: {filePath}");
                SaveOperationCompleted?.Invoke(this, true);
                return true;
                }
            catch (Exception ex)
                {
                StatusMessage?.Invoke(this, $"{SaveJSONErrForSN} {device.SerialNumber}: {ex.Message}");
                SaveOperationCompleted?.Invoke(this, false);
                return false;
                }
            }

        /// <summary>
        /// Asynchronously saves single measurement data to CSV file in device-specific subfolder.
        /// </summary>
        /// <param name="measurementCsvString">CSV formatted measurement data.</param>
        /// <param name="measurementLocationName">Measurement location identifier.</param>
        /// <param name="serialNumber">Device serial number.</param>
        /// <returns>True if save succeeded, false otherwise.</returns>
        public async Task<bool> SaveMeasurementToCsvAsync(string measurementCsvString, string measurementLocationName, string serialNumber)
            {
            if (string.IsNullOrWhiteSpace(serialNumber))
                {
                StatusMessage?.Invoke(this, $"{CsvSnErr}");
                return false;
                }
            if (string.IsNullOrWhiteSpace(measurementCsvString))
                {
                StatusMessage?.Invoke(this, $"{CsvDataErr}");
                return false;
                }
            if (string.IsNullOrWhiteSpace(measurementLocationName))
                {
                StatusMessage?.Invoke(this, $"{CsvLocationErr}");
                return false;
                }

            try
                {
                string serialNumberFolderPath = _fileSystem.Path.Combine(BaseFolderPath, serialNumber);

                if (!_fileSystem.Directory.Exists(serialNumberFolderPath))
                    {
                    _fileSystem.Directory.CreateDirectory(serialNumberFolderPath);
                    StatusMessage?.Invoke(this, $"{WorkFolderCreatedForSN}: {serialNumberFolderPath}");
                    }

                string fileName = $"{measurementLocationName}.csv";
                string filePath = _fileSystem.Path.Combine(serialNumberFolderPath, fileName);

                await _fileSystem.File.WriteAllTextAsync(filePath, measurementCsvString);
                return true;
                }
            catch (Exception ex)
                {
                StatusMessage?.Invoke(this, $"{ErrCSV} '{measurementLocationName}' (SN {serialNumber}): {ex.Message}");
                return false;
                }
            }

        /// <summary>
        /// Executes external program from application base directory.
        /// </summary>
        /// <param name="executableName">Name of executable file to run.</param>
        /// <returns>True if program started successfully, false otherwise.</returns>
        public bool RunExternalProgram(string executableName)
            {
            string executablePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, executableName);

            if (!File.Exists(executablePath)) return false;

            try
                {
                Process.Start(executablePath);
                return true;
                }
            catch
                {
                return false;
                }
            }
        }
    }