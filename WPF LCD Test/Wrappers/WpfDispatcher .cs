// В файле Services/WpfDispatcher.cs (или в другом подходящем месте)
using System.Windows; // Для Application.Current
using WPF_LCD_Test.Interfaces;

namespace WPF_LCD_Test.Services
{
    public class WpfDispatcher : IDispatcher
    {
        public bool CheckAccess()
        {
            // Убедитесь, что Application.Current не null, прежде чем получать доступ к Dispatcher
            // В реальном приложении WPF Application.Current будет установлен
            return Application.Current?.Dispatcher?.CheckAccess() ?? true; // Если Application.Current null, предполагаем, что мы уже в UI потоке для безопасности
        }

        public void Invoke(Action action)
        {
            Application.Current?.Dispatcher?.Invoke(action);
        }

        public void BeginInvoke(Action action)
        {
            Application.Current?.Dispatcher?.BeginInvoke(action);
        }
    }
}