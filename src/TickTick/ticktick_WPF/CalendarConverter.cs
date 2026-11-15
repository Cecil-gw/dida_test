using System;
using System.Collections.Generic;
using System.Globalization;
using ticktick_WPF.Util;

namespace ticktick_WPF;

public class CalendarConverter
{
	private static Dictionary<string, CalendarConvertType> nameDefined { get; set; } = new Dictionary<string, CalendarConvertType>
	{
		{
			"hijri",
			CalendarConvertType.Hijri
		},
		{
			"korean-lunar",
			CalendarConvertType.Korean
		},
		{
			"hebcal",
			CalendarConvertType.Hebcal
		},
		{
			"shaka",
			CalendarConvertType.Shaka
		},
		{
			"lunar",
			CalendarConvertType.Lunar
		},
		{
			"persian",
			CalendarConvertType.Persian
		},
		{
			"vietnam",
			CalendarConvertType.Vietnamese
		},
		{
			"hijri-saudi",
			CalendarConvertType.HijriSaudi
		},
		{
			"hijri-kuwaiti",
			CalendarConvertType.HijriKuwaiti
		}
	};

	public static Dictionary<int, int> KoreanLunarLeapYearMonth { get; set; } = new Dictionary<int, int>
	{
		{ 1971, 6 },
		{ 1974, 5 },
		{ 1976, 9 },
		{ 1979, 7 },
		{ 1982, 5 },
		{ 1984, 11 },
		{ 1987, 7 },
		{ 1990, 6 },
		{ 1993, 4 },
		{ 1995, 9 },
		{ 1998, 6 },
		{ 2001, 5 },
		{ 2004, 3 },
		{ 2006, 8 },
		{ 2009, 6 },
		{ 2012, 4 },
		{ 2014, 10 },
		{ 2017, 6 },
		{ 2020, 5 },
		{ 2023, 3 },
		{ 2025, 7 },
		{ 2028, 6 },
		{ 2031, 4 },
		{ 2033, 12 },
		{ 2036, 7 },
		{ 2039, 6 },
		{ 2042, 3 }
	};

	public static Dictionary<int, string> KoreanLunarLeapMonthNames { get; set; } = new Dictionary<int, string>
	{
		{ 1, "윤정월" },
		{ 2, "윤이월" },
		{ 3, "윤삼월" },
		{ 4, "윤사월" },
		{ 5, "윤오월" },
		{ 6, "윤유월" }
	};

	public static Dictionary<CalendarConvertType, string[]> CalendarMonthNamesEn { get; set; } = new Dictionary<CalendarConvertType, string[]>
	{
		{
			CalendarConvertType.Hijri,
			new string[12]
			{
				"Mhrm.", "Safr.", "Rab.Ⅰ", "Rab.Ⅱ", "Jmd.Ⅰ", "Jmd.Ⅱ", "Rajb.", "Shbn.", "Rmdn.", "Shwl.",
				"Dhu'l-Q.", "Dhu'l-H."
			}
		},
		{
			CalendarConvertType.Korean,
			new string[12]
			{
				"정월", "여월", "가월", "초월", "매월", "계하", "교월", "계월", "현월", "개동",
				"설한", "극월"
			}
		},
		{
			CalendarConvertType.Hebcal,
			new string[12]
			{
				"Tis", "Hes", "Kis", "Tev", "She", "Ada", "Nis", "Iya", "Siv", "Tam",
				"Av", "Elu"
			}
		},
		{
			CalendarConvertType.Shaka,
			new string[12]
			{
				"Chai", "Vai", "Jya", "Asad", "Sra", "Bha", "Asv", "Kar", "Agr", "Pau",
				"Mag", "Pha"
			}
		},
		{
			CalendarConvertType.Persian,
			new string[12]
			{
				"Far", "Ord", "Kho", "Tir", "Mor", "Shah", "Mehr", "Aban", "Azar", "Dey",
				"Bah", "Esf"
			}
		},
		{
			CalendarConvertType.Vietnamese,
			new string[12]
			{
				"1", "2", "3", "4", "5", "6", "7", "8", "9", "10",
				"11", "12"
			}
		}
	};

