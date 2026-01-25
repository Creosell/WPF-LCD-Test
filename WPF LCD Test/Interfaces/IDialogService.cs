namespace WPF_LCD_Test.Interfaces
{
    /// <summary>
    /// Interface for dialog operations, enabling testability through abstraction.
    /// </summary>
    public interface IDialogService
    {
        /// <summary>
        /// Displays a question dialog with Yes/No buttons.
        /// </summary>
        /// <param name="message">The question message to display.</param>
        /// <param name="caption">The dialog title.</param>
        /// <returns>True if user clicked Yes; false if user clicked No.</returns>
        bool ShowQuestion(string message, string caption);

        /// <summary>
        /// Displays an informational message dialog with OK button.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="caption">The dialog title.</param>
        void ShowMessage(string message, string caption);
    }
}