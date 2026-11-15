using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using ticktick_WPF.Cache;
using ticktick_WPF.Models;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Activity;

public class TaskActivityViewModel : BaseViewModel
{
	private bool _fold = true;

	public string Title { get; set; }

	public string Description { get; set; }

	public string User { get; set; }

	public string When { get; set; }

	public SolidColorBrush Color { get; set; }

	public Geometry Icon { get; set; }

	public int MaxDescLength { get; set; }

	public bool Fold
	{
		get
		{
			return _fold;
		}
		set
		{
			_fold = value;
			OnPropertyChanged("Fold");
		}
	}

	public TaskActivityViewModel(TaskModifyModel model, string userName, bool isAgendaCopy, bool taskFloating, string kind)
	{
		if (isAgendaCopy && model.action != "T_CREATE")
		{
			userName = Utils.GetString("Owner");
		}
		Title = GetTitle(model, userName, isAgendaCopy, model.kind ?? kind);
		Description = GetDescription(model, taskFloating, kind);
		User = userName;
		Color = GetColor(model.action);
		Icon = GetIcon(model.action, model.kind);
		if (DateTime.TryParse(model.when, out var result))
		{
			When = DateUtils.FormatCommentTime(result);
		}
		if (model.whoProfile != null && !model.whoProfile.isMyself)
		{
			User = (string.IsNullOrEmpty(model.whoProfile.name) ? model.whoProfile.username : model.whoProfile.name);
		}
	}

	private static Geometry GetIcon(string action, string kind)
	{
		switch (action)
		{
		case "T_CREATE":
			return Utils.GetGeometry("Ic.ProjectList.Add");
		case "T_DELETE":
		case "T_DEL_FILE":
			return Utils.GetGeometry("Ic.ProjectList.Delete");
		case "T_REPEAT":
		case "T_DONE":
			return Utils.GetGeometry("Ic.Common.CheckedInside");
		case "T_UNDONE":
		case "T_REOPEN":
		case "T_TRASH_BACK":
			return Utils.GetGeometry("Ic.Habit.Recover");
		case "T_ASSIGN":
			return Utils.GetGeometry("Ic.TaskOM.AssignToOther");
		case "T_ADD_FILE":
			return Utils.GetIcon("IcActionAddFile");
		case "C_REMINDER":
			return Utils.GetIcon("IcActionSubTime");
		case "T_KIND":
			if (!(kind == "TEXT"))
			{
				return Utils.GetIcon("IcActionList");
			}
			return Utils.GetIcon("IcActionText");
		case "C_CONTENT":
			return Utils.GetIcon("IcActionSubtaskEdit");
		case "C_CREATE":
			return Utils.GetIcon("IcActionSubtaskCreate");
		case "C_DELETE":
			return Utils.GetIcon("IcActionSubtaskDel");
		case "C_DONE":
			return Utils.GetIcon("IcActionSubtaskDone");
		case "C_UNDONE":
			return Utils.GetIcon("IcActionSubtaskUndone");
		case "T_CANCEL":
		case "T_REPEAT_CANCEL":
			return Utils.GetGeometry("Ic.Common.Close");
		case "T_MOVE":
			return Utils.GetIcon("IcActionMoveIn");
		default:
			return Utils.GetGeometry("Ic.ProjectList.Edit");
		}
	}

	private static SolidColorBrush GetColor(string action)
	{
		switch (action)
		{
		case "T_CREATE":
		case "C_CREATE":
		case "T_KIND":
		case "T_MOVE":
		case "T_ADD_FILE":
			return ThemeUtil.GetColor("CreateTaskColor");
		case "T_REPEAT":
		case "T_DONE":
		case "C_DONE":
			return ThemeUtil.GetColor("CompleteTaskColor");
		case "T_UNDONE":
		case "C_UNDONE":
		case "T_REOPEN":
		case "T_TRASH_BACK":
			return ThemeUtil.GetColor("UndoTaskColor");
		case "T_DELETE":
		case "C_DELETE":
		case "T_DEL_FILE":
			return ThemeUtil.GetColor("TextRedColor");
		case "T_CANCEL":
		case "T_REPEAT_CANCEL":
			return ThemeUtil.GetColor("AbandonedColor");
		default:
			return ThemeUtil.GetColor("EditTaskColor");
		}
	}

