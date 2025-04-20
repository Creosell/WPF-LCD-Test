using System.Text.Json; // Используется для сериализации в JSON
using System.Text.Json.Serialization; // Используется для атрибута JsonIgnore
using System.Collections.Generic; // Для List
using System; // Для DateTime, Environment, Path
using System.Linq; // Для LINQ (OrderBy, Select, Any)
// using System.Windows.Forms; // Этот using понадобится только для IsContainsAllMeasurements в текущем виде

namespace WPF_LCD_Test.Models // Пространство имен должно быть в папке Models
{
    // Делаем класс публичным, чтобы к нему можно было обращаться из ViewModel/Services
    public class DeviceUnderTest // Было internal, лучше сделать public
    {
        // Поля и свойства
        private string dateTimeFormat = "yyyyMMdd_HHmm"; // Формат для строки времени (связано с представлением/сохранением)

        public string SerialNumber { get; set; } // Серийный номер - данные устройства

        // Время измерения - сейчас строка. Лучше хранить как DateTime и форматировать для отображения/сохранения.
        public DateTime MeasurementDateTime { get; set; } 

        // Список измерений. Это основная коллекция данных.
        public List<Measurement> Measurements { get; set; }

        // Конструктор
        public DeviceUnderTest(string serialNumber)
        {
            if (string.IsNullOrWhiteSpace(serialNumber))
            {
                // В Модели лучше выбрасывать исключение при некорректных входных данных
                throw new ArgumentException("Серийный номер не может быть пустым.", nameof(serialNumber));
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

            // Возможно, если UI должен обновляться сразу при добавлении,
            // коллекция Measurements должна быть ObservableCollection<Measurement> в ViewModel,
            // а этот метод будет вызван ViewModel, которая добавит в свою ObservableCollection.
            // Но логика *замены* по Location остается здесь. ViewModel вызовет этот метод.
        }

        // Метод сохранения в JSON
        // ЭТА ЛОГИКА ДОЛЖНА БЫТЬ ПЕРЕНЕСЕНА В СЕРВИС ФАЙЛОВ (IFileService)
        /*
        public bool SaveToJson(string baseFolderPath)
        {
             // ... (твой код сохранения в файл) ...
             // Console.WriteLine - это вывод в консоль/отладку, не в лог UI.
             // Логику обработки ошибок (try-catch) и сообщения об ошибках
             // должен обрабатывать Сервис и/или ViewModel.
             throw new NotImplementedException("Логика сохранения в JSON должна быть в FileService.");
        }
        */


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