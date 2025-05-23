// В папке Services
// Файл SettingsService.cs

using System;
using System.IO; // Для работы с файлами
using System.Text.Json; // Для работы с JSON
using WPF_LCD_Test.Models; // Ссылка на вашу модель AppSettings
using System.Threading;
using System.Diagnostics; // Для Lazy
using WPF_LCD_Test.Interfaces; // Ссылка на интерфейс ISettingsService

namespace WPF_LCD_Test.Services
{
    /// <summary>
    /// Сервис для загрузки и сохранения настроек приложения в JSON файл.
    /// Реализован как Singleton.
    /// </summary>
    public class SettingsService : ISettingsService // Реализуем интерфейс
    {
        private string _currentSettingsFilePath;
      
        // --- Реализация Singleton ---

        // Lazy<T> для потокобезопасной ленивой инициализации
        private static Lazy<SettingsService> _lazyInstance = new(
            () => new SettingsService(), LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>
        /// Получает единственный экземпляр SettingsService.
        /// </summary>
        public static SettingsService Instance => _lazyInstance.Value;

        // Приватный конструктор для предотвращения создания экземпляров извне
        private SettingsService()
        {
            // Инициализируем _currentSettingsFilePath здесь для экземпляра синглтона
            _currentSettingsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _settingsFileName);
            LoadSettings(); // Здесь можно вызвать загрузку настроек при создании сервиса
        }

        // --- Вспомогательный метод для тестов: устанавливает _currentSettingsFilePath ---
        // Использование рефлексии для доступа к этому методу в тестах.
        internal void SetTestFilePath(string testFilePath) // Сделаем internal, чтобы тесты могли вызывать
        {
            _currentSettingsFilePath = testFilePath;
        }

        // Имя файла настроек
        private const string _settingsFileName = "appsettings.json";

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

       

        // Метод загрузки настроек (ваш текущий код для этого метода)
        public AppSettings LoadSettings()
        {
            // Создаем объект AppSettings со значениями по умолчанию
            AppSettings settings = new();

            // Проверяем, существует ли файл настроек
            if (File.Exists(_currentSettingsFilePath))
            {
                // --- Логика, если файл существует ---
                try
                {
                    string jsonString = File.ReadAllText(_currentSettingsFilePath);
                    var loadedSettings = JsonSerializer.Deserialize<AppSettings>(jsonString, _loadJsonSerializerOptions);

                    if (loadedSettings != null)
                    {
                        settings = loadedSettings;
                    }

                    Debug.WriteLine($"SettingsService: Настройки загружены из '{_currentSettingsFilePath}'.");
                }
                catch (JsonException jsonEx)
                {
                    Debug.WriteLine($"SettingsService Ошибка JSON: Не удалось загрузить настройки из '{_currentSettingsFilePath}'. Используются настройки по умолчанию. Ошибка: {jsonEx.Message}");
                }
                catch (IOException ioEx)
                {
                    Debug.WriteLine($"SettingsService Ошибка ввода/вывода: Не удалось прочитать файл настроек '{_currentSettingsFilePath}'. Используются настройки по умолчанию. Ошибка: {ioEx.Message}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"SettingsService Неожиданная ошибка: Не удалось загрузить настройки из '{_currentSettingsFilePath}'. Используются настройки по умолчанию. Ошибка: {ex.Message}");
                }
            }
            else
            {
                // --- Логика, если файл НЕ существует: создаем его с настройками по умолчанию ---
                Debug.WriteLine($"SettingsService: Файл настроек '{_currentSettingsFilePath}' не найден. Создаем его с настройками по умолчанию.");

                try
                {
                    // Используем уже созданный объект settings с настройками по умолчанию
                    // Вызываем метод сохранения, чтобы создать файл
                    SaveSettings(settings);

                    Debug.WriteLine($"SettingsService: Файл настроек '{_currentSettingsFilePath}' создан с настройками по умолчанию.");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"SettingsService Ошибка: Не удалось создать и сохранить файл настроек '{_currentSettingsFilePath}' с настройками по умолчанию. Ошибка: {ex.Message}");
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

                File.WriteAllText(_currentSettingsFilePath, jsonString);

                Debug.WriteLine($"SettingsService: Настройки сохранены в '{_currentSettingsFilePath}'.");
            }
            catch (JsonException jsonEx)
            {
                Debug.WriteLine($"SettingsService Ошибка JSON: Не удалось сохранить настройки в '{_currentSettingsFilePath}'. Ошибка: {jsonEx.Message}");
            }
            catch (IOException ioEx)
            {
                Debug.WriteLine($"SettingsService Ошибка ввода/вывода: Не удалось записать файл настроек '{_currentSettingsFilePath}'. Ошибка: {ioEx.Message}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SettingsService Неожиданная ошибка: Не удалось сохранить настройки в '{_currentSettingsFilePath}'. Ошибка: {ex.Message}");
            }
        }
    }
}