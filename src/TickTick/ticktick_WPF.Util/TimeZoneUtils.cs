using System;
using System.Collections.Concurrent;
using System.IO;
using TickTickUtils;
using TickTickUtils.Lang;
using TimeZoneConverter;
using ticktick_WPF.Resource;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Util;

public static class TimeZoneUtils
{
	private static readonly ConcurrentDictionary<string, TimeZoneInfo> _timeZoneInfoCache = new ConcurrentDictionary<string, TimeZoneInfo>(StringComparer.OrdinalIgnoreCase);

	private static readonly ConcurrentDictionary<string, byte> _invalidTimeZoneIds = new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase);

	private static ConcurrentDictionary<string, string> _timeZoneNameDict;

	public static TimeData TransferToSelectedTimeZoneTime(TimeData origin)
	{
		if (origin == null)
		{
			return new TimeData();
		}
		if (origin.IsAllDay == true)
		{
			return origin;
		}
		if (origin.TimeZone?.TimeZone == null)
		{
			origin.TimeZone = new TimeZoneViewModel(TimeZoneInfo.Local);
			return origin;
		}
		if (origin.TimeZone.TimeZone.Equals(TimeZoneInfo.Local))
		{
			return origin;
		}
		TimeData timeData = TimeData.Clone(origin);
		timeData.StartDate = LocalToTargetZoneTime(timeData.StartDate, timeData.TimeZone.TimeZoneName);
		timeData.DueDate = LocalToTargetZoneTime(timeData.DueDate, timeData.TimeZone.TimeZoneName);
		return timeData;
	}

	public static TimeData TransferToLocalTime(TimeData origin)
	{
		if (origin == null)
		{
			return new TimeData();
		}
		if (origin.IsAllDay == true)
		{
			return origin;
		}
		if (origin.TimeZone.TimeZone.Equals(TimeZoneInfo.Local))
		{
			return origin;
		}
		TimeData timeData = TimeData.Clone(origin);
		timeData.StartDate = ((!timeData.StartDate.HasValue) ? timeData.StartDate : new DateTime?(ToLocalTime(timeData.StartDate.Value, origin.TimeZone.TimeZoneName)));
		timeData.DueDate = ((!timeData.DueDate.HasValue) ? timeData.DueDate : new DateTime?(ToLocalTime(timeData.DueDate.Value, origin.TimeZone.TimeZoneName)));
		return timeData;
	}

	public static DateTime ToLocalTime(DateTime time, string timeZoneName)
	{
		TimeZoneInfo timeZoneInfo = GetTimeZoneInfo(timeZoneName);
		try
		{
			return TimeZoneInfo.ConvertTime(DateTime.SpecifyKind(time, DateTimeKind.Unspecified), timeZoneInfo, TimeZoneInfo.Local);
		}
		catch (ArgumentException)
		{
			if (timeZoneInfo.SupportsDaylightSavingTime == TimeZoneInfo.Local.SupportsDaylightSavingTime && timeZoneInfo.BaseUtcOffset == TimeZoneInfo.Local.BaseUtcOffset)
			{
				return time;
			}
			try
			{
				return TimeZoneInfo.ConvertTime(DateTime.SpecifyKind(time.AddHours(-1.0), DateTimeKind.Unspecified), timeZoneInfo, TimeZoneInfo.Local);
			}
			catch (Exception)
			{
				return time;
			}
		}
		catch (Exception)
		{
			return time;
		}
	}

	public static DateTime LocalToTargetTzTime(DateTime time, string timeZoneName)
	{
		TimeZoneInfo timeZoneInfo = GetTimeZoneInfo(timeZoneName);
		return LocalToTargetTzTime(time, timeZoneInfo);
	}

	public static DateTime LocalToTargetTzTime(DateTime time, TimeZoneInfo timeZone)
	{
		try
		{
			return TimeZoneInfo.ConvertTime(time, TimeZoneInfo.Local, timeZone);
		}
		catch (ArgumentException)
		{
			if (timeZone.SupportsDaylightSavingTime == TimeZoneInfo.Local.SupportsDaylightSavingTime && timeZone.BaseUtcOffset == TimeZoneInfo.Local.BaseUtcOffset)
			{
				return time;
			}
			try
			{
				return TimeZoneInfo.ConvertTime(time.AddHours(-1.0), TimeZoneInfo.Local, timeZone);
			}
			catch (Exception)
			{
				return time;
			}
		}
		catch (Exception)
		{
			return time;
		}
	}

	public static DateTime UtcToTargetTzTime(DateTime time, string toZone)
	{
		if (Utils.IsEmptyDate(time))
		{
			return time;
		}
		time = DateTime.SpecifyKind(time, DateTimeKind.Utc);
		TimeZoneInfo timeZoneInfo = GetTimeZoneInfo(toZone);
		return TimeZoneInfo.ConvertTime(time, timeZoneInfo);
	}

	public static DateTime ToUtcTime(DateTime time, string fromZone)
	{
		if (Utils.IsEmptyDate(time))
		{
			return time;
		}
		TimeZoneInfo timeZoneInfo = GetTimeZoneInfo(fromZone);
		return TimeZoneInfo.ConvertTime(DateTime.SpecifyKind(time, DateTimeKind.Unspecified), timeZoneInfo, TimeZoneInfo.Utc);
	}

	public static TimeZoneInfo GetTimeZoneInfo(string name)
	{
		if (string.IsNullOrWhiteSpace(name))
		{
			return TimeZoneInfo.Local;
		}
		name = name.Trim();
		if (_invalidTimeZoneIds.ContainsKey(name))
		{
			return TimeZoneInfo.Local;
		}
		if (_timeZoneInfoCache.TryGetValue(name, out var value))
		{
			return value;
		}
		try
		{
			if (TZConvert.TryGetTimeZoneInfo(name, out value))
			{
				_timeZoneInfoCache.TryAdd(name, value);
				return value;
			}
		}
		catch (Exception)
		{
		}
		if (_invalidTimeZoneIds.TryAdd(name, 0))
		{
			UtilLog.Warn("Unknown time zone id '" + name + "', fallback to local time zone '" + TimeZoneInfo.Local.Id + "'.");
		}
		return TimeZoneInfo.Local;
	}

	public static string GetTimeZoneName(TimeZoneInfo tz)
	{
		if (_timeZoneNameDict == null)
		{
			_timeZoneNameDict = new ConcurrentDictionary<string, string>();
			if (!File.Exists(AppPaths.TimeZoneDictPath))
			{
				try
				{
					File.WriteAllText(AppPaths.TimeZoneDictPath, string.Empty);
				}
				catch (AggregateException)
				{
				}
			}
			else
			{
				string[] array = File.ReadAllLines(AppPaths.TimeZoneDictPath);
				for (int i = 0; i < array.Length; i++)
				{
					string[] array2 = array[i].Split(':');
					if (array2.Length == 2 && !string.IsNullOrEmpty(array2[0]) && array2[1] != "Etc/UTC" && array2[1] != "Asia/Shanghai")
					{
						_timeZoneNameDict[array2[0]] = array2[1];
					}
				}
			}
		}
		string text = null;
		if (!string.IsNullOrEmpty(tz.Id) && _timeZoneNameDict.ContainsKey(tz.Id))
		{
			text = _timeZoneNameDict[tz.Id];
		}
		else
		{
			try
			{
				text = TZConvert.WindowsToIana(tz.Id);
			}
			catch (Exception ex2)
			{
				UtilLog.Info(ExceptionUtils.BuildExceptionMessage(ex2));
				text = "Asia/Shanghai";
			}
			_timeZoneNameDict[tz.Id] = text;
			if (text != "Asia/Shanghai" && text != "Etc/UTC")
			{
				File.AppendAllLines(AppPaths.TimeZoneDictPath, new string[1] { tz.Id + ":" + text });
			}
		}
		return text;
	}

	public static string GetTimeZoneDisplayName(TimeZoneInfo tz)
	{
		return TimeZoneData.GetDisplayNameForTimeZone(tz, App.Ci.ToString());
	}

	public static string GetTimeZoneUtcName(TimeZoneInfo tz)
	{
		return TimeZoneData.GetTimeZoneUtcName(tz, App.Ci.ToString());
	}

	public static DateTime ZoneToTargetTzTime(string zone, DateTime time, string targetZone)
	{
		if (zone == targetZone)
		{
			return time;
		}
		if (!string.IsNullOrEmpty(zone) && !string.IsNullOrEmpty(targetZone))
		{
			TimeZoneInfo timeZoneInfo = GetTimeZoneInfo(zone);
			int num = ((timeZoneInfo.IsDaylightSavingTime(time) && timeZoneInfo.SupportsDaylightSavingTime) ? 1 : 0);
			TimeZoneInfo timeZoneInfo2 = GetTimeZoneInfo(targetZone);
			int num2 = ((timeZoneInfo2.IsDaylightSavingTime(time) && timeZoneInfo2.SupportsDaylightSavingTime) ? 1 : 0);
			return time.AddMinutes((timeZoneInfo2.BaseUtcOffset - timeZoneInfo.BaseUtcOffset).TotalMinutes + (double)(num2 * 60) - (double)(num * 60));
		}
		return time;
	}

	public static DateTime? LocalToTargetZoneTime(DateTime? date, string timeZone)
	{
		if (date.HasValue && date.Value.Year != 1)
		{
			TimeZoneInfo timeZoneInfo = GetTimeZoneInfo(timeZone);
			return TimeZoneInfo.ConvertTime(date.Value, timeZoneInfo);
		}
		return date;
	}

	public static void ClearCache()
	{
		_timeZoneNameDict?.Clear();
	}
}
