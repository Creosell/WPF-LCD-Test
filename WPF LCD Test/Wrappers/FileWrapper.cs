// В папке Wrappers
// Файл FileWrapper.cs
using System.IO;
using WPF_LCD_Test.Interfaces; // Убедитесь, что это правильное пространство имен

namespace WPF_LCD_Test.Wrappers
{
    /// <summary>
    /// Wrapper around System.IO.File for testability.
    /// Provides an abstraction layer over file system operations to enable mocking in unit tests.
    /// </summary>
    public class FileWrapper : IFile
    {
        /// <summary>
        /// Asynchronously writes text to a file.
        /// </summary>
        /// <param name="path">The file path to write to.</param>
        /// <param name="contents">The text content to write.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public Task WriteAllTextAsync(string path, string contents)
        {
            return File.WriteAllTextAsync(path, contents);
        }

        /// <summary>
        /// Asynchronously reads all text from a file.
        /// </summary>
        /// <param name="path">The file path to read from.</param>
        /// <returns>A task containing the file contents as a string.</returns>
        public Task<string> ReadAllTextAsync(string path)
        {
            return File.ReadAllTextAsync(path);
        }

        /// <summary>
        /// Checks if a file exists at the specified path.
        /// </summary>
        /// <param name="path">The file path to check.</param>
        /// <returns>True if the file exists; otherwise, false.</returns>
        public bool Exists(string path)
        {
            return File.Exists(path);
        }

        /// <summary>
        /// Deletes the specified file.
        /// </summary>
        /// <param name="path">The path of the file to delete.</param>
        public void Delete(string path)
        {
            File.Delete(path);
        }

        /// <summary>
        /// Copies an existing file to a new file.
        /// </summary>
        /// <param name="sourceFileName">The file to copy.</param>
        /// <param name="destFileName">The name of the destination file.</param>
        public void Copy(string sourceFileName, string destFileName)
        {
            File.Copy(sourceFileName, destFileName);
        }

        /// <summary>
        /// Copies an existing file to a new file, optionally overwriting an existing file.
        /// </summary>
        /// <param name="sourceFileName">The file to copy.</param>
        /// <param name="destFileName">The name of the destination file.</param>
        /// <param name="overwrite">True to allow an existing file to be overwritten; otherwise, false.</param>
        public void Copy(string sourceFileName, string destFileName, bool overwrite)
        {
            File.Copy(sourceFileName, destFileName, overwrite);
        }

        /// <summary>
        /// Moves a file to a new location.
        /// </summary>
        /// <param name="sourceFileName">The file to move.</param>
        /// <param name="destFileName">The path to move the file to.</param>
        public void Move(string sourceFileName, string destFileName)
        {
            File.Move(sourceFileName, destFileName);
        }

        /// <summary>
        /// Opens a FileStream on the specified path with the specified mode.
        /// </summary>
        /// <param name="path">The file to open.</param>
        /// <param name="mode">The mode to open the file in.</param>
        /// <returns>A FileStream opened in the specified mode.</returns>
        public Stream Open(string path, FileMode mode)
        {
            return File.Open(path, mode);
        }

        /// <summary>
        /// Opens a FileStream on the specified path with the specified mode and access.
        /// </summary>
        /// <param name="path">The file to open.</param>
        /// <param name="mode">The mode to open the file in.</param>
        /// <param name="access">The access level for the file.</param>
        /// <returns>A FileStream opened with the specified mode and access.</returns>
        public Stream Open(string path, FileMode mode, FileAccess access)
        {
            return File.Open(path, mode, access);
        }

        /// <summary>
        /// Opens a FileStream on the specified path with the specified mode, access, and sharing options.
        /// </summary>
        /// <param name="path">The file to open.</param>
        /// <param name="mode">The mode to open the file in.</param>
        /// <param name="access">The access level for the file.</param>
        /// <param name="share">The file sharing mode.</param>
        /// <returns>A FileStream opened with the specified parameters.</returns>
        public Stream Open(string path, FileMode mode, FileAccess access, FileShare share)
        {
            return File.Open(path, mode, access, share);
        }
    }
}