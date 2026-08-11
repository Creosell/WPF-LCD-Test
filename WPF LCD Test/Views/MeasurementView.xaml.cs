using System.ComponentModel;
using System.Windows;
using System.Windows.Controls; // Нужно для создания экземпляров сервисов
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using WPF_LCD_Test.Models;
using WPF_LCD_Test.ViewModels; // Убедись, что используешь пространство имен твоего ViewModel

namespace WPF_LCD_Test.Views
{
    /// <summary>
    /// Interaction logic for MeasurementView.xaml
    /// </summary>
    public partial class MeasurementView : UserControl
    {
        public MeasurementView()
        {
            InitializeComponent(); // Инициализирует элементы UI, описанные в XAML

            //this.PreviewMouseDown += UserControl_PreviewMouseDown;
            //this.PreviewKeyDown += UserControl_PreviewKeyDown;

            this.DataContextChanged += MeasurementView_DataContextChanged;

            // Подписываемся на событие Unloaded для отписки от ViewModel
            this.Unloaded += MeasurementView_Unloaded;
        }

        private void DeviceConfigurationButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.ContextMenu != null)
            {
                button.ContextMenu.PlacementTarget = button;
                button.ContextMenu.IsOpen = true;
            }
        }

        // Containers generated inside a ContextMenu's nested Popups do not reliably invoke their
        // bound Command on click in this MaterialDesign setup, so this bridge forces the already-bound
        // Command/CommandParameter to execute; the selection logic itself stays in the ViewModel.
        private void DeviceConfigMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem { Command: ICommand command } menuItem && command.CanExecute(menuItem.CommandParameter))
            {
                command.Execute(menuItem.CommandParameter);
            }
        }

        private void SerialNumberTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox serialNumberTextBox = sender as TextBox;

            if (this.DataContext is MeasurementViewModel model)
            {
                serialNumberTextBox.Text = model.SerialNumber;
            }
        }

        // --- Обработчик события DataContextChanged этого UserControl ---
        private void MeasurementView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // Отписываемся от события PropertyChanged у предыдущего ViewModel (если он был и реализовывал интерфейс)
            if (e.OldValue is MeasurementViewModel oldViewModel)
            {
                oldViewModel.PropertyChanged -= ViewModel_PropertyChanged;
                oldViewModel.RequestClearInputFocus -= ViewModel_RequestClearInputFocus;
            }

            // Подписываемся на событие PropertyChanged у нового ViewModel (если он есть и реализует интерфейс)
            if (e.NewValue is MeasurementViewModel newViewModel)
            {
                newViewModel.PropertyChanged += ViewModel_PropertyChanged;
                newViewModel.RequestClearInputFocus += ViewModel_RequestClearInputFocus;
            }

            // Теперь ViewModel доступен через this.DataContext или sender.DataContext
        }

        // --- Обработчик события Unloaded этого UserControl (для отписки) ---
        // Срабатывает, когда UserControl удаляется из визуального дерева (например, при переключении страницы).
        private void MeasurementView_Unloaded(object sender, RoutedEventArgs e)
        {
            // Важно: отписываемся от события PropertyChanged ViewModel,
            // чтобы этот экземпляр UserControl не удерживал ViewModel в памяти
            // после того, как сам UserControl уже не отображается.
            if (this.DataContext is MeasurementViewModel viewModel)
            {
                viewModel.PropertyChanged -= ViewModel_PropertyChanged;
                viewModel.RequestClearInputFocus -= ViewModel_RequestClearInputFocus;
            }
            // TODO: Отпишитесь от любых других событий ViewModel или сервисов,
            // на которые вы подписались напрямую в коде-за этого View и
            // которые могут привести к утечкам памяти.
        }

        private void MeasurementTimeTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox measurementTimeTextBox = sender as TextBox;

            if (this.DataContext is MeasurementViewModel model)
            {
                // Convert the integer MeasurementTime to a string before assigning it to the TextBox
                measurementTimeTextBox.Text = model.MeasurementTime.ToString();
            }
        }

        // --- Обработчик события изменения коллекции лога ---
        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Если изменилось свойство LogText в ViewModel
            if (e.PropertyName == nameof(MeasurementViewModel.LogText))
            {
                // Выполняем прокрутку TextBox до конца
                // Используем Dispatcher, чтобы прокрутка произошла после обновления UI
                LogTextBox.Dispatcher.InvokeAsync(() =>
                {
                    LogTextBox.ScrollToEnd(); // Прокручиваем до конца LogTextBox
                }, System.Windows.Threading.DispatcherPriority.ContextIdle);
            }
            // TODO: Сохраните обработку изменений других свойств ViewModel, если необходимо
        }

        // --- Обработчик события запроса очистки фокуса ввода ---
        private void ViewModel_RequestClearInputFocus(object sender, EventArgs e)
        {
            this.Focus();
        }

        private void UserControl_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Получаем элемент, который в данный момент имеет клавиатурный фокус.

            // Проверяем, находится ли фокус на элементе ввода (например, TextBox),
            // где нажатие клавиши должно обрабатываться как ввод текста,
            // а не как запуск измерения.
            bool isInputControlFocused = false;
            if (Keyboard.FocusedElement is DependencyObject focusedElement)
            {
                // Проверяем, является ли элемент текстовым полем.
                if (focusedElement is TextBox)
                {
                    isInputControlFocused = true;
                }
            }

            // Если фокус находится на элементе ввода, просто выходим,
            // позволяя элементу ввода обработать нажатие клавиши

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
                    case Key.D1: case Key.NumPad1: measurementPointName = MeasurementLocation.TopLeft.ToString(); break;
                    case Key.D2: case Key.NumPad2: measurementPointName = MeasurementLocation.TopCenter.ToString(); break;
                    case Key.D3: case Key.NumPad3: measurementPointName = MeasurementLocation.TopRight.ToString(); break;
                    case Key.D4: case Key.NumPad4: measurementPointName = MeasurementLocation.MiddleLeft.ToString(); break;
                    case Key.D5: case Key.NumPad5: measurementPointName = MeasurementLocation.Center.ToString(); break;
                    case Key.D6: case Key.NumPad6: measurementPointName = MeasurementLocation.MiddleRight.ToString(); break;
                    case Key.D7: case Key.NumPad7: measurementPointName = MeasurementLocation.BottomLeft.ToString(); break;
                    case Key.D8: case Key.NumPad8: measurementPointName = MeasurementLocation.BottomCenter.ToString(); break;
                    case Key.D9: case Key.NumPad9: measurementPointName = MeasurementLocation.BottomRight.ToString(); break;

                    case Key.R: measurementPointName = MeasurementLocation.RedColor.ToString(); break;
                    case Key.G: measurementPointName = MeasurementLocation.GreenColor.ToString(); break;
                    case Key.B: measurementPointName = MeasurementLocation.BlueColor.ToString(); break;
                    case Key.W: measurementPointName = MeasurementLocation.WhiteColor.ToString(); break;
                    case Key.D0: case Key.NumPad0: measurementPointName = MeasurementLocation.BlackColor.ToString(); break;

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

        private void UserControl_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            // Получаем элемент, который был изначально кликнут мышкой
            // e.OriginalSource указывает на самый глубокий элемент под курсором.

            // Флаг, который покажет, был ли клик на (или внутри) интерактивного контрола, который может получать фокус.
            bool clickedOnFocusableControl = false;

            if (e.OriginalSource is DependencyObject originalSource)
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
                        current is ComboBoxItem || // Элемент выпадающего списка
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
                //Keyboard.ClearFocus();

                this.Focus();
                e.Handled = true;
            }
        }
    }
}