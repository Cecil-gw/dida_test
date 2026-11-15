using System.Windows.Input;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.QuickAdd;

public static class QuickAddViewCommands
{
	public static readonly ICommand SetPriorityNoneCommand = new RelayCommand(delegate(object o)
	{
		((QuickAddView)o).SetPriority(0);
	});

	public static readonly ICommand SetPriorityLowCommand = new RelayCommand(delegate(object o)
	{
		((QuickAddView)o).SetPriority(1);
	});

	public static readonly ICommand SetPriorityMediumCommand = new RelayCommand(delegate(object o)
	{
		((QuickAddView)o).SetPriority(3);
	});

	public static readonly ICommand SetPriorityHighCommand = new RelayCommand(delegate(object o)
	{
		((QuickAddView)o).SetPriority(5);
	});

	public static readonly ICommand SetTodayCommand = new RelayCommand(delegate(object o)
	{
		((QuickAddView)o).SetDate("today");
	});

	public static readonly ICommand SetTomorrowCommand = new RelayCommand(delegate(object o)
	{
		((QuickAddView)o).SetDate("tomorrow");
	});

	public static readonly ICommand SetNextWeekCommand = new RelayCommand(delegate(object o)
	{
		((QuickAddView)o).SetDate("nextweek");
	});

	public static readonly ICommand SelectDateCommand = new RelayCommand(delegate(object o)
	{
		((QuickAddView)o).SelectDate();
	});

	public static readonly ICommand ClearDateCommand = new RelayCommand(delegate(object o)
	{
		((QuickAddView)o).ClearDate();
	});
}
