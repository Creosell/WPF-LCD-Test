using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
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
        private const string UploadedReportsFolderName = "uploaded_reports";
        private const string RemoteBasePath = "SCT";

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

        // --- UPDATED METHOD: Manages parallel upload and deletion ---
        public async Task<bool> UploadReportsAsync()
        {
            CheckCurrentAppLanguage();
            var uploadItems = ScanLocalFolders();

            if (!uploadItems.Any())
            {
                StatusMessage?.Invoke(this, NoReportsFoundForUpload);
                return true;
            }

            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var uploadedReportsDir = Path.Combine(baseDir, UploadedReportsFolderName);

            if (!Directory.Exists(uploadedReportsDir))
            {
                Directory.CreateDirectory(uploadedReportsDir);
            }

            // 1. Create a list of Tasks
            var uploadTasks = new List<Task<bool>>();

            foreach (var batch in uploadItems)
            {
                foreach (var localPath in batch.LocalFilesToUpload)
                {
                    // Capture variables for the closure
                    string currentPath = localPath;
                    string remoteDir = batch.ReportRemoteDirectory;

                    string destinationFolder = uploadedReportsDir;

                    // Define a task that includes Upload AND Deletion logic
                    var processingTask = Task.Run(async () =>
                    {
                        // A. Execute the upload
                        bool success = await ExecuteSingleFileUploadAsync(currentPath, remoteDir);

                        // B. Delete local file if upload succeeded
                        if (success)
                        {
                            try
                            {
                                if (File.Exists(currentPath))
                                {
                                    string fileName = Path.GetFileName(currentPath);
                                    string extension = Path.GetExtension(currentPath).ToLower();

                                    if (extension == ".zip")
                                    {
                                        string destPath = Path.Combine(destinationFolder, fileName);

                                        if (File.Exists(destPath))
                                        {
                                            File.Delete(destPath);
                                        }

                                        File.Move(currentPath, destPath);
                                        Debug.Print($"Moved archive to: {destPath}");
                                    }
                                    else
                                    {
                                        File.Delete(currentPath);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                StatusMessage?.Invoke(this, $"{WarningFailedToCleanup} {Path.GetFileName(currentPath)}: {ex.Message}");
                            }
                        }

                        return success;
                    });

                    uploadTasks.Add(processingTask);
                }
            }

            StatusMessage?.Invoke(this, $"{StartingParallelUploadOf} {uploadTasks.Count} {Files}...");

            // 2. Wait for ALL tasks to complete (upload + deletion)
            bool[] results = await Task.WhenAll(uploadTasks);

            // 3. Analyze results
            bool allSucceeded = results.All(r => r);
            int failedCount = results.Count(r => !r);

            if (allSucceeded)
            {
                StatusMessage?.Invoke(this, $"{UploadedSuccessfully}: {results.Length}.");
                StatusMessage?.Invoke(this, $"{CopyOfUploadedArchivesInFolder}: '{UploadedReportsFolderName}'.");
            }
            else
            {
                StatusMessage?.Invoke(this, $"{UploadFinishedWithFail}: {failedCount}.");
            }

            return allSucceeded;
        }

        // Collects all files (ZIP, HTML, PDF) related to a single timestamp/device into batches.
        private List<UploadReportItem> ScanLocalFolders()
        {
            CheckCurrentAppLanguage();
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
                StatusMessage?.Invoke(this, $"{ArchiveFolderNotFound}: {archivePath}");
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

                    StatusMessage?.Invoke(this, $"{CreatedBatchFor} '{baseFileName}'");
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
            if (reports.Count>0)
            {
                StatusMessage?.Invoke(this, $"{ReadyToUpload} {reports.Count} {Batches}, {Containing} {reports.Sum(b => b.LocalFilesToUpload.Count)} {Files}.");
            }

            return reports;
        }

        // --- НОВЫЙ МЕТОД: Создает и запускает Task для выгрузки одного файла ---
        private async Task<bool> ExecuteSingleFileUploadAsync(string localPathArg, string remoteDir)
        {
            CheckCurrentAppLanguage();
            var fileName = _path.GetFileName(localPathArg);

            // Remote path is the directory + filename
            var remoteFullPath = remoteDir + fileName;
            var cleanRemoteFullPath = remoteFullPath.Replace('\\', '/'); // CRITICAL FIX for WebDAV

            // Arguments: upload -l LOCAL_FILE -r REMOTE_FULL_PATH
            var arguments = $"upload -l \"{localPathArg}\" -r \"{cleanRemoteFullPath}\" -f";

            StatusMessage?.Invoke(this, $"{StartingUploadOf} {fileName}");

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
                    StatusMessage?.Invoke(this, $"{UploadSuccessfulFor} {fileName}");
                    return true;
                }
                else
                {
                    string errorOutput = await process.StandardError.ReadToEndAsync();
                    StatusMessage?.Invoke(this, $"{CLI_ErrorFor} {fileName} ({Code} {process.ExitCode}): {errorOutput}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                StatusMessage?.Invoke(this, $"{FailedToExecuteCLI} {fileName}: {ex.Message}");
                return false;
            }
        }
        // Helper method to ensure the correct culture for resource strings on background threads.
        private static void CheckCurrentAppLanguage()
            {
            var culture = LocalizationService.Instance.CurrentCulture;

            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
            }

        }
}