using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ticktick_WPF.Util;

public class LunarUtils
{
	public class ChineseCalendar
	{
		private struct SolarHolidayStruct
		{
			public int Month;

			public int Day;

			public int Recess;

			public string HolidayName;

			public SolarHolidayStruct(int month, int day, int recess, string name)
			{
				Month = month;
				Day = day;
				Recess = recess;
				HolidayName = name;
			}
		}

		private struct LunarHolidayStruct
		{
			public int Month;

			public int Day;

			public int Recess;

			public string HolidayName;

			public LunarHolidayStruct(int month, int day, int recess, string name)
			{
				Month = month;
				Day = day;
				Recess = recess;
				HolidayName = name;
			}
		}

		private struct WeekHolidayStruct
		{
			public int Month;

			public int WeekAtMonth;

			public int WeekDay;

			public string HolidayName;

			public WeekHolidayStruct(int month, int weekAtMonth, int weekDay, string name)
			{
				Month = month;
				WeekAtMonth = weekAtMonth;
				WeekDay = weekDay;
				HolidayName = name;
			}
		}

		private DateTime _date;

		private DateTime _datetime;

		private int _cYear;

		private int _cMonth;

		private int _cDay;

		private bool _cIsLeapMonth;

		private bool _cIsLeapYear;

		private const int MinYear = 1900;

		private const int MaxYear = 2100;

		public static DateTime MinDay = new DateTime(1900, 1, 30);

		public static DateTime MaxDay = new DateTime(2100, 12, 31);

		private const int GanZhiStartYear = 1864;

		private static DateTime GanZhiStartDay = new DateTime(1899, 12, 22);

		private const string HZNum = "零一二三四五六七八九";

		private const int AnimalStartYear = 1900;

		private static DateTime ChineseConstellationReferDay = new DateTime(2007, 9, 13);

		private static string[] _constellationName = new string[12]
		{
			"白羊座", "金牛座", "双子座", "巨蟹座", "狮子座", "处女座", "天秤座", "天蝎座", "射手座", "摩羯座",
			"水瓶座", "双鱼座"
		};

		private static string[] _lunarHolidayName = new string[24]
		{
			"小寒", "大寒", "立春", "雨水", "惊蛰", "春分", "清明", "谷雨", "立夏", "小满",
			"芒种", "夏至", "小暑", "大暑", "立秋", "处暑", "白露", "秋分", "寒露", "霜降",
			"立冬", "小雪", "大雪", "冬至"
		};

		private static string[] _chineseConstellationName = new string[28]
		{
			"角木蛟", "亢金龙", "女土蝠", "房日兔", "心月狐", "尾火虎", "箕水豹", "斗木獬", "牛金牛", "氐土貉",
			"虚日鼠", "危月燕", "室火猪", "壁水獝", "奎木狼", "娄金狗", "胃土彘", "昴日鸡", "毕月乌", "觜火猴",
			"参水猿", "井木犴", "鬼金羊", "柳土獐", "星日马", "张月鹿", "翼火蛇", "轸水蚓"
		};

