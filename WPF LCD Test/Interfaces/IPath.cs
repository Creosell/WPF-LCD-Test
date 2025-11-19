namespace WPF_LCD_Test.Interfaces
{
    public interface IPath
    {
        string Combine(params string[] paths);

        /// <summary>
        /// Returns the file name and extension of the specified path string.
        /// </summary>
        string GetFileName(string path);
    }
}