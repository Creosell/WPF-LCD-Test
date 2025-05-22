// В папке Interfaces
// Файл IFileService.cs

using WPF_LCD_Test.Models;

namespace WPF_LCD_Test.Interfaces
{
    // Интерфейс для сервиса работы с файлами
    public interface IFileService
    {
        // Метод для определения и инициализации рабочих папок
        // Можно сделать его вызываемым явно или выполнять в конструкторе сервиса
        void InitializeWorkingFolders();

        // Свойства для получения путей, если они нужны ViewModel
        string BaseFolderPath { get; }

        string WorkFolderName { get; } // Возможно, тоже нужно

        // Метод для сохранения данных устройства в JSON
        // Принимает объект Модели DeviceUnderTest
        Task<bool> SaveDeviceDataToJsonAsync(DeviceUnderTest device); // Передаем объект, сервис знает куда сохранять

        // Метод для сохранения отдельных измерений в CSV
        // Принимает строковое представление измерения (CSV формат) и имя точки измерения
        Task<bool> SaveMeasurementToCsvAsync(string measurementCsvString, string measurementLocationName, string serialNumber);

        // Альтернативно: Task<bool> SaveMeasurementToCsvAsync(Measurement measurement, string serialNumber); // Если сервис сам формирует CSV строку

        // Метод для оповещения ViewModel о ходе выполнения или ошибках
        event EventHandler<string> StatusMessage; // Для отправки сообщений о сохранении/ошибках в лог UI

        event EventHandler<bool> SaveOperationCompleted; // Оповещение о завершении сохранения

        // Новый метод для запуска внешней программы
        bool RunExternalProgram(string executableName);
    }
}