	private static string GetDescription(TaskModifyModel model, bool taskFloating, string kind)
	{
		switch (model.action)
		{
		case "T_CREATE":
			return model.title + (string.IsNullOrEmpty(model.content) ? "" : ("\r\n" + model.content));
		case "T_REPEAT":
		case "T_REPEAT_CANCEL":
		{
			DateTime.TryParse(model.startDate, out var result8);
			DateTime.TryParse(model.startDateBefore, out var result9);
			DateTime.TryParse((model.dueDate != model.startDate) ? model.dueDate : string.Empty, out var result10);
			DateTime.TryParse((model.dueDateBefore != model.startDateBefore) ? model.dueDateBefore : string.Empty, out var result11);
			return GetRepeatDescription(result9, result8, result11, result10, model);
		}
		case "T_DUE":
		{
			DateTime.TryParse(model.startDate, out var result4);
			DateTime.TryParse(model.startDateBefore, out var result5);
			DateTime.TryParse((model.dueDate != model.startDate) ? model.dueDate : string.Empty, out var result6);
			DateTime.TryParse((model.dueDateBefore != model.startDateBefore) ? model.dueDateBefore : string.Empty, out var result7);
			return GetTimeDescription(result5, result4, result7, result6, model);
		}
		case "T_TITLE":
			return model.title;
		case "T_CONTENT":
		{
			string text4 = model.content;
			if (!string.IsNullOrEmpty(model.desc))
			{
				text4 += (string.IsNullOrEmpty(text4) ? model.desc : (Environment.NewLine + model.desc));
			}
			return text4;
		}
		case "T_ADD_FILE":
		case "T_DEL_FILE":
			if (model.attachments != null && model.attachments.Any())
			{
				return model.attachments[0].fileName;
			}
			return string.Empty;
		case "T_PARENT":
			return Utils.GetString("SetParentActivity") + ":" + model.parent;
		case "C_UNDONE":
		case "C_CREATE":
		case "C_DELETE":
		case "C_DONE":
			return model.description;
		case "C_REMINDER":
		{
			DateTime.TryParse(model.itemReminderDate, out var result);
			DateTime.TryParse(model.itemReminderDateBefore, out var result2);
			string text = (Utils.IsEmptyDate(result2) ? Utils.GetString("NoReminder") : DateUtils.GetTaskActivityTimeString(result2, null, model.isAllDayBefore, model.itemTimeZone, taskFloating));
			string text2 = (Utils.IsEmptyDate(result) ? Utils.GetString("NoReminder") : DateUtils.GetTaskActivityTimeString(result, null, model.isAllDay, model.itemTimeZone, taskFloating));
			if (text != text2)
			{
				return string.Concat(text + " → " + text2, "\r\n", model.description);
			}
			return string.Empty;
		}
		case "C_CONTENT":
		{
			string text3 = (string.IsNullOrEmpty(model.desc) ? "" : (model.desc + "\r\n"));
			List<string> itemTitles = model.itemTitles;
			if (itemTitles != null && itemTitles.Count > 0)
			{
				for (int i = 0; i < model.itemTitles.Count; i++)
				{
					text3 = text3 + "-" + model.itemTitles[i] + ((i == model.itemTitles.Count - 1) ? "" : "\r\n");
				}
			}
			return text3;
		}
		case "T_COMPLETED_TIME":
		{
			DateTime.TryParse(model.completedTime, out var result3);
			string arg3 = DateUtils.FormatShortDate(result3);
			return string.Format(Utils.GetString("ActionChangeCpDateDesc"), arg3);
		}
		case "T_MOVE":
		{
			string format = Utils.GetString((kind != "NOTE") ? "ActionMoveTaskMessage" : "ActionMoveNoteMessage");
			string arg = CacheManager.GetProjectById(model.fromProjectId)?.name ?? Utils.GetString("UnknownProject");
			string arg2 = CacheManager.GetProjectById(model.toProjectId)?.name ?? Utils.GetString("UnknownProject");
			return string.Format(format, arg, arg2);
		}
		default:
			return string.Empty;
		}
	}

	private static string GetRepeatDescription(DateTime startBefore, DateTime start, DateTime dueBefore, DateTime due, TaskModifyModel model)
	{
		string taskActivityTimeString = DateUtils.GetTaskActivityTimeString(startBefore, dueBefore, model.isAllDayBefore, model.timeZoneBefore, model.isFloatingBefore);
		string taskActivityTimeString2 = DateUtils.GetTaskActivityTimeString(start, due, model.isAllDay, model.timeZone, model.isFloating);
		return string.Format(Utils.GetString("RepeatCycleTo"), taskActivityTimeString, taskActivityTimeString2);
	}

