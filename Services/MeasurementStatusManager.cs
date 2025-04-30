// В новом файле MeasurementStatusManager.cs

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel; // Для ObservableCollection
using System.Linq;
using System.Text.Json.Serialization;
using System.Windows.Media;
using MvvmHelpers;
using WPF_LCD_Test.Models;
using WPF_LCD_Test.ViewModels; // Убедитесь, что пространство имен вашего ViewModel доступно

namespace WPF_LCD_Test.Services // Или Managers, в зависимости от вашей структуры
{
    // Класс, отвечающий за управление коллекцией статусов точек измерения
    public class MeasurementStatusManager
    {
        // --- Реализация Lazy Singleton ---

        // Приватное статическое поле для хранения единственного экземпляра класса
        // Используем System.Lazy для потокобезопасной и ленивой инициализации
        private static readonly Lazy<MeasurementStatusManager> _lazyInstance =
            new(() => new MeasurementStatusManager());

        // Публичное статическое свойство для доступа к единственному экземпляру класса
        // При первом обращении к Instance.Value будет создан экземпляр
        public static MeasurementStatusManager Instance => _lazyInstance.Value;

        // --- Конец реализации Lazy Singleton ---
        // Публичная коллекция статусов, к которой будут обращаться другие классы
        public ObservableCollection<MeasurementStatusViewModel> AllMeasurementButtonStatuses { get; }

        // Конструктор: инициализирует коллекцию и создает все объекты статусов
        public const string TopLeftLocationName = "TopLeft";
        public const string TopCenterLocationName = "TopCenter";
        public const string TopRightLocationName = "TopRight";
        public const string MiddleLeftLocationName = "MiddleLeft";
        public const string CenterLocationName = "Center";
        public const string MiddleRightLocationName = "MiddleRight";
        public const string BottomLeftLocationName = "BottomLeft";
        public const string BottomCenterLocationName = "BottomCenter";
        public const string BottomRightLocationName = "BottomRight";
        public const string RedColorLocationName = "RedColor";
        public const string GreenColorLocationName = "GreenColor";
        public const string BlueColorLocationName = "BlueColor";
        public const string BlackColorLocationName = "BlackColor";

        public MeasurementStatusViewModel? TopLeftStatus { get; }
        public MeasurementStatusViewModel? TopCenterStatus { get; }
        public MeasurementStatusViewModel? TopRightStatus { get; }
        public MeasurementStatusViewModel? MiddleLeftStatus { get; }
        public MeasurementStatusViewModel? CenterStatus { get; }
        public MeasurementStatusViewModel? MiddleRightStatus { get; }
        public MeasurementStatusViewModel? BottomLeftStatus { get; }
        public MeasurementStatusViewModel? BottomCenterStatus { get; }
        public MeasurementStatusViewModel? BottomRightStatus { get; }
        public MeasurementStatusViewModel? RedColorStatus { get; }
        public MeasurementStatusViewModel? GreenColorStatus { get; }
        public MeasurementStatusViewModel? BlueColorStatus { get; }
        public MeasurementStatusViewModel? BlackColorStatus { get; }

     

        public MeasurementStatusManager()
        {
            AllMeasurementButtonStatuses = []; // Инициализируем коллекцию

            // Список всех имен точек измерения, используем константы
            var locations = new List<string>
            {
                TopLeftLocationName,
                TopCenterLocationName,
                TopRightLocationName,
                MiddleLeftLocationName,
                CenterLocationName,
                MiddleRightLocationName,
                BottomLeftLocationName,
                BottomCenterLocationName,
                BottomRightLocationName,
                RedColorLocationName,
                GreenColorLocationName,
                BlueColorLocationName,
                BlackColorLocationName,
            };

            foreach (var location in locations)
            {
                var status = new MeasurementStatusViewModel(location); // Создаем экземпляр статуса
                AllMeasurementButtonStatuses.Add(status); // Добавляем в коллекцию

                // Присваиваем созданный объект статусному свойству в этом менеджере
                switch (location)
                {
                    case TopLeftLocationName:
                        TopLeftStatus = status;
                        break;
                    case TopCenterLocationName:
                        TopCenterStatus = status;
                        break;
                    case TopRightLocationName:
                        TopRightStatus = status;
                        break;
                    case MiddleLeftLocationName:
                        MiddleLeftStatus = status;
                        break;
                    case CenterLocationName:
                        CenterStatus = status;
                        break;
                    case MiddleRightLocationName:
                        MiddleRightStatus = status;
                        break;
                    case BottomLeftLocationName:
                        BottomLeftStatus = status;
                        break;
                    case BottomCenterLocationName:
                        BottomCenterStatus = status;
                        break;
                    case BottomRightLocationName:
                        BottomRightStatus = status;
                        break;
                    case RedColorLocationName:
                        RedColorStatus = status;
                        break;
                    case GreenColorLocationName:
                        GreenColorStatus = status;
                        break;
                    case BlueColorLocationName:
                        BlueColorStatus = status;
                        break;
                    case BlackColorLocationName:
                        BlackColorStatus = status;
                        break;
                }
            }
        }

        // Опционально: Метод для удобного поиска статуса по имени локации
        public MeasurementStatusViewModel? GetStatusByLocation(string location)
        {
            // Используем LINQ для поиска первого статуса с совпадающим именем локации
            return AllMeasurementButtonStatuses.FirstOrDefault(s => s.Location == location);
        }
    }

    public class MeasurementStatusViewModel : BaseViewModel // Наследует от BaseViewModel
    {
        public string Location { get; set; } // Имя точки измерения

        private bool? _isPassed; // Статус измерения: null - не измерено, true - успешно, false - ошибка

        public bool? IsPassed
        {
            get => _isPassed;
            set
            {
                if (_isPassed != value)
                {
                    _isPassed = value;
                    OnPropertyChanged(); // Уведомляем UI об изменении IsPassed
                    OnPropertyChanged(nameof(StatusColor)); // Уведомляем, что свойство StatusColor тоже могло измениться
                }
            }
        }

        // Свойство для определения цвета в UI (привязка к Background кнопки/TextBlock)
        // Возвращает WPF Brush
        [JsonIgnore] // Обычно это свойство не нужно сохранять в JSON, т.к. оно связано с представлением
        public Brush StatusColor
        {
            get
            {
                if (IsPassed == true)
                    return Brushes.DarkGreen; // Успех
                if (IsPassed == false)
                    return Brushes.DarkRed; // Ошибка
                return Brushes.DimGray; // По умолчанию (не измерено)
            }
        }

        // Свойство для отображения измеренных значений рядом с точкой в UI
        private string _measuredValuesString ="";

        public string MeasuredValuesString
        {
            get => _measuredValuesString;
            set
            {
                if (_measuredValuesString != value)
                {
                    _measuredValuesString = value;
                    OnPropertyChanged(); // Уведомляем UI об изменении текста
                }
            }
        }

        // Конструктор с параметром (имя точки) для удобства инициализации
        public MeasurementStatusViewModel(string location)
        {
            Location = location;
            IsPassed = null; // Изначально не измерено
            MeasuredValuesString = ""; // Изначально пусто
        }

        // Конструктор по умолчанию (может быть полезен для XAML дизайнера или сериализации)
        public MeasurementStatusViewModel() // Оставь, если хочешь использовать как отдельный класс
        {
            Location = "Unknown";
            IsPassed = null;
            MeasuredValuesString = "";
        }
    }
}
