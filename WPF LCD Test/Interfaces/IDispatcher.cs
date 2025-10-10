namespace WPF_LCD_Test.Interfaces
{
    public interface IDispatcher
    {
        bool CheckAccess();

        void Invoke(Action action);

        void BeginInvoke(Action action);
    }
}