using System.Windows.Input;

namespace WPF_LCD_Test.Commands
{
    public class RelayCommand : ICommand
    {
        private readonly Action<object>? _execute;
        private readonly Func<object, Task>? _executeAsync;
        private readonly Func<object, bool>? _canExecute;
        private bool _isExecuting;

        public event EventHandler? CanExecuteChanged;

        // Async command with parameter
        public RelayCommand(Func<object, Task> executeAsync, Func<object, bool>? canExecute = null)
        {
            _executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
            _canExecute = canExecute;
        }

        // Async command without parameter
        public RelayCommand(Func<Task> executeAsync, Func<bool>? canExecute = null)
            : this(async (p) => await executeAsync(), (p) => canExecute?.Invoke() ?? true)
        {
            ArgumentNullException.ThrowIfNull(executeAsync);
        }

        // Sync command with parameter
        public RelayCommand(Action<object> execute, Func<object, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        // Sync command without parameter
        public RelayCommand(Action execute, Func<bool>? canExecute = null)
            : this((p) => execute?.Invoke(), (p) => canExecute?.Invoke() ?? true)
        {
            ArgumentNullException.ThrowIfNull(execute);
        }

        public bool CanExecute(object? parameter)
        {
            bool baseCanExecute = _canExecute?.Invoke(parameter) ?? true;
            return _executeAsync != null ? !_isExecuting && baseCanExecute : baseCanExecute;
        }

        public async void Execute(object? parameter)
        {
            if (!CanExecute(parameter)) return;

            _isExecuting = true;
            RaiseCanExecuteChanged();

            try
            {
                if (_execute != null)
                    _execute(parameter);
                else if (_executeAsync != null)
                    await _executeAsync(parameter);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Command execution error: {ex.Message}");
            }
            finally
            {
                _isExecuting = false;
                RaiseCanExecuteChanged();
            }
        }

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);

        /// <summary>
        /// Executes the async command and returns the result.
        /// Used primarily in unit tests to await async command execution.
        /// </summary>
        public async Task<object?> ExecuteAsync(object? parameter)
        {
            if (!CanExecute(parameter))
                return false;

            _isExecuting = true;
            RaiseCanExecuteChanged();

            try
            {
                if (_execute != null)
                {
                    _execute(parameter);
                    return true;
                }
                else if (_executeAsync != null)
                {
                    var task = _executeAsync(parameter);
                    await task;

                    // If the task returns a value (Task<T>), extract and return it
                    if (task.GetType().IsGenericType)
                    {
                        var resultProperty = task.GetType().GetProperty("Result");
                        return resultProperty?.GetValue(task);
                    }

                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Command execution error: {ex.Message}");
                throw;
            }
            finally
            {
                _isExecuting = false;
                RaiseCanExecuteChanged();
            }
        }
    }
}