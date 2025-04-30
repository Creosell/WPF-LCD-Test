// В папке Services
// Файл SettingsService.cs

using System;
using System.IO; // Для работы с файлами
using System.Text.Json; // Для работы с JSON
using WPF_LCD_Test.Models; // Ссылка на вашу модель AppSettings
using System.Threading;
using System.Diagnostics; // Для Lazy

namespace WPF_LCD_Test.Services
{
    /// <summary>
    /// Сервис для загрузки и сохранения настроек приложения в JSON файл.
    /// Реализован как Singleton.
    /// </summary>
    public class SettingsService : ISettingsService // Реализуем интерфейс
    {
        // --- Реализация Singleton ---

        // Lazy<T> для потокобезопасной ленивой инициализации
        private static readonly Lazy<SettingsService> _lazyInstance = new(
            () => new SettingsService(), LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>
        /// Получает единственный экземпляр SettingsService.
        /// </summary>
        public static SettingsService Instance => _lazyInstance.Value;

        // Приватный конструктор для предотвращения создания экземпляров извне
        private SettingsService()
        {
            LoadSettings(); // Здесь можно вызвать загрузку настроек при создании сервиса
            // Здесь можно выполнить какую-то начальную инициализацию, если нужно.
            // Но логика загрузки настроек будет в методе LoadSettings.
        }

        // --- Остальная часть класса (ваши методы LoadSettings, SaveSettings) ---

        // Имя файла настроек
        private const string SettingsFileName = "appsettings.json";

        // Опции для ЗАГРУЗКИ настроек (десериализация)
        private static readonly JsonSerializerOptions _loadJsonSerializerOptions = new()
        {
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            PropertyNameCaseInsensitive = true
            // Добавьте другие опции, нужные для загрузки
        };

        // Опции для СОХРАНЕНИЯ настроек (сериализация) - Это те, что вызывали предупреждение WriteIndented
        private static readonly JsonSerializerOptions _saveJsonSerializerOptions = new()
        {
            WriteIndented = true
            // Добавьте другие опции, нужные для сохранения
        };

        // Полный путь к файлу настроек (в папке с исполняемым файлом)
        private static string SettingsFilePath
        {
            get
            {
                string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
                return Path.Combine(appDirectory, SettingsFileName);
            }
        }

        // Метод загрузки настроек (ваш текущий код для этого метода)
        public AppSettings LoadSettings()
        {
            // Создаем объект AppSettings со значениями по умолчанию
            AppSettings settings = new();

            // Проверяем, существует ли файл настроек
            if (File.Exists(SettingsFilePath))
            {
                // --- Логика, если файл существует ---
                try
                {
                    string jsonString = File.ReadAllText(SettingsFilePath);
                    var loadedSettings = JsonSerializer.Deserialize<AppSettings>(jsonString, _loadJsonSerializerOptions);

                    if (loadedSettings != null)
                    {
                        settings = loadedSettings;
                    }

                    Debug.WriteLine($"SettingsService: Настройки загружены из '{SettingsFilePath}'.");
                }
                catch (JsonException jsonEx)
                {
                    Debug.WriteLine($"SettingsService Ошибка JSON: Не удалось загрузить настройки из '{SettingsFilePath}'. Используются настройки по умолчанию. Ошибка: {jsonEx.Message}");
                }
                catch (IOException ioEx)
                {
                    Debug.WriteLine($"SettingsService Ошибка ввода/вывода: Не удалось прочитать файл настроек '{SettingsFilePath}'. Используются настройки по умолчанию. Ошибка: {ioEx.Message}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"SettingsService Неожиданная ошибка: Не удалось загрузить настройки из '{SettingsFilePath}'. Используются настройки по умолчанию. Ошибка: {ex.Message}");
                }
            }
            else
            {
                // --- Логика, если файл НЕ существует: создаем его с настройками по умолчанию ---
                Debug.WriteLine($"SettingsService: Файл настроек '{SettingsFilePath}' не найден. Создаем его с настройками по умолчанию.");

                try
                {
                    // Используем уже созданный объект settings с настройками по умолчанию
                    // Вызываем метод сохранения, чтобы создать файл
                    SaveSettings(settings);

                    Debug.WriteLine($"SettingsService: Файл настроек '{SettingsFilePath}' создан с настройками по умолчанию.");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"SettingsService Ошибка: Не удалось создать и сохранить файл настроек '{SettingsFilePath}' с настройками по умолчанию. Ошибка: {ex.Message}");
                }
            }

            return settings;
        }

        // Метод сохранения настроек (ваш текущий код для этого метода)
        public void SaveSettings(AppSettings settings)
        {
            if (settings == null)
            {
                Debug.WriteLine("SettingsService Ошибка: Невозможно сохранить настройки. Объект AppSettings равен null.");
                return;
            }

            try
            {
                string jsonString = JsonSerializer.Serialize(settings, _saveJsonSerializerOptions);

                File.WriteAllText(SettingsFilePath, jsonString);

                Debug.WriteLine($"SettingsService: Настройки сохранены в '{SettingsFilePath}'.");
            }
            catch (JsonException jsonEx)
            {
                Debug.WriteLine($"SettingsService Ошибка JSON: Не удалось сохранить настройки в '{SettingsFilePath}'. Ошибка: {jsonEx.Message}");
            }
            catch (IOException ioEx)
            {
                Debug.WriteLine($"SettingsService Ошибка ввода/вывода: Не удалось записать файл настроек '{SettingsFilePath}'. Ошибка: {ioEx.Message}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SettingsService Неожиданная ошибка: Не удалось сохранить настройки в '{SettingsFilePath}'. Ошибка: {ex.Message}");
            }
        }
    }
}