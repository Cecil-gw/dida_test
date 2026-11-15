using System.Windows.Input;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Calendar;

public static class CalendarCommands
{
	public static readonly ICommand PrintCommand = new RelayCommand(delegate(object o)
	{
		((CalendarControl)o).Print(isAll: false);
	});

	public static readonly ICommand PrintAllCommand = new RelayCommand(delegate(object o)
	{
		((CalendarControl)o).Print(isAll: true);
	});
}
