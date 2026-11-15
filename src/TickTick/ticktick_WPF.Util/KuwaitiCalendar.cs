using System;
using System.Collections.Generic;
using System.Globalization;

namespace ticktick_WPF.Util;

public class KuwaitiCalendar : Calendar
{
	private static readonly UmAlQuraCalendar _umalqura = new UmAlQuraCalendar();

	private static readonly DateTime _epoch = new DateTime(622, 7, 16);

	private static readonly Dictionary<(int, int), int> _daysInMonthCache = new Dictionary<(int, int), int>();

	public override CalendarAlgorithmType AlgorithmType => CalendarAlgorithmType.LunarCalendar;

	public override int[] Eras => new int[1] { 1 };

	public override DateTime MinSupportedDateTime => _epoch;

	public override DateTime MaxSupportedDateTime => DateTime.MaxValue;

	public static (int Year, int Month, int Day) GregorianToHijri(DateTime date)
	{
		int day = date.Day;
		int month = date.Month;
		int year = date.Year;
		int num = month;
		int num2 = year;
		if (num < 3)
		{
			num2--;
			num += 12;
		}
		int num3 = (int)Math.Floor((double)num2 / 100.0);
		int num4 = 2 - num3 + (int)Math.Floor((double)num3 / 4.0);
		if (num2 < 1583)
		{
			num4 = 0;
		}
		if (num2 == 1582)
		{
			if (num > 10)
			{
				num4 = -10;
			}
			if (num == 10)
			{
				num4 = 0;
				if (day > 4)
				{
					num4 = -10;
				}
			}
		}
		int num5 = (int)(Math.Floor(365.25 * (double)(num2 + 4716)) + Math.Floor(30.6001 * (double)(num + 1)) + (double)day + (double)num4 - 1524.0);
		num4 = 0;
		if (num5 > 2299160)
		{
			num3 = (int)Math.Floor(((double)num5 - 1867216.25) / 36524.25);
			num4 = 1 + num3 - (int)Math.Floor((double)num3 / 4.0);
		}
		int num6 = num5 + num4 + 1524;
		int num7 = (int)Math.Floor(((double)num6 - 122.1) / 365.25);
		int num8 = (int)Math.Floor(365.25 * (double)num7);
		int num9 = (int)Math.Floor((double)(num6 - num8) / 30.6001);
		Math.Floor(30.6001 * (double)num9);
		if (num9 > 13)
		{
			num7++;
		}
		int num10 = num5 - 1948084;
		int num11 = (int)Math.Floor((double)num10 / 10631.0);
		int num12 = num10 - 10631 * num11;
		int num13 = (int)Math.Floor(((double)num12 - 0.1335) / 354.3666666666667);
		int item = 30 * num11 + num13;
		int num14 = num12 - (int)Math.Floor((double)num13 * 354.3666666666667 + 0.1335);
		int num15 = (int)Math.Floor(((double)num14 + 28.5001) / 29.5);
		if (num15 == 13)
		{
			num15 = 12;
		}
		int item2 = num14 - (int)Math.Floor(29.5001 * (double)num15 - 29.0);
		return (Year: item, Month: num15, Day: item2);
	}

	public static DateTime HijriToGregorian(int year, int month, int day, int era)
	{
		DateTime dateTime;
		try
		{
			dateTime = _umalqura.ToDateTime(year, month, day, 0, 0, 0, 0, era);
		}
		catch (ArgumentOutOfRangeException)
		{
			int num = (int)((double)(year - 1) * 354.367 + (double)(month - 1) * 29.53 + (double)day - 1.0);
			dateTime = _epoch.AddDays(num);
		}
		(int, int, int) current = GregorianToHijri(dateTime);
		if (current.Item1 == year && current.Item2 == month && current.Item3 == day)
		{
			return dateTime;
		}
		int num2 = CalculateDaysToAdjust(current, (Year: year, Month: month, Day: day));
		DateTime dateTime2 = dateTime.AddDays(-num2);
		(int, int, int) tuple = GregorianToHijri(dateTime2);
		if (tuple.Item1 == year && tuple.Item2 == month && tuple.Item3 == day)
		{
			return dateTime2;
		}
		return FindExactDateByIteration(year, month, day, dateTime2);
	}

