using System;
using System.Collections.Generic;
using System.Text;

namespace ticktick_WPF.Util.DateParser;

public class TickTickDuration
{
	public static string OnTime = "TRIGGER:PT0S";

	private bool _isPositive;

	private int _years;

	private int _months;

	private int _weeks;

	private int _days;

	private int _hours;

	private int _minutes;

	private int _seconds;

	public int GetYears()
	{
		return _years;
	}

	public void SetYears(int years)
	{
		_years = years;
	}

	public int GetMonths()
	{
		return _months;
	}

	public void SetMonths(int months)
	{
		_months = months;
	}

	public int GetWeeks()
	{
		return _weeks;
	}

	public void SetWeeks(int weeks)
	{
		_weeks = weeks;
	}

	public int GetDays()
	{
		return _days;
	}

	public void SetDays(int days)
	{
		_days = days;
	}

	public int GetHours()
	{
		return _hours;
	}

	public void SetHours(int hours)
	{
		_hours = hours;
	}

	public int GetMinutes()
	{
		return _minutes;
	}

	public void SetMinutes(int minutes)
	{
		_minutes = minutes;
	}

	public int GetSeconds()
	{
		return _seconds;
	}

	public void SetSeconds(int seconds)
	{
		_seconds = seconds;
	}

	public bool IsPositive()
	{
		return _isPositive;
	}

	public void SetIsPositive(bool isPositive)
	{
		_isPositive = isPositive;
	}

	public TickTickDuration(int years, int months, int weeks, int days, int hours, int minutes, int seconds)
	{
		_years = years;
		_months = months;
		_weeks = weeks;
		_days = days;
		_hours = hours;
		_minutes = minutes;
		_seconds = seconds;
	}

	public TickTickDuration(bool isPositive, int days, int hours, int minutes)
	{
		_isPositive = isPositive;
		_days = days;
		_hours = hours;
		_minutes = minutes;
	}

	public TickTickDuration(bool isPositive, int weeks, int days)
	{
		_isPositive = isPositive;
		_weeks = weeks;
		_days = days;
	}

