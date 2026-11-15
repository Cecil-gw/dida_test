using System;
using System.Collections.ObjectModel;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Entity.Calendar;

internal class YearViewMonthData
{
	public YearCalendarViewModel ParentVM { get; }

	public string MonthName { get; set; }

	public int Year { get; set; }

	public int Month { get; set; }

	public DateTime MonthStart { get; set; }

	public ObservableCollection<int> Weeks { get; set; } = new ObservableCollection<int>();

	public ObservableCollection<string> DayOfWeeks { get; set; } = new ObservableCollection<string>();

	public ObservableCollection<YearViewCalendarDay> Days { get; set; } = new ObservableCollection<YearViewCalendarDay>();

	public YearViewMonthData(YearCalendarViewModel parentVm)
	{
		ParentVM = parentVm;
	}
}
