using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Ical.Net;
using Ical.Net.DataTypes;

namespace ticktick_WPF.Util.DateParser;

public class ChineseParser : IParser
{
	public RecurrencePattern MatchRepeat(string matchStr, ref DateTime? parseDate, DateTime baseDate, ref List<string> recgonizeStrList)
	{
		RecurrencePattern recurrencePattern = new RecurrencePattern();
		Match match = new Regex("每(\\d{1,2})?[年|个](\\d{1,2}|" + GetChinaNumPattern(12) + ")[/|\\-|－|月|\\s](\\d{1,2}|" + GetChinaNumPattern(31) + ")[日|号]").Match(matchStr);
		if (match.Success)
		{
			OldDateParser.AddMatchString(recgonizeStrList, match.Groups[0].Value);
			recurrencePattern.Interval = (OldDateParser.IsNull(match.Groups[1].Value) ? 1 : OldDateParser.TranslateNum(match.Groups[1].Value));
			recurrencePattern.Frequency = FrequencyType.Yearly;
			int year = baseDate.Year;
			int month = OldDateParser.TranslateNum(match.Groups[2].Value);
			int day = OldDateParser.TranslateNum(match.Groups[3].Value);
			parseDate = new DateTime(year, month, day);
			if (parseDate < baseDate.Date)
			{
				parseDate = OldDateParser.GetNextDate(month, hasDay: true, parseDate.Value);
			}
			return recurrencePattern;
		}
		match = new Regex("每(\\d{1,2}|" + GetChinaNumPattern(12) + "|两)?(个)?(日|天|礼拜|星期|周|月|年)?(工作日|末)?(最后一天|第)?(\\d{1,2}|天|日|" + GetChinaNumPattern(31) + ")?(天|号|月)?").Match(matchStr);
		if (match.Success)
		{
			OldDateParser.AddMatchString(recgonizeStrList, match.Groups[0].Value);
		}
		if (match.Success && !OldDateParser.IsNull(match.Groups[2].Value) && !OldDateParser.IsNull(match.Groups[3].Value))
		{
			recurrencePattern.Interval = 1;
			recurrencePattern.Frequency = FrequencyType.Yearly;
		}
		else if (match.Success && !OldDateParser.IsNull(match.Groups[3].Value))
		{
			recurrencePattern.Interval = (OldDateParser.IsNull(match.Groups[1].Value) ? 1 : OldDateParser.TranslateNum(match.Groups[1].Value));
			string value = match.Groups[3].Value;
			if (value != null)
			{
				int length = value.Length;
				if (length != 1)
				{
					if (length == 2)
					{
						char c = value[0];
						if (c != '星')
						{
							if (c == '礼' && value == "礼拜")
							{
								goto IL_0344;
							}
						}
						else if (value == "星期")
						{
							goto IL_0344;
						}
					}
				}
				else
				{
					char c = value[0];
					if ((uint)c <= 22825u)
					{
						if (c == '周')
						{
							goto IL_0344;
						}
						if (c == '天')
						{
							goto IL_0313;
						}
					}
					else if (c != '年')
					{
						if (c == '日')
						{
							goto IL_0313;
						}
						if (c == '月')
						{
							recurrencePattern.Frequency = FrequencyType.Monthly;
							int year2 = baseDate.Year;
							int month2 = baseDate.Month;
							if ("最后一天" == match.Groups[5].Value)
							{
								recurrencePattern.ByMonthDay = new List<int> { -1 };
								int day2 = DateTime.DaysInMonth(year2, month2);
								parseDate = new DateTime(year2, month2, day2);
							}
							else if (!OldDateParser.IsNull(match.Groups[6].Value))
							{
								recurrencePattern.ByMonthDay = new List<int> { OldDateParser.TranslateNum(match.Groups[6].Value) };
								int day3 = OldDateParser.TranslateNum(match.Groups[6].Value);
								parseDate = new DateTime(year2, month2, day3);
								if (parseDate.Value.Date < baseDate.Date)
								{
									parseDate = parseDate.Value.AddMonths(1);
								}
							}
							else
							{
								recurrencePattern.ByMonthDay = new List<int> { baseDate.Day };
								parseDate = new DateTime(year2, month2, baseDate.Day);
								if (parseDate.Value.Date < baseDate.Date)
								{
									parseDate = parseDate.Value.AddMonths(1);
								}
							}
						}
					}
					else
					{
						recurrencePattern.Frequency = FrequencyType.Yearly;
						if (!OldDateParser.IsNull(match.Groups[6].Value) && !OldDateParser.IsNull(match.Groups[7].Value) && "月" == match.Groups[7].Value)
						{
							int year2 = baseDate.Year;
							int month2 = OldDateParser.TranslateNum(match.Groups[6].Value);
							parseDate = new DateTime(year2, month2, baseDate.Day);
							if (parseDate.Value.Date < baseDate.Date)
							{
								parseDate = OldDateParser.GetNextDate(month2, hasDay: false, parseDate.Value);
							}
						}
						else
						{
							parseDate = DateTime.Today.Date;
						}
					}
				}
			}
		}
		goto IL_06ad;
		IL_0344:
		recurrencePattern.Frequency = FrequencyType.Weekly;
		if (!OldDateParser.IsNull(match.Groups[6].Value))
		{
			int num = OldDateParser.TranslateNum(match.Groups[6].Value);
			if (num > 0 && num < 8)
			{
				recurrencePattern.ByWeekNo = new List<int> { num };
			}
			parseDate = baseDate.AddDays((int)baseDate.DayOfWeek * -1).AddDays(num);
			if (parseDate.Value.Date < baseDate.Date)
			{
				parseDate = parseDate.Value.AddDays(7.0);
			}
		}
		else
		{
			parseDate = new DateTime(baseDate.Year, baseDate.Month, baseDate.Day);
		}
		goto IL_06ad;
		IL_06ad:
		return recurrencePattern;
		IL_0313:
		recurrencePattern.Frequency = FrequencyType.Daily;
		parseDate = new DateTime(baseDate.Year, baseDate.Month, baseDate.Day);
		goto IL_06ad;
	}

