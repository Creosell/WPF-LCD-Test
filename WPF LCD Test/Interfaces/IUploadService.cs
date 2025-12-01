namespace WPF_LCD_Test.Interfaces
{
    public interface IUploadService
    {
        // Event for sending status messages back to the ViewModel/Log
        event EventHandler<string> StatusMessage;

        /// <summary>
        /// Scans local folders, creates UploadReportItem objects, and initiates the upload process.
        /// </summary>
        /// <param name="currentDeviceName">The name of the device being tested.</param>
        /// <returns>True if the entire process completed without critical errors.</returns>
        Task<bool> UploadReportsAsync();
    }
}