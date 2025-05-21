// В папке Services (или Utilities)
// Файл MessageBoxWrapper.cs
using System.Windows;
using WPF_LCD_Test.Services;

namespace WPF_LCD_Test.Wrappers
{
    public class MessageBoxWrapper : IMessageBox
    {
        public MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon)
        {
            return MessageBox.Show(messageBoxText, caption, button, icon);
        }
    }
}