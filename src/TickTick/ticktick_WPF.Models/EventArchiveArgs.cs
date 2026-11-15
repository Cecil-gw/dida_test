using System;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Models;

public class EventArchiveArgs
{
	public DateTime? StartTime { get; set; }

	public DateTime? EndTime { get; set; }

	public string Id { get; set; }

	public string Title { get; set; }

	public string RepeatFlag { get; set; }

	public bool IsAllDay { get; set; }

	public EventArchiveArgs(CalendarEventModel model)
	{
		StartTime = model.DueStart;
		EndTime = model.DueEnd;
		Id = model.Id;
		Title = model.Title;
		RepeatFlag = model.RepeatFlag;
		IsAllDay = model.IsAllDay;
	}

	public EventArchiveArgs(TaskBaseViewModel model)
	{
		StartTime = model.StartDate;
		EndTime = model.DueDate;
		Id = model.Id;
		Title = model.Title;
		RepeatFlag = model.RepeatFlag;
		IsAllDay = model.IsAllDay ?? true;
	}
}
