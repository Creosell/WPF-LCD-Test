using System.IO;

namespace WPF_LCD_Test.Interfaces
{
    /// <summary>
    /// Interface for directory operations, enabling testability through abstraction.
    /// </summary>
    public interface IDirectory
    {
        /// <summary>
        /// Determines whether the specified directory exists.
        /// </summary>
        /// <param name="path">The directory path to check.</param>
        /// <returns>True if the directory exists; otherwise, false.</returns>
        bool Exists(string path);

        /// <summary>
        /// Creates a directory at the specified path.
        /// </summary>
        /// <param name="path">The directory path to create.</param>
        void CreateDirectory(string path);

        /// <summary>
        /// Retrieves file names matching a search pattern in a specified path, including an option for searching subdirectories.
        /// </summary>
        /// <param name="path">The directory path to search.</param>
        /// <param name="searchPattern">The search pattern to match against file names.</param>
        /// <param name="searchOption">Specifies whether to search subdirectories.</param>
        /// <returns>An enumerable collection of file names that match the search pattern.</returns>
        IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption);
    }
}