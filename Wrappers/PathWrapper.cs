// В папке Wrappers
// Файл PathWrapper.cs
using System.IO;
using WPF_LCD_Test.Interfaces;

namespace WPF_LCD_Test.Wrappers
{
    public class PathWrapper : IPath
    {
        public string Combine(params string[] paths)
        {
            return Path.Combine(paths);
        }
    }
}