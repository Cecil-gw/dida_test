using System;
using System.Collections.Generic;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Calendar.Month;

public class MonthGridViewModel : BaseViewModel
{
	private bool _showLastWeek;

	public DateTime StartDate { get; set; }

	public DateTime EndDate { get; set; }

	public List<MonthDayViewModel> Models { get; private set; }

	public Dictionary<int, List<MonthDayViewModel>> ModelDict { get; private set; }

	public bool ShowLastWeek
	{
		get
		{
			return _showLastWeek;
		}
		set
		{
			_showLastWeek = value;
			OnPropertyChanged("ShowLastWeek");
		}
	}

	public static MonthGridViewModel Build(DateTime startDate, int weeks)
	{
		DateTime end = startDate.AddDays(weeks * 7 - 1);
		int month = DateUtils.GetCurrentMonthDate(startDate, end).Month;
		List<HolidayModel> list = ((LocalSettings.Settings.EnableHoliday && Utils.IsDida()) ? HolidayManager.GetCacheHolidays() : new List<HolidayModel>());
		Dictionary<int, List<MonthDayViewModel>> dictionary = new Dictionary<int, List<MonthDayViewModel>>();
		List<MonthDayViewModel> list2 = new List<MonthDayViewModel>();
		for (int i = -1; i < weeks + 1; i++)
		{
			List<MonthDayViewModel> list3 = new List<MonthDayViewModel>();
			for (int j = 0; j < 7; j++)
			{
				DateTime current = startDate.AddDays(i * 7 + j);
				bool isWorkDay = list.Exists((HolidayModel day) => day.date == current.Date && day.type == 1) && SettingsHelper.GetShowHoliday();
				bool isRestDay = list.Exists((HolidayModel day) => day.date == current.Date && day.type == 0) && SettingsHelper.GetShowHoliday();
				bool isWeekend = current.Date.DayOfWeek == DayOfWeek.Saturday || current.Date.DayOfWeek == DayOfWeek.Sunday;
				bool isCurrentMonth = current.Date.Month == month;
				MonthDayViewModel item = new MonthDayViewModel
				{
					ShowMonthday = true,
					Date = current,
					IsToday = (current.Date == DateTime.Today),
					IsWeekend = isWeekend,
					IsCurrentMonth = isCurrentMonth,
					IsWorkDay = isWorkDay,
					IsRestDay = isRestDay
				};
				list3.Add(item);
				list2.Add(item);
			}
			dictionary[i] = list3;
		}
		return new MonthGridViewModel
		{
			StartDate = startDate,
			EndDate = startDate.AddDays(weeks * 7 - 1),
			ShowLastWeek = (weeks == 6),
			ModelDict = dictionary,
			Models = list2
		};
	}

	public static DateTime GetDisplayStartDate(DateTime pivotDate)
	{
		DateTime weekStart = Utils.GetWeekStart(pivotDate.AddDays(1 - pivotDate.Day));
		if (pivotDate.Month != weekStart.Month || weekStart.Day == 1)
		{
			return weekStart;
		}
		return weekStart.AddDays(-7.0);
	}

	public DateTime GetCurrentMonthDate()
	{
		return DateUtils.GetCurrentMonthDate(StartDate, EndDate);
	}
}