	public TickTickDuration MatchAdvanceTime(string matchStr, ref List<string> recgonizeStrList)
	{
		Match match = new Regex("(提前|提早)(\\d{1,2}|" + GetChinaNumPattern(60) + "|半|两)(分钟|小时|天|周|星期|礼拜)").Match(matchStr);
		if (match.Success)
		{
			OldDateParser.AddMatchString(recgonizeStrList, match.Groups[0].Value);
			return OldDateParser.GetDuration(match.Groups[2].Value, match.Groups[3].Value, isPositive: false);
		}
		match = new Regex("(提前|提早)(提醒我)").Match(matchStr);
		if (match.Success)
		{
			OldDateParser.AddMatchString(recgonizeStrList, match.Groups[0].Value);
			TickTickDuration tickTickDuration = new TickTickDuration(0, 0, 0, 0, 0, 5, 0);
			tickTickDuration.SetIsPositive(isPositive: false);
			return tickTickDuration;
		}
		return null;
	}

	public bool MatchAfterTime(string matchStr, ref DateTime? baseTime, ref List<string> recgonizeStrList)
	{
		bool flag = false;
		Match match = new Regex("(\\d{1,3}|两|" + GetChinaNumPattern(60) + ")(个)?(小时)(\\d{1,3}|两|" + GetChinaNumPattern(60) + ")(分钟|分)(后|之后|以后)").Match(matchStr);
		if (match.Success)
		{
			flag = true;
			OldDateParser.AddMatchString(recgonizeStrList, match.Groups[0].Value);
			string value = match.Groups[1].Value;
			int num = (OldDateParser.IsAnOrA(value) ? 1 : ((!OldDateParser.IsNull(value)) ? OldDateParser.TranslateNum(value) : 0));
			string value2 = match.Groups[4].Value;
			int num2 = (OldDateParser.IsAnOrA(value2) ? 1 : ((!OldDateParser.IsNull(value2)) ? OldDateParser.TranslateNum(value2) : 0));
			int num3 = ((num2 < 60) ? (num * 60 + num2) : num2);
			TickTickDuration duration = OldDateParser.GetDuration(num3, match.Groups[5].Value, isPositive: true);
			if (duration != null && baseTime.HasValue)
			{
				baseTime = OldDateParser.GetDateByDuration(baseTime.Value, duration);
			}
		}
		if (!flag)
		{
			match = new Regex("(\\d{1,3}|两|" + GetChinaNumPattern(60) + ")?(个)?(半)(个)?(小时)(后|之后|以后)").Match(matchStr);
			if (match.Success)
			{
				OldDateParser.AddMatchString(recgonizeStrList, match.Groups[0].Value);
				flag = true;
				string value3 = match.Groups[1].Value;
				int num4 = (OldDateParser.IsAnOrA(value3) ? 1 : ((!OldDateParser.IsNull(value3)) ? OldDateParser.TranslateNum(value3) : 0));
				TickTickDuration duration2 = OldDateParser.GetDuration(num4 * 60 + 30, "分钟", isPositive: true);
				if (duration2 != null && baseTime.HasValue)
				{
					baseTime = OldDateParser.GetDateByDuration(baseTime.Value, duration2);
				}
			}
		}
		if (!flag)
		{
			match = new Regex("(\\d{1,3}|两|" + GetChinaNumPattern(60) + ")(个)?(分钟|分|小时|天|周|星期|礼拜|月|年)(后|之后|以后)").Match(matchStr);
			if (match.Success)
			{
				OldDateParser.AddMatchString(recgonizeStrList, match.Groups[0].Value);
				flag = true;
				TickTickDuration duration3 = OldDateParser.GetDuration(match.Groups[1].Value, match.Groups[3].Value);
				if (duration3 != null && baseTime.HasValue)
				{
					baseTime = OldDateParser.GetDateByDuration(baseTime.Value, duration3);
				}
			}
		}
		return flag;
	}

