using MvvmHelpers;
using WPF_LCD_Test.Models;

namespace WPF_LCD_Test.ViewModels
    {
    /// <summary>
    /// One row of the results table: a measurement point with the values of its accepted measurement.
    /// The status (color of the dot) is the status of the point's button, so the table and the buttons never disagree.
    /// </summary>
    public sealed class MeasurementResultRow : BaseViewModel
        {
        private const string Dash = "—";

        private static readonly Dictionary<string, string> Hotkeys = new()
            {
            ["TopLeft"] = "1", ["TopCenter"] = "2", ["TopRight"] = "3",
            ["MiddleLeft"] = "4", ["Center"] = "5", ["MiddleRight"] = "6",
            ["BottomLeft"] = "7", ["BottomCenter"] = "8", ["BottomRight"] = "9",
            ["RedColor"] = "R", ["GreenColor"] = "G", ["BlueColor"] = "B", ["WhiteColor"] = "W", ["BlackColor"] = "0"
            };

        private string _name = string.Empty;
        private string _x = Dash;
        private string _y = Dash;
        private string _lv = Dash;
        private string _t = Dash;
        private bool _isAvailable = true;

        /// <summary>
        /// Initializes a new instance of MeasurementResultRow.
        /// </summary>
        /// <param name="status">Status of the measurement point (shared with its button).</param>
        public MeasurementResultRow(MeasurementStatus status)
            {
            Status = status;
            RefreshName();
            }

        /// <summary>
        /// Gets the status of the measurement point.
        /// </summary>
        public MeasurementStatus Status { get; }

        /// <summary>
        /// Gets the measurement location identifier.
        /// </summary>
        public string Location => Status.Location;

        /// <summary>
        /// Gets the point name as on its button ("1. Top left").
        /// </summary>
        public string Name
            {
            get => _name;
            private set => SetProperty(ref _name, value);
            }

        /// <summary>Gets the x chromaticity coordinate, or a dash.</summary>
        public string X
            {
            get => _x;
            private set => SetProperty(ref _x, value);
            }

        /// <summary>Gets the y chromaticity coordinate, or a dash.</summary>
        public string Y
            {
            get => _y;
            private set => SetProperty(ref _y, value);
            }

        /// <summary>Gets the luminance, or a dash.</summary>
        public string Lv
            {
            get => _lv;
            private set => SetProperty(ref _lv, value);
            }

        /// <summary>Gets the color temperature ("n/a" when infinite), or a dash.</summary>
        public string T
            {
            get => _t;
            private set => SetProperty(ref _t, value);
            }

        /// <summary>
        /// Gets or sets whether the point exists for the current device (the white point only does for a TV).
        /// </summary>
        public bool IsAvailable
            {
            get => _isAvailable;
            set => SetProperty(ref _isAvailable, value);
            }

        /// <summary>
        /// Shows the values of the measurement, or dashes when the point holds no accepted measurement.
        /// </summary>
        /// <param name="measurement">Accepted measurement of the point, if any.</param>
        public void Show(Measurement? measurement)
            {
            if (measurement == null)
                {
                (X, Y, Lv, T) = (Dash, Dash, Dash, Dash);
                return;
                }

            (X, Y, Lv, T) = MeasurementViewModel.FormatMeasurement(measurement);
            }

        /// <summary>
        /// Rebuilds the localized name (after the language changed).
        /// </summary>
        public void RefreshName()
            {
            var localized = Resources.Resources.ResourceManager.GetString($"SpecLocation_{Location}") ?? Location;
            Name = Hotkeys.TryGetValue(Location, out var hotkey) ? $"{hotkey}. {localized}" : localized;
            }
        }
    }
