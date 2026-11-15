using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Util;

public static class DateUtils
{
	public enum DateSectionCategory
	{
		NoDate,
		OutDated,
		Today,
		Tomorrow,
		ThisWeek,
		Future
	}

	public enum CreateTimeCategory
	{
		Today,
		Yesterday,
		LastWeek,
		LastMonth,
		Earlier
	}

	public enum TimeDisplayFormat
	{
		Hour12,
		Hour24
	}

	public const string Hour24 = "24Hour";

	public const string Hour12 = "12Hour";

	public static readonly Dictionary<int, string> ChineseMonthDict = new Dictionary<int, string>
	{
		{ 1, "正月" },
		{ 2, "二月" },
		{ 3, "三月" },
		{ 4, "四月" },
		{ 5, "五月" },
		{ 6, "六月" },
		{ 7, "七月" },
		{ 8, "八月" },
		{ 9, "九月" },
		{ 10, "十月" },
		{ 11, "冬月" },
		{ 12, "腊月" }
	};

	public static readonly Dictionary<int, string> ChineseNumDict = new Dictionary<int, string>
	{
		{ 1, "初一" },
		{ 2, "初二" },
		{ 3, "初三" },
		{ 4, "初四" },
		{ 5, "初五" },
		{ 6, "初六" },
		{ 7, "初七" },
		{ 8, "初八" },
		{ 9, "初九" },
		{ 10, "初十" },
		{ 11, "十一" },
		{ 12, "十二" },
		{ 13, "十三" },
		{ 14, "十四" },
		{ 15, "十五" },
		{ 16, "十六" },
		{ 17, "十七" },
		{ 18, "十八" },
		{ 19, "十九" },
		{ 20, "二十" },
		{ 21, "廿一" },
		{ 22, "廿二" },
		{ 23, "廿三" },
		{ 24, "廿四" },
		{ 25, "廿五" },
		{ 26, "廿六" },
		{ 27, "廿七" },
		{ 28, "廿八" },
		{ 29, "廿九" },
		{ 30, "三十" },
		{ 31, "三一" }
	};

	private static CultureInfo enUSCulture { get; } = new CultureInfo("en-US");

	private static CultureInfo enGBCulture { get; } = new CultureInfo("en-GB");

	private static string _systemHourFormat { get; set; }

	private static string FormatDateSafely(DateTime date, string pattern, CultureInfo culture, string fallbackPattern = null)
	{
		try
		{
			return date.ToString(pattern, culture);
		}
		catch (ArgumentOutOfRangeException)
		{
			return date.ToString(fallbackPattern ?? pattern, CultureInfo.InvariantCulture);
		}
	}

	private static string GetNumericMonthDayPattern()
	{
		string sysDateSeparator = GetSysDateSeparator();
		if (GetSysDateFormatType() != DateFormatType.DMY)
		{
			return "MM" + sysDateSeparator + "dd";
		}
		return "dd" + sysDateSeparator + "MM";
	}

	private static string GetNumericYearMonthPattern()
	{
		string sysDateSeparator = GetSysDateSeparator();
		if (GetSysDateFormatType() != DateFormatType.YMD)
		{
			return "MM" + sysDateSeparator + "yyyy";
		}
		return "yyyy" + sysDateSeparator + "MM";
	}

	private static string GetNumericYearMonthDayPattern()
	{
		string sysDateSeparator = GetSysDateSeparator();
		return GetSysDateFormatType() switch
		{
			DateFormatType.MDY => "MM" + sysDateSeparator + "dd" + sysDateSeparator + "yyyy", 
			DateFormatType.YMD => "yyyy" + sysDateSeparator + "MM" + sysDateSeparator + "dd", 
			_ => "dd" + sysDateSeparator + "MM" + sysDateSeparator + "yyyy", 
		};
	}

	public static string FormatCommentTime(DateTime date)
	{
		TimeSpan timeSpan = DateTime.Now - date;
		if (0.0 < timeSpan.TotalSeconds && timeSpan.TotalSeconds < 60.0)
		{
			return Utils.GetString("JustNow");
		}
		if (1.0 <= timeSpan.TotalMinutes && timeSpan.TotalMinutes < 60.0)
		{
			if (Math.Abs(timeSpan.TotalMinutes - 1.0) <= 0.0001)
			{
				return (int)timeSpan.TotalMinutes + (Utils.IsCn() ? "" : " ") + Utils.GetString("PublicMinute") + (Utils.IsCn() ? "" : " ") + Utils.GetString("Ago");
			}
			return (int)timeSpan.TotalMinutes + (Utils.IsCn() ? "" : " ") + Utils.GetString("PublicMinutes") + (Utils.IsCn() ? "" : " ") + Utils.GetString("Ago");
		}
		string text = date.ToString(GetTimeFormatString(), App.Ci);
		if (date.Date == DateTime.Today)
		{
			return text;
		}
		if (date.Date.Year == DateTime.Today.Year)
		{
			return FormatMonthDay(date, DateFormatLen.Short) + " " + text;
		}
		return FormatYearMonthDay(date, DateFormatLen.Short) + " " + text;
	}

	public static string FormatSummaryTime(DateTime date, bool withTime)
	{
		if (!withTime)
		{
			return FormatMonthDay(date, DateFormatLen.Long);
		}
		return FormatMonthDay(date, DateFormatLen.Long) + " " + date.ToString(GetTimeFormatString(), App.Ci);
	}

	public static string FormatSummaryTimeOnly(DateTime date)
	{
		return date.ToString(GetTimeFormatString(), App.Ci);
	}

	public static DateTime GetNextHour(DateTime date)
	{
		int hour = DateTime.Now.Hour;
		DateTime result = SetHourAndMinuteOnly(date, hour, 0);
		if (!(result.Date != DateTime.Today) || result.Hour != 23)
		{
			return result.AddHours(1.0);
		}
		return result;
	}

	public static DateTime GetNextHour(string timeZone)
	{
		DateTime dateTime = ((string.IsNullOrEmpty(timeZone) || timeZone == TimeZoneData.LocalTimeZoneModel?.TimeZoneName) ? DateTime.Now : TimeZoneUtils.LocalToTargetTzTime(DateTime.Now, timeZone));
		return SetHourAndMinuteOnly(dateTime, dateTime.Hour, 0).AddHours(1.0);
	}

	public static string FormatHourMinuteText(DateTime date)
	{
		return date.ToString(GetTimeFormatString(), App.Ci);
	}

	public static string GetHourText(DateTime date)
	{
		bool flag = GetTimeDisplayFormat() == TimeDisplayFormat.Hour12;
		string text = (flag ? "hh" : "HH");
		if (LocalSettings.Settings.SettingsModel.TimeFormat == "System" && !CheckSystemFormatPadZero())
		{
			text = (flag ? "%h" : "%H");
		}
		return date.ToString(text, CultureInfo.CreateSpecificCulture("en-US"));
	}

	public static string GetMinuteText(DateTime date)
	{
		return date.ToString("mm", CultureInfo.CreateSpecificCulture("en-US"));
	}

	public static string GetAmOrPm(DateTime date)
	{
		return date.ToString("tt", CultureInfo.CreateSpecificCulture("en-US"));
	}

	public static string GetHourMinuteText(DateTime time)
	{
		bool flag = GetTimeDisplayFormat() == TimeDisplayFormat.Hour12;
		string text = (flag ? "hh:mm tt" : "HH:mm");
		if (LocalSettings.Settings.SettingsModel.TimeFormat == "System" && !CheckSystemFormatPadZero())
		{
			text = (flag ? "h:mm tt" : "H:mm");
		}
		return time.ToString(text, new CultureInfo("en-US"));
	}

