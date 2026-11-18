using MvvmHelpers;
using WPF_LCD_Test.Models;

namespace WPF_LCD_Test.ViewModels
    {
    /// <summary>
    /// How the attempts counter of a point is colored.
    /// </summary>
    public enum TriesLevel
        {
        /// <summary>No counter is shown.</summary>
        None,

        /// <summary>Fewer than the expected three attempts.</summary>
        Few,

        /// <summary>Three attempts or more.</summary>
        Enough
        }

    /// <summary>
    /// One row of the results table: a measurement point with the values of its accepted measurement.
    /// The status (color of the dot) is the status of the point's button, so the table and the buttons never disagree.
    /// </summary>
    public sealed class MeasurementResultRow : BaseViewModel
        {
        private const string Dash = "—";

        /// <summary>
        /// The number of attempts a point with a remark is expected to get.
        /// </summary>
        public const int EnoughAttempts = 3;

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
        private string _tries = Dash;
        private TriesLevel _triesLevel = TriesLevel.None;

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
        /// Gets the attempts counter ("2/3") of a point that had a remark against the specification, or a dash.
        /// </summary>
        public string Tries
            {
            get => _tries;
            private set => SetProperty(ref _tries, value);
            }

        /// <summary>
        /// Gets how far the attempts of the point are from the usual three.
        /// </summary>
        public TriesLevel TriesLevel
            {
            get => _triesLevel;
            private set => SetProperty(ref _triesLevel, value);
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
        /// Shows the attempts counter: only for a point that had a remark against the specification in any attempt.
        /// </summary>
        /// <param name="info">What the specification check found out about the attempts of the point, if anything.</param>
        public void ShowAttempts(SpecAttemptInfo? info)
            {
            if (info is not { Deviated: true })
                {
                (Tries, TriesLevel) = (Dash, TriesLevel.None);
                return;
                }

            Tries = $"{info.Count}/{EnoughAttempts}";
            TriesLevel = info.Count >= EnoughAttempts ? TriesLevel.Enough : TriesLevel.Few;
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
