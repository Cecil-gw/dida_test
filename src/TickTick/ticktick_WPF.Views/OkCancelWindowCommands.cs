using System.Windows.Input;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views;

public static class OkCancelWindowCommands
{
	public static readonly ICommand EscCommand = new RelayCommand(delegate(object o)
	{
		((IOkCancelWindow)o).OnCancel();
	});

	public static readonly ICommand OkCommand = new RelayCommand(delegate(object o)
	{
		((IOkCancelWindow)o).Ok();
	});
}
