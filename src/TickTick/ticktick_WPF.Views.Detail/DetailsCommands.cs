using System.Windows.Input;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Detail;

public static class DetailsCommands
{
	public static readonly ICommand EscCommand = new RelayCommand(delegate(object o)
	{
		((TaskDetailWindows)o).TryClose();
	});

	public static readonly ICommand EnterCommand = new RelayCommand(delegate(object o)
	{
		((TaskDetailWindows)o).EnterCommand();
	});

	public static readonly ICommand CompleteTaskCommand = new RelayCommand(delegate(object o)
	{
		((TaskDetailWindows)o).CompletedTaskCommand();
	});
}
