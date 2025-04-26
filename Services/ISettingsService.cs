// В папке Services
// Файл ISettingsService.cs

using System;
using WPF_LCD_Test.Models; // Ссылка на вашу модель AppSettings

namespace WPF_LCD_Test.Services
{
    /// <summary>
    /// Интерфейс сервиса для загрузки и сохранения настроек приложения.
    /// </summary>
    public interface ISettingsService
    {
        /// <summary>
        /// Загружает настройки приложения из файла.
        /// </summary>
        /// <returns>Объект AppSettings с загруженными настройками.</returns>
        AppSettings LoadSettings();

        /// <summary>
        /// Сохраняет настройки приложения в файл.
        /// </summary>
        /// <param name="settings">Объект AppSettings для сохранения.</param>
        void SaveSettings(AppSettings settings);
    }
}