namespace WPF_LCD_Test.Models
    {
    /// <summary>
    /// Measurement speed of the color analyzer. The numeric values are driver independent;
    /// each driver translates them to the native values of its device.
    /// </summary>
    public enum MeasurementSpeed
        {
        /// <summary>
        /// Repeats fast measurements several times: the highest repeatability.
        /// </summary>
        Slow = 0,

        /// <summary>
        /// Shortest measurement time, lower repeatability at low luminance.
        /// </summary>
        Fast = 1,

        /// <summary>
        /// Switches the speed automatically depending on the luminance level.
        /// </summary>
        Auto = 2
        }
    }
