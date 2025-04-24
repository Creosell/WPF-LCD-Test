// В папке Services
// Файл DialogService.cs

// Нужно для использования MessageBox в реализации
using System.Windows;
using WPF_LCD_Test.Services; // Пространство имен интерфейса

namespace WPF_LCD_Test.Services // Пространство имен должно соответствовать папке Services
{
    // Реализация сервиса показа диалоговых окон с использованием стандартного MessageBox
    public class DialogService : IDialogService // Этот класс реализует интерфейс IDialogService
    {
        // Реализация метода показа диалога с вопросом
        public bool ShowQuestion(string message, string caption)
        {
            // Используем стандартный WPF MessageBox.Show
            MessageBoxResult result = MessageBox.Show(message, caption, MessageBoxButton.YesNo, MessageBoxImage.Question);

            // Возвращаем true, если пользователь нажал Yes
            return result == MessageBoxResult.Yes;
        }

        // Реализация метода показа информационного сообщения
        public void ShowMessage(string message, string caption)
        {
            // Используем стандартный WPF MessageBox.Show
            MessageBox.Show(message, caption, MessageBoxButton.OK, MessageBoxImage.Information);
        }



        // Реализация других методов интерфейса, если ты их добавил в IDialogService
        // Например:
        /*
        public string ShowOpenFileDialog(string filter)
        {
             // Здесь будет код для показа OpenFileDialog
             // return filePath;
             throw new NotImplementedException(); // Заглушка, пока не реализовано
        }
        */
    }
}