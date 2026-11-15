using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ticktick_WPF.Resource;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Util;

public static class FilterUtils
{
	public static (int?, int?) GetSpanPairInRule(string rule)
	{
		Match match = CommonRegex.SpanFilterRegex.Match(rule);
		if (match.Success)
		{
			int result;
			int? item = (int.TryParse(match.Groups[1].Value, out result) ? new int?(result) : ((int?)null));
			int result2;
			int? item2 = (int.TryParse(match.Groups[2].Value, out result2) ? new int?(result2) : ((int?)null));
			return (item, item2);
		}
		return (null, null);
	}

	public static string GetNthDayString(int nDay)
	{
		return nDay switch
		{
			-1 => Utils.GetString("PublicYesterday"), 
			0 => Utils.GetString("Today"), 
			1 => Utils.GetString("Tomorrow"), 
			_ => string.Format((nDay < 0) ? Utils.GetString("ThePreviousNDay") : Utils.GetString("TheNextNDay"), Utils.GetNthString(nDay)), 
		};
	}

	public static object GetSpanDateValue(int? from, int? to)
	{
		string arg = ((!from.HasValue) ? "" : from.ToString());
		string arg2 = ((!to.HasValue) ? "" : to.ToString());
		return $"span({arg}~{arg2})";
	}

	public static List<FilterDatePair> GetFilterDatePairs(IEnumerable<string> dates)
	{
		if (dates == null)
		{
			return null;
		}
		List<FilterDatePair> list = new List<FilterDatePair>();
		foreach (string date in dates)
		{
			switch (date)
			{
			case "today":
				list.Add(new FilterDatePair(DateTime.Today, DateTime.Today.AddDays(1.0)));
				continue;
			case "tomorrow":
				list.Add(new FilterDatePair(DateTime.Today.AddDays(1.0), DateTime.Today.AddDays(2.0)));
				continue;
			case "thisweek":
			{
				int nextWeekDayDiff = DateUtils.GetNextWeekDayDiff();
				list.Add(new FilterDatePair(DateTime.Today.AddDays(nextWeekDayDiff - 7), DateTime.Today.AddDays(nextWeekDayDiff)));
				continue;
			}
			case "nextweek":
			{
				int nextWeekDayDiff2 = DateUtils.GetNextWeekDayDiff();
				list.Add(new FilterDatePair(DateTime.Today.AddDays(nextWeekDayDiff2), DateTime.Today.AddDays(nextWeekDayDiff2 + 7)));
				continue;
			}
			case "thismonth":
			{
				DateTime value = DateTime.Now.Date.AddDays((DateTime.Now.Day - 1) * -1);
				DateTime value2 = value.Date.AddMonths(1);
				list.Add(new FilterDatePair(value, value2));
				continue;
			}
			case "nodue":
				list.Add(new FilterDatePair(isNoDate: true));
				continue;
			case "all":
				list.Add(new FilterDatePair(DateTime.Today, DateTime.Today.AddDays(180.0)));
				continue;
			case "overdue":
				list.Add(new FilterDatePair(null, DateTime.Today));
				continue;
			case "recurring":
				list.Add(new FilterDatePair(isNoDate: false));
				continue;
			}
			if (date.EndsWith("days"))
			{
				int.TryParse(date.Replace("days", string.Empty), out var result);
				list.Add(new FilterDatePair(DateTime.Today, DateTime.Today.AddDays(result)));
			}
			else if (date.EndsWith("dayslater"))
			{
				int.TryParse(date.Replace("dayslater", string.Empty), out var result2);
				list.Add(new FilterDatePair(DateTime.Today.AddDays(result2), null));
			}
			else if (date.EndsWith("daysfromtoday"))
			{
				int.TryParse(date.Replace("daysfromtoday", string.Empty), out var result3);
				list.Add(new FilterDatePair(DateTime.Today.AddDays(result3), DateTime.Today.AddDays(result3 + 1)));
			}
			else if (date.StartsWith("span"))
			{
				(int?, int?) spanPairInRule = GetSpanPairInRule(date);
				int? item = spanPairInRule.Item1;
				int? item2 = spanPairInRule.Item2;
				DateTime? start = ((!item.HasValue) ? ((DateTime?)null) : new DateTime?(DateTime.Today.AddDays(item.Value)));
				DateTime? end = ((!item2.HasValue) ? ((DateTime?)null) : new DateTime?(DateTime.Today.AddDays(item2.Value + 1)));
				list.Add(new FilterDatePair(start, end));
			}
			else if (date.StartsWith("offset"))
			{
				var (value3, value4) = GetDateOffsetPair(date);
				list.Add(new FilterDatePair(value3, value4));
			}
		}
		return list;
	}

	public static (DateTime, DateTime) GetDateOffsetPair(string rule)
	{
		if (rule.Length > 8)
		{
			string text = rule.Substring(7);
			text = text.Substring(0, text.Length - 1);
			if (int.TryParse(text.Substring(0, text.Length - 1), out var result))
			{
				switch (text.Substring(text.Length - 1))
				{
				case "D":
					return (DateTime.Today.AddDays(result), DateTime.Today.AddDays(result + 1));
				case "W":
				{
					DateTime weekStart = Utils.GetWeekStart(DateTime.Today);
					return (weekStart.AddDays(result), weekStart.AddDays(result + 7));
				}
				case "M":
				{
					DateTime item = DateTime.Today.AddDays(1 - DateTime.Today.Day).AddMonths(result);
					return (item, item.AddMonths(1));
				}
				}
			}
		}
		return (DateTime.Today, DateTime.Today.AddDays(1.0));
	}
}
