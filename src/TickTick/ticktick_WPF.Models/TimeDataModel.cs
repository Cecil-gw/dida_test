using System;

namespace ticktick_WPF.Models;

public class TimeDataModel
{
	public string TaskId { get; set; }

	public string ItemId { get; set; }

	public string EventId { get; set; }

	public DateTime? StartDate { get; set; }

	public DateTime? DueDate { get; set; }

	public bool? IsAllDay { get; set; }

	public string RepeatFrom { get; set; }

	public string RepeatFlag { get; set; }

	public ReminderMode HandleReminderMode { get; set; }
}
