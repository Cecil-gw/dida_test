using System.Windows.Input;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Detail;

public static class TaskStickyCommands
{
	public static readonly ICommand CreatNewCommand = new RelayCommand(delegate(object o)
	{
		UserActCollectUtils.AddShortCutEvent("task", "add task");
		((TaskStickyWindow)o).CreateNew();
	});

	public static readonly ICommand SetColorCommand = new RelayCommand(delegate(object o)
	{
		UserActCollectUtils.AddShortCutEvent("edit_sticky_note", "color_sticky");
		((TaskStickyWindow)o).QuickChangeColor();
	});

	public static readonly ICommand CloseCommand = new RelayCommand(delegate(object o)
	{
		((TaskStickyWindow)o).TryClose();
	});

	public static readonly ICommand PinCommand = new RelayCommand(delegate(object o)
	{
		UserActCollectUtils.AddShortCutEvent("edit_sticky_note", "pin_sticky");
		((TaskStickyWindow)o).Pin();
	});

	public static readonly ICommand CompleteCommand = new RelayCommand(delegate(object o)
	{
		((TaskStickyWindow)o).CompleteTaskCommand();
	});

	public static ICommand StickyCollapseCommand = new RelayCommand(delegate(object o)
	{
		UserActCollectUtils.AddShortCutEvent("edit_sticky_note", "toggle_all_sticky");
		((TaskStickyWindow)o).CollapseOrExpandAll();
	});

	public static ICommand StickyAlignTopCommand = new RelayCommand(delegate(object o)
	{
		UserActCollectUtils.AddShortCutEvent("edit_sticky_note", "arrange_by_top");
		((TaskStickyWindow)o).Align(isVertical: false, isRight: false);
	});

	public static ICommand StickyAlignLeftCommand = new RelayCommand(delegate(object o)
	{
		UserActCollectUtils.AddShortCutEvent("edit_sticky_note", "arrange_by_left");
		((TaskStickyWindow)o).Align(isVertical: true, isRight: false);
	});

	public static ICommand StickyAlignRightCommand = new RelayCommand(delegate(object o)
	{
		UserActCollectUtils.AddShortCutEvent("edit_sticky_note", "arrange_by_right");
		((TaskStickyWindow)o).Align(isVertical: true, isRight: true);
	});
}
