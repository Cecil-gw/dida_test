using ticktick_WPF.Models;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Util.Provider;

public sealed class ParentTaskIdentity : ProjectIdentity
{
	private TaskBaseViewModel _task;

	public string ProjectId;

	public override string SortProjectId => null;

	public ParentTaskIdentity(TaskBaseViewModel task)
	{
		_task = task;
		Id = task.Id;
		ProjectId = task.ProjectId;
		base.SortOption = new SortOption
		{
			groupBy = "none",
			orderBy = "sortOrder"
		};
	}

	public override ProjectIdentity Copy(ProjectIdentity project)
	{
		return new ParentTaskIdentity(((ParentTaskIdentity)project)._task);
	}
}
