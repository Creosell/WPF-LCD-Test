// В папке Services
// В новом файле MeasurementStatusService.cs

using System.Collections.ObjectModel; // Для ObservableCollection
using WPF_LCD_Test.Models;

namespace WPF_LCD_Test.Services // Или Managers, в зависимости от вашей структуры
{
    // Класс, отвечающий за управление коллекцией статусов точек измерения
    public class MeasurementStatusService
    {
        // --- Реализация Lazy Singleton ---

        // Приватное статическое поле для хранения единственного экземпляра класса
        // Используем System.Lazy для потокобезопасной и ленивой инициализации
        private static Lazy<MeasurementStatusService> _lazyInstance =
            new(() => new MeasurementStatusService());

        // Публичное статическое свойство для доступа к единственному экземпляру класса
        // При первом обращении к Instance.Value будет создан экземпляр
        public static MeasurementStatusService Instance => _lazyInstance.Value;

        // --- Конец реализации Lazy Singleton ---
        // Публичная коллекция статусов, к которой будут обращаться другие классы
        public ObservableCollection<MeasurementStatus> AllMeasurementButtonStatuses { get; }

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

        public MeasurementStatus? TopLeftStatus { get; }
        public MeasurementStatus? TopCenterStatus { get; }
        public MeasurementStatus? TopRightStatus { get; }
        public MeasurementStatus? MiddleLeftStatus { get; }
        public MeasurementStatus? CenterStatus { get; }
        public MeasurementStatus? MiddleRightStatus { get; }
        public MeasurementStatus? BottomLeftStatus { get; }
        public MeasurementStatus? BottomCenterStatus { get; }
        public MeasurementStatus? BottomRightStatus { get; }
        public MeasurementStatus? RedColorStatus { get; }
        public MeasurementStatus? GreenColorStatus { get; }
        public MeasurementStatus? BlueColorStatus { get; }
        public MeasurementStatus? BlackColorStatus { get; }

        private MeasurementStatusService()
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
                var status = new MeasurementStatus(location); // Создаем экземпляр статуса
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
        public MeasurementStatus? GetStatusByLocation(string location)
        {
            // Используем LINQ для поиска первого статуса с совпадающим именем локации
            return AllMeasurementButtonStatuses.FirstOrDefault(s => s.Location == location);
        }
    }
}