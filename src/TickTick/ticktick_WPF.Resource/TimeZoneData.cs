using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using TickTickUtils.Lang;
using ticktick_WPF.Dal;
using ticktick_WPF.Util;

namespace ticktick_WPF.Resource;

public static class TimeZoneData
{
	public static Dictionary<string, Dictionary<string, string>> DisplayNames;

	public static List<TimeZoneViewModel> Models;

	public static TimeZoneViewModel LocalTimeZoneModel { get; set; }

	public static bool EnableTz => UserDao.IsPro();

	static TimeZoneData()
	{
		DisplayNames = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
		try
		{
			string directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			if (directoryName != null)
			{
				string path = Path.Combine(directoryName, "TimeZoneDisplayName.txt");
				if (File.Exists(path))
				{
					DisplayNames = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(File.ReadAllText(path));
				}
			}
		}
		catch (Exception)
		{
		}
		LocalTimeZoneModel = new TimeZoneViewModel(TimeZoneInfo.Local);
		UtilLog.Info("LocalTimeZone:" + LocalTimeZoneModel.DisplayName + "   " + LocalTimeZoneModel.TimeZoneName);
	}

	public static string GetTimeZoneUtcName(TimeZoneInfo tz, string ci)
	{
		string text = tz?.DisplayName;
		ci = ci?.Replace("-", "_");
		if (!string.IsNullOrEmpty(ci) && DisplayNames.ContainsKey(ci))
		{
			Dictionary<string, string> dictionary = DisplayNames[ci];
			if (tz != null && dictionary.ContainsKey(tz.Id))
			{
				text = dictionary[tz.Id];
			}
		}
		if (!string.IsNullOrEmpty(text))
		{
			text = TimeZoneViewModel.GetShortName(text);
			int num = text?.IndexOf(")", StringComparison.Ordinal) ?? 0;
			if (num > 1)
			{
				string text2 = text.Substring(1, num - 1).Replace(":00", "");
				if (text2.Length > 2 && text2[text2.Length - 2] == '0')
				{
					text2 = text2.Substring(0, text2.Length - 2) + text2[text2.Length - 1];
				}
				return text2;
			}
		}
		return text;
	}

	public static string GetDisplayNameForTimeZone(TimeZoneInfo tz, string ci)
	{
		string text = tz?.DisplayName;
		ci = ci?.Replace("-", "_");
		if (!string.IsNullOrEmpty(ci) && DisplayNames.ContainsKey(ci))
		{
			Dictionary<string, string> dictionary = DisplayNames[ci];
			if (tz != null && dictionary.ContainsKey(tz.Id))
			{
				text = dictionary[tz.Id];
			}
		}
		if (!string.IsNullOrEmpty(text))
		{
			text = TimeZoneViewModel.GetShortName(text);
			int num = text?.IndexOf(")", StringComparison.Ordinal) ?? 0;
			if (num > 1)
			{
				string text2 = text.Substring(1, num - 1);
				text = text.Substring(num + 1).Trim() + ", " + text2;
			}
		}
		return text;
	}

	public static void GetTimeZoneModels()
	{
		if (Models != null)
		{
			return;
		}
		Models = new List<TimeZoneViewModel>();
		foreach (TimeZoneInfo item in TimeZoneInfo.GetSystemTimeZones().ToList())
		{
			try
			{
				Models.Add(new TimeZoneViewModel
				{
					TimeZone = item,
					DisplayName = TimeZoneUtils.GetTimeZoneDisplayName(item),
					TimeZoneName = TimeZoneUtils.GetTimeZoneName(item),
					IsFloat = false
				});
			}
			catch (Exception)
			{
			}
		}
		Models.Sort(delegate(TimeZoneViewModel a, TimeZoneViewModel b)
		{
			int num = a.TimeZone.BaseUtcOffset.CompareTo(b.TimeZone.BaseUtcOffset);
			return (num == 0) ? string.Compare(a.DisplayName, b.DisplayName, new CultureInfo(2052), CompareOptions.None) : num;
		});
	}
}
