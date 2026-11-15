using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Ical.Net;
using Ical.Net.DataTypes;
using ticktick_WPF.Resource;

namespace ticktick_WPF.Util.DateParser;

public class EnglishParser : IParser
{
	public static readonly bool IsUsOrUkDateFormat = DateUtils.GetSysDateFormatType() != DateFormatType.DMY;

	public DateTime? MatchDate(string inputText, DateTime baseDate, ref List<string> recgonizeStrList)
	{
		bool isUsOrUkDateFormat = IsUsOrUkDateFormat;
		Match match = new Regex("(?i)(\\(|\\b)(3[0-1]|[0-2]?[0-9])([/-])(3[0-1]|[0-2]?[0-9])([/-])?(\\d{4})(\\)|\\b)(\\s)*").Match(inputText);
		int year = baseDate.Year;
		if (match.Success)
		{
			OldDateParser.AddMatchString(recgonizeStrList, match.Groups[0].Value);
			string n;
			string value;
			if (isUsOrUkDateFormat)
			{
				n = match.Groups[2].Value.Trim();
				value = match.Groups[4].Value;
			}
			else
			{
				n = match.Groups[4].Value.Trim();
				value = match.Groups[2].Value;
			}
			int month = OldDateParser.TranslateNum(n);
			int day = OldDateParser.TranslateNum(value);
			if (!OldDateParser.IsNull(match.Groups[6].Value) && !OldDateParser.IsNull(match.Groups[6].Value.Trim()))
			{
				string n2 = match.Groups[6].Value;
				if (match.Groups[6].Value.Length == 2)
				{
					n2 = "20" + match.Groups[6].Value;
				}
				year = OldDateParser.TranslateNum(n2);
			}
			return new DateTime(year, month, day);
		}
		match = new Regex("(?i)(\\(|\\b)(\\d{4})([/-])?(1[0-2]|0?[1-9])([/-])(3[0-1]|[0-2]?[0-9])(\\)|\\b)(\\s)*").Match(inputText);
		if (match.Success)
		{
			OldDateParser.AddMatchString(recgonizeStrList, match.Groups[0].Value);
			string n3 = match.Groups[4].Value.Trim();
			string value2 = match.Groups[6].Value;
			int month = OldDateParser.TranslateNum(n3);
			int day = OldDateParser.TranslateNum(value2);
			string value3 = match.Groups[2].Value;
			if (!OldDateParser.IsNull(value3))
			{
				string n4 = value3;
				if (value3.Length == 2)
				{
					n4 = "20" + value3;
				}
				year = OldDateParser.TranslateNum(n4);
			}
			return new DateTime(year, month, day);
		}
		match = new Regex("(?i)(\\(|\\b)(3[0-1]|[0-2]?[0-9])([/-])(3[0-1]|[0-2]?[0-9])([/-])?(\\d{2})(\\)|\\b)(\\s)*").Match(inputText);
		if (match.Success)
		{
			OldDateParser.AddMatchString(recgonizeStrList, match.Groups[0].Value);
			string n5;
			string value4;
			if (isUsOrUkDateFormat)
			{
				n5 = match.Groups[2].Value.Trim();
				value4 = match.Groups[4].Value;
			}
			else
			{
				n5 = match.Groups[4].Value.Trim();
				value4 = match.Groups[2].Value;
			}
			int month = OldDateParser.TranslateNum(n5);
			int day = OldDateParser.TranslateNum(value4);
			if (!string.IsNullOrEmpty(match.Groups[6].Value))
			{
				string n6 = match.Groups[6].Value;
				if (match.Groups[6].Value.Length == 2)
				{
					n6 = "20" + match.Groups[6].Value;
				}
				year = OldDateParser.TranslateNum(n6);
			}
			return new DateTime(year, month, day);
		}
		match = new Regex("(?i)(?<![\\d|\\.])(\\(|\\b)(3[0-1]|[0-2]?[0-9])([/-])(3[0-1]|[0-2]?[0-9])([/-])?(\\)|\\b)(?!\\.)(\\s)*").Match(inputText);
		if (match.Success)
		{
			OldDateParser.AddMatchString(recgonizeStrList, match.Groups[0].Value);
			string n7;
			string value5;
			if (isUsOrUkDateFormat)
			{
				n7 = match.Groups[2].Value.Trim();
				value5 = match.Groups[4].Value;
			}
			else
			{
				n7 = match.Groups[4].Value.Trim();
				value5 = match.Groups[2].Value;
			}
			int month = OldDateParser.TranslateNum(n7);
			int day = OldDateParser.TranslateNum(value5);
			if (!string.IsNullOrEmpty(match.Groups[6].Value))
			{
				string n8 = match.Groups[6].Value;
				if (match.Groups[6].Value.Length == 2)
				{
					n8 = "20" + match.Groups[6].Value;
				}
				year = OldDateParser.TranslateNum(n8);
			}
			DateTime? dateTime = new DateTime(year, month, day);
			if (dateTime < baseDate)
			{
				dateTime = OldDateParser.GetNextDate(month, hasDay: true, dateTime.Value);
			}
			return dateTime;
		}
		return null;
	}