		private Dictionary<int, List<int>> solarDateDict = new Dictionary<int, List<int>>
		{
			{
				2000,
				new List<int>
				{
					106, 121, 204, 219, 305, 320, 404, 420, 505, 521,
					605, 621, 707, 722, 807, 823, 907, 923, 1008, 1023,
					1107, 1122, 1207, 1221
				}
			},
			{
				2001,
				new List<int>
				{
					105, 120, 204, 218, 305, 320, 405, 420, 505, 521,
					605, 621, 707, 723, 807, 823, 907, 923, 1008, 1023,
					1107, 1122, 1207, 1222
				}
			},
			{
				2002,
				new List<int>
				{
					105, 120, 204, 219, 306, 321, 405, 420, 506, 521,
					606, 621, 707, 723, 808, 823, 908, 923, 1008, 1023,
					1107, 1122, 1207, 1222
				}
			},
			{
				2003,
				new List<int>
				{
					106, 120, 204, 219, 306, 321, 405, 420, 506, 521,
					606, 622, 707, 723, 808, 823, 908, 923, 1009, 1024,
					1108, 1123, 1207, 1222
				}
			},
			{
				2004,
				new List<int>
				{
					106, 121, 204, 219, 305, 320, 404, 420, 505, 521,
					605, 621, 707, 722, 807, 823, 907, 923, 1008, 1023,
					1107, 1122, 1207, 1221
				}
			},
			{
				2005,
				new List<int>
				{
					105, 120, 204, 218, 305, 320, 405, 420, 505, 521,
					605, 621, 707, 723, 807, 823, 907, 923, 1008, 1023,
					1107, 1122, 1207, 1222
				}
			},
			{
				2006,
				new List<int>
				{
					105, 120, 204, 219, 306, 321, 405, 420, 505, 521,
					606, 621, 707, 723, 807, 823, 908, 923, 1008, 1023,
					1107, 1122, 1207, 1222
				}
			},
			{
				2007,
				new List<int>
				{
					106, 120, 204, 219, 306, 321, 405, 420, 506, 521,
					606, 622, 707, 723, 808, 823, 908, 923, 1009, 1024,
					1108, 1123, 1207, 1222
				}
			},
			{
				2008,
				new List<int>
				{
					106, 121, 204, 219, 305, 320, 404, 420, 505, 521,
					605, 621, 707, 722, 807, 823, 907, 922, 1008, 1023,
					1107, 1122, 1207, 1221
				}
			},
			{
				2009,
				new List<int>
				{
					105, 120, 204, 218, 305, 320, 404, 420, 505, 521,
					605, 621, 707, 723, 807, 823, 907, 923, 1008, 1023,
					1107, 1122, 1207, 1222
				}
			},
			{
				2010,
				new List<int>
				{
					105, 120, 204, 219, 306, 321, 405, 420, 505, 521,
					606, 621, 707, 723, 807, 823, 908, 923, 1008, 1023,
					1107, 1122, 1207, 1222
				}
			},
			{
				2011,
				new List<int>
				{
					106, 120, 204, 219, 306, 321, 405, 420, 506, 521,
					606, 622, 707, 723, 808, 823, 908, 923, 1008, 1024,
					1108, 1123, 1207, 1222
				}
			},
			{
				2012,
				new List<int>
				{
					106, 121, 204, 219, 305, 320, 404, 420, 505, 520,
					605, 621, 707, 722, 807, 823, 907, 922, 1008, 1023,
					1107, 1122, 1207, 1221
				}
			},
			{
				2013,
				new List<int>
				{
					105, 120, 204, 218, 305, 320, 404, 420, 505, 521,
					605, 621, 707, 722, 807, 823, 907, 923, 1008, 1023,
					1107, 1122, 1207, 1222
				}
			},
			{
				2014,
				new List<int>
				{
					105, 120, 204, 219, 306, 321, 405, 420, 505, 521,
					606, 621, 707, 723, 807, 823, 908, 923, 1008, 1023,
					1107, 1122, 1207, 1222
				}
			},
			{
				2015,
				new List<int>
				{
					106, 120, 204, 219, 306, 321, 405, 420, 506, 521,
					606, 622, 707, 723, 808, 823, 908, 923, 1008, 1024,
					1108, 1122, 1207, 1222
				}
			},
			{
				2016,
				new List<int>
				{
					106, 120, 204, 219, 305, 320, 404, 419, 505, 520,
					605, 621, 707, 722, 807, 823, 907, 922, 1008, 1023,
					1107, 1122, 1207, 1221
				}
			},
			{
				2017,
				new List<int>
				{
					105, 120, 203, 218, 305, 320, 404, 420, 505, 521,
					605, 621, 707, 722, 807, 823, 907, 923, 1008, 1023,
					1107, 1122, 1207, 1222
				}
			},
			{
				2018,
				new List<int>
				{
					105, 120, 204, 219, 305, 321, 405, 420, 505, 521,
					606, 621, 707, 723, 807, 823, 908, 923, 1008, 1023,
					1107, 1122, 1207, 1222
				}
			},
			{
				2019,
				new List<int>
				{
					105, 120, 204, 219, 306, 321, 405, 420, 506, 521,
					606, 621, 707, 723, 808, 823, 908, 923, 1008, 1024,
					1108, 1122, 1207, 1222
				}
			},
			{
				2020,
				new List<int>
				{
					106, 120, 204, 219, 305, 320, 404, 419, 505, 520,
					605, 621, 706, 722, 807, 822, 907, 922, 1008, 1023,
					1107, 1122, 1207, 1221
				}
			},
			{
				2021,
				new List<int>
				{
					105, 120, 203, 218, 305, 320, 404, 420, 505, 521,
					605, 621, 707, 722, 807, 823, 907, 923, 1008, 1023,
					1107, 1122, 1207, 1221
				}
			},
			{
				2022,
				new List<int>
				{
					105, 120, 204, 219, 305, 320, 405, 420, 505, 521,
					606, 621, 707, 723, 807, 823, 907, 923, 1008, 1023,
					1107, 1122, 1207, 1222
				}
			},
			{
				2023,
				new List<int>
				{
					105, 120, 204, 219, 306, 321, 405, 420, 506, 521,
					606, 621, 707, 723, 808, 823, 908, 923, 1008, 1024,
					1108, 1122, 1207, 1222
				}
			},
			{
				2024,
				new List<int>
				{
					106, 120, 204, 219, 305, 320, 404, 419, 505, 520,
					605, 621, 706, 722, 807, 822, 907, 922, 1008, 1023,
					1107, 1122, 1206, 1221
				}
			},
			{
				2025,
				new List<int>
				{
					105, 120, 203, 218, 305, 320, 404, 420, 505, 521,
					605, 621, 707, 722, 807, 823, 907, 923, 1008, 1023,
					1107, 1122, 1207, 1221
				}
			},
			{
				2026,
				new List<int>
				{
					105, 120, 204, 218, 305, 320, 405, 420, 505, 521,
					605, 621, 707, 723, 807, 823, 907, 923, 1008, 1023,
					1107, 1122, 1207, 1222
				}
			},
			{
				2027,
				new List<int>
				{
					105, 120, 204, 219, 306, 321, 405, 420, 506, 521,
					606, 621, 707, 723, 808, 823, 908, 923, 1008, 1023,
					1107, 1122, 1207, 1222
				}
			},
			{
				2028,
				new List<int>
				{
					106, 120, 204, 219, 305, 320, 404, 419, 505, 520,
					605, 621, 706, 722, 807, 822, 907, 922, 1008, 1023,
					1107, 1122, 1206, 1221
				}
			},
			{
				2029,
				new List<int>
				{
					105, 120, 203, 218, 305, 320, 404, 420, 505, 521,
					605, 621, 707, 722, 807, 823, 907, 923, 1008, 1023,
					1107, 1122, 1207, 1221
				}
			},
			{
				2030,
				new List<int>
				{
					105, 120, 204, 218, 305, 320, 405, 420, 505, 521,
					605, 621, 707, 723, 807, 823, 907, 923, 1008, 1023,
					1107, 1122, 1207, 1222
				}
			},
			{
				2031,
				new List<int>
				{
					105, 120, 204, 219, 306, 321, 405, 420, 506, 521,
					606, 621, 707, 723, 808, 823, 908, 923, 1008, 1023,
					1107, 1122, 1207, 1222
				}
			},
			{
				2032,
				new List<int>
				{
					106, 120, 204, 219, 305, 320, 404, 419, 505, 520,
					605, 621, 706, 722, 807, 822, 907, 922, 1008, 1023,
					1107, 1122, 1206, 1221
				}
			},
			{
				2033,
				new List<int>
				{
					105, 120, 203, 218, 305, 320, 404, 420, 505, 521,
					605, 621, 707, 722, 807, 823, 907, 923, 1008, 1023,
					1107, 1122, 1207, 1221
				}
			},
			{
				2034,
				new List<int>
				{
					105, 120, 204, 218, 305, 320, 405, 420, 505, 521,
					605, 621, 707, 723, 807, 823, 907, 923, 1008, 1023,
					1107, 1122, 1207, 1222
				}
			},
			{
				2035,
				new List<int>
				{
					105, 120, 204, 219, 306, 321, 405, 420, 505, 521,
					606, 621, 707, 723, 807, 823, 908, 923, 1008, 1023,
					1107, 1122, 1207, 1222
				}
			},
			{
				2036,
				new List<int>
				{
					106, 120, 204, 219, 305, 320, 404, 419, 505, 520,
					605, 621, 706, 722, 807, 822, 907, 922, 1008, 1023,
					1107, 1122, 1206, 1221
				}
			},
			{
				2037,
				new List<int>
				{
					105, 120, 203, 218, 305, 320, 404, 420, 505, 521,
					605, 621, 707, 722, 807, 823, 907, 922, 1008, 1023,
					1107, 1122, 1207, 1221
				}
			},
			{
				2038,
				new List<int>
				{
					105, 120, 204, 218, 305, 320, 405, 420, 505, 521,
					605, 621, 707, 723, 807, 823, 907, 923, 1008, 1023,
					1107, 1122, 1207, 1222
				}
			},
			{
				2039,
				new List<int>
				{
					105, 120, 204, 219, 306, 321, 405, 420, 505, 521,
					606, 621, 707, 723, 807, 823, 908, 923, 1008, 1023,
					1107, 1122, 1207, 1222
				}
			},
			{
				2040,
				new List<int>
				{
					106, 120, 204, 219, 305, 320, 404, 419, 505, 520,
					605, 621, 706, 722, 807, 822, 907, 922, 1007, 1023,
					1107, 1122, 1206, 1221
				}
			},
			{
				2041,
				new List<int>
				{
					105, 120, 203, 218, 305, 320, 404, 420, 505, 520,
					605, 621, 707, 722, 807, 823, 907, 922, 1008, 1023,
					1107, 1122, 1207, 1223
				}
			},
			{
				2042,
				new List<int>
				{
					105, 120, 204, 218, 305, 320, 404, 420, 505, 521,
					605, 621, 707, 722, 807, 823, 907, 923, 1008, 1023,
					1107, 1122, 1207, 1222
				}
			},
			{
				2043,
				new List<int>
				{
					105, 120, 204, 219, 306, 321, 405, 420, 505, 521,
					606, 621, 707, 723, 807, 823, 908, 923, 1008, 1023,
					1107, 1122, 1207, 1222
				}
			},
			{
				2044,
				new List<int>
				{
					106, 120, 204, 219, 305, 320, 404, 419, 505, 520,
					605, 621, 706, 720, 807, 822, 907, 922, 1007, 1023,
					1107, 1122, 1206, 1221
				}
			},
			{
				2045,
				new List<int>
				{
					105, 120, 203, 218, 305, 320, 404, 419, 505, 520,
					605, 621, 707, 722, 807, 823, 907, 922, 1008, 1023,
					1107, 1122, 1207, 1221
				}
			},
			{
				2046,
				new List<int>
				{
					105, 120, 204, 218, 305, 320, 404, 420, 505, 521,
					605, 621, 707, 722, 807, 823, 907, 923, 1008, 1023,
					1107, 1122, 1207, 1222
				}
			},
			{
				2047,
				new List<int>
				{
					105, 120, 204, 219, 306, 321, 405, 420, 505, 521,
					606, 621, 707, 723, 807, 823, 908, 923, 1008, 1023,
					1107, 1122, 1207, 1222
				}
			},
			{
				2048,
				new List<int>
				{
					106, 120, 204, 219, 305, 320, 404, 419, 505, 520,
					605, 620, 706, 722, 807, 822, 907, 922, 1007, 1023,
					1107, 1121, 1206, 1221
				}
			},
			{
				2049,
				new List<int>
				{
					105, 119, 203, 218, 305, 320, 404, 419, 505, 520,
					605, 621, 706, 722, 807, 822, 907, 922, 1008, 1023,
					1107, 1122, 1207, 1221
				}
			},
			{
				2050,
				new List<int>
				{
					105, 120, 203, 218, 305, 320, 404, 420, 505, 521,
					605, 621, 707, 722, 807, 823, 907, 923, 1008, 1023,
					1107, 1122, 1207, 1222
				}
			},
			{
				2051,
				new List<int>
				{
					105, 120, 204, 219, 305, 321, 405, 420, 505, 521,
					606, 621, 707, 723, 807, 823, 907, 923, 1008, 1023,
					1107, 1122, 1207, 1222
				}
			},
			{
				2052,
				new List<int>
				{
					105, 120, 204, 219, 305, 320, 404, 419, 505, 520,
					605, 620, 706, 722, 807, 822, 907, 922, 1007, 1023,
					1107, 1121, 1206, 1221
				}
			},
			{
				2053,
				new List<int>
				{
					105, 120, 203, 218, 305, 320, 404, 419, 505, 520,
					605, 621, 706, 722, 807, 822, 907, 922, 1008, 1023,
					1107, 1122, 1207, 1221
				}
			},
			{
				2054,
				new List<int>
				{
					105, 120, 203, 218, 305, 320, 404, 420, 505, 521,
					605, 621, 707, 722, 807, 823, 907, 923, 1008, 1023,
					1107, 1122, 1207, 1222
				}
			},
			{
				2055,
				new List<int>
				{
					105, 120, 204, 219, 305, 320, 405, 420, 505, 521,
					605, 621, 707, 723, 807, 823, 907, 923, 1008, 1023,
					1107, 1122, 1207, 1222
				}
			},
			{
				2056,
				new List<int>
				{
					105, 120, 204, 219, 305, 320, 404, 419, 505, 520,
					605, 620, 706, 722, 807, 822, 907, 922, 1007, 1023,
					1107, 1121, 1206, 1221
				}
			},
			{
				2057,
				new List<int>
				{
					105, 119, 203, 218, 305, 320, 404, 419, 505, 520,
					605, 621, 706, 722, 807, 822, 907, 922, 1008, 1023,
					1107, 1122, 1206, 1221
				}
			},
			{
				2058,
				new List<int>
				{
					105, 120, 203, 218, 305, 320, 404, 420, 505, 521,
					605, 621, 707, 722, 807, 823, 907, 923, 1008, 1023,
					1107, 1122, 1207, 1221
				}
			},
			{
				2059,
				new List<int>
				{
					105, 120, 204, 218, 305, 320, 405, 420, 505, 521,
					605, 621, 707, 723, 807, 823, 907, 923, 1008, 1023,
					1107, 1122, 1207, 1222
				}
			},
			{
				2060,
				new List<int>
				{
					105, 120, 204, 219, 305, 320, 404, 419, 505, 520,
					605, 620, 706, 722, 807, 822, 907, 922, 1007, 1022,
					1106, 1121, 1206, 1221
				}
			},
			{
				2061,
				new List<int>
				{
					105, 119, 203, 218, 305, 320, 404, 419, 505, 520,
					605, 621, 706, 722, 807, 822, 907, 922, 1008, 1023,
					1107, 1122, 1206, 1221
				}
			},
			{
				2062,
				new List<int>
				{
					105, 120, 203, 218, 305, 320, 404, 420, 505, 521,
					605, 621, 707, 722, 807, 823, 907, 923, 1008, 1023,
					1107, 1122, 1207, 1221
				}
			},
			{
				2063,
				new List<int>
				{
					105, 120, 204, 218, 305, 320, 405, 420, 505, 521,
					605, 621, 707, 723, 807, 823, 907, 923, 1008, 1023,
					1107, 1122, 1207, 1222
				}
			},
			{
				2064,
				new List<int>
				{
					105, 120, 204, 219, 305, 320, 404, 419, 505, 520,
					605, 620, 706, 722, 807, 822, 907, 922, 1007, 1022,
					1106, 1121, 1206, 1221
				}
			},
			{
				2065,
				new List<int>
				{
					105, 119, 203, 218, 305, 320, 404, 419, 505, 520,
					605, 621, 706, 722, 807, 822, 907, 922, 1008, 1023,
					1107, 1122, 1206, 1221
				}
			},
			{
				2066,
				new List<int>
				{
					105, 120, 203, 218, 305, 320, 404, 420, 505, 521,
					605, 621, 707, 722, 807, 823, 907, 923, 1006, 1023,
					1107, 1122, 1207, 1221
				}
			},
			{
				2067,
				new List<int>
				{
					105, 120, 204, 218, 305, 320, 405, 420, 505, 521,
					605, 621, 707, 723, 807, 823, 907, 923, 1008, 1023,
					1107, 1122, 1206, 1222
				}
			},
			{
				2068,
				new List<int>
				{
					105, 120, 204, 219, 305, 320, 404, 419, 504, 520,
					605, 620, 706, 722, 806, 822, 907, 922, 1007, 1022,
					1106, 1121, 1206, 1221
				}
			},
			{
				2069,
				new List<int>
				{
					105, 119, 203, 218, 305, 320, 404, 419, 505, 520,
					605, 621, 706, 722, 807, 822, 907, 922, 1008, 1023,
					1107, 1122, 1206, 1221
				}
			},
			{
				2070,
				new List<int>
				{
					105, 120, 203, 218, 305, 320, 404, 420, 505, 520,
					605, 621, 707, 722, 807, 823, 907, 922, 1008, 1023,
					1107, 1122, 1207, 1221
				}
			},
			{
				2071,
				new List<int>
				{
					105, 120, 204, 218, 305, 320, 405, 420, 505, 521,
					605, 621, 707, 723, 807, 823, 907, 923, 1008, 1023,
					1107, 1122, 1207, 1222
				}
			},
			{
				2072,
				new List<int>
				{
					105, 120, 204, 219, 305, 320, 404, 419, 504, 520,
					605, 620, 706, 722, 806, 822, 907, 922, 1007, 1022,
					1106, 1121, 1206, 1221
				}
			},
			{
				2073,
				new List<int>
				{
					105, 119, 203, 218, 305, 320, 404, 419, 505, 520,
					605, 621, 706, 722, 807, 822, 907, 922, 1007, 1023,
					1107, 1122, 1206, 1221
				}
			},
			{
				2074,
				new List<int>
				{
					105, 120, 203, 218, 305, 320, 404, 420, 505, 520,
					605, 621, 707, 722, 807, 823, 907, 922, 1008, 1023,
					1107, 1122, 1207, 1221
				}
			},
			{
				2075,
				new List<int>
				{
					105, 120, 204, 218, 305, 320, 404, 420, 505, 521,
					605, 621, 707, 722, 807, 823, 907, 923, 1008, 1023,
					1107, 1122, 1207, 1222
				}
			},
			{
				2076,
				new List<int>
				{
					105, 120, 204, 219, 305, 320, 404, 419, 504, 520,
					605, 620, 706, 722, 806, 822, 907, 922, 1007, 1022,
					1106, 1121, 1206, 1221
				}
			},
			{
				2077,
				new List<int>
				{
					105, 119, 203, 218, 305, 320, 404, 419, 505, 520,
					605, 621, 706, 722, 807, 822, 907, 922, 1007, 1023,
					1107, 1122, 1206, 1221
				}
			},
			{
				2078,
				new List<int>
				{
					105, 120, 203, 218, 305, 320, 404, 419, 505, 520,
					605, 621, 706, 722, 807, 823, 907, 922, 1008, 1023,
					1107, 1122, 1207, 1221
				}
			},
			{
				2079,
				new List<int>
				{
					105, 120, 204, 218, 305, 320, 404, 420, 505, 521,
					605, 621, 707, 722, 807, 823, 907, 923, 1008, 1023,
					1107, 1122, 1207, 1222
				}
			},
			{
				2080,
				new List<int>
				{
					105, 120, 204, 219, 305, 320, 404, 419, 504, 520,
					605, 620, 706, 722, 806, 822, 907, 922, 1007, 1022,
					1106, 1121, 1206, 1221
				}
			},
			{
				2081,
				new List<int>
				{
					105, 119, 203, 218, 305, 320, 404, 419, 505, 520,
					605, 620, 706, 722, 807, 822, 907, 922, 1007, 1023,
					1107, 1121, 1206, 1221
				}
			},
			{
				2082,
				new List<int>
				{
					105, 120, 203, 218, 305, 320, 404, 419, 505, 520,
					605, 621, 706, 722, 807, 822, 907, 922, 1008, 1023,
					1107, 1122, 1207, 1221
				}
			},
			{
				2083,
				new List<int>
				{
					105, 120, 204, 218, 305, 320, 404, 420, 505, 521,
					605, 621, 707, 722, 807, 823, 907, 923, 1008, 1023,
					1107, 1122, 1207, 1222
				}
			},
			{
				2084,
				new List<int>
				{
					105, 120, 204, 219, 304, 320, 404, 419, 504, 520,
					605, 620, 706, 722, 806, 822, 906, 922, 1007, 1022,
					1106, 1121, 1206, 1221
				}
			},
			{
				2085,
				new List<int>
				{
					105, 119, 203, 218, 305, 320, 404, 419, 505, 520,
					605, 620, 706, 722, 807, 822, 907, 922, 1007, 1023,
					1107, 1121, 1206, 1221
				}
			},
			{
				2086,
				new List<int>
				{
					105, 119, 203, 218, 305, 320, 404, 419, 505, 520,
					605, 621, 706, 722, 807, 822, 907, 922, 1008, 1023,
					1107, 1122, 1207, 1221
				}
			},
			{
				2087,
				new List<int>
				{
					105, 120, 203, 218, 305, 320, 404, 420, 505, 521,
					605, 621, 707, 722, 807, 823, 907, 923, 1008, 1023,
					1107, 1122, 1207, 1222
				}
			},
			{
				2088,
				new List<int>
				{
					105, 120, 204, 219, 304, 319, 404, 419, 504, 520,
					604, 620, 706, 722, 806, 822, 906, 922, 1007, 1022,
					1106, 1121, 1206, 1221
				}
			},
			{
				2089,
				new List<int>
				{
					104, 119, 203, 218, 305, 320, 404, 419, 505, 520,
					605, 620, 706, 722, 807, 822, 907, 922, 1007, 1023,
					1107, 1121, 1206, 1221
				}
			},
			{
				2090,
				new List<int>
				{
					105, 119, 203, 218, 305, 320, 404, 419, 505, 520,
					605, 621, 706, 722, 807, 822, 907, 922, 1008, 1023,
					1107, 1122, 1206, 1221
				}
			},
			{
				2091,
				new List<int>
				{
					105, 120, 203, 218, 305, 320, 404, 420, 505, 521,
					605, 621, 707, 722, 807, 823, 907, 923, 1008, 1023,
					1107, 1122, 1207, 1221
				}
			},
			{
				2092,
				new List<int>
				{
					105, 120, 204, 219, 304, 319, 404, 419, 504, 520,
					604, 620, 706, 722, 806, 822, 906, 922, 1007, 1022,
					1106, 1121, 1206, 1221
				}
			},
			{
				2093,
				new List<int>
				{
					104, 119, 203, 218, 305, 320, 404, 419, 505, 520,
					605, 620, 706, 722, 807, 822, 907, 922, 1007, 1022,
					1106, 1121, 1206, 1221
				}
			},
			{
				2094,
				new List<int>
				{
					105, 119, 203, 218, 305, 320, 404, 419, 505, 520,
					605, 621, 706, 722, 807, 822, 907, 922, 1008, 1023,
					1107, 1122, 1206, 1221
				}
			},
			{
				2095,
				new List<int>
				{
					105, 120, 203, 218, 305, 320, 404, 420, 505, 521,
					605, 621, 707, 722, 807, 823, 907, 923, 1008, 1023,
					1107, 1122, 1207, 1221
				}
			},
			{
				2096,
				new List<int>
				{
					105, 120, 204, 218, 304, 319, 404, 419, 504, 520,
					604, 620, 706, 722, 806, 822, 906, 922, 1007, 1022,
					1106, 1121, 1206, 1221
				}
			},
			{
				2097,
				new List<int>
				{
					104, 119, 203, 218, 305, 320, 404, 419, 504, 520,
					605, 620, 706, 722, 806, 822, 907, 922, 1007, 1022,
					1106, 1121, 1206, 1221
				}
			},
			{
				2098,
				new List<int>
				{
					105, 119, 203, 218, 305, 320, 404, 419, 505, 520,
					605, 621, 706, 722, 807, 822, 907, 922, 1008, 1023,
					1107, 1122, 1206, 1221
				}
			},
			{
				2099,
				new List<int>
				{
					105, 120, 203, 218, 305, 320, 404, 420, 505, 521,
					605, 621, 707, 722, 807, 823, 907, 923, 1008, 1023,
					1107, 1122, 1207, 1221
				}
			}
		};

