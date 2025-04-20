using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WPF_LCD_Test.Models;

namespace WPF_LCD_Test.Services
{  
        // Интерфейс для сервиса управления колориметром
        public interface IColorMeasurementService : IDisposable // Сервис тоже может требовать освобождения ресурсов
        {
            // Свойства состояния устройства, доступные для чтения
            bool IsConnected { get; }
            bool IsCalibrated { get; }
            string PortID { get; } // Возможно, захочешь отображать в UI

            // Методы для команд UI
            Task<bool> ConnectAsync(); // Асинхронная операция для подключения
            void Disconnect();
            Task<bool> CalibrateZeroAsync(); // Асинхронная операция для калибровки

            // Метод для выполнения измерения
            Task<Measurement> MeasureAsync(); // Асинхронная операция, возвращает объект Measurement

            // События для оповещения ViewModel об изменении состояния или ходе выполнения
            // Например, чтобы обновить UI или лог
            event EventHandler<bool> ConnectionStatusChanged;
            event EventHandler<bool> CalibrationStatusChanged;
            event EventHandler<string> StatusMessage; // Для отправки сообщений в лог UI
            event EventHandler<double> MeasurementProgress; // Если измерение занимает время
        }
    }

