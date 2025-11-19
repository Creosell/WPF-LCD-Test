// В папке Wrappers
// Файл DirectoryWrapper.cs
using System.IO;
using WPF_LCD_Test.Interfaces;

namespace WPF_LCD_Test.Wrappers
{
    public class DirectoryWrapper : IDirectory
    {
        public bool Exists(string path)
        {
            return Directory.Exists(path);
        }

        public void CreateDirectory(string path)
        {
            Directory.CreateDirectory(path);
        }

        public IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption)
        {
            return Directory.EnumerateFiles(path, searchPattern, searchOption);
        }
    }
}