		private static string[] SolarTerm = new string[24]
		{
			"小寒", "大寒", "立春", "雨水", "惊蛰", "春分", "清明", "谷雨", "立夏", "小满",
			"芒种", "夏至", "小暑", "大暑", "立秋", "处暑", "白露", "秋分", "寒露", "霜降",
			"立冬", "小雪", "大雪", "冬至"
		};

		private static int[] sTermInfo = new int[24]
		{
			0, 21208, 42467, 63836, 85337, 107014, 128867, 150921, 173149, 195551,
			218072, 240693, 263303, 285989, 308563, 330313, 353350, 375494, 397447, 419210,
			440795, 462224, 483652, 504758
		};

		private static string ganStr = "甲乙丙丁戊己庚辛壬癸";

		private static string zhiStr = "子丑寅卯辰巳午未申酉戌亥";

		private static string animalStr = "鼠牛虎兔龙蛇马羊猴鸡狗猪";

		private static string nStr1 = "日一二三四五六七八九";

		private static string nStr2 = "初十廿卅";

		private static string[] _monthString = new string[13]
		{
			"出错", "正月", "二月", "三月", "四月", "五月", "六月", "七月", "八月", "九月",
			"十月", "冬月", "腊月"
		};

		private static SolarHolidayStruct[] sHolidayInfo = new SolarHolidayStruct[13]
		{
			new SolarHolidayStruct(1, 1, 1, "元旦"),
			new SolarHolidayStruct(2, 14, 0, "情人节"),
			new SolarHolidayStruct(3, 8, 0, "妇女节"),
			new SolarHolidayStruct(4, 1, 0, "愚人节"),
			new SolarHolidayStruct(5, 1, 1, "劳动节"),
			new SolarHolidayStruct(6, 1, 0, "儿童节"),
			new SolarHolidayStruct(7, 1, 0, "建党节"),
			new SolarHolidayStruct(8, 1, 0, "建军节"),
			new SolarHolidayStruct(9, 10, 0, "教师节"),
			new SolarHolidayStruct(10, 1, 1, "国庆节"),
			new SolarHolidayStruct(10, 31, 0, "万圣夜"),
			new SolarHolidayStruct(12, 24, 0, "平安夜"),
			new SolarHolidayStruct(12, 25, 0, "圣诞节")
		};

