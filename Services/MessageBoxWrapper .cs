// В папке Services (или Utilities)
// Файл MessageBoxWrapper.cs
using System.Windows;

namespace WPF_LCD_Test.Services
{
    public class MessageBoxWrapper : IMessageBox
    {
        public MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon)
        {
            return MessageBox.Show(messageBoxText, caption, button, icon);
        }
    }
}