	public bool MatchTime(string text, ref DateTime? parseDate, DateTime baseDate, ref List<string> recgonizeStrList)
	{
		IEnumerable<string> matchTimePattern = GetMatchTimePattern();
		string amFlag = GetAmFlag();
		string pmFlag = GetPmFlag();
		bool flag = false;
		foreach (string item in matchTimePattern)
		{
			Match match = new Regex(item).Match(text);
			if (!match.Success)
			{
				continue;
			}
			OldDateParser.AddMatchString(recgonizeStrList, match.Groups[0].Value);
			int num = OldDateParser.MatchNum(match.Groups[3].Value);
			int num2 = num;
			int minutes = ((!string.IsNullOrEmpty(match.Groups[5].Value)) ? OldDateParser.MatchNum(match.Groups[5].Value) : 0);
			if (num <= 12)
			{
				Match match2 = new Regex(amFlag).Match(text);
				Match match3 = new Regex(pmFlag).Match(text);
				if (match2.Success)
				{
					flag = true;
					if (text.Contains("中午"))
					{
						if (num < 3)
						{
							num2 += 12;
						}
					}
					else if (text.Contains("凌晨") && num == 12)
					{
						num2 = 0;
					}
				}
				else if (match3.Success)
				{
					flag = true;
					num2 += 12;
					if (match3.Groups[0].Value.Contains("明"))
					{
						num2 += 24;
					}
				}
			}
			bool flag2 = true;
			if (!parseDate.HasValue)
			{
				flag2 = false;
				parseDate = baseDate.Date;
			}
			parseDate = DateUtils.SetHourAndMinuteOnly(parseDate.Value, 0, minutes);
			parseDate = parseDate.Value.AddHours(num2);
			if (!flag2)
			{
				if (parseDate <= baseDate)
				{
					parseDate = ((flag || num2 > 12) ? parseDate.Value.AddDays(1.0) : parseDate.Value.AddHours(12.0));
					if (parseDate <= baseDate)
					{
						parseDate = parseDate.Value.AddHours(12.0);
					}
				}
				if (num2 == 0 && !flag)
				{
					parseDate = DateUtils.SetHourAndMinuteOnly(parseDate.Value, 0, minutes);
					if (parseDate <= baseDate)
					{
						parseDate = parseDate.Value.AddDays(1.0);
					}
				}
			}
			else if (text.Contains("今天") && num2 < 12 && parseDate < baseDate)
			{
				parseDate = parseDate.Value.AddHours(12.0);
			}
			return true;
		}
		return false;
	}