		private static LunarHolidayStruct[] lHolidayInfo = new LunarHolidayStruct[10]
		{
			new LunarHolidayStruct(1, 1, 1, "春节"),
			new LunarHolidayStruct(1, 15, 0, "元宵节"),
			new LunarHolidayStruct(5, 5, 0, "端午节"),
			new LunarHolidayStruct(7, 7, 0, "七夕"),
			new LunarHolidayStruct(7, 15, 0, "中元节"),
			new LunarHolidayStruct(8, 15, 0, "中秋节"),
			new LunarHolidayStruct(9, 9, 0, "重阳节"),
			new LunarHolidayStruct(12, 8, 0, "腊八节"),
			new LunarHolidayStruct(12, 23, 0, "北方小年"),
			new LunarHolidayStruct(12, 24, 0, "南方小年")
		};

		private static WeekHolidayStruct[] wHolidayInfo = new WeekHolidayStruct[3]
		{
			new WeekHolidayStruct(5, 2, 1, "母亲节"),
			new WeekHolidayStruct(6, 3, 1, "父亲节"),
			new WeekHolidayStruct(11, 4, 5, "感恩节")
		};

		private static ChineseLunisolarCalendar calendar { get; set; } = new ChineseLunisolarCalendar();

		private static Dictionary<int, LunarDaysInfo> lunarDaysDict { get; set; } = new Dictionary<int, LunarDaysInfo> { 
		{
			1900,
			new LunarDaysInfo(19416)
		} };

