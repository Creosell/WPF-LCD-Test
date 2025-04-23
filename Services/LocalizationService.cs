// В файле Services/LocalizationService.cs

using System.Globalization;
using WPF_LCD_Test.Resources;

namespace WPF_LCD_Test.Services
{
    public class LocalizationService : ILocalizationService
    {
        public event EventHandler LanguageChanged;

        public CultureInfo CurrentCulture => Thread.CurrentThread.CurrentUICulture;

        public LocalizationService()
        {
            SetLanguage("en");
        }

        public void SetLanguage(string cultureCode)
        {
            try
            {
                // Создаем объект CultureInfo по коду языка
                CultureInfo culture = new CultureInfo(cultureCode);

                // Устанавливаем культуру для текущего потока.
                // CurrentCulture влияет на форматирование дат, чисел и т.д.
                Thread.CurrentThread.CurrentCulture = culture;
                // CurrentUICulture влияет на выбор строковых ресурсов (из .resx файлов)
                Thread.CurrentThread.CurrentUICulture = culture;

                // Опционально: Сохранить выбранный язык в настройках пользователя
                // Properties.Settings.Default.DefaultLanguage = cultureCode;
                // Properties.Settings.Default.Save();

                // Уведомляем всех подписчиков о смене языка
                OnLanguageChanged();
            }
            catch (CultureNotFoundException ex)
            {
                // Обработка ошибки: культура не найдена.
                // Можно вывести сообщение в лог или использовать культуру по умолчанию.
                Console.WriteLine($"Культура '{cultureCode}' не найдена. Использование текущей культуры. Ошибка: {ex.Message}");
                // В реальном приложении, возможно, стоит использовать сервис логирования
                // _logService.LogError($"Культура '{cultureCode}' не найдена...", ex);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при установке языка '{cultureCode}'. Ошибка: {ex.Message}");
                // _logService.LogError($"Ошибка при установке языка '{cultureCode}'", ex);
            }
        }

        public string GetString(string key)
        {
            // Ensure the Resources class has a method GetString that matches this usage
            return Resources.Resources.ResourceManager.GetString(key, CurrentCulture) ?? $"!{key}!"; // Возвращаем ключ в восклицательных знаках для отладки
        }

        public string GetString(string key, params object[] args)
        {
            // Получаем шаблон строки, а затем форматируем его
            string format = GetString(key);
            if (string.IsNullOrEmpty(format))
            {
                // Если ресурс не найден, просто возвращаем ключ или сообщение об ошибке
                return $"!{key}!"; // Возвращаем ключ в восклицательных знаках для отладки
            }
            try
            {
                return string.Format(CurrentCulture, format, args);
            }
            catch (FormatException ex)
            {
                // Обработка ошибки форматирования (например, несоответствие числа аргументов)
                Console.WriteLine($"Ошибка форматирования строки для ключа '{key}'. Ошибка: {ex.Message}");
                // _logService.LogError($"Ошибка форматирования строки для ключа '{key}'", ex);
                return format; // Возвращаем неформатированную строку
            }
        }

        protected virtual void OnLanguageChanged()
        {
            LanguageChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}