	public static Dictionary<CalendarConvertType, string[]> CalendarMonthNames { get; set; } = new Dictionary<CalendarConvertType, string[]>
	{
		{
			CalendarConvertType.Hijri,
			new string[12]
			{
				"محرم", "صفر", "  دربيعا", "ربيع٢", "جما", "جما٢", "رجب", "شعبان", "رمضان", "شوال",
				"ذو. ق", "ذو. ح"
			}
		},
		{
			CalendarConvertType.Korean,
			new string[12]
			{
				"정월", "여월", "가월", "초월", "매월", "계하", "교월", "계월", "현월", "개동",
				"설한", "극월"
			}
		},
		{
			CalendarConvertType.Hebcal,
			new string[12]
			{
				"ת\u05b4\u05bcש\u05b0\u05c1ר\u05b5י", "ח\u05b6ש\u05b0\u05c1ו\u05b8ן", "כ\u05b4\u05bcס\u05b0ל\u05b5ו\u200e", "ט\u05b5ב\u05b5ת", "ש\u05b0\u05c1ב\u05b8ט\u200e", "א\u05b2ד\u05b8ר", "נ\u05b4יס\u05b8ן", "א\u05b4י\u05b8\u05bcיר", "ס\u05b4יו\u05b8ן", "ת\u05b7\u05bcמ\u05bcו\u05bcז",
				"א\u05b8ב\u200e", "א\u05b1לו\u05bcל"
			}
		},
		{
			CalendarConvertType.Shaka,
			new string[12]
			{
				"Chai", "Vai", "Jya", "Asad", "Sra", "Bha", "Asv", "Kar", "Agr", "Pau",
				"Mag", "Pha"
			}
		},
		{
			CalendarConvertType.Persian,
			new string[12]
			{
				"فروردين", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهريور", "مهر", "آبان", "آذر", "دی",
				"بهمن", "اسفند"
			}
		},
		{
			CalendarConvertType.Vietnamese,
			new string[12]
			{
				"1", "2", "3", "4", "5", "6", "7", "8", "9", "10",
				"11", "12"
			}
		}
	};

	public static Dictionary<CalendarConvertType, string[]> CalendarDayNames { get; set; } = new Dictionary<CalendarConvertType, string[]>
	{
		{
			CalendarConvertType.Hijri,
			new string[31]
			{
				"١", "٢", "٣", "٤", "٥", "٦", "٧", "٨", "٩", "١٠",
				"١١", "١٢", "١٣", "١٤", "١٥", "١٦", "١٧", "١٨", "١٩", "٢٠",
				"٢١", "٢٢", "٢٣", "٢٤", "٢٥", "٢٦", "٢٧", "٢٨", "٢٩", "٣٠",
				"-"
			}
		},
		{
			CalendarConvertType.Korean,
			new string[31]
			{
				"1일", "2일", "3일", "4일", "5일", "6일", "7일", "8일", "9일", "10일",
				"11일", "12일", "13일", "14일", "15일", "16일", "17일", "18일", "19일", "20일",
				"21일", "22일", "23일", "24일", "25일", "26일", "27일", "28일", "29일", "30일",
				"31일"
			}
		},
		{
			CalendarConvertType.Hebcal,
			new string[31]
			{
				"א׳", "ב׳", "ג׳", "ד׳", "ה׳", "ו׳", "ז׳", "ח׳", "ט׳", "י׳",
				"י\"א", "י\"ב", "י\"ג", "י\"ד", "ט\"ו", "ט\"ז", "י\"ז", "י\"ח", "י\"ט", "כ׳",
				"כ\"א", "כ\"ב", "כ\"ג", "כ\"ד", "כ\"ה", "כ\"ו", "כ\"ז", "כ\"ח", "כ\"ט", "ל׳",
				"-"
			}
		},
		{
			CalendarConvertType.Persian,
			new string[31]
			{
				"۱ ", "۲ ", "۳ ", "۴ ", "۵ ", "۶ ", "۷ ", "۸ ", "۹", "۱۰",
				"۱۱", "۱۲", "۱۳", "۱۴", "۱۵", "۱۶", "۱۷", "۱۸", "۱۹", "۲۰",
				"۲۱", "۲۲", "۲۳", "۲۴", "۲۵", "۲۶", "۲۷", "۲۸", "۲۹", "۳۰",
				"۳۱"
			}
		}
	};

