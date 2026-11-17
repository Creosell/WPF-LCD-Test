namespace WPF_LCD_Test.Models
    {
    /// <summary>
    /// Identifies the colorimeter and channel a measurement was taken with.
    /// </summary>
    /// <param name="Model">Analyzer model display name, e.g. "CA-410".</param>
    /// <param name="ProbeSN">Serial number of the measuring probe.</param>
    /// <param name="Channel">Calibration channel active during the measurement.</param>
    public record ColorimeterInfo(string Model, string ProbeSN, int Channel);
    }
