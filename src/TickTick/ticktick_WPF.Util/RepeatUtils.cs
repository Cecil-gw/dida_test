using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Newtonsoft.Json;
using TickTickUtils;
using TickTickUtils.Lang;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util.KotlinUtils;
using ticktick_WPF.Util.Sync;

namespace ticktick_WPF.Util;

public static class RepeatUtils
{
	public const string OfficialWorkingDaysFlag = "RRULE:FREQ=DAILY;INTERVAL=1;TT_SKIP=HOLIDAY,WEEKEND";

	public const string WorkingDaysFlag = "RRULE:FREQ=WEEKLY;INTERVAL=1;BYDAY=WE,MO,TU,TH,FR";

	public static RecurrencePattern WorkingDaysRrule = new RecurrencePattern("RRULE:FREQ=WEEKLY;INTERVAL=1;BYDAY=WE,MO,TU,TH,FR");

	public const string OfficialHolidaysFlag = "RRULE:FREQ=DAILY;INTERVAL=1;TT_SKIP=OFFICIAL_WORKDAY";

	public const string WeekendFlag = "RRULE:FREQ=WEEKLY;INTERVAL=1;BYDAY=SA,SU";

	public static RecurrencePattern WeekendRrule = new RecurrencePattern("RRULE:FREQ=WEEKLY;INTERVAL=1;BYDAY=SA,SU");

	public const string MonthlyFirstWorkdayFlag = "TT_WORKDAY=1";

	public const string MonthlyLastWorkdayFlag = "TT_WORKDAY=-1";

