// В папке Services
// Файл IMessageBox.cs
using System.Windows; // Для MessageBoxResult, MessageBoxButton, MessageBoxImage

namespace WPF_LCD_Test.Services
{
    public interface IMessageBox
    {
        MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon);
    }
}