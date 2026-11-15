using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TickTickUtils.Lang;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;

namespace ticktick_WPF.Util;

public static class HolidayManager
{
	private static List<HolidayModel> _holidays;

	private static readonly ConcurrentDictionary<DateTime, HolidayModel> HolidayDict = new ConcurrentDictionary<DateTime, HolidayModel>();

	private static List<string> _twentyFourDay = new List<string>
	{
		"小寒", "大寒", "立春", "雨水", "惊蛰", "春分", "谷雨", "立夏", "小满", "芒种",
		"夏至", "小暑", "大暑", "立秋", "处暑", "白露", "秋分", "寒露", "霜降", "立冬",
		"小雪", "大雪", "冬至"
	};

	public static async Task TryPullRemoteHolidays()
	{
		long lastPullHolidayTime = LocalSettings.Settings.ExtraSettings.LastPullHolidayTime;
		bool needPull = false;
		if (lastPullHolidayTime == 0L)
		{
			LocalSettings.Settings.ExtraSettings.LastPullHolidayTime = DateTime.Now.Ticks;
			needPull = true;
		}
		else
		{
			long ticks = DateTime.Now.Ticks - lastPullHolidayTime;
			if (new TimeSpan(ticks).TotalDays >= 1.0)
			{
				LocalSettings.Settings.ExtraSettings.LastPullHolidayTime = DateTime.Now.Ticks;
				needPull = true;
			}
		}
		_holidays = await HolidayDao.GetRecentHolidays();
		UtilLog.Info("TryPullRemoteHolidays.RecentHolidays " + _holidays.Count);
		if (_holidays != null && _holidays.Count > 0)
		{
			if (Utils.IsJp())
			{
				if (_holidays.Exists((HolidayModel item) => item.region != HolidayRegion.jp))
				{
					_holidays?.Clear();
					needPull = true;
				}
			}
			else if (Utils.IsCn() && _holidays.Exists((HolidayModel item) => item.region != HolidayRegion.cn))
			{
				_holidays?.Clear();
				needPull = true;
			}
		}
		if (_holidays == null || !_holidays.Any())
		{
			needPull = true;
		}
		if (needPull)
		{
			await RefreshDataFromRemote();
		}
	}

	public static async Task RefreshDataFromRemote()
	{
		if (Utils.IsShowHoliday())
		{
			List<HolidayModel> holidays = await Communicator.GetRecentHoliday();
			if (holidays != null && holidays.Count > 0)
			{
				await HolidayDao.ClearHolidays();
				await HolidayDao.SaveHolidays(holidays);
				_holidays = holidays;
			}
		}
	}

	public static List<HolidayModel> GetHolidays()
	{
		return _holidays;
	}

	public static async Task<List<HolidayModel>> GetRecentHolidays()
	{
		if (!Utils.IsShowHoliday())
		{
			return new List<HolidayModel>();
		}
		if (_holidays != null && _holidays.Count > 0)
		{
			return _holidays;
		}
		List<HolidayModel> list = await HolidayDao.GetRecentHolidays();
		if (list == null || list.Count == 0)
		{
			await RefreshDataFromRemote();
			_holidays = await HolidayDao.GetRecentHolidays();
		}
		else
		{
			_holidays = list;
		}
		return _holidays;
	}

	public static async Task ForcePullHolidays()
	{
		await RefreshDataFromRemote();
		_holidays = await HolidayDao.GetRecentHolidays();
	}

	public static List<HolidayModel> GetCacheHolidays()
	{
		if (!Utils.IsShowHoliday())
		{
			return new List<HolidayModel>();
		}
		return _holidays?.ToList() ?? new List<HolidayModel>();
	}

	public static HolidayModel GetHoliday(DateTime date)
	{
		if (HolidayDict.Count == 0)
		{
			List<HolidayModel> holidays = _holidays;
			if (holidays != null && holidays.Count > 0)
			{
				foreach (HolidayModel holiday in _holidays)
				{
					HolidayDict.TryAdd(holiday.date, holiday);
				}
			}
		}
		HolidayDict.TryGetValue(date, out var value);
		return value;
	}

	public static bool IsHolidayText(string text)
	{
		if (Utils.IsJp())
		{
			HolidayModel holidayModel = GetCacheHolidays().FirstOrDefault((HolidayModel item) => item != null && item.name != null && item.name.Equals(text));
			if (holidayModel != null && holidayModel.region == HolidayRegion.jp && LocalSettings.Settings.EnableHoliday)
			{
				return true;
			}
			return false;
		}
		bool? flag = LocalSettings.Settings.UserPreference?.alternativeCalendar?.calendar.Equals("lunar");
		if (flag.HasValue && !flag.Value)
		{
			return false;
		}
		if (!string.IsNullOrWhiteSpace(text) && (text.Any(char.IsNumber) || text.Contains("初") || text.Contains("廿") || text.Contains("十") || text.Contains("三") || text.Contains("月") || _twentyFourDay.Contains(text)))
		{
			return false;
		}
		return true;
	}

	public static bool IsWorkDay(DateTime date)
	{
		List<HolidayModel> list = ((LocalSettings.Settings.EnableHoliday && Utils.IsDida()) ? GetCacheHolidays() : new List<HolidayModel>());
		if (list.Exists((HolidayModel model) => model.date.Date == date.Date && model.type == 1))
		{
			return true;
		}
		if (list.Exists((HolidayModel model) => model.date.Date == date.Date && model.type == 0))
		{
			return false;
		}
		if (date.DayOfWeek >= DayOfWeek.Monday)
		{
			return date.DayOfWeek <= DayOfWeek.Friday;
		}
		return false;
	}

	public static bool IsNonWorkWeekend(DateTime date)
	{
		if ((date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday) && !((LocalSettings.Settings.EnableHoliday && Utils.IsDida()) ? GetCacheHolidays() : new List<HolidayModel>()).Exists((HolidayModel model) => model.date.Date == date.Date && model.type == 1))
		{
			return true;
		}
		return false;
	}
}
