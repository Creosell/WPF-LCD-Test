// В папке Wrappers
// Файл FileWrapper.cs
using System.IO;
using System.Threading.Tasks;
using WPF_LCD_Test.Interfaces;

namespace WPF_LCD_Test.Wrappers
{
    public class FileWrapper : IFile
    {
        public Task WriteAllTextAsync(string path, string contents)
        {
            return File.WriteAllTextAsync(path, contents);
        }
    }
}