	public static TickTickDuration Build(string lexicalRepresentation)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(lexicalRepresentation))
			{
				return null;
			}
			return new TickTickDuration(lexicalRepresentation);
		}
		catch (Exception)
		{
		}
		return BuildOntimeDuration();
	}

	private static TickTickDuration BuildOntimeDuration()
	{
		return new TickTickDuration(isPositive: true, 0, 0, 0);
	}

	private TickTickDuration(string lexicalRepresentation)
	{
		string text = lexicalRepresentation.Replace("TRIGGER:", "");
		int[] array = new int[1];
		int length = text.Length;
		bool flag = false;
		array[0] = 0;
		bool isPositive;
		if (length != array[0] && text[array[0]] == '-')
		{
			array[0]++;
			isPositive = false;
		}
		else
		{
			isPositive = true;
		}
		if (length != array[0])
		{
			_ = text[array[0]++];
			_ = 80;
		}
		int num = 0;
		string[] array2 = new string[4];
		int[] array3 = new int[4];
		while (length != array[0] && IsDigit(text[array[0]]) && num < 4)
		{
			array3[num] = array[0];
			array2[num++] = ParsePiece(text, array);
		}
		if (length != array[0] && text[array[0]++] == 'T')
		{
			flag = true;
		}
		int num2 = 0;
		string[] array4 = new string[3];
		int[] array5 = new int[3];
		while (length != array[0] && IsDigitOrPeriod(text[array[0]]) && num2 < 3)
		{
			array5[num2] = array[0];
			array4[num2++] = ParsePiece(text, array);
		}
		if (flag)
		{
		}
		_ = array[0];
		if (num == 0)
		{
		}
		OrganizeParts(array2, array3, num, "YMWD");
		OrganizeParts(array4, array5, num2, "HMS");
		_years = ParseInteger(array2[0]);
		_months = ParseInteger(array2[1]);
		_weeks = ParseInteger(array2[2]);
		_days = ParseInteger(array2[3]);
		_hours = ParseInteger(array4[0]);
		_minutes = ParseInteger(array4[1]);
		_seconds = ParseInteger(array4[2]);
		_isPositive = isPositive;
	}

	private static string ParsePiece(string whole, IList<int> idx)
	{
		int startIndex = idx[0];
		while (idx[0] < whole.Length && IsDigitOrPeriod(whole[idx[0]]))
		{
			idx[0]++;
		}
		_ = idx[0];
		_ = whole.Length;
		idx[0]++;
		return whole.Substring(startIndex, idx[0]);
	}

	private static void OrganizeParts(IList<string> parts, IList<int> partsIndex, int len, string tokens)
	{
		int num = tokens.Length;
		for (int num2 = len - 1; num2 >= 0; num2--)
		{
			int num3 = tokens.LastIndexOf(parts[num2][parts[num2].Length - 1], num - 1);
			_ = -1;
			for (int i = num3 + 1; i < num; i++)
			{
				parts[i] = null;
			}
			num = num3;
			parts[num] = parts[num2];
			partsIndex[num] = partsIndex[num2];
		}
		for (num--; num >= 0; num--)
		{
			parts[num] = null;
		}
	}

	private static bool IsDigit(char ch)
	{
		if ('0' <= ch)
		{
			return ch <= '9';
		}
		return false;
	}

	private static bool IsDigitOrPeriod(char ch)
	{
		if (!IsDigit(ch))
		{
			return ch == '.';
		}
		return true;
	}

	private static int ParseInteger(string part)
	{
		if (part == null)
		{
			return 0;
		}
		part = part.Substring(0, part.Length - 1);
		return int.Parse(part);
	}

	public override bool Equals(object obj)
	{
		if (obj is TickTickDuration tickTickDuration)
		{
			if (_isPositive == tickTickDuration.IsPositive() && _years == tickTickDuration.GetYears() && _months == tickTickDuration.GetMonths() && _weeks == tickTickDuration.GetWeeks() && _days == tickTickDuration.GetDays() && _days == tickTickDuration.GetDays() && _hours == tickTickDuration.GetHours())
			{
				return _minutes == tickTickDuration.GetMinutes();
			}
			return false;
		}
		return false;
	}

	public override string ToString()
	{
		bool flag = HasValue(_years);
		bool flag2 = HasValue(_months);
		bool flag3 = HasValue(_weeks);
		bool flag4 = HasValue(_days);
		bool flag5 = HasValue(_hours);
		bool flag6 = HasValue(_minutes);
		bool flag7 = HasValue(_seconds);
		if (!flag && !flag2 && !flag3 && !flag4 && !flag5 && !flag6 && !flag7)
		{
			return OnTime;
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("TRIGGER:");
		if (!_isPositive)
		{
			stringBuilder.Append('-');
		}
		stringBuilder.Append('P');
		if (flag)
		{
			stringBuilder.Append(_years + "Y");
		}
		if (flag2)
		{
			stringBuilder.Append(_months + "M");
		}
		if (flag3)
		{
			stringBuilder.Append(_weeks + "W");
		}
		if (flag4)
		{
			stringBuilder.Append(_days + "D");
		}
		if (flag5 || flag6 || flag7)
		{
			stringBuilder.Append('T');
			if (flag5)
			{
				stringBuilder.Append(_hours + "H");
			}
			if (flag6)
			{
				stringBuilder.Append(_minutes + "M");
			}
			if (flag7)
			{
				stringBuilder.Append(_seconds + "S");
			}
		}
		return stringBuilder.ToString();
	}

	private static bool HasValue(int value)
	{
		return value != 0;
	}

	public string ToReminderText()
	{
		return ToReminderText(isAllday: false);
	}

	public string ToReminderText(bool isAllday)
	{
		return ToReminderText(isAllday, showUnit: true);
	}

	public string ToReminderText(bool isAllday, bool showUnit)
	{
		string text = "";
		if (isAllday && showUnit)
		{
			text = "（%1$s）";
		}
		if (HasValue(_weeks))
		{
			return " 提前 %d 周";
		}
		if (HasValue(_days))
		{
			if (_days > 0)
			{
				return " 提前 %d 天";
			}
			return "当天";
		}
		if (HasValue(_hours))
		{
			return " 提前 %d 小时";
		}
		if (HasValue(_minutes))
		{
			return " 提前 %d 分钟";
		}
		if (!isAllday)
		{
			return "准时";
		}
		return "当天" + text;
	}
}
