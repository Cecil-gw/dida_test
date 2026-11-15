using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using KotlinModels;
using Newtonsoft.Json;
using TickTickHandler;
using TickTickUtils.Lang;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;

namespace ticktick_WPF.Util.KotlinUtils;

public static class RepeatCalculator
{
	public static ConcurrentDictionary<string, RepeatModel> repeatCache = new ConcurrentDictionary<string, RepeatModel>();

	public static ConcurrentDictionary<string, System.DateTime> nextDateCatch = new ConcurrentDictionary<string, System.DateTime>();

	public static List<System.DateTime> GetRepeatDates(string rrule, string repeatFrom, System.DateTime startDate, System.DateTime spanStart, System.DateTime spanEnd, List<string> exDates = null, string timeZone = null, bool toLocalTime = true, int limit = 0, bool includeToday = false, bool ignoreLunarIfNotLunar = false, bool preloadRange = true)
	{
		if (repeatFrom == "1" || string.IsNullOrEmpty(rrule))
		{
			return new List<System.DateTime>();
		}
		spanStart = spanStart.Date;
		spanEnd = spanEnd.Date;
		if (IsRepeatRangeBeforeStart(startDate, spanEnd))
		{
			return new List<System.DateTime>();
		}
		if (!TTCalendarDllInitializer.CheckInitialized())
		{
			return new List<System.DateTime>();
		}
		List<HolidayModel> list = (Utils.IsDida() ? HolidayManager.GetCacheHolidays() : new List<HolidayModel>());
		string text = new StringBuilder(rrule).Append(repeatFrom).Append(startDate.ToString("yyyyMMddHHmmss", App.Ci)).Append((exDates == null) ? string.Empty : JsonConvert.SerializeObject(exDates))
			.Append(timeZone)
			.Append(list?.Count)
			.Append(includeToday)
			.ToString();
		System.DateTime spanStart2 = spanStart;
		System.DateTime spanEnd2 = spanEnd;
		if (repeatCache.TryGetValue(text, out var value))
		{
			(System.DateTime, System.DateTime) notExistRange = value.GetNotExistRange(spanStart, spanEnd);
			if (notExistRange.Item1 == notExistRange.Item2)
			{
				return value.GetRepeatDates(spanStart, spanEnd);
			}
			(spanStart, spanEnd) = notExistRange;
		}
		CalendarConvertType repeatFlagType = CalendarConverter.GetRepeatFlagType(rrule);
		bool flag = (rrule.Contains("RRULE") || repeatFlagType == CalendarConvertType.Unknown) && !rrule.Contains("TT_SKIP=OFFICIAL_WORKDAY");
		if (preloadRange && flag && spanStart > System.DateTime.Today.AddYears(-1) && spanEnd < System.DateTime.Today.AddYears(1))
		{
			spanStart = System.DateTime.Today.AddYears(-1);
			spanEnd = System.DateTime.Today.AddYears(1);
		}
		List<System.DateTime> list2 = new List<System.DateTime>();
		List<TTCalendar> list3 = new List<TTCalendar>();
		if (exDates != null)
		{
			exDates = exDates.Select((string text2) => text2.Replace("\"", "")).ToList();
			foreach (string exDate in exDates)
			{
				if (System.DateTime.TryParseExact(exDate, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
				{
					TTCalendar item = ModelsDao.NewTTCalendar(result, null);
					list2.Add(result);
					list3.Add(item);
				}
			}
		}
		List<System.DateTime> list4 = new List<System.DateTime>();
		if (!flag)
		{
			return CalculateRepeat(rrule, repeatFrom, list2, limit, timeZone, toLocalTime, startDate, spanStart, spanEnd, ignoreLunarIfNotLunar);
		}
		TTCalendar start = ModelsDao.NewTTCalendar(startDate, timeZone);
		TTCalendar spanStart3 = ModelsDao.NewTTCalendar(spanStart, timeZone);
		TTCalendar spanEnd3 = ModelsDao.NewTTCalendar(spanEnd, timeZone);
		TTCalendar completeStamp = new TTCalendar();
		timeZone = (string.IsNullOrEmpty(timeZone) ? TimeZoneData.LocalTimeZoneModel.TimeZoneName : timeZone);
		foreach (TTCalendar repeat in GetRepeats(list3, start, completeStamp, timeZone, rrule, repeatFrom, spanStart3, spanEnd3, limit, 0))
		{
			if (repeat.year <= 0)
			{
				continue;
			}
			System.DateTime dateTime = ModelsDao.TTCalendarToDateTime(repeat);
			if (!(dateTime.Date <= startDate.Date) || (includeToday && dateTime.Date == System.DateTime.Today.Date && startDate.Date == System.DateTime.Today.Date))
			{
				if (toLocalTime)
				{
					dateTime = TimeZoneUtils.ToLocalTime(dateTime, repeat.zone);
				}
				list4.Add(dateTime);
			}
		}
		RepeatModel repeatModel = new RepeatModel();
		if (repeatCache.TryGetValue(text, out var value2))
		{
			repeatModel = value2;
		}
		else if (text != null && repeatModel != null)
		{
			repeatCache[text] = repeatModel;
		}
		repeatModel.AddRange(spanStart, spanEnd, list4);
		return repeatModel.GetRepeatDates(spanStart2, spanEnd2);
	}

	internal static bool IsRepeatRangeBeforeStart(System.DateTime startDate, System.DateTime spanEnd)
	{
		return startDate.Date > spanEnd.Date;
	}

	private static List<System.DateTime> CalculateRepeat(string rrule, string repeatFrom, List<System.DateTime> exceptDateLists, int limit, string timeZone, bool toLocalTime, System.DateTime startDate, System.DateTime spanStart, System.DateTime spanEnd, bool ignoreLunarIfNotLunar = false)
	{
		if (string.IsNullOrEmpty(rrule))
		{
			return new List<System.DateTime>();
		}
		if (!rrule.Contains("RRULE") && CalendarConverter.GetRepeatFlagType(rrule) != CalendarConvertType.Unknown)
		{
			return CalculateSubCalendar(rrule, exceptDateLists, limit, timeZone, toLocalTime, startDate, spanStart, spanEnd, ignoreLunarIfNotLunar);
		}
		if (rrule.Contains("TT_SKIP=OFFICIAL_WORKDAY"))
		{
			(System.DateTime?, int?) tuple = ParseRruleUntilAndCount(rrule);
			return CalculateOfficialWorkDay(exceptDateLists, limit, timeZone, toLocalTime, startDate, spanStart, spanEnd, tuple.Item1, tuple.Item2);
		}
		return new List<System.DateTime>();
	}

	private static List<System.DateTime> CalculateOfficialWorkDay(List<System.DateTime> exceptDateLists, int limit, string timeZone, bool toLocalTime, System.DateTime startDate, System.DateTime spanStart, System.DateTime spanEnd, System.DateTime? untilDate, int? count)
	{
		startDate = startDate.AddDays(1.0);
		List<System.DateTime> list = new List<System.DateTime>();
		try
		{
			if (spanStart < startDate)
			{
				spanStart = startDate;
			}
			if (spanStart < System.DateTime.Today)
			{
				spanStart = System.DateTime.Today;
			}
			if (untilDate.HasValue && spanEnd > untilDate.Value.AddDays(-1.0))
			{
				spanEnd = untilDate.Value.AddDays(-1.0);
			}
			System.DateTime dateTime = startDate;
			if (dateTime > spanEnd)
			{
				return list;
			}
			if (dateTime < spanStart)
			{
				dateTime = spanStart;
			}
			int num = 0;
			int num2 = limit;
			if (count.HasValue && count > 0 && (num2 <= 0 || count < num2))
			{
				if (count == 1)
				{
					return new List<System.DateTime>();
				}
				num2 = count.Value - 1;
			}
			while (dateTime <= spanEnd && (num2 <= 0 || list.Count < num2) && num < 10000)
			{
				num++;
				if (CheckIsHolidayWeekend(dateTime.Date, exceptDateLists))
				{
					System.DateTime dateTime2 = dateTime.Date + startDate.TimeOfDay;
					if (toLocalTime && !string.IsNullOrEmpty(timeZone))
					{
						dateTime2 = TimeZoneUtils.ToLocalTime(dateTime2, timeZone);
					}
					list.Add(dateTime2);
				}
				dateTime = dateTime.AddDays(1.0);
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine("计算重复日期时出错: " + ex.Message);
		}
		return list;
	}

	public static int GetPassedCountInTotal(TaskModel task, System.DateTime startDate, System.DateTime targetDate, int totalCount)
	{
		if (task.repeatFlag.Contains("TT_SKIP=OFFICIAL_WORKDAY"))
		{
			return GetPassedCountInTotalHolidayWeekend(startDate, targetDate, totalCount);
		}
		return RepeatUtils.GetValidRepeatDates(task.repeatFlag, task.repeatFrom, startDate.AddDays(-1.0), startDate, targetDate, task.exDate?.ToList()).Count;
	}

	private static int GetPassedCountInTotalHolidayWeekend(System.DateTime startDate, System.DateTime targetDate, int totalCount)
	{
		int num = 1;
		try
		{
			System.DateTime dateTime = startDate.AddDays(1.0);
			int num2 = 0;
			while (dateTime <= targetDate && num < totalCount && num2 < 10000)
			{
				num2++;
				if (CheckIsHolidayWeekend(dateTime.Date, null))
				{
					num++;
				}
				dateTime = dateTime.AddDays(1.0);
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine("计算重复日期时出错: " + ex.Message);
		}
		return num;
	}

	private static List<System.DateTime> CalculateSubCalendar(string rrule, List<System.DateTime> exceptDateLists, int limit, string timeZone, bool toLocalTime, System.DateTime startDate, System.DateTime spanStart, System.DateTime spanEnd, bool ignoreLunarIfNotLunar = false)
	{
		System.DateTime? rawDate = null;
		if (ignoreLunarIfNotLunar)
		{
			rawDate = startDate;
		}
		RecurrenceModel recurrenceModel = RecurrenceModel.GetRecurrenceModel(rrule);
		List<System.DateTime> list = new List<System.DateTime>();
		if (spanStart < startDate)
		{
			spanStart = startDate;
		}
		else if (recurrenceModel.Count <= 0 && limit <= 0 && recurrenceModel.Frequency == FrequencyType.Monthly && (spanStart - startDate).TotalDays > 60.0 && recurrenceModel.Interval <= 1)
		{
			System.Globalization.Calendar calendar = CalendarConverter.GetCalendar(CalendarConverter.GetRepeatFlagType(rrule));
			calendar.GetYear(startDate);
			calendar.GetMonth(startDate);
			int dayOfMonth = calendar.GetDayOfMonth(startDate);
			int num = calendar.GetYear(spanStart);
			int month = calendar.GetMonth(spanStart);
			if (month == 1)
			{
				num--;
				month = 12;
			}
			else
			{
				month--;
			}
			try
			{
				startDate = calendar.ToDateTime(num, month, dayOfMonth, 0, 0, 0, 0);
			}
			catch
			{
			}
		}
		while (startDate <= spanEnd)
		{
			System.DateTime? dateTime = CalculateSubCalendarNextDate(rrule, startDate, exceptDateLists, onlyFuture: false, rawDate);
			if (!dateTime.HasValue || dateTime == startDate || dateTime > spanEnd || dateTime < startDate)
			{
				break;
			}
			startDate = dateTime.Value;
			if (ignoreLunarIfNotLunar || !(startDate < System.DateTime.Today))
			{
				System.DateTime item = (toLocalTime ? TimeZoneUtils.ToLocalTime(startDate, timeZone) : startDate);
				if (!list.Contains(item))
				{
					list.Add(item);
				}
				if ((limit > 0 && list.Count >= limit) || (recurrenceModel.Count > 1 && list.Count >= recurrenceModel.Count - 1))
				{
					break;
				}
			}
		}
		return list;
	}

	private static (System.DateTime? untilDate, int? count) ParseRruleUntilAndCount(string rrule)
	{
		string[] source = rrule.Split(';');
		System.DateTime? dateTime = null;
		int? item = null;
		if (rrule.Contains("COUNT"))
		{
			int.TryParse(source.FirstOrDefault((string r) => r.Contains("COUNT"))?.Replace("COUNT=", ""), out var result);
			item = result;
		}
		if (rrule.Contains("UNTIL"))
		{
			string text = source.FirstOrDefault((string r) => r.Contains("UNTIL"));
			try
			{
				dateTime = System.DateTime.ParseExact(text?.Replace("UNTIL=", ""), "yyyyMMdd", App.Ci);
				if (!Utils.IsEmptyDate(dateTime))
				{
					dateTime = dateTime.Value.AddDays(1.0);
				}
			}
			catch (Exception)
			{
			}
		}
		return (untilDate: dateTime, count: item);
	}

	public static System.DateTime GetCountdownRepeatNextDate(string repeatFlag, System.DateTime initialDate)
	{
		System.DateTime today = System.DateTime.Today;
		System.DateTime dateTime = initialDate;
		int num = 0;
		do
		{
			System.DateTime dateTime2 = CalcCountdownRepeatNextDate(repeatFlag, dateTime, initialDate);
			if (dateTime2 <= dateTime)
			{
				break;
			}
			dateTime = dateTime2;
			num++;
		}
		while (dateTime < today && num < 1000);
		return dateTime;
	}

	private static System.DateTime CalcCountdownRepeatNextDate(string rrule, System.DateTime startDate, System.DateTime rawDate)
	{
		if (startDate.Month == 2 && startDate.Day == 29 && rrule.Contains("RRULE:FREQ=YEARLY"))
		{
			int result;
			int num = ((!int.TryParse(Regex.Match(rrule, "INTERVAL=(\\d+)").Groups[1]?.Value, out result)) ? 1 : result);
			if (num == 1)
			{
				int year = System.DateTime.Now.Year + num;
				if (!System.DateTime.IsLeapYear(year))
				{
					return new System.DateTime(year, 2, 28);
				}
				return new System.DateTime(year, 2, 29);
			}
			for (int i = 1; i < 100; i++)
			{
				int year2 = startDate.Year + num * i;
				if ((System.DateTime.IsLeapYear(year2) ? new System.DateTime(year2, 2, 29) : new System.DateTime(year2, 2, 28)) >= System.DateTime.Today)
				{
					return startDate;
				}
			}
			return startDate;
		}
		return GetRepeatNextDate(rrule, "2", startDate, null, null, null, onlyFuture: false, rawDate);
	}

	public static System.DateTime GetCurrentOrNextRepeatDate(string rrule, System.DateTime startDate)
	{
		System.DateTime dateTime = startDate;
		for (int i = 0; i < 24; i++)
		{
			if (CheckDateInRRule(rrule, dateTime))
			{
				break;
			}
			dateTime = CalcCountdownRepeatNextDate(rrule, dateTime, startDate);
		}
		return dateTime;
	}

	public static bool CheckDateInRRule(string rrule, System.DateTime targetDate)
	{
		if (string.IsNullOrEmpty(rrule))
		{
			return false;
		}
		RecurrenceModel recurrenceModel = RecurrenceModel.GetRecurrenceModel(rrule);
		if (recurrenceModel.Frequency == FrequencyType.None)
		{
			return false;
		}
		if (rrule.Contains("RRULE:"))
		{
			if (rrule.Contains("TT_SKIP=OFFICIAL_WORKDAY"))
			{
				return CheckIsHolidayWeekend(targetDate, null);
			}
			if (rrule.Contains("TT_SKIP=HOLIDAY,WEEKEND"))
			{
				return !CheckIsHolidayWeekend(targetDate, null);
			}
			Ical.Net.Calendar calendar = new Ical.Net.Calendar();
			CalendarEvent item = new CalendarEvent
			{
				Start = new CalDateTime(targetDate),
				RecurrenceRules = new List<RecurrencePattern> { recurrenceModel }
			};
			calendar.Events.Add(item);
			return calendar.GetOccurrences(new CalDateTime(targetDate), new CalDateTime(targetDate.AddDays(1.0))).Count > 0;
		}
		CalendarConvertType repeatFlagType = CalendarConverter.GetRepeatFlagType(rrule);
		if (repeatFlagType == CalendarConvertType.Unknown)
		{
			return false;
		}
		System.Globalization.Calendar calendar2 = CalendarConverter.GetCalendar(repeatFlagType);
		int year = calendar2.GetYear(targetDate);
		int num = calendar2.GetMonth(targetDate);
		int dayOfMonth = calendar2.GetDayOfMonth(targetDate);
		int leapMonth = calendar2.GetLeapMonth(year);
		if (leapMonth > 0 && num >= leapMonth)
		{
			num--;
		}
		if (recurrenceModel.Frequency == FrequencyType.Monthly)
		{
			return dayOfMonth == recurrenceModel.ByMonthDay.FirstOrDefault();
		}
		if (recurrenceModel.Frequency == FrequencyType.Yearly)
		{
			if (num == recurrenceModel.ByMonth.FirstOrDefault())
			{
				return dayOfMonth == recurrenceModel.ByMonthDay.FirstOrDefault();
			}
			return false;
		}
		return false;
	}

	private static System.DateTime? CalculateSubCalendarNextDate(string rrule, System.DateTime startDate, List<System.DateTime> exceptDateLists, bool onlyFuture = false, System.DateTime? rawDate = null)
	{
		if (string.IsNullOrEmpty(rrule))
		{
			return null;
		}
		CalendarConvertType repeatFlagType = CalendarConverter.GetRepeatFlagType(rrule);
		if (repeatFlagType == CalendarConvertType.Unknown)
		{
			return null;
		}
		(System.DateTime?, System.DateTime?) limitDate = CalendarConverter.GetLimitDate(repeatFlagType);
		if (limitDate.Item2.HasValue && limitDate.Item2 < startDate)
		{
			return null;
		}
		RecurrenceModel recurrenceModel = RecurrenceModel.GetRecurrenceModel(rrule);
		if (recurrenceModel.Count > 0 && recurrenceModel.Count <= 1)
		{
			return startDate;
		}
		System.DateTime? dateTime = null;
		System.Globalization.Calendar calendar = CalendarConverter.GetCalendar(repeatFlagType);
		int num = calendar.GetYear(startDate);
		int month = calendar.GetMonth(startDate);
		int dayOfMonth = calendar.GetDayOfMonth(startDate);
		int leapMonth = calendar.GetLeapMonth(num);
		if (recurrenceModel.Frequency == FrequencyType.Monthly)
		{
			int daysInMonth = calendar.GetDaysInMonth(num, month);
			int num2 = dayOfMonth;
			if (recurrenceModel.ByMonthDay.Any())
			{
				num2 = recurrenceModel.ByMonthDay.FirstOrDefault();
			}
			if (daysInMonth < num2 && daysInMonth == dayOfMonth)
			{
				int monthsInYear = calendar.GetMonthsInYear(num);
				if (month >= monthsInYear)
				{
					num++;
					month = 1;
				}
				else
				{
					month++;
				}
				num2 = Math.Min(calendar.GetDaysInMonth(num, month), num2);
				dateTime = calendar.AddMonths(startDate, recurrenceModel.Interval);
				if (num2 > dayOfMonth)
				{
					dateTime = dateTime.Value.AddDays(num2 - dayOfMonth);
				}
			}
			else
			{
				if (num2 <= dayOfMonth)
				{
					dateTime = calendar.AddMonths(startDate, recurrenceModel.Interval);
					if (dateTime < startDate)
					{
						UtilLog.Error($"calculate failed :{repeatFlagType} {Utils.GetSafeDateLogText(startDate)} {rrule}");
						dateTime = startDate;
					}
				}
				else
				{
					dateTime = startDate;
				}
				if (num2 != dayOfMonth)
				{
					dateTime = dateTime.Value.AddDays(num2 - dayOfMonth);
				}
			}
		}
		else if (recurrenceModel.Frequency == FrequencyType.Yearly)
		{
			int num3 = ((leapMonth > 0 && month >= leapMonth) ? (month - 1) : month);
			int num4 = num3;
			if (recurrenceModel.ByMonth.Any())
			{
				num4 = recurrenceModel.ByMonth.FirstOrDefault();
			}
			int val = dayOfMonth;
			if (recurrenceModel.ByMonthDay.Any())
			{
				val = recurrenceModel.ByMonthDay.FirstOrDefault();
			}
			int year = num + recurrenceModel.Interval;
			bool flag = false;
			if (leapMonth > 0 && leapMonth - 1 == num3 && leapMonth != month)
			{
				if (!rawDate.HasValue)
				{
					flag = true;
				}
				else
				{
					int year2 = calendar.GetYear(rawDate.Value);
					int month2 = calendar.GetMonth(rawDate.Value);
					if (calendar.GetLeapMonth(year2) == month2)
					{
						flag = true;
					}
				}
			}
			if (flag)
			{
				num4 = leapMonth;
				year = num;
			}
			else
			{
				int leapMonth2 = calendar.GetLeapMonth(year);
				if (leapMonth2 > 0 && leapMonth2 - 1 < num4)
				{
					num4++;
				}
			}
			int daysInMonth2 = calendar.GetDaysInMonth(year, num4);
			int day = Math.Min(val, daysInMonth2);
			try
			{
				dateTime = (System.DateTime?)calendar.ToDateTime(year, num4, day, 0, 0, 0, 0) + (TimeSpan?)startDate.TimeOfDay;
			}
			catch
			{
				dateTime = startDate;
			}
		}
		if (dateTime.HasValue)
		{
			if (recurrenceModel.Until != System.DateTime.MinValue)
			{
				System.DateTime until = recurrenceModel.Until;
				System.DateTime? dateTime2 = dateTime;
				if (until < dateTime2)
				{
					dateTime = startDate;
					goto IL_0474;
				}
			}
			if (dateTime < startDate)
			{
				UtilLog.Error($"calculate failed :{repeatFlagType} {Utils.GetSafeDateLogText(startDate)} {rrule}");
				dateTime = startDate;
			}
			else if ((exceptDateLists != null && exceptDateLists.Contains(dateTime.Value)) || (onlyFuture && dateTime < System.DateTime.Today))
			{
				dateTime = CalculateSubCalendarNextDate(rrule, dateTime.Value.AddDays(1.0), exceptDateLists, onlyFuture, rawDate);
			}
		}
		goto IL_0474;
		IL_0474:
		return dateTime;
	}

	public static System.DateTime GetRepeatNextDate(string rrule, string repeatFrom, System.DateTime startDate, System.DateTime? completeTime, List<string> exDates = null, string timeZone = null, bool onlyFuture = false, System.DateTime? rawDate = null)
	{
		if (!TTCalendarDllInitializer.CheckInitialized())
		{
			UtilLog.Info($"GetRepeatNextDate UnInitialized {rrule}  {repeatFrom}  {Utils.GetSafeDateLogText(completeTime)} {exDates?.Count} {timeZone}");
			return startDate;
		}
		string text = new StringBuilder(rrule).Append(repeatFrom).Append(startDate.ToString("yyyyMMddHHmm", App.Ci)).Append(completeTime?.ToString("yyyyMMddHHmm", App.Ci))
			.Append((exDates == null) ? string.Empty : JsonConvert.SerializeObject(exDates))
			.Append(timeZone)
			.ToString();
		if (nextDateCatch.TryGetValue(text, out var value))
		{
			return value;
		}
		List<TTCalendar> list = new List<TTCalendar>();
		List<System.DateTime> list2 = new List<System.DateTime>();
		if (exDates != null)
		{
			exDates = exDates.Select((string text2) => text2.Replace("\"", "")).ToList();
			foreach (string exDate in exDates)
			{
				if (System.DateTime.TryParseExact(exDate, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
				{
					TTCalendar item = ModelsDao.NewTTCalendar(result, null);
					list.Add(item);
					list2.Add(result);
				}
			}
		}
		if (rrule.Contains("TT_SKIP=OFFICIAL_WORKDAY"))
		{
			(System.DateTime?, int?) tuple = ParseRruleUntilAndCount(rrule);
			System.DateTime? dateTime = CalculateOfficialWorkDayNextDate(startDate, list2, tuple.Item1, tuple.Item2);
			if (dateTime.HasValue && tuple.Item2 == 1 && CheckIsHolidayWeekend(startDate, list2))
			{
				return startDate;
			}
			return dateTime ?? startDate;
		}
		if (!string.IsNullOrEmpty(rrule) && !rrule.Contains("RRULE"))
		{
			System.DateTime? dateTime2 = CalculateSubCalendarNextDate(rrule, startDate, list2, onlyFuture, rawDate);
			if (dateTime2.HasValue)
			{
				return dateTime2.Value;
			}
		}
		list = list.ToList();
		TTCalendar start = ModelsDao.NewTTCalendar(startDate, timeZone);
		TTCalendar completeStamp = ((!completeTime.HasValue) ? new TTCalendar() : ModelsDao.NewTTCalendar(completeTime.Value, timeZone));
		foreach (TTCalendar repeat in GetRepeats(list, start, completeStamp, timeZone, rrule, repeatFrom, new TTCalendar(), new TTCalendar(), 1, 1))
		{
			if (repeat.year > 0)
			{
				System.DateTime time = ModelsDao.TTCalendarToDateTime(repeat);
				time = TimeZoneUtils.ToLocalTime(time, repeat.zone);
				if (Utils.IsEmptyDate(time) && !string.IsNullOrEmpty(rrule))
				{
					UtilLog.Info($"GetRepeatNextDate is Empty {rrule}  {repeatFrom}  {Utils.GetSafeDateLogText(completeTime)} {exDates?.Count} {timeZone}");
				}
				if (text != null)
				{
					nextDateCatch[text] = time;
				}
				return time;
			}
		}
		if (!string.IsNullOrEmpty(rrule))
		{
			UtilLog.Info($"GetRepeatNextDate is Empty {rrule}  {repeatFrom}  {Utils.GetSafeDateLogText(completeTime)} {exDates?.Count} {timeZone}");
		}
		if (text != null)
		{
			nextDateCatch[text] = startDate;
		}
		UtilLog.Info($"GetRepeatNextDate calculate failed {rrule}  {repeatFrom} {Utils.GetSafeDateLogText(startDate)} {Utils.GetSafeDateLogText(completeTime)} {exDates?.Count} {timeZone}");
		return startDate;
	}

	private static System.DateTime? CalculateOfficialWorkDayNextDate(System.DateTime startDate, List<System.DateTime> exceptDateLists, System.DateTime? untilDate, int? count)
	{
		if (count.HasValue && count <= 0)
		{
			return null;
		}
		System.DateTime dateTime = startDate.AddDays(1.0);
		System.DateTime today = System.DateTime.Today;
		if (dateTime < today)
		{
			dateTime = today;
		}
		if (untilDate.HasValue && dateTime >= untilDate.Value)
		{
			return null;
		}
		int num = 0;
		while (num < 1000)
		{
			num++;
			if (untilDate.HasValue && dateTime > untilDate.Value)
			{
				return null;
			}
			if (CheckIsHolidayWeekend(dateTime.Date, exceptDateLists))
			{
				return dateTime;
			}
			dateTime = dateTime.AddDays(1.0);
		}
		return null;
	}

	private static bool CheckIsHolidayWeekend(System.DateTime date, List<System.DateTime> exceptDateLists)
	{
		if (exceptDateLists != null && exceptDateLists.Any((System.DateTime d) => d.Date == date))
		{
			return false;
		}
		if (IsLegalHoliday(date))
		{
			return true;
		}
		if (IsLegalWorkingDay(date))
		{
			return false;
		}
		if (IsWeekend(date))
		{
			return true;
		}
		return false;
	}

	private static bool IsLegalHoliday(System.DateTime date)
	{
		return HolidayManager.GetHolidays()?.FirstOrDefault((HolidayModel h) => h.type == 0 && h.date.Date == date.Date) != null;
	}

	private static bool IsLegalWorkingDay(System.DateTime date)
	{
		return HolidayManager.GetHolidays()?.FirstOrDefault((HolidayModel h) => h.type == 1 && h.date.Date == date.Date) != null;
	}

	private static bool IsWeekend(System.DateTime date)
	{
		if (date.DayOfWeek != DayOfWeek.Saturday)
		{
			return date.DayOfWeek == DayOfWeek.Sunday;
		}
		return true;
	}

	private static List<TTCalendar> GetRepeats(List<TTCalendar> except, TTCalendar start, TTCalendar completeStamp, string timeZone, string rrule, string repeatFrom, TTCalendar spanStart, TTCalendar spanEnd, int limit, int skip)
	{
		rrule = RRuleUtils.HandleUntilText(rrule, timeZone);
		string msg = $"GetRepeats except[{except?.Count}], start={start}, spanStart={spanStart}, spanEnd={spanEnd}, completeStamp={completeStamp}, timeZone={timeZone}, rrule={rrule}, repeatFrom={repeatFrom}, limit={limit}, skip={skip}";
		int callerThreadId = Thread.CurrentThread.ManagedThreadId;
		Stopwatch dispatcherWaitWatch = Stopwatch.StartNew();
		return UtilRun.InvokeOnUI(msg, delegate
		{
			dispatcherWaitWatch.Stop();
			Stopwatch stopwatch = Stopwatch.StartNew();
			try
			{
				return RecurrenceCalcualte.GetRepeats(except, except?.Count ?? 0, start, spanStart, spanEnd, completeStamp, timeZone, rrule, repeatFrom, limit, skip);
			}
			finally
			{
				stopwatch.Stop();
				if (dispatcherWaitWatch.ElapsedMilliseconds > 300 || stopwatch.ElapsedMilliseconds > 300)
				{
					UtilLog.Warn($"GetRepeats timing dispatcherWaitMs={dispatcherWaitWatch.ElapsedMilliseconds}, nativeCalculateMs={stopwatch.ElapsedMilliseconds}, callerThreadId={callerThreadId}, executeThreadId={Thread.CurrentThread.ManagedThreadId}, {msg}");
				}
			}
		});
	}
}
