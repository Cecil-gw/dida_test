using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TickTickDao;
using TickTickModels;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Pomo;

namespace ticktick_WPF.Service;

public class TimerService
{
	public static async Task UpdateTimer(TimerModel timer)
	{
		if (await TimerDao.GetTimerById(timer.Id) == null)
		{
			await BaseDao<TimerModel>.InsertAsync(timer);
			return;
		}
		if (timer.SyncStatus != 0)
		{
			timer.SyncStatus = 1;
		}
		await BaseDao<TimerModel>.UpdateAsync(timer);
	}

	public static async Task<long> GetNewTimerSortOrder()
	{
		return await TimerDao.GetMinSortOrder() - 134217728;
	}

	public static async Task SaveSortOrder(string id, long sortOrder)
	{
		await TimerDao.SaveSortOrder(id, sortOrder);
		SyncManager.TryDelaySync(2000);
	}

	public static async Task SetTimerTodayDuration(List<FocusTimerViewModel> models)
	{
		if (models == null || models.Count == 0)
		{
			return;
		}
		List<PomoTask> list = await PomoTaskDao.GetTodayPomoTasks();
		Dictionary<FocusTimerViewModel, long> durations = models.ToDictionary((FocusTimerViewModel model) => model, (FocusTimerViewModel _) => 0L);
		if (list != null && list.Count > 0)
		{
			foreach (PomoTask pomoTask in list)
			{
				PomodoroModel pomodoroModel = await PomoService.GetAdjustPomo(pomoTask.PomoId);
				if (pomodoroModel == null || pomodoroModel.SyncStatus == -1)
				{
					continue;
				}
				PomoTask adjustTask = pomodoroModel.Tasks.FirstOrDefault((PomoTask x) => x._Id == pomoTask._Id);
				if (adjustTask == null)
				{
					continue;
				}
				long todayDuration = adjustTask.GetTodayDuration();
				if (todayDuration <= 0)
				{
					continue;
				}
				foreach (FocusTimerViewModel item in models.Where((FocusTimerViewModel model) => IsPomoTaskMatchedToTimer(adjustTask, model)))
				{
					durations[item] += todayDuration;
				}
			}
		}
		foreach (KeyValuePair<FocusTimerViewModel, long> item2 in durations)
		{
			item2.Key.DurationText = Utils.GetShortDurationString(item2.Value, showZeroMinute: false);
		}
	}

	private static bool IsPomoTaskMatchedToTimer(PomoTask pomoTask, FocusTimerViewModel model)
	{
		if (pomoTask == null || model == null)
		{
			return false;
		}
		if (!string.IsNullOrEmpty(model.Id) && model.Id == pomoTask.TimerSid)
		{
			return true;
		}
		if (string.IsNullOrEmpty(model.ObjId))
		{
			return false;
		}
		if (!(model.ObjId == pomoTask.TaskId))
		{
			return model.ObjId == pomoTask.HabitId;
		}
		return true;
	}

	public static async Task<string> GetObjTitle(string objId, string objType)
	{
		if (objType == "task")
		{
			TaskBaseViewModel taskById = TaskCache.GetTaskById(objId);
			if (taskById != null)
			{
				return taskById.Title ?? string.Empty;
			}
		}
		else if (objType == "habit")
		{
			string text = await HabitDao.GetHabitNameById(objId);
			if (text != null)
			{
				return text;
			}
		}
		return null;
	}

	public static async Task UpdateName(string id, string title)
	{
		await TimerDao.SaveName(id, title);
	}

	public static async Task ChangeArchiveStatus(string id)
	{
		await TimerDao.ChangeArchiveStatus(id, await GetNewTimerSortOrder());
		PomoNotifier.NotifyTimerChanged();
		SyncManager.TryDelaySync(2000);
	}

	public static async Task DeleteTimer(string id)
	{
		await TimerDao.SetDeleteStatus(id);
		PomoNotifier.NotifyTimerChanged();
		SyncManager.TryDelaySync(2000);
	}

	public static async Task SaveEtag(string timerId, string etag)
	{
		await TimerDao.SaveEtag(timerId, etag);
	}

	public static async Task<Dictionary<DateTime, double>> GetTimerRecord(string timerId, string objId, string objType, bool isNewAdd = false, DateTime? start = null, DateTime? end = null, bool delete = false)
	{
		List<PomoTask> list = await PomoTaskDao.GetPomoTaskOfTimer(timerId, objId, objType == "habit", start, end, isNewAdd, delete);
		Dictionary<DateTime, double> dict = new Dictionary<DateTime, double>();
		if (list == null || list.Count == 0)
		{
			return dict;
		}
		foreach (PomoTask item in list)
		{
			DateTime date = item.StartTime.Date;
			double totalMinutes = (((item.EndTime > item.StartTime.Date.AddDays(1.0)) ? item.StartTime.Date.AddDays(1.0) : item.EndTime) - item.StartTime).TotalMinutes;
			AddToDict(date, totalMinutes);
			if (item.EndTime > item.StartTime.Date.AddDays(1.0))
			{
				DateTime date2 = item.EndTime.Date;
				double totalMinutes2 = (item.EndTime - item.StartTime.Date.AddDays(1.0)).TotalMinutes;
				AddToDict(date2, totalMinutes2);
			}
		}
		return dict;
		void AddToDict(DateTime key, double duration)
		{
			if (duration > 0.0)
			{
				if (dict.ContainsKey(key))
				{
					dict[key] += duration;
				}
				else
				{
					dict[key] = duration;
				}
			}
		}
	}

