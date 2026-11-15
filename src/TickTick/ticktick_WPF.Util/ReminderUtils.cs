using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Views.Time;

namespace ticktick_WPF.Util;

public static class ReminderUtils
{
	private enum Span
	{
		Year,
		Month,
		Week,
		Day,
		Hour,
		Minute
	}

	public static string GetReminderListDisplayText(ICollection<TaskReminderModel> reminderList, bool isAllday, string emptyText = "")
	{
		if (reminderList == null || reminderList.Count == 0)
		{
			if (string.IsNullOrEmpty(emptyText))
			{
				return string.Empty;
			}
			return emptyText;
		}
		return RemoveMatchingParenthesesContent(from model in (from rule in reminderList
				where rule != null && !string.IsNullOrEmpty(rule.trigger)
				select new AdvanceDateModel(rule.trigger, isAllday)).ToList()
			select model.ToString());
	}

	public static string GetReminderTriggerListDisplayText(ICollection<string> triggers, bool isAllday, string emptyText = "")
	{
		if (triggers == null || triggers.Count == 0)
		{
			if (string.IsNullOrEmpty(emptyText))
			{
				return string.Empty;
			}
			return emptyText;
		}
		return RemoveMatchingParenthesesContent(from model in (from rule in triggers
				where rule != null && !string.IsNullOrEmpty(rule)
				select new AdvanceDateModel(rule, isAllday)).ToList()
			select model.ToString());
	}

	private static string RemoveMatchingParenthesesContent(IEnumerable<string> inputs)
	{
		if (inputs == null || inputs.Count() == 0)
		{
			return string.Empty;
		}
		List<Match> list = (from input in inputs
			where input != null
			select Regex.Match(input, "\\(([^)]+)\\)") into m
			where m.Success
			select m).ToList();
		if (list.Count == 0)
		{
			return string.Join(", ", inputs);
		}
		string firstContent = list[0].Groups[1].Value;
		if (list.All((Match m) => m.Groups[1].Value == firstContent))
		{
			List<string> values = (from input in inputs
				where input != null
				select Regex.Replace(input, "\\([^)]+\\)", "") into s
				select s.TrimEnd()).ToList();
			return string.Join(", ", values);
		}
		return string.Join(", ", inputs);
	}

	public static string GetCalendarReminderText(IEnumerable<int> minutes, bool isAllDay)
	{
		return string.Join(", ", from min in minutes
			orderby min
			select GetReminderText(TriggerUtils.ReminderToTrigger(min), isAllDay));
	}

	public static string GetReminderText(string trigger, bool isAllDay)
	{
		if (!isAllDay)
		{
			return ToTimeText(trigger);
		}
		return ToAllDayText(trigger);
	}

	private static string ToTimeText(string trigger)
	{
		Match match = new Regex("(-)?P((\\d{0,})Y)?((\\d{0,})M)?((\\d{0,})W)?((\\d{0,})D)?T?((\\d{0,})H)?((\\d{0,})M)?((\\d{0,})S)?").Match(trigger);
		if (match.Success)
		{
			bool flag = trigger.Contains("RELATED=END");
			int.TryParse(match.Groups[7].ToString(), out var result);
			int.TryParse(match.Groups[9].ToString(), out var result2);
			int.TryParse(match.Groups[11].ToString(), out var result3);
			int.TryParse(match.Groups[13].ToString(), out var result4);
			if (result == 0 && result2 == 0 && result3 == 0 && result4 == 0)
			{
				return Utils.GetString(flag ? "AtTheEnd" : "OnTime");
			}
			result4 = result * 10080 + result2 * 1440 + result3 * 60 + result4;
			result = 0;
			if (result4 > 0)
			{
				result2 = result4 / 1440;
				result3 = result4 % 1440 / 60;
				result4 %= 60;
			}
			string arg = BuildUnitText(result, Span.Week) + BuildUnitText(result2, Span.Day) + BuildUnitText(result3, Span.Hour) + BuildUnitText(result4, Span.Minute);
			return string.Format(Utils.GetString("AdvanceText"), arg);
		}
		return string.Empty;
	}

