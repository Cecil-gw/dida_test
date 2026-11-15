using System.Windows.Input;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.MarkDown;

public static class InsertUrlCommands
{
	public static readonly ICommand ExitCommand = new RelayCommand(delegate(object window)
	{
		((InsertLinkWindow)window).Close();
	});
}
