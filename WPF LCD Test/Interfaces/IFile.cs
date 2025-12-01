using System.IO;

namespace WPF_LCD_Test.Interfaces
{
    public interface IFile
    {
        Task WriteAllTextAsync(string path, string contents);
        Task<string> ReadAllTextAsync(string path);
        bool Exists(string path);
        void Delete(string path);
        void Copy(string sourceFileName, string destFileName);
        void Copy(string sourceFileName, string destFileName, bool overwrite);
        void Move(string sourceFileName, string destFileName);
        Stream Open(string path, FileMode mode);
        Stream Open(string path, FileMode mode, FileAccess access);
        Stream Open(string path, FileMode mode, FileAccess access, FileShare share);
    }
}