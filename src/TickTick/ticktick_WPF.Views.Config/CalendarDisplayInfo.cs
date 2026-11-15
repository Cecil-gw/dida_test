using System.Collections.Generic;
using System.Linq;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Config;

public class CalendarDisplayInfo
{
	public CalendarConvertType Key { get; set; }

	public string Value { get; set; }

	public static List<CalendarDisplayInfo> GetCalendarDisplayInfo()
	{
		List<CalendarDisplayInfo> list = new List<CalendarDisplayInfo>
		{
			new CalendarDisplayInfo
			{
				Key = CalendarConvertType.Korean,
				Value = Utils.GetString("korean_lunar")
			},
			new CalendarDisplayInfo
			{
				Key = CalendarConvertType.Hebcal,
				Value = Utils.GetString("hebrew")
			},
			new CalendarDisplayInfo
			{
				Key = CalendarConvertType.Hijri,
				Value = Utils.GetString("HijriCivil")
			},
			new CalendarDisplayInfo
			{
				Key = CalendarConvertType.Shaka,
				Value = Utils.GetString("saka_era")
			},
			new CalendarDisplayInfo
			{
				Key = CalendarConvertType.Lunar,
				Value = Utils.GetString("chinese_lunar")
			},
			new CalendarDisplayInfo
			{
				Key = CalendarConvertType.Persian,
				Value = Utils.GetString("PersianCalendar")
			},
			new CalendarDisplayInfo
			{
				Key = CalendarConvertType.Vietnamese,
				Value = Utils.GetString("VietnameseLunar")
			},
			new CalendarDisplayInfo
			{
				Key = CalendarConvertType.HijriSaudi,
				Value = Utils.GetString("HijriSaudi")
			},
			new CalendarDisplayInfo
			{
				Key = CalendarConvertType.HijriKuwaiti,
				Value = Utils.GetString("HijriKuwaiti")
			}
		}.OrderBy((CalendarDisplayInfo it) => it.Value).ToList();
		list.Insert(0, new CalendarDisplayInfo
		{
			Key = CalendarConvertType.Unknown,
			Value = Utils.GetString("none")
		});
		return list;
	}

	public static List<string> GetCalendarDisplayNames()
	{
		return (from it in GetCalendarDisplayInfo()
			select it.Value).ToList();
	}

	public static string GetValueByKey(string key)
	{
		CalendarConvertType type = CalendarConverter.GetCalendarType(key);
		CalendarDisplayInfo calendarDisplayInfo = GetCalendarDisplayInfo().FirstOrDefault((CalendarDisplayInfo it) => it.Key == type);
		if (calendarDisplayInfo != null)
		{
			return calendarDisplayInfo.Value;
		}
		return "";
	}

	public static string GetKeyByValue(string val)
	{
		CalendarDisplayInfo calendarDisplayInfo = GetCalendarDisplayInfo().FirstOrDefault((CalendarDisplayInfo it) => it.Value == val);
		if (calendarDisplayInfo != null)
		{
			return CalendarConverter.GetCalendarTypeName(calendarDisplayInfo.Key);
		}
		return "";
	}

	public static string GetLabelByKey(string key)
	{
		return CalendarConverter.GetCalendarType(key) switch
		{
			CalendarConvertType.Korean => "korean_lunar", 
			CalendarConvertType.Hijri => "hijri", 
			CalendarConvertType.Hebcal => "hebrew", 
			CalendarConvertType.Shaka => "indian", 
			CalendarConvertType.Lunar => "chinese_lunar", 
			CalendarConvertType.Persian => "persian_calendar", 
			CalendarConvertType.HijriKuwaiti => "hijri_kuwaiti", 
			CalendarConvertType.HijriSaudi => "hijri_saudi", 
			CalendarConvertType.Vietnamese => "vietnamese_lunar", 
			_ => null, 
		};
	}
}
