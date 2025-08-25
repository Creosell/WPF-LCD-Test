// В папке Interfaces
// Файл IDispatcher.cs
namespace WPF_LCD_Test.Interfaces
{
    public interface IDispatcher
    {
        bool CheckAccess();

        void Invoke(Action action); // Если нужно Invoke с DispatcherPriority, добавьте перегрузки

        void BeginInvoke(Action action); // Если нужно BeginInvoke с DispatcherPriority, добавьте перегрузки
    }
}