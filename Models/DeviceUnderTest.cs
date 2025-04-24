using System.Text.Json; // Используется для сериализации в JSON
using System.Text.Json.Serialization; // Используется для атрибута JsonIgnore
using System.Collections.Generic; // Для List
using System; // Для DateTime, Environment, Path
using System.Linq; // Для LINQ (OrderBy, Select, Any)
using static WPF_LCD_Test.Resources.Resources; 
// using System.Windows.Forms; // Этот using понадобится только для IsContainsAllMeasurements в текущем виде

namespace WPF_LCD_Test.Models // Пространство имен должно быть в папке Models
{
    // Делаем класс публичным, чтобы к нему можно было обращаться из ViewModel/Services
    public class DeviceUnderTest // Было internal, лучше сделать public
    {
        // Поля и свойства
        private string dateTimeFormat = "yyyyMMdd_HHmm"; // Формат для строки времени (связано с представлением/сохранением)

        public string SerialNumber { get; set; } // Серийный номер - данные устройства

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



        // Предложение: Перегрузка или изменение метода, чтобы принимать список имен (string)
        public bool IsContainsAllMeasurements(List<string> requiredMeasurementNames)
        {
            if (requiredMeasurementNames == null || !requiredMeasurementNames.Any())
            {
                // Нет обязательных имен для проверки
                return false;
            }

            // Проверяем, что все обязательные имена присутствуют среди Location имеющихся измерений
            foreach (string requiredName in requiredMeasurementNames)
            {
                if (!Measurements.Any(existingMeasurement => existingMeasurement.Location == requiredName))
                {
                    return false; // Нашли обязательное имя, для которого нет измерения
                }
            }
            return true; // Все обязательные имена найдены
        }
    }
}