// В папке Services
// Файл FileService.cs (реализация IFileService)

using System;
using System.IO; // Для работы с файлами и папками
using System.Text.Json; // Для сериализации в JSON
using System.Threading.Tasks; // Для асинхронных операций
using WPF_LCD_Test.Models;
using WPF_LCD_Test.Services;

// using System.Globalization; // Если потребуется для форматирования чисел при сохранении CSV
namespace WPF_LCD_Test.Services
{
    // Класс, реализующий интерфейс сервиса работы с файлами
    public class FileService : IFileService
    {
        // Приватные поля для хранения путей
        private string _desktopPath;

        private string _baseFolderPath;
        private readonly string _workFolerName = "Color measurement data"; // Константа лучше здесь или в Helpers

        // Публичные свойства из интерфейса
        public string BaseFolderPath => _baseFolderPath;

        public string WorkFolderName => _workFolerName;

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
                _desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                _baseFolderPath = Path.Combine(_desktopPath, _workFolerName);

                // Проверяем и создаем базовую папку, если ее нет
                if (!Directory.Exists(_baseFolderPath))
                {
                    Directory.CreateDirectory(_baseFolderPath);
                    StatusMessage?.Invoke(this, $"Создана рабочая папка: {_baseFolderPath}\r\n"); // Сообщение
                }
            }
            catch (Exception ex)
            {
                StatusMessage?.Invoke(this, $"Ошибка при инициализации рабочих папок: {ex.Message}\r\n"); // Сообщение об ошибке
                                                                                                          // Обработка ошибки инициализации - возможно, стоит бросить исключение или установить флаг
            }
        }

        // Метод сохранения данных устройства в JSON (реализация)
        public async Task<bool> SaveDeviceDataToJsonAsync(DeviceUnderTest device)
        {
            if (device == null)
            {
                StatusMessage?.Invoke(this, "Ошибка сохранения: Нет данных устройства.\r\n");
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
                var options = new JsonSerializerOptions { WriteIndented = true };
                string jsonString = JsonSerializer.Serialize(device, options); // Сериализуем объект Модели

                // Асинхронно записываем JSON строку в файл
                await File.WriteAllTextAsync(filePath, jsonString); // Используем асинхронный метод записи

                StatusMessage?.Invoke(this, $"Данные для SN {device.SerialNumber} сохранены в JSON: {filePath}\r\n"); // Сообщение об успехе
                SaveOperationCompleted?.Invoke(this, true);
                return true; // Успех
            }
            catch (Exception ex)
            {
                StatusMessage?.Invoke(this, $"Ошибка при сохранении в JSON для SN {device.SerialNumber}: {ex.Message}\r\n"); // Сообщение об ошибке
                SaveOperationCompleted?.Invoke(this, false);
                return false; // Ошибка
            }
        }

        // Метод сохранения отдельного измерения в CSV (реализация)
        public async Task<bool> SaveMeasurementToCsvAsync(string measurementCsvString, string measurementLocationName, string serialNumber)
        {
            if (string.IsNullOrWhiteSpace(serialNumber))
            {
                StatusMessage?.Invoke(this, "Ошибка сохранения CSV: Не указан серийный номер.\r\n");
                // OnSaveOperationCompleted?.Invoke(this, false); // Может быть, не нужно оповещать о завершении каждого CSV
                return false;
            }
            if (string.IsNullOrWhiteSpace(measurementCsvString))
            {
                StatusMessage?.Invoke(this, "Ошибка сохранения CSV: Нет данных для сохранения.\r\n");
                return false;
            }
            if (string.IsNullOrWhiteSpace(measurementLocationName))
            {
                StatusMessage?.Invoke(this, "Ошибка сохранения CSV: Не указано имя локации.\r\n");
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
                    StatusMessage?.Invoke(this, $"Создана папка для устройства: {serialNumberFolderPath}\r\n"); // Сообщение
                }

                // Определяем путь к CSV файлу (используя имя локации)
                string fileName = $"{measurementLocationName}.csv"; // Имя файла по локации
                string filePath = Path.Combine(serialNumberFolderPath, fileName);

                // Асинхронно записываем CSV строку в файл
                await File.WriteAllTextAsync(filePath, measurementCsvString); // Сохраняем уже готовую CSV строку

                // OnStatusMessage?.Invoke(this, $"Измерение '{measurementLocationName}' сохранено в CSV: {filePath}\r\n"); // Может быть слишком много сообщений для лога
                // OnSaveOperationCompleted?.Invoke(this, true); // Может быть, не нужно оповещать о завершении каждого CSV
                return true; // Успех
            }
            catch (Exception ex)
            {
                StatusMessage?.Invoke(this, $"Ошибка при сохранении в CSV для '{measurementLocationName}' (SN {serialNumber}): {ex.Message}\r\n"); // Сообщение об ошибке
                                                                                                                                                   // OnSaveOperationCompleted?.Invoke(this, false); // Может быть, не нужно оповещать о завершении каждого CSV
                return false; // Ошибка
            }
        }
    }
}