	private static int CalculateDaysToAdjust((int Year, int Month, int Day) current, (int Year, int Month, int Day) target)
	{
		if (current.Year == target.Year && current.Month == target.Month)
		{
			return current.Day - target.Day;
		}
		int num = 0;
		if (current.Year != target.Year)
		{
			int num2 = Math.Min(current.Year, target.Year);
			int num3 = Math.Max(current.Year, target.Year);
			for (int i = num2; i < num3; i++)
			{
				num += (IsLeapYearStatic(i) ? 355 : 354);
			}
			if (current.Year > target.Year)
			{
				num = -num;
			}
		}
		if (current.Month != target.Month)
		{
			bool flag = false;
			int item;
			int item2;
			if (current.Month < target.Month)
			{
				item = current.Month;
				item2 = target.Month;
			}
			else
			{
				item = target.Month;
				item2 = current.Month;
				flag = true;
			}
			for (int j = item; j < item2; j++)
			{
				num += GetDaysInMonthStatic(current.Year, j);
			}
			if (flag)
			{
				num = -num;
			}
		}
		return num + (current.Day - target.Day);
	}

	private static bool IsLeapYearStatic(int year)
	{
		int num = year % 30;
		if (num != 2 && num != 5 && num != 7 && num != 10 && num != 13 && num != 16 && num != 18 && num != 21 && num != 24 && num != 26)
		{
			return num == 29;
		}
		return true;
	}

	private static int GetDaysInMonthStatic(int year, int month)
	{
		(int, int) key = (year, month);
		if (_daysInMonthCache.TryGetValue(key, out var value))
		{
			return value;
		}
		value = ((month % 2 != 1 && (month != 12 || !IsLeapYearStatic(year))) ? 29 : 30);
		_daysInMonthCache[key] = value;
		return value;
	}

	private static DateTime FindExactDateByIteration(int year, int month, int day, DateTime startDate)
	{
		DateTime dateTime = startDate;
		int num = 60;
		int num2 = 0;
		while (num2++ < num)
		{
			(int, int, int) tuple = GregorianToHijri(dateTime);
			if (tuple.Item1 == year && tuple.Item2 == month && tuple.Item3 == day)
			{
				return dateTime;
			}
			int num3 = 0;
			num3 = ((tuple.Item1 == year) ? ((tuple.Item2 == month) ? ((tuple.Item3 < day) ? 1 : (-1)) : ((tuple.Item2 < month) ? 1 : (-1))) : ((tuple.Item1 < year) ? 1 : (-1)));
			dateTime = dateTime.AddDays(num3);
		}
		return dateTime;
	}

	public override int GetDaysInMonth(int year, int month, int era)
	{
		return GetDaysInMonthStatic(year, month);
	}

	public override int GetDaysInYear(int year, int era)
	{
		int num = 0;
		for (int i = 1; i <= 12; i++)
		{
			num += GetDaysInMonthStatic(year, i);
		}
		return num;
	}

	public override bool IsLeapYear(int year, int era)
	{
		return IsLeapYearStatic(year);
	}

	public override DateTime AddMonths(DateTime time, int months)
	{
		(int, int, int) tuple = GregorianToHijri(time);
		int num = tuple.Item2 + months;
		var (num2, _, _) = tuple;
		while (num > 12)
		{
			num -= 12;
			num2++;
		}
		while (num < 1)
		{
			num += 12;
			num2--;
		}
		int daysInMonthStatic = GetDaysInMonthStatic(num2, num);
		int day = Math.Min(tuple.Item3, daysInMonthStatic);
		return HijriToGregorian(num2, num, day, 1);
	}

	public override DateTime AddYears(DateTime time, int years)
	{
		(int, int, int) tuple = GregorianToHijri(time);
		int year = tuple.Item1 + years;
		return HijriToGregorian(day: Math.Min(val2: GetDaysInMonthStatic(year, tuple.Item2), val1: tuple.Item3), year: year, month: tuple.Item2, era: 1);
	}

	public override int GetDayOfMonth(DateTime time)
	{
		return GregorianToHijri(time).Day;
	}

	public override DayOfWeek GetDayOfWeek(DateTime time)
	{
		return time.DayOfWeek;
	}

	public override int GetDayOfYear(DateTime time)
	{
		(int, int, int) tuple = GregorianToHijri(time);
		int num = 0;
		for (int i = 1; i < tuple.Item2; i++)
		{
			num += GetDaysInMonthStatic(tuple.Item1, i);
		}
		return num + tuple.Item3;
	}

	public override int GetEra(DateTime time)
	{
		return 1;
	}

	public override int GetMonth(DateTime time)
	{
		return GregorianToHijri(time).Month;
	}

	public override int GetMonthsInYear(int year, int era)
	{
		return 12;
	}

	public override int GetYear(DateTime time)
	{
		return GregorianToHijri(time).Year;
	}

	public override bool IsLeapDay(int year, int month, int day, int era)
	{
		return false;
	}

	public override bool IsLeapMonth(int year, int month, int era)
	{
		return false;
	}

	public override DateTime ToDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, int era)
	{
		DateTime dateTime = HijriToGregorian(year, month, day, era);
		return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, hour, minute, second, millisecond);
	}
}
