using System.Windows;
using System.Windows.Input;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views;

public static class WindowCommands
{
	public static readonly ICommand EscCommand = new RelayCommand(delegate(object o)
	{
		((Window)o).Close();
	});
}
