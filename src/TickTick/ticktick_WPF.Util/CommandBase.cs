using System;
using System.Windows.Input;

namespace ticktick_WPF.Util;

public class CommandBase : ICommand
{
	private readonly Action<object> _commandWithParam;

	private readonly Action _command;

	private readonly Func<bool> _canExecute;

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

	public CommandBase(Action command, Func<bool> canExecute = null)
	{
		_canExecute = canExecute;
		_command = command ?? throw new ArgumentNullException();
	}

	public CommandBase(Action<object> commandWidthParam, Func<bool> canExecute = null)
	{
		_canExecute = canExecute;
		_commandWithParam = commandWidthParam ?? throw new ArgumentNullException();
	}

	public bool CanExecute(object parameter)
	{
		if (_canExecute != null)
		{
			return _canExecute();
		}
		return true;
	}

	public void Execute(object parameter)
	{
		if (parameter != null)
		{
			_commandWithParam(parameter);
		}
		else if (_command != null)
		{
			_command();
		}
		else
		{
			_commandWithParam?.Invoke(null);
		}
	}
}