	public static string ToAllDayText(string trigger, DateTime date = default(DateTime))
	{
		Match match = new Regex("(-)?P((\\d{0,})Y)?((\\d{0,})M)?((\\d{0,})W)?((\\d{0,})D)?T?((\\d{0,})H)?((\\d{0,})M)?((\\d{0,})S)?").Match(trigger);
		if (match.Success)
		{
			bool num = trigger.Contains("-");
			bool flag = trigger.Contains("RELATED=END");
			int.TryParse(match.Groups[7].ToString(), out var result);
			int.TryParse(match.Groups[9].ToString(), out var result2);
			int.TryParse(match.Groups[11].ToString(), out var result3);
			int.TryParse(match.Groups[13].ToString(), out var result4);
			int num2 = result2 + result * 7;
			if (num)
			{
				int num3 = (int)Math.Ceiling((double)(Math.Abs((long)result3) * 60 + Math.Abs((long)result4)) / 1440.0);
				num2 += num3;
				result3 *= -1;
				result4 *= -1;
			}
			result = num2 / 7;
			result2 = num2 % 7;
			DateTime dateTime = date;
			if (Utils.IsEmptyDate(date))
			{
				dateTime = DateTime.Today;
			}
			DateTime date2 = dateTime.AddHours(result3).AddMinutes(result4);
			string text = DateUtils.FormatHourMinuteText(date2);
			if (result == 0 && result2 == 0 && Utils.IsEmptyDate(date))
			{
				if (flag)
				{
					return string.Format(Utils.GetString("OnTheEndDate"), text);
				}
				return Utils.GetString("CenterToday") + "  (" + text + ")";
			}
			if (Utils.IsEmptyDate(date))
			{
				string arg = BuildUnitText(result, Span.Week) + BuildUnitText(result2, Span.Day);
				return string.Format(Utils.GetString("AdvanceText"), arg) + "  (" + text + ")";
			}
			text = " " + text;
			if (result != 0 || result2 != 0)
			{
				string arg2 = BuildUnitText(result, Span.Week) + BuildUnitText(result2, Span.Day);
				string arg3 = DateUtils.FormatYearMonthDay(date2.AddDays((result * 7 + result2) * -1 + 1), DateFormatLen.Middle);
				string arg4 = string.Format(Utils.GetString("AdvanceText"), arg2);
				return string.Format(Utils.GetString("AdvanceFullText"), arg4, text, arg3);
			}
			string arg5 = DateUtils.FormatYearMonthDay(date2, DateFormatLen.Middle);
			string arg6 = Utils.GetString("CenterToday");
			return string.Format(Utils.GetString("AdvanceFullText"), arg6, text, arg5);
		}
		return string.Empty;
	}

	private static string BuildUnitText(int num, Span unit)
	{
		if (num == 0)
		{
			return string.Empty;
		}
		string text = (Utils.IsCn() ? "" : " ");
		string text2 = (Utils.IsCn() ? " " : "");
		return unit switch
		{
			Span.Year => text2 + num + " " + Utils.GetString((num > 1) ? "PublicYears" : "PublicYear") + text, 
			Span.Month => text2 + num + " " + Utils.GetString((num > 1) ? "PublicMonths" : "PublicMonth") + text, 
			Span.Week => text2 + string.Format(Utils.GetString((num > 1) ? "XWeeks" : "XWeek"), num) + text, 
			Span.Day => text2 + string.Format(Utils.GetString((num > 1) ? "XDays" : "XDay"), num) + text, 
			Span.Hour => text2 + string.Format(Utils.GetString((num > 1) ? "XHours" : "XHour"), num) + text, 
			Span.Minute => text2 + string.Format(Utils.GetString((num > 1) ? "XMinutes" : "XMinute"), num) + text, 
			_ => string.Empty, 
		};
	}
}
