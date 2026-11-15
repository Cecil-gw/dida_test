using System;
using System.Globalization;

namespace ticktick_WPF.Util;

public class IndianCalendar : Calendar
{
	public override CalendarAlgorithmType AlgorithmType => CalendarAlgorithmType.SolarCalendar;

	public override int[] Eras => new int[1] { 1 };

	public override DateTime MinSupportedDateTime => new DateTime(1900, 1, 1);

	public override DateTime MaxSupportedDateTime => new DateTime(2999, 12, 31);

	public override int GetEra(DateTime time)
	{
		return 1;
	}

	public override int GetYear(DateTime time)
	{
		return ConvertToIndianDate(time).Year;
	}

	public override int GetMonth(DateTime time)
	{
		return ConvertToIndianDate(time).Month;
	}

	public override int GetDayOfMonth(DateTime time)
	{
		return ConvertToIndianDate(time).Day;
	}

	public override int GetDaysInMonth(int year, int month, int era)
	{
		ValidateYearMonthEra(year, month, era);
		switch (month)
		{
		case 1:
			if (!IsLeapYear(year))
			{
				return 30;
			}
			return 31;
		case 2:
		case 3:
		case 4:
		case 5:
		case 6:
			return 31;
		default:
			return 30;
		}
	}

	public override int GetMonthsInYear(int year, int era)
	{
		ValidateYearEra(year, era);
		return 12;
	}

	public override int GetDaysInYear(int year, int era)
	{
		ValidateYearEra(year, era);
		if (!IsLeapYear(year))
		{
			return 365;
		}
		return 366;
	}

	public override bool IsLeapYear(int year, int era)
	{
		ValidateYearEra(year, era);
		return new GregorianCalendar().IsLeapYear(year + 78);
	}

	public override bool IsLeapMonth(int year, int month, int era)
	{
		ValidateYearMonthEra(year, month, era);
		return false;
	}

	public override bool IsLeapDay(int year, int month, int day, int era)
	{
		ValidateYearMonthDayEra(year, month, day, era);
		if (IsLeapYear(year) && month == 1)
		{
			return day == 31;
		}
		return false;
	}

