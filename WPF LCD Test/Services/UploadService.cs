using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WPF_LCD_Test.Interfaces;
using WPF_LCD_Test.Models;
using WPF_LCD_Test.Wrappers;
using static WPF_LCD_Test.Resources.Resources; // Access to localized strings

namespace WPF_LCD_Test.Services
{
    // Service class responsible for scanning local reports and uploading them via external CLI.
    public partial class UploadService : IUploadService
    {
        // Dependencies
        private readonly IDirectory _directory;
        private readonly IPath _path;

        // Configuration
        private const string UploadCliName = "Nextcloud_CLI.exe";
        private const string ArchiveFolderName = "report_archive";
        private const string ResultsFolderName = "results";
        private const string RemoteBasePath = "SCT"; // Fixed part of the remote path

        // Regex pattern to parse the filename: (Group 1: DeviceName)_(Group 2: YYYYMMDD_HHMM).zip
        private const string FilenameRegexPattern = @"^(.+?)_(\d{8}_\d{4})\.zip$";

        public event EventHandler<string>? StatusMessage;

        // Constructor for Dependency Injection (DI)
        public UploadService(IDirectory directory, IPath path)
        {
            _directory = directory;
            _path = path;
        }

        // Default Constructor uses production wrappers
        public UploadService() : this(new DirectoryWrapper(), new PathWrapper()) { }


        // --- ОБНОВЛЕННЫЙ МЕТОД: Управляет параллельной выгрузкой ---
        public async Task<bool> UploadReportsAsync()
        {
            var uploadItems = ScanLocalFolders();

            if (!uploadItems.Any())
            {
                StatusMessage?.Invoke(this, "No reports found for upload.");
                return true;
            }

            // 1. Создаем список задач (Tasks) для параллельного выполнения
            var uploadTasks = new List<Task<bool>>();

            foreach (var batch in uploadItems)
            {
                // Для каждого файла в пакете создаем отдельную задачу выгрузки
                foreach (var localPath in batch.LocalFilesToUpload)
                {
                    var uploadTask = ExecuteSingleFileUploadAsync(localPath, batch.ReportRemoteDirectory);
                    uploadTasks.Add(uploadTask);
                }
            }

            StatusMessage?.Invoke(this, $"Starting parallel upload of {uploadTasks.Count} files...");

            // 2. Ожидаем завершения ВСЕХ задач одновременно
            // Результатом будет массив bool, указывающий на успех каждой отдельной выгрузки.
            bool[] results = await Task.WhenAll(uploadTasks);

            // 3. Анализ результатов
            bool allSucceeded = results.All(r => r);
            int failedCount = results.Count(r => !r);

            if (allSucceeded)
            {
                StatusMessage?.Invoke(this, $"Upload process finished successfully. Total files uploaded: {results.Length}.");
            }
            else
            {
                StatusMessage?.Invoke(this, $"Upload process finished with failures. Total files failed: {failedCount}.");
            }

            return allSucceeded;
        }

        // Collects all files (ZIP, HTML, PDF) related to a single timestamp/device into batches.
        private List<UploadReportItem> ScanLocalFolders()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var archivePath = _path.Combine(baseDir, ArchiveFolderName);
            var resultsPath = _path.Combine(baseDir, ResultsFolderName);

            var reports = new List<UploadReportItem>();
            var regex = new Regex(FilenameRegexPattern, RegexOptions.IgnoreCase);

            // Dictionary to group all files by their base name before creating UploadReportItem objects.
            // Key: Base FileName (e.g., SDX-43U4169_CH19_20251118_1700)
            // Value: UploadReportItem (the batch container)
            var uploadBatches = new Dictionary<string, UploadReportItem>();

            // 1. Scan ZIP archives (Primary source for metadata and batch creation)
            if (!_directory.Exists(archivePath))
            {
                StatusMessage?.Invoke(this, $"Archive folder not found: {archivePath}");
                return reports;
            }

            // Find all ZIP archives
            var zipFiles = _directory.EnumerateFiles(archivePath, "*.zip", SearchOption.TopDirectoryOnly);

