// В папке Interfaces
// Файл IFile.cs
using System.IO;
using System.Threading.Tasks;

namespace WPF_LCD_Test.Interfaces
{
    public interface IFile
    {
        /// <summary>
        /// Асинхронно записывает указанный текст в файл по указанному пути.
        /// Если файл существует, он перезаписывается.
        /// </summary>
        /// <param name="path">Путь к файлу.</param>
        /// <param name="contents">Текст для записи.</param>
        /// <returns>Задача, представляющая асинхронную операцию записи.</returns>
        Task WriteAllTextAsync(string path, string contents);

        /// <summary>
        /// Асинхронно читает весь текст из файла по указанному пути.
        /// </summary>
        /// <param name="path">Путь к файлу.</param>
        /// <returns>Задача, представляющая асинхронную операцию чтения. Результат задачи - строка, содержащая весь текст файла.</returns>
        Task<string> ReadAllTextAsync(string path);

        /// <summary>
        /// Определяет, существует ли указанный файл.
        /// </summary>
        /// <param name="path">Путь к файлу для проверки.</param>
        /// <returns>true, если файл существует; в противном случае - false.</returns>
        bool Exists(string path);

        /// <summary>
        /// Удаляет указанный файл.
        /// </summary>
        /// <param name="path">Путь к файлу для удаления.</param>
        void Delete(string path);

        /// <summary>
        /// Копирует существующий файл в новый файл. Перезапись не допускается.
        /// </summary>
        /// <param name="sourceFileName">Путь к копируемому файлу.</param>
        /// <param name="destFileName">Путь к целевому файлу.</param>
        void Copy(string sourceFileName, string destFileName);

        /// <summary>
        /// Копирует существующий файл в новый файл.
        /// </summary>
        /// <param name="sourceFileName">Путь к копируемому файлу.</param>
        /// <param name="destFileName">Путь к целевому файлу.</param>
        /// <param name="overwrite">true, чтобы перезаписать целевой файл, если он существует; в противном случае - false.</param>
        void Copy(string sourceFileName, string destFileName, bool overwrite);

        /// <summary>
        /// Перемещает указанный файл в новое место, предоставляя возможность указать новое имя файла.
        /// </summary>
        /// <param name="sourceFileName">Путь к файлу, который нужно переместить.</param>
        /// <param name="destFileName">Путь к новому расположению файла и его новому имени.</param>
        void Move(string sourceFileName, string destFileName);

        /// <summary>
        /// Открывает Stream для чтения, записи или и того, и другого.
        /// </summary>
        /// <param name="path">Путь к файлу.</param>
        /// <param name="mode">Режим открытия файла.</param>
        /// <returns>Новый FileStream.</returns>
        Stream Open(string path, FileMode mode);

        /// <summary>
        /// Открывает Stream для чтения, записи или и того, и другого с указанным доступом.
        /// </summary>
        /// <param name="path">Путь к файлу.</param>
        /// <param name="mode">Режим открытия файла.</param>
        /// <param name="access">Доступ к файлу.</param>
        /// <returns>Новый FileStream.</returns>
        Stream Open(string path, FileMode mode, FileAccess access);

        /// <summary>
        /// Открывает Stream для чтения, записи или и того, и другого с указанным доступом и совместным доступом.
        /// </summary>
        /// <param name="path">Путь к файлу.</param>
        /// <param name="mode">Режим открытия файла.</param>
        /// <param name="access">Доступ к файлу.</param>
        /// <param name="share">Режим совместного доступа к файлу.</param>
        /// <returns>Новый FileStream.</returns>
        Stream Open(string path, FileMode mode, FileAccess access, FileShare share);
    }
}