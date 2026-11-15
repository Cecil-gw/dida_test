using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ticktick_WPF.Views.CountDown.ViewModels;

public class AsyncRelayCommand : ICommand
{
	private readonly Func<object, Task> _execute;

	private readonly Predicate<object> _canExecute;

	public event EventHandler CanExecuteChanged
	{
		add
		{
			CommandManager.RequerySuggested += value;
		}
		remove
		{
			CommandManager.RequerySuggested -= value;
		}
	}

	public AsyncRelayCommand(Func<object, Task> execute, Predicate<object> canExecute = null)
	{
		_execute = execute ?? throw new ArgumentNullException("execute");
		_canExecute = canExecute;
	}

	public bool CanExecute(object parameter)
	{
		return _canExecute?.Invoke(parameter) ?? true;
	}

	public async void Execute(object parameter)
	{
		await _execute(parameter);
	}
}
