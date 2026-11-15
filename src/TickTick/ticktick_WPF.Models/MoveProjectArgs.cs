namespace ticktick_WPF.Models;

public class MoveProjectArgs
{
	public string TaskId { get; set; }

	public string ProjectId { get; set; }

	public string UndoId { get; set; }

	public string ColumnId { get; set; }

	public ProjectModel Project { get; set; }

	public bool KeepAssignee { get; set; }

	public bool? IsTop { get; set; }

	public bool Notify { get; set; } = true;

	public MoveProjectArgs(ProjectModel project)
	{
		Project = project;
		ProjectId = project?.id;
	}

	public MoveProjectArgs(string taskId, ProjectModel project, bool keepAssignee = false, string undoId = null)
	{
		TaskId = taskId;
		Project = project;
		ProjectId = project?.id;
		KeepAssignee = keepAssignee;
		UndoId = undoId;
	}
}
