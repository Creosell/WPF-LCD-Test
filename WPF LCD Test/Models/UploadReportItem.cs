using System.Collections.Generic;

namespace WPF_LCD_Test.Models
{
    // Model representing a single logical upload batch destined for a unique remote folder.
    public class UploadReportItem
    {
        // Target remote directory path (e.g., "SCT/Results/SDX-43U4169_20251118/")
        public required string ReportRemoteDirectory { get; init; }

        // List of all local files to upload (ZIP, HTML, PDF, etc.)
        public required List<string> LocalFilesToUpload { get; init; } = new();
    }
}