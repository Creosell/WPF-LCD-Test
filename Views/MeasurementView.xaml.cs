using System.Windows;
using System.Windows.Controls; // Нужно для создания экземпляров сервисов
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using WPF_LCD_Test.Services;
using WPF_LCD_Test.ViewModels; // Убедись, что используешь пространство имен твоего ViewModel

namespace WPF_LCD_Test.Views
{
    /// <summary>
    /// Interaction logic for MeasurementView.xaml
    /// </summary>
    public partial class MeasurementView : UserControl
    {
        private MeasurementViewModel _viewModel;
        public MeasurementView()
        {
            InitializeComponent(); // Инициализирует элементы UI, описанные в XAML

            this.PreviewMouseDown += Window_PreviewMouseDown;
            this.PreviewKeyDown += Window_PreviewKeyDown;
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
            _viewModel = new MeasurementViewModel(colorMeasurementService, fileService, dialogService, localizationService);

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
            //this.Closed += (sender, e) =>
            //{
            //    // Отписка от события CollectionChanged
            //    if (_viewModel != null && _viewModel.LogMessages != null)
            //    {
            //        _viewModel.LogMessages.CollectionChanged -= LogMessages_CollectionChanged;
            //    }
            //    // Вызов Dispose у ViewModel, если он реализует IDisposable
            //    (_viewModel as IDisposable)?.Dispose();
            //};
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

        private void Window_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            // Получаем элемент, который был изначально кликнут мышкой
            // e.OriginalSource указывает на самый глубокий элемент под курсором.
            DependencyObject originalSource = e.OriginalSource as DependencyObject;

            // Флаг, который покажет, был ли клик на (или внутри) интерактивного контрола, который может получать фокус.
            bool clickedOnFocusableControl = false;

            if (originalSource != null)
            {
                // Проходим вверх по визуальному дереву от кликнутого элемента.
                // Это нужно, чтобы поймать клик, если он был на дочернем элементе контрола
                // (например, на TextBlock внутри Button, или на части шаблона ListBox).
                DependencyObject current = originalSource;
                while (current != null)
                {
                    // Проверяем, является ли текущий элемент (или его предок) одним из типов контролов,
                    // которые обычно могут получать клавиатурный фокус и с которых мы хотим его снимать
                    // при клике вне их области.
                    if (current is TextBox ||
                        current is Button ||
                        current is ListBox ||
                        current is Menu ||       // Элемент меню
                        current is MenuItem ||   // Отдельный пункт меню
                        current is ComboBox ||   // Выпадающий список
                        current is CheckBox ||   // Флажок
                        current is RadioButton || // Переключатель
                        current is Slider ||     // Слайдер
                        current is ScrollBar  // Полоса прокрутки
                                              // Можете добавить другие типы контролов, если это необходимо
                                              // Например: current is DataGrid, current is TabControl и т.д.
                                              // Более общий, но потенциально агрессивный вариант: current is Control && ((Control)current).Focusable
                       )
                    {
                        clickedOnFocusableControl = true;
                        break; // Если нашли такой контрол в дереве, значит, клик был "внутри" него. Останавливаем поиск.
                    }

                    // Переходим к следующему родителю в визуальном дереве
                    current = VisualTreeHelper.GetParent(current);
                }
            }

            // Если клик НЕ произошел на (или внутри) одного из перечисленных выше контролов,
            // то считаем, что это клик по "пустому" месту и снимаем клавиатурный фокус.
            if (!clickedOnFocusableControl)
            {
                // Снимаем клавиатурный фокус с текущего элемента.
                Keyboard.ClearFocus();

                // Опционально: Вы можете установить фокус на само окно.
                // Это может быть полезно, если вы хотите, чтобы окно получало
                // определенные события клавиатуры после снятия фокуса с контрола.
                // this.Focus();
            }
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Получаем элемент, который в данный момент имеет клавиатурный фокус.
            DependencyObject focusedElement = Keyboard.FocusedElement as DependencyObject;

            // Проверяем, находится ли фокус на элементе ввода (например, TextBox),
            // где нажатие клавиши должно обрабатываться как ввод текста,
            // а не как запуск измерения.
            bool isInputControlFocused = false;
            if (focusedElement != null)
            {
                // Проверяем, является ли элемент текстовым полем.
                if (focusedElement is TextBox)
                {
                    isInputControlFocused = true;
                }
            }

            // Если фокус находится на элементе ввода, просто выходим,
            // позволяя элементу ввода обработать нажатие клавиши.
            // Мы не устанавливаем e.Handled = true, чтобы символ появился в поле ввода.
            if (isInputControlFocused)
            {
                return;
            }

            // Если фокус НЕ на элементе ввода, проверяем, какая клавиша была нажата,
            // чтобы запустить соответствующую команду измерения.
            // Нам нужен доступ к ViewModel. Предполагаем, что ViewModel установлен
            // как DataContext окна (this.DataContext).
            if (this.DataContext is MeasurementViewModel viewModel)
            {
                string measurementPointName = null; // Переменная для хранения имени точки измерения (параметра команды)

                // Используем оператор switch для определения нажатой клавиши.
                // WPF использует перечисление Key.
                switch (e.Key)
                {
                    case Key.D1: measurementPointName = "Top left"; break;
                    case Key.D2: measurementPointName = "Top center"; break;
                    case Key.D3: measurementPointName = "Top right"; break;
                    case Key.D4: measurementPointName = "Middle left"; break;
                    case Key.D5: measurementPointName = "Center"; break;
                    case Key.D6: measurementPointName = "Middle right"; break;
                    case Key.D7: measurementPointName = "Bottom left"; break;
                    case Key.D8: measurementPointName = "Bottom center"; break;
                    case Key.D9: measurementPointName = "Bottom right"; break;
                    case Key.D0: measurementPointName = "Black"; break;

                    case Key.R: measurementPointName = "Red"; break;
                    case Key.G: measurementPointName = "Green"; break;
                    case Key.B: measurementPointName = "Blue"; break;

                    case Key.NumPad1: measurementPointName = "Top left"; break;
                    case Key.NumPad2: measurementPointName = "Top center"; break;
                    case Key.NumPad3: measurementPointName = "Top right"; break;
                    case Key.NumPad4: measurementPointName = "Middle left"; break;
                    case Key.NumPad5: measurementPointName = "Center"; break;
                    case Key.NumPad6: measurementPointName = "Middle right"; break;
                    case Key.NumPad7: measurementPointName = "Bottom left"; break;
                    case Key.NumPad8: measurementPointName = "Bottom center"; break;
                    case Key.NumPad9: measurementPointName = "Bottom right"; break;
                    case Key.NumPad0: measurementPointName = "Black"; break;
                }

                // Если нажатая клавиша соответствует одной из точек измерения (т.е. measurementPointName != null)
                if (measurementPointName != null)
                {
                    // Получаем команду MeasureCommand из ViewModel
                    ICommand measureCommand = viewModel.MeasureCommand;

                    if (measureCommand != null && measureCommand.CanExecute(measurementPointName))
                    {
                        measureCommand.Execute(measurementPointName);

                        // Отмечаем событие как обработанное.
                        // Это ОЧЕНЬ ВАЖНО, чтобы нажатие клавиши не обрабатывалось другими элементами
                        // после того, как мы использовали его для запуска команды (например, чтобы
                        // нажатие '1' не появилось в поле ввода, если фокус был на чем-то другом, кроме TextBox).
                        e.Handled = true;
                    }
                }
            }
        }
    }
}
    


