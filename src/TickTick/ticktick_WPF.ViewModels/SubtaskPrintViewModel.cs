using System;
using ticktick_WPF.Models;

namespace ticktick_WPF.ViewModels;

public class SubtaskPrintViewModel
{
	public string Title { get; set; }

	public int Status { get; set; }

	public bool? IsAllDay { get; set; }

	public DateTime? StartDate { get; set; }

	public DateTime? SnoozeReminderTime { get; set; }

	public int Level { get; set; }

	public string Kind { get; set; }

	public int Priority { get; set; }

	public SubtaskPrintViewModel(TaskDetailItemModel model)
	{
		Title = model.title;
		Status = model.status;
		IsAllDay = model.isAllDay;
		StartDate = model.startDate;
		SnoozeReminderTime = model.snoozeReminderTime;
		Kind = "TEXT";
		Level = 1;
	}

	public SubtaskPrintViewModel(DisplayItemModel model)
	{
		Title = model.Title;
		Status = model.Status;
		IsAllDay = model.IsAllDay;
		StartDate = model.StartDate;
		SnoozeReminderTime = model.RemindTime;
		Level = model.Level;
		Kind = model.Kind;
		Priority = model.Priority;
	}
}
