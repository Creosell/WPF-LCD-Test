// В папке Commands
// Файл RelayCommand.cs

using System.Windows.Input;

namespace WPF_LCD_Test.Commands
{
    public class RelayCommand : ICommand
    {
        // Меняем поля для поддержки как синхронных (Action), так и асинхронных (Func<Task>) execute методов
        private readonly Action<object>? _execute; // Для синхронных методов

        private readonly Func<object, Task>? _executeAsync; // Для асинхронных методов

        private readonly Func<object, bool>? _canExecute;

        // Поле для отслеживания выполнения асинхронной команды (опционально, но полезно для CanExecute)
        private bool _isExecuting;

        // Событие CanExecuteChanged
        public event EventHandler? CanExecuteChanged;

        // --- Добавляем новые конструкторы для асинхронных команд ---

        // Конструктор для асинхронной команды с параметром
        public RelayCommand(Func<object, Task> executeAsync, Func<object, bool>? canExecute = null)
        {
            _executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
            if (canExecute != null)
            {
                _canExecute = canExecute;
            }
            _execute = null; // Указываем, что синхронный execute не используется
        }

        // Конструктор для асинхронной команды без параметра
        public RelayCommand(Func<Task> executeAsync, Func<bool>? canExecute = null)
             : this(async (p) => await executeAsync(), (p) => canExecute?.Invoke() ?? true) // Переиспользуем основной асинхронный конструктор
        {
            ArgumentNullException.ThrowIfNull(executeAsync);
        }

        // --- Оставляем старые конструкторы для синхронных команд ---

        // Конструктор для синхронной команды с параметром
        public RelayCommand(Action<object> execute, Func<object, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            if (canExecute != null)
            {
                _canExecute = canExecute;
            }
            _executeAsync = null; // Указываем, что асинхронный execute не используется
        }

        // Конструктор для синхронной команды без параметра
        public RelayCommand(Action execute, Func<bool>? canExecute = null)
             : this((p) => execute?.Invoke(), (p) => canExecute?.Invoke() ?? true) // Переиспользуем основной синхронный конструктор
        {
            ArgumentNullException.ThrowIfNull(execute);
        }

        // --- Методы из интерфейса ICommand ---

        // Проверка доступности команды
        public bool CanExecute(object? parameter)
        {
            // Команда доступна, если она не выполняется прямо сейчас (для асинхронных)
            // И если метод CanExecute разрешает выполнение
            bool baseCanExecute = _canExecute == null || _canExecute(parameter);

            // Если команда асинхронная, добавляем проверку _isExecuting
            if (_executeAsync != null)
            {
                return !_isExecuting && baseCanExecute;
            }

            // Если команда синхронная
            return baseCanExecute;
        }

        // Выполнение команды
        public async void Execute(object? parameter) // Используем async void здесь для поддержки await внутри Execute
        {
            if (!CanExecute(parameter)) return;

            // Устанавливаем флаг выполнения и уведомляем UI (отключаем кнопку)
            _isExecuting = true;
            RaiseCanExecuteChanged(); // Уведомляем UI, чтобы обновить состояние (например, IsEnabled)

            try
            {
                if (_execute != null) // Если это синхронная команда
                {
                    _execute(parameter); // Просто выполняем синхронный метод
                }
                else if (_executeAsync != null) // Если это асинхронная команда
                {
                    await _executeAsync(parameter); // Ожидаем завершения асинхронного метода ViewModel
                }
            }
            catch (Exception ex)
            {
                // ОБРАБОТКА ИСКЛЮЧЕНИЙ:
                // async void методы, которые выбрасывают исключения, по умолчанию "роняют" приложение.
                // Здесь в RelayCommand мы перехватываем исключение из async Task метода ViewModel.
                // Можно залогировать ошибку, показать сообщение пользователю и т.д.
                System.Diagnostics.Debug.WriteLine($"Ошибка при выполнении команды: {ex.Message}");
                // Возможно, нужно передать ошибку дальше или обработать ее более централизованно
            }
            finally
            {
                // Сбрасываем флаг выполнения и уведомляем UI (включаем кнопку)
                _isExecuting = false;
                RaiseCanExecuteChanged(); // Уведомляем UI после завершения (независимо от успеха)
            }
        }

        // --- Метод для принудительного уведомления WPF об изменении CanExecute ---
        // Этот публичный метод мы вызываем из ViewModel
        public void RaiseCanExecuteChanged()
        {
            // Вызываем событие CanExecuteChanged
            // Проверка на null важна
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}