	public DateTime? MatchMonth(string inputText, DateTime baseDate, ref List<string> recgonizeStrList)
	{
		Match match = new Regex(GetMonthRecognition()).Match(inputText);
		int year = baseDate.Year;
		if (match.Success)
		{
			OldDateParser.AddMatchString(recgonizeStrList, match.Groups[0].Value);
			int enMonthCalendar = GetEnMonthCalendar(match.Groups[6].Value);
			bool hasDay = true;
			int day;
			if (!string.IsNullOrEmpty(match.Groups[3].Value))
			{
				day = OldDateParser.TranslateNum(match.Groups[3].Value);
			}
			else if (!string.IsNullOrEmpty(match.Groups[20].Value))
			{
				day = OldDateParser.TranslateNum(match.Groups[20].Value);
			}
			else
			{
				day = 1;
				hasDay = false;
			}
			if (!string.IsNullOrEmpty(match.Groups[23].Value))
			{
				string text = match.Groups[23].Value.Trim();
				year = OldDateParser.TranslateNum(text);
				if (text.Length <= 3)
				{
					year += 2000;
				}
				year = year;
			}
			bool flag = string.IsNullOrEmpty(match.Groups[23].Value);
			DateTime dateTime = new DateTime(year, enMonthCalendar, day);
			if (dateTime < baseDate && flag)
			{
				dateTime = OldDateParser.GetNextDate(enMonthCalendar, hasDay, dateTime);
			}
			return dateTime;
		}
		match = new Regex("(3[0-1]|[0-2]?[0-9])(st\\.?|ed\\.?|rd\\.?|th\\.?|nd\\.?)").Match(inputText);
		if (match.Success)
		{
			OldDateParser.AddMatchString(recgonizeStrList, match.Groups[0].Value);
			int enMonthCalendar = baseDate.Month;
			int day = OldDateParser.TranslateNum(match.Groups[1].Value);
			DateTime dateTime2 = new DateTime(year, enMonthCalendar, day);
			if (dateTime2 < baseDate)
			{
				dateTime2 = dateTime2.AddMonths(1);
			}
			return dateTime2;
		}
		return null;
	}

	private static string GetMonthRecognition()
	{
		return "(?i)(\\(|\\b)((\\s?(3[0-1]|[0-2]?[0-9]))(st\\.?|ed\\.?|rd\\.?|th\\.?|nd\\.?)?\\s*of\\s*)?(jan(\\.|uary|\\b)|feb(\\.|ruary|\\b)|mar(\\.|ch|\\b)|apr(\\.|il|\\b)|may(\\.|\\b)|jun(\\.|e|\\b)|jul(\\.|y|\\b)|aug(\\.|ust|\\b)|sep(t\\.|\\.|tember|\\b)|oct(\\.|ober|\\b)|nov(\\.|ember|\\b)|dec(\\.|ember|\\b))(\\s?(3[0-1]|[0-2]?[0-9]))?(st\\.?|ed\\.?|rd\\.?|th\\.?|nd\\.?)?,?( *(\\d{4}))?";
	}