	public bool MatchSpecialTime(string matchStr, ref DateTime? parseDate, DateTime baseDate, ref List<string> recgonizeStrList)
	{
		bool flag = !parseDate.HasValue;
		Match match = new Regex("(早上|上午|中午|下午|傍晚|晚上|凌晨|午夜|半夜|明晚|今晚|明早)").Match(matchStr);
		if (match.Success)
		{
			OldDateParser.AddMatchString(recgonizeStrList, match.Groups[0].Value);
			Dictionary<string, int> specialHourMap = GetSpecialHourMap();
			string value = match.Groups[0].Value;
			if (!parseDate.HasValue)
			{
				parseDate = baseDate.Date;
			}
			parseDate = DateUtils.SetHourAndMinuteOnly(parseDate.Value, 0, 0);
			parseDate = parseDate.Value.AddHours(specialHourMap[value]);
			if (parseDate.Value <= baseDate && flag)
			{
				parseDate = parseDate.Value.AddDays(1.0);
			}
			return true;
		}
		return false;
	}

	private static Dictionary<string, int> GetSpecialHourMap()
	{
		return new Dictionary<string, int>
		{
			{ "早上", 7 },
			{ "上午", 9 },
			{ "中午", 12 },
			{ "下午", 13 },
			{ "傍晚", 17 },
			{ "晚上", 20 },
			{ "今晚", 20 },
			{ "凌晨", 24 },
			{ "午夜", 24 },
			{ "半夜", 26 },
			{ "明早", 31 },
			{ "明晚", 44 }
		};
	}

	private static IEnumerable<string> GetMatchTimePattern()
	{
		string amFlag = GetAmFlag();
		string pmFlag = GetPmFlag();
		return new string[4]
		{
			amFlag + "?" + pmFlag + "?\\s?(\\d{1,2}|" + GetChinaNumPattern(24) + "|零|两)\\s*点\\s*((\\d{1,2}|半|1?3?一?三?刻|零?两|" + GetChinaNumPattern(60) + ")\\s*(分)?)?" + amFlag + "?" + pmFlag + "?",
			amFlag + "?" + pmFlag + "?\\s?(\\d{1,2}|" + GetChinaNumPattern(24) + "|零|两)\\s*点钟\\s*((\\d{1,2}|零?两|" + GetChinaNumPattern(60) + ")\\s*(分)?)?" + amFlag + "?" + pmFlag + "?",
			amFlag + "?" + pmFlag + "?\\s?(\\d{1,2}|" + GetChinaNumPattern(24) + "|零|两)\\s*[点|:|：]\\s*((\\d{1,2}|半|1?3?一?三?刻)())?" + amFlag + "?" + pmFlag + "?",
			amFlag + "?" + pmFlag + "?\\s?(\\d{1,2}|" + GetChinaNumPattern(24) + "|零|两)\\s*点钟\\s*((\\d{1,2}|半|1?3?一?三?刻)())?" + amFlag + "?" + pmFlag + "?"
		};
	}

	private static string GetPmFlag()
	{
		return "(下午|傍晚|晚上|明晚|今晚)";
	}

