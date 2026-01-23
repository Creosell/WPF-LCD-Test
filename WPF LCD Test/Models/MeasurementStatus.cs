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
        private string _measuredValuesString = "";

        /// <summary>
        /// Gets or sets the measurement location identifier.
        /// </summary>
        public string Location { get; set; }

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
                    OnPropertyChanged(nameof(StatusColor));
                }
            }

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
            set => SetProperty(ref _measuredValuesString, value);
            }

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