	public override DateTime ToDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, int era)
	{
		ValidateYearMonthDayEra(year, month, day, era);
		int num = year + 78;
		bool flag = IsLeapYear(year);
		int month2;
		int day2;
		switch (month)
		{
		case 1:
			if (day > (flag ? 11 : 10))
			{
				month2 = 4;
				day2 = day - (flag ? 11 : 10);
			}
			else
			{
				month2 = 3;
				day2 = day + (flag ? 20 : 21);
			}
			break;
		case 2:
			if (day > 10)
			{
				month2 = 5;
				day2 = day - 10;
			}
			else
			{
				month2 = 4;
				day2 = day + 20;
			}
			break;
		case 3:
			if (day > 10)
			{
				month2 = 6;
				day2 = day - 10;
			}
			else
			{
				month2 = 5;
				day2 = day + 21;
			}
			break;
		case 4:
			if (day > 9)
			{
				month2 = 7;
				day2 = day - 9;
			}
			else
			{
				month2 = 6;
				day2 = day + 21;
			}
			break;
		case 5:
			if (day > 9)
			{
				month2 = 8;
				day2 = day - 9;
			}
			else
			{
				month2 = 7;
				day2 = day + 22;
			}
			break;
		case 6:
			if (day > 9)
			{
				month2 = 9;
				day2 = day - 9;
			}
			else
			{
				month2 = 8;
				day2 = day + 22;
			}
			break;
		case 7:
			if (day > 8)
			{
				month2 = 10;
				day2 = day - 8;
			}
			else
			{
				month2 = 9;
				day2 = day + 22;
			}
			break;
		case 8:
			if (day > 9)
			{
				month2 = 11;
				day2 = day - 9;
			}
			else
			{
				month2 = 10;
				day2 = day + 22;
			}
			break;
		case 9:
			if (day > 9)
			{
				month2 = 12;
				day2 = day - 9;
			}
			else
			{
				month2 = 11;
				day2 = day + 21;
			}
			break;
		case 10:
			if (day > 10)
			{
				num++;
				month2 = 1;
				day2 = day - 10;
			}
			else
			{
				month2 = 12;
				day2 = day + 21;
			}
			break;
		case 11:
			if (day > 11)
			{
				num++;
				month2 = 2;
				day2 = day - 11;
			}
			else
			{
				num++;
				month2 = 1;
				day2 = day + 20;
			}
			break;
		default:
			if (day > 9)
			{
				num++;
				month2 = 3;
				day2 = day - 9;
			}
			else
			{
				num++;
				month2 = 2;
				day2 = day + 19;
			}
			break;
		}
		try
		{
			return new DateTime(num, month2, day2, hour, minute, second, millisecond);
		}
		catch (ArgumentOutOfRangeException)
		{
			throw new ArgumentOutOfRangeException(null, "Invalid Indian date.");
		}
	}

	public override DateTime AddMonths(DateTime time, int months)
	{
		(int Year, int Month, int Day) tuple = ConvertToIndianDate(time);
		int item = tuple.Year;
		int item2 = tuple.Month;
		int item3 = tuple.Day;
		int num = item2 + months;
		int num2 = item + (num - 1) / 12;
		int num3 = (num - 1) % 12 + 1;
		if (num3 < 1)
		{
			num2--;
			num3 += 12;
		}
		int daysInMonth = GetDaysInMonth(num2, num3, 1);
		int day = Math.Min(item3, daysInMonth);
		return ToDateTime(num2, num3, day, time.Hour, time.Minute, time.Second, time.Millisecond, 1);
	}

	public override DateTime AddYears(DateTime time, int years)
	{
		(int Year, int Month, int Day) tuple = ConvertToIndianDate(time);
		int item = tuple.Year;
		int item2 = tuple.Month;
		int item3 = tuple.Day;
		int year = item + years;
		int daysInMonth = GetDaysInMonth(year, item2, 1);
		int day = Math.Min(item3, daysInMonth);
		return ToDateTime(year, item2, day, time.Hour, time.Minute, time.Second, time.Millisecond, 1);
	}

	private (int Year, int Month, int Day) ConvertToIndianDate(DateTime date)
	{
		GregorianCalendar gregorianCalendar = new GregorianCalendar();
		int year = gregorianCalendar.GetYear(date);
		int month = gregorianCalendar.GetMonth(date);
		int dayOfMonth = gregorianCalendar.GetDayOfMonth(date);
		bool flag = gregorianCalendar.IsLeapYear(year);
		int num = (flag ? 21 : 22);
		int num2 = year - 78;
		int item;
		int item2;
		if (month == 12 && dayOfMonth >= 22)
		{
			item = 10;
			item2 = dayOfMonth - 21;
		}
		else if (month == 12)
		{
			item = 9;
			item2 = dayOfMonth + 9;
		}
		else if (month == 11 && dayOfMonth >= 22)
		{
			item = 9;
			item2 = dayOfMonth - 21;
		}
		else if (month == 11)
		{
			item = 8;
			item2 = dayOfMonth + 9;
		}
		else if (month == 10 && dayOfMonth >= 23)
		{
			item = 8;
			item2 = dayOfMonth - 22;
		}
		else if (month == 10)
		{
			item = 7;
			item2 = dayOfMonth + 8;
		}
		else if (month == 9 && dayOfMonth >= 23)
		{
			item = 7;
			item2 = dayOfMonth - 22;
		}
		else if (month == 9)
		{
			item = 6;
			item2 = dayOfMonth + 9;
		}
		else if (month == 8 && dayOfMonth >= 23)
		{
			item = 6;
			item2 = dayOfMonth - 22;
		}
		else if (month == 8)
		{
			item = 5;
			item2 = dayOfMonth + 9;
		}
		else if (month == 7 && dayOfMonth >= 23)
		{
			item = 5;
			item2 = dayOfMonth - 22;
		}
		else if (month == 7)
		{
			item = 4;
			item2 = dayOfMonth + 9;
		}
		else if (month == 6 && dayOfMonth >= 22)
		{
			item = 4;
			item2 = dayOfMonth - 21;
		}
		else if (month == 6)
		{
			item = 3;
			item2 = dayOfMonth + 10;
		}
		else if (month == 5 && dayOfMonth >= 22)
		{
			item = 3;
			item2 = dayOfMonth - 21;
		}
		else if (month == 5)
		{
			item = 2;
			item2 = dayOfMonth + 10;
		}
		else if (month == 4 && dayOfMonth >= 21)
		{
			item = 2;
			item2 = dayOfMonth - 20;
		}
		else if (month == 4)
		{
			item = 1;
			item2 = dayOfMonth + (flag ? 11 : 10);
		}
		else if (month == 3 && dayOfMonth >= num)
		{
			item = 1;
			item2 = dayOfMonth - num + 1;
		}
		else if (month == 3)
		{
			num2--;
			item = 12;
			item2 = dayOfMonth + (flag ? 10 : 9);
		}
		else if (month == 2 && dayOfMonth >= 20)
		{
			num2--;
			item = 12;
			item2 = dayOfMonth - 19;
		}
		else if (month == 2)
		{
			num2--;
			item = 11;
			item2 = dayOfMonth + 11;
		}
		else if (month == 1 && dayOfMonth >= 21)
		{
			num2--;
			item = 11;
			item2 = dayOfMonth - 20;
		}
		else
		{
			num2--;
			item = 10;
			item2 = dayOfMonth + 10;
		}
		return (Year: num2, Month: item, Day: item2);
	}

	private void ValidateYearEra(int year, int era)
	{
		if (era != 0 && era != 1)
		{
			throw new ArgumentOutOfRangeException("era", "Invalid era. Era must be 0 or 1.");
		}
		int year2 = GetYear(MinSupportedDateTime);
		int year3 = GetYear(MaxSupportedDateTime);
		if (year < year2 || year > year3)
		{
			throw new ArgumentOutOfRangeException("year");
		}
	}

	private void ValidateYearMonthEra(int year, int month, int era)
	{
		ValidateYearEra(year, era);
		if (month < 1 || month > 12)
		{
			throw new ArgumentOutOfRangeException("month");
		}
	}

	private void ValidateYearMonthDayEra(int year, int month, int day, int era)
	{
		ValidateYearMonthEra(year, month, era);
		int daysInMonth = GetDaysInMonth(year, month, era);
		if (day < 1 || day > daysInMonth)
		{
			throw new ArgumentOutOfRangeException("day");
		}
	}

	public override DayOfWeek GetDayOfWeek(DateTime time)
	{
		return time.DayOfWeek;
	}

	public override int GetHour(DateTime time)
	{
		return time.Hour;
	}

	public override int GetMinute(DateTime time)
	{
		return time.Minute;
	}

	public override int GetSecond(DateTime time)
	{
		return time.Second;
	}

	public override double GetMilliseconds(DateTime time)
	{
		return time.Millisecond;
	}

	public override int GetDayOfYear(DateTime time)
	{
		(int, int, int) tuple = ConvertToIndianDate(time);
		int num = 0;
		for (int i = 1; i < tuple.Item2; i++)
		{
			num += GetDaysInMonth(tuple.Item1, i, 1);
		}
		return num + tuple.Item3;
	}

	public override int GetWeekOfYear(DateTime time, CalendarWeekRule rule, DayOfWeek firstDayOfWeek)
	{
		return new GregorianCalendar().GetWeekOfYear(time, rule, firstDayOfWeek);
	}
}