	public static bool IsValidCalendarType(string calendarType)
	{
		if (!string.IsNullOrEmpty(calendarType))
		{
			return nameDefined.ContainsKey(calendarType);
		}
		return false;
	}

	public static CalendarConvertType GetCalendarType(string calendarType)
	{
		if (string.IsNullOrEmpty(calendarType) || !nameDefined.TryGetValue(calendarType, out var value))
		{
			return CalendarConvertType.Unknown;
		}
		return value;
	}

	public static string GetCalendarTypeName(CalendarConvertType type)
	{
		foreach (KeyValuePair<string, CalendarConvertType> item in nameDefined)
		{
			if (item.Value == type)
			{
				return item.Key;
			}
		}
		return string.Empty;
	}

	public static (DateTime? min, DateTime? max) GetLimitDate(string calendarType)
	{
		return GetLimitDate(GetCalendarType(calendarType));
	}

	public static (DateTime? min, DateTime? max) GetLimitDate(CalendarConvertType type)
	{
		switch (type)
		{
		case CalendarConvertType.Lunar:
			return (min: LunarUtils.ChineseCalendar.MinDay, max: LunarUtils.ChineseCalendar.MaxDay);
		case CalendarConvertType.Unknown:
			return (min: null, max: null);
		default:
		{
			Calendar calendar = GetCalendar(type);
			return (min: calendar.MinSupportedDateTime, max: calendar.MaxSupportedDateTime);
		}
		}
	}

	public static (DateTime? min, DateTime? max) GetCurrentCultureLimitDate()
	{
		Calendar calendar = CultureInfo.CurrentUICulture?.Calendar;
		if (calendar == null)
		{
			return (min: null, max: null);
		}
		return (min: calendar.MinSupportedDateTime, max: calendar.MaxSupportedDateTime);
	}

	public static string GetRepeatKey(CalendarConvertType type)
	{
		return type switch
		{
			CalendarConvertType.Unknown => null, 
			CalendarConvertType.Vietnamese => "VIETNAM", 
			CalendarConvertType.HijriKuwaiti => "KUWAITI", 
			CalendarConvertType.HijriSaudi => "SAUDI", 
			_ => type.ToString().ToUpper(), 
		};
	}

	public static CalendarConvertType GetRepeatFlagType(string repeatFlag)
	{
		CalendarConvertType result = CalendarConvertType.Unknown;
		if (string.IsNullOrEmpty(repeatFlag))
		{
			return result;
		}
		foreach (CalendarConvertType value in Enum.GetValues(typeof(CalendarConvertType)))
		{
			if (value != CalendarConvertType.Unknown && repeatFlag.Contains(GetRepeatKey(value)))
			{
				result = value;
				break;
			}
		}
		return result;
	}

	public static Calendar GetCalendar(CalendarConvertType type)
	{
		return type switch
		{
			CalendarConvertType.Korean => new KoreanLunisolarCalendar(), 
			CalendarConvertType.Hebcal => new HebrewCalendar(), 
			CalendarConvertType.Hijri => new HijriCalendar
			{
				HijriAdjustment = -1
			}, 
			CalendarConvertType.Shaka => new IndianCalendar(), 
			CalendarConvertType.Lunar => new ChineseLunisolarCalendar(), 
			CalendarConvertType.Persian => new PersianCalendar(), 
			CalendarConvertType.Vietnamese => new VietnameseCalendar(), 
			CalendarConvertType.HijriSaudi => new UmAlQuraCalendar(), 
			CalendarConvertType.HijriKuwaiti => new KuwaitiCalendar(), 
			_ => throw new ArgumentException("Invalid calendar type"), 
		};
	}

	public static string ConvertDate(DateTime date, Calendar calendar)
	{
		return calendar.ToDateTime(date.Year, date.Month, date.Day, 0, 0, 0, 0).ToString();
	}
}
