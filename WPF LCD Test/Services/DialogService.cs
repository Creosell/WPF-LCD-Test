using System.Windows;
using WPF_LCD_Test.Interfaces;
using WPF_LCD_Test.Wrappers;

namespace WPF_LCD_Test.Services
{
    public class DialogService : IDialogService
    {
        private readonly IMessageBox _messageBox;
        public DialogService(IMessageBox messageBox)
        {
            _messageBox = messageBox;
        }
        public DialogService() : this(new MessageBoxWrapper())
        {
        }
        public bool ShowQuestion(string message, string caption)
        {
            MessageBoxResult result = _messageBox.Show(message, caption, MessageBoxButton.YesNo, MessageBoxImage.Question);
            return result == MessageBoxResult.Yes;
        }
        public void ShowMessage(string message, string caption)
        {
            _messageBox.Show(message, caption, MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}