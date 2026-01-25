namespace WPF_LCD_Test.Interfaces
{
    /// <summary>
    /// Interface for WPF dispatcher operations, enabling thread marshalling and testability.
    /// </summary>
    public interface IDispatcher
    {
        /// <summary>
        /// Determines whether the calling thread is the dispatcher thread.
        /// </summary>
        /// <returns>True if the calling thread is the dispatcher thread; otherwise, false.</returns>
        bool CheckAccess();

        /// <summary>
        /// Executes the specified action synchronously on the dispatcher thread.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        void Invoke(Action action);

        /// <summary>
        /// Executes the specified action asynchronously on the dispatcher thread.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        void BeginInvoke(Action action);
    }
}