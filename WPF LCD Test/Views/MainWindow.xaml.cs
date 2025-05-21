

using System.Windows;
using System.Windows.Input;
using WPF_LCD_Test.Services;
using WPF_LCD_Test.ViewModels;

namespace WPF_LCD_Test.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void MinimizeImage_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            this.WindowState = WindowState.Minimized; // Сворачиваем окно
        }

        private void MaximizeRestoreImage_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Логика для переключения между Maximize и Normal
            if (this.WindowState == WindowState.Normal)
            {
                this.WindowState = WindowState.Maximized; // Развернуть окно
            }
            else
            {
                this.WindowState = WindowState.Normal; // Восстановить нормальный размер
            }
            // При желании, здесь нужно обновить иконку кнопки
        }

        private void CloseImage_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            this.Close(); // Закрываем окно
        }

        private void Grid_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            {
                // Проверяем, была ли нажата левая кнопка мыши
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    // Вызываем метод DragMove для начала перетаскивания окна
                    // Обернем в try-catch, т.к. он может выбросить исключение,
                    // если кнопка мыши отпущена слишком быстро.
                    try
                    {
                        this.DragMove();
                    }
                    catch (InvalidOperationException)
                    {
                        // Игнорируем исключение, если DragMove не может быть вызван
                    }
                }
                // Опционально: Обработка двойного клика для развертывания/восстановления
                if (e.ClickCount == 2)
                {
                    MaximizeRestoreImage_PreviewMouseLeftButtonDown(sender, e); // Вызываем логику кнопки развернуть/восстановить
                }
            }
        }
    }
}
