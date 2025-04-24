// В файле Services/LocalizationService.cs

using System;
using System.Globalization;
using System.Threading;
using WPF_LCD_Test.Resources; // Убедись, что это пространство имен соответствует твоим .resx файлам
using System.Windows; // Добавляем для доступа к Application.Current.Resources (хотя в этой версии сервиса это не используется, но для полноты)


namespace WPF_LCD_Test.Services
{
    public class LocalizationService : ILocalizationService
    {
        // --- Ручная реализация Синглтона (потокобезопасная) ---

        // Приватное статическое поле для хранения единственного экземпляра
        private static ILocalizationService _instance;

        // Объект для синхронизации потоков при создании экземпляра
        private static readonly object _lock = new object();

        // Приватный конструктор, чтобы нельзя было создать экземпляр напрямую
        private LocalizationService()
        {
            // Этот код выполнится только один раз, при первом создании экземпляра
            Console.WriteLine("LocalizationService: Конструктор экземпляра выполняется (вручную)."); // <-- Добавь для отладки
            // Установка начального языка при создании экземпляра
            // Это вызовет SetLanguage, который вызовет LanguageChanged
            SetLanguage("en"); // Устанавливаем язык по умолчанию при старте
        }

        // Публичное статическое свойство для получения единственного экземпляра
        // Используется техника "double-checked locking" для потокобезопасности
        public static ILocalizationService Instance
        {
            get
            {
                // Первая быстрая проверка без блокировки
                if (_instance == null)
                {
                    // Блокировка потоков только если экземпляр еще не создан
                    lock (_lock)
                    {
                        // Вторая проверка внутри блокировки (на случай, если другой поток создал экземпляр, пока мы ждали блокировку)
                        if (_instance == null)
                        {
                            // Создание единственного экземпляра сервиса
                            _instance = new LocalizationService();
                            Console.WriteLine("LocalizationService: Экземпляр синглтона создан (через Instance)."); // <-- Добавь для отладки
                        }
                    }
                }
                // Возвращаем единственный экземпляр
                return _instance;
            }
        }

        // --- Реализация интерфейса ILocalizationService ---

        // Событие, которое будет срабатывать при смене языка
        public event EventHandler LanguageChanged;

        // Свойство для получения текущей культуры (языка) приложения
        public CultureInfo CurrentCulture => Thread.CurrentThread.CurrentUICulture;

        // Метод для установки нового языка по его коду (например, "en", "zh-Hans")
        public void SetLanguage(string cultureCode)
        {
            Console.WriteLine($"LocalizationService: Попытка установки языка на {cultureCode}"); // <-- Добавь для отладки
            try
            {
                // Создаем объект CultureInfo по коду языка
                CultureInfo culture = new CultureInfo(cultureCode);

                // Устанавливаем культуру для текущего потока.
                // CurrentCulture влияет на форматирование дат, чисел и т.д.
                Thread.CurrentThread.CurrentCulture = culture;
                // CurrentUICulture влияет на выбор строковых ресурсов (из .resx файлов и XAML словарей)
                Thread.CurrentThread.CurrentUICulture = culture;

                // Опционально: Сохранить выбранный язык в настройках пользователя
                // Properties.Settings.Default.DefaultLanguage = cultureCode;
                // Properties.Settings.Default.Save();

                Console.WriteLine($"LocalizationService: Язык успешно изменен на: {cultureCode}"); // <-- Добавь для отладки
                // Уведомляем всех подписчиков о смене языка
                OnLanguageChanged();

            }
            catch (CultureNotFoundException ex)
            {
                // Обработка ошибки: культура не найдена.
                Console.WriteLine($"LocalizationService Ошибка: Культура '{cultureCode}' не найдена. {ex.Message}"); // <-- Добавь для отладки
            }
            catch (Exception ex)
            {
                Console.WriteLine($"LocalizationService Ошибка: При установке языка '{cultureCode}'. {ex.Message}"); // <-- Добавь для отладки
            }
        }

        // Метод для получения локализованной строки по ключу из .resx
        // Этот метод больше не нужен для строк в XAML словарях, но может использоваться для строк ViewModel из .resx
        public string GetString(string key)
        {
            // Получаем строку из ресурсов .resx для текущей CultureInfo потока
            // Убедись, что пространство имен WPF_LCD_Test.Resources правильное для твоего Resources.resx
            // return Resources.Resources.ResourceManager.GetString(key, CurrentCulture) ?? $"!{key}!"; // Возвращаем ключ в восклицательных знаках для отладки

            // Дополнительная проверка на null для ResourceManager (на всякий случай)
            if (WPF_LCD_Test.Resources.Resources.ResourceManager == null)
            {
                Console.WriteLine($"LocalizationService Ошибка: ResourceManager для ключа '{key}' равен null!");
                return $"!{key}!";
            }

            string result = Resources.Resources.ResourceManager.GetString(key, CurrentCulture);
            if (result == null)
            {
                Console.WriteLine($"LocalizationService Предупреждение: Ресурс с ключом '{key}' не найден в .resx для культуры {CurrentCulture.Name}");
                return $"!{key}!";
            }
            return result;
        }

        // Метод для получения локализованной строки с форматированием из .resx
        public string GetString(string key, params object[] args)
        {
            // Получаем шаблон строки из .resx, а затем форматируем его
            string format = GetString(key);
            // Проверяем, был ли формат найден (не вернулся ли ключ для отладки)
            if (string.IsNullOrEmpty(format) || format.StartsWith("!{") && format.EndsWith("}!") && format.Contains(key))
            {
                // Если ресурс не найден или вернулся ключ для отладки, просто возвращаем ключ или шаблон
                return format;
            }
            try
            {
                // Используем CultureInfo для правильного форматирования чисел, дат и т.д.
                return string.Format(CurrentCulture, format, args);
            }
            catch (FormatException ex)
            {
                // Обработка ошибки форматирования (например, несоответствие числа аргументов)
                Console.WriteLine($"LocalizationService Ошибка: Форматирования строки для ключа '{key}'. Ошибка: {ex.Message}"); // <-- Добавь для отладки
                return format; // Возвращаем неформатированную строку
            }
        }

        // Защищенный виртуальный метод для вызова события
        protected virtual void OnLanguageChanged()
        {
            // Проверка на null перед вызовом события
            LanguageChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}