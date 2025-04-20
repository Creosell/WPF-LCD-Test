// В файле MainWindow.xaml.cs (код позади MainWindow.xaml)

using System.Windows;
using WPF_LCD_Test.ViewModels; // Убедись, что используешь пространство имен твоего ViewModel
using WPF_LCD_Test.Services; // Нужно для создания экземпляров сервисов

namespace WPF_LCD_Test // Пространство имен твоего приложения
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent(); // Инициализирует элементы UI, описанные в XAML


            // --- Установка DataContext на экземпляр ViewModel ---
            // В реальном приложении здесь используется Инъекция Зависимостей (DI)
            // Для простоты, пока создадим экземпляры сервисов вручную и передадим их в ViewModel

            // Создаем экземпляры сервисов
            // Убедись, что у тебя есть классы ColorMeasurementService, FileService, DialogService
            IColorMeasurementService colorMeasurementService = new ColorMeasurementService(); // Реализация сервиса прибора
            IFileService fileService = new FileService(); // Реализация сервиса файлов
            IDialogService dialogService = new DialogService(); // Реализация сервиса диалогов

            // Создаем экземпляр ViewModel, передавая ему зависимости (сервисы)
            MainWindowViewModel viewModel = new MainWindowViewModel(colorMeasurementService, fileService, dialogService);

            // Устанавливаем DataContext окна на созданный ViewModel
            this.DataContext = viewModel;

            // Опционально: Если ViewModel реализует IDisposable, подписываемся на событие закрытия окна
            // для корректной очистки ресурсов ViewModel при закрытии окна.
            this.Closed += (sender, e) =>
            {
                (this.DataContext as IDisposable)?.Dispose();
            };
        }
    }
}