	public DateTime? MatchWeekday(string inputText, DateTime baseDate, ref List<string> recgonizeStrList)
	{
		Match match = new Regex(GetDayOfWeekRecog()).Match(inputText);
		if (match.Success)
		{
			OldDateParser.AddMatchString(recgonizeStrList, match.Groups[0].Value);
			DateTime dateTime = baseDate.AddDays((int)baseDate.DayOfWeek * -1);
			int num = TranslateDayOfWeek(match.Groups[2].Value);
			if (num != -1)
			{
				dateTime = dateTime.AddDays(num);
			}
			if (!OldDateParser.IsNull(match.Groups[1].Value))
			{
				string text = match.Groups[1].Value.ToLower().Trim();
				if (!(text == "next"))
				{
					if (text == "last")
					{
						dateTime = dateTime.AddDays(-7.0);
					}
				}
				else
				{
					dateTime = dateTime.AddDays(7.0);
				}
			}
			else if (dateTime < baseDate)
			{
				dateTime = dateTime.AddDays(7.0);
			}
			return dateTime;
		}
		return null;
	}

	public DateTime? MatchSpecialDay(string inputText, DateTime baseDate, ref List<string> recgonizeStrList)
	{
		string[] specialDayRecognition = GetSpecialDayRecognition();
		for (int i = 0; i < specialDayRecognition.Length; i++)
		{
			Match match = new Regex(specialDayRecognition[i]).Match(inputText);
			if (match.Success)
			{
				OldDateParser.AddMatchString(recgonizeStrList, match.Groups[0].Value);
				switch (i)
				{
				case 0:
					return baseDate.AddDays(-1.0);
				case 1:
					return baseDate;
				case 2:
					return baseDate.AddDays(1.0);
				}
				break;
			}
		}
		return null;
	}

	public TickTickDuration MatchAdvanceTime(string matchStr, ref List<string> recgonizeStrList)
	{
		string str = matchStr;
		try
		{
			str = Numerizer.Numerize(str);
		}
		catch (Exception)
		{
			str = matchStr;
		}
		Match match = new Regex("(?i)(remind|reminds)\\s+(\\S+ )?(\\d{1,2}|half a|half an|a|an)\\s+(minutes|minute|mins|min|hours|hour|h|days|day|weeks|week)\\s+(early|earlier|before|in advance)").Match(str);
		if (match.Success)
		{
			TickTickDuration duration = OldDateParser.GetDuration(match.Groups[3].Value, match.Groups[4].Value, isPositive: false);
			if (duration != null)
			{
				return duration;
			}
		}
		match = new Regex("(?i)(remind|reminds)\\s+(\\S+ ){1,2}(early|earlier)").Match(str);
		if (match.Success)
		{
			TickTickDuration tickTickDuration = new TickTickDuration(0, 0, 0, 0, 0, 5, 0);
			tickTickDuration.SetIsPositive(isPositive: false);
			return tickTickDuration;
		}
		return null;
	}

