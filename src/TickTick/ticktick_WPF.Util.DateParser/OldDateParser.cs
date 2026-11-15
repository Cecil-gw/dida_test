using System;
using System.Collections.Generic;
using System.Linq;
using Ical.Net.DataTypes;

namespace ticktick_WPF.Util.DateParser;

public static class OldDateParser
{
	private static readonly string[] HalfExps = new string[3] { "半", "half a", "half an" };

	private static readonly string[] YearExps = new string[5] { "年", "year", "years", "yr", "yrs" };

	private static readonly string[] MonthExps = new string[3] { "月", "month", "months" };

	private static readonly string[] WeekExps = new string[6] { "周", "礼拜", "星期", "week", "wk", "weeks" };

	private static readonly string[] DayExps = new string[4] { "天", "day", "d", "days" };

	private static readonly string[] HourExps = new string[6] { "小时", "h", "hr", "hrs", "hour", "hours" };

	private static readonly string[] MinuteExps = new string[6] { "分钟", "分", "minute", "minutes", "min", "mins" };

	public static OldParseDueDate Parse(string content)
	{
		return Parse(content, DateTime.Now);
	}

	public static OldParseDueDate Parse(string content, DateTime baseDate)
	{
		try
		{
			bool flag = Utils.ContainsChineseInText(content);
			OldParseDueDate oldParseDueDate = (flag ? ParseChinese(content, baseDate) : ParseEnglish(content, baseDate));
			if (oldParseDueDate.RecognizeStrings.Count > 0)
			{
				return oldParseDueDate;
			}
			return flag ? ParseEnglish(content, baseDate) : ParseChinese(content, baseDate);
		}
		catch (Exception)
		{
			return new OldParseDueDate();
		}
	}

	public static DateTime GetNextDate(int month, bool hasDay, DateTime date)
	{
		if (date.Month == month && !hasDay)
		{
			return DateTime.Today;
		}
		return date.AddYears(1);
	}

	private static OldParseDueDate ParseEnglish(string content, DateTime baseDate)
	{
		return Parse(content, new EnglishParser(), baseDate);
	}

	private static OldParseDueDate ParseChinese(string content, DateTime baseDate)
	{
		return Parse(content, new ChineseParser(), baseDate);
	}

	private static OldParseDueDate Parse(string content, IParser parser, DateTime baseDate)
	{
		OldParseDueDate oldParseDueDate = new OldParseDueDate();
		DateTime? date = null;
		RecurrencePattern recurrencePattern = parser.MatchRepeat(content, ref date, baseDate, ref oldParseDueDate.RecognizeStrings);
		if (date.HasValue)
		{
			if (date.Value.Date < baseDate.Date)
			{
				date = date.Value.AddDays(1.0);
			}
			oldParseDueDate.SetDueDate(date.Value);
			if (recurrencePattern != null)
			{
				oldParseDueDate.RepeatFlag = "RRULE:" + RecurrenceModel.Normalize(recurrencePattern);
			}
		}
		if (!date.HasValue)
		{
			date = baseDate;
		}
		if (parser.MatchAfterTime(content, ref date, ref oldParseDueDate.RecognizeStrings) && date.HasValue)
		{
			oldParseDueDate.DueDate = date.Value;
			oldParseDueDate.SetReminder(TickTickDuration.OnTime);
		}
		DateTime? dateTime = parser.MatchWeekday(content, baseDate, ref oldParseDueDate.RecognizeStrings);
		if (dateTime.HasValue)
		{
			oldParseDueDate.DueDate = dateTime.Value;
		}
		DateTime? dateTime2 = parser.MatchMonth(content, baseDate, ref oldParseDueDate.RecognizeStrings);
		if (dateTime2.HasValue)
		{
			oldParseDueDate.DueDate = dateTime2.Value;
		}
		DateTime? dateTime3 = parser.MatchDate(content, baseDate, ref oldParseDueDate.RecognizeStrings);
		if (dateTime3.HasValue)
		{
			oldParseDueDate.DueDate = dateTime3.Value;
		}
		DateTime? dateTime4 = parser.MatchSpecialDay(content, baseDate, ref oldParseDueDate.RecognizeStrings);
		if (dateTime4.HasValue)
		{
			oldParseDueDate.DueDate = dateTime4.Value;
		}
		DateTime? parseDate = (Utils.IsEmptyDate(oldParseDueDate.DueDate) ? ((DateTime?)null) : new DateTime?(oldParseDueDate.DueDate));
		if (parser.MatchTime(content, ref parseDate, baseDate, ref oldParseDueDate.RecognizeStrings))
		{
			oldParseDueDate.SetReminder(TickTickDuration.OnTime);
			if (parseDate.HasValue)
			{
				oldParseDueDate.DueDate = parseDate.Value;
			}
		}
		else if (parser.MatchSpecialTime(content, ref parseDate, baseDate, ref oldParseDueDate.RecognizeStrings))
		{
			oldParseDueDate.SetReminder(TickTickDuration.OnTime);
			if (parseDate.HasValue)
			{
				oldParseDueDate.DueDate = parseDate.Value;
			}
		}
		if (!Utils.IsEmptyDate(oldParseDueDate.DueDate))
		{
			TickTickDuration duration = parser.MatchAdvanceTime(content, ref oldParseDueDate.RecognizeStrings);
			oldParseDueDate.Duration = duration;
		}
		return oldParseDueDate;
	}

