using System;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Habit;

public class DayCheckInColumnInfo
{
	public bool IsToday { get; set; }

	public string DayText { get; set; }

	public double Height { get; set; }

	public DateTime Date { get; set; }

	public string Hint { get; set; }

	public double Opacity { get; set; }

	public DayCheckInColumnInfo(DateTime date, double height, double value, double goal, string unit)
	{
		Hint = DateUtils.FormatMonthDay(date, DateFormatLen.Long) + ", " + DateUtils.FormatWeekDayName(date) + "\r\n" + value + " " + HabitUtils.GetUnitText(unit);
		Date = date;
		DayText = date.Day.ToString();
		Height = height;
		Opacity = ((value >= goal) ? 1f : 0.36f);
		IsToday = date.Date == DateTime.Today;
	}
}
