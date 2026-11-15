using System;
using System.Collections.Generic;
using ticktick_WPF.Models;

namespace ticktick_WPF.ViewModels;

public class BatchData
{
	public DateTime? StartDate;

	public DateTime? DueDate;

	public bool? IsAllDay;

	public string RepeatFlag;

	public string RepeatFrom;

	public string TimeZone;

	public string Assign;

	public bool IsFloating;

	public bool IsUnified
	{
		get
		{
			if (IsReminderUnified)
			{
				return IsRepeatUnified;
			}
			return false;
		}
	}

	public List<TaskReminderModel> Reminders { get; set; }

	public bool IsDateUnified { get; set; } = true;

	public bool IsTimeUnified { get; set; } = true;

	public bool IsReminderUnified { get; set; } = true;

	public bool IsRepeatUnified { get; set; } = true;

	public BatchData Clone()
	{
		return (BatchData)MemberwiseClone();
	}

	public void SetUnified()
	{
		IsTimeUnified = true;
		IsReminderUnified = true;
		IsRepeatUnified = true;
	}
}