	public static DateTime SetHourAndMinuteOnly(DateTime dateTime, int hours, int minutes)
	{
		return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, hours, minutes, 0, 0, dateTime.Kind);
	}

	public static DateTime CropHourAndMinute(DateTime dateTime)
	{
		return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, 0, 0, 0, 0, dateTime.Kind);
	}

	public static DateTime SetMinuteOnly(DateTime date, int minute)
	{
		if (minute >= 0 && minute < 60)
		{
			return new DateTime(date.Year, date.Month, date.Day, date.Hour, minute, date.Second, date.Millisecond, date.Kind);
		}
		return date;
	}

	public static int GetNextWeekDayDiff()
	{
		DayOfWeek weekStartFromType = LocalSettings.Settings.WeekStartFromType;
		DayOfWeek dayOfWeek = DateTime.Now.DayOfWeek;
		int num = ((int)(7 - dayOfWeek) + (int)weekStartFromType) % 7;
		if (num != 0)
		{
			return num;
		}
		return 7;
	}

	public static TimeDisplayFormat GetTimeDisplayFormat()
	{
		if (!(LocalSettings.Settings.TimeFormat == "24Hour"))
		{
			return TimeDisplayFormat.Hour12;
		}
		return TimeDisplayFormat.Hour24;
	}

	public static string GetDefaultReminder()
	{
		if (GetTimeDisplayFormat() != TimeDisplayFormat.Hour12)
		{
			return "(09:00)";
		}
		return "(09:00 AM)";
	}

	public static string GetTriggerDisplayText(string trigger, DateTime pivotDate, bool isAllDay)
	{
		Match match = new Regex("(-)?P((\\d{0,})Y)?((\\d{0,})M)?((\\d{0,})W)?((\\d{0,})D)?T?((\\d{0,})H)?((\\d{0,})M)?((\\d{0,})S)?").Match(trigger);
		if (match.Success)
		{
			int.TryParse(match.Groups[9].ToString(), out var result);
			int.TryParse(match.Groups[11].ToString(), out var result2);
			int.TryParse(match.Groups[13].ToString(), out var result3);
			return GetTriggerDisplayText(pivotDate, result, result2, result3);
		}
		return string.Empty;
	}

	public static string GetTriggerDisplayText(DateTime pivotDate, int days, int hours, int mins)
	{
		days++;
		DateTime date = pivotDate.AddDays(days * -1).AddHours(hours * -1).AddMinutes(mins * -1);
		string text = Utils.GetString((days > 1) ? "PublicDays" : "PublicDay");
		string text2 = (Utils.IsCn() ? "" : " ");
		string text3 = (Utils.IsCn() ? " " : "");
		return string.Format(Utils.GetString("AdvanceText"), $"{text3}{days} {text}{text2}") + "(" + FormatHourMinuteText(date) + ")";
	}

	public static Tuple<int, string> GetIntervalAndUnit(int day, int hour, int min)
	{
		int num = day + 1;
		if (num > 0 && num % 7 == 0)
		{
			int num2 = num / 7;
			return new Tuple<int, string>(num2, Utils.GetString((num2 > 1) ? "PublicWeeks" : "PublicWeek"));
		}
		if (num > 0)
		{
			return new Tuple<int, string>(day, Utils.GetString((day > 1) ? "PublicDays" : "PublicDay"));
		}
		if (hour > 0)
		{
			return new Tuple<int, string>(hour, Utils.GetString((day > 1) ? "PublicHours" : "PublicHour"));
		}
		if (min > 0)
		{
			return new Tuple<int, string>(min, Utils.GetString((day > 1) ? "PublicMinutes" : "PublicMinute"));
		}
		return new Tuple<int, string>(0, string.Empty);
	}

	public static DateTime SetDateOnly(DateTime original, DateTime modify)
	{
		return new DateTime(modify.Year, modify.Month, modify.Day, original.Hour, original.Minute, original.Second, original.Millisecond, original.Kind);
	}

	public static string FormatMonthDay(DateTime date, DateFormatLen lenType)
	{
		try
		{
			CultureInfo cultureInfo = App.Ci;
			string text = "d MMMM";
			switch (lenType)
			{
			case DateFormatLen.Short:
				cultureInfo = App.SystemCulture;
				text = GetNumericMonthDayPattern();
				break;
			case DateFormatLen.Middle:
			case DateFormatLen.Long:
				if (App.Ci.TwoLetterISOLanguageName == "en" && App.SystemCulture.TwoLetterISOLanguageName == "en")
				{
					cultureInfo = App.SystemCulture;
				}
				text = cultureInfo.DateTimeFormat.MonthDayPattern;
				if (lenType == DateFormatLen.Middle && !string.IsNullOrEmpty(text) && text.Contains("MMMM"))
				{
					text = text.Replace("MMMM", "MMM");
				}
				break;
			}
			return FormatDateSafely(date, text, cultureInfo, GetNumericMonthDayPattern());
		}
		catch (Exception)
		{
			return date.ToString(GetNumericMonthDayPattern(), CultureInfo.InvariantCulture);
		}
	}

	public static string FormatMonth(DateTime date, bool longMonth = false)
	{
		if (!longMonth)
		{
			return FormatShortMonth(date);
		}
		return FormatLongMonth(date);
	}

	public static string FormatWeekDayName(DateTime date, bool withSmart = false)
	{
		if (withSmart)
		{
			switch ((int)(date.Date - DateTime.Today).TotalDays)
			{
			case 0:
				return Utils.GetString("Today");
			case 1:
				return Utils.GetString("Tomorrow");
			case -1:
				return Utils.GetString("PublicYesterday");
			}
		}
		return date.ToString("ddd", App.Ci);
	}

	public static string FormatSimpleWeekDayName(DateTime date)
	{
		DayOfWeek dayOfWeek = date.DayOfWeek;
		return App.Ci.DateTimeFormat.GetShortestDayName(dayOfWeek);
	}

	public static string FormatYearMonth(DateTime date, DateFormatLen lenType)
	{
		try
		{
			CultureInfo cultureInfo = App.Ci;
			string text = "MMMM yyyy";
			switch (lenType)
			{
			case DateFormatLen.Short:
				cultureInfo = App.SystemCulture;
				text = GetNumericYearMonthPattern();
				break;
			case DateFormatLen.Middle:
			case DateFormatLen.Long:
				if (App.Ci.TwoLetterISOLanguageName == "en" && App.SystemCulture.TwoLetterISOLanguageName == "en")
				{
					cultureInfo = App.SystemCulture;
				}
				text = cultureInfo.DateTimeFormat.YearMonthPattern;
				if (lenType == DateFormatLen.Middle && !string.IsNullOrEmpty(text) && text.Contains("MMMM"))
				{
					text = text.Replace("MMMM", "MMM");
				}
				break;
			}
			return FormatDateSafely(date, text, cultureInfo, GetNumericYearMonthPattern());
		}
		catch (Exception)
		{
			return date.ToString(GetNumericYearMonthPattern(), CultureInfo.InvariantCulture);
		}
	}

	public static string FormatYear(DateTime date)
	{
		return FormatDateSafely(date, "yyyy", App.Ci) + ((Utils.IsCn() || App.Ci.ToString() == "ja-JP") ? "年" : ((App.Ci.ToString() == "ko-KR") ? "년" : ""));
	}

	public static string FormatShortDate(DateTime date)
	{
		return FormatDateCheckYear(date);
	}

	public static string FormatDateCheckYear(DateTime date)
	{
		if (date.Year != DateTime.Today.Year)
		{
			return FormatYearMonthDay(date, DateFormatLen.Short);
		}
		return FormatMonthDay(date, DateFormatLen.Middle);
	}

	private static string FormatShortDate(DateTime date, bool withYear)
	{
		try
		{
			return withYear ? FormatYearMonthDay(date, DateFormatLen.Short) : FormatMonthDay(date, DateFormatLen.Middle);
		}
		catch (Exception)
		{
			return date.ToString(withYear ? GetNumericYearMonthDayPattern() : GetNumericMonthDayPattern(), CultureInfo.InvariantCulture);
		}
	}

	public static string FormatLongMonth(DateTime date)
	{
		if (Utils.IsCn())
		{
			return FormatDateSafely(date, "MMM", App.Ci);
		}
		return FormatDateSafely(date, "MMMM", App.Ci);
	}

	public static string FormatShortMonth(DateTime date)
	{
		return FormatDateSafely(date, "MMM", App.Ci) + ((App.Ci.ToString() == "ja-JP") ? "月" : ((App.Ci.ToString() == "ko-KR") ? Utils.GetString("Month") : ""));
	}

	public static string FormatDay(DateTime date)
	{
		string[] obj = (string[])Application.Current?.FindResource("OneMonthDay");
		if (obj == null)
		{
			return null;
		}
		return obj[date.Day - 1];
	}

	public static string FormatDay(int day)
	{
		string[] array = (string[])Application.Current?.FindResource("OneMonthDay");
		if (array == null)
		{
			return null;
		}
		if (day < 0)
		{
			day = DateTime.DaysInMonth(DateTime.Today.Year, DateTime.Today.Month) + day + 1;
		}
		if (day > 0 && day <= array.Length)
		{
			return array[day - 1];
		}
		return null;
	}

	private static string FormatCalendarDateDesc(DateTime date)
	{
		if (Utils.IsEmptyDate(date))
		{
			return string.Empty;
		}
		if (date.Year - DateTime.Today.Year != 0)
		{
			return FormatYearMonthDay(date, DateFormatLen.Middle) + ", " + FormatWeekDayName(date, withSmart: true);
		}
		return FormatMonthDay(date, DateFormatLen.Long) + ", " + FormatWeekDayName(date, withSmart: true);
	}

	public static string FormatTimeDesc(DateTime date, bool isAllDay, bool showDesc = true, bool inTomorrowList = false)
	{
		try
		{
			if (Utils.IsEmptyDate(date))
			{
				return string.Empty;
			}
			int num = (int)(date.Date - DateTime.Today.Date).TotalDays;
			double totalDays = (Utils.GetWeekStart(DateTime.Today) - DateTime.Today.Date).TotalDays;
			switch (num)
			{
			case -1:
				if (showDesc)
				{
					return Utils.GetString("PublicYesterday");
				}
				break;
			case 0:
				return (!isAllDay) ? FormatHourMinuteText(date) : Utils.GetString("Today");
			case 1:
				return (inTomorrowList && !isAllDay) ? FormatHourMinuteText(date) : Utils.GetString("Tomorrow");
			case 2:
				if (Utils.IsCn())
				{
					return Utils.GetString("In2Days");
				}
				goto default;
			default:
				if (num >= 2 && (double)num < totalDays + 7.0)
				{
					return date.ToString("ddd", App.Ci);
				}
				if ((double)num >= totalDays + 7.0 && (double)num < totalDays + 14.0)
				{
					return FormatNextWeekDay(date);
				}
				break;
			}
			bool withYear = date.Year != DateTime.Today.Year;
			return FormatShortDate(date, withYear);
		}
		catch (Exception)
		{
			return date.ToString((date.Year != DateTime.Today.Year) ? GetNumericYearMonthDayPattern() : GetNumericMonthDayPattern(), CultureInfo.InvariantCulture);
		}
	}

	private static string FormatNextWeekDay(DateTime date)
	{
		if (Utils.IsPt() && date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
		{
			return Utils.GetString("Next2") + " " + date.ToString("ddd", App.Ci);
		}
		return string.Format(Utils.GetString("NextWeekDay"), date.ToString("ddd", App.Ci));
	}

	public static string FormatYearMonthDay(DateTime date, DateFormatLen lenType, bool withWeek = false)
	{
		try
		{
			CultureInfo cultureInfo = App.Ci;
			string text = "dd MMMM yyyy";
			switch (lenType)
			{
			case DateFormatLen.Short:
				cultureInfo = App.SystemCulture;
				text = GetNumericYearMonthDayPattern();
				break;
			case DateFormatLen.Middle:
			case DateFormatLen.Long:
				if (App.Ci.TwoLetterISOLanguageName == "en" && App.SystemCulture.TwoLetterISOLanguageName == "en")
				{
					cultureInfo = App.SystemCulture;
				}
				text = cultureInfo.DateTimeFormat.LongDatePattern;
				if (!string.IsNullOrEmpty(text) && !withWeek)
				{
					if (text.Contains("dddd "))
					{
						text = text.Replace("dddd ", "");
					}
					if (text.Contains(" dddd"))
					{
						text = text.Replace(" dddd", "");
					}
					if (text.Contains("dddd,"))
					{
						text = text.Replace("dddd,", "");
					}
					if (text.Contains(",dddd"))
					{
						text = text.Replace(",dddd", "");
					}
					if (text.Contains("dddd"))
					{
						text = text.Replace("dddd", "");
					}
					text = text.Trim();
				}
				if (lenType == DateFormatLen.Middle && !string.IsNullOrEmpty(text) && text.Contains("MMMM"))
				{
					text = text.Replace("MMMM", "MMM");
				}
				break;
			}
			return FormatDateSafely(date, text, cultureInfo, GetNumericYearMonthDayPattern());
		}
		catch (Exception)
		{
			return date.ToString(GetNumericYearMonthDayPattern(), CultureInfo.InvariantCulture);
		}
	}

	public static string FormatHourMinute(DateTime date)
	{
		return date.ToString(GetTimeFormatString(), App.Ci);
	}

	public static string GetTimeFormatString()
	{
		return ((GetTimeDisplayFormat() == TimeDisplayFormat.Hour12) ? (App.Ci.DateTimeFormat.GetAllDateTimePatterns('t').FirstOrDefault((string f) => f.Contains("t")) ?? "hh:mm tt") : "H:mm").Replace("'", "\\'");
	}

	public static string FormatListDateString(DateTime startDate, DateTime? dueDate, bool? isAllDay, bool showDetail = false, bool inTomorrowList = false)
	{
		if (showDetail)
		{
			return FormatDateString(startDate, dueDate, isAllDay, useSmartDate: true, showOverDue: false, showSmartOnly: true, inTomorrowList, inList: true);
		}
		if (!dueDate.HasValue || Utils.IsEmptyDate(dueDate))
		{
			return FormatTimeDesc(startDate, isAllDay ?? true, showDesc: true, inTomorrowList);
		}
		if (inTomorrowList)
		{
			if (IsDateInTomorrow(startDate, dueDate, isAllDay))
			{
				return FormatDayDescription(startDate, dueDate.Value, isAllDay, 1, "Tomorrow");
			}
		}
		else if (IsDateInToday(startDate, dueDate, isAllDay))
		{
			return FormatDayDescription(startDate, dueDate.Value, isAllDay, 0, "Today");
		}
		if (IsOutDated(startDate, dueDate, isAllDay))
		{
			DateTime? dateTime = dueDate;
			if (isAllDay.HasValue && isAllDay.Value)
			{
				dateTime = dueDate.Value.AddDays(-1.0);
				if (dateTime.Value.Date <= startDate.Date)
				{
					dateTime = null;
				}
			}
			return FormatTimeDesc(dateTime ?? startDate, isAllDay ?? true);
		}
		return FormatTimeDesc(startDate, isAllDay ?? true, showDesc: false);
	}

	private static string FormatDayDescription(DateTime startDate, DateTime dueDate, bool? isAllDay, int dayDiff, string defaultStringKey)
	{
		if (isAllDay.HasValue && isAllDay.Value)
		{
			return Utils.GetString(defaultStringKey);
		}
		if (startDate.Date.Date == DateTime.Today.AddDays(dayDiff).Date)
		{
			return FormatHourMinuteText(startDate);
		}
		if (!(dueDate.Date.Date == DateTime.Today.AddDays(dayDiff).Date))
		{
			return Utils.GetString(defaultStringKey);
		}
		return FormatHourMinuteText(dueDate);
	}

	public static string FormatCalendarDateString(DateTime startDate, DateTime endDate, bool isAllDay)
	{
		if (IsDurationInSameDay(startDate, endDate))
		{
			string text = FormatCalendarDateDesc(startDate);
			if (!isAllDay)
			{
				string text2 = FormatHourMinute(startDate) + " - " + FormatHourMinute(endDate);
				return text + ", " + text2;
			}
			return text;
		}
		if (!isAllDay)
		{
			return FormatDateDuration(startDate, endDate, false);
		}
		bool withYear = startDate.Year != DateTime.Today.Year || endDate.Year != DateTime.Today.Year;
		return FormatShortDate(startDate, withYear) + " - " + FormatShortDate(endDate, withYear);
	}

	public static string GetTaskActivityTimeString(DateTime start, DateTime? due, bool? isAllDay, string timeZone = null, bool isFloat = false)
	{
		return FormatDetailDateString(start, due, isAllDay, useSmartDate: true, timeZone, isFloat, showTimeZone: true);
	}

	public static string GetTaskActivityTimeStringForToast(DateTime start, DateTime? due, bool? isAllDay, string timeZone = null, bool isFloat = false)
	{
		DateTime? dueDate = null;
		if (isAllDay == true)
		{
			return FormatDetailDateString(start, dueDate, isAllDay, useSmartDate: true, null, isFloat: false, showTimeZone: false, showSmartOnly: true);
		}
		if (string.IsNullOrEmpty(timeZone) || timeZone == TimeZoneData.LocalTimeZoneModel?.TimeZoneName || isFloat)
		{
			return FormatDetailDateString(start, dueDate, isAllDay, useSmartDate: true, null, isFloat: false, showTimeZone: false, showSmartOnly: true);
		}
		string text = FormatDetailDateString(start, dueDate, isAllDay, useSmartDate: true, null, isFloat: false, showTimeZone: false, showSmartOnly: true);
		DateTime date = TimeZoneUtils.LocalToTargetTzTime(start, timeZone);
		string text2 = FormatHourMinute(date);
		string text3 = TimeZoneData.GetTimeZoneUtcName(TimeZoneUtils.GetTimeZoneInfo(timeZone), App.Ci.ToString())?.Replace("UTC", "GMT");
		if (date.Date != start.Date)
		{
			string text4 = ((date.Year != DateTime.Today.Year) ? FormatYearMonthDay(date, DateFormatLen.Middle) : FormatMonthDay(date, DateFormatLen.Middle));
			return text + " (" + text4 + ", " + text2 + " " + text3 + ")";
		}
		return text + " (" + text2 + " " + text3 + ")";
	}

	public static string FormatDateStringNoWord(DateTime start, DateTime? due, bool isAllDay)
	{
		if (due.HasValue)
		{
			return ToStr(start, isAllDay) + " - " + ToStr(due.Value, isAllDay);
		}
		return ToStr(start, isAllDay);
		static string ToStr(DateTime date, bool noTime)
		{
			if (noTime)
			{
				return FormatYearMonthDay(date, DateFormatLen.Short);
			}
			return FormatYearMonthDay(date, DateFormatLen.Short) + " " + FormatHourMinute(date);
		}
	}

	public static string FormatDateString(DateTime start, DateTime? due, bool? isAllDay, bool useSmartDate = true, bool showOverDue = true, bool showSmartOnly = false, bool inTomorrowList = false, bool inList = false)
	{
		return FormatDetailDateString(start, due, isAllDay, useSmartDate, null, isFloat: false, showTimeZone: false, showSmartOnly, showOverDue, inTomorrowList, inList);
	}

	public static string FormatQuickAddDateString(DateTime start, DateTime? due, bool? isAllDay)
	{
		return FormatDetailDateString(start, due, isAllDay, useSmartDate: true, null, isFloat: false, showTimeZone: false, showSmartOnly: true);
	}

	private static string FormatDetailDateString(DateTime startDate, DateTime? dueDate, bool? isAllDay, bool useSmartDate = true, string timeZone = null, bool isFloat = false, bool showTimeZone = false, bool showSmartOnly = false, bool showOverDue = true, bool inTomorrowList = false, bool inList = false)
	{
		string text = "";
		if (showTimeZone && isAllDay != true && !string.IsNullOrEmpty(timeZone) && timeZone != TimeZoneData.LocalTimeZoneModel?.TimeZoneName)
		{
			DateTime date = startDate;
			if (!Utils.IsEmptyDate(startDate))
			{
				startDate = TimeZoneUtils.LocalToTargetTzTime(startDate, timeZone);
			}
			if (!Utils.IsEmptyDate(dueDate) && dueDate.HasValue)
			{
				dueDate = TimeZoneUtils.LocalToTargetTzTime(dueDate.Value, timeZone);
			}
			if (!isFloat)
			{
				string text2 = TimeZoneData.GetTimeZoneUtcName(TimeZoneUtils.GetTimeZoneInfo(timeZone), App.Ci.ToString())?.Replace("UTC", "GMT");
				DateTime date2 = startDate;
				if (!Utils.IsEmptyDate(date) && date2.Date != date.Date)
				{
					string text3 = ((date2.Year != DateTime.Today.Year) ? FormatYearMonthDay(date2, DateFormatLen.Short) : FormatMonthDay(date2, DateFormatLen.Short));
					text = " (" + text3 + ", " + FormatHourMinute(date2) + " " + text2 + ")";
				}
				else
				{
					text = " (" + FormatHourMinute(date2) + " " + text2 + ")";
				}
			}
		}
		DateTime? date3 = dueDate;
		if (!Utils.IsEmptyDate(dueDate) && (startDate == dueDate.Value || (isAllDay.HasValue && isAllDay.Value && (startDate.Date == dueDate.Value.Date || startDate.Date == dueDate.Value.Date.AddDays(-1.0)))))
		{
			date3 = null;
		}
		if (Utils.IsEmptyDate(startDate))
		{
			return Utils.GetString("DateAndReminder");
		}
		if (!date3.HasValue || Utils.IsEmptyDate(date3))
		{
			return FormatTime(startDate, isAllDay, useSmartDate, showSmartOnly, showOverDue, inTomorrowList, inList) + text;
		}
		if (IsDurationInSameDay(startDate, date3.Value))
		{
			return FormatTimeDuration(startDate, date3.Value, isAllDay, useSmartDate, showSmartOnly, showOverDue, inTomorrowList, inList) + text;
		}
		return FormatDateDuration(startDate, date3.Value, isAllDay) + text;
	}

	private static bool IsDurationInSameDay(DateTime starDate, DateTime dueDate)
	{
		return starDate.Date == dueDate.Date;
	}

	private static string FormatDateDuration(DateTime startDate, DateTime dueDate, bool? isAllDay)
	{
		bool withYear;
		if (isAllDay == true || (!isAllDay.HasValue && startDate.TimeOfDay == TimeSpan.Zero && dueDate.TimeOfDay == TimeSpan.Zero))
		{
			withYear = startDate.Year != DateTime.Today.Year || dueDate.AddDays(-1.0).Year != DateTime.Today.Year;
			return FormatShortDate(startDate, withYear) + " - " + FormatShortDate(dueDate.AddDays(-1.0), withYear);
		}
		withYear = startDate.Year != DateTime.Today.Year || dueDate.Year != DateTime.Today.Year;
		return FormatShortDate(startDate, withYear) + ", " + FormatHourMinute(startDate) + " - " + FormatShortDate(dueDate, withYear) + ", " + FormatHourMinute(dueDate);
	}

	private static string FormatTimeDuration(DateTime startDate, DateTime dueDate, bool? isAllDay, bool useSmartDate = true, bool showSmartOnly = false, bool showOverDue = true, bool inTomorrowList = false, bool inList = false)
	{
		string text = ((isAllDay.HasValue && isAllDay.Value) ? "" : (FormatHourMinute(startDate) + " - " + FormatHourMinute(dueDate)));
		if (!useSmartDate)
		{
			return FormatShortDate(startDate) + (string.IsNullOrEmpty(text) ? "" : (", " + text));
		}
		if (!showSmartOnly)
		{
			return FormatSmartDateString(startDate, text, showOverDue, inTomorrowList, inList);
		}
		return FormatSmartDateShortString(startDate, text, showOverDue, inTomorrowList, inList);
	}

	public static string FormatHourMinute(DateTime date, bool longTime = false)
	{
		string text = GetTimeFormatString();
		if (longTime && !text.ToLower().Contains("hh"))
		{
			text = text.Replace("h", "hh").Replace("H", "HH");
		}
		return date.ToString(text, App.Ci);
	}

	public static bool IsEqualTime(DateTime a, DateTime b)
	{
		if (a.Hour == b.Hour)
		{
			return a.Minute == b.Minute;
		}
		return false;
	}

	private static string FormatTime(DateTime startDate, bool? isAllDay, bool useSmartDate = true, bool showSmartOnly = false, bool showOverDue = true, bool inTomorrowList = false, bool inList = false)
	{
		string text = ((isAllDay.HasValue && isAllDay.Value) ? "" : FormatHourMinute(startDate));
		if (!useSmartDate)
		{
			return FormatShortDate(startDate) + (string.IsNullOrEmpty(text) ? "" : (", " + text));
		}
		if (!showSmartOnly)
		{
			return FormatSmartDateString(startDate, text, showOverDue, inTomorrowList, inList);
		}
		return FormatSmartDateShortString(startDate, text, showOverDue, inTomorrowList, inList);
	}

	public static string FormatAddTaskDateString(DateTime date)
	{
		switch ((int)(date.Date - DateTime.Today.Date).TotalDays)
		{
		case -1:
			return Utils.GetString("PublicYesterday");
		case 0:
			return Utils.GetString("Today");
		case 1:
			return Utils.GetString("Tomorrow");
		case 2:
		case 3:
		case 4:
		case 5:
		case 6:
			return date.ToString("ddd", App.Ci);
		default:
			return FormatShortDate(date);
		}
	}

	public static string FormatSmartDateShortString(DateTime date, string timeText, bool showOverDue = true, bool inTomorrowList = false, bool inList = false)
	{
		int num = (int)(date.Date - DateTime.Today.Date).TotalDays;
		double totalDays = (Utils.GetWeekStart(DateTime.Today) - DateTime.Today.Date).TotalDays;
		bool flag = string.IsNullOrEmpty(timeText);
		string text = (flag ? "" : ", ");
		switch (num)
		{
		case -1:
			return Utils.GetString("PublicYesterday") + text + timeText;
		case 0:
			return ((!inList || flag) ? (Utils.GetString("Today") + text) : "") + timeText;
		case 1:
			return ((!inTomorrowList || flag) ? (Utils.GetString("Tomorrow") + text) : "") + timeText;
		case 2:
			if (Utils.IsCn())
			{
				return Utils.GetString("In2Days") + text + timeText;
			}
			break;
		}
		if (inList)
		{
			return FormatShortDate(date) + text + timeText;
		}
		if ((double)num >= totalDays && num < -1)
		{
			return timeText + (showOverDue ? (text + DayDiffDescription(num)) : "");
		}
		if (num >= 2 && (double)num < totalDays + 7.0)
		{
			return date.ToString("ddd", App.Ci) + text + timeText;
		}
		if ((double)num >= totalDays + 7.0 && (double)num < totalDays + 14.0)
		{
			return FormatNextWeekDay(date) + text + timeText;
		}
		return FormatShortDate(date) + text + timeText;
	}

	public static string FormatSmartDateString(DateTime date, string timeText, bool showOverDue = true, bool inTomorrowList = false, bool inList = false)
	{
		int value = date.Year - DateTime.Today.Year;
		int num = (int)(date.Date - DateTime.Today.Date).TotalDays;
		bool flag = string.IsNullOrEmpty(timeText);
		string text = (flag ? "" : ", ");
		string text2;
		if (Math.Abs(value) > 1)
		{
			text2 = timeText + (showOverDue ? (text + DayDiffDescription(num)) : "");
		}
		else
		{
			double totalDays = (Utils.GetWeekStart(DateTime.Today) - DateTime.Today.Date).TotalDays;
			if (Math.Abs(num) > 30)
			{
				text2 = timeText + (showOverDue ? (text + DayDiffDescription(num)) : "");
			}
			else
			{
				switch (num)
				{
				case -1:
					text2 = Utils.GetString("PublicYesterday") + text + timeText;
					break;
				case 0:
					text2 = ((!inList || flag) ? (Utils.GetString("Today") + text) : "") + timeText;
					break;
				case 1:
					text2 = ((!inTomorrowList || flag) ? (Utils.GetString("Tomorrow") + text) : "") + timeText;
					break;
				case 2:
					if (Utils.IsCn())
					{
						text2 = Utils.GetString("In2Days") + text + timeText;
						break;
					}
					goto default;
				default:
					text2 = ((num >= 2 && (double)num < totalDays + 7.0) ? (date.ToString("ddd", App.Ci) + text + timeText) : ((!((double)num >= totalDays + 7.0) || !((double)num < totalDays + 14.0)) ? (timeText + (showOverDue ? (text + DayDiffDescription(num)) : "")) : (FormatNextWeekDay(date) + text + timeText)));
					break;
				}
			}
		}
		if (string.IsNullOrEmpty(text2))
		{
			return FormatShortDate(date);
		}
		return FormatShortDate(date) + ", " + text2;
	}

	private static string DiffDescription(int count, string single, string plur, string unit = "month")
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>
		{
			["one"] = Utils.GetString(single),
			["other"] = Utils.GetString(plur)
		};
		if (Utils.IsRussia() && unit == "month")
		{
			dictionary["one"] = "месяц";
			dictionary["many"] = "месяца";
			dictionary["other"] = "месяцев";
		}
		return DiffDescription(count, dictionary);
	}

	private static string DayDiffDescription(int count)
	{
		return string.Format(Utils.GetString((count < 0) ? "ndOverdue" : "ndLeft"), Math.Abs(count));
	}

	private static string DiffDescription(int count, IReadOnlyDictionary<string, string> pluralDict)
	{
		if (!pluralDict.ContainsKey("other"))
		{
			return string.Empty;
		}
		string key;
		switch (Math.Abs(count))
		{
		case 1:
			key = "one";
			break;
		case 2:
		case 3:
		case 4:
			key = "many";
			break;
		default:
			key = "other";
			break;
		}
		if (!pluralDict.ContainsKey(key))
		{
			key = "other";
		}
		string arg = pluralDict[key];
		string arg2 = Utils.GetString((count < 0) ? "TimeBefore" : "TimeAfter").ToLower();
		return string.Format(Utils.GetString("DeltaTimeFormat"), Math.Abs(count), arg, arg2);
	}

	public static bool IsOutDated(DateTime? startDate, DateTime? dueDate, bool? isAllDay)
	{
		if (dueDate.HasValue && !Utils.IsEmptyDate(dueDate) && isAllDay.HasValue && isAllDay.Value)
		{
			DateTime value = dueDate.Value.AddSeconds(-1.0);
			if (startDate.HasValue && value.Date < startDate.Value.Date)
			{
				value = startDate.Value;
			}
			return IsOutDated(startDate, value);
		}
		return IsOutDated(startDate, dueDate);
	}

	private static bool IsOutDated(DateTime? startDate, DateTime? dueDate)
	{
		if (!startDate.HasValue || Utils.IsEmptyDate(startDate))
		{
			return true;
		}
		if (!dueDate.HasValue || Utils.IsEmptyDate(dueDate))
		{
			if (startDate.Value.Date < DateTime.Now.Date)
			{
				return true;
			}
		}
		else if (startDate.Value.Date < DateTime.Now.Date && dueDate.Value.AddSeconds(-1.0).Date < DateTime.Today)
		{
			return true;
		}
		return false;
	}

	public static CreateTimeCategory GetCreateTimeSectionCategory(DateTime time)
	{
		DateTime today = DateTime.Today;
		if (time.Date == today)
		{
			return CreateTimeCategory.Today;
		}
		if (time.Date == today.AddDays(-1.0))
		{
			return CreateTimeCategory.Yesterday;
		}
		if (time.Date >= today.AddDays(-7.0))
		{
			return CreateTimeCategory.LastWeek;
		}
		if (time.Date >= today.AddDays(-30.0))
		{
			return CreateTimeCategory.LastMonth;
		}
		return CreateTimeCategory.Earlier;
	}

	public static string GetCreateTimeSectionEntityId(CreateTimeCategory category)
	{
		return category switch
		{
			CreateTimeCategory.LastWeek => "p7ds", 
			CreateTimeCategory.LastMonth => "p30ds", 
			_ => category.ToString().ToLower(), 
		};
	}

	public static DateSectionCategory GetSectionCategory(DateTime? startDate, DateTime? dueDate, bool? isAllDay)
	{
		if (!startDate.HasValue || Utils.IsEmptyDate(startDate))
		{
			return DateSectionCategory.NoDate;
		}
		if (IsOutDated(startDate, dueDate, isAllDay))
		{
			return DateSectionCategory.OutDated;
		}
		if (IsDateInToday(startDate, dueDate, isAllDay))
		{
			return DateSectionCategory.Today;
		}
		if (IsDateInTomorrow(startDate, dueDate, isAllDay))
		{
			return DateSectionCategory.Tomorrow;
		}
		if (!IsDateInWeek(startDate, dueDate, isAllDay))
		{
			return DateSectionCategory.Future;
		}
		return DateSectionCategory.ThisWeek;
	}

	public static DateTime? GetDateSectionDate(DateSectionCategory section)
	{
		return section switch
		{
			DateSectionCategory.NoDate => null, 
			DateSectionCategory.OutDated => DateTime.Today.AddDays(-1.0), 
			DateSectionCategory.Today => DateTime.Today, 
			DateSectionCategory.Tomorrow => DateTime.Today.AddDays(1.0), 
			DateSectionCategory.ThisWeek => DateTime.Today.AddDays(2.0), 
			DateSectionCategory.Future => null, 
			_ => null, 
		};
	}

	public static bool IsDateInToday(DateTime? start, DateTime? due, bool? isAllDay)
	{
		return IsDateInSpan(DateTime.Today.Date, DateTime.Today.Date.AddDays(1.0), start, due, isAllDay);
	}

	private static bool IsDateInTomorrow(DateTime? start, DateTime? due, bool? isAllDay)
	{
		return IsDateInSpan(DateTime.Today.Date.AddDays(1.0), DateTime.Today.Date.AddDays(2.0), start, due, isAllDay);
	}

	private static bool IsDateInWeek(DateTime? start, DateTime? due, bool? isAllDay)
	{
		return IsDateInSpan(DateTime.Today.Date.AddDays(1.0), DateTime.Today.Date.AddDays(8.0), start, due, isAllDay);
	}

	private static bool IsDateInSpan(DateTime spanStart, DateTime spanEnd, DateTime? start, DateTime? due, bool? isAllDay)
	{
		if (!start.HasValue || Utils.IsEmptyDate(start))
		{
			return false;
		}
		if (start.Value >= spanStart && start.Value < spanEnd)
		{
			return true;
		}
		if (!due.HasValue || Utils.IsEmptyDate(due))
		{
			if (start.Value >= spanStart)
			{
				return start.Value < spanEnd;
			}
			return false;
		}
		if (!Utils.IsEmptyDate(due))
		{
			if (isAllDay.HasValue && isAllDay.Value)
			{
				if (start.Value == due.Value)
				{
					due = start.Value.Date.AddDays(1.0);
				}
				due = due.Value.AddSeconds(-1.0);
			}
			if (start.Value < spanEnd)
			{
				return due.Value > spanStart;
			}
			return false;
		}
		return false;
	}

	public static DateTime? TryParseTime(string text, string format)
	{
		try
		{
			return DateTime.ParseExact(text, format, CultureInfo.InvariantCulture);
		}
		catch (Exception)
		{
			return null;
		}
	}

	public static string GetLunarDay(DateTime date)
	{
		if (Utils.IsEmptyDate(date))
		{
			return string.Empty;
		}
		return GetChineseDay(new ChineseLunisolarCalendar().GetDayOfMonth(date));
	}

	public static string GetLunarMonthDay(DateTime date)
	{
		if (Utils.IsEmptyDate(date))
		{
			return string.Empty;
		}
		ChineseLunisolarCalendar chineseLunisolarCalendar = new ChineseLunisolarCalendar();
		int num = chineseLunisolarCalendar.GetMonth(date);
		int dayOfMonth = chineseLunisolarCalendar.GetDayOfMonth(date);
		int year = chineseLunisolarCalendar.GetYear(date);
		if (chineseLunisolarCalendar.IsLeapYear(year))
		{
			int leapMonth = chineseLunisolarCalendar.GetLeapMonth(year);
			if (num >= leapMonth)
			{
				num--;
			}
		}
		return GetChineseMonth(num) + GetChineseDay(dayOfMonth);
	}

	private static string GetChineseMonth(int month)
	{
		if (ChineseMonthDict.ContainsKey(month))
		{
			return ChineseMonthDict[month];
		}
		return month.ToString();
	}

	private static string GetChineseDay(int dayOfMonth)
	{
		if (ChineseNumDict.ContainsKey(dayOfMonth))
		{
			return ChineseNumDict[dayOfMonth];
		}
		return dayOfMonth.ToString();
	}

	public static DateTime ConvertTimeZone(DateTime dateTime, string timeZone)
	{
		try
		{
			if (string.IsNullOrEmpty(timeZone))
			{
				return dateTime;
			}
			if (!string.IsNullOrEmpty(timeZone))
			{
				TimeZoneInfo timeZoneInfo = TimeZoneUtils.GetTimeZoneInfo(timeZone);
				return TimeZoneInfo.ConvertTime(dateTime, timeZoneInfo);
			}
			return dateTime;
		}
		catch (Exception)
		{
			return dateTime;
		}
	}

	public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
	{
		DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
		return TimeZone.CurrentTimeZone.ToLocalTime(dateTime.AddSeconds(unixTimeStamp));
	}

	public static double DateTimeToUnixTimestamp(DateTime dateTime)
	{
		DateTime dateTime2 = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
		return (double)(dateTime.ToUniversalTime() - dateTime2).Ticks / 10000000.0;
	}

	public static bool IsTaskInDay(DateTime targetDate, DateTime? startDate, DateTime? dueDate, bool? isAllDay)
	{
		if (isAllDay == true)
		{
			return false;
		}
		if (!isAllDay.HasValue && startDate.HasValue && startDate.Value.TimeOfDay == TimeSpan.Zero && dueDate.HasValue && dueDate.Value.TimeOfDay == TimeSpan.Zero)
		{
			return false;
		}
		if (startDate.HasValue)
		{
			if (!dueDate.HasValue || dueDate == startDate)
			{
				return startDate.Value.Date == targetDate.Date;
			}
			double totalHours = (dueDate.Value - startDate.Value).TotalHours;
			double totalHours2 = (dueDate.Value - targetDate.Date).TotalHours;
			if (totalHours <= 24.0 && totalHours2 > 0.0)
			{
				if (!(startDate.Value.Date == targetDate.Date))
				{
					return dueDate.Value.Date == targetDate.Date;
				}
				return true;
			}
		}
		return false;
	}

	public static void CheckIfTomorrowWronglySet(TimeData date)
	{
		if (date != null && !date.DueDate.HasValue && date.StartDate.HasValue && date.StartDate.Value.Date == DateTime.Today.AddDays(1.0) && IsPreDawnNow())
		{
			Utils.Toast(string.Format(Utils.GetString("TomorrowSetHint"), DateTime.Now.ToString("HH:mm")));
		}
	}

	private static bool IsPreDawnNow()
	{
		return (DateTime.Now - DateTime.Today).TotalMinutes <= 180.0;
	}

	public static int GetWeekNumOfYear(DateTime date)
	{
		if (!Utils.IsEmptyDate(date))
		{
			if (string.IsNullOrEmpty(LocalSettings.Settings.StartWeekOfYear))
			{
				CultureInfo currentCulture = new Thread((ThreadStart)delegate
				{
				}).CurrentCulture;
				DateTimeFormatInfo dateTimeFormatInfo = currentCulture.DateTimeFormat ?? App.Ci.DateTimeFormat;
				return dateTimeFormatInfo.Calendar.GetWeekOfYear(date, Utils.IsCn(currentCulture.Name) ? CalendarWeekRule.FirstFourDayWeek : dateTimeFormatInfo.CalendarWeekRule, LocalSettings.Settings.WeekStartFromType);
			}
			DateTime dateTime = new DateTime(date.Year, 1, 1, 0, 0, 0, 0);
			string startWeekOfYear = LocalSettings.Settings.StartWeekOfYear;
			if (startWeekOfYear != null && startWeekOfYear.Length == 4 && int.TryParse(startWeekOfYear.Substring(0, 2), out var result) && int.TryParse(startWeekOfYear.Substring(2, 2), out var result2))
			{
				dateTime = dateTime.AddMonths(result - 1).AddDays(result2 - 1);
			}
			if (date.Date < dateTime.Date)
			{
				DateTime weekStart = Utils.GetWeekStart(date);
				if (weekStart < dateTime && weekStart.AddDays(6.0) > dateTime)
				{
					return 1;
				}
				dateTime = dateTime.AddYears(-1);
			}
			return GetWeekNum(date, dateTime);
		}
		return 0;
	}

	public static int GetWeekNum(DateTime date, DateTime startDate)
	{
		int num = (int)(date.Date - startDate.Date).TotalDays + 1;
		DayOfWeek weekStartFromType = LocalSettings.Settings.WeekStartFromType;
		int num2 = (startDate.DayOfWeek - weekStartFromType + 7) % 7;
		return (num + num2 + 6) / 7;
	}

	public static string GetWeekNumStrOfYear(DateTime date)
	{
		int weekNumOfYear = GetWeekNumOfYear(date);
		if (weekNumOfYear <= 0)
		{
			return "";
		}
		return string.Format(Utils.GetString("WeekNum"), weekNumOfYear);
	}

	public static int GetDateNum(DateTime date)
	{
		return date.Year * 10000 + date.Month * 100 + date.Day;
	}

	private static string GetLeftTimeString(int leftMinutes)
	{
		return ((leftMinutes >= 60) ? (leftMinutes / 60) : leftMinutes) + (Utils.IsCn() ? "" : " ") + Utils.GetString((leftMinutes < 60) ? ((leftMinutes > 1) ? "PublicUpMinutes" : "PublicUpMinute") : ((leftMinutes / 60 > 1) ? "PublicUpHours" : "PublicUpHour"));
	}

	public static string FormatCountDownDateString(DateTime startDate, DateTime? dueDate, bool isAllDay, bool showDetail)
	{
		if (startDate >= DateTime.Now.AddMinutes(-1.0))
		{
			if (startDate.Date > DateTime.Today && (isAllDay || startDate >= DateTime.Now.AddHours(12.0)))
			{
				int num = (int)(startDate.Date - DateTime.Today).TotalDays;
				return string.Format(Utils.GetString("DaysLeft"), num + (Utils.IsCn() ? "" : " ") + Utils.GetString((num > 1) ? "PublicDays" : "PublicDay"));
			}
			int num2 = (int)Math.Ceiling((startDate - DateTime.Now).TotalMinutes);
			if (num2 != 0)
			{
				return string.Format(Utils.GetString("DaysLeft"), GetLeftTimeString(num2));
			}
			return Utils.GetString("Now");
		}
		if (isAllDay && dueDate.HasValue)
		{
			dueDate = dueDate.Value.AddMinutes(-1.0);
			if (startDate.Date == dueDate.Value.Date)
			{
				dueDate = null;
			}
		}
		if (dueDate.HasValue)
		{
			if (dueDate.Value >= DateTime.Now.AddMinutes(-1.0))
			{
				if (!showDetail)
				{
					if (isAllDay || (dueDate.Value.Date != DateTime.Today && startDate.Date != DateTime.Today))
					{
						return Utils.GetString("Today");
					}
					return Utils.GetString("Now");
				}
				if (isAllDay || (dueDate.Value.Date != DateTime.Today && startDate.Date != DateTime.Today))
				{
					int num3 = (int)(DateTime.Today - startDate.Date).TotalDays;
					int num4 = (int)(dueDate.Value.Date - DateTime.Today).TotalDays;
					return string.Format(Utils.GetString("DaysPast"), num3 + (Utils.IsCn() ? "" : " ") + Utils.GetString((num3 > 1) ? "PublicDays" : "PublicDay"), num4 + (Utils.IsCn() ? "" : " ") + Utils.GetString((num4 > 1) ? "PublicDays" : "PublicDay"));
				}
				string arg;
				if (startDate.Date < DateTime.Today && DateTime.Now >= startDate.AddHours(12.0))
				{
					int num5 = (int)(DateTime.Today - startDate.Date).TotalDays;
					arg = num5 + (Utils.IsCn() ? "" : " ") + Utils.GetString((num5 > 1) ? "PublicDays" : "PublicDay");
				}
				else
				{
					int num6 = (int)(DateTime.Now - startDate).TotalMinutes;
					if (num6 == 0)
					{
						return Utils.GetString("Now");
					}
					arg = GetLeftTimeString(num6);
				}
				string arg2;
				if (dueDate.Value.Date > DateTime.Today && dueDate.Value >= DateTime.Now.AddHours(12.0))
				{
					int num7 = (int)(dueDate.Value.Date - DateTime.Today).TotalDays;
					arg2 = num7 + (Utils.IsCn() ? "" : " ") + Utils.GetString((num7 > 1) ? "PublicDays" : "PublicDay");
				}
				else
				{
					int num8 = (int)Math.Ceiling((dueDate.Value - DateTime.Now).TotalMinutes);
					if (num8 == 0)
					{
						return Utils.GetString("Now");
					}
					arg2 = GetLeftTimeString(num8);
				}
				return string.Format(Utils.GetString("DaysPast"), arg, arg2);
			}
			if (isAllDay || (dueDate.Value.Date != DateTime.Today && Math.Abs((dueDate.Value - DateTime.Now).TotalHours) >= 12.0))
			{
				int num9 = (int)(DateTime.Today - dueDate.Value.Date).TotalDays;
				return string.Format(Utils.GetString((num9 == 0) ? "DaysLeft" : "DaysOver"), num9 + (Utils.IsCn() ? "" : " ") + Utils.GetString((num9 > 1) ? "PublicDays" : "PublicDay"));
			}
			int num10 = (int)(DateTime.Now - dueDate.Value).TotalMinutes;
			if (num10 != 0)
			{
				return string.Format(Utils.GetString("DaysOver"), GetLeftTimeString(num10));
			}
			return Utils.GetString("Now");
		}
		if (startDate.Date == DateTime.Today && isAllDay)
		{
			return Utils.GetString("Today");
		}
		if (isAllDay || (startDate.Date < DateTime.Today && Math.Abs((startDate - DateTime.Now).TotalHours) >= 12.0))
		{
			int num11 = (int)(DateTime.Today - startDate.Date).TotalDays;
			return string.Format(Utils.GetString("DaysOver"), num11 + (Utils.IsCn() ? "" : " ") + Utils.GetString((num11 > 1) ? "PublicDays" : "PublicDay"));
		}
		int num12 = (int)(DateTime.Now - startDate).TotalMinutes;
		if (num12 != 0)
		{
			return string.Format(Utils.GetString("DaysOver"), GetLeftTimeString(num12));
		}
		return Utils.GetString("Now");
	}

	public static bool CountDownOutDate(DateTime? startDate, DateTime? dueDate, bool isAllDay)
	{
		if (!startDate.HasValue)
		{
			return false;
		}
		if (isAllDay)
		{
			if ((!dueDate.HasValue || startDate.Value.Date == dueDate.Value.Date) && startDate.Value.Date < DateTime.Today)
			{
				return true;
			}
			if (dueDate.HasValue && dueDate.Value.AddMinutes(-1.0).Date < DateTime.Today)
			{
				return true;
			}
		}
		else
		{
			if (!dueDate.HasValue && startDate.Value < DateTime.Now.AddMinutes(-1.0))
			{
				return true;
			}
			if (dueDate.HasValue && dueDate.Value < DateTime.Now.AddMinutes(-1.0))
			{
				return true;
			}
		}
		return false;
	}

	public static DateTime GetLocalTime(DateTime date)
	{
		return TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(date.Year, date.Month, date.Day, date.Hour, date.Minute, date.Second, DateTimeKind.Utc));
	}

	public static bool IsSameTime(DateTime? left, DateTime? right)
	{
		if (!left.HasValue && !right.HasValue)
		{
			return true;
		}
		if (left.HasValue && right.HasValue)
		{
			if (left.Value.Hour == right.Value.Hour)
			{
				return left.Value.Minute == right.Value.Minute;
			}
			return false;
		}
		return false;
	}

	public static bool IsSameDate(DateTime? left, DateTime? right)
	{
		if (!left.HasValue && !right.HasValue)
		{
			return true;
		}
		if (left.HasValue && right.HasValue)
		{
			return left.Value.Date == right.Value.Date;
		}
		return false;
	}

	public static DateTime ParseDateTime(string str, string format = "yyyyMMdd")
	{
		DateTime result = default(DateTime);
		DateTime.TryParseExact(str, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out result);
		return result;
	}

	public static DateTime IntToDateTime(int timeInt)
	{
		return new DateTime(timeInt / 10000, timeInt % 10000 / 100, timeInt % 100);
	}

	public static int DateTimeToInt(DateTime time)
	{
		return 0 + time.Year * 10000 + time.Month * 100 + time.Day;
	}

	public static bool IsWeekEnds(DateTime startDate)
	{
		if (startDate.DayOfWeek != DayOfWeek.Sunday)
		{
			return startDate.DayOfWeek == DayOfWeek.Saturday;
		}
		return true;
	}

	public static bool CheckDateInSpan(DateTime checkStart, DateTime checkEnd, DateTime spanStart, DateTime spanEnd)
	{
		if ((!(checkStart <= spanStart) || !(checkEnd >= spanEnd)) && (!(checkStart >= spanStart) || !(checkStart <= spanEnd)))
		{
			if (spanStart <= checkEnd)
			{
				return checkEnd <= spanEnd;
			}
			return false;
		}
		return true;
	}

	public static DateTime GetDateByWeek(DateTime startDate, int week, int weekday)
	{
		DateTime weekStart = Utils.GetWeekStart(startDate);
		int num = weekday % 7;
		int weekStartFromType = (int)LocalSettings.Settings.WeekStartFromType;
		int num2 = (num - weekStartFromType + 7) % 7;
		return weekStart.AddDays((week - 1) * 7 + num2);
	}

	public static (int, int) GetHourAndMinuteInString(string lessonStart)
	{
		if (string.IsNullOrEmpty(lessonStart))
		{
			return (0, 0);
		}
		string[] array = lessonStart.Split(':');
		if (array.Length != 2)
		{
			return (0, 0);
		}
		if (int.TryParse(array[0], out var result) && int.TryParse(array[1], out var result2))
		{
			return (result, result2);
		}
		return (0, 0);
	}

	public static string GetWeekTextByWeekDay(int weekDay)
	{
		switch (weekDay)
		{
		case 1:
			return Utils.GetString("Mon");
		case 2:
			return Utils.GetString("Tue");
		case 3:
			return Utils.GetString("Wed");
		case 4:
			return Utils.GetString("Thu");
		case 5:
			return Utils.GetString("Fri");
		case 6:
			return Utils.GetString("Sat");
		case 0:
		case 7:
			return Utils.GetString("Sun");
		default:
			return null;
		}
	}

	public static string GetShortWeekTextByWeekDay(int weekDay)
	{
		switch (weekDay)
		{
		case 1:
			return Utils.GetString("Mo");
		case 2:
			return Utils.GetString("Tu");
		case 3:
			return Utils.GetString("We");
		case 4:
			return Utils.GetString("Th");
		case 5:
			return Utils.GetString("Fr");
		case 6:
			return Utils.GetString("Sa");
		case 0:
		case 7:
			return Utils.GetString("Su");
		default:
			return null;
		}
	}

	public static string GetTimeText(DateTime startTime)
	{
		return startTime.ToString(GetTimeFormatString(), App.Ci);
	}

	public static string GetSystemTimeFormat()
	{
		string text = _systemHourFormat;
		if (string.IsNullOrEmpty(text))
		{
			text = (_systemHourFormat = (new CultureInfo(LocalSettings.Settings.UserChooseLanguage).DateTimeFormat.ShortTimePattern.Contains("H") ? "24Hour" : "12Hour"));
			Task.Run(async delegate
			{
				await Task.Delay(2000);
				_systemHourFormat = null;
			});
		}
		return text;
	}

	public static bool CheckSystemFormatPadZero()
	{
		string shortTimePattern = CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern;
		if (!string.IsNullOrEmpty(shortTimePattern))
		{
			return shortTimePattern.ToLower().Contains("hh");
		}
		return true;
	}

	public static string GetLunarText(DateTime date, bool showWeek)
	{
		string text = WeekName();
		if (Utils.IsTickPackage())
		{
			if (Utils.IsJp())
			{
				HolidayModel holidayModel = HolidayManager.GetCacheHolidays().Find((HolidayModel item) => item.date.Date.Equals(date.Date));
				if (holidayModel != null && holidayModel.region == HolidayRegion.jp && LocalSettings.Settings.EnableHoliday)
				{
					return holidayModel.name;
				}
			}
			if (!string.IsNullOrEmpty(text))
			{
				return text;
			}
			AlternativeCalendar alternativeCalendar = LocalSettings.Settings.UserPreference.alternativeCalendar;
			if (alternativeCalendar != null)
			{
				string text2 = (string.Equals(alternativeCalendar.calendar, "lunar") ? GetChineseLunarText(date, "", withDay: false) : GetDisplayLunarText(date, alternativeCalendar.calendar));
				if (!string.IsNullOrEmpty(text2))
				{
					return text2;
				}
			}
		}
		else if (LocalSettings.Settings.EnableLunar)
		{
			string chineseLunarText = GetChineseLunarText(date, text);
			if (!string.IsNullOrEmpty(chineseLunarText))
			{
				return chineseLunarText;
			}
		}
		return text;
		string WeekName()
		{
			if (showWeek && (date.DayOfWeek == LocalSettings.Settings.WeekStartFromType || (!LocalSettings.Settings.ShowCalWeekend && date.DayOfWeek == DayOfWeek.Monday)))
			{
				return GetWeekNumStrOfYear(date.AddDays(LocalSettings.Settings.ShowCalWeekend ? 6 : 4));
			}
			return string.Empty;
		}
	}

	private static string GetDisplayLunarText(DateTime date, string alternativeCalendar)
	{
		if (CalendarConverter.IsValidCalendarType(alternativeCalendar))
		{
			CalendarDisplay calendarDisplay = new CalendarDisplay(date, alternativeCalendar);
			if (!calendarDisplay.OutLimit)
			{
				return calendarDisplay.DisplayText();
			}
		}
		return string.Empty;
	}

	private static string GetChineseLunarText(DateTime date, string defaultText = "", bool withDay = true)
	{
		LunarUtils.ChineseCalendar chineseCalendar = new LunarUtils.ChineseCalendar(date);
		if (Utils.IsEmptyDate(chineseCalendar.Date))
		{
			return defaultText;
		}
		try
		{
			if (withDay)
			{
				if (!string.IsNullOrEmpty(chineseCalendar.ChineseCalendarHoliday) && (chineseCalendar.Date.Month != 10 || chineseCalendar.Date.Day != 1))
				{
					string chineseCalendarHoliday = chineseCalendar.ChineseCalendarHoliday;
					if (chineseCalendarHoliday.Contains("除夕") || chineseCalendarHoliday.Contains("春节") || chineseCalendarHoliday.Contains("中秋") || chineseCalendarHoliday.Contains("端午"))
					{
						return chineseCalendarHoliday;
					}
				}
				if (LocalSettings.Settings.EnableHoliday)
				{
					if (!string.IsNullOrEmpty(chineseCalendar.DateHoliday))
					{
						return chineseCalendar.DateHoliday;
					}
					if (!string.IsNullOrEmpty(chineseCalendar.WeekDayHoliday))
					{
						return chineseCalendar.WeekDayHoliday;
					}
				}
				if (!string.IsNullOrEmpty(chineseCalendar.ChineseCalendarHoliday))
				{
					return chineseCalendar.ChineseCalendarHoliday;
				}
				string chineseTwentyFourDay = chineseCalendar.ChineseTwentyFourDay;
				if (!string.IsNullOrEmpty(chineseTwentyFourDay) && (LocalSettings.Settings.EnableLunar || (LocalSettings.Settings.EnableHoliday && chineseTwentyFourDay.Contains("清明"))))
				{
					return chineseTwentyFourDay;
				}
			}
			if (!string.IsNullOrEmpty(defaultText))
			{
				return defaultText;
			}
			if (chineseCalendar.ChineseDay == 1)
			{
				return chineseCalendar.ChineseMonthString;
			}
			return chineseCalendar.ChineseDayString;
		}
		catch (Exception)
		{
			return defaultText;
		}
	}

	public static DateTime GetMonthStart(DateTime date)
	{
		if (date.Day == 1)
		{
			return date;
		}
		return date.Date.AddDays(1 - date.Day);
	}

	public static DateTime GetMonthEnd(DateTime date)
	{
		return GetMonthStart(date).AddMonths(1).AddDays(-1.0);
	}

	public static DateTime GetCurrentMonthDate(DateTime start, DateTime end)
	{
		DateTime result = start.Date.AddDays(1 - start.Day);
		DateTime dateTime = end.Date.AddDays(1 - end.Day);
		if (result.AddMonths(1) == dateTime)
		{
			int day = end.Day;
			if ((int)(end.Date - start.Date).TotalDays + 1 - day <= day)
			{
				return dateTime;
			}
			return result;
		}
		if (!(result.AddMonths(2) == dateTime))
		{
			return result;
		}
		return result.AddMonths(1);
	}

	public static int GetWeekendsCountInSpan(DateTime start, DateTime end, bool checkEnd)
	{
		int days = (end - start).Days;
		bool flag = days < 0;
		int num = (int)(flag ? end.DayOfWeek : start.DayOfWeek);
		if (num == 0)
		{
			num = 7;
		}
		int num2 = 0;
		days = Math.Abs(days) + ((!checkEnd) ? (-1) : 0);
		for (int i = 0; i <= days; i++)
		{
			if (num > 5)
			{
				num2++;
			}
			num = ((num == 7) ? 1 : (num + 1));
		}
		if (!flag)
		{
			return num2;
		}
		return -1 * num2;
	}

	public static string GetZhTimeText(DateTime date)
	{
		if (date.Hour <= 5)
		{
			return "凌晨";
		}
		if (date.Hour < 12)
		{
			return "上午";
		}
		if (date.Hour == 12)
		{
			return "中午";
		}
		if (date.Hour <= 17)
		{
			return "下午";
		}
		if (date.Hour < 24)
		{
			return "晚上";
		}
		return "";
	}

	public static string GetZhTime(DateTime date)
	{
		int num = ((date.Hour % 12 == 0) ? 12 : (date.Hour % 12));
		int minute = date.Minute;
		string text = ((minute > 0) ? $"{num}:{minute}" : (num + "点"));
		if (date.Hour <= 5)
		{
			return "凌晨" + text;
		}
		if (date.Hour < 12)
		{
			return "上午" + text;
		}
		if (date.Hour == 12)
		{
			return "正午";
		}
		if (date.Hour <= 17)
		{
			return "下午" + text;
		}
		if (date.Hour < 24)
		{
			return "晚上" + text;
		}
		return "";
	}

	public static string GetAmPmTimeText(DateTime date)
	{
		int num = ((date.Hour % 12 == 0) ? 12 : (date.Hour % 12));
		int minute = date.Minute;
		if (date.Hour < 12)
		{
			if (minute <= 0)
			{
				return num + " AM";
			}
			return $"{num}:{minute} AM";
		}
		if (date.Hour == 12)
		{
			return Utils.GetString("Noon");
		}
		if (date.Hour < 24)
		{
			if (minute <= 0)
			{
				return num + " PM";
			}
			return $"{num}:{minute} PM";
		}
		return "";
	}

	public static bool DatesBetween(DateTime min, DateTime max, DateTime? start, DateTime? end)
	{
		if (!start.HasValue)
		{
			return false;
		}
		DateTime? dateTime = start;
		DateTime dateTime2 = min;
		if (dateTime.HasValue && dateTime.GetValueOrDefault() >= dateTime2 && start < max)
		{
			return true;
		}
		if (end.HasValue)
		{
			if (end.Value > min && end.Value <= max)
			{
				return true;
			}
			dateTime = start;
			dateTime2 = min;
			if (dateTime.HasValue && dateTime.GetValueOrDefault() < dateTime2 && end.Value >= max)
			{
				return true;
			}
		}
		return false;
	}

	public static string FormatHM(DateTime date)
	{
		return date.ToString((LocalSettings.Settings.TimeFormat != "24Hour") ? "hh':'mm" : "H':'mm");
	}

	public static string GetChineseYearName(int year)
	{
		string[] array = new string[10] { "甲", "乙", "丙", "丁", "戊", "己", "庚", "辛", "壬", "癸" };
		string[] array2 = new string[12]
		{
			"子", "丑", "寅", "卯", "辰", "巳", "午", "未", "申", "酉",
			"戌", "亥"
		};
		_ = new string[12]
		{
			"鼠", "牛", "虎", "兔", "龙", "蛇", "马", "羊", "猴", "鸡",
			"狗", "猪"
		};
		int num = (year - 4) % 10;
		int num2 = (year - 4) % 12;
		if (num < 0)
		{
			num += 10;
		}
		if (num2 < 0)
		{
			num2 += 12;
		}
		return array[num] + array2[num2] + "年";
	}

	public static string GetChineseMonthName(int month)
	{
		string[] array = new string[12]
		{
			"正月", "二月", "三月", "四月", "五月", "六月", "七月", "八月", "九月", "十月",
			"冬月", "腊月"
		};
		if (month >= 1 && month <= 12)
		{
			return array[month - 1];
		}
		return "月";
	}

	public static string GetChineseDayName(int day)
	{
		switch (day)
		{
		case 10:
			return "初十";
		case 20:
			return "二十";
		case 30:
			return "三十";
		default:
		{
			string[] array = new string[4] { "初", "十", "廿", "三十" };
			string[] array2 = new string[10] { "十", "一", "二", "三", "四", "五", "六", "七", "八", "九" };
			int num = day / 10;
			int num2 = day % 10;
			if (day <= 10)
			{
				return "初" + array2[num2];
			}
			if (num2 == 0)
			{
				return array[num];
			}
			return array[num] + array2[num2];
		}
		}
	}

	public static DateFormatType GetSysDateFormatType()
	{
		string shortDatePattern = App.SystemCulture.DateTimeFormat.ShortDatePattern;
		List<char> list = new List<char>();
		for (int i = 0; i < shortDatePattern.Length; i++)
		{
			switch (shortDatePattern[i])
			{
			case 'Y':
			case 'y':
				if (!list.Contains('Y'))
				{
					list.Add('Y');
				}
				break;
			case 'M':
			case 'm':
				if (!list.Contains('M'))
				{
					list.Add('M');
				}
				break;
			case 'D':
			case 'd':
				if (!list.Contains('D'))
				{
					list.Add('D');
				}
				break;
			}
		}
		if (list.Count < 3)
		{
			shortDatePattern = App.SystemCulture.DateTimeFormat.LongDatePattern;
			list.Clear();
			for (int j = 0; j < shortDatePattern.Length; j++)
			{
				switch (shortDatePattern[j])
				{
				case 'Y':
				case 'y':
					if (!list.Contains('Y'))
					{
						list.Add('Y');
					}
					break;
				case 'M':
				case 'm':
					if (!list.Contains('M'))
					{
						list.Add('M');
					}
					break;
				case 'D':
				case 'd':
					if (!list.Contains('D'))
					{
						list.Add('D');
					}
					break;
				}
			}
		}
		return new string(list.ToArray()) switch
		{
			"MDY" => DateFormatType.MDY, 
			"YMD" => DateFormatType.YMD, 
			_ => DateFormatType.DMY, 
		};
	}

	public static string GetSysDateSeparator()
	{
		string dateSeparator = App.SystemCulture.DateTimeFormat.DateSeparator;
		switch (dateSeparator)
		{
		case "/":
		case "-":
		case ".":
			return dateSeparator;
		default:
			return "\\/";
		}
	}
}
