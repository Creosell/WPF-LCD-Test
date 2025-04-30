// В папке Services
// Файл FileService.cs (реализация IFileService)

using System.Globalization;
using System.IO; // Для работы с файлами и папками
using System.Text.Json; // Для сериализации в JSON
using WPF_LCD_Test.Models;
using static WPF_LCD_Test.Resources.Resources;

// using System.Globalization; // Если потребуется для форматирования чисел при сохранении CSV
namespace WPF_LCD_Test.Services
{
    // Класс, реализующий интерфейс сервиса работы с файлами
    public class FileService : IFileService
    {
        // Приватные поля для хранения путей
        private string _applicationBasePath;

        private string _baseFolderPath;
        private readonly string _workFolerName = "data";

        // Публичные свойства из интерфейса
        public string BaseFolderPath => _baseFolderPath;

        public string WorkFolderName => _workFolerName;

        // Опции для ЗАГРУЗКИ настроек (десериализация)
        private static readonly JsonSerializerOptions _saveSerializerOptions = new()
        {
            WriteIndented = true
            // Добавьте другие опции, нужные для загрузки
        };

        // События из интерфейса
        public event EventHandler<string> StatusMessage;

        public event EventHandler<bool> SaveOperationCompleted;

        // Конструктор сервиса
        public FileService()
        {
            // Инициализация путей и папок может происходить при создании сервиса
            InitializeWorkingFolders();
        }

        // Метод инициализации рабочих папок (реализация)
        public void InitializeWorkingFolders()
        {
            try
            {
                _applicationBasePath = AppDomain.CurrentDomain.BaseDirectory;
                _baseFolderPath = Path.Combine(_applicationBasePath, _workFolerName);

                // Проверяем и создаем базовую папку, если ее нет
                if (!Directory.Exists(_baseFolderPath))
                {
                    Directory.CreateDirectory(_baseFolderPath);
                    StatusMessage?.Invoke(this, $"{WorkFolderCreated}: {_baseFolderPath}"); // Сообщение
                }
            }
            catch (Exception ex)
            {
                StatusMessage?.Invoke(this, $"{WorkingFolderInitErr}: {ex.Message}"); // Сообщение об ошибке
                                                                                      // Обработка ошибки инициализации - возможно, стоит бросить исключение или установить флаг
            }
        }

        private static void CheckCurrentAppLanguage()
        {
            CultureInfo culture = LocalizationService.Instance.CurrentCulture; // Получаем текущую культуру из сервиса локализации

            // Устанавливаем эту культуру для текущего потока из пула
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
        }

        // Метод сохранения данных устройства в JSON (реализация)
        public async Task<bool> SaveDeviceDataToJsonAsync(DeviceUnderTest device)
        {
            CheckCurrentAppLanguage(); // Проверяем текущий язык приложения
            if (device == null)
            {
                StatusMessage?.Invoke(this, $"{SaveJSONErrDeviceIsEmpty}");
                SaveOperationCompleted?.Invoke(this, false);
                return false;
            }

            try
            {
                // Определяем путь к папке для этого устройства по серийному номеру
                string serialNumberFolderPath = Path.Combine(BaseFolderPath, device.SerialNumber);

                // Определяем путь к JSON файлу
                string fileName = $"{device.SerialNumber}.json"; // Имя файла основано на серийном номере
                string filePath = Path.Combine(BaseFolderPath, fileName); // Путь - базовая папка + имя файла

                // Сериализуем объект DeviceUnderTest в JSON
                
                string jsonString = JsonSerializer.Serialize(device, _saveSerializerOptions); // Сериализуем объект Модели

                // Асинхронно записываем JSON строку в файл
                await File.WriteAllTextAsync(filePath, jsonString); // Используем асинхронный метод записи

                StatusMessage?.Invoke(this, $"{ResultsForSN} {device.SerialNumber} {SavedToJSON}: {filePath}"); // Сообщение об успехе
                SaveOperationCompleted?.Invoke(this, true);
                return true; // Успех
            }
            catch (Exception ex)
            {
                StatusMessage?.Invoke(this, $"{SaveJSONErrForSN} {device.SerialNumber}: {ex.Message}"); // Сообщение об ошибке
                SaveOperationCompleted?.Invoke(this, false);
                return false; // Ошибка
            }
        }

        // Метод сохранения отдельного измерения в CSV (реализация)
        public async Task<bool> SaveMeasurementToCsvAsync(string measurementCsvString, string measurementLocationName, string serialNumber)
        {
            CheckCurrentAppLanguage(); // Проверяем текущий язык приложения

            if (string.IsNullOrWhiteSpace(serialNumber))
            {
                StatusMessage?.Invoke(this, $"{CsvSnErr}");
                // OnSaveOperationCompleted?.Invoke(this, false); // Может быть, не нужно оповещать о завершении каждого CSV
                return false;
            }
            if (string.IsNullOrWhiteSpace(measurementCsvString))
            {
                StatusMessage?.Invoke(this, $"{CsvDataErr}");
                return false;
            }
            if (string.IsNullOrWhiteSpace(measurementLocationName))
            {
                StatusMessage?.Invoke(this, $"{CsvLocationErr}");
                return false;
            }

            try
            {
                // Определяем путь к папке для этого устройства
                string serialNumberFolderPath = Path.Combine(BaseFolderPath, serialNumber);

                // Убеждаемся, что папка устройства существует
                if (!Directory.Exists(serialNumberFolderPath))
                {
                    Directory.CreateDirectory(serialNumberFolderPath);
                    StatusMessage?.Invoke(this, $"{WorkFolderCreatedForSN}: {serialNumberFolderPath}"); // Сообщение
                }

                // Определяем путь к CSV файлу (используя имя локации)
                string fileName = $"{measurementLocationName}.csv"; // Имя файла по локации
                string filePath = Path.Combine(serialNumberFolderPath, fileName);

                // Асинхронно записываем CSV строку в файл
                await File.WriteAllTextAsync(filePath, measurementCsvString); // Сохраняем уже готовую CSV строку

                // OnStatusMessage?.Invoke(this, $"Измерение '{measurementLocationName}' сохранено в CSV: {filePath}"); // Может быть слишком много сообщений для лога
                // OnSaveOperationCompleted?.Invoke(this, true); // Может быть, не нужно оповещать о завершении каждого CSV
                return true; // Успех
            }
            catch (Exception ex)
            {
                StatusMessage?.Invoke(this, $"{ErrCSV} '{measurementLocationName}' (SN {serialNumber}): {ex.Message}"); // Сообщение об ошибке
                                                                                                                        // OnSaveOperationCompleted?.Invoke(this, false); // Может быть, не нужно оповещать о завершении каждого CSV
                return false; // Ошибка
            }
        }
    }
}