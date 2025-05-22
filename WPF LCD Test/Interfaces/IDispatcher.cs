// В файле Interfaces/IDispatcher.cs (или в другом подходящем месте)
using System;
using System.Windows.Threading; // Для DispatcherPriority, если вам это нужно в Invoke/BeginInvoke

namespace WPF_LCD_Test.Interfaces
{
    public interface IDispatcher
    {
        bool CheckAccess();
        void Invoke(Action action); // Если нужно Invoke с DispatcherPriority, добавьте перегрузки

       
        void BeginInvoke(Action action); // Если нужно BeginInvoke с DispatcherPriority, добавьте перегрузки
    }
}