	public static DateTime GetDateByDuration(DateTime date, TickTickDuration duration)
	{
		int num = (duration.IsPositive() ? 1 : (-1));
		if (duration.GetYears() != 0)
		{
			date = date.AddYears(duration.GetYears() * num);
		}
		if (duration.GetMonths() != 0)
		{
			date = date.AddMonths(duration.GetMonths() * num);
		}
		if (duration.GetDays() != 0)
		{
			date = date.AddDays(duration.GetDays() * num);
		}
		if (duration.GetWeeks() != 0)
		{
			date = date.AddDays(duration.GetWeeks() * 7 * num);
		}
		if (duration.GetHours() != 0)
		{
			date = date.AddHours(duration.GetHours() * num);
		}
		if (duration.GetMinutes() != 0)
		{
			date = date.AddMinutes(duration.GetMinutes() * num);
		}
		return date;
	}

	public static TickTickDuration GetDuration(string numStr, string timeUnit, bool isPositive = true)
	{
		int num = (IsAnOrA(numStr) ? 1 : ((!IsNull(numStr)) ? TranslateNum(numStr) : 0));
		TickTickDuration tickTickDuration = null;
		if (IsMinutesUnit(timeUnit))
		{
			if (IsHalf(numStr))
			{
				tickTickDuration = new TickTickDuration(0, 0, 0, 0, 0, 0, 30);
			}
			else if (num != 0)
			{
				tickTickDuration = new TickTickDuration(0, 0, 0, 0, 0, num, 0);
			}
		}
		else if (IsHourUnit(timeUnit))
		{
			if (IsHalf(numStr))
			{
				tickTickDuration = new TickTickDuration(0, 0, 0, 0, 0, 30, 0);
			}
			else if (num != 0)
			{
				tickTickDuration = new TickTickDuration(0, 0, 0, 0, num, 0, 0);
			}
		}
		else if (IsDayUnit(timeUnit))
		{
			if (IsHalf(numStr))
			{
				tickTickDuration = new TickTickDuration(0, 0, 0, 0, 12, 0, 0);
			}
			else if (num != 0)
			{
				tickTickDuration = new TickTickDuration(0, 0, 0, num, 0, 0, 0);
			}
		}
		else if (IsWeekUnit(timeUnit))
		{
			if (IsHalf(numStr))
			{
				tickTickDuration = new TickTickDuration(0, 0, 0, 3, 0, 0, 0);
			}
			else if (num != 0)
			{
				tickTickDuration = new TickTickDuration(0, 0, num, 0, 0, 0, 0);
			}
		}
		else if (IsMonthUnit(timeUnit))
		{
			tickTickDuration = new TickTickDuration(0, num, 0, 0, 0, 0, 0);
		}
		else if (IsYearUnit(timeUnit))
		{
			tickTickDuration = new TickTickDuration(num, 0, 0, 0, 0, 0, 0);
		}
		tickTickDuration?.SetIsPositive(isPositive);
		return tickTickDuration;
	}

	public static TickTickDuration GetDuration(int num, string timeUnit, bool isPositive)
	{
		TickTickDuration tickTickDuration = null;
		if (IsMinutesUnit(timeUnit))
		{
			tickTickDuration = new TickTickDuration(0, 0, 0, 0, 0, num, 0);
		}
		else if (IsHourUnit(timeUnit))
		{
			tickTickDuration = new TickTickDuration(0, 0, 0, 0, num, 0, 0);
		}
		else if (IsDayUnit(timeUnit))
		{
			tickTickDuration = new TickTickDuration(0, 0, 0, num, 0, 0, 0);
		}
		else if (IsWeekUnit(timeUnit))
		{
			tickTickDuration = new TickTickDuration(0, 0, num, 0, 0, 0, 0);
		}
		else if (IsMonthUnit(timeUnit))
		{
			tickTickDuration = new TickTickDuration(0, num, 0, 0, 0, 0, 0);
		}
		else if (IsYearUnit(timeUnit))
		{
			tickTickDuration = new TickTickDuration(num, 0, 0, 0, 0, 0, 0);
		}
		tickTickDuration?.SetIsPositive(isPositive);
		return tickTickDuration;
	}

