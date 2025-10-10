using System.Windows;

namespace WPF_LCD_Test.Interfaces
{
    public interface IMessageBox
    {
        MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon);
    }
}