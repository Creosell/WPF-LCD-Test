namespace WPF_LCD_Test.Models
    {
    /// <summary>
    /// Represents a single logical upload batch destined for a unique remote folder.
    /// </summary>
    public class UploadReportItem
        {
        /// <summary>
        /// Gets or initializes the target remote directory path (e.g., "SCT/Results/DeviceName/Date/").
        /// </summary>
        public required string ReportRemoteDirectory { get; init; }

        /// <summary>
        /// Gets or initializes the list of local file paths to upload (ZIP, HTML, PDF, etc.).
        /// </summary>
        public required List<string> LocalFilesToUpload { get; init; } = new();
        }
    }