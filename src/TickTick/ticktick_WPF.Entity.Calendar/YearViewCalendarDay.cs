using System;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Entity.Calendar;

internal class YearViewCalendarDay
{
	public enum YearViewCalendarDayStyle
	{
		None,
		Light,
		Medium,
		MediumDeep,
		Deep,
		Today,
		OtherMonth
	}

	public YearCalendarViewModel ParentVM { get; }

	public DateTime Date { get; set; }

	public bool IsCurrentMonth { get; set; }

	public bool IsToday { get; set; }

	public int Year { get; set; }

	public int Month { get; set; }

	public int Day { get; set; }

	public int Count { get; set; }

	public bool IsMouseOver { get; set; }

	public YearViewCalendarDay(YearCalendarViewModel viewModel)
	{
		ParentVM = viewModel;
	}
}
