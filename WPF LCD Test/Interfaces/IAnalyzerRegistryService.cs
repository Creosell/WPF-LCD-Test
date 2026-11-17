using WPF_LCD_Test.Models;

namespace WPF_LCD_Test.Interfaces
{
    /// <summary>
    /// Persistent registry of known color analyzer probes and the channel last used with each of them.
    /// </summary>
    public interface IAnalyzerRegistryService
    {
        /// <summary>
        /// Gets the channel last confirmed for the probe.
        /// </summary>
        /// <param name="probeSN">Probe serial number.</param>
        /// <returns>The last used channel, or null when the probe has not been seen before.</returns>
        int? GetLastChannel(string probeSN);

        /// <summary>
        /// Creates or updates the registry entry for the probe and saves the registry.
        /// </summary>
        /// <param name="probeSN">Probe serial number.</param>
        /// <param name="model">Analyzer family the probe is connected through.</param>
        /// <param name="channel">Channel confirmed by the inspector.</param>
        void Update(string probeSN, ColorAnalyzerModel model, int channel);
    }
}