		public string ChineseCalendarHoliday
		{
			get
			{
				string result = "";
				if (!_cIsLeapMonth)
				{
					LunarHolidayStruct[] array = lHolidayInfo;
					for (int i = 0; i < array.Length; i++)
					{
						LunarHolidayStruct lunarHolidayStruct = array[i];
						if (lunarHolidayStruct.Month == _cMonth && lunarHolidayStruct.Day == _cDay)
						{
							result = lunarHolidayStruct.HolidayName;
							break;
						}
					}
					if (_cMonth == 12)
					{
						int chineseMonthDays = GetChineseMonthDays(_cYear, 12);
						if (_cDay == chineseMonthDays)
						{
							result = "除夕";
						}
					}
				}
				return result;
			}
		}

		public string WeekDayHoliday
		{
			get
			{
				string result = "";
				WeekHolidayStruct[] array = wHolidayInfo;
				for (int i = 0; i < array.Length; i++)
				{
					WeekHolidayStruct weekHolidayStruct = array[i];
					if (CompareWeekDayHoliday(_date, weekHolidayStruct.Month, weekHolidayStruct.WeekAtMonth, weekHolidayStruct.WeekDay))
					{
						result = weekHolidayStruct.HolidayName;
						break;
					}
				}
				return result;
			}
		}

		public string DateHoliday
		{
			get
			{
				string result = "";
				SolarHolidayStruct[] array = sHolidayInfo;
				for (int i = 0; i < array.Length; i++)
				{
					SolarHolidayStruct solarHolidayStruct = array[i];
					if (solarHolidayStruct.Month == _date.Month && solarHolidayStruct.Day == _date.Day)
					{
						result = solarHolidayStruct.HolidayName;
						break;
					}
				}
				return result;
			}
		}

		public DateTime Date
		{
			get
			{
				return _date;
			}
			set
			{
				_date = value;
			}
		}

		public DayOfWeek WeekDay => _date.DayOfWeek;

		public string WeekDayStr => _date.DayOfWeek switch
		{
			DayOfWeek.Sunday => "星期日", 
			DayOfWeek.Monday => "星期一", 
			DayOfWeek.Tuesday => "星期二", 
			DayOfWeek.Wednesday => "星期三", 
			DayOfWeek.Thursday => "星期四", 
			DayOfWeek.Friday => "星期五", 
			_ => "星期六", 
		};

		public string DateString => "公元" + _date.ToLongDateString();

		public bool IsLeapYear => DateTime.IsLeapYear(_date.Year);

		public string ChineseConstellation
		{
			get
			{
				int num = 0;
				num = (_date - ChineseConstellationReferDay).Days % 28;
				if (num < 0)
				{
					return _chineseConstellationName[27 + num];
				}
				return _chineseConstellationName[num];
			}
		}

		public string ChineseHour => GetChineseHour(_datetime);

		public bool IsChineseLeapMonth => _cIsLeapMonth;

		public bool IsChineseLeapYear => _cIsLeapYear;

		public int ChineseDay => _cDay;

		public string ChineseDayString => _cDay switch
		{
			0 => "", 
			10 => "初十", 
			20 => "二十", 
			30 => "三十", 
			_ => nStr2[_cDay / 10].ToString() + nStr1[_cDay % 10], 
		};

		public int ChineseMonth => _cMonth;

		public string ChineseMonthString => (_cIsLeapMonth ? "闰" : "") + _monthString[_cMonth];

		public int ChineseYear => _cYear;

		public string ChineseYearString
		{
			get
			{
				string text = "";
				string text2 = _cYear.ToString();
				for (int i = 0; i < 4; i++)
				{
					text += ConvertNumToChineseNum(text2[i]);
				}
				return text + "年";
			}
		}