	public static string GetTimerStatisticsKey(string timerId, DateTime start, DateTime end, string interval)
	{
		return timerId + DateUtils.GetDateNum(start) + DateUtils.GetDateNum(end) + interval;
	}

	public static async Task<Dictionary<string, long>> CalTimerStatistics(TimerModel timer, DateTime start, DateTime end, string interval, bool isNewAdd = false)
	{
		Dictionary<DateTime, double> dictionary = await GetTimerRecord(timer.Id, timer.ObjId, timer.ObjType, isNewAdd, start, end);
		Dictionary<string, long> dictionary2 = new Dictionary<string, long>();
		for (int i = 0; i < (end - start).Days; i++)
		{
			DateTime dateTime = start.AddDays(i);
			string text = DateUtils.GetDateNum(dateTime).ToString();
			string key = ((interval == "year") ? DateUtils.GetDateNum(dateTime.AddDays(1 - dateTime.Day)).ToString() : text);
			if (dictionary.ContainsKey(dateTime))
			{
				if (dictionary2.ContainsKey(key))
				{
					dictionary2[key] += (long)dictionary[dateTime];
				}
				else
				{
					dictionary2[key] = (long)dictionary[dateTime];
				}
			}
		}
		return dictionary2;
	}

	public static async Task<Dictionary<string, long>> GetTimerStatistics(TimerModel timer, DateTime start, DateTime end, string interval)
	{
		string timerStatisticsKey = GetTimerStatisticsKey(timer.Id, start, end, interval);
		Dictionary<string, long> statistics;
		try
		{
			statistics = JsonConvert.DeserializeObject<Dictionary<string, long>>((await TimerDao.GetStatistics(timerStatisticsKey))?.Value ?? "") ?? new Dictionary<string, long>();
		}
		catch (Exception)
		{
			statistics = new Dictionary<string, long>();
		}
		await HandleDeletedOrAdded(delete: false);
		await HandleDeletedOrAdded(delete: true);
		return statistics;
		async Task HandleDeletedOrAdded(bool delete)
		{
			Dictionary<DateTime, double> dictionary = await GetTimerRecord(timer.Id, timer.ObjId, timer.ObjType, !delete, start, end, delete);
			Dictionary<string, long> dictionary2 = new Dictionary<string, long>();
			foreach (DateTime key2 in dictionary.Keys)
			{
				string key = ((interval == "year") ? DateUtils.GetDateNum(key2.AddDays(1 - key2.Day)).ToString() : DateUtils.GetDateNum(key2).ToString());
				if (dictionary2.ContainsKey(key))
				{
					dictionary2[key] += (long)dictionary[key2];
				}
				else
				{
					dictionary2[key] = (long)dictionary[key2];
				}
			}
			foreach (string key3 in dictionary2.Keys)
			{
				if (statistics.ContainsKey(key3))
				{
					long val = statistics[key3] + ((!delete) ? 1 : (-1)) * dictionary2[key3];
					statistics[key3] = Math.Max(0L, val);
				}
				else if (!delete)
				{
					statistics[key3] = dictionary2[key3];
				}
			}
		}
	}

	public static async Task<TimerOverviewModel> GetTimerOverView(TimerModel timer)
	{
		TimerOverviewModel overview = (string.IsNullOrEmpty(timer.Overview) ? null : JsonConvert.DeserializeObject<TimerOverviewModel>(timer.Overview)) ?? new TimerOverviewModel();
		if (overview.Date < DateUtils.GetDateNum(DateTime.Today))
		{
			overview.Today = 0L;
		}
		Dictionary<DateTime, double> dictionary = await GetTimerRecord(timer.Id, timer.ObjId, timer.ObjType, isNewAdd: true);
		TimerOverviewModel extra = new TimerOverviewModel
		{
			Days = dictionary.Keys.Count,
			Today = (dictionary.ContainsKey(DateTime.Today) ? ((long)dictionary[DateTime.Today]) : 0),
			Total = (long)dictionary.Values.ToList().Sum()
		};
		overview.Add(extra);
		Dictionary<DateTime, double> dictionary2 = await GetTimerRecord(timer.Id, timer.ObjId, timer.ObjType, isNewAdd: false, null, null, delete: true);
		TimerOverviewModel cut = new TimerOverviewModel
		{
			Days = dictionary2.Keys.Count,
			Today = (dictionary2.ContainsKey(DateTime.Today) ? ((long)dictionary2[DateTime.Today]) : 0),
			Total = (long)dictionary2.Values.ToList().Sum()
		};
		overview.Cut(cut);
		return overview;
	}

	public static async Task TryUpdateTimerIcon(string habitId, string icon, string color)
	{
		TimerModel timerModel = await TimerDao.GetTimerByObjId(habitId);
		if (timerModel != null && (timerModel.Icon != icon || timerModel.Color != color))
		{
			timerModel.Icon = icon;
			timerModel.Color = color;
			timerModel.ModifiedTime = DateTime.Now;
			timerModel.SyncStatus = ((timerModel.SyncStatus != 0) ? 1 : 0);
			await BaseDao<TimerModel>.UpdateAsync(timerModel);
			SyncManager.TryDelaySync();
		}
	}
}
