using System;
using System.Collections.Generic;
using ticktick_WPF.Models;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Util.DateParser;

public class OldParseDueDate : IPaserDueDate
{
	public string RepeatFlag;

	public DateTime DueDate;

	private string _reminder;

	public List<string> RecognizeStrings = new List<string>();

	public TickTickDuration Duration { private get; set; }

	public void SetDueDate(DateTime dueDate)
	{
		DueDate = dueDate;
	}

	public void SetReminder(string reminder)
	{
		_reminder = reminder;
	}

	public TimeData ToTimeData(bool addDefaultReminder)
	{
		if (Utils.IsEmptyDate(DueDate))
		{
			DueDate = DateTime.Today;
		}
		TimeData timeData = new TimeData();
		if (Duration != null)
		{
			if (Utils.IsEmptyDate(DueDate) && !string.IsNullOrEmpty(_reminder))
			{
				DueDate = DateTime.Now.Date;
			}
			if (!Duration.IsPositive())
			{
				SetReminder(Duration.ToString());
			}
		}
		if (!Utils.IsEmptyDate(DueDate))
		{
			timeData.StartDate = DueDate;
			timeData.IsAllDay = _reminder == null;
		}
		if (!string.IsNullOrEmpty(RepeatFlag))
		{
			timeData.RepeatFlag = RepeatFlag;
		}
		if (_reminder != null)
		{
			timeData.Reminders = ((_reminder == "TRIGGER:PT0S") ? TimeData.GetDefaultTimeReminders() : new List<TaskReminderModel>
			{
				new TaskReminderModel
				{
					trigger = _reminder,
					id = Utils.GetGuid()
				}
			});
		}
		else
		{
			timeData.Reminders = TimeData.GetDefaultAllDayReminders();
		}
		if (addDefaultReminder)
		{
			timeData.Reminders = ((timeData.IsAllDay.HasValue && timeData.IsAllDay.Value) ? TimeData.GetDefaultAllDayReminders() : TimeData.GetDefaultTimeReminders());
		}
		return timeData;
	}

	public List<string> GetRecognizeStrings()
	{
		return RecognizeStrings;
	}

	public bool IsTimeSeted()
	{
		return !Utils.IsEmptyDate(DueDate);
	}
}
