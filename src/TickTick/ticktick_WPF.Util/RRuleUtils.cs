using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using Ical.Net;
using Ical.Net.DataTypes;
using ticktick_WPF.Resource;
using ticktick_WPF.Util.KotlinUtils;

namespace ticktick_WPF.Util;

public class RRuleUtils
{
	private readonly string _repeatFrom;

	private readonly string _rrule;

	private readonly DateTime _startDate;

	private RecurrencePattern _pattern;

	private RRuleUtils(string rrule, string repeatFrom, DateTime? startDate)
	{
		_rrule = rrule;
		_repeatFrom = repeatFrom;
		if (!Utils.IsEmptyDate(startDate) && startDate.HasValue)
		{
			_startDate = startDate.Value;
		}
	}

	public static string RRule2String(string repeatFrom, string repeatFlag, DateTime? startDate, bool showUntil = true, bool showDesc = false)
	{
		string repeatFrom2 = (string.IsNullOrEmpty(repeatFrom) ? "0" : repeatFrom);
		return new RRuleUtils(string.IsNullOrEmpty(repeatFlag) ? string.Empty : repeatFlag, repeatFrom2, startDate).RRule2String(startDate, showUntil, showDesc);
	}

	private string RRule2String(DateTime? startDate, bool showUntil = true, bool showDesc = false)
	{
		RecurrencePattern recurrenceModel;
		try
		{
			recurrenceModel = RecurrenceModel.GetRecurrenceModel(_rrule);
		}
		catch (Exception)
		{
			return string.Empty;
		}
		string text = "";
		CalendarConvertType repeatFlagType = CalendarConverter.GetRepeatFlagType(_rrule);
		if (_rrule.Contains("FORGETTINGCURVE"))
		{
			text = Utils.GetString("Ebbinghaus");
			if (showUntil || showDesc)
			{
				string text2 = _rrule.Split(';').FirstOrDefault((string r) => r.Contains("CYCLE"));
				if (text2 != null && int.TryParse(text2.Replace("CYCLE=", ""), out var result))
				{
					result++;
					text = text + ", " + string.Format(Utils.GetString("NTimes"), Utils.GetNthString(result));
				}
			}
		}
		else if (_rrule.Contains("TT_SKIP=OFFICIAL_WORKDAY"))
		{
			text = Utils.GetString("official_holidays");
		}
		else if (repeatFlagType != CalendarConvertType.Unknown)
		{
			string text3 = string.Empty;
			switch (repeatFlagType)
			{
			case CalendarConvertType.Hijri:
			case CalendarConvertType.HijriSaudi:
			case CalendarConvertType.HijriKuwaiti:
				text3 = "Hijri";
				break;
			case CalendarConvertType.Lunar:
				text3 = "ChineseLunar";
				break;
			case CalendarConvertType.Korean:
				text3 = "KoreanLunar";
				break;
			case CalendarConvertType.Vietnamese:
				text3 = "VietnameseLunar";
				break;
			case CalendarConvertType.Persian:
				text3 = "Persian";
				break;
			case CalendarConvertType.Hebcal:
				text3 = "Hebrew";
				break;
			case CalendarConvertType.Shaka:
				text3 = "Indian";
				break;
			}
			if (_rrule.Contains("MONTHLY"))
			{
				text = ((Utils.IsTickPackage() || repeatFlagType != CalendarConvertType.Lunar) ? Utils.GetString(text3 + "Monthly") : Utils.GetString("LunarMonthly"));
				if (startDate.HasValue)
				{
					CalendarDisplay calendarDisplay = new CalendarDisplay(RepeatCalculator.GetCurrentOrNextRepeatDate(_rrule, startDate.Value), repeatFlagType);
					if (!calendarDisplay.OutLimit)
					{
						text = text + " (" + calendarDisplay.GetRepeatDateText(isMonthly: true) + ")";
					}
				}
			}
			else
			{
				text = ((Utils.IsTickPackage() || repeatFlagType != CalendarConvertType.Lunar) ? Utils.GetString(text3 + "Yearly") : Utils.GetString("EveryLunarYear"));
				if (startDate.HasValue)
				{
					CalendarDisplay calendarDisplay2 = new CalendarDisplay(RepeatCalculator.GetCurrentOrNextRepeatDate(_rrule, startDate.Value), repeatFlagType);
					if (!calendarDisplay2.OutLimit)
					{
						text = text + " (" + calendarDisplay2.GetRepeatDateText(isMonthly: false) + ")";
					}
				}
			}
		}
		else
		{
			if (_rrule.Contains("TT_WORKDAY=1"))
			{
				if (recurrenceModel.Interval == 1)
				{
					return Utils.GetString("MonthlyFirstWorkday");
				}
				return string.Format(Utils.GetString("EveryMonthFirstWorkday"), recurrenceModel.Interval);
			}
			if (_rrule.Contains("TT_WORKDAY=-1"))
			{
				if (recurrenceModel.Interval == 1)
				{
					return Utils.GetString("MonthlyLastWorkday");
				}
				return string.Format(Utils.GetString("EveryMonthLastWorkday"), recurrenceModel.Interval);
			}
			_pattern = RecurrenceModel.GetRecurrenceModel(_rrule);
			switch (_pattern.Frequency)
			{
			case FrequencyType.Daily:
				text = GetDailyRRule();
				break;
			case FrequencyType.Weekly:
				text = GetWeeklyRRule(showDesc ? startDate : ((DateTime?)null));
				break;
			case FrequencyType.Monthly:
				text = GetMonthlyRRule(showDesc ? startDate : ((DateTime?)null));
				break;
			case FrequencyType.Yearly:
				text = GetYearlyyRRule(showDesc ? startDate : ((DateTime?)null));
				break;
			}
			if (_pattern.Frequency == FrequencyType.None && _rrule.Contains("RRULE:FREQ=DAILY"))
			{
				text = GetDailyRRule();
			}
		}
		if (showUntil)
		{
			string untilText = GetUntilText(recurrenceModel, _rrule);
			if (!string.IsNullOrEmpty(untilText))
			{
				text = text + ", " + untilText;
			}
		}
		return text;
	}