	private static string GetAmFlag()
	{
		return "(早上|上午|中午|凌晨|午夜|半夜|明早)";
	}

	public DateTime? MatchSpecialDay(string text, DateTime baseDate, ref List<string> recgonizeStrList)
	{
		Match match = new Regex(GetSpecialDateRecognition()).Match(text);
		if (match.Success)
		{
			string value = match.Groups[0].Value;
			if (GetSpecialDateMaps().ContainsKey(value))
			{
				OldDateParser.AddMatchString(recgonizeStrList, match.Groups[0].Value);
				int num = GetSpecialDateMaps()[value];
				return baseDate.AddDays(num);
			}
		}
		return null;
	}

	private static string GetSpecialDateRecognition()
	{
		return "(大前天|前天|昨天|今天|明天|后天|大后天)";
	}

	private static Dictionary<string, int> GetSpecialDateMaps()
	{
		return new Dictionary<string, int>
		{
			{ "大前天", -3 },
			{ "前天", -2 },
			{ "昨天", -1 },
			{ "今天", 0 },
			{ "明天", 1 },
			{ "后天", 2 },
			{ "大后天", 3 }
		};
	}

	public DateTime? MatchWeekday(string text, DateTime baseDate, ref List<string> recgonizeStrList)
	{
		Match match = new Regex(GetWeekDayRecognition()).Match(text);
		if (match.Success)
		{
			OldDateParser.AddMatchString(recgonizeStrList, match.Groups[0].Value);
			string value = match.Groups[2].Value;
			string value2 = match.Groups[4].Value;
			string value3 = match.Groups[1].Value;
			if (!string.IsNullOrEmpty(value2))
			{
				int num = OldDateParser.MatchNum(value2);
				if (value.Contains("下"))
				{
					num += 7;
				}
				DateTime dateTime = baseDate.Date.AddDays((int)baseDate.DayOfWeek * -1).AddDays(num);
				if (dateTime < baseDate.Date && string.IsNullOrEmpty(value3))
				{
					dateTime = dateTime.AddDays(7.0);
				}
				return dateTime;
			}
		}
		return null;
	}

	private static string GetWeekDayRecognition()
	{
		return "(本|这|这个)?(下个|下)?(星期|礼拜|周)(一|二|三|四|五|六|日|天|1|2|3|4|5|6|7)";
	}

	public DateTime? MatchMonth(string text, DateTime baseDate, ref List<string> recgonizeStrList)
	{
		return null;
	}

	public DateTime? MatchDate(string text, DateTime baseDate, ref List<string> recgonizeStrList)
	{
		DateTime? matchDateResult = GetMatchDateResult(GetChineseMonthDayYearPattern(), text, 5, 1, 3, 4, 2, baseDate, ref recgonizeStrList);
		if (matchDateResult.HasValue)
		{
			return matchDateResult;
		}
		foreach (string item in GetChineseDatePattern())
		{
			DateTime? matchDateResult2 = GetMatchDateResult(item, text, 1, 3, 5, 2, 4, baseDate, ref recgonizeStrList);
			if (matchDateResult2.HasValue)
			{
				return matchDateResult2;
			}
		}
		return null;
	}

