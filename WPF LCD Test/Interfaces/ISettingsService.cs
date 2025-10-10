using WPF_LCD_Test.Models;

namespace WPF_LCD_Test.Interfaces
{
    public interface ISettingsService
    {
        AppSettings LoadSettings();
        void SaveSettings(AppSettings settings);
    }
}