using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Provider;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.QuickAdd;
using ticktick_WPF.Views.Tag;

namespace ticktick_WPF.Views.TaskList;

public static class SubtaskToTaskHelper
{
	public static async Task<TaskPrimaryProperty> GetTaskPrimaryProperty(TaskPrimaryProperty primaryTaskProperty, ProjectIdentity projectIdentity, DisplayItemModel dropTarget, bool below, Section dropSection, bool checkSection)
	{
		TaskDefaultModel defaultSafely = TaskDefaultDao.GetDefaultSafely();
		TaskPrimaryProperty defaultProperty = new TaskPrimaryProperty
		{
			Priority = defaultSafely.Priority,
			ProjectId = Utils.GetInboxId(),
			TimeData = TimeData.BuildFromDefault(defaultSafely)
		};
		if ((primaryTaskProperty.TimeData?.IsAllDay.HasValue ?? false) && !primaryTaskProperty.TimeData.IsAllDay.Value)
		{
			defaultProperty.TimeData.Reminders = TimeData.GetDefaultTimeReminders();
		}
		return await MergeTaskProperty(primaryTaskProperty, defaultProperty, await GetDropDefaultProperty(projectIdentity.SortOption.groupByType, dropTarget, below, dropSection, checkSection));
	}

	public static async Task<TaskPrimaryProperty> MergeTaskProperty(TaskPrimaryProperty taskProperty, TaskPrimaryProperty defaultProperty, TaskPrimaryProperty dropProperty)
	{
		TaskPrimaryProperty result = new TaskPrimaryProperty
		{
			Priority = defaultProperty.Priority,
			ProjectId = defaultProperty.ProjectId,
			TaskStatus = dropProperty.TaskStatus,
			AssigneeId = dropProperty.AssigneeId,
			SortOrder = dropProperty.SortOrder,
			ParentId = taskProperty.ParentId
		};
		MergePriority(taskProperty, dropProperty, result);
		MergeProjectId(taskProperty, dropProperty, result);
		MergeTimeData(defaultProperty, taskProperty, dropProperty, result);
		MergeTags(taskProperty, dropProperty, result);
		return result;
	}

	private static void MergeTags(TaskPrimaryProperty taskProperty, TaskPrimaryProperty dropProperty, TaskPrimaryProperty result)
	{
		result.Tags = new List<string>();
		if (taskProperty.Tags != null && taskProperty.Tags.Any())
		{
			result.Tags.AddRange(taskProperty.Tags);
		}
		if (dropProperty.Tags != null && dropProperty.Tags.Any())
		{
			result.Tags.AddRange(dropProperty.Tags);
		}
	}

	private static void MergeTimeData(TaskPrimaryProperty defaultProperty, TaskPrimaryProperty taskProperty, TaskPrimaryProperty dropProperty, TaskPrimaryProperty result)
	{
		result.TimeData = new TimeData();
		if (taskProperty.TimeData != null && !Utils.IsEmptyDate(taskProperty.TimeData.StartDate))
		{
			result.TimeData.StartDate = taskProperty.TimeData.StartDate;
			result.TimeData.DueDate = taskProperty.TimeData.DueDate;
			result.TimeData.IsAllDay = taskProperty.TimeData.IsAllDay;
			if (!string.IsNullOrEmpty(taskProperty.TimeData.RepeatFlag))
			{
				result.TimeData.RepeatFrom = taskProperty.TimeData.RepeatFrom;
				result.TimeData.RepeatFlag = taskProperty.TimeData.RepeatFlag;
			}
			if (taskProperty.TimeData.Reminders != null && taskProperty.TimeData.Reminders.Any())
			{
				result.TimeData.Reminders = new List<TaskReminderModel>();
				foreach (TaskReminderModel reminder in taskProperty.TimeData.Reminders)
				{
					result.TimeData.Reminders.Add(new TaskReminderModel
					{
						trigger = reminder.trigger
					});
				}
			}
		}
		if (dropProperty.TimeData != null)
		{
			result.TimeData.StartDate = dropProperty.TimeData.StartDate;
			result.TimeData.IsAllDay = dropProperty.TimeData.IsAllDay;
			TimeData timeData = taskProperty.TimeData;
			if (timeData != null && timeData.StartDate.HasValue)
			{
				result.TimeData.IsAllDay = taskProperty.TimeData.IsAllDay;
				result.TimeData.StartDate = taskProperty.TimeData.StartDate;
				if (dropProperty.TimeData.StartDate.HasValue)
				{
					int num = (int)(dropProperty.TimeData.StartDate.Value.Date - taskProperty.TimeData.StartDate.Value.Date).TotalDays;
					result.TimeData.StartDate = result.TimeData.StartDate.Value.AddDays(num);
					TimeData timeData2 = taskProperty.TimeData;
					if (timeData2 != null && timeData2.DueDate.HasValue)
					{
						double totalMinutes = (taskProperty.TimeData.DueDate.Value - taskProperty.TimeData.StartDate.Value).TotalMinutes;
						result.TimeData.DueDate = result.TimeData.StartDate.Value.AddMinutes(totalMinutes);
					}
				}
			}
		}
		if (result.TimeData != null && (result.TimeData.Reminders == null || !result.TimeData.Reminders.Any()) && result.TimeData.StartDate.HasValue && !Utils.IsEmptyDate(result.TimeData.StartDate))
		{
			TimeData timeData3 = taskProperty.TimeData;
			if (timeData3 != null && timeData3.IsAllDay.HasValue && taskProperty.TimeData?.Reminders != null && taskProperty.TimeData.Reminders.Any() && taskProperty.TimeData.IsAllDay.Value)
			{
				result.TimeData.Reminders = taskProperty.TimeData.Reminders;
			}
		}
		if (result.TimeData != null && !Utils.IsEmptyDate(result.TimeData.StartDate) && (result.TimeData.Reminders == null || !result.TimeData.Reminders.Any()))
		{
			result.TimeData.Reminders = defaultProperty.TimeData.Reminders;
		}
		result.TimeData.TimeZone = ((taskProperty.TimeData == null) ? defaultProperty.TimeData.TimeZone : taskProperty.TimeData.TimeZone);
	}