	public static bool IsStandardRRule(string repeatFlag)
	{
		if (string.IsNullOrWhiteSpace(repeatFlag))
		{
			return false;
		}
		string text = RecurrenceModel.Normalize(repeatFlag.Trim());
		int num = text.IndexOf(':');
		if (num >= 0 && !string.Equals(text.Substring(0, num), "RRULE", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		if (text.IndexOf("FORGETTINGCURVE", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("NAME=CUSTOM", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("TT_SKIP", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("TT_WORKDAY", StringComparison.OrdinalIgnoreCase) >= 0 || CalendarConverter.GetRepeatFlagType(text) != CalendarConvertType.Unknown)
		{
			return false;
		}
		CultureInfo currentCulture = Thread.CurrentThread.CurrentCulture;
		try
		{
			Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
			return new RecurrencePattern(text).Frequency != FrequencyType.None;
		}
		catch
		{
			return false;
		}
		finally
		{
			Thread.CurrentThread.CurrentCulture = currentCulture;
		}
	}

	public static List<DateTime> GetValidRepeatDates(string rrule, string repeatFrom, DateTime startDate, DateTime spanStart, DateTime spanEnd, List<string> exDate = null, string timeZone = null, bool toLocalTime = true, int limit = 0, bool preloadRange = true)
	{
		try
		{
			return RepeatCalculator.GetRepeatDates(rrule, repeatFrom, startDate, spanStart, spanEnd, exDate, timeZone, toLocalTime, limit, includeToday: false, ignoreLunarIfNotLunar: false, preloadRange);
		}
		catch (Exception)
		{
		}
		List<HolidayModel> holidays = (Utils.IsDida() ? HolidayManager.GetCacheHolidays() : new List<HolidayModel>());
		exDate = exDate?.Select((string text) => text.Replace("\"", "")).ToList();
		List<DateTime> list = new List<DateTime>();
		if (repeatFrom == "1")
		{
			return list;
		}
		if (Utils.IsEmptyRepeatFlag(rrule))
		{
			return list;
		}
		if (rrule.ToUpper().Contains("FORGETTINGCURVE"))
		{
			return GetValidEbbinghausRepeatDates(rrule.ToUpper(), startDate, spanStart, spanEnd, exDate, timeZone);
		}
		if (IsMonthlyWorkday(rrule))
		{
			return GetValidWorkingDays(rrule, spanStart, spanEnd, startDate);
		}
		int num = -1;
		RecurrencePattern recurrenceModel;
		try
		{
			recurrenceModel = RecurrenceModel.GetRecurrenceModel(rrule);
			if (recurrenceModel.Count > 0)
			{
				num = recurrenceModel.Count;
				recurrenceModel.Count = int.MinValue;
			}
			switch (recurrenceModel.Frequency)
			{
			case FrequencyType.Weekly:
				if (recurrenceModel.ByDay.Count == 0)
				{
					spanStart = spanStart.AddDays(-7.0);
					recurrenceModel.ByDay.Add(new WeekDay(startDate.DayOfWeek));
				}
				break;
			case FrequencyType.Monthly:
			{
				if (recurrenceModel.ByMonthDay.Count == 0 && recurrenceModel.ByDay.Count == 0)
				{
					spanStart = spanStart.AddMonths(-1);
					recurrenceModel.ByMonthDay.Add(startDate.Day);
				}
				if (recurrenceModel.ByMonthDay.Count != 1 || (recurrenceModel.ByMonthDay[0] != 29 && recurrenceModel.ByMonthDay[0] != 30 && recurrenceModel.ByMonthDay[0] != 31))
				{
					break;
				}
				List<DateTime> list2 = GetCreditCardDate(spanStart, spanEnd, startDate, recurrenceModel, num, rrule, holidays);
				if (repeatFrom == "2")
				{
					list2 = list2.Where((DateTime date) => date >= DateTime.Today).ToList();
				}
				return list2;
			}
			case FrequencyType.Yearly:
				if (recurrenceModel.ByMonthDay.Count == 0 && recurrenceModel.ByMonth.Count == 0)
				{
					recurrenceModel.ByMonth.Add(startDate.Month);
					recurrenceModel.ByMonthDay.Add(startDate.Day);
				}
				break;
			}
		}
		catch (Exception ex2)
		{
			UtilLog.Error("get_repeat_dates_exception_2:" + ExceptionUtils.BuildExceptionMessage(ex2));
			return list;
		}
		DateTime? endDate = null;
		if (!Utils.IsEmptyDate(recurrenceModel.Until) && recurrenceModel.Until.Date <= spanEnd.Date)
		{
			if (!string.IsNullOrEmpty(timeZone))
			{
				endDate = TimeZoneUtils.ToLocalTime(recurrenceModel.Until.AddDays(1.0), timeZone);
			}
			else
			{
				endDate = recurrenceModel.Until.AddDays(1.0);
			}
			recurrenceModel.Until = recurrenceModel.Until.AddDays(2.0);
			spanEnd = recurrenceModel.Until;
		}
		DateTime dateTime = startDate;
		if (repeatFrom == "2" && num <= 0)
		{
			dateTime = ((dateTime < DateTime.Today) ? DateTime.Today : dateTime);
		}
		RecurringComponent recurringComponent = new RecurringComponent
		{
			Start = new CalDateTime
			{
				Value = dateTime
			}
		};
		recurringComponent.RecurrenceRules.Add(recurrenceModel);
		HashSet<Occurrence> hashSet = null;
		try
		{
			if (num > 0)
			{
				spanStart = startDate;
			}
			hashSet = recurringComponent.GetOccurrences(spanStart, spanEnd);
			hashSet.RemoveWhere((Occurrence occ) => IsOccurrenceEqualStartDate(occ, startDate));
			if (exDate != null && exDate.Any())
			{
				hashSet.RemoveWhere((Occurrence occ) => IsOccurenceInExDates(occ, exDate));
			}
			if (rrule.ToUpper() == "RRULE:FREQ=DAILY;INTERVAL=1;TT_SKIP=HOLIDAY,WEEKEND")
			{
				hashSet.RemoveWhere((Occurrence occ) => IsOccurenceNonOfficalWorkDay(occ, holidays));
			}
			else
			{
				if (rrule.ToUpper().Contains("HOLIDAY"))
				{
					hashSet.RemoveWhere((Occurrence occ) => IsOccurenceHoilday(occ, holidays));
				}
				if (rrule.ToUpper().Contains("WEEKEND"))
				{
					hashSet.RemoveWhere(IsOccurenceWeekend);
				}
			}
		}
		catch (Exception ex3)
		{
			UtilLog.Error("get_repeat_dates_exception_2:" + ExceptionUtils.BuildExceptionMessage(ex3));
		}
		if (hashSet != null && hashSet.Count > 0)
		{
			foreach (Occurrence item in hashSet)
			{
				DateTime? dateTime2;
				try
				{
					dateTime2 = new DateTime(item.Period.StartTime.Value.Year, item.Period.StartTime.Value.Month, item.Period.StartTime.Value.Day, startDate.Hour, startDate.Millisecond, startDate.Second);
				}
				catch (Exception)
				{
					dateTime2 = null;
				}
				if (dateTime2.HasValue)
				{
					list.Add(dateTime2.Value);
				}
			}
			if (num > 0)
			{
				list = list.Take(num - 1).ToList();
			}
		}
		if (endDate.HasValue)
		{
			return list?.Where(delegate(DateTime r)
			{
				DateTime? dateTime3 = endDate;
				return r < dateTime3;
			}).ToList();
		}
		return list;
	}

	internal static bool RepeatOutDate(string repeatFlag)
	{
		try
		{
			RecurrenceModel recurrenceModel = RecurrenceModel.GetRecurrenceModel(repeatFlag);
			return !Utils.IsEmptyDate(recurrenceModel.Until) && recurrenceModel.Until < DateTime.Today;
		}
		catch
		{
			return false;
		}
	}

	private static List<DateTime> GetCreditCardDate(DateTime spanStart, DateTime spanEnd, DateTime startDate, RecurrencePattern pattern, int count, string rule, List<HolidayModel> holidays)
	{
		List<DateTime> list = new List<DateTime>();
		List<DateTime> list2 = new List<DateTime>();
		if (!Utils.IsEmptyDate(pattern.Until) && pattern.Until < spanStart)
		{
			return list2;
		}
		int num = (spanEnd.Year - startDate.Year) * 12 + (spanEnd.Month - startDate.Month);
		if (count > 0)
		{
			num = Math.Min(count * pattern.Interval, num);
		}
		for (int i = 0; i <= num; i++)
		{
			DateTime date = new DateTime(startDate.Year, startDate.Month, 1).AddMonths(i + 1).AddDays(-1.0);
			if (date.Day > pattern.ByMonthDay[0])
			{
				date = new DateTime(startDate.Year, startDate.Month, pattern.ByMonthDay[0]).AddMonths(i);
			}
			if (date > startDate && (!rule.ToUpper().Contains("HOLIDAY") || !holidays.Any((HolidayModel h) => h.date == date && h.type == 0)) && (!rule.ToUpper().Contains("WEEKEND") || (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)))
			{
				list.Add(date.AddHours(startDate.Hour).AddMinutes(startDate.Minute));
			}
		}
		for (int num2 = 0; num2 < list.Count; num2++)
		{
			if ((Utils.IsEmptyDate(pattern.Until) || !(list[num2] > pattern.Until)) && (count <= 0 || ((num2 + 1) % pattern.Interval == 0 && (num2 + 1) / pattern.Interval <= count - 1)) && list[num2] >= spanStart && list[num2] <= spanEnd && (num2 + 1) % pattern.Interval == 0)
			{
				list2.Add(list[num2]);
			}
		}
		return list2;
	}

	private static List<DateTime> GetValidEbbinghausRepeatDates(string rrule, DateTime startDate, DateTime spanStart, DateTime spanEnd, List<string> exDate, string timeZone = null)
	{
		List<DateTime> list = new List<DateTime>();
		if (spanStart < startDate)
		{
			spanStart = startDate;
		}
		if (spanEnd < startDate)
		{
			return list;
		}
		if (exDate == null)
		{
			exDate = new List<string>();
		}
		if (rrule.Contains("CYCLE"))
		{
			string[] source = rrule.Split(';');
			if (int.TryParse(source.FirstOrDefault((string r) => r.Contains("CYCLE"))?.Replace("CYCLE=", ""), out var result))
			{
				int num = 10000;
				string text = source.FirstOrDefault((string r) => r.Contains("COUNT"));
				if (text != null && int.TryParse(text?.Replace("COUNT=", ""), out var result2))
				{
					num = result2;
					spanStart = startDate;
				}
				string text2 = source.FirstOrDefault((string r) => r.Contains("UNTIL"));
				if (text2 != null)
				{
					DateTime date = default(DateTime);
					try
					{
						date = DateTime.ParseExact(text2?.Replace("UNTIL=", ""), "yyyyMMdd", App.Ci);
					}
					catch (Exception)
					{
					}
					if (!Utils.IsEmptyDate(date))
					{
						date = date.AddDays(1.0);
						if (!string.IsNullOrEmpty(timeZone))
						{
							date = TimeZoneUtils.ToLocalTime(date, timeZone);
						}
						spanEnd = spanEnd.AddDays(1.0);
						spanEnd = ((spanEnd > date) ? date : spanEnd);
					}
				}
				DateTime dateTime = startDate;
				switch (result)
				{
				case 0:
					dateTime = startDate.AddDays(0.0);
					break;
				case 1:
					dateTime = startDate.AddDays(-1.0);
					break;
				case 2:
					dateTime = startDate.AddDays(-2.0);
					break;
				case 3:
					dateTime = startDate.AddDays(-4.0);
					break;
				case 4:
					dateTime = startDate.AddDays(-7.0);
					break;
				}
				int num2 = 1;
				double totalDays = (spanStart - dateTime).TotalDays;
				DateTime dateTime2 = dateTime;
				if (totalDays < 15.0)
				{
					for (int num3 = result; num3 < 5; num3++)
					{
						switch (num3)
						{
						case 0:
							dateTime2 = dateTime.AddDays(0.0);
							break;
						case 1:
							dateTime2 = dateTime.AddDays(1.0);
							break;
						case 2:
							dateTime2 = dateTime.AddDays(2.0);
							break;
						case 3:
							dateTime2 = dateTime.AddDays(4.0);
							break;
						case 4:
							dateTime2 = dateTime.AddDays(7.0);
							break;
						}
						if (dateTime2 < spanEnd && num2 < num)
						{
							if (dateTime2 >= spanStart && !exDate.Contains(dateTime2.ToString("yyyyMMdd", App.Ci)) && dateTime2 != startDate)
							{
								list.Add(dateTime2);
								num2++;
							}
							continue;
						}
						return list;
					}
				}
				dateTime2 = dateTime.AddDays((int)(totalDays / 15.0) * 15);
				while (dateTime2 < spanEnd && num2 < num)
				{
					if (dateTime2 >= spanStart && !exDate.Contains(dateTime2.ToString("yyyyMMdd", App.Ci)) && dateTime2 != startDate)
					{
						list.Add(dateTime2);
						num2++;
					}
					dateTime2 = dateTime2.AddDays(15.0);
				}
			}
		}
		return list;
	}

	private static List<DateTime> GetValidWorkingDays(string rrule, DateTime spanStart, DateTime spanEnd, DateTime startDate)
	{
		try
		{
			RecurrenceModel pattern = RecurrenceModel.GetRecurrenceModel(rrule);
			if (!Utils.IsEmptyDate(spanStart) && !Utils.IsEmptyDate(startDate) && spanStart < startDate)
			{
				spanStart = startDate;
			}
			List<DateTime> repeatDates = (rrule.Contains("TT_WORKDAY=1") ? GetValidFirstWorkingDays(spanStart, spanEnd, pattern.Interval) : GetValidLastWorkingDays(spanStart, spanEnd, pattern.Interval));
			if (pattern.Count >= 1)
			{
				repeatDates = repeatDates.Where((DateTime date) => repeatDates.IndexOf(date) < pattern.Count - 1).ToList();
			}
			if (!Utils.IsEmptyDate(pattern.Until))
			{
				repeatDates = repeatDates.Where((DateTime date) => date < pattern.Until.Date.AddDays(1.0)).ToList();
			}
			return repeatDates;
		}
		catch (Exception)
		{
			return new List<DateTime>();
		}
	}

	private static List<DateTime> GetValidFirstWorkingDays(DateTime startDate, DateTime spanEnd, int interval)
	{
		List<DateTime> list = new List<DateTime>();
		spanEnd = (Utils.IsEmptyDate(spanEnd) ? startDate.AddMonths(interval + 1) : spanEnd);
		int num = (spanEnd.Year - startDate.Year) * 12 + (spanEnd.Month - startDate.Month);
		for (int i = 0; i < num + 1; i += interval)
		{
			DateTime monthlyFirstWorkday = GetMonthlyFirstWorkday(startDate.AddDays(1 - startDate.Day).AddMonths(i));
			if (monthlyFirstWorkday <= spanEnd && monthlyFirstWorkday.Date > startDate.Date)
			{
				monthlyFirstWorkday = monthlyFirstWorkday.AddHours(startDate.Hour).AddMinutes(startDate.Minute);
				list.Add(monthlyFirstWorkday);
			}
		}
		return list;
	}

	private static List<DateTime> GetValidLastWorkingDays(DateTime startDate, DateTime spanEnd, int interval)
	{
		List<DateTime> list = new List<DateTime>();
		spanEnd = (Utils.IsEmptyDate(spanEnd) ? startDate.AddMonths(interval + 1) : spanEnd);
		int num = (spanEnd.Year - startDate.Year) * 12 + (spanEnd.Month - startDate.Month);
		for (int i = 0; i < num + 1; i += interval)
		{
			DateTime monthlyLastWorkday = GetMonthlyLastWorkday(startDate.AddDays(1 - startDate.Day).AddMonths(i));
			if (monthlyLastWorkday <= spanEnd && monthlyLastWorkday > startDate)
			{
				monthlyLastWorkday = monthlyLastWorkday.AddHours(startDate.Hour).AddMinutes(startDate.Minute);
				list.Add(monthlyLastWorkday);
			}
		}
		return list;
	}

	public static bool IsMonthlyWorkday(string flag)
	{
		if (!string.IsNullOrEmpty(flag))
		{
			if (!flag.Contains("TT_WORKDAY=1"))
			{
				return flag.Contains("TT_WORKDAY=-1");
			}
			return true;
		}
		return false;
	}

	private static bool IsOccurenceNonOfficalWorkDay(Occurrence occ, IEnumerable<HolidayModel> holidays)
	{
		DateTime startDate = occ.Period.StartTime.Date;
		List<HolidayModel> source = holidays.ToList();
		if (source.FirstOrDefault((HolidayModel day) => day.date == startDate.Date && day.type == 0) != null)
		{
			return true;
		}
		if (source.FirstOrDefault((HolidayModel day) => day.date == startDate.Date && day.type == 1) != null || (startDate.DayOfWeek >= DayOfWeek.Monday && startDate.DayOfWeek <= DayOfWeek.Friday))
		{
			return false;
		}
		return true;
	}

	private static bool IsOccurenceHoilday(Occurrence occ, IEnumerable<HolidayModel> holidays)
	{
		DateTime startDate = occ.Period.StartTime.Date;
		return holidays.FirstOrDefault((HolidayModel day) => day.date == startDate.Date && day.type == 0) != null;
	}

	private static bool IsOccurenceWeekend(Occurrence occ)
	{
		if (occ.Period.StartTime.DayOfWeek != DayOfWeek.Saturday)
		{
			return occ.Period.StartTime.DayOfWeek == DayOfWeek.Sunday;
		}
		return true;
	}

	private static bool IsOccurrenceEqualStartDate(Occurrence occ, DateTime startDate)
	{
		if (occ.Period.StartTime.Year == startDate.Date.Year && occ.Period.StartTime.Month == startDate.Date.Month)
		{
			return occ.Period.StartTime.Day == startDate.Date.Day;
		}
		return false;
	}

	private static RecurrencePattern GetRecurrencePattern(string flag, int count = -1)
	{
		RecurrenceModel recurrenceModel = RecurrenceModel.GetRecurrenceModel(flag);
		recurrenceModel.Until = default(DateTime);
		recurrenceModel.Count = ((count > 0) ? count : int.MinValue);
		return recurrenceModel;
	}

	public static string GetRepeatFlag(string flag, DateTime until, int count = -1)
	{
		bool flag2 = flag.ToUpper().Contains("HOLIDAY");
		bool flag3 = flag.ToUpper().Contains("WEEKEND");
		bool flag4 = flag.ToUpper().Contains("TT_WORKDAY");
		CalendarConvertType repeatFlagType = CalendarConverter.GetRepeatFlagType(flag);
		bool flag5 = flag.ToUpper().Contains("OFFICIAL_WORKDAY");
		string text = string.Empty;
		if (!Utils.IsEmptyDate(until))
		{
			text = ";UNTIL=" + until.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
		}
		if (repeatFlagType != CalendarConvertType.Unknown)
		{
			return CalendarConverter.GetRepeatKey(repeatFlagType) + ":" + RecurrenceModel.Normalize(GetRecurrencePattern(flag, count)) + text;
		}
		if (flag.ToUpper().Contains("FORGETTINGCURVE"))
		{
			List<string> list = flag.Split(';').ToList();
			list.Remove("");
			string text2 = list.FirstOrDefault((string r) => r.Contains("UNTIL"));
			if (text2 != null)
			{
				list.Remove(text2);
			}
			string text3 = list.FirstOrDefault((string r) => r.Contains("COUNT"));
			if (text3 != null)
			{
				list.Remove(text3);
			}
			if (count > 0)
			{
				list.Add("COUNT=" + count);
			}
			return string.Join(";", list) + text;
		}
		if (flag4)
		{
			string text4 = "RRULE:" + RecurrenceModel.Normalize(GetRecurrencePattern(flag, count));
			if (!text4.ToUpper().Contains("INTERVAL"))
			{
				text4 += ";INTERVAL=1";
			}
			string[] array = flag.Split(';');
			foreach (string text5 in array)
			{
				if (text5.Contains("TT_WORKDAY"))
				{
					text4 = text4 + ";" + text5;
				}
			}
			return text4 + text;
		}
		if (flag3 || flag2 || flag5)
		{
			string text6 = string.Empty;
			if (flag3 && flag2)
			{
				text6 = ";TT_SKIP=HOLIDAY,WEEKEND";
			}
			if (flag3 && !flag2)
			{
				text6 = ";TT_SKIP=WEEKEND";
			}
			if (!flag3)
			{
				text6 = ";TT_SKIP=HOLIDAY";
			}
			if (flag5)
			{
				text6 = ";TT_SKIP=OFFICIAL_WORKDAY";
			}
			return "RRULE:" + RecurrenceModel.Normalize(GetRecurrencePattern(flag, count)) + text6 + text;
		}
		return "RRULE:" + RecurrenceModel.Normalize(GetRecurrencePattern(flag, count)) + text;
	}

	public static DateTime? GetRepeatStartDate(DateTime? repeatStartDate)
	{
		if (!Utils.IsEmptyDate(repeatStartDate))
		{
			if (repeatStartDate.HasValue)
			{
				repeatStartDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, repeatStartDate.Value.Hour, repeatStartDate.Value.Minute, 0);
			}
		}
		else
		{
			repeatStartDate = DateTime.Now;
		}
		return repeatStartDate;
	}

	public static DateTime GetNextRepeatDate(TaskModel task, ref int addForgotCycle)
	{
		List<string> list = new List<string>();
		if (!string.IsNullOrEmpty(task.exDates))
		{
			list = ExDateSerilizer.ToArray(task.exDates).ToList();
		}
		string text = (((!task.isAllDay.HasValue || !task.isAllDay.Value) && !task.Floating && task.timeZone != TimeZoneData.LocalTimeZoneModel?.TimeZoneName) ? task.timeZone : null);
		DateTime? date = task.startDate;
		if (date.HasValue)
		{
			date = TimeZoneUtils.LocalToTargetTzTime(date.Value, text);
		}
		DateTime dateTime = RRule2NextDateTime(task.repeatFlag, HolidayManager.GetCacheHolidays(), date, (task.repeatFrom == "1") ? new DateTime?(DateTime.Now) : ((DateTime?)null), task.repeatFrom, list, text);
		if (date.HasValue)
		{
			int result;
			List<int> source = list.Select((string ex) => int.TryParse(ex, out result) ? result : 0).ToList();
			int startDateNum = DateUtils.GetDateNum(date.Value);
			int nextDateNum = DateUtils.GetDateNum(dateTime);
			addForgotCycle += source.Count((int num) => num > startDateNum && num < nextDateNum && num != 0);
		}
		return dateTime;
	}

	public static DateTime RRule2NextDateTime(string rrule, List<HolidayModel> holidays, DateTime? date = null, DateTime? completeDate = null, string repeatFrom = "", List<string> exDate = null, string timeZone = null)
	{
		try
		{
			DateTime repeatNextDate = RepeatCalculator.GetRepeatNextDate(rrule, repeatFrom, date ?? DateTime.Now, completeDate, exDate, timeZone, onlyFuture: true);
			if (Utils.IsEmptyDate(repeatNextDate) && IsValidRepeatRule(rrule))
			{
				string text = "NextRepeatDate is Empty " + rrule + "," + repeatFrom + "," + Utils.GetSafeDateLogText(date) + "," + Utils.GetSafeDateLogText(completeDate) + "," + repeatFrom + "," + ((exDate == null) ? "" : JsonConvert.SerializeObject(exDate)) + "," + timeZone;
				UtilLog.Info("RRule2NextDateTime  " + text);
			}
			return repeatNextDate;
		}
		catch (Exception)
		{
		}
		try
		{
			exDate = exDate?.Select((string text2) => text2.Replace("\"", "")).ToList();
			if (rrule.Contains("FORGETTINGCURVE"))
			{
				try
				{
					return GetNextEbbinghausRepeatDate(rrule, date, exDate, timeZone);
				}
				catch (Exception)
				{
					return default(DateTime);
				}
			}
			if (IsMonthlyWorkday(rrule))
			{
				DateTime startDate = date ?? DateTime.Now;
				return GetValidWorkingDays(rrule, startDate, default(DateTime), default(DateTime)).FirstOrDefault((DateTime repeatDate) => repeatDate >= startDate);
			}
			if (rrule == "RRULE:FREQ=DAILY;INTERVAL=1;TT_SKIP=HOLIDAY,WEEKEND")
			{
				return GetNextValidWorkingDay(date, holidays);
			}
			if (rrule.Contains("FREQ=NONE") || rrule.Contains("FREQ = NONE"))
			{
				return default(DateTime);
			}
			RecurringComponent recurringComponent = new RecurringComponent();
			RecurrenceModel recurrenceModel = RecurrenceModel.GetRecurrenceModel(rrule);
			int count = recurrenceModel.Count;
			if (recurrenceModel.Count == 1)
			{
				return default(DateTime);
			}
			recurrenceModel.Count = int.MinValue;
			DateTime? dateTime = null;
			DateTime dateTime2 = default(DateTime);
			try
			{
				if (!Utils.IsEmptyDate(recurrenceModel.Until))
				{
					dateTime = (string.IsNullOrEmpty(timeZone) ? new DateTime?(recurrenceModel.Until.AddDays(1.0)) : new DateTime?(TimeZoneUtils.ToLocalTime(recurrenceModel.Until.AddDays(1.0), timeZone)));
					recurrenceModel.Until = recurrenceModel.Until.AddDays(2.0);
				}
				if (!Utils.IsEmptyDate(date) && date.HasValue)
				{
					switch (recurrenceModel.Frequency)
					{
					case FrequencyType.Weekly:
						if (recurrenceModel.ByDay.Count == 0)
						{
							recurrenceModel.ByDay.Add(new WeekDay(date.Value.DayOfWeek));
						}
						break;
					case FrequencyType.Monthly:
						if (recurrenceModel.ByMonthDay.Count == 0 && recurrenceModel.ByDay.Count == 0)
						{
							recurrenceModel.ByMonthDay.Add(date.Value.Day);
						}
						if (recurrenceModel.ByMonthDay.Count != 1 || (recurrenceModel.ByMonthDay[0] != 29 && recurrenceModel.ByMonthDay[0] != 30 && recurrenceModel.ByMonthDay[0] != 31))
						{
							break;
						}
						if (date.Value < DateTime.Today)
						{
							date = DateTime.Today;
						}
						dateTime2 = GetCreditCardDate(date.Value, date.Value.AddMonths(2), date.Value, recurrenceModel, 3, rrule, holidays).First();
						if (dateTime.HasValue)
						{
							DateTime value = dateTime2;
							DateTime? dateTime3 = dateTime;
							if (value > dateTime3)
							{
								dateTime2 = default(DateTime);
							}
						}
						return dateTime2;
					case FrequencyType.Yearly:
						if (recurrenceModel.ByMonthDay.Count == 0 && recurrenceModel.ByMonth.Count == 0)
						{
							recurrenceModel.ByMonth.Add(date.Value.Month);
							recurrenceModel.ByMonthDay.Add(date.Value.Day);
						}
						break;
					}
					if (recurrenceModel.Until == default(DateTime) || recurrenceModel.Until.Date > date.Value.Date)
					{
						recurringComponent.RecurrenceRules.Add(recurrenceModel);
						DateTime value2 = date.Value.Date;
						if (repeatFrom == "2" && count <= 0)
						{
							value2 = DateTime.Today;
						}
						recurringComponent.Start = new CalDateTime
						{
							Value = value2
						};
						int num = 0;
						HashSet<Occurrence> occurrences;
						do
						{
							occurrences = recurringComponent.GetOccurrences(date.Value.AddMonths(2 * num).AddDays(0.0), date.Value.AddMonths(2 * (num + 1)).AddDays(1.0));
							if (rrule.ToUpper().Contains("HOLIDAY"))
							{
								occurrences.RemoveWhere((Occurrence occ) => IsOccurenceHoilday(occ, holidays));
							}
							if (exDate != null && exDate.Any())
							{
								occurrences.RemoveWhere((Occurrence occ) => IsOccurenceInExDates(occ, exDate));
							}
							if (rrule.ToUpper().Contains("WEEKEND"))
							{
								occurrences.RemoveWhere(IsOccurenceWeekend);
							}
							num++;
						}
						while ((occurrences.Count == 0 && num < 30) || (occurrences.Count == 1 && occurrences.ToList()[0].Period.StartTime.Value.Date == date.Value.Date));
						if (occurrences.Count != 0)
						{
							foreach (Occurrence item in occurrences)
							{
								if (!(item.Period.StartTime.Value.Date != date.Value.Date))
								{
									continue;
								}
								dateTime2 = new DateTime(item.Period.StartTime.Value.Year, item.Period.StartTime.Value.Month, item.Period.StartTime.Value.Day, date.Value.Hour, date.Value.Minute, date.Value.Second);
								if (dateTime.HasValue)
								{
									DateTime value = dateTime2;
									DateTime? dateTime3 = dateTime;
									if (value >= dateTime3)
									{
										dateTime2 = default(DateTime);
									}
								}
								return dateTime2;
							}
						}
					}
				}
				else
				{
					switch (recurrenceModel.Frequency)
					{
					case FrequencyType.Weekly:
						if (recurrenceModel.ByDay.Count == 0)
						{
							recurrenceModel.ByDay.Add(new WeekDay(DateTime.Now.DayOfWeek));
						}
						break;
					case FrequencyType.Monthly:
						if (recurrenceModel.ByMonthDay.Count == 0)
						{
							recurrenceModel.ByMonthDay.Add(DateTime.Now.Day);
						}
						break;
					case FrequencyType.Yearly:
						if (recurrenceModel.ByMonthDay.Count == 0 && recurrenceModel.ByMonth.Count == 0)
						{
							recurrenceModel.ByMonth.Add(DateTime.Now.Month);
							recurrenceModel.ByMonthDay.Add(DateTime.Now.Day);
						}
						break;
					}
					if (recurrenceModel.Until == default(DateTime) || recurrenceModel.Until.Date >= DateTime.Now.Date)
					{
						recurringComponent.RecurrenceRules.Add(recurrenceModel);
						recurringComponent.Start = new CalDateTime();
						int num2 = 0;
						HashSet<Occurrence> occurrences2;
						do
						{
							occurrences2 = recurringComponent.GetOccurrences(DateTime.Now.AddMonths(num2), DateTime.Now.AddMonths(num2 + 1));
							num2++;
						}
						while (occurrences2.Count == 0 && num2 < 60);
						if (occurrences2.Count != 0)
						{
							using HashSet<Occurrence>.Enumerator enumerator = occurrences2.GetEnumerator();
							if (enumerator.MoveNext())
							{
								dateTime2 = enumerator.Current.Period.StartTime.Value;
								if (dateTime.HasValue)
								{
									DateTime value = dateTime2;
									DateTime? dateTime3 = dateTime;
									if (value >= dateTime3)
									{
										dateTime2 = default(DateTime);
									}
								}
								return dateTime2;
							}
						}
					}
				}
			}
			catch (Exception)
			{
			}
			return default(DateTime);
		}
		catch (Exception)
		{
			return default(DateTime);
		}
	}

	private static bool IsValidRepeatRule(string rrule)
	{
		if (Utils.IsEmptyRepeatFlag(rrule) || rrule.Contains("Count=1"))
		{
			return false;
		}
		if (rrule.Contains("Until") && !RepeatOutDate(rrule))
		{
			return false;
		}
		return true;
	}

	private static DateTime GetNextEbbinghausRepeatDate(string rrule, DateTime? date, List<string> exDate, string timeZone = null)
	{
		string[] source = rrule.Split(';');
		if (exDate == null)
		{
			exDate = new List<string>();
		}
		DateTime startDate = date ?? DateTime.Today;
		if (int.TryParse(source.FirstOrDefault((string r) => r.Contains("CYCLE"))?.Replace("CYCLE=", ""), out var result))
		{
			DateTime nextEbbinghausDate = GetNextEbbinghausDate(startDate, result);
			while (exDate.Contains(nextEbbinghausDate.ToString("yyyyMMdd")))
			{
				result++;
				nextEbbinghausDate = GetNextEbbinghausDate(nextEbbinghausDate, result);
			}
			if (rrule.Contains("COUNT") && int.TryParse(source.FirstOrDefault((string r) => r.Contains("COUNT"))?.Replace("COUNT=", ""), out var result2))
			{
				if (result2 <= 1)
				{
					return default(DateTime);
				}
				return nextEbbinghausDate;
			}
			if (rrule.Contains("UNTIL"))
			{
				string text = source.FirstOrDefault((string r) => r.Contains("UNTIL"));
				try
				{
					DateTime date2 = DateTime.ParseExact(text?.Replace("UNTIL=", ""), "yyyyMMdd", App.Ci);
					if (!Utils.IsEmptyDate(date2))
					{
						date2 = date2.AddDays(1.0);
						if (!string.IsNullOrEmpty(timeZone))
						{
							date2 = TimeZoneUtils.ToLocalTime(date2, timeZone);
						}
						if (date2 < nextEbbinghausDate)
						{
							return default(DateTime);
						}
					}
				}
				catch (Exception)
				{
				}
			}
			return nextEbbinghausDate;
		}
		return default(DateTime);
	}

	private static DateTime GetNextEbbinghausDate(DateTime startDate, int cycle)
	{
		return cycle switch
		{
			0 => startDate.AddDays(1.0), 
			1 => startDate.AddDays(1.0), 
			2 => startDate.AddDays(2.0), 
			3 => startDate.AddDays(3.0), 
			4 => startDate.AddDays(8.0), 
			_ => startDate.AddDays(15.0), 
		};
	}

	private static bool IsOccurenceInExDates(Occurrence occ, ICollection<string> exDate)
	{
		string item = occ.Period.StartTime.Date.ToString("yyyyMMdd");
		if (exDate.Contains(item))
		{
			return true;
		}
		return false;
	}

	private static DateTime GetMonthlyFirstWorkday(DateTime? date)
	{
		if (!date.HasValue)
		{
			date = DateTime.Today;
		}
		return GetFirstWorkdayInMonth(date.Value.Year, date.Value.Month);
	}

	private static DateTime GetFirstWorkdayInMonth(int year, int month)
	{
		DateTime dateTime = new DateTime(year, month, 1);
		for (int i = 0; i < 31; i++)
		{
			DateTime dateTime2 = dateTime.AddDays(i);
			if (HolidayManager.IsWorkDay(dateTime2))
			{
				return dateTime2;
			}
		}
		return default(DateTime);
	}

	private static DateTime GetMonthlyLastWorkday(DateTime? date)
	{
		if (!date.HasValue)
		{
			date = DateTime.Today;
		}
		return GetLastWorkdayInMonth(date.Value.Year, date.Value.Month);
	}

	private static DateTime GetLastWorkdayInMonth(int year, int month)
	{
		DateTime dateTime = new DateTime(year, month, 1).AddMonths(1).AddDays(-1.0);
		for (int i = 0; i < 31; i++)
		{
			DateTime dateTime2 = dateTime.AddDays(i * -1);
			if (HolidayManager.IsWorkDay(dateTime2))
			{
				return dateTime2;
			}
		}
		return default(DateTime);
	}

	private static DateTime GetNextValidWorkingDay(DateTime? date, List<HolidayModel> holidays)
	{
		DateTime dateTime = date ?? DateTime.Today;
		if (dateTime < DateTime.Today)
		{
			dateTime = DateTime.Today.AddDays(-1.0);
		}
		if (date.HasValue)
		{
			dateTime = DateUtils.SetHourAndMinuteOnly(dateTime, date.Value.Hour, date.Value.Minute);
		}
		for (int i = 1; i < 20; i++)
		{
			DateTime dateTime2 = dateTime.AddDays(i);
			if (IsWorkingDay(dateTime2, holidays))
			{
				return dateTime2;
			}
		}
		return dateTime;
	}

	private static bool IsWorkingDay(DateTime checkDay, List<HolidayModel> holidays)
	{
		HolidayModel holidayModel = holidays.FirstOrDefault((HolidayModel day) => day.date == checkDay.Date && day.type == 1);
		HolidayModel holidayModel2 = holidays.FirstOrDefault((HolidayModel day) => day.date == checkDay.Date && day.type == 0);
		if (holidayModel != null)
		{
			return true;
		}
		if (holidayModel2 != null)
		{
			return false;
		}
		if (checkDay.DayOfWeek != DayOfWeek.Saturday && checkDay.DayOfWeek != DayOfWeek.Sunday)
		{
			return true;
		}
		return false;
	}

	public static string GetSubCalendarRepeatFlag(CalendarConvertType calendarType, bool isMonthly, DateTime date)
	{
		CalendarDisplay calendarDisplay = new CalendarDisplay(date, calendarType);
		if (calendarDisplay.OutLimit)
		{
			return null;
		}
		string repeatKey = CalendarConverter.GetRepeatKey(calendarType);
		if (string.IsNullOrEmpty(repeatKey))
		{
			return null;
		}
		if (isMonthly)
		{
			return $"{repeatKey}:FREQ=MONTHLY;INTERVAL=1;BYMONTHDAY={calendarDisplay.Day}";
		}
		int num = calendarDisplay.Month;
		int leapMonth = calendarDisplay.LeapMonth;
		if (leapMonth > 0 && num >= leapMonth)
		{
			num--;
		}
		return $"{repeatKey}:FREQ=YEARLY;INTERVAL=1;BYMONTH={num};BYMONTHDAY={calendarDisplay.Day}";
	}

	public static string SetSubCalendarRepeatStartDate(CalendarConvertType calendarType, string rawFlag, DateTime date)
	{
		CalendarDisplay calendarDisplay = new CalendarDisplay(date, calendarType);
		RecurrenceModel recurrenceModel = RecurrenceModel.GetRecurrenceModel(rawFlag);
		string repeatKey = CalendarConverter.GetRepeatKey(calendarType);
		if (string.IsNullOrEmpty(repeatKey))
		{
			return rawFlag;
		}
		if (recurrenceModel.Frequency == FrequencyType.Monthly)
		{
			recurrenceModel.ByMonthDay = new List<int> { calendarDisplay.Day };
		}
		else if (recurrenceModel.Frequency == FrequencyType.Yearly)
		{
			int num = calendarDisplay.Month;
			int leapMonth = calendarDisplay.LeapMonth;
			if (leapMonth > 0 && num >= leapMonth)
			{
				num--;
			}
			recurrenceModel.ByMonth = new List<int> { num };
			recurrenceModel.ByMonthDay = new List<int> { calendarDisplay.Day };
		}
		string text = repeatKey + ":" + RecurrenceModel.Normalize(recurrenceModel);
		if (recurrenceModel.Interval == 1 && !text.Contains("INTERVAL"))
		{
			text += ";INTERVAL=1";
		}
		return text;
	}

	public static string GetEbbinghausRepeatFlag()
	{
		return "ERULE:NAME=FORGETTINGCURVE;CYCLE=0";
	}

	public static int GetRepeatCount(string erule)
	{
		if (erule.Contains("COUNT") && int.TryParse(erule.Split(';').FirstOrDefault((string r) => r.Contains("COUNT"))?.Replace("COUNT=", ""), out var result))
		{
			return result;
		}
		return -1;
	}

	public static string GetNextEbbinghausCycle(string erule, int count = 1)
	{
		if (erule.Contains("FORGETTINGCURVE"))
		{
			List<string> list = erule.Split(';').ToList();
			string text = list.FirstOrDefault((string r) => r.Contains("CYCLE"));
			if (text != null && int.TryParse(text.Replace("CYCLE=", ""), out var result))
			{
				list.Remove("");
				return erule.Replace(text, "CYCLE=" + (result + count));
			}
		}
		return erule;
	}

	public static string RemoveRulePart(string ruleString, params string[] keysToRemove)
	{
		if (string.IsNullOrEmpty(ruleString))
		{
			return ruleString;
		}
		int num = ruleString.IndexOf(':');
		string text = ((num >= 0) ? ruleString.Substring(0, num + 1) : "");
		string text2 = ((num >= 0) ? ruleString.Substring(num + 1) : ruleString);
		if (keysToRemove == null || keysToRemove.Length == 0)
		{
			return ruleString;
		}
		string[] source = text2.Split(new char[1] { ';' }, StringSplitOptions.RemoveEmptyEntries);
		string[] keysWithEquals = (from key in keysToRemove
			where !string.IsNullOrEmpty(key)
			select key.Trim() + "=").ToArray();
		List<string> list = source.Where((string part) => !keysWithEquals.Any((string key) => part.Trim().StartsWith(key, StringComparison.OrdinalIgnoreCase))).ToList();
		if (list.Count == 0)
		{
			return text;
		}
		return text + string.Join(";", list);
	}

	public static bool IsNonRepeatTask(DateTime? startDate, bool? isAllDay, string rFlag, string rFrom, string timeZone, bool isFloating, List<string> exDates)
	{
		if (!startDate.HasValue || string.IsNullOrEmpty(rFlag))
		{
			return true;
		}
		timeZone = (((!isAllDay.HasValue || !isAllDay.Value) && !isFloating && timeZone != TimeZoneData.LocalTimeZoneModel.TimeZoneName) ? timeZone : null);
		startDate = TimeZoneUtils.LocalToTargetTzTime(startDate.Value, timeZone);
		return Utils.IsEmptyDate(RRule2NextDateTime(rFlag, new List<HolidayModel>(), startDate, (rFrom == "1") ? new DateTime?(DateTime.Now) : ((DateTime?)null), rFrom, exDates, timeZone));
	}

	public static RepeatFromType GetRepeatType(string repeatFrom, string repeatFlag)
	{
		if (!string.IsNullOrEmpty(repeatFlag) && repeatFlag.StartsWith("ERULE") && repeatFlag.Contains("NAME=CUSTOM"))
		{
			return RepeatFromType.Custom;
		}
		if (repeatFrom == "1")
		{
			return RepeatFromType.CompleteTime;
		}
		return RepeatFromType.Duedate;
	}

	public static bool CheckIfCountdownCustom(string repeatFlag, DateTime date, bool ignoreInterval = false)
	{
		if (repeatFlag == null)
		{
			return false;
		}
		if (repeatFlag.Contains("TT_SKIP"))
		{
			return true;
		}
		if (repeatFlag.Contains("TT_WORKDAY"))
		{
			return true;
		}
		bool flag = repeatFlag.Contains("RRULE:");
		RecurrenceModel recurrenceModel = RecurrenceModel.GetRecurrenceModel(repeatFlag);
		if (!ignoreInterval && recurrenceModel.Interval != 1)
		{
			return true;
		}
		switch (recurrenceModel.Frequency)
		{
		case FrequencyType.Weekly:
		{
			if (recurrenceModel.ByDay.Count > 1)
			{
				return true;
			}
			DayOfWeek dayOfWeek = date.DayOfWeek;
			if (recurrenceModel.ByDay.Any((WeekDay x) => x.DayOfWeek != dayOfWeek))
			{
				return true;
			}
			break;
		}
		case FrequencyType.Monthly:
			if (recurrenceModel.ByDay.Any())
			{
				return true;
			}
			if (recurrenceModel.ByMonthDay.Count > 1)
			{
				return true;
			}
			if (recurrenceModel.ByMonthDay.Any((int x) => x < 0))
			{
				return true;
			}
			if (flag && recurrenceModel.ByMonthDay.Any((int x) => x != date.Day))
			{
				return true;
			}
			break;
		case FrequencyType.Yearly:
			if (recurrenceModel.ByDay.Any())
			{
				return true;
			}
			if (recurrenceModel.ByMonthDay.Count > 1)
			{
				return true;
			}
			if (recurrenceModel.ByMonthDay.Any((int x) => x < 0))
			{
				return true;
			}
			if (flag && (recurrenceModel.ByMonth.Any((int x) => x != date.Month) || recurrenceModel.ByMonthDay.Any((int x) => x != date.Day)))
			{
				return true;
			}
			break;
		}
		return false;
	}
}
