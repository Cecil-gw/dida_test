using System;
using Newtonsoft.Json;
using SQLite;
using ticktick_WPF.Util.ReminderTime;

namespace ticktick_WPF.Models;

[Serializable]
public class ReminderModel
{
	public enum RemindType
	{
		Task = 0,
		Checklist = 1,
		Calendar = 2,
		Habit = 4,
		Course = 8,
		Countdown = 16,
		PayRemind = 90
	}

	public RemindType Type { get; set; }

	public string TaskId { get; set; }

	public string CheckItemId { get; set; }

	public string EventId { get; set; }

	public string EventArchivedId { get; set; }

	public string HabitId { get; set; }

	public string Creator { get; set; }

	public DateTime? StartDate { get; set; }

	public DateTime? DueDate { get; set; }

	public DateTime? ReminderTime { get; set; }

	public string Trigger { get; set; }

	public bool? IsAllDay { get; set; }

	public string Assignee { get; set; }

	public string ProjectId { get; set; }

	public string RepeatFlag { get; set; }

	public string Reminders { get; set; }

	public string Id { get; set; }

	public string GroupId { get; set; }

	public string Title { get; set; }

	public string Content { get; set; }

	public bool IsEnd { get; set; }

	[JsonIgnore]
	[Ignore]
	public bool IsAnnoyingAlert { get; set; }

	[JsonIgnore]
	[Ignore]
	public bool HasResolvedAnnoyingAlert { get; set; }

	public ReminderModel Copy()
	{
		return (ReminderModel)MemberwiseClone();
	}

	public string GetObjType()
	{
		return Type.ToString().ToLower();
	}

	public string GetObjId()
	{
		string result = Id;
		switch (Type)
		{
		case RemindType.Habit:
			result = HabitId;
			break;
		case RemindType.Calendar:
			result = EventId;
			break;
		case RemindType.Checklist:
			result = CheckItemId;
			break;
		case RemindType.Task:
			result = TaskId;
			break;
		}
		return result;
	}

	public string GetReminderKey()
	{
		return AnnoyingReminderManager.GetReminderKey(this);
	}

	public bool CanDelayTomorrow()
	{
		if (Type != RemindType.Course)
		{
			return Type != RemindType.Habit;
		}
		return false;
	}
}
