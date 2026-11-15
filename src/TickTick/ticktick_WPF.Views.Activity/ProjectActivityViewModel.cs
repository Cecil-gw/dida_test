using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Activity;

public class ProjectActivityViewModel
{
	public string Title { get; set; }

	public string User { get; set; }

	public string When { get; set; }

	public SolidColorBrush Color { get; set; }

	public Geometry Icon { get; set; }

	public string TaskId { get; set; }

	public string TaskTitle { get; set; }

	public bool IsSection { get; set; }

	public DateTime Date { get; set; }

	public ProjectActivityViewModel(string date)
	{
		IsSection = true;
		Title = date;
	}

	public ProjectActivityViewModel(ProjectModifyModel model, string userName, IReadOnlyDictionary<string, TaskDao.TitleModel> titles)
	{
		Color = GetColor(model.action);
		Icon = GetIcon(model.action);
		bool? isNote = null;
		if (model.taskIds != null && model.taskIds.Any())
		{
			TaskId = model.taskIds[0];
			TaskDao.TitleModel value;
			bool flag = titles.TryGetValue(TaskId, out value);
			TaskTitle = (flag ? value.Title : Utils.GetString("Unknown"));
			isNote = (flag ? new bool?(value.Kind == "NOTE") : ((bool?)null));
		}
		Title = GetTitle(model, isNote);
		if (DateTime.TryParse(model.when, out var result))
		{
			When = DateUtils.FormatCommentTime(result);
			Date = result;
		}
		User = userName;
		if (model.whoProfile != null && !model.whoProfile.isMyself)
		{
			User = (string.IsNullOrEmpty(model.whoProfile.name) ? model.whoProfile.username : model.whoProfile.name);
		}
	}

	private static Geometry GetIcon(string action)
	{
		switch (action)
		{
		case "P_CREATE":
		case "T_CREATE":
			return Utils.GetGeometry("Ic.ProjectList.Add");
		case "P_IN":
			return Utils.GetIcon("IcActionMoveIn");
		case "P_OUT":
			return Utils.GetIcon("IcActionMoveOut");
		case "P_SHARE_JOIN":
			return Utils.GetIcon("IcActionJoin");
		case "P_SHARE_DELETE":
			return Utils.GetGeometry("Ic.Tabbar.Logout");
		case "T_DELETE":
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
		case "T_CANCEL":
		case "T_REPEAT_CANCEL":
			return Utils.GetGeometry("Ic.Common.Close");
		case "P_OPEN":
			return Utils.GetGeometry("Ic.Habit.Unarchive");
		case "P_CLOSE":
			return Utils.GetGeometry("Ic.Focus.Archive");
		default:
			return Utils.GetGeometry("Ic.ProjectList.Edit");
		}
	}

	private static SolidColorBrush GetColor(string action)
	{
		switch (action)
		{
		case "T_CREATE":
		case "P_CREATE":
		case "P_SHARE_JOIN":
		case "P_OPEN":
		case "P_IN":
		case "P_CLOSE":
			return ThemeUtil.GetColor("CreateTaskColor");
		case "T_DELETE":
		case "P_OUT":
		case "P_SHARE_DELETE":
			return ThemeUtil.GetColor("TextRedColor");
		case "T_REPEAT":
		case "T_DONE":
			return ThemeUtil.GetColor("CompleteTaskColor");
		case "T_UNDONE":
		case "T_REOPEN":
		case "T_TRASH_BACK":
			return ThemeUtil.GetColor("UndoTaskColor");
		case "T_CANCEL":
		case "T_REPEAT_CANCEL":
			return ThemeUtil.GetColor("AbandonedColor");
		default:
			return ThemeUtil.GetColor("EditTaskColor");
		}
	}

	private static string GetTitle(ProjectModifyModel model, bool? isNote)
	{
		string arg = "";
		return model.action switch
		{
			"P_CREATE" => Utils.GetString("ActionCreateProject"), 
			"P_DELETE" => Utils.GetString("ActionDeleteProject"), 
			"P_IN" => string.Format(Utils.GetString("ActionMoveIn"), (!isNote.HasValue) ? "" : Utils.GetString(isNote.Value ? "Notes" : "Task")), 
			"P_OUT" => string.Format(Utils.GetString("ActionMoveOut"), (!isNote.HasValue) ? "" : Utils.GetString(isNote.Value ? "Notes" : "Task")), 
			"P_TITLE" => Utils.GetString("ActionListTitle"), 
			"P_SHARE_JOIN" => Utils.GetString("ActionJoinIn"), 
			"P_CLOSE" => Utils.GetString("ArchivedShareList"), 
			"P_OPEN" => Utils.GetString("UnarchiveShareList"), 
			"P_SHARE_DELETE" => Utils.GetString("ActionQuit"), 
			"T_CREATE" => string.Format(Utils.GetString("ActionCreate"), (!isNote.HasValue) ? "" : Utils.GetString(isNote.Value ? "Notes" : "Task")), 
			"T_DELETE" => string.Format(Utils.GetString("ActionDelete"), (!isNote.HasValue) ? "" : Utils.GetString(isNote.Value ? "Notes" : "Task")), 
			"T_DONE" => Utils.GetString("ActionCompleteTask"), 
			"T_UNDONE" => Utils.GetString("ActionUnCompleteTask"), 
			"T_ASSIGN" => Utils.GetString("ActionAssignTask"), 
			"T_REPEAT" => Utils.GetString("ActionRepeat"), 
			"T_DUE" => Utils.GetString("ActionSetDate"), 
			"T_TITLE" => Utils.GetString("ActionModifyTitle"), 
			"T_CONTENT" => Utils.GetString("ActionModifyContent"), 
			"T_TRASH_BACK" => string.Format(Utils.GetString("ActionRestore"), (!isNote.HasValue) ? "" : Utils.GetString(isNote.Value ? "Notes" : "Task")), 
			"T_PROGRESS" => string.Format(Utils.GetString("ActionSetProgress"), arg), 
			"T_CANCEL" => Utils.GetString("ActionAbandonedTask"), 
			"T_REOPEN" => Utils.GetString("ActionReopenTask"), 
			"T_REPEAT_CANCEL" => Utils.GetString("ActionRepeatAbandoned"), 
			_ => string.Empty, 
		};
	}
}
