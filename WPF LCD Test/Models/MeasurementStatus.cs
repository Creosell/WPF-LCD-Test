// В папке Models
// Файл MeasurementStatus.cs

using MvvmHelpers;
using System.Text.Json.Serialization;
using System.Windows.Media;

namespace WPF_LCD_Test.Models
{
    public class MeasurementStatus : BaseViewModel // Наследует от BaseViewModel
    {
        public string Location { get; set; } // Имя точки измерения

        private bool? _isPassed; // Статус измерения: null - не измерено, true - успешно, false - ошибка

        public bool? IsPassed
        {
            get => _isPassed;
            set
            {
                // !!! Используем SetProperty. SetProperty возвращает true, если значение ДЕЙСТВИТЕЛЬНО изменилось. !!!
                // Если значение изменилось (SetProperty вернул true), выполняем дополнительную логику.
                if (SetProperty(ref _isPassed, value))
                {
                    // Если IsPassed изменилось, уведомляем UI об этом (это делает SetProperty).
                    // И дополнительно уведомляем, что свойство StatusColor тоже могло измениться,
                    // так как оно вычисляется на основе IsPassed.
                    OnPropertyChanged(nameof(StatusColor));
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
                if (IsPassed == true) return Brushes.DarkGreen; // Успех
                if (IsPassed == false) return Brushes.DarkRed;      // Ошибка
                return Brushes.DimGray; // По умолчанию (не измерено)
            }
        }

        // Свойство для отображения измеренных значений рядом с точкой в UI
        private string _measuredValuesString;

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
        public MeasurementStatus(string location)
        {
            Location = location;
            IsPassed = null; // Изначально не измерено
            MeasuredValuesString = ""; // Изначально пусто
        }

        // Конструктор по умолчанию (может быть полезен для XAML дизайнера или сериализации)
        public MeasurementStatus() // Оставь, если хочешь использовать как отдельный класс
        {
            Location = "Unknown";
            IsPassed = null;
            MeasuredValuesString = "";
        }
    }
}