using MvvmHelpers;
using System.Text.Json.Serialization;
using System.Windows.Media;

namespace WPF_LCD_Test.Models
    {
    /// <summary>
    /// Represents the measurement status for a specific location on the device under test.
    /// </summary>
    public class MeasurementStatus : BaseViewModel
        {
        private bool? _isPassed;
        private SpecStatus _specRemark;
        private string _specTooltip = "";
        private string _measuredValuesString = "";

        /// <summary>
        /// Gets or sets the measurement location identifier.
        /// </summary>
        public string Location { get; set; }

        /// <summary>
        /// Gets the signal the measurement button of this point glows on when a measurement of the point is accepted or rejected.
        /// </summary>
        [JsonIgnore]
        public ActionFeedbackSignal Feedback { get; } = new();

        /// <summary>
        /// Gets or sets whether the measurement passed validation criteria.
        /// Null indicates no measurement has been performed yet.
        /// </summary>
        public bool? IsPassed
            {
            get => _isPassed;
            set
                {
                if (SetProperty(ref _isPassed, value))
                    {
                    OnPropertyChanged(nameof(StatusColor));
                    OnPropertyChanged(nameof(SpecBadge));
                    }
                }
            }

        /// <summary>
        /// Gets or sets the worst remark the specification check has about this point
        /// (<see cref="SpecStatus.Pass"/> when there is none).
        /// </summary>
        [JsonIgnore]
        public SpecStatus SpecRemark
            {
            get => _specRemark;
            set
                {
                if (SetProperty(ref _specRemark, value))
                    OnPropertyChanged(nameof(SpecBadge));
                }
            }

        /// <summary>
        /// Gets or sets the text of the remarks shown in the badge tooltip.
        /// </summary>
        [JsonIgnore]
        public string SpecTooltip
            {
            get => _specTooltip;
            set => SetProperty(ref _specTooltip, value);
            }

        /// <summary>
        /// Gets the remark to show as a badge: only while the point holds an accepted measurement,
        /// so a failed or running measurement does not keep the badge of the previous one.
        /// </summary>
        [JsonIgnore]
        public SpecStatus SpecBadge => IsPassed == true ? SpecRemark : SpecStatus.Pass;

        /// <summary>
        /// Gets the status color brush based on measurement result.
        /// Green for passed, red for failed, gray for not measured.
        /// </summary>
        [JsonIgnore]
        public Brush StatusColor
            {
            get => IsPassed switch
                {
                    true => Brushes.DarkGreen,
                    false => Brushes.DarkRed,
                    _ => Brushes.DimGray
                    };
            }

        /// <summary>
        /// Gets or sets the formatted string of measured values (x, y, Lv, T).
        /// </summary>
        public string MeasuredValuesString
            {
            get => _measuredValuesString;
            set
                {
                if (SetProperty(ref _measuredValuesString, value))
                    OnPropertyChanged(nameof(MeasuredValuesToolTip));
                }
            }

        /// <summary>
        /// Gets the tooltip of the measurement button: the measured values, or null while there is nothing to show,
        /// so an empty tooltip box does not pop up before the first measurement.
        /// </summary>
        [JsonIgnore]
        public string? MeasuredValuesToolTip => string.IsNullOrEmpty(_measuredValuesString) ? null : _measuredValuesString;

        /// <summary>
        /// Initializes a new instance of MeasurementStatus with specified location.
        /// </summary>
        /// <param name="location">Measurement location identifier.</param>
        public MeasurementStatus(string location)
            {
            Location = location;
            }

        /// <summary>
        /// Initializes a new instance of MeasurementStatus with default location.
        /// </summary>
        public MeasurementStatus()
            {
            Location = "Unknown";
            }
        }
    }