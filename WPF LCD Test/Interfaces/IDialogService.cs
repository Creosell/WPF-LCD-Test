// В папке Services
// Файл IDialogService.cs

namespace WPF_LCD_Test.Interfaces // Пространство имен должно соответствовать папке Services
{
    // Интерфейс для сервиса показа диалоговых окон
    public interface IDialogService
    {
        // Метод для показа диалога с вопросом (например, с кнопками Да/Нет)
        // Возвращает true, если пользователь ответил утвердительно (например, нажал Да), иначе false.
        bool ShowQuestion(string message, string caption);

        // Метод для показа информационного сообщения
        void ShowMessage(string message, string caption);

        // Можно добавить другие методы, если нужны другие типы диалогов:
        // string ShowOpenFileDialog(string filter); // Для диалога открытия файла
        // string ShowSaveFileDialog(stringFileDialog filter); // Для диалога сохранения файла
        // bool? ShowCustomDialog(SomeCustomViewModel viewModel); // Для показа кастомного диалога
    }
}