using System.Windows.Input;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.CheckList;

public static class CheckItemCommands
{
	public static readonly ICommand SetTodayCommand = new RelayCommand(delegate(object o)
	{
		((ChecklistItemControl)o).SetDate("today");
	});

	public static readonly ICommand SetTomorrowCommand = new RelayCommand(delegate(object o)
	{
		((ChecklistItemControl)o).SetDate("tomorrow");
	});

	public static readonly ICommand SetNextWeekCommand = new RelayCommand(delegate(object o)
	{
		((ChecklistItemControl)o).SetDate("nextweek");
	});

	public static readonly ICommand SelectDateCommand = new RelayCommand(delegate(object o)
	{
		((ChecklistItemControl)o).SelectDate();
	});

	public static readonly ICommand ClearDateCommand = new RelayCommand(delegate(object o)
	{
		((ChecklistItemControl)o).ClearDate();
	});

	public static readonly ICommand CompleteCommand = new RelayCommand(delegate(object o)
	{
		((ChecklistItemControl)o).ToggleTaskCompleted();
	});
}
