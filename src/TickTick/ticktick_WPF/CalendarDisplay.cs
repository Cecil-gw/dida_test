using System;
using System.Collections.Generic;
using System.Globalization;
using TickTickUtils.Lang;
using ticktick_WPF.Util;

namespace ticktick_WPF;

public class CalendarDisplay
{
	public bool OutLimit { get; set; }

	public int Year { get; set; }

	public int Month { get; set; }

	public int Day { get; set; }

	public int Era { get; set; }

	public int LeapMonth { get; set; }

	public bool IsLeapMonth { get; set; }

	public bool LearYear { get; set; }

	public string CalendarType { get; set; }

	public CalendarConvertType Type { get; set; }

	public CalendarDisplay(DateTime date, string calendarType)
	{
		CalendarType = calendarType;
		Type = CalendarConverter.GetCalendarType(CalendarType);
		Init(date);
	}

	public CalendarDisplay(DateTime date, CalendarConvertType type)
	{
		Type = type;
		CalendarType = CalendarConverter.GetCalendarTypeName(type);
		Init(date);
	}

	public void Init(DateTime date)
	{
		Calendar calendar = CalendarConverter.GetCalendar(Type);
		OutLimit = date < calendar.MinSupportedDateTime || date > calendar.MaxSupportedDateTime;
		if (!OutLimit)
		{
			Year = calendar.GetYear(date);
			Month = calendar.GetMonth(date);
			Day = calendar.GetDayOfMonth(date);
			Era = calendar.GetEra(date);
			IsLeapMonth = calendar.IsLeapMonth(Year, Month, Era);
			LearYear = calendar.IsLeapYear(Year);
			LeapMonth = calendar.GetLeapMonth(Year);
		}
	}

	public string DisplayText()
	{
		try
		{
			return DoDisplayText();
		}
		catch (Exception ex)
		{
			UtilLog.Error(ex);
			return string.Empty;
		}
	}

	public string DoDisplayText()
	{
		if (Day != 1)
		{
			return GetDayName();
		}
		return GetMonthName();
	}

	public string GetRepeatDateText(bool isMonthly)
	{
		if (isMonthly)
		{
			return GetDayName();
		}
		string text = GetMonthName();
		string dayName = GetDayName();
		switch (Type)
		{
		case CalendarConvertType.Hijri:
		case CalendarConvertType.HijriSaudi:
		case CalendarConvertType.HijriKuwaiti:
			return text + " \u200e" + dayName;
		case CalendarConvertType.Korean:
		case CalendarConvertType.Lunar:
			return text + dayName;
		case CalendarConvertType.Vietnamese:
		{
			if (CalendarConverter.CalendarMonthNames.TryGetValue(Type, out var value))
			{
				text = GetVietnameseMonthName(value, withDayFirst: false);
			}
			return dayName + "/" + text;
		}
		case CalendarConvertType.Persian:
			if (App.Ci.ToString() == "fa-IR")
			{
				return dayName + " " + text;
			}
			return text + " " + dayName;
		case CalendarConvertType.Hebcal:
			if (App.Ci.ToString() == "he-IL")
			{
				return text + " " + dayName;
			}
			return dayName + " " + text;
		case CalendarConvertType.Shaka:
			return dayName + " " + text;
		default:
			return null;
		}
	}

	private string GetDayName()
	{
		if (OutLimit)
		{
			return string.Empty;
		}
		switch (Type)
		{
		case CalendarConvertType.Shaka:
		case CalendarConvertType.Vietnamese:
			return Day.ToString();
		case CalendarConvertType.Hijri:
		case CalendarConvertType.Hebcal:
		case CalendarConvertType.Persian:
		case CalendarConvertType.HijriSaudi:
		case CalendarConvertType.HijriKuwaiti:
			if (Utils.IsEn())
			{
				return Day.ToString();
			}
			break;
		case CalendarConvertType.Lunar:
		{
			if (DateUtils.ChineseNumDict.TryGetValue(Day, out var value))
			{
				return value;
			}
			return Day.ToString();
		}
		}
		CalendarConvertType key = Type;
		CalendarConvertType type = Type;
		if ((uint)(type - 8) <= 1u)
		{
			key = CalendarConvertType.Hijri;
		}
		if (!CalendarConverter.CalendarDayNames.TryGetValue(key, out var value2))
		{
			return Day.ToString();
		}
		return value2[Day - 1] ?? "";
	}

	private string GetMonthName()
	{
		if (OutLimit)
		{
			return string.Empty;
		}
		int num = Month;
		if (LeapMonth > 0 && Month >= LeapMonth)
		{
			num--;
		}
		if (Type == CalendarConvertType.Lunar)
		{
			if (DateUtils.ChineseMonthDict.TryGetValue(num, out var value))
			{
				return value;
			}
			return num.ToString();
		}
		Dictionary<CalendarConvertType, string[]> dictionary = (Utils.IsEn() ? CalendarConverter.CalendarMonthNamesEn : CalendarConverter.CalendarMonthNames);
		CalendarConvertType key = Type;
		CalendarConvertType type = Type;
		if ((uint)(type - 8) <= 1u)
		{
			key = CalendarConvertType.Hijri;
		}
		if (dictionary.TryGetValue(key, out var value2))
		{
			switch (Type)
			{
			case CalendarConvertType.Hebcal:
				return GetHebcalMonthName(value2);
			case CalendarConvertType.Korean:
				return GetKoreanLunarMonthName(value2);
			case CalendarConvertType.Vietnamese:
				return GetVietnameseMonthName(value2, withDayFirst: true);
			default:
			{
				int num2 = num - 1;
				if (num2 < 0 || num2 >= value2.Length)
				{
					return num.ToString();
				}
				return value2[num2] ?? "";
			}
			}
		}
		return num.ToString();
	}