		public string ChineseDateString
		{
			get
			{
				if (_cIsLeapMonth)
				{
					return "农历" + ChineseYearString + "闰" + ChineseMonthString + ChineseDayString;
				}
				return "农历" + ChineseYearString + ChineseMonthString + ChineseDayString;
			}
		}

		public string ChineseTwentyFourDay
		{
			get
			{
				if (solarDateDict.ContainsKey(_date.Year))
				{
					List<int> list = solarDateDict[_date.Year];
					int item = _date.Month * 100 + _date.Day;
					if (list.Contains(item))
					{
						int num = list.IndexOf(item);
						if (num > 0)
						{
							return SolarTerm[num];
						}
					}
					return string.Empty;
				}
				DateTime dateTime = new DateTime(1900, 1, 6, 2, 5, 0);
				string result = "";
				int year = _date.Year;
				for (int i = 1; i <= 24; i++)
				{
					double value = 525948.76 * (double)(year - 1900) + (double)sTermInfo[i - 1];
					if (dateTime.AddMinutes(value).DayOfYear == _date.DayOfYear)
					{
						result = SolarTerm[i - 1];
						break;
					}
				}
				return result;
			}
		}

		public string ChineseTwentyFourPrevDay
		{
			get
			{
				if (solarDateDict.ContainsKey(_date.Year))
				{
					List<int> list = solarDateDict[_date.Year];
					int num = _date.Year;
					int dayNum = _date.Month * 100 + _date.Day;
					int num2 = 0;
					string text = string.Empty;
					int num3 = list.FindLastIndex((int day) => day < dayNum);
					if (num3 >= 0)
					{
						num2 = list[num3];
						text = SolarTerm[num3];
					}
					else if (solarDateDict.ContainsKey(_date.Year - 1))
					{
						num = _date.Year - 1;
						num2 = solarDateDict[num].Last();
						text = SolarTerm.Last();
					}
					if (num2 > 0)
					{
						int num4 = num2 / 100;
						string text2 = ((num4 > 9) ? "" : ("0" + num4));
						int num5 = num2 % 100;
						string text3 = ((num5 > 9) ? "" : ("0" + num5));
						return $"{text}[{num}-{text2}-{text3}]";
					}
				}
				DateTime dateTime = new DateTime(1900, 1, 6, 2, 5, 0);
				string result = "";
				int year = _date.Year;
				for (int num6 = 24; num6 >= 1; num6--)
				{
					double value = 525948.76 * (double)(year - 1900) + (double)sTermInfo[num6 - 1];
					DateTime dateTime2 = dateTime.AddMinutes(value);
					if (dateTime2.DayOfYear < _date.DayOfYear)
					{
						result = $"{SolarTerm[num6 - 1]}[{dateTime2:yyyy-MM-dd}]";
						break;
					}
				}
				return result;
			}
		}

		public string ChineseTwentyFourNextDay
		{
			get
			{
				if (solarDateDict.ContainsKey(_date.Year))
				{
					List<int> list = solarDateDict[_date.Year];
					int num = _date.Year;
					int dayNum = _date.Month * 100 + _date.Day;
					int num2 = 0;
					string text = string.Empty;
					int num3 = list.FindIndex((int day) => day > dayNum);
					if (num3 >= 0)
					{
						num2 = list[num3];
						text = SolarTerm[num3];
					}
					else if (solarDateDict.ContainsKey(_date.Year + 1))
					{
						num = _date.Year + 1;
						num2 = solarDateDict[num].First();
						text = SolarTerm.First();
					}
					if (num2 > 0)
					{
						int num4 = num2 / 100;
						string text2 = ((num4 > 9) ? "" : ("0" + num4));
						int num5 = num2 % 100;
						string text3 = ((num5 > 9) ? "" : ("0" + num5));
						return $"{text}[{num}-{text2}-{text3}]";
					}
				}
				DateTime dateTime = new DateTime(1900, 1, 6, 2, 5, 0);
				string result = "";
				int year = _date.Year;
				for (int num6 = 1; num6 <= 24; num6++)
				{
					double value = 525948.76 * (double)(year - 1900) + (double)sTermInfo[num6 - 1];
					DateTime dateTime2 = dateTime.AddMinutes(value);
					if (dateTime2.DayOfYear > _date.DayOfYear)
					{
						result = string.Format("{0}[{1}]", SolarTerm[num6 - 1], dateTime2.ToString("yyyy'-'MM'-'dd", App.Ci));
						break;
					}
				}
				return result;
			}
		}

		public string Constellation
		{
			get
			{
				int num = 0;
				int year = _date.Year;
				int month = _date.Month;
				int day = _date.Day;
				year = month * 100 + day;
				num = ((year < 321 || year > 419) ? ((year >= 420 && year <= 520) ? 1 : ((year >= 521 && year <= 620) ? 2 : ((year >= 621 && year <= 722) ? 3 : ((year >= 723 && year <= 822) ? 4 : ((year >= 823 && year <= 922) ? 5 : ((year >= 923 && year <= 1022) ? 6 : ((year >= 1023 && year <= 1121) ? 7 : ((year >= 1122 && year <= 1221) ? 8 : ((year >= 1222 || year <= 119) ? 9 : ((year >= 120 && year <= 218) ? 10 : ((year >= 219 && year <= 320) ? 11 : 0))))))))))) : 0);
				return _constellationName[num];
			}
		}

		public int Animal => (_date.Year - 1900) % 12 + 1;

		public string AnimalString
		{
			get
			{
				int num = _date.Year - 1900;
				return animalStr[num % 12].ToString();
			}
		}

		public string GanZhiYearString
		{
			get
			{
				int num = (_cYear - 1864) % 60;
				return ganStr[num % 10].ToString() + zhiStr[num % 12] + "年";
			}
		}

		public string GanZhiMonthString
		{
			get
			{
				int num = ((_cMonth <= 10) ? (_cMonth + 2) : (_cMonth - 10));
				string text = zhiStr[num - 1].ToString();
				int num2 = 1;
				switch ((_cYear - 1864) % 60 % 10)
				{
				case 0:
					num2 = 3;
					break;
				case 1:
					num2 = 5;
					break;
				case 2:
					num2 = 7;
					break;
				case 3:
					num2 = 9;
					break;
				case 4:
					num2 = 1;
					break;
				case 5:
					num2 = 3;
					break;
				case 6:
					num2 = 5;
					break;
				case 7:
					num2 = 7;
					break;
				case 8:
					num2 = 9;
					break;
				case 9:
					num2 = 1;
					break;
				}
				return ganStr[(num2 + _cMonth - 2) % 10] + text + "月";
			}
		}

		public string GanZhiDayString
		{
			get
			{
				int num = (_date - GanZhiStartDay).Days % 60;
				return ganStr[num % 10].ToString() + zhiStr[num % 12] + "日";
			}
		}

		public string GanZhiDateString => GanZhiYearString + GanZhiMonthString + GanZhiDayString;

