namespace WPF_LCD_Test.Interfaces
{
    /// <summary>
    /// Interface wrapper for Konica Minolta CA-200 color analyzer COM object.
    /// </summary>
    public interface IColorAnalyzer200 : IDisposable
    {
        /// <summary>
        /// Gets the single color analyzer instance.
        /// </summary>
        IColorAnalyzer SingleCa { get; }

        /// <summary>
        /// Automatically connects to the color analyzer device.
        /// </summary>
        void AutoConnect();
    }

    /// <summary>
    /// Interface wrapper for color analyzer device operations.
    /// </summary>
    public interface IColorAnalyzer
    {
        /// <summary>
        /// Gets the color analyzer probe instance.
        /// </summary>
        IColorAnalyzerProbe SingleProbe { get; }

        /// <summary>
        /// Gets the color analyzer memory interface.
        /// </summary>
        IColorAnalyzerMemory Memory { get; }

        /// <summary>
        /// Gets the analyzer number.
        /// </summary>
        int Number { get; }

        /// <summary>
        /// Gets the analyzer ID.
        /// </summary>
        string ID { get; }

        /// <summary>
        /// Gets the port ID for the connection.
        /// </summary>
        string PortID { get; }

        /// <summary>
        /// Performs zero calibration on the analyzer.
        /// </summary>
        void CalZero();

        /// <summary>
        /// Gets or sets the sync mode.
        /// </summary>
        int SyncMode { get; set; }

        /// <summary>
        /// Gets or sets the averaging mode.
        /// </summary>
        int AveragingMode { get; set; }

        /// <summary>
        /// Sets the analog range for measurement.
        /// </summary>
        /// <param name="Range1">First range value.</param>
        /// <param name="Range2">Second range value.</param>
        void SetAnalogRange(float Range1, float Range2);

        /// <summary>
        /// Gets or sets the display mode.
        /// </summary>
        int DisplayMode { get; set; }

        /// <summary>
        /// Performs a measurement operation.
        /// </summary>
        void Measure();
    }

    /// <summary>
    /// Interface wrapper for color analyzer probe that provides measurement data.
    /// </summary>
    public interface IColorAnalyzerProbe
    {
        /// <summary>
        /// Gets the x chromaticity coordinate.
        /// </summary>
        double sx { get; }

        /// <summary>
        /// Gets the y chromaticity coordinate.
        /// </summary>
        double sy { get; }

        /// <summary>
        /// Gets the luminance value in cd/m².
        /// </summary>
        double Lv { get; }

        /// <summary>
        /// Gets the color temperature in Kelvin.
        /// </summary>
        double T { get; }

        /// <summary>
        /// Gets the probe serial number.
        /// </summary>
        string SerialNO { get; }
    }

    /// <summary>
    /// Interface wrapper for color analyzer memory operations.
    /// </summary>
    public interface IColorAnalyzerMemory
    {
        /// <summary>
        /// Gets or sets the channel number (0-99).
        /// </summary>
        int ChannelNO { get; set; }
    }
}