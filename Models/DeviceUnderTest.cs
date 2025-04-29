using System.Text.Json.Serialization;
using WPF_LCD_Test.Converters;
using static WPF_LCD_Test.Resources.Resources;


namespace WPF_LCD_Test.Models // Пространство имен должно быть в папке Models
{
    // Делаем класс публичным, чтобы к нему можно было обращаться из ViewModel/Services
    public class DeviceUnderTest // Было internal, лучше сделать public
    {
        // Поля и свойства
        private string dateTimeFormat = "yyyyMMdd_HHmm"; // Формат для строки времени (связано с представлением/сохранением)

        public string SerialNumber { get; set; } // Серийный номер - данные устройства

        [JsonConverter(typeof(CustomDateTimeConverter))]
        public DateTime MeasurementDateTime { get; set; }

        // Список измерений. Это основная коллекция данных.
        public List<Measurement> Measurements { get; set; }

        // Конструктор
        public DeviceUnderTest(string serialNumber)
        {
            if (string.IsNullOrWhiteSpace(serialNumber))
            {
                // В Модели лучше выбрасывать исключение при некорректных входных данных
                throw new ArgumentException($"{SnCantBeEmpty}", nameof(serialNumber));
            }
            SerialNumber = serialNumber;
            MeasurementDateTime = DateTime.Now;
            Measurements = new List<Measurement>();
        }

        // Метод добавления измерения
        public void AddMeasurement(Measurement newMeasurement)
        {
            if (newMeasurement == null)
            {
                throw new ArgumentNullException(nameof(newMeasurement));
            }

            // Логика замены существующего измерения по Location - это бизнес-логика Модели, оставляем здесь
            Measurement existingMeasurement = Measurements.Find(deviceMeasurement => deviceMeasurement.Location == newMeasurement.Location);

            if (existingMeasurement != null)
            {
                Measurements.Remove(existingMeasurement);
            }

            Measurements.Add(newMeasurement);
        }
    }
}