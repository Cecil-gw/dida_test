using System;
using System.Globalization;
using System.Linq;
using KotlinModels;
using Newtonsoft.Json;
using TickTickHandler;
using TickTickModels;
using TickTickUtils.Lang;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;

namespace ticktick_WPF.Util.KotlinUtils;

public static class TTCalendarDllInitializer
{
	private static bool _inited;

	static TTCalendarDllInitializer()
	{
		TickTickHandler.Utils.ToastTimeCallback(ToastTime);
		TickTickHandler.Utils.ToastCallback(Toast);
		TickTickHandler.DateTime.GetNowCallback(GetNowFun);
		TickTickHandler.DateTime.GetNowByZoneCallback(GetNowByZoneFun);
		TickTickHandler.DateTime.GetTimeInMillisCallback(GetTimeInMillisFun);
		TickTickHandler.DateTime.GetTimeByMillisAndZoneCallback(GetTimeByMillisAndZoneFun);
		TickTickHandler.DateTime.GetTimeCallback(GetTimeFun);
		TickTickHandler.DateTime.GetDayOfWeekCallback(GetDayOfWeekFun);
		TickTickHandler.DateTime.GetLocalZoneCallback(GetLocalZoneFun);
		TickTickHandler.DateTime.GetZoneOffsetCallback(GetZoneOffsetFun);
		TickTickHandler.DateTime.GetIsWorkDayCallback(IsWorkDay);
		TickTickHandler.DateTime.GetIsRestCallback(IsRest);
		TickTickHandler.DateTime.GetWeekOfYearCallback(GetWeekOfYearFun);
		TickTickHandler.DateTime.SetWeekOfYearCallback(SetWeekOfYearFun);
		TickTickHandler.DateTime.ParseServerDateByPatternFunCallback(ParseServerDateByPatternFun);
		TickTickHandler.DateTime.GetFirstDayOfWeekCallback(GetFirstDayOfWeekFun);
		TickTickHandler.DateTime.SetDayOfWeekCallback(SetDayOfWeekFun);
		TickTickHandler.DateTime.CurrentTimeMillisCallback(CurrentTimeMillisFun);
		TickTickHandler.DateTime.FormatServerDateCallback(FormatServerDateFun);
		TickTickHandler.DateTime.ParseServerDateCallback(ParseServerDateFun);
		TickTickHandler.DateTime.ConvertDateToTimeCallback(ConvertDateToTimeFun);
		TickTickHandler.DateTime.ConvertDateToUTCCallback(ConvertDateToUTCFun);
		TickTickHandler.DateTime.FormatDateByTimeZoneCallback(FormatDateByTimeZoneFun);
		TickTickHandler.DateTime.ConvertDateFromOneTimeZoneToAnotherTimeZoneCallback(ConvertDateFromOneTimeZoneToAnotherTimeZoneFun);
		Tools.GetUUIDFunCallback(GetUUIDFun);
		TickTickHandler.Utils.InitDll();
		_inited = true;
	}

	public static void Init()
	{
	}

	private static string FormatDateByTimeZoneFun(string format, long timeStamp, string zoneName)
	{
		System.DateTime dateTime = new System.DateTime(1970, 1, 1).AddMilliseconds(timeStamp);
		TimeZoneInfo timeZoneInfo = TimeZoneUtils.GetTimeZoneInfo(zoneName);
		return TimeZoneInfo.ConvertTime(dateTime, timeZoneInfo).ToString(format);
	}

	private static string GetUUIDFun()
	{
		return Guid.NewGuid().ToString("N").Substring(8);
	}

	private static TTCalendar ConvertDateFromOneTimeZoneToAnotherTimeZoneFun(string zoneName, TTCalendar date, string targetZoneName)
	{
		System.DateTime time = ModelsDao.TTCalendarToDateTime(date);
		return ModelsDao.NewTTCalendar(TimeZoneUtils.ZoneToTargetTzTime(zoneName, time, targetZoneName), targetZoneName);
	}

	private static TTCalendar ConvertDateToUTCFun(string zoneName, TTCalendar date)
	{
		System.DateTime dateTime = ModelsDao.TTCalendarToDateTime(date);
		TimeZoneInfo timeZoneInfo = TimeZoneUtils.GetTimeZoneInfo(zoneName);
		return ModelsDao.NewTTCalendar(TimeZoneInfo.ConvertTime(dateTime, timeZoneInfo, TimeZoneInfo.Utc), "Etc/UTC");
	}

	private static long ConvertDateToTimeFun(TTCalendar date)
	{
		if (date.year > 1970 && date.month >= 0 && date.day >= 0 && date.hour >= 0)
		{
			_ = date.minute;
			_ = 0;
		}
		System.DateTime time = ModelsDao.TTCalendarToDateTime(date);
		string zone = date.zone;
		return (long)(TimeZoneUtils.ToLocalTime(time, zone) - new System.DateTime(1970, 1, 1)).TotalMilliseconds;
	}

	private static TTCalendar ParseServerDateByPatternFun(string patternStr, string dateString)
	{
		return ModelsDao.NewTTCalendar(System.DateTime.ParseExact(dateString, patternStr, CultureInfo.InvariantCulture), TimeZoneData.LocalTimeZoneModel.TimeZoneName);
	}

	private static TTCalendar ParseServerDateFun(string str)
	{
		return ModelsDao.NewTTCalendar(JsonConvert.DeserializeObject<System.DateTime>(str, new JsonConverter[1]
		{
			new UtcDateTimeConverter()
		}), TimeZoneData.LocalTimeZoneModel.TimeZoneName);
	}

	private static string FormatServerDateFun(TTCalendar ttCal)
	{
		return JsonConvert.SerializeObject(ModelsDao.TTCalendarToDateTime(ttCal), new UtcDateTimeConverter());
	}