	private static DateTime? GetMatchDateResult(string pattern, string matchStr, int yearGroupIndex, int monthGroupIndex, int dayOfMonthGroupIndex, int yearUnitGroupIndex, int monthUnitGroupIndex, DateTime baseDate, ref List<string> recgonizeStrList)
	{
		Match match = new Regex(pattern).Match(matchStr);
		if (match.Success)
		{
			OldDateParser.AddMatchString(recgonizeStrList, match.Groups[0].Value);
			string text = match.Groups[yearGroupIndex].Value;
			string text2 = match.Groups[monthGroupIndex].Value;
			string text3 = match.Groups[dayOfMonthGroupIndex].Value;
			string value = match.Groups[yearUnitGroupIndex].Value;
			string value2 = match.Groups[monthUnitGroupIndex].Value;
			if ("年" != value && "月" != value2 && !string.IsNullOrEmpty(value) && value != value2)
			{
				text3 = text2;
				text2 = text;
				text = "";
			}
			if (!string.IsNullOrEmpty(text) && !string.IsNullOrEmpty(text2))
			{
				int year = int.Parse(text);
				int month = OldDateParser.MatchNum(text2);
				int day = OldDateParser.MatchNum(text3);
				return new DateTime(year, month, day);
			}
			if (!string.IsNullOrEmpty(text2))
			{
				int year2 = baseDate.Year;
				int month2 = OldDateParser.MatchNum(text2);
				int day2 = (string.IsNullOrEmpty(text3) ? 1 : OldDateParser.MatchNum(text3));
				bool hasDay = !string.IsNullOrEmpty(text3);
				DateTime dateTime = new DateTime(year2, month2, day2);
				if (dateTime.Date < baseDate.Date)
				{
					dateTime = OldDateParser.GetNextDate(month2, hasDay, dateTime);
				}
				return dateTime;
			}
			if (!string.IsNullOrEmpty(text3))
			{
				int year3 = baseDate.Year;
				int month3 = baseDate.Month;
				int day3 = OldDateParser.MatchNum(text3);
				DateTime dateTime2 = new DateTime(year3, month3, day3);
				if (dateTime2.Date < baseDate.Date)
				{
					dateTime2 = (string.IsNullOrEmpty(text2) ? dateTime2.AddMonths(1) : OldDateParser.GetNextDate(month3, hasDay: true, dateTime2));
				}
				return dateTime2;
			}
		}
		return null;
	}

	private static string GetChineseMonthDayYearPattern()
	{
		return "(?i)(\\d{1,2}|" + GetChinaNumPattern(12) + ")([\\/|\\-|\\－|月|\\s])(\\d{1,2}|" + GetChinaNumPattern(31) + "|上旬|中旬|下旬)?[日|号]?([\\/|\\-|\\－|年|\\s])(\\d{4})";
	}

	private static IEnumerable<string> GetChineseDatePattern()
	{
		return new string[3]
		{
			"(?i)(\\d{2,4})([\\/|\\-|\\－|年|\\s])(\\d{1,2}|" + GetChinaNumPattern(12) + ")([\\/|\\-|\\－|月|\\s])(\\d{1,2}|" + GetChinaNumPattern(31) + "|上旬|中旬|下旬)?[日|号]?",
			"(?i)()()(?<![\\d|\\.])(\\d{1,2}|下|下个|" + GetChinaNumPattern(12) + ")([\\/|\\-|\\－|月])(\\d{1,2}|" + GetChinaNumPattern(31) + "|上旬|中旬|下旬)?[日|号]?",
			"(?i)()()()()(\\d{1,2}|" + GetChinaNumPattern(31) + "|上旬|中旬|下旬)[日|号]"
		};
	}

	public static string GetChinaNumPattern(int num)
	{
		Dictionary<int, string> dictionary = new Dictionary<int, string> { { 6, "零?六|零?五|零?四|零?三|零?二|零?一" } };
		dictionary.Add(12, "十二|十一|十|零?九|零?八|零?七|" + dictionary[6]);
		dictionary.Add(24, "二十四|二十三|二十二|二十一|二十|十九|十八|十七|十六|十五|十四|十三|" + dictionary[12]);
		dictionary.Add(31, "三十一|三十|二十九|二十八|二十七|二十六|二十五|" + dictionary[24]);
		dictionary.Add(60, "六十|五十九|五十八|五十七|五十六|五十五|五十四|五十三|五十二|五十一|五十|四十九|四十八|四十七|四十六|四十五|四十四|四十三|四十二|四十一|四十|三十九|三十八|三十七|三十六|三十五|三十四|三十三|三十二|" + dictionary[31]);
		if (!dictionary.ContainsKey(num))
		{
			return dictionary[60];
		}
		return dictionary[num];
	}
}
