using System.IO;

namespace WPF_LCD_Test.Interfaces
    {
    /// <summary>
    /// Interface wrapper for file system operations, enabling testability.
    /// </summary>
    public interface IFile
        {
        /// <summary>
        /// Asynchronously writes text to a file.
        /// </summary>
        /// <param name="path">File path to write to.</param>
        /// <param name="contents">Text content to write.</param>
        Task WriteAllTextAsync(string path, string contents);

        /// <summary>
        /// Asynchronously reads all text from a file.
        /// </summary>
        /// <param name="path">File path to read from.</param>
        /// <returns>File content as string.</returns>
        Task<string> ReadAllTextAsync(string path);

        /// <summary>
        /// Checks if file exists at specified path.
        /// </summary>
        /// <param name="path">File path to check.</param>
        /// <returns>True if file exists, false otherwise.</returns>
        bool Exists(string path);

        /// <summary>
        /// Deletes the specified file.
        /// </summary>
        /// <param name="path">File path to delete.</param>
        void Delete(string path);

        /// <summary>
        /// Copies file to a new location.
        /// </summary>
        /// <param name="sourceFileName">Source file path.</param>
        /// <param name="destFileName">Destination file path.</param>
        void Copy(string sourceFileName, string destFileName);

        /// <summary>
        /// Copies file to a new location with overwrite option.
        /// </summary>
        /// <param name="sourceFileName">Source file path.</param>
        /// <param name="destFileName">Destination file path.</param>
        /// <param name="overwrite">True to overwrite existing file.</param>
        void Copy(string sourceFileName, string destFileName, bool overwrite);

        /// <summary>
        /// Moves file to a new location.
        /// </summary>
        /// <param name="sourceFileName">Source file path.</param>
        /// <param name="destFileName">Destination file path.</param>
        void Move(string sourceFileName, string destFileName);

        /// <summary>
        /// Opens file stream with specified mode.
        /// </summary>
        /// <param name="path">File path to open.</param>
        /// <param name="mode">File open mode.</param>
        /// <returns>File stream.</returns>
        Stream Open(string path, FileMode mode);

        /// <summary>
        /// Opens file stream with specified mode and access.
        /// </summary>
        /// <param name="path">File path to open.</param>
        /// <param name="mode">File open mode.</param>
        /// <param name="access">File access rights.</param>
        /// <returns>File stream.</returns>
        Stream Open(string path, FileMode mode, FileAccess access);

        /// <summary>
        /// Opens file stream with specified mode, access, and sharing.
        /// </summary>
        /// <param name="path">File path to open.</param>
        /// <param name="mode">File open mode.</param>
        /// <param name="access">File access rights.</param>
        /// <param name="share">File sharing mode.</param>
        /// <returns>File stream.</returns>
        Stream Open(string path, FileMode mode, FileAccess access, FileShare share);
        }
    }