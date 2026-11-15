using System.Collections.Generic;
using System.Linq;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Util.Provider;

public class ProjectGroupData : SortProjectData
{
	public bool IsNote { get; set; }

	public ProjectGroupModel ProjectGroup { get; }

	public ProjectGroupData(ProjectGroupModel projectGroup, IEnumerable<ProjectModel> projects)
	{
		ProjectGroup = projectGroup;
		base.ShowProjectSort = true;
		List<ProjectModel> list = (from p in projects.ToList()
			where p.groupId == projectGroup.id && (!p.closed.HasValue || !p.closed.Value)
			select p).ToList();
		base.DefaultProjectModel = list.FirstOrDefault((ProjectModel p) => p.IsProjectPermit());
		if (base.DefaultProjectModel != null)
		{
			TitleInProjectGroup = base.DefaultProjectModel.name;
		}
		base.AddTaskHint = ((!TeamDao.IsTeamExpired(projectGroup.teamId) && list.Any((ProjectModel p) => p.IsProjectPermit())) ? string.Format(Utils.GetString("CenterAddTaskTextBoxPreviewText"), TitleInProjectGroup, Utils.GetString((base.DefaultProjectModel != null && base.DefaultProjectModel.IsNote) ? "Notes" : "Task").ToLower()) : string.Empty);
		base.ShowCustomSort = false;
		IsNote = list.Count > 0 && list.All((ProjectModel p) => p.IsNote);
		base.ShowLoadMore = true;
	}

	public override string GetTitle()
	{
		return ProjectGroup.name;
	}

	public override string GetEmptyTitle(bool allCompleted)
	{
		if (IsNote)
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
		if (IsNote)
		{
			return Utils.GetString("NoNoteDesc");
		}
		if (allCompleted)
		{
			return Utils.GetString("TaskItEasy");
		}
		return Utils.GetString("NoTaskNormalDesc");
	}

	public override EmptyIconsSourceModel GetEmptyIconsSource(bool allCompleted)
	{
		if (!allCompleted)
		{
			if (!IsNote)
			{
				return new EmptyIconsSourceModel("NormalEmpty", new List<int> { 1, 2, 3, 4, 6 });
			}
			return new EmptyIconsSourceModel("NoteEmpty");
		}
		return new EmptyIconsSourceModel("TaskCompletedEmpty");
	}

	public override async void SaveSortOption(SortOption sortOption)
	{
		ProjectGroup.SortOption = sortOption;
		ProjectGroup.sortType = ((sortOption.groupByType == Constants.SortType.none || sortOption.groupByType == Constants.SortType.assignee) ? sortOption.orderBy : sortOption.groupBy);
		await ProjectGroupDao.TrySaveProjectGroup(ProjectGroup);
	}
}
