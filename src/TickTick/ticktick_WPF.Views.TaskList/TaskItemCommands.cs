using System.Windows.Input;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.TaskList;

public static class TaskItemCommands
{
	public static readonly ICommand SetPriorityNoneCommand = new RelayCommand(delegate(object o)
	{
		((ITaskOperation)o).SetPriority(0);
	});

	public static readonly ICommand SetPriorityLowCommand = new RelayCommand(delegate(object o)
	{
		((ITaskOperation)o).SetPriority(1);
	});

	public static readonly ICommand SetPriorityMediumCommand = new RelayCommand(delegate(object o)
	{
		((ITaskOperation)o).SetPriority(3);
	});

	public static readonly ICommand SetPriorityHighCommand = new RelayCommand(delegate(object o)
	{
		((ITaskOperation)o).SetPriority(5);
	});

	public static readonly ICommand SetTodayCommand = new RelayCommand(delegate(object o)
	{
		((ITaskOperation)o).SetDate("today");
	});

	public static readonly ICommand SetTomorrowCommand = new RelayCommand(delegate(object o)
	{
		((ITaskOperation)o).SetDate("tomorrow");
	});

	public static readonly ICommand SetNextWeekCommand = new RelayCommand(delegate(object o)
	{
		((ITaskOperation)o).SetDate("nextweek");
	});

	public static readonly ICommand SelectDateCommand = new RelayCommand(delegate(object o)
	{
		((ITaskOperation)o).SelectDate(relative: false);
	});

	public static readonly ICommand ClearDateCommand = new RelayCommand(delegate(object o)
	{
		((ITaskOperation)o).ClearDate();
	});

	public static readonly ICommand CompleteCommand = new RelayCommand(delegate(object o)
	{
		((ITaskOperation)o).ToggleTaskCompleted();
	});
}
