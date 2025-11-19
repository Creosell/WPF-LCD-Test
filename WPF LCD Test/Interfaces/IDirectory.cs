using System.IO;

namespace WPF_LCD_Test.Interfaces
{
    public interface IDirectory
    {
        bool Exists(string path);

        void CreateDirectory(string path);

        /// <summary>
        /// Retrieves file names matching a search pattern in a specified path, including an option for searching subdirectories.
        /// </summary>
        IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption);
    }
}