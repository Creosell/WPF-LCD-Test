namespace WPF_LCD_Test.Models
    {
    /// <summary>
    /// Measurement synchronization mode of the color analyzer. The numeric values match the native device values.
    /// </summary>
    public enum SyncMode
        {
        /// <summary>NTSC display (59.94 Hz).</summary>
        Ntsc = 0,

        /// <summary>PAL / SECAM display (50 Hz).</summary>
        Pal = 1,

        /// <summary>Synchronization with the vertical sync signal of the display.</summary>
        Ext = 2,

        /// <summary>Universal: 100 ms integration time for displays of unknown frequency.</summary>
        Universal = 3,

        /// <summary>Internal synchronization with a user-defined vertical frequency.</summary>
        Internal = 4,

        /// <summary>User-defined integration time (CA-410 only).</summary>
        Manual = 5
        }

    /// <summary>
    /// A synchronization mode together with its parameter.
    /// </summary>
    /// <param name="Mode">Synchronization mode.</param>
    /// <param name="Value">Frequency in Hz for <see cref="SyncMode.Internal"/>, integration time in ms for <see cref="SyncMode.Manual"/>; ignored otherwise.</param>
    public readonly record struct SyncSetting(SyncMode Mode, double Value = 0);

    /// <summary>
    /// Presentation helpers for <see cref="SyncMode"/>.
    /// </summary>
    public static class SyncModeExtensions
        {
        /// <summary>
        /// Returns the short name used on the device and in the settings view.
        /// </summary>
        /// <param name="mode">Synchronization mode.</param>
        public static string ToDisplayName(this SyncMode mode) => mode switch
            {
            SyncMode.Ntsc => "NTSC",
            SyncMode.Pal => "PAL",
            SyncMode.Ext => "EXT",
            SyncMode.Universal => "UNIV",
            SyncMode.Internal => "INT",
            SyncMode.Manual => "MANUAL",
            _ => mode.ToString()
            };
        }
    }
