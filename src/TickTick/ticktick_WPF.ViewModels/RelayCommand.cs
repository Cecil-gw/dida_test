using System;
using System.Windows.Input;

namespace ticktick_WPF.ViewModels;

public class RelayCommand : ICommand
{
	private Action<object> _action;

	public event EventHandler CanExecuteChanged;

	public RelayCommand(Action<object> action)
	{
		_action = action;
	}

	public virtual bool CanExecute(object parameter)
	{
		return true;
	}

	public virtual void Execute(object parameter)
	{
		_action?.Invoke(parameter);
	}
}
