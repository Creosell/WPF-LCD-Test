// В папке Services
// Файл DialogService.cs

using System.Windows;

namespace WPF_LCD_Test.Services
{
    public class DialogService : IDialogService
    {
        private readonly IMessageBox _messageBox; // Добавляем зависимость

        // Конструктор для внедрения зависимости
        public DialogService(IMessageBox messageBox)
        {
            _messageBox = messageBox;
        }

        // Конструктор по умолчанию для продакшн-кода (если не используется DI-контейнер)
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