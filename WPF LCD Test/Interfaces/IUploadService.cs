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

        /// <summary>
        /// Cancels the running upload: the CLI processes are killed and <see cref="UploadReportsAsync"/> throws <see cref="OperationCanceledException"/>.
        /// Does nothing when no upload is running.
        /// </summary>
        void CancelUpload();

        /// <summary>
        /// Counts the report archives waiting for upload.
        /// </summary>
        /// <returns>Number of archives in the report archive folder that match the report naming.</returns>
        int GetPendingReportCount();
        }
    }