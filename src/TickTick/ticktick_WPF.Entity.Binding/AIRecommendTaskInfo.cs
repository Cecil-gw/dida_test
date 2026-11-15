using System;
using ticktick_WPF.Models;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Entity.Binding;

public class AIRecommendTaskInfo
{
	public class TaskInfo
	{
		public string Id { get; set; }

		public string Title { get; set; }

		public int Status { get; set; }

		public int Deleted { get; set; }

		public string RepeatFlag { get; set; }

		public DateTime? StartDate { get; set; }

		public DateTime? DueDate { get; set; }

		public DateTime? CreatedTime { get; set; }

		public DateTime? ModifiedTime { get; set; }

		public string Kind { get; set; }

		public int Priority { get; set; }

		public bool IsAllDay { get; set; }

		public TaskInfo()
		{
		}

		public TaskInfo(TaskModel task)
		{
			Id = task.id;
			Title = task.title;
			Status = task.status;
			RepeatFlag = task.repeatFlag;
			StartDate = task.startDate;
			DueDate = task.dueDate;
			CreatedTime = task.createdTime;
			ModifiedTime = task.modifiedTime;
			Kind = task.kind;
			Priority = task.priority;
			Deleted = task.deleted;
			if (task.isAllDay.HasValue)
			{
				IsAllDay = task.isAllDay.Value;
			}
			else if (task.startDate.HasValue && task.startDate.Value.TimeOfDay == TimeSpan.Zero && task.dueDate.HasValue && task.dueDate.Value.TimeOfDay == TimeSpan.Zero)
			{
				IsAllDay = true;
			}
			else
			{
				IsAllDay = false;
			}
		}

		public TaskInfo(TaskBaseViewModel task)
		{
			Id = task.Id;
			Title = task.Title;
			Status = task.Status;
			RepeatFlag = task.RepeatFlag;
			StartDate = task.StartDate;
			DueDate = task.DueDate;
			CreatedTime = task.CreatedTime;
			ModifiedTime = task.ModifiedTime;
			Kind = task.Kind;
			Priority = task.Priority;
			Deleted = task.Deleted;
			if (task.IsAllDay.HasValue)
			{
				IsAllDay = task.IsAllDay.Value;
			}
			else if (task.StartDate.HasValue && task.StartDate.Value.TimeOfDay == TimeSpan.Zero && task.DueDate.HasValue && task.DueDate.Value.TimeOfDay == TimeSpan.Zero)
			{
				IsAllDay = true;
			}
			else
			{
				IsAllDay = false;
			}
		}
	}

	public TaskInfo Task { get; set; }

	public int ActivityCount { get; set; }

	public int Duration { get; set; }

	public int Importance { get; set; }

	public AIRecommendTaskInfo()
	{
	}

	public AIRecommendTaskInfo(TaskModel task)
	{
		Task = new TaskInfo(task);
	}

	public AIRecommendTaskInfo(TaskBaseViewModel task)
	{
		Task = new TaskInfo(task);
	}
}
