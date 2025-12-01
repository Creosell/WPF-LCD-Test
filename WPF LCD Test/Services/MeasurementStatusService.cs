// In Services/MeasurementStatusService.cs

using System.Collections.ObjectModel;
using WPF_LCD_Test.Models;

namespace WPF_LCD_Test.Services
{
    // Manages the collection of measurement point statuses.
    public class MeasurementStatusService
    {
        // --- Lazy Singleton Implementation ---
        private static readonly Lazy<MeasurementStatusService> _lazyInstance =
            new(() => new MeasurementStatusService());

        public static MeasurementStatusService Instance => _lazyInstance.Value;

        // --- Public Properties ---

        // A dictionary for quick access to any status by its location (key).
        public Dictionary<MeasurementLocation, MeasurementStatus> Statuses { get; }

        // An ObservableCollection for easy data binding in the WPF UI.
        public ObservableCollection<MeasurementStatus> AllMeasurementButtonStatuses { get; }

        // The private constructor initializes everything.
        private MeasurementStatusService()
        {
            // Create a temporary dictionary from the enum.
            var statuses = Enum.GetValues<MeasurementLocation>()
                               .ToDictionary(
                                   location => location, // Key: Enum value (e.g., MeasurementLocation.TopLeft)
                                   location => new MeasurementStatus(location.ToString()) // Value: new MeasurementStatus("TopLeft")
                               );

            Statuses = new Dictionary<MeasurementLocation, MeasurementStatus>(statuses);

            // The UI can bind to this collection. It's created once from the dictionary values.
            AllMeasurementButtonStatuses = new ObservableCollection<MeasurementStatus>(Statuses.Values);
        }

        // A convenient, type-safe method to get a status.
        public MeasurementStatus GetStatus(MeasurementLocation location)
        {
            // Using TryGetValue is safer if the key might not exist,
            // but in our case, the dictionary is guaranteed to be full.
            return Statuses[location];
        }
    }
}