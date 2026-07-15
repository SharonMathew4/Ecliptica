using System;
using System.Windows.Input;

namespace Ecliptica.Engine.Commands;

public class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) => _canExecute == null || _canExecute();

    public void Execute(object? parameter) => _execute();
}

public class RelayCommand<T> : ICommand
{
    private readonly Action<T?> _execute;
    private readonly Func<T?, bool>? _canExecute;

    public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter)
    {
        if (_canExecute == null) return true;
        
        T? typedParameter = default;
        if (parameter != null)
        {
            try
            {
                if (parameter is T val)
                {
                    typedParameter = val;
                }
                else
                {
                    typedParameter = (T?)Convert.ChangeType(parameter, Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T));
                }
            }
            catch
            {
                if (typeof(T).IsValueType && Nullable.GetUnderlyingType(typeof(T)) == null)
                    return false;
            }
        }
        
        return _canExecute(typedParameter);
    }

    public void Execute(object? parameter)
    {
        T? typedParameter = default;
        if (parameter != null)
        {
            try
            {
                if (parameter is T val)
                {
                    typedParameter = val;
                }
                else
                {
                    typedParameter = (T?)Convert.ChangeType(parameter, Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T));
                }
            }
            catch
            {
                // Safe fallback to default
            }
        }
        _execute(typedParameter);
    }
}
