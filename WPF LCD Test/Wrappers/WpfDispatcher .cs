// В папке Wrappers
//Файл WpfDispatcher.cs
using System.Windows;
using WPF_LCD_Test.Interfaces;

namespace WPF_LCD_Test.Services
{
    /// <summary>
    /// Wrapper around WPF Dispatcher for testability.
    /// Provides an abstraction layer over the WPF dispatcher to enable thread marshalling and mocking in unit tests.
    /// </summary>
    public class WpfDispatcher : IDispatcher
    {
        /// <summary>
        /// Determines whether the calling thread is the dispatcher thread.
        /// </summary>
        /// <returns>True if the calling thread is the dispatcher thread; otherwise, false.</returns>
        public bool CheckAccess()
        {
            return Application.Current?.Dispatcher?.CheckAccess() ?? true;
        }

        /// <summary>
        /// Executes the specified action synchronously on the dispatcher thread.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        public void Invoke(Action action)
        {
            Application.Current?.Dispatcher?.Invoke(action);
        }

        /// <summary>
        /// Executes the specified action asynchronously on the dispatcher thread.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        public void BeginInvoke(Action action)
        {
            Application.Current?.Dispatcher?.BeginInvoke(action);
        }
    }
}