	private static long CurrentTimeMillisFun()
	{
		return (System.DateTime.UtcNow.Ticks - 621355968000000000L) / 10000000;
	}

	private static TTCalendar SetDayOfWeekFun(int year, int month, int day, string zoneName, int firstdayofweek, int dayofweek)
	{
		System.DateTime weekStart = Utils.GetWeekStart(new System.DateTime(1, 1, 1).AddYears(year - 1).AddMonths(month).AddDays(day - 1));
		int num = dayofweek - firstdayofweek;
		if (num < 0)
		{
			num += 7;
		}
		return ModelsDao.NewTTCalendar(weekStart.AddDays(num), zoneName);
	}

	private static int GetFirstDayOfWeekFun()
	{
		return (int)(LocalSettings.Settings.WeekStartFromType + 1);
	}

	private static int GetWeekOfYearFun(int year, int month, int day, int hour, int minute, int second, int milliSecond, string zoneName)
	{
		return DateUtils.GetWeekNumOfYear(new System.DateTime(1, 1, 1, 0, 0, 0).AddYears(year - 1).AddMonths(month).AddDays(day - 1)
			.AddHours(hour)
			.AddMinutes(minute)
			.AddSeconds(second)
			.AddMilliseconds(milliSecond));
	}

	private static TTCalendar SetWeekOfYearFun(int year, int month, int day, string zoneName, int weekOfYear)
	{
		if (weekOfYear < 1)
		{
			weekOfYear = 1;
		}
		return ModelsDao.NewTTCalendar(Utils.GetWeekStart(new System.DateTime(year, 1, 1)).AddDays((weekOfYear - 1) * 7), zoneName);
	}

	public static int IsRest(TTCalendar date)
	{
		System.DateTime time = ModelsDao.TTCalendarToDateTime(date);
		return (HolidayManager.GetHolidays()?.FirstOrDefault((HolidayModel h) => h.type == 0 && h.date.Date == time.Date) != null) ? 1 : 0;
	}

	private static int IsWorkDay(TTCalendar date)
	{
		System.DateTime time = ModelsDao.TTCalendarToDateTime(date);
		return (HolidayManager.GetHolidays()?.FirstOrDefault((HolidayModel h) => h.type == 1 && h.date.Date == time.Date) != null) ? 1 : 0;
	}

	private static void ToastTime(TTCalendar zone)
	{
	}

	private static void Toast(string str)
	{
		UtilLog.Info(str);
	}

	public static bool CheckInitialized()
	{
		return _inited;
	}

	public static int GetZoneOffsetFun(string zoneName, long millis)
	{
		System.DateTime dateTime = new System.DateTime(1970, 1, 1).AddMilliseconds(millis);
		TimeZoneInfo timeZoneInfo = TimeZoneUtils.GetTimeZoneInfo(zoneName);
		System.DateTime dateTime2 = TimeZoneInfo.ConvertTime(dateTime, timeZoneInfo, TimeZoneInfo.Utc);
		return (int)(dateTime - dateTime2).TotalSeconds;
	}

	public static string GetLocalZoneFun()
	{
		return TimeZoneData.LocalTimeZoneModel.TimeZoneName;
	}

	public static int GetDayOfWeekFun(int year, int month, int day, int hour, int minute, int second, int millisecond, string timezone)
	{
		return (int)(new System.DateTime(1, 1, 1).AddYears(year - 1).AddMonths(month).AddDays(day - 1)
			.AddHours(hour)
			.AddMinutes(minute)
			.AddSeconds(second)
			.AddMilliseconds(millisecond)
			.DayOfWeek + 1);
	}

	public static TTCalendar GetTimeFun(int year, int month, int day, int hour, int minute, int second, int millisecond, string zoneName)
	{
		try
		{
			return ModelsDao.NewTTCalendar(new System.DateTime(1, 1, 1).AddYears(year - 1).AddMonths(month).AddDays(day - 1)
				.AddHours(hour)
				.AddMinutes(minute)
				.AddSeconds(second)
				.AddMilliseconds(millisecond), zoneName ?? TimeZoneData.LocalTimeZoneModel.TimeZoneName);
		}
		catch (Exception)
		{
			return new TTCalendar();
		}
	}

	public static TTCalendar GetTimeByMillisAndZoneFun(long millis, string zoneName)
	{
		try
		{
			return new TTCalendar(new System.DateTime(1970, 1, 1).AddMilliseconds(millis), zoneName);
		}
		catch (Exception)
		{
			return new TTCalendar();
		}
	}

	public static long GetTimeInMillisFun(TTCalendar date)
	{
		if (date.year <= 0)
		{
			return (long)(default(System.DateTime) - new System.DateTime(1970, 1, 1)).TotalMilliseconds;
		}
		return (long)(ModelsDao.TTCalendarToDateTime(date) - new System.DateTime(1970, 1, 1)).TotalMilliseconds;
	}

	public static TTCalendar GetNowByZoneFun(string zoneName)
	{
		System.DateTime now = System.DateTime.Now;
		now = TimeZoneUtils.LocalToTargetTzTime(now, zoneName);
		return new TTCalendar
		{
			year = now.Year,
			month = now.Month,
			day = now.Day,
			hour = now.Hour,
			minute = now.Minute,
			second = now.Second,
			zone = zoneName
		};
	}

	public static TTCalendar GetNowFun()
	{
		System.DateTime now = System.DateTime.Now;
		return new TTCalendar
		{
			year = now.Year,
			month = now.Month,
			day = now.Day,
			hour = now.Hour,
			minute = now.Minute,
			second = now.Second,
			zone = TimeZoneData.LocalTimeZoneModel.TimeZoneName
		};
	}
}