	private static bool ContainsExp(IEnumerable<string> exps, string str)
	{
		return exps.Contains(str);
	}

	public static bool IsHalf(string str)
	{
		return ContainsExp(HalfExps, str);
	}

	public static bool IsYearUnit(string str)
	{
		return ContainsExp(YearExps, str);
	}

	public static bool IsMonthUnit(string str)
	{
		return ContainsExp(MonthExps, str);
	}

	public static bool IsWeekUnit(string str)
	{
		return ContainsExp(WeekExps, str);
	}

	public static bool IsDayUnit(string str)
	{
		return ContainsExp(DayExps, str);
	}

	public static bool IsHourUnit(string str)
	{
		return ContainsExp(HourExps, str);
	}

	public static bool IsMinutesUnit(string str)
	{
		return ContainsExp(MinuteExps, str);
	}

	public static int TranslateNum(string n)
	{
		if (IsNull(n))
		{
			return 0;
		}
		try
		{
			return int.Parse(n);
		}
		catch (Exception)
		{
			return MatchNum(n);
		}
	}

	public static bool IsNull(string group)
	{
		return string.IsNullOrEmpty(group);
	}

	public static bool IsAnOrA(string str)
	{
		if (!("a" == str))
		{
			return "an" == str;
		}
		return true;
	}

	public static int MatchNum(string str)
	{
		if (int.TryParse(str, out var result))
		{
			return result;
		}
		if (str != null)
		{
			int length = str.Length;
			if (length != 1)
			{
				if (length == 2)
				{
					switch (str[0])
					{
					case '1':
						if (str == "1刻")
						{
							goto IL_0154;
						}
						goto IL_016b;
					case '一':
						if (str == "一刻")
						{
							goto IL_0154;
						}
						goto IL_016b;
					case '3':
						if (str == "3刻")
						{
							goto IL_0157;
						}
						goto IL_016b;
					case '三':
						if (str == "三刻")
						{
							goto IL_0157;
						}
						goto IL_016b;
					case '零':
						break;
					case '中':
						if (!(str == "中旬"))
						{
							goto IL_016b;
						}
						return 11;
					case '上':
						if (!(str == "上旬"))
						{
							goto IL_016b;
						}
						return 1;
					case '下':
						if (!(str == "下旬"))
						{
							goto IL_016b;
						}
						return 21;
					default:
						goto IL_016b;
						IL_0154:
						return 15;
						IL_0157:
						return 45;
					}
					if (str == "零两")
					{
						goto IL_015a;
					}
				}
			}
			else
			{
				char c = str[0];
				if ((uint)c <= 21313u)
				{
					if (c == '两')
					{
						goto IL_015a;
					}
					switch (c)
					{
					case '个':
						return 1;
					case '十':
						return 10;
					}
				}
				else
				{
					switch (c)
					{
					case '半':
						return 30;
					case '天':
					case '日':
						return 7;
					}
				}
			}
		}
		goto IL_016b;
		IL_016b:
		string[] array = new string[10] { "零", "一", "二", "三", "四", "五", "六", "七", "八", "九" };
		if (str.IndexOf("零", StringComparison.Ordinal) == 0)
		{
			if (str.Length == 1)
			{
				return 0;
			}
			return MatchNum(str.Substring(1, str.Length));
		}
		if (str.Contains("十"))
		{
			int num = str.IndexOf("十", StringComparison.Ordinal);
			string text = str.Substring(0, num);
			string str2 = string.Empty;
			if (num + 2 <= str.Length)
			{
				str2 = str.Substring(num + 1, 1);
			}
			if (text.Length == 0)
			{
				return 10 + MatchNum(str2);
			}
			return MatchNum(text) * 10 + MatchNum(str2);
		}
		for (int i = 0; i < array.Length; i++)
		{
			if (str.Contains(array[i]))
			{
				return i;
			}
		}
		return 0;
		IL_015a:
		return 2;
	}

	public static void AddMatchString(ICollection<string> recgonizeStrList, string matched)
	{
		string item = matched.Trim().Replace(",", "");
		if (!recgonizeStrList.Contains(item))
		{
			recgonizeStrList.Add(item);
		}
	}
}
