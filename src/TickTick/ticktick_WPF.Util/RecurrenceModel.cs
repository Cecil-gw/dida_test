using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Ical.Net.DataTypes;

namespace ticktick_WPF.Util;

public class RecurrenceModel : RecurrencePattern
{
	private RecurrenceModel(string flag)
		: base(flag)
	{
	}

	public static string Normalize(RecurrencePattern pattern)
	{
		return Normalize(pattern?.ToString()) ?? string.Empty;
	}

	public static string Normalize(string flag)
	{
		return flag?.Replace('İ', 'I').Replace('ı', 'i');
	}

	public static RecurrenceModel GetRecurrenceModel(string flag, DateTime until = default(DateTime), int count = -1)
	{
		flag = Normalize(flag);
		CultureInfo currentCulture = Thread.CurrentThread.CurrentCulture;
		try
		{
			Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
			return new RecurrenceModel(flag);
		}
		catch (Exception)
		{
			string text = HandleErrorFlag(flag);
			if (text != flag)
			{
				return GetRecurrenceModel(text);
			}
		}
		finally
		{
			Thread.CurrentThread.CurrentCulture = currentCulture;
		}
		return new RecurrenceModel("");
	}

	private static string HandleErrorFlag(string flag)
	{
		if (flag != null && flag.Contains("COUNT"))
		{
			List<string> list = flag.Split(';').ToList();
			string text = list.FirstOrDefault((string r) => r.Contains("COUNT"));
			if (text != null && int.TryParse(text.Replace("COUNT=", ""), out var result) && result <= 1)
			{
				list.Remove(text);
				return string.Join(";", list);
			}
		}
		return flag;
	}

	public bool IsEquals(RecurrencePattern other)
	{
		if (other == null)
		{
			return false;
		}
		if (this == other)
		{
			return true;
		}
		if (base.Interval == other.Interval && base.RestrictionType == other.RestrictionType && base.EvaluationMode == other.EvaluationMode && base.Frequency == other.Frequency && base.Until.Equals(other.Until) && base.Count == other.Count && base.FirstDayOfWeek == other.FirstDayOfWeek && CollectionEqualsOrder(base.BySecond, other.BySecond) && CollectionEqualsOrder(base.ByMinute, other.ByMinute) && CollectionEqualsOrder(base.ByHour, other.ByHour) && CollectionEquals(base.ByDay, other.ByDay, (WeekDay x) => x.DayOfWeek) && CollectionEquals(base.ByMonthDay, other.ByMonthDay) && CollectionEquals(base.ByYearDay, other.ByYearDay) && CollectionEquals(base.ByWeekNo, other.ByWeekNo) && CollectionEquals(base.ByMonth, other.ByMonth))
		{
			return CollectionEquals(base.BySetPosition, other.BySetPosition);
		}
		return false;
	}

	private static bool CollectionEqualsOrder<T>(IEnumerable<T> c1, IEnumerable<T> c2)
	{
		if (!c1.Any() && !c2.Any())
		{
			return true;
		}
		return c1.SequenceEqual(c2);
	}

	private static bool CollectionEquals<T>(IEnumerable<T> c1, IEnumerable<T> c2)
	{
		if (!c1.Any() && !c2.Any())
		{
			return true;
		}
		return c1.OrderBy((T t) => t).SequenceEqual(c2.OrderBy((T t) => t));
	}

	private static bool CollectionEquals<T, TKey>(IEnumerable<T> c1, IEnumerable<T> c2, Func<T, TKey> order)
	{
		if (!c1.Any() && !c2.Any())
		{
			return true;
		}
		return c1.OrderBy(order).SequenceEqual(c2.OrderBy(order));
	}
}
