using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ticktick_WPF.Models;

namespace ticktick_WPF.Dal;

public class HabitCheckinCache
{
	private static ConcurrentDictionary<string, List<HabitCheckInModel>> _habitCheckIns = new ConcurrentDictionary<string, List<HabitCheckInModel>>();

	public static ConcurrentDictionary<string, Dictionary<DateTime, HabitCheckInStatus>> CheckInStatusCache = new ConcurrentDictionary<string, Dictionary<DateTime, HabitCheckInStatus>>();

	public static event EventHandler<string> HabitCheckInChanged;

	public static event EventHandler HabitsCheckInChanged;

	public static async Task InitCache()
	{
		List<HabitCheckInModel> checkIns = await HabitCheckInDao.GetHabitCheckIns();
		_habitCheckIns = await Task.Run(delegate
		{
			ConcurrentDictionary<string, List<HabitCheckInModel>> concurrentDictionary = new ConcurrentDictionary<string, List<HabitCheckInModel>>();
			foreach (HabitCheckInModel item in checkIns)
			{
				if (!string.IsNullOrEmpty(item.HabitId))
				{
					if (concurrentDictionary.ContainsKey(item.HabitId))
					{
						concurrentDictionary[item.HabitId].Add(item);
					}
					else
					{
						concurrentDictionary[item.HabitId] = new List<HabitCheckInModel> { item };
					}
				}
			}
			return concurrentDictionary;
		});
	}

	public static async Task OnHabitCheckInChanged(object sender, string habitId)
	{
		List<HabitCheckInModel> value = await HabitCheckInDao.GetHabitCheckInsByHabitId(habitId);
		_habitCheckIns[habitId] = value;
		HabitCheckinCache.HabitCheckInChanged?.Invoke(sender, habitId);
	}

	public static async Task<List<HabitCheckInModel>> GetHabitCheckInsById(string habitId)
	{
		if (_habitCheckIns.TryGetValue(habitId, out var value))
		{
			return value;
		}
		List<HabitCheckInModel> list = await HabitCheckInDao.GetHabitCheckInsByHabitId(habitId);
		_habitCheckIns[habitId] = list;
		return list;
	}

	public static async Task Init()
	{
		if (!_habitCheckIns.Any())
		{
			await InitCache();
		}
	}

	public static void Clear()
	{
		_habitCheckIns.Clear();
		CheckInStatusCache.Clear();
	}

	public static async void Reset()
	{
		Clear();
		await Init();
		HabitCheckinCache.HabitsCheckInChanged?.Invoke(null, null);
	}
}
