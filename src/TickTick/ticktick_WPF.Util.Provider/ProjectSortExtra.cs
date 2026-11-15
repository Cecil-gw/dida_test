using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Views.Kanban;

namespace ticktick_WPF.Util.Provider;

public class ProjectSortExtra
{
	public bool LoadAll;

	public string SortCatId;

	public string SortKey;

	public string ProjectId;

	public long DefaultOrder;

	public bool ShowCompleted;

	public bool IsAll { get; set; }

	public bool IsToday { get; set; }

	public bool IsTomorrow { get; set; }

	public bool IsWeek { get; set; }

	public bool IsCompleted { get; set; }

	public bool IsAbandoned { get; set; }

	public bool IsClosed
	{
		get
		{
			if (!IsCompleted)
			{
				return IsAbandoned;
			}
			return true;
		}
	}

	public bool IsSearch { get; set; }

	public bool IsTrash { get; set; }

	public bool InKanban { get; set; }

	public bool InGroup { get; set; }

	public bool InNoteColumn { get; set; }

	public SortOption SortOption { get; set; }

	public bool ExistSpecialOrderItem { get; set; }

	public bool ShowFold { get; set; }

	public bool Editable { get; set; }

	public bool BuildByChildIds { get; set; }

	public static ProjectSortExtra Build(ProjectIdentity projectIdentity)
	{
		ProjectIdentity projectIdentity2 = ((projectIdentity is ColumnProjectIdentity columnProjectIdentity) ? columnProjectIdentity.Project : projectIdentity);
		ProjectSortExtra projectSortExtra = new ProjectSortExtra
		{
			ProjectId = projectIdentity.GetProjectId(),
			IsAll = (projectIdentity2 is AllProjectIdentity),
			IsToday = (projectIdentity2 is TodayProjectIdentity),
			IsTomorrow = (projectIdentity2 is TomorrowProjectIdentity),
			IsWeek = (projectIdentity2 is WeekProjectIdentity),
			IsCompleted = (projectIdentity is CompletedProjectIdentity),
			IsAbandoned = (projectIdentity is AbandonedProjectIdentity),
			ShowCompleted = ((projectIdentity is MatrixQuadrantIdentity) ? LocalSettings.Settings.MatrixShowCompleted : (!LocalSettings.Settings.HideComplete)),
			IsSearch = (projectIdentity is SearchProjectIdentity),
			IsTrash = (projectIdentity is TrashProjectIdentity),
			SortOption = projectIdentity.SortOption.Copy(),
			LoadAll = projectIdentity.LoadAll,
			SortCatId = projectIdentity.GetSortProjectId(),
			InGroup = (projectIdentity is GroupProjectIdentity)
		};
		if (projectSortExtra.SortOption.orderByType != Constants.SortType.sortOrder)
		{
			projectSortExtra.SortKey = projectSortExtra.SortOption.GetSortKey();
		}
		projectSortExtra.Editable = projectIdentity.CanEdit;
		if (projectIdentity is ColumnProjectIdentity columnProjectIdentity2)
		{
			projectSortExtra.InKanban = true;
			projectSortExtra.InNoteColumn = columnProjectIdentity2.Id == "note";
			SortOption realSortOption = columnProjectIdentity2.GetRealSortOption();
			if (projectSortExtra.SortOption.orderByType != Constants.SortType.sortOrder)
			{
				projectSortExtra.SortKey = ColumnViewModel.GetTaskSortKey(realSortOption, columnProjectIdentity2.ColumnId);
			}
			if (columnProjectIdentity2.ColumnId == "habit")
			{
				projectSortExtra.SortOption.groupBy = "none";
			}
			if (projectSortExtra.SortOption.groupByType != Constants.SortType.dueDate && (columnProjectIdentity2.ColumnId == "calendar" || columnProjectIdentity2.ColumnId == "course"))
			{
				projectSortExtra.SortOption.groupBy = "none";
			}
		}
		projectSortExtra.DefaultOrder = ((TaskDefaultDao.GetDefaultSafely().AddTo == 0) ? long.MinValue : long.MaxValue);
		return projectSortExtra;
	}
}
