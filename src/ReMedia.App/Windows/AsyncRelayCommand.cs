namespace ReMedia.App.Windows;

using System.Windows.Input;

public sealed class AsyncRelayCommand : ICommand
{
    private readonly Func<CancellationToken, Task> _execute;
    private readonly Func<bool>? _canExecute;
    private CancellationTokenSource? _cts;
    private bool _isExecuting;

    public AsyncRelayCommand(Func<CancellationToken, Task> execute, Func<bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool IsExecuting => _isExecuting;

    public bool CanExecute(object? parameter)
    {
        return !_isExecuting && (_canExecute?.Invoke() ?? true);
    }

    public async void Execute(object? parameter)
    {
        if (_isExecuting)
        {
            return;
        }

        _cts = new CancellationTokenSource();
        _isExecuting = true;
        RaiseCanExecuteChanged();

        try
        {
            await _execute(_cts.Token);
        }
        finally
        {
            _cts.Dispose();
            _cts = null;
            _isExecuting = false;
            RaiseCanExecuteChanged();
        }
    }

    public void Cancel()
    {
        _cts?.Cancel();
    }

    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
