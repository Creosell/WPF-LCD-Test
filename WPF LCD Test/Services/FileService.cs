using System.Diagnostics;
using System.IO;
using System.Text.Json;
using WPF_LCD_Test.Interfaces;
using WPF_LCD_Test.Models;
using WPF_LCD_Test.Wrappers;
using static WPF_LCD_Test.Resources.Resources;

namespace WPF_LCD_Test.Services
    {
    /// <summary>
    /// Service for handling file operations including JSON serialization, CSV export, and external program execution.
    /// </summary>
    public class FileService : IFileService
        {
        private readonly string _workFolerName = "data";
        private string _applicationBasePath;
        private string _baseFolderPath;

        private readonly IDirectory _directory;
        private readonly IFile _file;
        private readonly IPath _path;
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
        public string WorkFolderName => _workFolerName;

        /// <summary>
        /// Occurs when status message needs to be reported.
        /// </summary>
        public event EventHandler<string>? StatusMessage;

        /// <summary>
        /// Occurs when save operation completes.
        /// </summary>
        public event EventHandler<bool>? SaveOperationCompleted;

        /// <summary>
        /// Initializes a new instance of FileService with default dependencies.
        /// </summary>
        public FileService() : this(new DirectoryWrapper(), new FileWrapper(), new PathWrapper(), AppDomain.CurrentDomain.BaseDirectory, LocalizationService.Instance)
            {
            }

        /// <summary>
        /// Initializes a new instance of FileService with dependency injection.
        /// </summary>
        /// <param name="directory">Directory wrapper for directory operations.</param>
        /// <param name="file">File wrapper for file operations.</param>
        /// <param name="path">Path wrapper for path operations.</param>
        /// <param name="applicationBasePath">Base path for application data.</param>
        /// <param name="localizationService">Service for localized messages.</param>
        public FileService(IDirectory directory, IFile file, IPath path, string applicationBasePath, ILocalizationService localizationService)
            {
            (_directory, _file, _path, _applicationBasePath, _localizationService) = (directory, file, path, applicationBasePath, localizationService);
            InitializeWorkingFolders();
            }

        /// <summary>
        /// Initializes working directory structure, creates base folder if it doesn't exist.
        /// </summary>
        public void InitializeWorkingFolders()
            {
            try
                {
                _baseFolderPath = _path.Combine(_applicationBasePath, _workFolerName);

                if (!_directory.Exists(_baseFolderPath))
                    {
                    _directory.CreateDirectory(_baseFolderPath);
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
                string filePath = _path.Combine(BaseFolderPath, fileName);

                string jsonString = JsonSerializer.Serialize(device, _saveSerializerOptions);

                await _file.WriteAllTextAsync(filePath, jsonString);

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
                string serialNumberFolderPath = _path.Combine(BaseFolderPath, serialNumber);

                if (!_directory.Exists(serialNumberFolderPath))
                    {
                    _directory.CreateDirectory(serialNumberFolderPath);
                    StatusMessage?.Invoke(this, $"{WorkFolderCreatedForSN}: {serialNumberFolderPath}");
                    }

                string fileName = $"{measurementLocationName}.csv";
                string filePath = _path.Combine(serialNumberFolderPath, fileName);

                await _file.WriteAllTextAsync(filePath, measurementCsvString);
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