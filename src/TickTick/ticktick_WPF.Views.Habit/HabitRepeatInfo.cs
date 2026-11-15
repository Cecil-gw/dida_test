using System;
using System.Collections.Generic;

namespace ticktick_WPF.Views.Habit;

public class HabitRepeatInfo
{
	public HabitRepeatType Type { get; set; }

	public int Count { get; set; }

	public List<DayOfWeek> ByDays { get; set; }

	public int Interval { get; set; } = 1;

	public string[] ExDates { get; set; }
}
