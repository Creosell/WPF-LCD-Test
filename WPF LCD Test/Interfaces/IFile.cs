// В папке Interfaces
// Файл IFile.cs
using System.Threading.Tasks;

namespace WPF_LCD_Test.Interfaces
{
    public interface IFile
    {
        Task WriteAllTextAsync(string path, string contents);
        // Можно добавить другие методы, если File.xyz используется в будущем
        // bool Exists(string path);
        // string ReadAllText(string path);
    }
}