		public ChineseCalendar(DateTime dt)
		{
			if (!CheckDateLimit(dt))
			{
				return;
			}
			_date = dt.Date;
			_datetime = dt;
			int num = 0;
			int num2 = 0;
			int num3 = (_date - MinDay).Days;
			int i;
			for (i = 1900; i <= 2100; i++)
			{
				num2 = GetChineseYearDays(i);
				if (num3 - num2 < 1)
				{
					break;
				}
				num3 -= num2;
			}
			_cYear = i;
			num = GetChineseLeapMonth(_cYear);
			if (num > 0)
			{
				_cIsLeapYear = true;
			}
			else
			{
				_cIsLeapYear = false;
			}
			_cIsLeapMonth = false;
			for (i = 1; i <= 12; i++)
			{
				if (num > 0 && i == num + 1 && !_cIsLeapMonth)
				{
					_cIsLeapMonth = true;
					i--;
					num2 = GetChineseLeapMonthDays(_cYear);
				}
				else
				{
					_cIsLeapMonth = false;
					num2 = GetChineseMonthDays(_cYear, i);
				}
				num3 -= num2;
				if (num3 <= 0)
				{
					break;
				}
			}
			num3 += num2;
			_cMonth = i;
			_cDay = num3;
		}

		public ChineseCalendar(int cy, int cm, int cd, bool leapMonthFlag)
		{
			CheckChineseDateLimit(cy, cm, cd, leapMonthFlag);
			_cYear = cy;
			_cMonth = cm;
			_cDay = cd;
			int num = 0;
			for (int i = 1900; i < cy; i++)
			{
				int chineseYearDays = GetChineseYearDays(i);
				num += chineseYearDays;
			}
			int chineseLeapMonth = GetChineseLeapMonth(cy);
			if (chineseLeapMonth != 0)
			{
				_cIsLeapYear = true;
			}
			else
			{
				_cIsLeapYear = false;
			}
			if (cm != chineseLeapMonth)
			{
				_cIsLeapMonth = false;
			}
			else
			{
				_cIsLeapMonth = leapMonthFlag;
			}
			if (!_cIsLeapYear || cm < chineseLeapMonth || (cm == chineseLeapMonth && !_cIsLeapMonth))
			{
				for (int i = 1; i < cm; i++)
				{
					int chineseYearDays = GetChineseMonthDays(cy, i);
					num += chineseYearDays;
				}
				if (cd > GetChineseMonthDays(cy, cm))
				{
					throw new ChineseCalendarException("不合法的农历日期");
				}
				num += cd;
			}
			else
			{
				for (int i = 1; i < cm; i++)
				{
					int chineseYearDays = GetChineseMonthDays(cy, i);
					num += chineseYearDays;
				}
				if (cm > chineseLeapMonth)
				{
					int chineseYearDays = GetChineseLeapMonthDays(cy);
					num += chineseYearDays;
					if (cd > GetChineseMonthDays(cy, cm))
					{
						throw new ChineseCalendarException("不合法的农历日期");
					}
					num += cd;
				}
				else
				{
					if (_cIsLeapMonth)
					{
						int chineseYearDays = GetChineseMonthDays(cy, cm);
						num += chineseYearDays;
					}
					if (cd > GetChineseLeapMonthDays(cy))
					{
						throw new ChineseCalendarException("不合法的农历日期");
					}
					num += cd;
				}
			}
			_date = MinDay.AddDays(num);
		}

		public int GetChineseMonthDays(int year, int month)
		{
			if (GetLunarDaysInfo(year).MonthDays[month - 1])
			{
				return 30;
			}
			return 29;
		}

		public int GetMonthTotalDays()
		{
			LunarDaysInfo lunarDaysInfo = GetLunarDaysInfo(ChineseYear);
			if (IsChineseLeapMonth)
			{
				if (!lunarDaysInfo.IsLeapMonth30Days)
				{
					return 29;
				}
				return 30;
			}
			if (!lunarDaysInfo.MonthDays[ChineseMonth - 1])
			{
				return 29;
			}
			return 30;
		}

		private int GetChineseLeapMonth(int year)
		{
			return GetLunarDaysInfo(year).LeapMonth;
		}

		private int GetChineseLeapMonthDays(int year)
		{
			LunarDaysInfo lunarDaysInfo = GetLunarDaysInfo(year);
			if (lunarDaysInfo.IsLeapMonth30Days)
			{
				return 30;
			}
			if (!lunarDaysInfo.IsLeapMonth30Days)
			{
				return 29;
			}
			return 0;
		}

		private int GetChineseYearDays(int year)
		{
			LunarDaysInfo lunarDaysInfo = GetLunarDaysInfo(year);
			int num = 0;
			for (int i = 0; i < lunarDaysInfo.MonthDays.Length; i++)
			{
				num += (lunarDaysInfo.MonthDays[i] ? 30 : 29);
			}
			if (lunarDaysInfo.LeapMonth > 0)
			{
				num += (lunarDaysInfo.IsLeapMonth30Days ? 30 : 29);
			}
			return num;
		}

		private string GetChineseHour(DateTime dt)
		{
			int num = dt.Hour;
			if (dt.Minute != 0)
			{
				num++;
			}
			int num2 = num / 2;
			if (num2 >= 12)
			{
				num2 = 0;
			}
			int num3 = (((_date - GanZhiStartDay).Days % 60 % 10 + 1) * 2 - 1) % 10 - 1;
			return (ganStr.Substring(num3) + ganStr.Substring(0, num3 + 2))[num2].ToString() + zhiStr[num2];
		}

		private bool CheckDateLimit(DateTime dt)
		{
			if (dt >= MinDay)
			{
				return dt <= MaxDay;
			}
			return false;
		}

		private void CheckChineseDateLimit(int year, int month, int day, bool leapMonth)
		{
			if (year < 1900 || year > 2100)
			{
				throw new ChineseCalendarException("非法农历日期");
			}
			if (month < 1 || month > 12)
			{
				throw new ChineseCalendarException("非法农历日期");
			}
			if (day < 1 || day > 30)
			{
				throw new ChineseCalendarException("非法农历日期");
			}
			int chineseLeapMonth = GetChineseLeapMonth(year);
			if (leapMonth && month != chineseLeapMonth)
			{
				throw new ChineseCalendarException("非法农历日期");
			}
		}

		private string ConvertNumToChineseNum(char n)
		{
			if (n < '0' || n > '9')
			{
				return "";
			}
			return n switch
			{
				'0' => "零一二三四五六七八九"[0].ToString(), 
				'1' => "零一二三四五六七八九"[1].ToString(), 
				'2' => "零一二三四五六七八九"[2].ToString(), 
				'3' => "零一二三四五六七八九"[3].ToString(), 
				'4' => "零一二三四五六七八九"[4].ToString(), 
				'5' => "零一二三四五六七八九"[5].ToString(), 
				'6' => "零一二三四五六七八九"[6].ToString(), 
				'7' => "零一二三四五六七八九"[7].ToString(), 
				'8' => "零一二三四五六七八九"[8].ToString(), 
				'9' => "零一二三四五六七八九"[9].ToString(), 
				_ => "", 
			};
		}

		private bool BitTest32(int num, int bitpostion)
		{
			if (bitpostion > 31 || bitpostion < 0)
			{
				throw new Exception("Error Param: bitpostion[0-31]:" + bitpostion);
			}
			int num2 = 1 << bitpostion;
			if ((num & num2) == 0)
			{
				return false;
			}
			return true;
		}

