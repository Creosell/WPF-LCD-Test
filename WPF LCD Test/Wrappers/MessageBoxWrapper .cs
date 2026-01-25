// В папке Wrappers
// Файл MessageBoxWrapper.cs
using System.Windows;
using WPF_LCD_Test.Interfaces;

namespace WPF_LCD_Test.Wrappers
{
    /// <summary>
    /// Wrapper around System.Windows.MessageBox for testability.
    /// Provides an abstraction layer over WPF message boxes to enable mocking in unit tests.
    /// </summary>
    public class MessageBoxWrapper : IMessageBox
    {
        /// <summary>
        /// Displays a message box with specified text, caption, button, and icon.
        /// </summary>
        /// <param name="messageBoxText">The text to display in the message box.</param>
        /// <param name="caption">The title bar caption of the message box.</param>
        /// <param name="button">The buttons to display in the message box.</param>
        /// <param name="icon">The icon to display in the message box.</param>
        /// <returns>A MessageBoxResult value that specifies which message box button the user clicked.</returns>
        public MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon)
        {
            return MessageBox.Show(messageBoxText, caption, button, icon);
        }
    }
}