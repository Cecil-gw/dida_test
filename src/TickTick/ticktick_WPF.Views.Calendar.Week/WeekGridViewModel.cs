using System;
using System.Collections.Generic;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Calendar.Month;

namespace ticktick_WPF.Views.Calendar.Week;

public class WeekGridViewModel : BaseViewModel
{
	public DateTime StartDate { get; set; }

	public DateTime EndDate { get; set; }

	public bool ShowWeekend { get; set; }

	public List<MonthDayViewModel> Models { get; private set; }

	public static DateTime GetDisplayStartDate(DateTime pivotDate)
	{
		return Utils.GetWeekStart(pivotDate);
	}

	public static WeekGridViewModel Build(DateTime date, int days, bool showWeekend = true)
	{
		DateTime startDate = date;
		List<HolidayModel> list = ((LocalSettings.Settings.EnableHoliday && Utils.IsDida()) ? HolidayManager.GetCacheHolidays() : new List<HolidayModel>());
		int month = startDate.AddDays(3.0).Month;
		List<MonthDayViewModel> list2 = new List<MonthDayViewModel>();
		DateTime current = startDate.AddDays(-1.0);
		for (int i = 0; i < days + 2; i++)
		{
			bool isWeekend = current.Date.DayOfWeek == DayOfWeek.Saturday || current.Date.DayOfWeek == DayOfWeek.Sunday;
			if (!showWeekend)
			{
				while (DateUtils.IsWeekEnds(current))
				{
					current = current.AddDays((i != 0) ? 1 : (-1));
				}
			}
			bool isWorkDay = list.Exists((HolidayModel day) => day.date == current.Date && day.type == 1) && SettingsHelper.GetShowHoliday();
			bool isRestDay = list.Exists((HolidayModel day) => day.date == current.Date && day.type == 0) && SettingsHelper.GetShowHoliday();
			list2.Add(new MonthDayViewModel
			{
				Date = current,
				IsToday = (current.Date == DateTime.Today),
				IsCurrentMonth = (month == current.Month),
				IsWeekend = isWeekend,
				IsWorkDay = isWorkDay,
				IsRestDay = isRestDay
			});
			current = current.AddDays(1.0);
		}
		return new WeekGridViewModel
		{
			StartDate = startDate,
			EndDate = startDate.AddDays(days - 1 + ((!showWeekend) ? 2 : 0)),
			Models = list2,
			ShowWeekend = showWeekend
		};
	}
}
