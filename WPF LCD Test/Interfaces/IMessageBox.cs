using System.Windows;

namespace WPF_LCD_Test.Interfaces
    {
    /// <summary>
    /// Interface for displaying message box dialogs, enabling testability through abstraction.
    /// </summary>
    public interface IMessageBox
        {
        /// <summary>
        /// Displays a message box with specified text, caption, buttons, and icon.
        /// </summary>
        /// <param name="messageBoxText">Message text to display.</param>
        /// <param name="caption">Title bar caption.</param>
        /// <param name="button">Button configuration to display.</param>
        /// <param name="icon">Icon to display in the message box.</param>
        /// <returns>User's choice as MessageBoxResult.</returns>
        MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon);
        }
    }