	public bool MatchAfterTime(string content, ref DateTime? baseTime, ref List<string> recgonizeStrList)
	{
		string input;
		try
		{
			input = Numerizer.Numerize(content);
		}
		catch (Exception)
		{
			input = content;
		}
		Match match = new Regex("(?i)(in|after)\\s+(\\d{1,3}|half a|half an|a|an)\\s*(hours|hour|hrs|hr|h)\\s+(\\d{1,3}|a|an)\\s+(minutes|minute|mins|min)").Match(input);
		if (match.Success)
		{
			OldDateParser.AddMatchString(recgonizeStrList, match.Groups[0].Value);
			string value = match.Groups[2].Value;
			int num;
			if (OldDateParser.IsAnOrA(value))
			{
				num = 60;
			}
			else if (OldDateParser.IsHalf(value))
			{
				num = 30;
			}
			else
			{
				num = ((!OldDateParser.IsNull(value)) ? OldDateParser.TranslateNum(value) : 0);
				num *= 60;
			}
			value = match.Groups[4].Value;
			num = ((!OldDateParser.IsAnOrA(value)) ? (num + ((!OldDateParser.IsNull(value)) ? OldDateParser.TranslateNum(value) : 0)) : (num + 1));
			TickTickDuration duration = OldDateParser.GetDuration(num, match.Groups[5].Value, isPositive: true);
			if (duration != null && baseTime.HasValue)
			{
				baseTime = OldDateParser.GetDateByDuration(baseTime.Value, duration);
				return true;
			}
		}
		match = new Regex("(?i)(\\d{1,3}|half a|half an|a|an)\\s*(hours|hour|hrs|hr|h)\\s+(\\d{1,3}|a|an)\\s+(minutes|minute|mins|min)\\s+later").Match(input);
		if (match.Success)
		{
			OldDateParser.AddMatchString(recgonizeStrList, match.Groups[0].Value);
			string value2 = match.Groups[1].Value;
			int num2;
			if (OldDateParser.IsAnOrA(value2))
			{
				num2 = 60;
			}
			else if (OldDateParser.IsHalf(value2))
			{
				num2 = 30;
			}
			else
			{
				num2 = ((!OldDateParser.IsNull(value2)) ? OldDateParser.TranslateNum(value2) : 0);
				num2 *= 60;
			}
			value2 = match.Groups[3].Value;
			num2 = ((!OldDateParser.IsAnOrA(value2)) ? (num2 + ((!OldDateParser.IsNull(value2)) ? OldDateParser.TranslateNum(value2) : 0)) : (num2 + 1));
			TickTickDuration duration2 = OldDateParser.GetDuration(num2, match.Groups[4].Value, isPositive: true);
			if (duration2 != null && baseTime.HasValue)
			{
				baseTime = OldDateParser.GetDateByDuration(baseTime.Value, duration2);
				return true;
			}
		}
		match = new Regex("(?i)(in|after)\\s+(\\d{1,3}|half a|half an|a|an)\\s*(minutes|minute|mins|min|hours|hour|hrs|hr|h|days|day|d|weeks|week|wk|months|month|years|year|yrs|yr)").Match(input);
		if (match.Success)
		{
			OldDateParser.AddMatchString(recgonizeStrList, match.Groups[0].Value);
			TickTickDuration duration3 = OldDateParser.GetDuration(match.Groups[2].Value, match.Groups[3].Value);
			if (duration3 != null && baseTime.HasValue)
			{
				baseTime = OldDateParser.GetDateByDuration(baseTime.Value, duration3);
				return true;
			}
		}
		match = new Regex("(?i)(\\d{1,3}|half a|half an|a|an)\\s*(minutes|minute|mins|min|hours|hour|hrs|hr|h|days|day|d|weeks|week|wk|months|month|years|year|yrs|yr)\\s+later").Match(input);
		if (match.Success)
		{
			TickTickDuration duration4 = OldDateParser.GetDuration(match.Groups[1].Value, match.Groups[2].Value);
			if (duration4 != null && baseTime.HasValue)
			{
				baseTime = OldDateParser.GetDateByDuration(baseTime.Value, duration4);
				return true;
			}
		}
		return false;
	}

	public bool MatchSpecialTime(string inputText, ref DateTime? parseDate, DateTime baseTime, ref List<string> recgonizeStrList)
	{
		bool flag = !parseDate.HasValue;
		Dictionary<string, int> enSpecialHourMap = GetEnSpecialHourMap();
		foreach (string key in enSpecialHourMap.Keys)
		{
			Match match = new Regex(key).Match(inputText);
			if (match.Success)
			{
				OldDateParser.AddMatchString(recgonizeStrList, match.Groups[0].Value);
				int num = enSpecialHourMap[key];
				if (!parseDate.HasValue)
				{
					parseDate = baseTime.Date;
				}
				parseDate = DateUtils.SetHourAndMinuteOnly(parseDate.Value, 0, 0);
				parseDate = parseDate.Value.AddHours(num);
				if (flag && parseDate.Value <= baseTime)
				{
					parseDate = parseDate.Value.AddDays(1.0);
				}
				return true;
			}
		}
		return false;
	}

	private static Dictionary<string, int> GetEnSpecialHourMap()
	{
		return new Dictionary<string, int>
		{
			{ "(?i)\\bbreakfast\\b", 8 },
			{ "(?i)\\blunch\\b", 12 },
			{ "(?i)\\bsupper\\b", 18 },
			{ "(?i)\\bbrunch\\b", 10 },
			{ "(?i)\\bmorning\\b", 9 },
			{ "(?i)\\bafternoon\\b", 13 },
			{ "(?i)\\bevening\\b", 17 },
			{ "(?i)\\bnight\\b", 20 },
			{ "(?i)\\btonight\\b", 21 },
			{ "(?i)\\bmidnight\\b", 24 },
			{ "(?i)\\bnoon\\b", 12 }
		};
	}

