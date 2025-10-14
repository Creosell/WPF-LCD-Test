using System.Windows;
using WPF_LCD_Test.Interfaces;
using WPF_LCD_Test.Wrappers;

namespace WPF_LCD_Test.Services
{
    // Service for displaying messages and questions using a customizable MessageBox wrapper.
    public class DialogService : IDialogService
    {
        private readonly IMessageBox _messageBox;

        // Constructor for Dependency Injection.
        public DialogService(IMessageBox messageBox)
        {
            _messageBox = messageBox;
        }

        // Default constructor uses the production wrapper.
        public DialogService() : this(new MessageBoxWrapper()) { }

        // Shows a Yes/No question dialog and returns the boolean result (using expression body).
        public bool ShowQuestion(string message, string caption) =>
            _messageBox.Show(message, caption, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

        // Shows an information message dialog (using expression body).
        public void ShowMessage(string message, string caption) =>
            _messageBox.Show(message, caption, MessageBoxButton.OK, MessageBoxImage.Information);
    }
}