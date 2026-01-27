using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using WPF_LCD_Test.Interfaces;
using WPF_LCD_Test.Models;
using static WPF_LCD_Test.Resources.Resources;

namespace WPF_LCD_Test.Services
    {
    /// <summary>
    /// Service for uploading test reports to remote storage using Nextcloud CLI.
    /// </summary>
    public partial class UploadService : IUploadService
        {
        private readonly IFileSystem _fileSystem;
        private readonly IPathProvider _pathProvider;
        private readonly LogHandler _logHandler;

        private const string UploadCliName = "Nextcloud_CLI.exe";
        private const string ArchiveFolderName = "report_archive";
        private const string ResultsFolderName = "results";
        private const string UploadedReportsFolderName = "uploaded_reports";
        private const string RemoteBasePath = "SCT";
        private const string FilenameRegexPattern = @"^(.+?)_(\d{8}_\d{4})\.zip$";

        public event EventHandler<string>? StatusMessage;

        /// <summary>
        /// Initializes a new instance of UploadService with dependency injection.
        /// </summary>
        /// <param name="fileSystem">File system abstraction for file operations.</param>
        /// <param name="pathProvider">Path provider for application directories.</param>
        /// <param name="localizationService">Service for localized messages.</param>
        public UploadService(IFileSystem fileSystem, IPathProvider pathProvider, ILocalizationService localizationService)
            {
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _pathProvider = pathProvider ?? throw new ArgumentNullException(nameof(pathProvider));

            if (localizationService == null) throw new ArgumentNullException(nameof(localizationService));

            _logHandler = new LogHandler(localizationService, (message) =>
            {
                StatusMessage?.Invoke(this, message);
            });
            }

        /// <summary>
        /// Reports status message through LogHandler with optional formatting arguments.
        /// </summary>
        /// <param name="messageOrKey">Resource key or message text.</param>
        /// <param name="args">Optional formatting arguments.</param>
        /// <param name="resourceName">Automatically captured resource name.</param>
        private void ReportStatus(string messageOrKey, object[]? args = null, [CallerArgumentExpression("messageOrKey")] string? resourceName = null)
            {
            _logHandler.Log(messageOrKey, args, resourceName);
            }

        /// <summary>
        /// Scans local folders, uploads all pending reports to remote storage, and moves uploaded files to archive.
        /// </summary>
        /// <returns>True if all uploads succeeded, false if any failed.</returns>
        public async Task<bool> UploadReportsAsync()
            {
            var uploadItems = ScanLocalFolders();

            if (!uploadItems.Any())
                {
                ReportStatus(NoReportsFoundForUpload);
                return true;
                }

            var baseDir = _pathProvider.BaseDirectory;
            var uploadedReportsDir = Path.Combine(baseDir, UploadedReportsFolderName);

            if (!Directory.Exists(uploadedReportsDir))
                {
                Directory.CreateDirectory(uploadedReportsDir);
                }

            var uploadTasks = new List<Task<bool>>();

            foreach (var batch in uploadItems)
                {
                foreach (var localPath in batch.LocalFilesToUpload)
                    {
                    string currentPath = localPath;
                    string remoteDir = batch.ReportRemoteDirectory;
                    string destinationFolder = uploadedReportsDir;

                    var processingTask = Task.Run(async () =>
                    {
                        bool success = await ExecuteSingleFileUploadAsync(currentPath, remoteDir);

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
                                        if (File.Exists(destPath)) File.Delete(destPath);
                                        File.Move(currentPath, destPath);
                                        }
                                    else
                                        {
                                        File.Delete(currentPath);
                                        }
                                    }
                                }
                            catch (Exception ex)
                                {
                                ReportStatus(WarningFailedToCleanup, [Path.GetFileName(currentPath), ex.Message]);
                                }
                            }
                        return success;
                    });

                    uploadTasks.Add(processingTask);
                    }
                }

            ReportStatus(StartingParallelUploadOf, [uploadTasks.Count]);

            bool[] results = await Task.WhenAll(uploadTasks);

            bool allSucceeded = results.All(r => r);
            int failedCount = results.Count(r => !r);

            if (allSucceeded)
                {
                ReportStatus(UploadedSuccessfully, [results.Length]);
                ReportStatus(CopyOfUploadedArchivesInFolder, [UploadedReportsFolderName]);
                }
            else
                {
                ReportStatus(UploadFinishedWithFail, [failedCount]);
                }

            return allSucceeded;
            }

        /// <summary>
        /// Scans archive and results folders for pending uploads, groups files by report identifier.
        /// </summary>
        /// <returns>List of upload items with local files and remote directory paths.</returns>
        private List<UploadReportItem> ScanLocalFolders()
            {
            var baseDir = _pathProvider.BaseDirectory;
            var archivePath = _fileSystem.Path.Combine(baseDir, ArchiveFolderName);
            var resultsPath = _fileSystem.Path.Combine(baseDir, ResultsFolderName);

            var reports = new List<UploadReportItem>();
            var regex = new Regex(FilenameRegexPattern, RegexOptions.IgnoreCase);
            var uploadBatches = new Dictionary<string, UploadReportItem>();

            if (!_fileSystem.Directory.Exists(archivePath))
                {
                ReportStatus(ArchiveFolderNotFound, [archivePath]);
                return reports;
                }

            var zipFiles = _fileSystem.Directory.EnumerateFiles(archivePath, "*.zip", SearchOption.TopDirectoryOnly);

            foreach (var zipFilePath in zipFiles)
                {
                var zipFileName = _fileSystem.Path.GetFileName(zipFilePath);
                var match = regex.Match(zipFileName);

                if (match.Success)
                    {
                    var deviceIdentifier = match.Groups[1].Value;
                    var timestamp = match.Groups[2].Value;
                    var datePart = timestamp.Substring(0, 8);
                    var baseFileName = zipFileName.Replace(".zip", "");

                    string deviceNamePart = deviceIdentifier;
                    string configOrderPart = "General";
                    int lastUnderscore = deviceIdentifier.LastIndexOf('_');
                    if (lastUnderscore > 0)
                        {
                        deviceNamePart = deviceIdentifier.Substring(0, lastUnderscore);
                        configOrderPart = deviceIdentifier.Substring(lastUnderscore + 1);
                        }

                    var remoteDir = $"{RemoteBasePath}/{deviceNamePart}/{configOrderPart}/{datePart}/";

                    var uploadItem = new UploadReportItem
                        {
                        ReportRemoteDirectory = remoteDir,
                        LocalFilesToUpload = new List<string>()
                        };

                    uploadItem.LocalFilesToUpload.Add(zipFilePath);
                    uploadBatches[baseFileName] = uploadItem;

                    ReportStatus(CreatedBatchFor, [baseFileName]);
                    }
                }

            if (_fileSystem.Directory.Exists(resultsPath))
                {
                var baseFileNames = uploadBatches.Keys;
                foreach (var baseName in baseFileNames)
                    {
                    var resultFiles = _fileSystem.Directory.EnumerateFiles(resultsPath, $"{baseName}.*", SearchOption.TopDirectoryOnly).ToList();
                    if (uploadBatches.TryGetValue(baseName, out var item))
                        {
                        item.LocalFilesToUpload.AddRange(resultFiles);
                        }
                    }
                }

            reports = uploadBatches.Values.ToList();
            if (reports.Count > 0)
                {
                ReportStatus(ReadyToUpload, [reports.Count]);
                }

            return reports;
            }

        /// <summary>
        /// Executes single file upload using Nextcloud CLI.
        /// </summary>
        /// <param name="localPathArg">Local file path to upload.</param>
        /// <param name="remoteDir">Remote directory path.</param>
        /// <returns>True if upload succeeded, false otherwise.</returns>
        private async Task<bool> ExecuteSingleFileUploadAsync(string localPathArg, string remoteDir)
            {
            var fileName = _fileSystem.Path.GetFileName(localPathArg);
            var remoteFullPath = remoteDir + fileName;
            var cleanRemoteFullPath = remoteFullPath.Replace('\\', '/');
            var arguments = $"upload -l \"{localPathArg}\" -r \"{cleanRemoteFullPath}\" -f";

            ReportStatus(StartingUploadOf, [fileName]);

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

                await Task.Run(() => process.Start());
                await process.WaitForExitAsync();

                if (process.ExitCode == 0)
                    {
                    ReportStatus(UploadSuccessfulFor, [fileName]);
                    return true;
                    }
                else
                    {
                    string errorOutput = await process.StandardError.ReadToEndAsync();
                    ReportStatus(CLI_ErrorFor, [fileName, process.ExitCode, errorOutput]);
                    return false;
                    }
                }
            catch (Exception ex)
                {
                ReportStatus(FailedToExecuteCLI, [fileName, ex.Message]);
                return false;
                }
            }
        }
    }