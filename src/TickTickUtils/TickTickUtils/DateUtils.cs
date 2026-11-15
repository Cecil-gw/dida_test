using System;

namespace TickTickUtils;

public class DateUtils
{
	public static bool IsWeekends(DateTime date)
	{
		if (date.DayOfWeek != DayOfWeek.Sunday)
		{
			return date.DayOfWeek == DayOfWeek.Saturday;
		}
		return true;
	}

	public static int GetDateNum(DateTime date)
	{
		return date.Year * 10000 + date.Month * 100 + date.Day;
	}
}
