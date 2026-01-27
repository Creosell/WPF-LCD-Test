using System.Collections.ObjectModel;
using WPF_LCD_Test.Models;

namespace WPF_LCD_Test.Interfaces
    {
    /// <summary>
    /// Service for managing measurement point statuses.
    /// </summary>
    public interface IMeasurementStatusService
        {
        /// <summary>
        /// Gets dictionary for quick access to measurement status by location.
        /// </summary>
        Dictionary<MeasurementLocation, MeasurementStatus> Statuses { get; }

        /// <summary>
        /// Gets observable collection of all measurement statuses for UI data binding.
        /// </summary>
        ObservableCollection<MeasurementStatus> AllMeasurementButtonStatuses { get; }

        /// <summary>
        /// Retrieves measurement status for specified location.
        /// </summary>
        /// <param name="location">Measurement location identifier.</param>
        /// <returns>MeasurementStatus object for the location.</returns>
        MeasurementStatus GetStatus(MeasurementLocation location);
        }
    }
