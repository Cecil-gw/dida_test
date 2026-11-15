using System.Windows.Input;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Calendar;

public static class IndependentWindowCommand
{
	public static readonly ICommand SyncCommand = new RelayCommand(KeyBindingCommand.SyncCommand);

	public static readonly ICommand InputCommand = new RelayCommand(delegate(object o)
	{
		KeyBindingCommand.InputCommand(o, null);
	});

	public static readonly ICommand PrintCommand = new RelayCommand(delegate(object o)
	{
		((IndependentWindow)o).Print();
	});

	public static readonly ICommand SetPriorityNoneCommand = new RelayCommand(delegate(object o)
	{
		KeyBindingCommand.SetPriorityCommand(o, 0);
	});

	public static readonly ICommand SetPriorityLowCommand = new RelayCommand(delegate(object o)
	{
		KeyBindingCommand.SetPriorityCommand(o, 1);
	});

	public static readonly ICommand SetPriorityMediumCommand = new RelayCommand(delegate(object o)
	{
		KeyBindingCommand.SetPriorityCommand(o, 3);
	});

	public static readonly ICommand SetPriorityHighCommand = new RelayCommand(delegate(object o)
	{
		KeyBindingCommand.SetPriorityCommand(o, 5);
	});

	public static readonly ICommand SetTodayCommand = new RelayCommand(delegate(object o)
	{
		KeyBindingCommand.SetDateCommand(o, "today");
	});

	public static readonly ICommand SetTomorrowCommand = new RelayCommand(delegate(object o)
	{
		KeyBindingCommand.SetDateCommand(o, "tomorrow");
	});

	public static readonly ICommand SetNextWeekCommand = new RelayCommand(delegate(object o)
	{
		KeyBindingCommand.SetDateCommand(o, "nextweek");
	});

	public static readonly ICommand ClearDateCommand = new RelayCommand(KeyBindingCommand.ClearDateCommand);

	public static readonly ICommand PinTaskCommand = new RelayCommand(KeyBindingCommand.PinTaskCommand);

	public static readonly ICommand DeleteCommand = new RelayCommand(KeyBindingCommand.DeleteCommand);

	public static readonly ICommand OpenStickyCommand = new RelayCommand(KeyBindingCommand.OpenStickyCommand);

	public static readonly ICommand ListViewCommand = new RelayCommand(delegate(object o)
	{
		((IndependentWindow)o).SwitchViewMode("list");
	});

	public static readonly ICommand KanbanViewCommand = new RelayCommand(delegate(object o)
	{
		((IndependentWindow)o).SwitchViewMode("kanban");
	});

	public static readonly ICommand TimelineViewCommand = new RelayCommand(delegate(object o)
	{
		((IndependentWindow)o).SwitchViewMode("timeline");
	});
}
