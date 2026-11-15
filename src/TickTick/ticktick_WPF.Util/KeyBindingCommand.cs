using System;
using System.Windows;
using System.Windows.Input;
using ICSharpCode.AvalonEdit.Editing;
using ticktick_WPF.Models;
using ticktick_WPF.Util.Provider;
using ticktick_WPF.Views;
using ticktick_WPF.Views.QuickAdd;
using ticktick_WPF.Views.TaskList;

namespace ticktick_WPF.Util;

public static class KeyBindingCommand
{
	public static void SyncCommand(object sender = null)
	{
		SyncManager.Sync();
	}

	public static void InputCommand(object sender, ProjectIdentity identity)
	{
		if (identity != null)
		{
			ProjectIdentity projectIdentity = identity;
			if (projectIdentity is NormalProjectIdentity normalProjectIdentity)
			{
				ProjectModel project = normalProjectIdentity.Project;
				if (project == null || !project.IsEnable())
				{
					goto IL_0033;
				}
			}
			if (projectIdentity is SubscribeCalendarProjectIdentity || projectIdentity is BindAccountCalendarProjectIdentity)
			{
				goto IL_0033;
			}
			identity = projectIdentity.Copy(projectIdentity);
		}
		else
		{
			identity = ProjectIdentity.GetDefaultProject();
		}
		goto IL_004e;
		IL_004e:
		AddTaskWindow.ShowWindow(sender as Window, identity);
		return;
		IL_0033:
		identity = ProjectIdentity.GetDefaultProject();
		goto IL_004e;
	}

	private static ITaskOperation GetFocusedTaskItem(object sender)
	{
		if (sender is DependencyObject element)
		{
			IInputElement focusedElement = FocusManager.GetFocusedElement(element);
			if (focusedElement is ITaskOperation result)
			{
				return result;
			}
			if (focusedElement is TextArea child)
			{
				ITaskOperation taskOperation = Utils.FindParent<ITaskOperation>(child);
				if (taskOperation != null)
				{
					return taskOperation;
				}
			}
		}
		return null;
	}

	public static void SetPriorityCommand(object sender, int priority)
	{
		ITaskOperation focusedTaskItem = GetFocusedTaskItem(sender);
		if (focusedTaskItem != null)
		{
			focusedTaskItem.SetPriority(priority);
		}
		else
		{
			(sender as IKeyBinding)?.BatchSetPriorityCommand(priority);
		}
	}

	public static void SetDateCommand(object sender, string key)
	{
		UserActCollectUtils.AddShortCutEvent("edit_tasks", "set_date");
		ITaskOperation focusedTaskItem = GetFocusedTaskItem(sender);
		if (focusedTaskItem != null)
		{
			focusedTaskItem.SetDate(key);
			return;
		}
		DateTime value = DateTime.Today;
		switch (key)
		{
		case "today":
			value = DateTime.Today;
			break;
		case "tomorrow":
			value = DateTime.Today.AddDays(1.0);
			break;
		case "nextweek":
			value = DateTime.Today.AddDays(7.0);
			break;
		}
		(sender as IKeyBinding)?.BatchSetDateCommand(value);
	}

	public static void ClearDateCommand(object sender)
	{
		UserActCollectUtils.AddShortCutEvent("edit_tasks", "clear_time");
		ITaskOperation focusedTaskItem = GetFocusedTaskItem(sender);
		if (focusedTaskItem != null)
		{
			focusedTaskItem.ClearDate();
		}
		else
		{
			(sender as IKeyBinding)?.BatchSetDateCommand(null);
		}
	}

	public static void PinTaskCommand(object sender)
	{
		UserActCollectUtils.AddShortCutEvent("edit_tasks", "pin_tasks");
		ITaskOperation focusedTaskItem = GetFocusedTaskItem(sender);
		if (focusedTaskItem != null)
		{
			focusedTaskItem.PinOrUnpinTask();
		}
		else
		{
			(sender as IKeyBinding)?.BatchPinTaskCommand();
		}
	}

	public static void OpenStickyCommand(object sender)
	{
		UserActCollectUtils.AddShortCutEvent("edit_tasks", "open_as_sticky_note");
		ITaskOperation focusedTaskItem = GetFocusedTaskItem(sender);
		if (focusedTaskItem != null)
		{
			focusedTaskItem.OpenSticky();
		}
		else
		{
			(sender as IKeyBinding)?.BatchOpenStickyCommand();
		}
	}

	public static void DeleteCommand(object sender)
	{
		UserActCollectUtils.AddShortCutEvent("edit_tasks", "delete_tasks");
		ITaskOperation focusedTaskItem = GetFocusedTaskItem(sender);
		if (focusedTaskItem != null)
		{
			focusedTaskItem.Delete();
		}
		else
		{
			(sender as IKeyBinding)?.BatchDeleteCommand();
		}
	}
}
