using System.Windows.Input;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Detail;

public static class DetailWindowCommands
{
	public static readonly ICommand OpenAsSticky = new RelayCommand(delegate(object o)
	{
		((TaskDetailWindow)o).OpenAsSticky();
	});
}
