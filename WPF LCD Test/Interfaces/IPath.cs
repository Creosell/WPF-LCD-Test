namespace WPF_LCD_Test.Interfaces
{
    /// <summary>
    /// Interface for path operations, enabling testability through abstraction.
    /// </summary>
    public interface IPath
    {
        /// <summary>
        /// Combines multiple path strings into a single path.
        /// </summary>
        /// <param name="paths">An array of path strings to combine.</param>
        /// <returns>The combined path.</returns>
        string Combine(params string[] paths);

        /// <summary>
        /// Returns the file name and extension of the specified path string.
        /// </summary>
        /// <param name="path">The path string from which to extract the file name.</param>
        /// <returns>The file name and extension.</returns>
        string GetFileName(string path);
    }
}