// В папке Interfaces
// Файл IMessageBox.cs
using System.Windows; // Для MessageBoxResult, MessageBoxButton, MessageBoxImage

namespace WPF_LCD_Test.Interfaces
{
    public interface IMessageBox
    {
        MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon);
    }
}