	private static string GetTimeDescription(DateTime startBefore, DateTime start, DateTime dueBefore, DateTime due, TaskModifyModel model)
	{
		if (Utils.IsEmptyDate(start))
		{
			return Utils.GetString("ActionClearDate");
		}
		if (Utils.IsEmptyDate(startBefore))
		{
			string taskActivityTimeString = DateUtils.GetTaskActivityTimeString(start, due, model.isAllDay, model.timeZone, model.isFloating);
			return string.Format(Utils.GetString("ActionModifyDate"), string.Empty, taskActivityTimeString);
		}
		string taskActivityTimeString2 = DateUtils.GetTaskActivityTimeString(startBefore, dueBefore, model.isAllDayBefore, model.timeZoneBefore, model.isFloatingBefore);
		string taskActivityTimeString3 = DateUtils.GetTaskActivityTimeString(start, due, model.isAllDay, model.timeZone, model.isFloating);
		return string.Format(Utils.GetString("ActionModifyDate"), taskActivityTimeString2, taskActivityTimeString3);
	}

	private static string GetTitle(TaskModifyModel model, string username, bool isAgendaCopy, string kind)
	{
		switch (model.action)
		{
		case "T_CREATE":
			return Utils.GetString(isAgendaCopy ? "ActionSaveAgenda" : ((kind == "NOTE") ? "ActionCreateNote" : "ActionCreateTask"));
		case "T_DELETE":
			return Utils.GetString((kind == "NOTE") ? "ActionDeleteNote" : "ActionDeleteTask");
		case "T_DONE":
			return Utils.GetString("ActionCompleteTask");
		case "T_UNDONE":
			return Utils.GetString("ActionUnCompleteTask");
		case "T_COMPLETED_TIME":
			return Utils.GetString("ActionChangeCpDate");
		case "T_ASSIGN":
			if (model.assigneeProfile != null)
			{
				return string.Format(arg0: (!model.assigneeProfile.isMyself) ? (string.IsNullOrEmpty(model.assigneeProfile.name) ? model.assigneeProfile.username : model.assigneeProfile.name) : username, format: Utils.GetString((kind == "NOTE") ? "ActionAssignNote" : "ActionAssignTask"));
			}
			return Utils.GetString("ActionCancelAssign");
		case "T_REPEAT":
			return Utils.GetString("ActionRepeat");
		case "T_DUE":
			return Utils.GetString("ActionSetDate");
		case "T_TITLE":
			return Utils.GetString("ActionModifyTitle");
		case "T_CONTENT":
			return Utils.GetString("ActionModifyContent");
		case "T_TRASH_BACK":
			return Utils.GetString((kind == "NOTE") ? "ActionRestoreNote" : "ActionRestoreTask");
		case "T_PROGRESS":
		{
			string arg = model.progress + "%";
			return string.Format(Utils.GetString("ActionSetProgress"), arg);
		}
		case "T_ADD_FILE":
			return Utils.GetString("ActionAddFile");
		case "T_DEL_FILE":
			return Utils.GetString("ActionDeleteFile");
		case "T_PARENT":
			return Utils.GetString("SetParentActivity");
		case "T_DELETE_PARENT":
			return Utils.GetString("DelParentActivity");
		case "T_KIND":
			if (!(model.kind == "TEXT"))
			{
				return Utils.GetString("ActionSwitchToChecklist");
			}
			return Utils.GetString("ActionSwitchToText");
		case "C_CREATE":
			return Utils.GetString("ActionCreateItem");
		case "C_DELETE":
			return Utils.GetString("ActionDeleteItem");
		case "C_DONE":
			return Utils.GetString("ActionDoneItem");
		case "C_UNDONE":
			return Utils.GetString("ActionUndoneItem");
		case "C_REMINDER":
			return Utils.GetString("ActionModifyItemDue");
		case "C_CONTENT":
			return Utils.GetString("ActionModifyDesc");
		case "T_AS_TASK":
			return Utils.GetString("ActionConvertToTask");
		case "T_AS_NOTE":
			return Utils.GetString("ActionConvertToNote");
		case "T_CANCEL":
			return Utils.GetString("ActionAbandonedTask");
		case "T_REOPEN":
			return Utils.GetString("ActionReopenTask");
		case "T_REPEAT_CANCEL":
			return Utils.GetString("ActionRepeatAbandoned");
		case "T_MOVE":
			return Utils.GetString((kind != "NOTE") ? "ActionMoveTask" : "ActionMoveNote");
		default:
			return string.Empty;
		}
	}
}
