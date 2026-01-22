using System.Windows;
using WPF_LCD_Test.Interfaces;
using WPF_LCD_Test.Wrappers;

namespace WPF_LCD_Test.Services
    {
    /// <summary>
    /// Service for displaying message dialogs and user prompts using MessageBox wrapper.
    /// </summary>
    public class DialogService : IDialogService
        {
        private readonly IMessageBox _messageBox;

        /// <summary>
        /// Initializes a new instance of DialogService with default MessageBox wrapper.
        /// </summary>
        public DialogService() : this(new MessageBoxWrapper()) { }

        /// <summary>
        /// Initializes a new instance of DialogService with dependency injection.
        /// </summary>
        /// <param name="messageBox">MessageBox wrapper for displaying dialogs.</param>
        public DialogService(IMessageBox messageBox)
            {
            _messageBox = messageBox;
            }

        /// <summary>
        /// Displays Yes/No question dialog and returns user's choice.
        /// </summary>
        /// <param name="message">Question message text.</param>
        /// <param name="caption">Dialog window caption.</param>
        /// <returns>True if user clicked Yes, false if user clicked No.</returns>
        public bool ShowQuestion(string message, string caption) =>
            _messageBox.Show(message, caption, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

        /// <summary>
        /// Displays information message dialog with OK button.
        /// </summary>
        /// <param name="message">Information message text.</param>
        /// <param name="caption">Dialog window caption.</param>
        public void ShowMessage(string message, string caption) =>
            _messageBox.Show(message, caption, MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }