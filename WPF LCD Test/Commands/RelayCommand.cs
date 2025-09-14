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

        public RelayCommand(Func<object, Task> executeAsync, Func<object, bool>? canExecute = null)
        {
            _executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
            _canExecute = canExecute;
        }
        public RelayCommand(Func<Task> executeAsync, Func<bool>? canExecute = null)
            : this(async (p) => await executeAsync(), (p) => canExecute?.Invoke() ?? true) { }
        public RelayCommand(Action<object> execute, Func<object, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }
        public RelayCommand(Action execute, Func<bool>? canExecute = null)
            : this((p) => execute?.Invoke(), (p) => canExecute?.Invoke() ?? true) { }

        public bool CanExecute(object? parameter)
        {
            bool baseCanExecute = _canExecute == null || _canExecute(parameter);
            return _executeAsync != null ? !_isExecuting && baseCanExecute : baseCanExecute;
        }

        public async void Execute(object? parameter)
        {
            if (!CanExecute(parameter)) return;
            _isExecuting = true;
            RaiseCanExecuteChanged();
            try
            {
                if (_execute != null) _execute(parameter);
                else if (_executeAsync != null) await _executeAsync(parameter);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"RelayCommand error: {ex.Message}");
            }
            finally
            {
                _isExecuting = false;
                RaiseCanExecuteChanged();
            }
        }

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}