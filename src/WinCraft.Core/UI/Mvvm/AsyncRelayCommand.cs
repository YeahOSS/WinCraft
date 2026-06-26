using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace WinCraft.UI.Mvvm
{
    /// <summary>
    /// An async-friendly <see cref="ICommand"/> that executes a
    /// <see cref="Func{Task}"/>. Exposes <see cref="IsExecuting"/> so the UI
    /// can show progress or disable controls during execution. Re-entrant calls
    /// are ignored while a previous invocation is still running.
    /// </summary>
    /// <param name="execute">The async delegate to run.</param>
    /// <param name="canExecute">
    /// Optional guard. When <c>null</c>, the command is executable
    /// whenever it is not already executing.
    /// </param>
    public sealed class AsyncRelayCommand(Func<Task> execute, Func<bool> canExecute = null) : ICommand
    {
        private readonly Func<Task> _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        private readonly Func<bool> _canExecute = canExecute;
        private bool _isExecuting;

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        /// <summary>
        /// True while a prior <see cref="Execute"/> is still awaiting completion.
        /// Bind to this property to show a progress indicator or disable the
        /// triggering control.
        /// </summary>
        public bool IsExecuting
        {
            get => _isExecuting;
            private set
            {
                if (_isExecuting != value)
                {
                    _isExecuting = value;
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public bool CanExecute(object parameter)
        {
            return !_isExecuting && (_canExecute == null || _canExecute());
        }

        public async void Execute(object parameter)
        {
            if (_isExecuting || (_canExecute != null && !_canExecute()))
            {
                return;
            }

            IsExecuting = true;
            try
            {
                await _execute();
            }
            finally
            {
                IsExecuting = false;
            }
        }
    }

    /// <summary>
    /// A generic async-friendly <see cref="ICommand"/> that receives a typed
    /// command parameter. When the parameter cannot be cast to
    /// <typeparamref name="T"/> the command is not executable and execution is
    /// silently skipped.
    /// </summary>
    /// <param name="execute">The async delegate to run.</param>
    /// <param name="canExecute">
    /// Optional guard. The predicate only receives the parameter when it can
    /// be cast to <typeparamref name="T"/>. When <c>null</c>, the command
    /// is executable whenever it is not already executing.
    /// </param>
    public sealed class AsyncRelayCommand<T>(Func<T, Task> execute, Func<T, bool> canExecute = null) : ICommand
    {
        private readonly Func<T, Task> _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        private readonly Func<T, bool> _canExecute = canExecute;
        private bool _isExecuting;

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool IsExecuting
        {
            get => _isExecuting;
            private set
            {
                if (_isExecuting != value)
                {
                    _isExecuting = value;
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public bool CanExecute(object parameter)
        {
            if (_isExecuting)
            {
                return false;
            }

            if (_canExecute == null)
            {
                return true;
            }

            return CommandParameter.TryGet(parameter, out T value) && _canExecute(value);
        }

        public async void Execute(object parameter)
        {
            if (_isExecuting || !CommandParameter.TryGet(parameter, out T value))
            {
                return;
            }

            if (_canExecute != null && !_canExecute(value))
            {
                return;
            }

            IsExecuting = true;
            try
            {
                await _execute(value);
            }
            finally
            {
                IsExecuting = false;
            }
        }
    }
}