            foreach (var zipFilePath in zipFiles)
            {
                var zipFileName = _path.GetFileName(zipFilePath);
                var match = regex.Match(zipFileName);

                if (match.Success)
                {
                    var deviceIdentifier = match.Groups[1].Value; // e.g., SDX-43U4169_CH19
                    var timestamp = match.Groups[2].Value;
                    var datePart = timestamp.Substring(0, 8);

                    // Base filename for grouping (without extension)
                    var baseFileName = zipFileName.Replace(".zip", "");

                    // LOGIC TO DETERMINE HIERARCHICAL FOLDERS:
                    string deviceNamePart = deviceIdentifier;
                    string configOrderPart = "General";
                    int lastUnderscore = deviceIdentifier.LastIndexOf('_');
                    if (lastUnderscore > 0)
                    {
                        deviceNamePart = deviceIdentifier.Substring(0, lastUnderscore);
                        configOrderPart = deviceIdentifier.Substring(lastUnderscore + 1);
                    }

                    // BUILD REMOTE PATH: SCT/Results/{DeviceNamePart}/{ConfigOrderPart}/{Date}/
                    var remoteDir = $"{RemoteBasePath}/{deviceNamePart}/{configOrderPart}/{datePart}/";

                    // Initialize the batch item for this unique device/date/time combination
                    var uploadItem = new UploadReportItem
                    {
                        ReportRemoteDirectory = remoteDir,
                        LocalFilesToUpload = new List<string>() // Ensure it's a new list
                    };

                    // A. Add the primary ZIP archive
                    uploadItem.LocalFilesToUpload.Add(zipFilePath);

                    // Store the item using its unique base filename as the key
                    uploadBatches[baseFileName] = uploadItem;

                    StatusMessage?.Invoke(this, $"Created batch for '{baseFileName}'");
                }
            }

            // 2. Scan Results Folder (Secondary source for loose files like HTML/PDF)
            if (_directory.Exists(resultsPath))
            {
                // Search for all loose files that match the base filename pattern
                var baseFileNames = uploadBatches.Keys;

                foreach (var baseName in baseFileNames)
                {
                    // Find files starting with {baseName}.* (e.g., .html, .pdf)
                    var resultFiles = _directory.EnumerateFiles(
                        resultsPath,
                        $"{baseName}.*",
                        SearchOption.TopDirectoryOnly
                    ).ToList();

                    // Add these loose files to the corresponding batch
                    if (uploadBatches.TryGetValue(baseName, out var item))
                    {
                        item.LocalFilesToUpload.AddRange(resultFiles);
                    }
                }
            }

            // Convert the dictionary values back to a list of batches
            reports = uploadBatches.Values.ToList();

            StatusMessage?.Invoke(this, $"Ready to upload {reports.Count} batches, containing {reports.Sum(b => b.LocalFilesToUpload.Count)} files.");
            return reports;
        }

        // --- НОВЫЙ МЕТОД: Создает и запускает Task для выгрузки одного файла ---
        private async Task<bool> ExecuteSingleFileUploadAsync(string localPathArg, string remoteDir)
        {
            var fileName = _path.GetFileName(localPathArg);

            // Remote path is the directory + filename
            var remoteFullPath = remoteDir + fileName;
            var cleanRemoteFullPath = remoteFullPath.Replace('\\', '/'); // CRITICAL FIX for WebDAV

            // Arguments: upload -l LOCAL_FILE -r REMOTE_FULL_PATH
            var arguments = $"upload -l \"{localPathArg}\" -r \"{cleanRemoteFullPath}\" -f";

            StatusMessage?.Invoke(this, $"Starting upload of {fileName}");

            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = UploadCliName,
                        Arguments = arguments,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                // Запуск процесса асинхронно
                await Task.Run(() => process.Start());
                await process.WaitForExitAsync();

                if (process.ExitCode == 0)
                {
                    StatusMessage?.Invoke(this, $"Upload successful for {fileName}");
                    return true;
                }
                else
                {
                    string errorOutput = await process.StandardError.ReadToEndAsync();
                    StatusMessage?.Invoke(this, $"CLI Error for {fileName} (Code {process.ExitCode}): {errorOutput}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                StatusMessage?.Invoke(this, $"Failed to execute CLI for {fileName}: {ex.Message}");
                return false;
            }
        }


    }
}