		private int ConvertDayOfWeek(DayOfWeek dayOfWeek)
		{
			return dayOfWeek switch
			{
				DayOfWeek.Sunday => 1, 
				DayOfWeek.Monday => 2, 
				DayOfWeek.Tuesday => 3, 
				DayOfWeek.Wednesday => 4, 
				DayOfWeek.Thursday => 5, 
				DayOfWeek.Friday => 6, 
				DayOfWeek.Saturday => 7, 
				_ => 0, 
			};
		}

		private bool CompareWeekDayHoliday(DateTime date, int month, int week, int day)
		{
			bool result = false;
			if (date.Month == month && ConvertDayOfWeek(date.DayOfWeek) == day)
			{
				DateTime dateTime = new DateTime(date.Year, date.Month, 1);
				int num = ConvertDayOfWeek(dateTime.DayOfWeek);
				int num2 = 7 - ConvertDayOfWeek(dateTime.DayOfWeek) + 1;
				if (num > day)
				{
					if ((week - 1) * 7 + day + num2 == date.Day)
					{
						result = true;
					}
				}
				else if (day + num2 + (week - 2) * 7 == date.Day)
				{
					result = true;
				}
			}
			return result;
		}

		public ChineseCalendar NextDay()
		{
			return new ChineseCalendar(_date.AddDays(1.0));
		}

		public ChineseCalendar PervDay()
		{
			return new ChineseCalendar(_date.AddDays(-1.0));
		}

		public static LunarDaysInfo GetLunarDaysInfo(int lunarYear)
		{
			if (lunarDaysDict.ContainsKey(lunarYear))
			{
				return lunarDaysDict[lunarYear];
			}
			LunarDaysInfo lunarDaysInfo = new LunarDaysInfo();
			lunarDaysInfo.LeapMonth = calendar.GetLeapMonth(lunarYear);
			List<bool> list = new List<bool>();
			for (int i = 1; i <= calendar.GetMonthsInYear(lunarYear); i++)
			{
				bool flag = calendar.GetDaysInMonth(lunarYear, i) == 30;
				if (i == lunarDaysInfo.LeapMonth)
				{
					lunarDaysInfo.IsLeapMonth30Days = flag;
				}
				else
				{
					list.Add(flag);
				}
			}
			lunarDaysInfo.MonthDays = list.ToArray();
			if (lunarDaysInfo.LeapMonth > 0)
			{
				lunarDaysInfo.LeapMonth--;
			}
			lunarDaysDict[lunarYear] = lunarDaysInfo;
			return lunarDaysInfo;
		}

		public static int GetMonthTotalDays(int lunarYear, int lunarMonth, bool isLeapMonth)
		{
			LunarDaysInfo lunarDaysInfo = GetLunarDaysInfo(lunarYear);
			if (isLeapMonth && lunarDaysInfo.LeapMonth == lunarMonth)
			{
				if (!lunarDaysInfo.IsLeapMonth30Days)
				{
					return 29;
				}
				return 30;
			}
			if (!lunarDaysInfo.MonthDays[lunarMonth - 1])
			{
				return 29;
			}
			return 30;
		}

		public static (int year, int month, int day) ComputeLunarYearMonthDay(DateTime anchorDate, DateTime date)
		{
			DateTime dateTime = ((anchorDate > date) ? anchorDate : date);
			DateTime dateTime2 = ((anchorDate <= date) ? anchorDate : date);
			dateTime = dateTime.Date;
			dateTime2 = dateTime2.Date;
			int year = calendar.GetYear(dateTime2);
			int month = calendar.GetMonth(dateTime2);
			int dayOfMonth = calendar.GetDayOfMonth(dateTime2);
			int leapMonth = calendar.GetLeapMonth(year);
			int num = ((leapMonth > 0) ? (leapMonth - 1) : 0);
			bool flag = leapMonth == month;
			int num2 = ((leapMonth > 0 && month >= leapMonth) ? (month - 1) : month);
			int year2 = calendar.GetYear(dateTime);
			int month2 = calendar.GetMonth(dateTime);
			int dayOfMonth2 = calendar.GetDayOfMonth(dateTime);
			int leapMonth2 = calendar.GetLeapMonth(year2);
			int num3 = ((leapMonth2 > 0) ? (leapMonth2 - 1) : 0);
			bool flag2 = leapMonth2 == month2;
			int num4 = ((leapMonth2 > 0 && month2 >= leapMonth2) ? (month2 - 1) : month2);
			int daysInMonth = calendar.GetDaysInMonth(year2, month2);
			int val = 0;
			if (year == year2 || num3 == num)
			{
				val = calendar.GetDaysInMonth(year2, month);
			}
			else
			{
				for (int i = 1; i <= calendar.GetMonthsInYear(year2); i++)
				{
					int num5 = i;
					if (leapMonth2 > 0 && i >= leapMonth2)
					{
						num5 = i - 1;
					}
					if (num2 == num5)
					{
						val = calendar.GetDaysInMonth(year2, i);
						break;
					}
				}
			}
			bool flag3 = num4 > num2 || (num4 == num2 && flag2 && !flag) || (num4 == num2 && Math.Min(dayOfMonth2, daysInMonth) >= Math.Min(dayOfMonth, val));
			int num6 = (flag3 ? (year2 - year) : (year2 - year - 1));
			int num7 = 0;
			if (flag3)
			{
				for (int j = 1; j <= 12; j++)
				{
					if (j >= num2 + 1 && j <= num4)
					{
						num7++;
					}
				}
				if (num3 > 0)
				{
					if (num3 == num2 && !flag && num4 > num3)
					{
						num7++;
					}
					else if (num2 + 1 <= num3 && num3 < num4)
					{
						num7++;
					}
					else if (num3 == num4 && flag2 && (num2 != num3 || !flag))
					{
						num7++;
					}
				}
			}
			else
			{
				int year3 = year + num6;
				int num8 = num2;
				int leapMonth3 = calendar.GetLeapMonth(year3);
				int num9 = ((leapMonth3 > 0) ? (leapMonth3 - 1) : 0);
				bool flag4 = leapMonth3 == month && flag;
				for (int k = 1; k <= 12; k++)
				{
					if (k > num8)
					{
						num7++;
					}
				}
				if (num9 > 0)
				{
					if (!flag4 && num9 == num8)
					{
						num7++;
					}
					else if (num9 > num8)
					{
						num7++;
					}
				}
				for (int l = 1; l <= 12; l++)
				{
					if (l < num4)
					{
						num7++;
					}
					else if (l == num4 && !flag2)
					{
						num7++;
					}
				}
				if (num3 > 0 && num3 < num4)
				{
					num7++;
				}
				else if (flag2)
				{
					num7++;
				}
			}
			int num10 = Math.Min(dayOfMonth2, daysInMonth) - Math.Min(dayOfMonth, daysInMonth);
			if (num10 < 0)
			{
				num10 = Math.Min(dayOfMonth2, daysInMonth);
				int num11 = ((month2 > 1) ? calendar.GetDaysInMonth(year2, month2 - 1) : calendar.GetDaysInMonth(year2 - 1, calendar.GetMonthsInYear(year2 - 1)));
				num10 += Math.Max(0, num11 - dayOfMonth);
				num7--;
			}
			return (year: num6, month: num7, day: num10);
		}
	}
}
