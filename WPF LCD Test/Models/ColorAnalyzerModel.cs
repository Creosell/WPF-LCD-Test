namespace WPF_LCD_Test.Models
    {
    /// <summary>
    /// Konica Minolta color analyzer family detected at connection time.
    /// </summary>
    public enum ColorAnalyzerModel
        {
        /// <summary>Model has not been detected.</summary>
        Unknown = 0,

        /// <summary>CA-200 / CA-310 family, accessed through the legacy CA-SDK COM server.</summary>
        CA310 = 1,

        /// <summary>CA-410 family, accessed through CA-SDK2.</summary>
        CA410 = 2
        }

    /// <summary>
    /// Presentation helpers for <see cref="ColorAnalyzerModel"/>.
    /// </summary>
    public static class ColorAnalyzerModelExtensions
        {
        /// <summary>
        /// Returns the user-facing model name, or an em dash when the model is unknown.
        /// </summary>
        /// <param name="model">Analyzer model.</param>
        /// <returns>Display name such as "CA-410".</returns>
        public static string ToDisplayName(this ColorAnalyzerModel model) => model switch
            {
            ColorAnalyzerModel.CA310 => "CA-310",
            ColorAnalyzerModel.CA410 => "CA-410",
            _ => "—"
            };
        }
    }
