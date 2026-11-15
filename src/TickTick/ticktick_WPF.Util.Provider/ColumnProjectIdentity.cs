using System;
using System.Collections.Generic;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Kanban;

namespace ticktick_WPF.Util.Provider;

public sealed class ColumnProjectIdentity : ProjectIdentity
{
	public readonly string ColumnId;

	public override string LoadMoreId => Project?.CatId;

	public override string QueryId => Project?.CatId;

	public override string CatId => Project?.CatId;

	public override string Title => Project?.GetDisplayTitle();

	public override bool CanEdit
	{
		get
		{
			if (ColumnId.Contains("project:"))
			{
				return CacheManager.GetProjectById(ColumnViewModel.GetProject(ColumnId))?.IsEnable() ?? true;
			}
			return (Project as NormalProjectIdentity)?.Project?.IsEnable() ?? (Project as GroupProjectIdentity)?.CanAddTask() ?? true;
		}
	}

	public ProjectIdentity Project { get; }

	public override string SortProjectId => Project?.SortProjectId;

	public ColumnProjectIdentity(ProjectIdentity project, string columnId)
	{
		Id = columnId;
		ColumnId = columnId;
		Project = project;
		if (project != null)
		{
			SortOption sortOption = project.SortOption;
			base.SortOption = new SortOption
			{
				groupBy = ((sortOption.orderByType == sortOption.groupByType || (columnId == "note" && sortOption.orderByType != Constants.SortType.tag)) ? "none" : sortOption.orderBy),
				orderBy = sortOption.orderBy,
				order = sortOption.order,
				isFromKanbanColumn = true
			};
			if (columnId.StartsWith("project:"))
			{
				IsNote = CacheManager.GetProjectById(ColumnViewModel.GetProject(columnId))?.IsNote ?? project.IsNote;
			}
			else
			{
				IsNote = project.IsNote || columnId == "note";
			}
		}
		else
		{
			base.SortOption = SortOptionUtils.GetSortOptionBySortType(Constants.SortType.dueDate, inKanban: true);
		}
	}

	public override string GetProjectId()
	{
		if (ColumnId.StartsWith("assign:") && Project is GroupProjectIdentity groupProjectIdentity)
		{
			string assignee = GetAssignee();
			if (!string.IsNullOrEmpty(assignee) && assignee != "-1")
			{
				string text = ProjectDao.GetAssignProjectInGroup(groupProjectIdentity.GroupId, assignee)?.id;
				if (text != null)
				{
					return text;
				}
			}
		}
		if (ColumnId.StartsWith("project:"))
		{
			return ColumnViewModel.GetProject(ColumnId);
		}
		return Project?.GetProjectId();
	}

	public override string GetProjectName()
	{
		return Project?.GetProjectName();
	}

	public override string GetDisplayTitle()
	{
		return Project?.GetDisplayTitle();
	}

	public override ProjectIdentity Copy(ProjectIdentity project)
	{
		return new NormalProjectIdentity(CacheManager.GetProjectById(project.Id) ?? ((NormalProjectIdentity)project).Project);
	}

	public SortOption GetRealSortOption()
	{
		return Project?.SortOption;
	}

	public override bool CanAddTask()
	{
		if (ColumnId == "habit" || ColumnId == "course" || ColumnId == "calendar")
		{
			return false;
		}
		return Project?.CanAddTask() ?? false;
	}

	public override string GetSortProjectId()
	{
		SortOption sortOption = Project?.SortOption;
		if ((sortOption == null || sortOption.groupByType != Constants.SortType.dueDate) && (sortOption == null || sortOption.groupByType != Constants.SortType.project))
		{
			return CatId;
		}
		return SortProjectId;
	}

	public override int GetPriority()
	{
		if (ColumnId.StartsWith("priority:"))
		{
			return ColumnViewModel.GetPriority(ColumnId);
		}
		return Project?.GetPriority() ?? base.GetPriority();
	}

	public override TimeData GetTimeData()
	{
		if (ColumnId.StartsWith("date:"))
		{
			DateTime? date = ColumnViewModel.GetDate(ColumnId);
			return new TimeData
			{
				StartDate = date,
				IsAllDay = true,
				IsDefault = true,
				Reminders = TimeData.GetDefaultAllDayReminders()
			};
		}
		TimeData timeData = Project?.GetTimeData();
		if (timeData != null && ColumnId == "note")
		{
			timeData.Reminders = null;
			if (!(Project is TodayProjectIdentity) && !(Project is TomorrowProjectIdentity) && !(Project is WeekProjectIdentity))
			{
				timeData = null;
			}
		}
		return timeData;
	}

	public override string GetColumnId()
	{
		return ColumnId;
	}

	public override List<string> GetTags()
	{
		if (ColumnId.StartsWith("tag:"))
		{
			if (ColumnId == "tag:#notag")
			{
				return new List<string>();
			}
			string item = ColumnId.Replace("tag:", "");
			return new List<string> { item };
		}
		return Project?.GetTags();
	}

	public override string GetAssignee()
	{
		if (ColumnId.StartsWith("assign:"))
		{
			if (ColumnId == "assign:-1")
			{
				return null;
			}
			return ColumnId.Replace("assign:", "");
		}
		return null;
	}
}
