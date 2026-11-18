// В папке Wrappers
// Файл MessageBoxWrapper.cs
using System.Windows;
using WPF_LCD_Test.Interfaces;
using WPF_LCD_Test.Views;

namespace WPF_LCD_Test.Wrappers
{
    /// <summary>
    /// Shows message dialogs as WPF windows (so animations of the owner keep playing) behind an interface for mocking in unit tests.
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
            var dialog = new MessageDialogWindow(messageBoxText, caption, button);
            if (Application.Current?.MainWindow is { IsVisible: true } owner)
            {
                dialog.Owner = owner;
            }

            dialog.ShowDialog();
            return dialog.Result;
        }
    }
}