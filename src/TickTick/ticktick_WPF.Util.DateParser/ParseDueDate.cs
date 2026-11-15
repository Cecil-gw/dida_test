using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using ticktick_WPF.Models;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Util.DateParser;

public class ParseDueDate : IPaserDueDate
{
	public static Regex RemindMeRegex = new Regex("remind me|提醒我|リマインドする|알림|me rappeler|lembre-me|hапомнить мне");

	public string repeatFlag { get; set; }

	[JsonConverter(typeof(DateTimeConverter))]
	public DateTime? startDate { get; set; }

	[JsonConverter(typeof(DateTimeConverter))]
	public DateTime? dueDate { get; set; }

	public List<string> reminder { get; set; }

	public List<string> recognizeStrings { get; set; } = new List<string>();

	public bool isAllDay { get; set; }

	public string Text { get; set; }

	public List<string> GetRecognizeStrings()
	{
		return recognizeStrings;
	}

	public TimeData ToTimeData(bool addDefaultReminder)
	{
		DateTime? dateTime = dueDate;
		if (isAllDay && !Utils.IsEmptyDate(dateTime))
		{
			dateTime = dateTime?.AddDays(1.0);
		}
		if (startDate.HasValue)
		{
			startDate = startDate.Value.AddSeconds(startDate.Value.Second * -1);
		}
		if (dateTime.HasValue)
		{
			dateTime = dateTime.Value.AddSeconds(dateTime.Value.Second * -1);
		}
		TimeData timeData = new TimeData
		{
			StartDate = startDate,
			DueDate = dateTime,
			IsAllDay = isAllDay,
			RepeatFlag = repeatFlag,
			IsDefault = false
		};
		if (repeatFlag != null && repeatFlag.Contains("YEARLY"))
		{
			if (repeatFlag.Contains("INTERVAL=1") || !repeatFlag.Contains("INTERVAL"))
			{
				timeData.RepeatFrom = "2";
			}
			else
			{
				timeData.RepeatFrom = "0";
			}
		}
		reminder?.ForEach(delegate(string trigger)
		{
			timeData.Reminders.Add(new TaskReminderModel
			{
				trigger = trigger
			});
		});
		if (reminder == null || (reminder != null && ((reminder.Count == 1 && reminder[0] == "TRIGGER:-PT0S") || reminder.Count == 0)))
		{
			if (!UseDefaultReminder())
			{
				timeData.Reminders = (timeData.IsAllDay.Value ? TimeData.GetDefaultAllDayReminders() : TimeData.GetDefaultTimeReminders());
			}
			else if (reminder == null || reminder.Count == 0)
			{
				timeData.Reminders.Add(new TaskReminderModel
				{
					trigger = "TRIGGER:-PT0S"
				});
			}
		}
		return timeData;
	}

	private bool UseDefaultReminder()
	{
		if (string.IsNullOrEmpty(Text))
		{
			return true;
		}
		if (!RemindMeRegex.Match(Text.ToLower()).Success)
		{
			return Text.Contains(Utils.GetString("RemindMe"));
		}
		return true;
	}

	public bool IsTimeSeted()
	{
		return !Utils.IsEmptyDate(startDate);
	}
}
