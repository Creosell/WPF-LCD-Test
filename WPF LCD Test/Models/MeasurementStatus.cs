using MvvmHelpers;
using System.Text.Json.Serialization;
using System.Windows.Media;

namespace WPF_LCD_Test.Models
{
    public class MeasurementStatus : BaseViewModel
    {
        public string Location { get; set; }

        private bool? _isPassed;
        public bool? IsPassed
        {
            get => _isPassed;
            set
            {
                if (SetProperty(ref _isPassed, value))
                    OnPropertyChanged(nameof(StatusColor));
            }
        }

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

        private string _measuredValuesString = "";
        public string MeasuredValuesString
        {
            get => _measuredValuesString;
            set => SetProperty(ref _measuredValuesString, value);
        }

        public MeasurementStatus(string location)
        {
            Location = location;
        }

        public MeasurementStatus()
        {
            Location = "Unknown";
        }
    }
}