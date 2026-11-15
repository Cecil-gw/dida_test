using System.Windows.Input;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Detail;

public static class DetailPopupCommands
{
	public static readonly ICommand OpenAsSticky = new RelayCommand(delegate(object o)
	{
		((TaskDetailPopup)o).OpenAsSticky();
	});

	public static readonly ICommand ToggleCompleted = new RelayCommand(delegate(object o)
	{
		((TaskDetailPopup)o).ToggleCompleted();
	});

	public static readonly ICommand PinTask = new RelayCommand(delegate(object o)
	{
		((TaskDetailPopup)o).PinTask();
	});

	public static readonly ICommand DeleteTask = new RelayCommand(delegate(object o)
	{
		((TaskDetailPopup)o).DeleteTask();
	});

	public static readonly ICommand SetPriorityNoneCommand = new RelayCommand(delegate(object o)
	{
		UserActCollectUtils.AddShortCutEvent("edit_tasks", "set_no_priority");
		((TaskDetailPopup)o).SetPriority(0);
	});

	public static readonly ICommand SetPriorityLowCommand = new RelayCommand(delegate(object o)
	{
		UserActCollectUtils.AddShortCutEvent("edit_tasks", "set_low_priority");
		((TaskDetailPopup)o).SetPriority(1);
	});

	public static readonly ICommand SetPriorityMediumCommand = new RelayCommand(delegate(object o)
	{
		UserActCollectUtils.AddShortCutEvent("edit_tasks", "set_medium_priority");
		((TaskDetailPopup)o).SetPriority(3);
	});

	public static readonly ICommand SetPriorityHighCommand = new RelayCommand(delegate(object o)
	{
		UserActCollectUtils.AddShortCutEvent("edit_tasks", "set_high_priority");
		((TaskDetailPopup)o).SetPriority(5);
	});

	public static readonly ICommand SetTodayCommand = new RelayCommand(delegate(object o)
	{
		((TaskDetailPopup)o).SetDate("today");
	});

	public static readonly ICommand SetTomorrowCommand = new RelayCommand(delegate(object o)
	{
		((TaskDetailPopup)o).SetDate("tomorrow");
	});

	public static readonly ICommand SetNextWeekCommand = new RelayCommand(delegate(object o)
	{
		((TaskDetailPopup)o).SetDate("nextweek");
	});

	public static readonly ICommand SelectDateCommand = new RelayCommand(delegate(object o)
	{
		((TaskDetailPopup)o).SelectDate();
	});

	public static readonly ICommand ClearDateCommand = new RelayCommand(delegate(object o)
	{
		UserActCollectUtils.AddShortCutEvent("edit_tasks", "clear_time");
		((TaskDetailPopup)o).ClearDate();
	});
}
