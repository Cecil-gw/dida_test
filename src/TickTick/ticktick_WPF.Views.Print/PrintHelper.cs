using System;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Print;

public static class PrintHelper
{
	public static string GetSubTaskTimeText(SubtaskPrintViewModel item)
	{
		if (item.SnoozeReminderTime.HasValue && !Utils.IsEmptyDate(item.SnoozeReminderTime) && item.SnoozeReminderTime.Value > DateTime.Now)
		{
			string arg = ((item.SnoozeReminderTime.Value.Date == DateTime.Today) ? Utils.GetString("Today") : Utils.GetString("Tomorrow")) + DateUtils.FormatHourMinuteText(item.SnoozeReminderTime.Value);
			return Utils.GetString("Today") + "," + string.Format(Utils.GetString("PreviewSnoozeText"), arg);
		}
		if (item.StartDate.HasValue && !Utils.IsEmptyDate(item.StartDate))
		{
			return DateUtils.FormatListDateString(item.StartDate.Value, null, item.IsAllDay);
		}
		return string.Empty;
	}
}
