using System.Collections.ObjectModel;
using WPF_LCD_Test.Interfaces;
using WPF_LCD_Test.Models;

namespace WPF_LCD_Test.Services
    {
    /// <summary>
    /// Manages the collection of measurement point statuses for UI binding.
    /// </summary>
    public class MeasurementStatusService : IMeasurementStatusService
        {
        /// <summary>
        /// Gets dictionary for quick access to measurement status by location.
        /// </summary>
        public Dictionary<MeasurementLocation, MeasurementStatus> Statuses { get; }

        /// <summary>
        /// Gets observable collection of all measurement statuses for UI data binding.
        /// </summary>
        public ObservableCollection<MeasurementStatus> AllMeasurementButtonStatuses { get; }

        /// <summary>
        /// Initializes a new instance of MeasurementStatusService.
        /// </summary>
        public MeasurementStatusService()
            {
            var statuses = Enum.GetValues<MeasurementLocation>()
                               .ToDictionary(
                                   location => location,
                                   location => new MeasurementStatus(location.ToString())
                               );

            Statuses = new Dictionary<MeasurementLocation, MeasurementStatus>(statuses);
            AllMeasurementButtonStatuses = new ObservableCollection<MeasurementStatus>(Statuses.Values);
            }

        /// <summary>
        /// Retrieves measurement status for specified location.
        /// </summary>
        /// <param name="location">Measurement location identifier.</param>
        /// <returns>MeasurementStatus object for the location.</returns>
        public MeasurementStatus GetStatus(MeasurementLocation location)
            {
            return Statuses[location];
            }
        }
    }