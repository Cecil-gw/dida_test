using System;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;

namespace ticktick_WPF.Util.Provider;

public abstract class ProjectData
{
	public string TitleInProjectGroup = "";

	public ProjectIdentity ProjectIdentity { get; set; }

	public bool IsCompleted { get; set; }

	public bool IsTrash { get; set; }

	public bool IsProjectClosed { get; set; }

	public bool IsTeamExpired { get; set; }

	public string EmptyTitle { get; protected set; } = Utils.GetString("InboxA");

	public string EmptyContent { get; protected set; } = Utils.GetString("InboxB");

	public string AddTaskHint { get; protected set; } = Utils.GetString("CenterAddTaskTextBoxPreviewTextInbox");

	public DateTime? DefaultTaskDate { get; protected set; }

	public ProjectModel DefaultProjectModel { get; protected set; } = new ProjectModel
	{
		_Id = LocalSettings.Settings.InBoxId,
		id = "inbox" + LocalSettings.Settings.LoginUserId,
		name = Utils.GetString("Inbox")
	};

	public bool ShowShare { get; protected set; }

	public bool ShowProjectSort { get; protected set; }

	public bool ShowCustomSort { get; protected set; }

	public bool ShowAssignSort { get; protected set; }

	protected bool IgnoreTodaySection { get; set; }
}
