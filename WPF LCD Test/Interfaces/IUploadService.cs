namespace WPF_LCD_Test.Interfaces
    {
    /// <summary>
    /// Service interface for uploading test reports to remote storage.
    /// </summary>
    public interface IUploadService
        {
        /// <summary>
        /// Occurs when status message needs to be reported during upload process.
        /// </summary>
        event EventHandler<string> StatusMessage;

        /// <summary>
        /// Scans local folders, creates upload batches, and uploads reports to remote storage.
        /// </summary>
        /// <returns>True if all uploads completed successfully, false if any upload failed.</returns>
        Task<bool> UploadReportsAsync();
        }
    }