	private static void MergeProjectId(TaskPrimaryProperty taskProperty, TaskPrimaryProperty dropProperty, TaskPrimaryProperty result)
	{
		if (!string.IsNullOrEmpty(taskProperty.ProjectId))
		{
			result.ProjectId = taskProperty.ProjectId;
		}
		if (!string.IsNullOrEmpty(dropProperty.ProjectId))
		{
			result.ProjectId = dropProperty.ProjectId;
		}
	}

	private static void MergePriority(TaskPrimaryProperty taskProperty, TaskPrimaryProperty dropProperty, TaskPrimaryProperty result)
	{
		if (taskProperty.Priority.HasValue && taskProperty.Priority != 0)
		{
			result.Priority = taskProperty.Priority;
		}
		if (dropProperty.Priority.HasValue)
		{
			result.Priority = dropProperty.Priority;
		}
		if (!result.Priority.HasValue)
		{
			result.Priority = 0;
		}
	}

	public static TaskPrimaryProperty GetProjectDefaultProperty(IProjectTaskDefault project)
	{
		TaskPrimaryProperty taskPrimaryProperty = new TaskPrimaryProperty();
		if (!(project is FilterProjectIdentity filterProjectIdentity))
		{
			if (!(project is TagProjectIdentity tagProjectIdentity))
			{
				if (!(project is NormalProjectIdentity normalProjectIdentity))
				{
					if (project is GroupProjectIdentity groupProjectIdentity)
					{
						taskPrimaryProperty.ProjectId = groupProjectIdentity.GetProjectId();
					}
					else
					{
						taskPrimaryProperty.TimeData = project.GetTimeData();
					}
				}
				else
				{
					taskPrimaryProperty.ProjectId = normalProjectIdentity.GetProjectId();
				}
			}
			else
			{
				taskPrimaryProperty.Tags = tagProjectIdentity.GetTags();
			}
		}
		else
		{
			taskPrimaryProperty.Priority = filterProjectIdentity.GetPriority();
			taskPrimaryProperty.ProjectId = filterProjectIdentity.GetProjectId();
			taskPrimaryProperty.Tags = filterProjectIdentity.GetTags();
			taskPrimaryProperty.TimeData = filterProjectIdentity.GetTimeData();
		}
		return taskPrimaryProperty;
	}

	private static async Task<TaskPrimaryProperty> GetDropDefaultProperty(Constants.SortType groupBy, DisplayItemModel dropTarget, bool below, Section dropSection, bool checkSection)
	{
		TaskPrimaryProperty taskPrimaryProperty = new TaskPrimaryProperty();
		if (dropSection is CompletedSection)
		{
			taskPrimaryProperty.TaskStatus = 2;
			return taskPrimaryProperty;
		}
		switch (groupBy)
		{
		case Constants.SortType.tag:
			if (checkSection && dropSection != null && dropSection is TagSection tagSection)
			{
				taskPrimaryProperty.Tags = TagSerializer.ToTags(tagSection.GetTag());
			}
			break;
		case Constants.SortType.sortOrder:
		case Constants.SortType.project:
			if (dropTarget != null)
			{
				taskPrimaryProperty.ProjectId = dropTarget.ProjectId;
				taskPrimaryProperty.SortOrder = ProjectSortOrderDao.GetSiblingSortOrderInProject(dropTarget.ProjectId, dropTarget.TaskId, !below);
			}
			break;
		case Constants.SortType.priority:
			if (checkSection && dropTarget != null)
			{
				taskPrimaryProperty.ProjectId = dropTarget.ProjectId;
				taskPrimaryProperty.SortOrder = ProjectSortOrderDao.GetSiblingSortOrderInProject(dropTarget.ProjectId, dropTarget.TaskId, !below);
				taskPrimaryProperty.Priority = dropTarget.Priority;
			}
			break;
		case Constants.SortType.dueDate:
			if (checkSection)
			{
				if (dropTarget?.StartDate.HasValue ?? false)
				{
					taskPrimaryProperty.TimeData = new TimeData
					{
						StartDate = dropTarget.StartDate.Value.Date,
						IsAllDay = true
					};
				}
				if (dropSection != null)
				{
					taskPrimaryProperty.TimeData = new TimeData
					{
						StartDate = dropSection.GetStartDate(),
						IsAllDay = true
					};
				}
			}
			break;
		case Constants.SortType.assignee:
			if (checkSection && dropSection != null)
			{
				taskPrimaryProperty.AssigneeId = dropSection.GetAssignee();
			}
			break;
		}
		return taskPrimaryProperty;
	}
}
