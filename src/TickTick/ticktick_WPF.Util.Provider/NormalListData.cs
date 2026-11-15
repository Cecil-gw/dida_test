using System.Collections.Generic;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util.Sync;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Util.Provider;

public class NormalListData : SortProjectData
{
	public ProjectModel Project { get; }

	public NormalListData(ProjectModel project)
	{
		Project = project;
		base.AddTaskHint = (project.IsEnable() ? string.Format(Utils.GetString("CenterAddTaskTextBoxPreviewText"), GetTitle(), Utils.GetString(project.IsNote ? "Notes" : "Task").ToLower()) : "");
		base.DefaultProjectModel = Project;
		base.IsProjectClosed = project.closed == true;
		base.IsTeamExpired = TeamDao.IsTeamExpired(project.teamId);
		base.ShowShare = !project.Isinbox;
		base.ShowAssignSort = project.IsShareList();
		base.ShowCustomSort = true;
		base.ShowLoadMore = true;
	}

	public sealed override string GetTitle()
	{
		if (!Project.Isinbox)
		{
			return Project.name;
		}
		return Utils.GetString("Inbox");
	}

	public override string GetEmptyTitle(bool allCompleted)
	{
		if (Project.IsNote)
		{
			return Utils.GetString("NoNotes");
		}
		if (allCompleted)
		{
			return Utils.GetString("TaskAllCompleted");
		}
		return Utils.GetString("NoTasks");
	}

	public override string GetEmptyContent(bool allCompleted)
	{
		if (Project.IsNote)
		{
			return Utils.GetString("NoNoteDesc");
		}
		if (allCompleted)
		{
			return Utils.GetString("TaskItEasy");
		}
		if (Project.Isinbox)
		{
			return Utils.GetString("NoTaskInboxDesc");
		}
		return Utils.GetString("NoTaskNormalDesc");
	}

	public override async void SaveSortOption(SortOption sortOption)
	{
		if (!Project.Isinbox)
		{
			if (Project.SortOption == null || !Project.SortOption.Equal(sortOption))
			{
				Project.sync_status = Constants.SyncStatus.SYNC_UPDATE.ToString();
				Project.SortOption = sortOption;
				Project.sortType = ((sortOption.groupByType == Constants.SortType.none) ? sortOption.orderBy : sortOption.groupBy);
				await ProjectDao.TryUpdateProject(Project);
			}
		}
		else
		{
			if (sortOption.groupByType == Constants.SortType.sortOrder)
			{
				ColumnBatchHandler.MergeWithServer(Project.id);
			}
			SortProjectData.SaveSpecialProjectSortType("SortTypeOfInbox", sortOption);
			LocalSettings.Settings.SaveSmartProjectOptions("inbox", sortOption);
			ProjectDao.UpdateInboxSortType(sortOption);
		}
	}

	public override EmptyIconsSourceModel GetEmptyIconsSource(bool allCompleted)
	{
		bool flag = Project?.Isinbox ?? false;
		ProjectModel project = Project;
		if (project != null && project.IsNote)
		{
			return new EmptyIconsSourceModel("NoteEmpty");
		}
		if (allCompleted)
		{
			return new EmptyIconsSourceModel("TaskCompletedEmpty");
		}
		if (flag)
		{
			return new EmptyIconsSourceModel("InboxEmpty", new List<int> { 1, 2, 3, 4, 6 });
		}
		return new EmptyIconsSourceModel("NormalEmpty", new List<int> { 1, 2, 3, 4, 6 });
	}
}
