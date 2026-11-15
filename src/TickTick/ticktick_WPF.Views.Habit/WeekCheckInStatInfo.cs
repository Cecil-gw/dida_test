using System;

namespace ticktick_WPF.Views.Habit;

public class WeekCheckInStatInfo
{
	public bool Completed;

	public int Count;

	public DateTime WeekEnd;

	public DateTime WeekStart;

	public WeekCheckInStatInfo(DateTime start, DateTime end)
	{
		WeekStart = start;
		WeekEnd = end;
	}
}
