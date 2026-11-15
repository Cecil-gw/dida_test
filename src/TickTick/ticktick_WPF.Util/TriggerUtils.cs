using System;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace ticktick_WPF.Util;

public static class TriggerUtils
{
	private static ConcurrentDictionary<string, TimeSpan> _triggerSpans = new ConcurrentDictionary<string, TimeSpan>();

	public static TimeSpan ParseTrigger(string trigger)
	{
		if (string.IsNullOrEmpty(trigger))
		{
			return default(TimeSpan);
		}
		if (_triggerSpans.ContainsKey(trigger))
		{
			return _triggerSpans[trigger];
		}
		TimeSpan timeSpan = (IsLegacyTrigger(trigger) ? LegacyTrigger2TimeSpan(trigger) : Trigger2TimeSpan(trigger));
		_triggerSpans[trigger] = timeSpan;
		return timeSpan;
	}

	public static string ConvertLegacyTrigger(string trigger)
	{
		try
		{
			if (string.IsNullOrEmpty(trigger))
			{
				return string.Empty;
			}
			if (trigger == "TRIGGER:PT0S")
			{
				return "TRIGGER:P0DT9H0M0S";
			}
			if (IsLegacyTrigger(trigger))
			{
				Match match = new Regex("(-)?P((\\d{0,})Y)?((\\d{0,})M)?((\\d{0,})W)?((\\d{0,})D)?T?((\\d{0,})H)?((\\d{0,})M)?((\\d{0,})S)?").Match(trigger);
				if (match.Success)
				{
					int.TryParse(match.Groups[7].ToString(), out var result);
					int.TryParse(match.Groups[9].ToString(), out var result2);
					return "TRIGGER:-P" + (result * 7 + result2 - 1) + "DT15H0M0S";
				}
			}
			return trigger;
		}
		catch (Exception)
		{
			return trigger;
		}
	}

	private static bool IsLegacyTrigger(string trigger)
	{
		return !trigger.Contains("0S");
	}

	private static TimeSpan Trigger2TimeSpan(string trigger)
	{
		Match match = new Regex("(-)?P((\\d{0,})Y)?((\\d{0,})M)?((\\d{0,})W)?((\\d{0,})D)?T?((\\d{0,})H)?((\\d{0,})M)?((\\d{0,})S)?").Match(trigger);
		if (match.Success)
		{
			bool num = match.Groups[1].ToString() == "-";
			int.TryParse(match.Groups[7].ToString(), out var result);
			int.TryParse(match.Groups[9].ToString(), out var result2);
			int.TryParse(match.Groups[11].ToString(), out var result3);
			int.TryParse(match.Groups[13].ToString(), out var result4);
			result2 = result * 7 + result2;
			if (!num)
			{
				return new TimeSpan(0, result3 * -1, result4 * -1, 0);
			}
			return new TimeSpan(result2, result3, result4, 0);
		}
		return TimeSpan.Zero;
	}

	private static TimeSpan LegacyTrigger2TimeSpan(string trigger)
	{
		if (!string.IsNullOrEmpty(trigger))
		{
			if (trigger == "TRIGGER:PT0S")
			{
				return TimeSpan.Zero;
			}
			int num;
			if ((num = trigger.IndexOf("D", StringComparison.Ordinal)) != -1 && num > 10 && int.TryParse(trigger.Substring(10, num - 10), out var result))
			{
				return new TimeSpan(result, 0, 0, 0);
			}
			if ((num = trigger.IndexOf("W", StringComparison.Ordinal)) != -1 && num > 10 && int.TryParse(trigger.Substring(10, num - 10), out result))
			{
				return new TimeSpan(result * 7, 0, 0, 0);
			}
			if ((num = trigger.IndexOf("M", StringComparison.Ordinal)) != -1 && num > 11 && int.TryParse(trigger.Substring(11, num - 11), out result))
			{
				return new TimeSpan(0, result, 0);
			}
			if ((num = trigger.IndexOf("H", StringComparison.Ordinal)) != -1 && num > 11 && int.TryParse(trigger.Substring(11, num - 11), out result))
			{
				return new TimeSpan(result, 0, 0);
			}
		}
		return TimeSpan.Zero;
	}

	public static int TriggerToReminder(string trigger)
	{
		Match match = new Regex("(-)?P((\\d{0,})Y)?((\\d{0,})M)?((\\d{0,})W)?T?((\\d{0,})D)?T?((\\d{0,})H)?((\\d{0,})M)?((\\d{0,})S)?").Match(trigger);
		if (match.Success)
		{
			int num = 1;
			if (!trigger.Contains("-"))
			{
				num = -1;
			}
			int.TryParse(match.Groups[7].ToString(), out var result);
			int.TryParse(match.Groups[9].ToString(), out var result2);
			int.TryParse(match.Groups[11].ToString(), out var result3);
			int.TryParse(match.Groups[13].ToString(), out var result4);
			return num * (result * 10080 + result2 * 1440 + result3 * 60 + result4);
		}
		return 0;
	}

	public static string ReminderToTrigger(int minutes)
	{
		if (minutes == 0)
		{
			return "TRIGGER:PT0S";
		}
		if (minutes % 10080 == 0)
		{
			int num = minutes / 10080;
			if (num <= 0)
			{
				return $"TRIGGER:PT{Math.Abs(num)}WT";
			}
			return $"TRIGGER:-P{num}WT";
		}
		if (minutes % 1440 == 0)
		{
			int num2 = minutes / 1440;
			if (num2 <= 0)
			{
				return $"TRIGGER:P{Math.Abs(num2)}DT";
			}
			return $"TRIGGER:-P{num2}DT";
		}
		if (minutes % 60 == 0)
		{
			int num3 = minutes / 60;
			if (num3 <= 0)
			{
				return $"TRIGGER:PT{Math.Abs(num3)}H";
			}
			return $"TRIGGER:-PT{num3}H";
		}
		if (minutes <= 0)
		{
			return $"TRIGGER:PT{Math.Abs(minutes)}M";
		}
		return $"TRIGGER:-PT{minutes}M";
	}
}