	public string GetVietnameseMonthName(IReadOnlyList<string> monthNames, bool withDayFirst)
	{
		string text = string.Empty;
		if (LeapMonth > 0 && Month >= LeapMonth)
		{
			text = monthNames[Month - 2];
		}
		if (string.IsNullOrEmpty(text))
		{
			text = GetNonLeapMonthName(monthNames);
		}
		if (!withDayFirst)
		{
			return text;
		}
		return "1/" + text;
	}

	private string GetKoreanLunarMonthName(IReadOnlyList<string> monthNames)
	{
		if (CalendarConverter.KoreanLunarLeapYearMonth.TryGetValue(Year, out var value))
		{
			if (IsLeapMonth && Month == value)
			{
				return "윤" + GetNonLeapMonthName(monthNames, Month - 2);
			}
			if (Month > value)
			{
				return monthNames[Month - 2] ?? "";
			}
		}
		return GetNonLeapMonthName(monthNames);
	}

	private string GetHebcalMonthName(IReadOnlyList<string> monthNames)
	{
		if (LearYear)
		{
			if (Month == 6)
			{
				if (!Utils.IsEn())
				{
					return "א\u05b2ד\u05b8ר א";
				}
				return "Ada Ⅰ";
			}
			if (IsLeapMonth && Month == 7)
			{
				if (!Utils.IsEn())
				{
					return "א\u05b2ד\u05b8ר ב'";
				}
				return "Ada Ⅱ";
			}
			if (Month >= 8)
			{
				return monthNames[Month - 2] ?? "";
			}
		}
		return GetNonLeapMonthName(monthNames);
	}

	private string GetNonLeapMonthName(IReadOnlyList<string> monthNames, int monthIndex = -1)
	{
		if (monthIndex == -1)
		{
			monthIndex = Month - 1;
		}
		if (monthIndex < 0 || monthIndex >= monthNames.Count)
		{
			return string.Empty;
		}
		return monthNames[monthIndex] ?? "";
	}

	public static Tuple<int, int, int> ConvertIndianDate(DateTime date)
	{
		GregorianCalendar gregorianCalendar = new GregorianCalendar();
		int year = gregorianCalendar.GetYear(date);
		int month = gregorianCalendar.GetMonth(date);
		int dayOfMonth = gregorianCalendar.GetDayOfMonth(date);
		bool flag = gregorianCalendar.IsLeapYear(year);
		int num = (flag ? 21 : 22);
		int num2 = gregorianCalendar.GetYear(date) - 78;
		int item;
		int item2;
		if (month == 12 && dayOfMonth >= 22)
		{
			item = 10;
			item2 = dayOfMonth - 21;
		}
		else if (month == 12)
		{
			item = 9;
			item2 = dayOfMonth + 9;
		}
		else if (month == 11 && dayOfMonth >= 22)
		{
			item = 9;
			item2 = dayOfMonth - 21;
		}
		else if (month == 11)
		{
			item = 8;
			item2 = dayOfMonth + 9;
		}
		else if (month == 10 && dayOfMonth >= 23)
		{
			item = 8;
			item2 = dayOfMonth - 22;
		}
		else if (month == 10)
		{
			item = 7;
			item2 = dayOfMonth + 8;
		}
		else if (month == 9 && dayOfMonth >= 23)
		{
			item = 7;
			item2 = dayOfMonth - 22;
		}
		else if (month == 9)
		{
			item = 6;
			item2 = dayOfMonth + 9;
		}
		else if (month == 8 && dayOfMonth >= 23)
		{
			item = 6;
			item2 = dayOfMonth - 22;
		}
		else if (month == 8)
		{
			item = 5;
			item2 = dayOfMonth + 9;
		}
		else if (month == 7 && dayOfMonth >= 23)
		{
			item = 5;
			item2 = dayOfMonth - 22;
		}
		else if (month == 7)
		{
			item = 4;
			item2 = dayOfMonth + 9;
		}
		else if (month == 6 && dayOfMonth >= 22)
		{
			item = 4;
			item2 = dayOfMonth - 21;
		}
		else if (month == 6)
		{
			item = 3;
			item2 = dayOfMonth + 10;
		}
		else if (month == 5 && dayOfMonth >= 22)
		{
			item = 3;
			item2 = dayOfMonth - 21;
		}
		else if (month == 5)
		{
			item = 2;
			item2 = dayOfMonth + 10;
		}
		else if (month == 4 && dayOfMonth >= 21)
		{
			item = 2;
			item2 = dayOfMonth - 20;
		}
		else if (month == 4)
		{
			item = 1;
			item2 = dayOfMonth + (flag ? 11 : 10);
		}
		else if (month == 3 && dayOfMonth >= num)
		{
			item = 1;
			item2 = dayOfMonth - num + 1;
		}
		else if (month == 3)
		{
			num2--;
			item = 12;
			item2 = dayOfMonth + (flag ? 10 : 9);
		}
		else if (month == 2 && dayOfMonth >= 20)
		{
			num2--;
			item = 12;
			item2 = dayOfMonth - 19;
		}
		else if (month == 2)
		{
			num2--;
			item = 11;
			item2 = dayOfMonth + 11;
		}
		else if (month == 1 && dayOfMonth >= 21)
		{
			num2--;
			item = 11;
			item2 = dayOfMonth - 20;
		}
		else
		{
			num2--;
			item = 10;
			item2 = dayOfMonth + 10;
		}
		return new Tuple<int, int, int>(num2, item, item2);
	}
}
