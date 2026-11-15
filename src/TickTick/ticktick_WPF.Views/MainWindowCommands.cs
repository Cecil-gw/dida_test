using System.Windows.Input;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views;

public static class MainWindowCommands
{
	public static readonly ICommand SyncCommand = new RelayCommand(delegate
	{
		KeyBindingCommand.SyncCommand();
	});

	public static readonly ICommand InputCommand = new RelayCommand(delegate(object o)
	{
		UserActCollectUtils.AddShortCutEvent("task", "add task");
		((MainWindow)o).InputCommand();
	});

	public static readonly ICommand CloseCommand = new RelayCommand(delegate(object o)
	{
		((MainWindow)o).CloseCommand();
	});

	public static readonly ICommand ExitImmersiveCommand = new RelayCommand(delegate(object o)
	{
		((MainWindow)o).ExitImmersiveCommand();
	});

	public static readonly ICommand JumpTodayList = new RelayCommand(delegate(object o)
	{
		UserActCollectUtils.AddShortCutEvent("navigate", "go_to_tod");
		((MainWindow)o).JumpSmartProject(SmartListType.Today);
	});

	public static readonly ICommand JumpAllList = new RelayCommand(delegate(object o)
	{
		UserActCollectUtils.AddShortCutEvent("navigate", "go_to_all");
		((MainWindow)o).JumpSmartProject(SmartListType.All);
	});

	public static readonly ICommand JumpTomorrowList = new RelayCommand(delegate(object o)
	{
		UserActCollectUtils.AddShortCutEvent("navigate", "go_to_tom");
		((MainWindow)o).JumpSmartProject(SmartListType.Tomorrow);
	});

	public static readonly ICommand JumpWeekList = new RelayCommand(delegate(object o)
	{
		UserActCollectUtils.AddShortCutEvent("navigate", "go_to_7D");
		((MainWindow)o).JumpSmartProject(SmartListType.Week);
	});

	public static readonly ICommand JumpAssignMeList = new RelayCommand(delegate(object o)
	{
		UserActCollectUtils.AddShortCutEvent("navigate", "go_to_assign");
		((MainWindow)o).JumpSmartProject(SmartListType.Assign);
	});

	public static readonly ICommand JumpInboxList = new RelayCommand(delegate(object o)
	{
		UserActCollectUtils.AddShortCutEvent("navigate", "go_to_inbox");
		((MainWindow)o).JumpSmartProject(SmartListType.Inbox);
	});

	public static readonly ICommand JumpCompleteList = new RelayCommand(delegate(object o)
	{
		UserActCollectUtils.AddShortCutEvent("navigate", "go_to_completed");
		((MainWindow)o).JumpSmartProject(SmartListType.Completed);
	});

	public static readonly ICommand JumpAbandonList = new RelayCommand(delegate(object o)
	{
		UserActCollectUtils.AddShortCutEvent("navigate", "go_to_wont_do");
		((MainWindow)o).JumpSmartProject(SmartListType.Abandoned);
	});

	public static readonly ICommand JumpTrashList = new RelayCommand(delegate(object o)
	{
		UserActCollectUtils.AddShortCutEvent("navigate", "go_to_trash");
		((MainWindow)o).JumpSmartProject(SmartListType.Trash);
	});

	public static readonly ICommand JumpSummary = new RelayCommand(delegate(object o)
	{
		UserActCollectUtils.AddShortCutEvent("navigate", "go_to_summary");
		((MainWindow)o).JumpSmartProject(SmartListType.Summary);
	});

	public static readonly ICommand OpenSettingCommand = new RelayCommand(delegate(object o)
	{
		UserActCollectUtils.AddShortCutEvent("navigate", "go_to_settings");
		((MainWindow)o).LeftTabBar.ShowSettingDialog();
	});

	public static readonly ICommand PrintCommand = new RelayCommand(delegate(object o)
	{
		UserActCollectUtils.AddShortCutEvent("general", "print");
		((MainWindow)o).Print();
	});

	public static readonly ICommand PrintDetailCommand = new RelayCommand(delegate(object o)
	{
		UserActCollectUtils.AddShortCutEvent("general", "print_detail");
		((MainWindow)o).Print(isDetail: true);
	});

	public static readonly ICommand SetPriorityNoneCommand = new RelayCommand(delegate(object o)
	{
		UserActCollectUtils.AddShortCutEvent("edit_tasks", "set_no_priority");
		KeyBindingCommand.SetPriorityCommand(o, 0);
	});

	public static readonly ICommand SetPriorityLowCommand = new RelayCommand(delegate(object o)
	{
		UserActCollectUtils.AddShortCutEvent("edit_tasks", "set_low_priority");
		KeyBindingCommand.SetPriorityCommand(o, 1);
	});

	public static readonly ICommand SetPriorityMediumCommand = new RelayCommand(delegate(object o)
	{
		UserActCollectUtils.AddShortCutEvent("edit_tasks", "set_medium_priority");
		KeyBindingCommand.SetPriorityCommand(o, 3);
	});

	public static readonly ICommand SetPriorityHighCommand = new RelayCommand(delegate(object o)
	{
		UserActCollectUtils.AddShortCutEvent("edit_tasks", "set_high_priority");
		KeyBindingCommand.SetPriorityCommand(o, 5);
	});

	public static readonly ICommand SetTodayCommand = new RelayCommand(delegate(object o)
	{
		UserActCollectUtils.AddShortCutEvent("edit_tasks", "set_today");
		KeyBindingCommand.SetDateCommand(o, "today");
	});

