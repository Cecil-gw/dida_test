using System;
using System.Collections.Generic;
using System.Linq;
using ticktick_WPF.Util;
using ticktick_WPF.Util.KotlinUtils;
using ticktick_WPF.Util.Sync;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Dal;

public class RepeatDao
{
	public static DateTime GetEventNextRepeatDate(string rrule, DateTime start, string timeZone)
	{
		DateTime today = DateTime.Today;
		DateTime spanEnd = today.AddDays(3.0);
		List<DateTime> repeatDates = RepeatCalculator.GetRepeatDates(rrule, "0", start, today, spanEnd, null, timeZone);
		if (repeatDates != null && repeatDates.Count > 0)
		{
			return repeatDates[0];
		}
		return RepeatCalculator.GetRepeatNextDate(rrule, "2", start, null, null, timeZone);
	}

	public static List<DateTime> GetEventRepeatDates(string repeatFlag, DateTime calStart, DateTime checkStart, DateTime checkEnd, string timezone, bool isAllDay, bool preloadRange = true)
	{
		List<string> exDate = new List<string>();
		return RepeatUtils.GetValidRepeatDates(repeatFlag, "0", calStart, checkStart, checkEnd, exDate, timezone, !isAllDay, 0, preloadRange);
	}

	public static List<DateTime> GetRepeatFlagDate(TaskBaseViewModel model, DateTime startTime, DateTime endTime, bool preloadRange = true)
	{
		List<DateTime> result = new List<DateTime>();
		if (model.StartDate.HasValue && !string.IsNullOrEmpty(model.RepeatFlag))
		{
			List<string> exDate = new List<string>();
			if (!string.IsNullOrEmpty(model.ExDates))
			{
				exDate = ExDateSerilizer.ToArray(model.ExDates).ToList();
			}
			DateTime? startDate = model.StartDate;
			bool flag = !(model.IsAllDay ?? true) && !model.IsFloating;
			startDate = ((!flag) ? startDate : new DateTime?(TimeZoneUtils.LocalToTargetTzTime(startDate.Value, model.TimeZoneName)));
			result = RepeatUtils.GetValidRepeatDates(model.RepeatFlag, model.RepeatFrom, startDate.Value, startTime, endTime, exDate, model.TimeZoneName, flag, 0, preloadRange);
		}
		return result;
	}
}
