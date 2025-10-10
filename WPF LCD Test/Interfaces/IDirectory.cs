namespace WPF_LCD_Test.Interfaces
{
    public interface IDirectory
    {
        bool Exists(string path);

        void CreateDirectory(string path);
    }
}