	public static readonly ICommand SetTomorrowCommand = new RelayCommand(delegate(object o)
	{
		UserActCollectUtils.AddShortCutEvent("edit_tasks", "set_tomorrow");
		KeyBindingCommand.SetDateCommand(o, "tomorrow");
	});

	public static readonly ICommand SetNextWeekCommand = new RelayCommand(delegate(object o)
	{
		UserActCollectUtils.AddShortCutEvent("edit_tasks", "set_next_week");
		KeyBindingCommand.SetDateCommand(o, "nextweek");
	});

	public static readonly ICommand SelectDateCommand = new RelayCommand(delegate(object o)
	{
		UserActCollectUtils.AddShortCutEvent("edit_tasks", "set_date");
		((MainWindow)o).SelectDate(relative: false);
	});

	public static readonly ICommand ClearDateCommand = new RelayCommand(KeyBindingCommand.ClearDateCommand);

	public static readonly ICommand PinTaskCommand = new RelayCommand(KeyBindingCommand.PinTaskCommand);

	public static readonly ICommand CompleteCommand = new RelayCommand(delegate(object o)
	{
		UserActCollectUtils.AddShortCutEvent("edit_tasks", "complete_tasks");
		((MainWindow)o).ToggleTaskCompleted();
	});

	public static readonly ICommand DeleteCommand = new RelayCommand(KeyBindingCommand.DeleteCommand);

	public static readonly ICommand ExpandAllTaskCommand = new RelayCommand(delegate(object o)
	{
		UserActCollectUtils.AddShortCutEvent("task", "toggle_all_subtasks");
		((MainWindow)o).ExpandOrFoldAllTask(isOpen: true);
	});

	public static readonly ICommand ExpandAllSectionCommand = new RelayCommand(delegate(object o)
	{
		UserActCollectUtils.AddShortCutEvent("task", "toggle_all_groups");
		((MainWindow)o).ExpandOrFoldAllSection();
	});

	public static readonly ICommand SearchCommand = new RelayCommand(delegate(object o)
	{
		UserActCollectUtils.AddShortCutEvent("navigate", "go_to_search");
		((MainWindow)o).SearchCommand();
	});

	public static readonly ICommand SearchOperationCommand = new RelayCommand(delegate(object o)
	{
		((MainWindow)o).SearchOperationCommand();
	});

	public static readonly ICommand JumpCalendar = new RelayCommand(delegate(object o)
	{
		((MainWindow)o).JumpCalendar();
	});

	public static readonly ICommand JumpHabitList = new RelayCommand(delegate(object o)
	{
		((MainWindow)o).JumpHabit();
	});

	public static readonly ICommand ToggleParallelView = new RelayCommand(delegate(object o)
	{
		((MainWindow)o).ToggleParallelView();
	});

	public static readonly ICommand AINewConversation = new RelayCommand(delegate(object o)
	{
		((MainWindow)o).OnAINewConversationShortcut();
	});

	public static readonly ICommand AIToggleFloatingWindow = new RelayCommand(delegate(object o)
	{
		((MainWindow)o).ToggleAIFloatingWindow();
	});

	public static readonly ICommand Tab01Command = new RelayCommand(delegate(object o)
	{
		((MainWindow)o).JumpTab(1);
	});

	public static readonly ICommand Tab02Command = new RelayCommand(delegate(object o)
	{
		((MainWindow)o).JumpTab(2);
	});

	public static readonly ICommand Tab03Command = new RelayCommand(delegate(object o)
	{
		((MainWindow)o).JumpTab(3);
	});

	public static readonly ICommand Tab04Command = new RelayCommand(delegate(object o)
	{
		((MainWindow)o).JumpTab(4);
	});

	public static readonly ICommand Tab05Command = new RelayCommand(delegate(object o)
	{
		((MainWindow)o).JumpTab(5);
	});

	public static readonly ICommand Tab06Command = new RelayCommand(delegate(object o)
	{
		((MainWindow)o).JumpTab(6);
	});

	public static readonly ICommand Tab07Command = new RelayCommand(delegate(object o)
	{
		((MainWindow)o).JumpTab(7);
	});

	public static readonly ICommand Tab08Command = new RelayCommand(delegate(object o)
	{
		((MainWindow)o).JumpTab(8);
	});

	public static readonly ICommand Tab09Command = new RelayCommand(delegate(object o)
	{
		((MainWindow)o).JumpTab(9);
	});

	public static readonly ICommand OpenStickyCommand = new RelayCommand(KeyBindingCommand.OpenStickyCommand);

	public static readonly ICommand TabCommand = new RelayCommand(delegate(object o)
	{
		((MainWindow)o).OnTabKeyUp();
	});

	public static readonly ICommand ListViewCommand = new RelayCommand(delegate(object o)
	{
		UserActCollectUtils.AddShortCutEvent("switch_views", "list_view");
		((MainWindow)o).SwitchViewMode("list");
	});

	public static readonly ICommand KanbanViewCommand = new RelayCommand(delegate(object o)
	{
		UserActCollectUtils.AddShortCutEvent("switch_views", "kanban_view");
		((MainWindow)o).SwitchViewMode("kanban");
	});

	public static readonly ICommand TimelineViewCommand = new RelayCommand(delegate(object o)
	{
		UserActCollectUtils.AddShortCutEvent("switch_views", "timeline_view");
		((MainWindow)o).SwitchViewMode("timeline");
	});
}
