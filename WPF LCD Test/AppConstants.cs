namespace WPF_LCD_Test
{
    /// <summary>
    /// Application-wide constants.
    /// </summary>
    public static class AppConstants
    {
        /// <summary>
        /// Color analyzer hardware constants.
        /// </summary>
        public static class ColorAnalyzer
        {
            /// <summary>
            /// Minimum valid measurement channel number.
            /// </summary>
            public const int MinChannel = 0;

            /// <summary>
            /// Maximum valid measurement channel number.
            /// </summary>
            public const int MaxChannel = 99;

            /// <summary>
            /// Lowest sync mode value that is read as an internal synchronization frequency in Hz.
            /// </summary>
            public const int MinSyncFrequency = 40;

            /// <summary>
            /// Highest internal synchronization frequency in Hz supported by all analyzers.
            /// </summary>
            public const int MaxSyncFrequency = 200;
        }
    }
}
