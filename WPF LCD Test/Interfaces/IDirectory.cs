// В папке Interfaces
// Файл IDirectory.cs
namespace WPF_LCD_Test.Interfaces
{
    public interface IDirectory
    {
        bool Exists(string path);
        void CreateDirectory(string path);
        // Можно добавить другие методы, если Directory.xyz используется в будущем
        // void Delete(string path, bool recursive);
    }
}