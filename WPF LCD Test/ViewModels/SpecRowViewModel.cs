using WPF_LCD_Test.Models;

namespace WPF_LCD_Test.ViewModels
    {
    /// <summary>
    /// One row of the device info table: a metric with its value, specification and status.
    /// </summary>
    /// <param name="Name">Localized metric name.</param>
    /// <param name="ValueText">Formatted value, or a dash when it is not available yet.</param>
    /// <param name="SpecText">Formatted specification bounds.</param>
    /// <param name="Status">Check status that selects the row color.</param>
    /// <param name="ToolTip">What the row waits for or what is wrong; empty when there is nothing to add.</param>
    public sealed record SpecRowViewModel(string Name, string ValueText, string SpecText, SpecStatus Status, string ToolTip);
    }
