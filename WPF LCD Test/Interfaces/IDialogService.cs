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

        /// <summary>
        /// Displays a dialog with two custom-labelled options.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="caption">The dialog title.</param>
        /// <param name="firstOption">Caption of the first option.</param>
        /// <param name="secondOption">Caption of the second option.</param>
        /// <returns>0 or 1 for the chosen option; null if the dialog was closed without a choice.</returns>
        int? ShowChoice(string message, string caption, string firstOption, string secondOption);
    }
}