	public bool MatchTime(string inputText, ref DateTime? parseDate, DateTime baseTime, ref List<string> recgonizeStrList)
	{
		bool flag = false;
		if (!parseDate.HasValue)
		{
			flag = true;
			parseDate = new DateTime(baseTime.Year, baseTime.Month, baseTime.Day, baseTime.Hour, baseTime.Minute, baseTime.Second);
		}
		foreach (string item in GetEnTimePattern())
		{
			Match match = new Regex(item).Match(inputText);
			if (!match.Success)
			{
				continue;
			}
			OldDateParser.AddMatchString(recgonizeStrList, match.Groups[0].Value);
			int num = MatchEnglishNum(match.Groups[2].Value);
			int minutes = 0;
			if (num <= 12 && !OldDateParser.IsNull(match.Groups[4].Value))
			{
				num %= 12;
			}
			bool flag2 = false;
			if (!string.IsNullOrEmpty(match.Groups[3].Value))
			{
				minutes = OldDateParser.TranslateNum(match.Groups[3].Value);
			}
			if (num <= 12)
			{
				if (!string.IsNullOrEmpty(match.Groups[4].Value))
				{
					switch (AmpmToNumber(match.Groups[4].Value))
					{
					case 0:
						if (num == 12)
						{
							num = 0;
						}
						break;
					case 1:
						if (num != 12)
						{
							num += 12;
						}
						break;
					}
					flag2 = true;
				}
				else
				{
					if (new Regex("(?i)(\\bmorning|\\bmidnight)()").Match(inputText).Success)
					{
						flag2 = true;
					}
					if (new Regex("(?i)(\\bafternoon|\\bevening|\\bnight|\\btonight|\\bnoon)").Match(inputText).Success)
					{
						num += 12;
						flag2 = true;
					}
				}
			}
			parseDate = DateUtils.SetHourAndMinuteOnly(parseDate.Value, 0, minutes);
			parseDate = parseDate.Value.AddHours(num % 24);
			if (flag)
			{
				if (num <= 12 && OldDateParser.IsNull(match.Groups[4].Value) && !flag2)
				{
					if (parseDate <= baseTime)
					{
						parseDate = parseDate.Value.AddHours((num > 0) ? 12 : 24);
					}
					if (parseDate <= baseTime)
					{
						parseDate = parseDate.Value.AddHours(12.0);
					}
				}
				else if (MatchEnglishNum(match.Groups[2].Value) != 0 && parseDate <= baseTime)
				{
					parseDate = parseDate.Value.AddDays(1.0);
				}
			}
			return true;
		}
		parseDate = null;
		return false;
	}

	private static int AmpmToNumber(string amPmString)
	{
		int result = 1;
		if (amPmString == null)
		{
			return result;
		}
		string text = amPmString.ToLower().Trim();
		if (!OldDateParser.IsNull(text) && text.Contains("a"))
		{
			result = 0;
		}
		if (!OldDateParser.IsNull(text) && text.Contains("p"))
		{
			result = 1;
		}
		return result;
	}

