// В файле Services/LocalizationService.cs

using System;
using System.Globalization;
using System.Threading;
using static WPF_LCD_Test.Resources.Resources; // Убедись, что это пространство имен соответствует твоим .resx файлам
using System.Windows;
// Добавьте using для ResourceManager, если его нет
using System.Resources;

// Возможно, вам все еще нужен using static для Resources.Resources для StatusMessage?.Invoke внутри этого класса, если вы там их используете.
// using static WPF_LCD_Test.Resources.Resources;

namespace WPF_LCD_Test.Services
{
    public class LocalizationService : ILocalizationService
    {
        // Оставьте StatusMessage event, если он используется для ошибок сервиса
        public event EventHandler<string> StatusMessage; // Событие для передачи сообщений об ошибках и статусах

        // --- Реализация Синглтона с использованием Lazy<T> (Рекомендуемый, Потокобезопасный) ---

        // Приватное статическое поле, использующее Lazy<T>
        private static readonly Lazy<ILocalizationService> _lazyInstance =
            new Lazy<ILocalizationService>(() => new LocalizationService());

        // !!! ДОБАВЬТЕ ЭТО ПРИВАТНОЕ ПОЛЕ ДЛЯ ХРАНЕНИЯ ТЕКУЩЕЙ КУЛЬТУРЫ ПРИЛОЖЕНИЯ !!!
        private CultureInfo _applicationCulture;

        // Приватный конструктор
        private LocalizationService()
        {
            // !!! Установите начальную культуру И сохраните ее в поле при создании сервиса !!!
            // Это вызовет SetLanguage, который установит культуру потока и сохранит ее в _applicationCulture
            SetLanguage("en"); // Устанавливаем язык по умолчанию при старте
        }

        // Публичное статическое свойство для получения единственного экземпляра
        public static ILocalizationService Instance
        {
            get
            {
                return _lazyInstance.Value;
            }
        }

        // Событие, которое будет срабатывать при смене языка
        public event EventHandler LanguageChanged;

        // !!! ИЗМЕНИТЕ ЭТО СВОЙСТВО, ЧТОБЫ ОНО ВОЗВРАЩАЛО ХРАНИМУЮ В СЕРВИСЕ КУЛЬТУРУ !!!
        // Оно ДОЛЖНО возвращать значение поля _applicationCulture, а не культуру текущего потока.
        public CultureInfo CurrentCulture => _applicationCulture; // <-- ВОЗВРАЩАЕМ ХРАНИМУЮ КУЛЬТУРУ

        // Метод для установки нового языка
        public void SetLanguage(string cultureCode)
        {
            // Keep the try-catch blocks and Console.WriteLine if desired for debugging
            // Console.WriteLine($"LocalizationService: Попытка установки языка на {cultureCode}");
            try
            {
                CultureInfo culture = new CultureInfo(cultureCode);

                // !!! ОПЦИОНАЛЬНО: Оставьте проверку, но УБЕДИТЕСЬ, что OnLanguageChanged() вызывается,
                // !!! даже если культура не изменилась, если вам нужно перезагрузить ресурсы (как было в вашем рабочем варианте).
                // !!! Проще всего удалить этот if блок или вынести OnLanguageChanged() за его пределы.
                // if (Equals(Thread.CurrentThread.CurrentUICulture, culture))
                // {
                //     Console.WriteLine($"LocalizationService: Язык уже установлен на {cultureCode}. Пропускаем.");
                //     // Если язык уже установлен, мы все равно можем захотеть вызвать OnLanguageChanged
                //     // чтобы UI обновился, если ресурсы были изменены или добавлены.
                //     // Если возвращаете здесь, OnLanguageChanged() будет пропущен!
                //     // Если вам нужно, чтобы SetLanguage("en") при старте всегда приводил к вызову OnLanguageChanged,
                //     // даже если культура ОС уже "en", то не возвращайте здесь!
                // }

                // Устанавливаем культуру для ТЕКУЩЕГО потока (это важно для UI привязок DynamicResource)
                Thread.CurrentThread.CurrentCulture = culture;
                Thread.CurrentThread.CurrentUICulture = culture;

                // !!! СОХРАНЯЕМ УСТАНОВЛЕННУЮ КУЛЬТУРУ В ПОЛЕ СЕРВИСА !!!
                _applicationCulture = culture; // Сохраняем культуру, которую установили

                // Console.WriteLine($"LocalizationService: Язык успешно изменен на: {cultureCode}");

                // Вызываем событие смены языка.
                // !!! УБЕДИТЕСЬ, что этот вызов происходит ВСЕГДА, когда язык должен "смениться"
                // !!! (т.е., когда вызывается SetLanguage и культура успешно создана).
                OnLanguageChanged();

            }
            catch (CultureNotFoundException ex)
            {
                StatusMessage?.Invoke(this, $"{CultureNotFoundErr}: {ex.Message}"); // Предполагая, что CultureNotFoundErr из Resources.Resources
            }
            catch (Exception ex)
            {
                StatusMessage?.Invoke(this, $"{ErrUnexpected}: {ex.Message}"); // Предполагая, что ErrUnexpected из Resources.Resources
            }
        }

        // Метод для получения локализованной строки из .resx
        // !!! ИЗМЕНИТЕ ЭТОТ МЕТОД, ЧТОБЫ ОН ИСПОЛЬЗОВАЛ ХРАНИМУЮ _applicationCulture !!!
        public string GetString(string key)
        {
            // Предполагая, что ResourceManager доступен через using или это WPF_LCD_Test.Resources.Resources.ResourceManager
            if (Resources.Resources.ResourceManager == null) // Или WPF_LCD_Test.Resources.Resources.ResourceManager == null
            {
                StatusMessage?.Invoke(this, $"{ErrUnexpected}:  null!"); // Предполагая, что ErrUnexpected из Resources.Resources
                return $"!{key}!";
            }

            // !!! Используем ХРАНИМУЮ в сервисе культуру для поиска ресурса !!!
            string result = Resources.Resources.ResourceManager.GetString(key, _applicationCulture); // <-- ИСПОЛЬЗУЕМ _applicationCulture

            if (result == null)
            {
                // Предполагая, что LocalizationServiceResErr и Err из Resources.Resources
                StatusMessage?.Invoke(this, $"{LocalizationServiceResErr} {_applicationCulture?.Name} : {key}"); // Используем _applicationCulture?.Name
                return $"!{key}!";
            }
            return result;
        }

        // Метод для получения локализованной строки с форматированием из .resx
        // !!! ИЗМЕНИТЕ ЭТОТ МЕТОД, ЧТОБЫ ОН ИСПОЛЬЗОВАЛ ХРАНИМУЮ _applicationCulture !!!
        public string GetString(string key, params object[] args)
        {
            string format = GetString(key); // Этот вызов уже использует обновленный GetString
            if (string.IsNullOrEmpty(format) || (format.StartsWith("!{") && format.EndsWith("}!") && format.Contains(key)))
            {
                return format;
            }
            try
            {
                // !!! Используем ХРАНИМУЮ в сервисе культуру для форматирования !!!
                return string.Format(_applicationCulture, format, args); // <-- ИСПОЛЬЗУЕМ _applicationCulture
            }
            catch (FormatException ex)
            {
                // Предполагая, что LocalizationServiceFormatErr, key, и Err из Resources.Resources
                StatusMessage?.Invoke(this, $"{LocalizationServiceFormatErr} {key} {Err}: {ex.Message}");
                return format;
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