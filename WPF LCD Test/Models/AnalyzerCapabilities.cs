namespace WPF_LCD_Test.Models
    {
    /// <summary>
    /// What each analyzer family supports, so that settings of one device are never sent to another.
    /// An unknown model is treated as the most capable one.
    /// </summary>
    public static class AnalyzerCapabilities
        {
        /// <summary>Internal synchronization frequency offered by default, Hz.</summary>
        public const double DefaultFrequency = 60;

        /// <summary>Manual integration time offered by default, ms.</summary>
        public const double DefaultIntegrationTime = 100;

        private static readonly SyncMode[] Ca310SyncModes = [SyncMode.Ntsc, SyncMode.Pal, SyncMode.Ext, SyncMode.Universal, SyncMode.Internal];
        private static readonly SyncMode[] AllSyncModes = [.. Ca310SyncModes, SyncMode.Manual];

        /// <summary>
        /// Gets the synchronization modes supported by the model.
        /// </summary>
        /// <param name="model">Analyzer model.</param>
        public static IReadOnlyList<SyncMode> GetSyncModes(ColorAnalyzerModel model) =>
            model == ColorAnalyzerModel.CA310 ? Ca310SyncModes : AllSyncModes;

        /// <summary>
        /// Gets the allowed internal synchronization frequency range in Hz.
        /// </summary>
        /// <param name="model">Analyzer model.</param>
        public static (double Min, double Max) GetFrequencyRange(ColorAnalyzerModel model) =>
            model == ColorAnalyzerModel.CA310 ? (40, 200) : (0.5, 240);

        /// <summary>
        /// Gets the allowed manual integration time range in ms.
        /// </summary>
        public static (double Min, double Max) GetIntegrationTimeRange() => (4, 4000);

        /// <summary>
        /// Checks that the model supports the mode and that the parameter is inside the allowed range.
        /// </summary>
        /// <param name="model">Analyzer model.</param>
        /// <param name="setting">Synchronization setting to check.</param>
        public static bool IsSupported(ColorAnalyzerModel model, SyncSetting setting)
            {
            if (!GetSyncModes(model).Contains(setting.Mode)) return false;

            return setting.Mode switch
                {
                SyncMode.Internal => IsInRange(setting.Value, GetFrequencyRange(model)),
                SyncMode.Manual => IsInRange(setting.Value, GetIntegrationTimeRange()),
                _ => true
                };
            }

        private static bool IsInRange(double value, (double Min, double Max) range) => value >= range.Min && value <= range.Max;
        }
    }