	public RecurrencePattern MatchRepeat(string content, ref DateTime? parseDate, DateTime baseDate, ref List<string> recgonizeStrList)
	{
		RecurrencePattern recurrencePattern = new RecurrencePattern();
		Dictionary<string, FrequencyType> dictionary = new Dictionary<string, FrequencyType>
		{
			{
				"(?i)\\b(every|each) ?\\w{0,6} days?\\b",
				FrequencyType.Daily
			},
			{
				"(?i)\\b(every|each) ?\\w{0,6} ?nights?\\b",
				FrequencyType.Daily
			},
			{
				"(?i)\\b(every|each) ?\\w{0,6} ?mornings?\\b",
				FrequencyType.Daily
			},
			{
				"(?i)\\b(every|each) ?\\w{0,6} ?evenings?\\b",
				FrequencyType.Daily
			},
			{
				"(?i)\\b(every|each) ?\\w{0,6} ?afternoons?\\b",
				FrequencyType.Daily
			},
			{
				"(?i)\\b(every|each) \\w{0,6} ?weeks?\\b",
				FrequencyType.Weekly
			},
			{
				"(?i)\\b(every|each) \\w{0,6} ?weekend(s)?\\b",
				FrequencyType.Weekly
			},
			{
				"(?i)\\b(every|each) \\w{0,6} ?weekday(s)?\\b",
				FrequencyType.Weekly
			},
			{
				"(?i)\\b(every|each) \\w{0,6} ?(mon|tues|tue|wednes|wed|thurs|thu|fri|satur|sat|sun)(day)?(s)?\\b",
				FrequencyType.Weekly
			},
			{
				"(?i)\\b(\\d{1,2})?(st|nd|rd|ed|th|)?(last)? ?(day of)? ?(every|each) \\w{0,6} ?months?\\b",
				FrequencyType.Monthly
			},
			{
				"(?i)\\b(every|each) \\w{0,6} ?years?\\b",
				FrequencyType.Yearly
			}
		};
		Dictionary<string, FrequencyType> dictionary2 = new Dictionary<string, FrequencyType>
		{
			{
				"(?i)\\bdaily\\b",
				FrequencyType.Daily
			},
			{
				"(?i)\\beveryday\\b",
				FrequencyType.Daily
			},
			{
				"(?i)\\bweekly\\b",
				FrequencyType.Weekly
			},
			{
				"(?i)\\bmonthly\\b",
				FrequencyType.Monthly
			},
			{
				"(?i)\\byearly\\b",
				FrequencyType.Yearly
			}
		};
		foreach (string key in dictionary.Keys)
		{
			Match match = new Regex(key).Match(content);
			if (!match.Success)
			{
				continue;
			}
			OldDateParser.AddMatchString(recgonizeStrList, match.Groups[0].Value);
			int year = baseDate.Year;
			int month = baseDate.Month;
			FrequencyType frequencyType = dictionary[key];
			recurrencePattern = new RecurrencePattern
			{
				Frequency = frequencyType,
				Interval = FindInterval(content)
			};
			switch (frequencyType)
			{
			case FrequencyType.Monthly:
				if (!OldDateParser.IsNull(match.Groups[3].Value))
				{
					recurrencePattern.ByMonthDay = new List<int> { -1 };
					parseDate = new DateTime(year, month, DateTime.DaysInMonth(year, month));
				}
				else if (!OldDateParser.IsNull(match.Groups[1].Value))
				{
					int num2 = OldDateParser.TranslateNum(match.Groups[1].Value);
					recurrencePattern.ByMonthDay = new List<int> { num2 };
					parseDate = new DateTime(year, month, num2);
					if (parseDate < baseDate)
					{
						parseDate = parseDate.Value.AddMonths(1);
					}
				}
				recurrencePattern.Frequency = FrequencyType.Monthly;
				break;
			case FrequencyType.Weekly:
				if (match.Groups.Count > 2)
				{
					int num = 0;
					string value = match.Groups[2].Value;
					if (!OldDateParser.IsNull(value) && value.Contains("mon"))
					{
						num = 1;
					}
					else if (!OldDateParser.IsNull(value) && value.Contains("tue"))
					{
						num = 2;
					}
					else if (!OldDateParser.IsNull(value) && value.Contains("wed"))
					{
						num = 3;
					}
					else if (!OldDateParser.IsNull(value) && value.Contains("thu"))
					{
						num = 4;
					}
					else if (!OldDateParser.IsNull(value) && value.Contains("fri"))
					{
						num = 5;
					}
					else if (!OldDateParser.IsNull(value) && value.Contains("sat"))
					{
						num = 6;
					}
					else if (!OldDateParser.IsNull(value) && value.Contains("sun"))
					{
						num = 0;
					}
					parseDate = baseDate.AddDays((int)baseDate.DayOfWeek * -1).AddDays(num);
					if (parseDate < baseDate)
					{
						parseDate = parseDate.Value.AddDays(7.0);
					}
					recurrencePattern.ByWeekNo = new List<int> { num };
					return recurrencePattern;
				}
				break;
			}
			return null;
		}
		foreach (string key2 in dictionary2.Keys)
		{
			Match match2 = new Regex(key2).Match(content);
			if (match2.Success)
			{
				OldDateParser.AddMatchString(recgonizeStrList, match2.Groups[0].Value);
				FrequencyType frequency = dictionary2[key2];
				recurrencePattern.Frequency = frequency;
				recurrencePattern.Interval = 1;
				return recurrencePattern;
			}
		}
		return recurrencePattern;
	}

