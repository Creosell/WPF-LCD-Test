using System.Diagnostics;
using System.IO;
using System.Text.Json;
using WPF_LCD_Test.Interfaces;
using WPF_LCD_Test.Models;
using WPF_LCD_Test.Wrappers;
using static WPF_LCD_Test.Resources.Resources;

namespace WPF_LCD_Test.Services
{
    // Implementation of file operations service. Dependencies are injected via traditional constructor.
    public class FileService : IFileService
    {
        // Private fields for internal state and paths.
        private readonly string _workFolerName = "data";
        private string _applicationBasePath;
        private string _baseFolderPath;

        // Injected dependencies (wrappers for file/directory system).
        private readonly IDirectory _directory;
        private readonly IFile _file;
        private readonly IPath _path;
        private readonly ILocalizationService _localizationService;

        // Public properties with concise expression bodies.
        public string BaseFolderPath => _baseFolderPath;
        public string WorkFolderName => _workFolerName;

        // Options for JSON serialization (pretty-printed output).
        private static readonly JsonSerializerOptions _saveSerializerOptions = new()
        {
            WriteIndented = true
        };

        public event EventHandler<string>? StatusMessage;
        public event EventHandler<bool>? SaveOperationCompleted;

        // Default constructor calls the parameterized constructor with production dependencies.
        public FileService() : this(new DirectoryWrapper(), new FileWrapper(), new PathWrapper(), AppDomain.CurrentDomain.BaseDirectory, LocalizationService.Instance)
        {
        }

        // Parameterized constructor for Dependency Injection (DI).
        public FileService(IDirectory directory, IFile file, IPath path, string applicationBasePath, ILocalizationService localizationService)
        {
            // Use tuple assignment for concise dependency initialization.
            (_directory, _file, _path, _applicationBasePath, _localizationService) = (directory, file, path, applicationBasePath, localizationService);

            InitializeWorkingFolders();
        }

        // Initializes the application's working directory.
        public void InitializeWorkingFolders()
        {
            try
            {
                _baseFolderPath = _path.Combine(_applicationBasePath, _workFolerName);

                // Creates the base directory if it doesn't exist.
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

        // Sets the current thread culture for correct localization in async operations.
        private static void CheckCurrentAppLanguage()
        {
            var culture = LocalizationService.Instance.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
        }

        // Asynchronously saves all device measurement data to a single JSON file.
        public async Task<bool> SaveDeviceDataToJsonAsync(DeviceUnderTest? device)
        {
            CheckCurrentAppLanguage();
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

        // Asynchronously saves a single measurement's data string to a CSV file.
        public async Task<bool> SaveMeasurementToCsvAsync(string measurementCsvString, string measurementLocationName, string serialNumber)
        {
            CheckCurrentAppLanguage();

            // Input validation checks for required fields.
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

                // Create device-specific subfolder if it doesn't exist.
                if (!_directory.Exists(serialNumberFolderPath))
                {
                    _directory.CreateDirectory(serialNumberFolderPath);
                    StatusMessage?.Invoke(this, $"{WorkFolderCreatedForSN}: {serialNumberFolderPath}");
                }

                string fileName = $"{measurementLocationName}.csv";
                string filePath = _path.Combine(serialNumberFolderPath, fileName);

                await _file.WriteAllTextAsync(filePath, measurementCsvString);
                return true; // Return success inside the try block for clearer logic flow.
            }
            catch (Exception ex)
            {
                StatusMessage?.Invoke(this, $"{ErrCSV} '{measurementLocationName}' (SN {serialNumber}): {ex.Message}");
                return false;
            }
        }

        // Executes an external program located in the application's base directory.
        public bool RunExternalProgram(string executableName)
        {
            // Conciseness: Use early returns for checks.
            string executablePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, executableName);

            if (!File.Exists(executablePath)) return false;

            try
            {
                Process.Start(executablePath);
                return true;
            }
            catch // Catching any exception during process start (e.g., file access denied).
            {
                return false;
            }
        }
    }
}