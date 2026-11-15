using System.Windows.Input;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.MainListView.DetailView;

public static class DetailControlCommands
{
	public static readonly ICommand SetPriorityNoneCommand = new RelayCommand(delegate(object o)
	{
		UserActCollectUtils.AddShortCutEvent("edit_tasks", "set_no_priority");
		((TaskDetailView)o).SetPriority(0);
	});

	public static readonly ICommand SetPriorityLowCommand = new RelayCommand(delegate(object o)
	{
		UserActCollectUtils.AddShortCutEvent("edit_tasks", "set_low_priority");
		((TaskDetailView)o).SetPriority(1);
	});

	public static readonly ICommand SetPriorityMediumCommand = new RelayCommand(delegate(object o)
	{
		UserActCollectUtils.AddShortCutEvent("edit_tasks", "set_medium_priority");
		((TaskDetailView)o).SetPriority(3);
	});

	public static readonly ICommand SetPriorityHighCommand = new RelayCommand(delegate(object o)
	{
		UserActCollectUtils.AddShortCutEvent("edit_tasks", "set_high_priority");
		((TaskDetailView)o).SetPriority(5);
	});

	public static readonly ICommand SetTodayCommand = new RelayCommand(delegate(object o)
	{
		((TaskDetailView)o).SetDate("today");
	});

	public static readonly ICommand SetTomorrowCommand = new RelayCommand(delegate(object o)
	{
		((TaskDetailView)o).SetDate("tomorrow");
	});

	public static readonly ICommand SetNextWeekCommand = new RelayCommand(delegate(object o)
	{
		((TaskDetailView)o).SetDate("nextweek");
	});

	public static readonly ICommand SelectDateCommand = new RelayCommand(delegate(object o)
	{
		((TaskDetailView)o).SelectDate();
	});

	public static readonly ICommand ClearDateCommand = new RelayCommand(delegate(object o)
	{
		UserActCollectUtils.AddShortCutEvent("edit_tasks", "clear_time");
		((TaskDetailView)o).ClearDate();
	});

	public static readonly ICommand CompleteCommand = new RelayCommand(delegate(object o)
	{
		((TaskDetailView)o).ToggleTaskCompleted();
	});

	public static readonly ICommand PrintCommand = new RelayCommand(delegate(object o)
	{
		UserActCollectUtils.AddShortCutEvent("general", "print");
		((TaskDetailView)o).OnPrint();
	});

	public static readonly ICommand EscCommand = new RelayCommand(delegate(object o)
	{
		((TaskDetailView)o).OnEsc();
	});

	public static readonly ICommand PinCommand = new RelayCommand(delegate(object o)
	{
		UserActCollectUtils.AddShortCutEvent("edit_tasks", "pin_tasks");
		((TaskDetailView)o).OnPin();
	});

	public static readonly ICommand OpenAsSticky = new RelayCommand(delegate(object o)
	{
		((TaskDetailView)o).OpenAsSticky();
	});
}