	private static int FindInterval(string inputText)
	{
		Dictionary<string, int> dictionary = new Dictionary<string, int>();
		string[] array = new string[12]
		{
			"one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten",
			"eleven", "twelve"
		};
		for (int i = 0; i < array.Length; i++)
		{
			dictionary.Add(array[i], i + 1);
			dictionary.Add((i + 1).ToString(), i + 1);
		}
		dictionary.Add("other", 2);
		Regex regex = new Regex("(?i)\\bevery (\\w*)\\b");
		int result = 1;
		Match match = regex.Match(inputText);
		if (match.Success && !string.IsNullOrEmpty(match.Groups[1].Value))
		{
			string value = match.Groups[1].Value;
			if (dictionary.ContainsKey(value))
			{
				result = dictionary[value];
			}
			else
			{
				try
				{
					result = int.Parse(value);
				}
				catch (Exception)
				{
				}
			}
		}
		return result;
	}

	private static IEnumerable<string> GetEnTimePattern()
	{
		return new string[5] { "(?i)\\s*(\\b)([0-2]?\\d)(?::([0-5]\\d))? ?([ap][\\.|m]m?\\.?)(\\s)*", "(?i)\\b(([0-2]?[0-9])[:|：]([0-5][0-9]))(\\b)(\\s)*", "(?i)\\b(([0-2]?\\d)() ?o'? ?clock) ?([ap]\\.?m\\.?)?\\b(\\s)*", "(?i)(\\bat)\\s{1,}([01]?\\d)()(())($|\\s)*", "(?i)(\\b)(one|two|three|four|five|six|seven|eight|nine|ten|eleven|twelve)( ?)([ap]\\.?m?\\.?)?\\b(\\s)*" };
	}

	private static string[] GetSpecialDayRecognition()
	{
		return new string[3] { "(?i)(\\(|\\b)yesterday(\\)|\\b)(\\s)*", "(?i)(\\(|\\b)tod(ay|\\)|\\b|\\.)(\\s)*", "(?i)(\\(|\\b)tom(orrow|\\)|\\b|\\.)(\\s)*" };
	}

	private static int MatchEnglishNum(string s)
	{
		Dictionary<string, int> dictionary = new Dictionary<string, int>
		{
			{ "one", 1 },
			{ "two", 2 },
			{ "three", 3 },
			{ "four", 4 },
			{ "five", 5 },
			{ "six", 6 },
			{ "seven", 7 },
			{ "eight", 8 },
			{ "nine", 9 },
			{ "ten", 10 },
			{ "eleven", 11 },
			{ "twelve", 12 }
		};
		string key = s.Trim().ToLower();
		if (dictionary.ContainsKey(key))
		{
			return dictionary[key];
		}
		return OldDateParser.TranslateNum(s);
	}

	private static int TranslateDayOfWeek(string day)
	{
		Utils.TupleList<string, int> tupleList = new Utils.TupleList<string, int>
		{
			{ "mon", 1 },
			{ "tue", 2 },
			{ "wed", 3 },
			{ "thu", 4 },
			{ "fri", 5 },
			{ "sat", 6 },
			{ "sun", 7 }
		};
		string text = day.Trim().ToLower();
		if (!string.IsNullOrEmpty(text))
		{
			foreach (Tuple<string, int> item in tupleList)
			{
				if (text.Contains(item.Item1))
				{
					return item.Item2;
				}
			}
		}
		return -1;
	}

	private static string GetDayOfWeekRecog()
	{
		return "(?i)(^|\\b|next |last |this )(sun|mon|tue|wed|thu|fri|sat)((day|sday|nesday|rsday|urday)(\\)|\\b)|(\\)|\\.|(\\b)))(\\s)*";
	}

	private static int GetEnMonthCalendar(string month)
	{
		Utils.TupleList<string, int> tupleList = new Utils.TupleList<string, int>
		{
			{ "jan", 1 },
			{ "feb", 2 },
			{ "mar", 3 },
			{ "apr", 4 },
			{ "may", 5 },
			{ "jun", 6 },
			{ "jul", 7 },
			{ "aug", 8 },
			{ "sep", 9 },
			{ "oct", 10 },
			{ "nov", 11 },
			{ "dec", 12 }
		};
		string text = month.Trim().ToLower();
		if (!string.IsNullOrEmpty(text))
		{
			foreach (Tuple<string, int> item in tupleList)
			{
				if (text.Contains(item.Item1))
				{
					return item.Item2;
				}
			}
		}
		return -1;
	}
}
