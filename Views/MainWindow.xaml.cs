// В файле MainWindow.xaml.cs (код позади MainWindow.xaml)

using System.Windows;
using System.Windows.Controls; // Нужно для создания экземпляров сервисов
using WPF_LCD_Test.Services;
using WPF_LCD_Test.ViewModels; // Убедись, что используешь пространство имен твоего ViewModel

namespace WPF_LCD_Test // Пространство имен твоего приложения
{
    public partial class MainWindow : Window
    {
        private MainWindowViewModel _viewModel;
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
            ILocalizationService localizationService = LocalizationService.Instance; // Реализация сервиса локализации

            // Создаем экземпляр ViewModel, передавая ему зависимости (сервисы)
            _viewModel = new MainWindowViewModel(colorMeasurementService, fileService, dialogService, localizationService);


            // Устанавливаем DataContext окна на созданный ViewModel
            this.DataContext = _viewModel;

            // --- Подписка на событие изменения коллекции лога для автопрокрутки ---
            // Убедимся, что ViewModel и LogMessages не равны null
            if (_viewModel != null && _viewModel.LogMessages != null)
            {
                _viewModel.LogMessages.CollectionChanged += LogMessages_CollectionChanged;
            }
            // --- Конец подписки ---


            // Опционально: Отписка при закрытии окна для предотвращения утечки памяти
            this.Closed += (sender, e) =>
            {
                // Отписка от события CollectionChanged
                if (_viewModel != null && _viewModel.LogMessages != null)
                {
                    _viewModel.LogMessages.CollectionChanged -= LogMessages_CollectionChanged;
                }
                // Вызов Dispose у ViewModel, если он реализует IDisposable
                (_viewModel as IDisposable)?.Dispose();
            };



        }
        private void SerialNumberTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox serialNumberTextBox = sender as TextBox;

            if (serialNumberTextBox != null && _viewModel != null)
            {
                serialNumberTextBox.Text = _viewModel.SerialNumber;
            }
        }

        private void MeasurementTimeTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox measurementTimeTextBox = sender as TextBox;

            if (measurementTimeTextBox != null && _viewModel != null)
            {
                // Convert the integer MeasurementTime to a string before assigning it to the TextBox
                measurementTimeTextBox.Text = _viewModel.MeasurementTime.ToString();
            }
        }

        // --- Обработчик события изменения коллекции лога ---
        private void LogMessages_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            // Проверяем, что было добавлено новое сообщение
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                // Убеждаемся, что ListBox существует и есть добавленные элементы
                if (LogListBox != null && e.NewItems != null && e.NewItems.Count > 0)
                {
                    // !!! ИСПРАВЛЕНИЕ ОШИБКИ: Откладываем вызов ScrollIntoView с помощью Dispatcher !!!
                    // Используем Dispatcher.InvokeAsync (предпочтительнее в .NET Core/.NET 5+)
                    // Или Dispatcher.BeginInvoke (для .NET Framework)
                    // DispatcherPriority.ContextIdle - низкий приоритет, выполняется, когда Dispatcher свободен
                    LogListBox.Dispatcher.InvokeAsync(() =>
                    {
                        // Этот код выполнится в UI-потоке после того, как ListBox обновится
                        LogListBox.ScrollIntoView(e.NewItems[0]);
                    }, System.Windows.Threading.DispatcherPriority.ContextIdle);

                    // Если используешь .NET Framework, может потребоваться:
                    // LogListBox.Dispatcher.BeginInvoke(
                    //     System.Windows.Threading.DispatcherPriority.ContextIdle,
                    //     new Action(() =>
                    //     {
                    //         LogListBox.ScrollIntoView(e.NewItems[0]);
                    //     }));
                }
            }
        }


    }


}