	public static string GetUntilText(RecurrencePattern pattern, string erule, bool toLower = true)
	{
		string text = "";
		if (erule != null && erule.Contains("FORGETTINGCURVE"))
		{
			string[] source = erule.Split(';');
			if (int.TryParse(source.FirstOrDefault((string r) => r.Contains("COUNT"))?.Replace("COUNT=", ""), out var result))
			{
				return string.Format(Utils.GetString("InCountTimes").ToLower(), result);
			}
			if (DateTime.TryParseExact(source.FirstOrDefault((string r) => r.Contains("UNTIL"))?.Replace("UNTIL=", ""), "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var result2))
			{
				return string.Format(Utils.GetString("Until"), DateUtils.FormatYearMonthDay(result2, DateFormatLen.Short));
			}
		}
		if (pattern.Count >= 1)
		{
			return string.Format(Utils.GetString("InCountTimes").ToLower(), pattern.Count);
		}
		if (!Utils.IsEmptyDate(pattern.Until))
		{
			text += string.Format(toLower ? Utils.GetString("Until").ToLower() : Utils.GetString("Until"), DateUtils.FormatYearMonthDay(pattern.Until, DateFormatLen.Short));
		}
		return text;
	}

	private string GetDailyRRule()
	{
		if (_repeatFrom == "1")
		{
			return GetQuantityString(_pattern.Interval, "RepeatFromCompleteTimeDay", "RepeatFromCompleteTimeDays");
		}
		if (_pattern.Interval > 1)
		{
			return string.Format(Utils.GetString("DescriptionDailySetRepeatMore"), _pattern.Interval);
		}
		return string.Format(Utils.GetString("EveryDay"));
	}

	private string GetWeeklyRRule(DateTime? date = null)
	{
		if (_repeatFrom == "1")
		{
			return GetQuantityString(_pattern.Interval, "RepeatFromCompleteTimeWeek", "RepeatFromCompleteTimeWeeks");
		}
		int offset = (int)(LocalSettings.Settings.WeekStartFromType + 7) % 7;
		List<WeekDay> list = _pattern.ByDay.OrderBy((WeekDay x) => (int)(x.DayOfWeek - offset + 7) % 7).ToList();
		if (_pattern.Interval > 1)
		{
			return string.Format(Utils.GetString("DescriptionWeekdaysSetRepeatMore"), GetWeekdaysText(list), _pattern.Interval);
		}
		if (list.Count == 0)
		{
			return Utils.GetString("EveryWeek") + ((!date.HasValue || Utils.IsEmptyDate(date.Value)) ? "" : (" (" + date.Value.ToString("ddd", App.Ci) + ")"));
		}
		return string.Format(Utils.GetString("DescriptionWeekdaysSetRepeatOne"), GetWeekdaysText(list));
	}

	private string GetMonthlyRRule(DateTime? date = null)
	{
		if (_repeatFrom == "1")
		{
			return GetQuantityString(_pattern.Interval, "RepeatFromCompleteTimeMonth", "RepeatFromCompleteTimeMonths");
		}
		List<WeekDay> list = _pattern.ByDay.OrderBy((WeekDay p) => p.DayOfWeek).ToList();
		List<int> list2 = _pattern.ByMonthDay.OrderBy((int p) => p).ToList();
		if (_pattern.Interval > 1 && list2.Count == 0 && !Utils.IsEmptyDate(_startDate))
		{
			list2.Add(_startDate.Day);
		}
		if (list.Count != 0)
		{
			string weekOnDayString = GetWeekOnDayString(list);
			if (string.IsNullOrEmpty(weekOnDayString))
			{
				if (_pattern.Interval > 1)
				{
					return string.Format(Utils.GetString("DescriptionMonthDaySetRepeatMore"), "", _pattern.Interval);
				}
				return string.Format(Utils.GetString("DescriptionMonthDaySetRepeatOne"), "");
			}
			if (_pattern.Interval > 1)
			{
				return string.Format(Utils.GetString("DescriptionMonthWeekSetRepeatMore"), weekOnDayString, _pattern.Interval);
			}
			return string.Format(Utils.GetString("DescriptionMonthWeekSetRepeatOne"), weekOnDayString);
		}
		if (list2.Count > 0)
		{
			string weekOnDayString = GetMonthDayString(list2);
			if (string.IsNullOrWhiteSpace(weekOnDayString))
			{
				if (_pattern.Interval > 1)
				{
					return string.Format(Utils.GetString("DescriptionMonthDaySetRepeatMore"), "", _pattern.Interval);
				}
				return Utils.GetString("EveryMonth") + ((!date.HasValue || Utils.IsEmptyDate(date.Value)) ? "" : (" (" + DateUtils.FormatDay(date.Value) + ")"));
			}
			if (_pattern.Interval > 1)
			{
				return string.Format(Utils.GetString("DescriptionMonthDaySetRepeatMore"), weekOnDayString, _pattern.Interval);
			}
			return string.Format(Utils.GetString("DescriptionMonthWeekSetRepeatOne"), weekOnDayString);
		}
		return Utils.GetString("EveryMonth") + ((!date.HasValue || Utils.IsEmptyDate(date.Value)) ? "" : (" (" + DateUtils.FormatDay(date.Value) + ")"));
	}

	private string GetYearlyyRRule(DateTime? date = null)
	{
		if (_repeatFrom == "1")
		{
			return GetQuantityString(_pattern.Interval, "RepeatFromCompleteTimeYear", "RepeatFromCompleteTimeYears");
		}
		string text = ((_pattern.Interval > 1) ? string.Format(Utils.GetString("DescriptionYearlySetRepeatMore"), _pattern.Interval) : string.Format(Utils.GetString("EveryYear")));
		List<int> byMonth = _pattern.ByMonth;
		if (byMonth != null && byMonth.Count > 0)
		{
			List<WeekDay> byDay = _pattern.ByDay;
			if (byDay != null && byDay.Count > 0)
			{
				int num = _pattern.ByMonth[0];
				string weekOnDayString = GetWeekOnDayString(_pattern.ByDay.OrderBy((WeekDay p) => p.DayOfWeek).ToList());
				return string.Format(Utils.GetString("YearMonthRepeat"), text, DateTime.Today.AddMonths(num - DateTime.Today.Month).ToString("MMM", App.Ci), weekOnDayString);
			}
		}
		if (date.HasValue)
		{
			List<int> byMonth2 = _pattern.ByMonth;
			if (byMonth2 != null && byMonth2.Count > 0)
			{
				List<int> byMonthDay = _pattern.ByMonthDay;
				if (byMonthDay != null && byMonthDay.Count > 0)
				{
					int num2 = _pattern.ByMonth[0];
					int num3 = _pattern.ByMonthDay[0];
					if (num3 < 0)
					{
						return string.Format(Utils.GetString("YearMonthRepeat"), text, DateTime.Today.AddMonths(num2 - DateTime.Today.Month).ToString("MMM", App.Ci), Utils.GetString("LastDay"));
					}
					return string.Format(Utils.GetString("YearMonthDayRepeat"), text, DateUtils.FormatMonthDay(new DateTime(2024, num2, num3), DateFormatLen.Long));
				}
			}
		}
		return text + ((!date.HasValue || Utils.IsEmptyDate(date.Value)) ? "" : (" (" + DateUtils.FormatMonthDay(date.Value, DateFormatLen.Middle) + ")"));
	}

	private static string GetQuantityString(int interval, string oneString, string moreString)
	{
		return string.Format((interval > 1) ? Utils.GetString(moreString) : Utils.GetString(oneString), interval);
	}

	private static string GetWeekdaysText(List<WeekDay> weekdayNums)
	{
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < weekdayNums.Count; i++)
		{
			stringBuilder.Append(Utils.GetShortWeekName((int)weekdayNums[i].DayOfWeek));
			if (i < weekdayNums.Count - 1)
			{
				stringBuilder.Append(", ");
			}
		}
		return stringBuilder.ToString();
	}

	private static string GetWeekOnDayString(IReadOnlyList<WeekDay> byDays)
	{
		string[] array = (string[])Application.Current?.FindResource("DescriptionOrdinalLabels");
		WeekDay weekDay = byDays[0];
		int num = weekDay.Offset - 1;
		if (num < 0 && array != null)
		{
			num = array.Length - 1;
		}
		if (byDays.Count == 1)
		{
			int dayOfWeek = (int)weekDay.DayOfWeek;
			if (array != null && num >= 0 && num <= 5)
			{
				return array[num] + Utils.GetShortWeekName(dayOfWeek);
			}
		}
		return "";
	}

	private static string GetMonthDayString(IReadOnlyList<int> byMonthDay)
	{
		if (byMonthDay.Count <= 0)
		{
			return "";
		}
		string[] array = (string[])Application.Current?.FindResource("OneMonthDay");
		StringBuilder stringBuilder = new StringBuilder();
		StringBuilder stringBuilder2 = new StringBuilder();
		int i = 0;
		for (int count = byMonthDay.Count; i < count; i++)
		{
			int num = byMonthDay[i];
			if (i == 0)
			{
				if (num == -1)
				{
					if (count > 1)
					{
						stringBuilder2.Append(", " + Utils.GetString("LastDay"));
					}
					else
					{
						stringBuilder2.Append(Utils.GetString("LastDay"));
					}
				}
				else if (num >= -1 && array != null)
				{
					stringBuilder.Append(array[num - 1]);
				}
			}
			else if (num == -1)
			{
				stringBuilder2.Append(", " + Utils.GetString("LastDay"));
			}
			else
			{
				if (num < -1)
				{
					continue;
				}
				if (stringBuilder.Length == 0)
				{
					if (array != null)
					{
						stringBuilder.Append(array[num - 1]);
					}
				}
				else if (array != null)
				{
					stringBuilder.Append(", " + array[num - 1]);
				}
			}
		}
		if (!string.IsNullOrWhiteSpace(stringBuilder.ToString()) && string.IsNullOrWhiteSpace(stringBuilder2.ToString()))
		{
			return stringBuilder.ToString();
		}
		return stringBuilder.Append(stringBuilder2).ToString();
	}

	public static string GetNextCount(string flag, bool handleEbbingCount)
	{
		RecurrenceModel recurrenceModel = RecurrenceModel.GetRecurrenceModel(flag);
		int num = ((recurrenceModel.Count > 0) ? (recurrenceModel.Count - 1) : 0);
		int count = 1;
		if (flag != null && flag.Contains("FORGETTINGCURVE"))
		{
			num = RepeatUtils.GetRepeatCount(flag) - 1;
			if (handleEbbingCount)
			{
				flag = RepeatUtils.GetNextEbbinghausCycle(flag, count);
			}
		}
		if (num > 0)
		{
			flag = RepeatUtils.GetRepeatFlag(flag, default(DateTime), num);
		}
		return flag;
	}

	public static string HandleUntilText(string rrule, string timeZone)
	{
		if (rrule.Contains("UNTIL"))
		{
			string text = rrule.Split(';').FirstOrDefault((string p) => p.StartsWith("UNTIL"));
			if (text != null && text.Contains("T") && DateTime.TryParseExact(text.Replace("UNTIL=", ""), "yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
			{
				rrule = rrule.Replace(text, "UNTIL=" + TimeZoneUtils.UtcToTargetTzTime(result, timeZone).ToString("yyyyMMdd", CultureInfo.InvariantCulture));
			}
		}
		return rrule;
	}
}
