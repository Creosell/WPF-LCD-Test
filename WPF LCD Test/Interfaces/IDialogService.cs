namespace WPF_LCD_Test.Interfaces
{
    public interface IDialogService
    {
        bool ShowQuestion(string message, string caption);
        void ShowMessage(string message, string caption);
    }
}