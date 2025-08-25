// В папке Wrappers
//Файл WpfDispatcher.cs
using System.Windows;
using WPF_LCD_Test.Interfaces;

namespace WPF_LCD_Test.Services
{
    public class WpfDispatcher : IDispatcher
    {
        public bool CheckAccess()
        {
            return Application.Current?.Dispatcher?.CheckAccess() ?? true;
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