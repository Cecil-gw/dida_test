using System;
using System.Collections.Generic;
using System.Linq;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Provider;

namespace ticktick_WPF.Views.Search;

public class SearchFilterModel
{
	public List<string> GroupIds;

	public List<string> ProjectIds;

	public List<string> CalendarAccountIds;

	public List<string> CalendarIds;

	public List<string> Tags;

	public List<int> Priorities;

	public TaskType TaskType = TaskType.TaskAndNote;

	public List<string> Assignees;

	public int? Status;

	public DateTime? Start;

	public DateTime? End;

	public string Key;

	public DateFilter DateFilter;

	public SearchKind Kind;

	public SearchCategory Category;

	public List<string> ExcludeTaskIds;

	public bool IsCategorySearch
	{
		get
		{
			if (Category != SearchCategory.None)
			{
				if (string.IsNullOrWhiteSpace(Key))
				{
					return HasFilterConditions();
				}
				return true;
			}
			return false;
		}
	}

	public bool HasFilterConditions()
	{
		if ((ProjectIds == null || ProjectIds.Count <= 0) && (GroupIds == null || GroupIds.Count <= 0) && (CalendarAccountIds == null || CalendarAccountIds.Count <= 0) && (CalendarIds == null || CalendarIds.Count <= 0) && (Tags == null || Tags.Count <= 0) && (Priorities == null || Priorities.Count <= 0) && (Assignees == null || Assignees.Count <= 0) && !Status.HasValue && !Start.HasValue && !End.HasValue)
		{
			return TaskType != TaskType.TaskAndNote;
		}
		return true;
	}

	public bool HasCalendarFilterOnly()
	{
		bool flag = ProjectIds?.Contains("Calendar5959a2259161d16d23a4f272") ?? false;
		bool num = (CalendarAccountIds != null && CalendarAccountIds.Count > 0) || (CalendarIds != null && CalendarIds.Count > 0) || flag;
		bool flag2 = HasListScope();
		if (num)
		{
			return !flag2;
		}
		return false;
	}

	public bool SearchEvent()
	{
		bool flag = ProjectIds?.Contains("Calendar5959a2259161d16d23a4f272") ?? false;
		bool flag2 = (CalendarAccountIds != null && CalendarAccountIds.Count > 0) || (CalendarIds != null && CalendarIds.Count > 0) || flag;
		bool flag3 = HasListScope();
		List<string> projectIds = ProjectIds;
		if ((projectIds == null || !projectIds.Any((string id) => !CalendarUtils.IsNotConcreteProjectFilterId(id))) && (!flag3 || flag2) && (Tags == null || Tags.Count == 0) && (GroupIds == null || GroupIds.Count == 0) && (Priorities == null || Priorities.Count == 0) && (Assignees == null || Assignees.Count == 0) && !Status.HasValue)
		{
			return TaskType == TaskType.TaskAndNote;
		}
		return false;
	}

	private bool HasListScope()
	{
		if (!CalendarUtils.ContainsAllListId(ProjectIds))
		{
			List<string> projectIds = ProjectIds;
			if (projectIds == null || !projectIds.Any((string id) => !CalendarUtils.IsNotConcreteProjectFilterId(id)))
			{
				if (GroupIds != null)
				{
					return GroupIds.Count > 0;
				}
				return false;
			}
		}
		return true;
	}
}
