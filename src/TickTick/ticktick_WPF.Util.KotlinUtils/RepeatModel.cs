using System;
using System.Collections.Generic;
using System.Linq;

namespace ticktick_WPF.Util.KotlinUtils;

public class RepeatModel
{
	public List<(DateTime, DateTime)> Ranges = new List<(DateTime, DateTime)>();

	public HashSet<DateTime> RepeatDates = new HashSet<DateTime>();

	public void AddRange(DateTime start, DateTime end, List<DateTime> dates)
	{
		List<(DateTime, DateTime)> list = Ranges.ToList();
		list.Add((start, end));
		list.Sort(((DateTime, DateTime) a, (DateTime, DateTime) b) => (a.Item1 != b.Item1) ? a.Item1.CompareTo(b.Item1) : b.Item2.CompareTo(a.Item2));
		List<(DateTime, DateTime)> list2 = new List<(DateTime, DateTime)>();
		(DateTime, DateTime) item = list[0];
		foreach (var item2 in list)
		{
			if (item2.Item1 <= item.Item2 && item2.Item2 > item.Item2)
			{
				item.Item2 = item2.Item2;
			}
			else if (item2.Item1 > item.Item2)
			{
				list2.Add(item);
				item = item2;
			}
		}
		list2.Add(item);
		Ranges = list2;
		foreach (DateTime date in dates)
		{
			RepeatDates.Add(date);
		}
	}

	public (DateTime, DateTime) GetNotExistRange(DateTime start, DateTime end)
	{
		(DateTime, DateTime) tuple = (start, end);
		foreach (var range in Ranges)
		{
			if (range.Item1 <= tuple.Item1 && range.Item2 >= tuple.Item2)
			{
				(tuple.Item2, _) = tuple;
			}
			else if (range.Item1 <= tuple.Item1 && range.Item2 <= tuple.Item2 && range.Item2 >= tuple.Item1)
			{
				tuple.Item1 = range.Item2;
			}
			else if (range.Item1 >= tuple.Item1 && tuple.Item2 >= range.Item1 && range.Item2 >= tuple.Item2)
			{
				(tuple.Item2, _) = range;
			}
		}
		return tuple;
	}

	public List<DateTime> GetRepeatDates(DateTime spanStart, DateTime spanEnd)
	{
		return (from d in RepeatDates.ToList()
			where d >= spanStart && d <= spanEnd
			select d).ToList();
	}
}
