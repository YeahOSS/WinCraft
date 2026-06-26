using System;
using System.Windows.Input;

namespace WinCraft.UI.Mvvm
{
    /// <summary>
    /// An <see cref="ICommand"/> that executes an <see cref="Action"/>.
    /// Relays <see cref="CanExecuteChanged"/> through
    /// <see cref="CommandManager.RequerySuggested"/> for automatic re-evaluation.
    /// </summary>
    public sealed class RelayCommand(Action action) : ICommand
    {
        private Func<bool> _canExecuteFunc;

        public Action ExecuteAction { get; set; } = action ?? throw new ArgumentNullException(nameof(action));

        public Func<bool> CanExecuteFunc
        {
            get => _canExecuteFunc;
            set
            {
                _canExecuteFunc = value;
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecuteFunc == null || _canExecuteFunc();
        }

        public void Execute(object parameter)
        {
            ExecuteAction?.Invoke();
        }
    }

    /// <summary>
    /// A generic <see cref="ICommand"/> that receives a typed command parameter.
    /// When the parameter cannot be cast to <typeparamref name="T"/> the command
    /// is not executable and execution is silently skipped.
    /// </summary>
    public sealed class RelayCommand<T>(Action<T> action) : ICommand
    {
        private Func<T, bool> _canExecuteFunc;

        public Action<T> ExecuteAction { get; } = action;

        public Func<T, bool> CanExecuteFunc
        {
            get => _canExecuteFunc;
            set
            {
                _canExecuteFunc = value;
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object parameter)
        {
            if (_canExecuteFunc == null)
            {
                return true;
            }

            return CommandParameter.TryGet(parameter, out T value) && _canExecuteFunc(value);
        }

        public void Execute(object parameter)
        {
            if (CommandParameter.TryGet(parameter, out T value))
            {
                ExecuteAction?.Invoke(value);
            }
        }
    }
}
