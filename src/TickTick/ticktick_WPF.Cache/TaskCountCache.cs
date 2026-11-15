using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TickTickUtils.Lang;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Provider;
using ticktick_WPF.Views.MainListView;

namespace ticktick_WPF.Cache;

public static class TaskCountCache
{
	public static readonly ConcurrentDictionary<string, int> CountData = new ConcurrentDictionary<string, int>();

	public static readonly ConcurrentDictionary<string, DateTime> CountCalculateTimeDict = new ConcurrentDictionary<string, DateTime>();

	public static DateTime LoadTime { get; set; }

	public static void Clear()
	{
		CountData.Clear();
		CountCalculateTimeDict.Clear();
		LoadTime = DateTime.Now.AddDays(-1.0);
	}

	public static int TryGetCount(string key)
	{
		if (string.IsNullOrEmpty(key))
		{
			return 0;
		}
		if (LocalSettings.Settings.ExtraSettings.NumDisplayType == 2)
		{
			return 0;
		}
		if (CountData != null && CountData.ContainsKey(key))
		{
			return CountData[key];
		}
		return -1;
	}

	public static async Task<int> TryGetCount(ProjectIdentity pId)
	{
		if (LocalSettings.Settings.ExtraSettings.NumDisplayType == 2)
		{
			return 0;
		}
		if (CountData != null && pId != null && !(pId is CompletedProjectIdentity) && !(pId is TrashProjectIdentity) && !(pId is AbandonedProjectIdentity))
		{
			string key = pId.QueryId;
			if (string.IsNullOrEmpty(key))
			{
				return 0;
			}
			if ((CountCalculateTimeDict.ContainsKey(key) && CountCalculateTimeDict[key] < LoadTime) || !CountData.ContainsKey(key))
			{
				int num = await ProjectAndTaskIdsCache.GetCountByIdentity(pId);
				TryAddValue(key, num);
				return num;
			}
			if (CountData.ContainsKey(key))
			{
				return CountData[key];
			}
		}
		return 0;
	}

	public static void SetNeedLoad()
	{
		LoadTime = DateTime.Now;
	}

	public static async Task LoadTodayOrWeekCount()
	{
		LocalSettings settings = LocalSettings.Settings;
		if (settings.SmartListToday != 1)
		{
			TryAddValue("_special_id_today", await ProjectAndTaskIdsCache.GetCountByIdentity(SmartProjectIdentity.BuildSmartProject("_special_id_today")));
		}
		if (settings.SmartList7Day != 1)
		{
			TryAddValue("_special_id_week", await ProjectAndTaskIdsCache.GetCountByIdentity(SmartProjectIdentity.BuildSmartProject("_special_id_week")));
		}
	}

	private static async Task<bool> ReloadSmartListCount()
	{
		LocalSettings settings = LocalSettings.Settings;
		bool changed = false;
		if (settings.SmartListAll != 1)
		{
			AllProjectIdentity allIdentity = new AllProjectIdentity();
			await ProjectAndTaskIdsCache.ResetProjectTaskIds(allIdentity);
			int num = await ProjectAndTaskIdsCache.GetCountByIdentity(allIdentity);
			if (!CountData.TryGetValue("_special_id_all", out var value) || value != num)
			{
				changed = true;
				TryAddValue("_special_id_all", num);
			}
		}
		if (settings.SmartListToday != 1)
		{
			TodayProjectIdentity todIdentity = new TodayProjectIdentity();
			await ProjectAndTaskIdsCache.ResetProjectTaskIds(todIdentity);
			int num2 = await ProjectAndTaskIdsCache.GetCountByIdentity(todIdentity);
			if (!CountData.TryGetValue("_special_id_today", out var value2) || value2 != num2)
			{
				changed = true;
				TryAddValue("_special_id_today", num2);
			}
		}
		if (settings.SmartListTomorrow != 1)
		{
			TomorrowProjectIdentity tomIdentity = new TomorrowProjectIdentity();
			await ProjectAndTaskIdsCache.ResetProjectTaskIds(tomIdentity);
			int num3 = await ProjectAndTaskIdsCache.GetCountByIdentity(tomIdentity);
			if (!CountData.TryGetValue("_special_id_tomorrow", out var value3) || value3 != num3)
			{
				changed = true;
				TryAddValue("_special_id_tomorrow", num3);
			}
		}
		if (settings.SmartList7Day != 1)
		{
			WeekProjectIdentity weekIdentity = new WeekProjectIdentity();
			await ProjectAndTaskIdsCache.ResetProjectTaskIds(weekIdentity);
			int num4 = await ProjectAndTaskIdsCache.GetCountByIdentity(weekIdentity);
			if (!CountData.TryGetValue("_special_id_week", out var value4) || value4 != num4)
			{
				changed = true;
				TryAddValue("_special_id_week", num4);
			}
		}
		return changed;
	}

	private static void TryAddValue(string key, int count)
	{
		if (!string.IsNullOrEmpty(key))
		{
			CountData[key] = count;
			CountCalculateTimeDict[key] = DateTime.Now;
		}
	}

	public static async Task ReloadProjectTaskCount(ProjectIdentity selectedProject)
	{
		if (LocalSettings.Settings.ExtraSettings.NumDisplayType != 2 && selectedProject != null)
		{
			Task.Run(async delegate
			{
				Thread.Sleep(200);
				ProjectAndTaskIdsCache.RemoveEventCache(selectedProject);
				await ProjectAndTaskIdsCache.ResetProjectTaskIds(selectedProject);
				int count = await ProjectAndTaskIdsCache.GetCountByIdentity(selectedProject);
				TryAddValue(selectedProject.QueryId, count);
				ListViewContainer.ResetIdentityCount(selectedProject, count);
			});
		}
	}

	public static void TryReloadGroupAndSmartCount()
	{
		DelayActionHandlerCenter.TryDoAction("TryReloadGroupAndSmartCount", ReloadGroupAndSmartCount);
	}

	public static void TryReloadSmartCount()
	{
		DelayActionHandlerCenter.TryDoAction("TryReloadSmartCount", ReloadSmartCount);
	}

	private static async void ReloadSmartCount(object sender, EventArgs eventArgs)
	{
		_ = 1;
		try
		{
			if (await ReloadSmartListCount())
			{
				await Task.Delay(100);
				ThreadUtil.DetachedRunOnUiBackThread(ListViewContainer.ReloadCount);
			}
		}
		catch (Exception ex)
		{
			UtilLog.Error(ex);
		}
	}

	private static async void ReloadGroupAndSmartCount(object sender, EventArgs eventArgs)
	{
		_ = 3;
		try
		{
			bool changed = false;
			List<ProjectGroupModel> projectGroups = CacheManager.GetProjectGroups();
			foreach (ProjectGroupModel item in projectGroups)
			{
				GroupProjectIdentity groupIdentity = new GroupProjectIdentity(item);
				await ProjectAndTaskIdsCache.ResetProjectTaskIds(groupIdentity);
				int num = await ProjectAndTaskIdsCache.GetCountByIdentity(groupIdentity);
				if (!CountData.ContainsKey(groupIdentity.QueryId) || CountData[groupIdentity.QueryId] != num)
				{
					changed = true;
					TryAddValue(groupIdentity.QueryId, num);
				}
			}
			if (changed | await ReloadSmartListCount())
			{
				await Task.Delay(100);
				ThreadUtil.DetachedRunOnUiBackThread(ListViewContainer.ReloadCount);
			}
		}
		catch (Exception ex)
		{
			UtilLog.Error(ex);
		}
	}
}
