using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;
using TickTickDao;
using TickTickModels;
using TickTickUtils.Lang;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views;
using ticktick_WPF.Views.Config;
using ticktick_WPF.Views.Tag;

namespace ticktick_WPF.Service;

public class CalendarConnectService
{
	private static string _notionCleanupCheckedState;

	internal static List<string> NormalizeDefaultTags(CalendarContentProject project)
	{
		return project.defaultTags = TagSerializer.NormalizeLabels(project.defaultTags) ?? new List<string>();
	}

	public static async Task SaveConnects(List<CalendarConnectInfo> connects)
	{
		List<CalendarConnectInfo> locals = await CalendarConnectInfoDao.GetAllConnectsAsync();
		await CalendarConnectInfoDao.DeleteConnectsAsync(locals);
		List<CalendarConnectInfo> expiredConnects = new List<CalendarConnectInfo>();
		if (connects != null)
		{
			foreach (CalendarConnectInfo connect in connects)
			{
				if (string.IsNullOrEmpty(connect.userId))
				{
					connect.userId = LocalSettings.Settings.LoginUserId;
				}
				if (connect.status == 1)
				{
					CalendarConnectInfo calendarConnectInfo = locals.FirstOrDefault((CalendarConnectInfo l) => l.account == connect.account);
					if (calendarConnectInfo == null || calendarConnectInfo.status != 1)
					{
						expiredConnects.Add(connect);
					}
				}
				if (connect.relProjects != null)
				{
					foreach (CalendarContentProject relProject in connect.relProjects)
					{
						NormalizeDefaultTags(relProject);
					}
					connect.relProjectsString = JsonConvert.SerializeObject(connect.relProjects);
				}
				if (connect.calendars != null)
				{
					connect.calendarString = JsonConvert.SerializeObject(connect.calendars);
				}
			}
			await BaseDao<CalendarConnectInfo>.InsertAllAsync(connects);
		}
		foreach (CalendarConnectInfo item in expiredConnects)
		{
			SubscribeExpiredWindow.TryShowWindow(null, null, new List<CalendarConnectInfo> { item });
		}
		await TryClearDisconnectedNotionBlocks(connects);
		await TaskGoogleEventInfoService.SyncForConnectionsAsync(connects);
		if (locals == null || locals.Count <= 0)
		{
			if (connects == null || connects.Count <= 0)
			{
				return;
			}
		}
		DataChangedNotifier.NotifyCalendarConnectChanged();
	}

	private static async Task TryClearDisconnectedNotionBlocks(IEnumerable<CalendarConnectInfo> remoteConnects)
	{
		try
		{
			await ClearDisconnectedNotionBlocks(GetNotionProjectIds(remoteConnects));
		}
		catch (Exception arg)
		{
			UtilLog.Error($"ClearDisconnectedNotionBlocks error: {arg}");
		}
	}

	private static HashSet<string> GetNotionProjectIds(IEnumerable<CalendarConnectInfo> connects)
	{
		HashSet<string> hashSet = new HashSet<string>();
		if (connects == null)
		{
			return hashSet;
		}
		foreach (CalendarConnectInfo item in connects.Where((CalendarConnectInfo connect) => string.Equals(connect?.site, "notion", StringComparison.OrdinalIgnoreCase)))
		{
			if (item.relProjects == null)
			{
				item.GetItems();
			}
			if (item.relProjects == null)
			{
				continue;
			}
			foreach (CalendarContentProject relProject in item.relProjects)
			{
				if (!string.IsNullOrEmpty(relProject?.syncProjectScope))
				{
					hashSet.Add(relProject.syncProjectScope);
				}
			}
		}
		return hashSet;
	}

	private static bool ShouldClearNotionBlock(string projectId, ISet<string> remoteProjectIds)
	{
		if (remoteProjectIds != null && remoteProjectIds.Contains("all"))
		{
			return false;
		}
		if (remoteProjectIds == null || remoteProjectIds.Count == 0)
		{
			return true;
		}
		return !remoteProjectIds.Contains(projectId);
	}

	private static async Task ClearDisconnectedNotionBlocks(ISet<string> remoteProjectIds)
	{
		string loginUserId = LocalSettings.Settings.LoginUserId;
		string cleanupState = loginUserId + ":" + string.Join(",", remoteProjectIds.OrderBy((string projectId) => projectId));
		if (_notionCleanupCheckedState == cleanupState)
		{
			return;
		}
		if (remoteProjectIds.Contains("all"))
		{
			_notionCleanupCheckedState = cleanupState;
			return;
		}
		List<string> staleTaskIds = (from task in await TaskDao.GetTasksWithNotionBlock()
			where ShouldClearNotionBlock(task.projectId, remoteProjectIds)
			select task.id into taskId
			where !string.IsNullOrEmpty(taskId)
			select taskId).Distinct().ToList();
		if (!staleTaskIds.Any())
		{
			_notionCleanupCheckedState = cleanupState;
			return;
		}
		foreach (string item in staleTaskIds)
		{
			TaskBaseViewModel taskById = TaskCache.GetTaskById(item);
			if (taskById != null)
			{
				taskById.NotionBlock = string.Empty;
			}
		}
		await TaskDao.ClearNotionBlocks(staleTaskIds);
		TaskChangeNotifier.NotifyTaskBatchChanged(staleTaskIds);
		_notionCleanupCheckedState = cleanupState;
		UtilLog.Info($"ClearDisconnectedNotionBlocks count:{staleTaskIds.Count}");
	}

	public static async Task<string> SaveConnect(CalendarConnectInfo calendar)
	{
		if (calendar != null)
		{
			CalendarConnectInfo local = await CalendarConnectInfoDao.GetInfoByIdAsync(calendar.id);
			if (local != null)
			{
				await BaseDao<CalendarConnectInfo>.DeleteAsync(local);
			}
			if (string.IsNullOrEmpty(calendar.userId))
			{
				calendar.userId = LocalSettings.Settings.LoginUserId;
			}
			if (calendar.relProjects != null)
			{
				calendar.relProjectsString = JsonConvert.SerializeObject(calendar.relProjects);
			}
			if (calendar.calendars != null)
			{
				calendar.calendarString = JsonConvert.SerializeObject(calendar.calendars);
			}
			await BaseDao<CalendarConnectInfo>.InsertAsync(calendar);
			await TaskGoogleEventInfoService.SyncForConnectionsAsync(await CalendarConnectInfoDao.GetAllConnectsAsync());
			DataChangedNotifier.NotifyCalendarConnectChanged();
			return Utils.GetString((local == null || local.status == 1) ? "Integrated" : "AccountHasIntegrated");
		}
		return null;
	}

	public static async Task<List<string>> GetConnectedCalendarIds()
	{
		List<string> result = new List<string>();
		foreach (CalendarConnectInfo item in await CalendarConnectInfoDao.GetAllConnectsAsync())
		{
			item.GetItems();
			if (item.relProjects == null)
			{
				continue;
			}
			foreach (CalendarContentProject relProject in item.relProjects)
			{
				if (!string.IsNullOrEmpty(relProject.calendarId))
				{
					result.Add(relProject.calendarId);
				}
			}
		}
		return result;
	}

	public static async Task<List<string>> GetNotionConnectedProjectIds()
	{
		List<string> projectIds = new List<string>();
		foreach (CalendarConnectInfo item in await CalendarConnectInfoDao.GetAllConnectsAsync())
		{
			if (item.site != "notion")
			{
				continue;
			}
			item.GetItems();
			if (item.relProjects != null)
			{
				projectIds.AddRange(item.relProjects.Select((CalendarContentProject proj) => proj.syncProjectScope));
			}
		}
		return projectIds;
	}

	public static async Task<(List<string>, List<string>)> GetConnectedProjectIds()
	{
		List<string> projectIds = new List<string>();
		List<string> defaultProjectIds = new List<string>();
		foreach (CalendarConnectInfo item in await CalendarConnectInfoDao.GetAllConnectsAsync())
		{
			item.GetItems();
			if (item.relProjects == null)
			{
				continue;
			}
			foreach (CalendarContentProject relProject in item.relProjects)
			{
				if (relProject.syncProjectScope == "all" && !string.IsNullOrEmpty(relProject.syncProjectId))
				{
					defaultProjectIds.Add(relProject.syncProjectId);
				}
				projectIds.Add(relProject.syncProjectScope);
			}
		}
		return (projectIds, defaultProjectIds);
	}

	public static async Task<CalendarConnectInfo> GetConnectInfoByDefaultId(string projectId)
	{
		foreach (CalendarConnectInfo item in await CalendarConnectInfoDao.GetAllConnectsAsync())
		{
			item.GetItems();
			if (item.relProjects == null)
			{
				continue;
			}
			foreach (CalendarContentProject relProject in item.relProjects)
			{
				if (relProject.syncProjectScope == "all" && relProject.syncProjectId == projectId)
				{
					return item;
				}
			}
		}
		return null;
	}

	public static async Task<List<CalendarConnectInfo>> GetConnectInfosByProjectId(string projectId)
	{
		List<CalendarConnectInfo> obj = await CalendarConnectInfoDao.GetAllConnectsAsync();
		List<CalendarConnectInfo> list = new List<CalendarConnectInfo>();
		foreach (CalendarConnectInfo item in obj)
		{
			item.GetItems();
			if (item.relProjects == null)
			{
				continue;
			}
			foreach (CalendarContentProject relProject in item.relProjects)
			{
				if (relProject.syncProjectScope == projectId)
				{
					list.Add(item);
				}
			}
		}
		return list;
	}

	public static async Task<bool> CheckConnectCount(Window owner, string site)
	{
		if ((await CalendarConnectInfoDao.GetAllConnectsAsync()).Count((CalendarConnectInfo c) => c.site == site) >= 3)
		{
			new CustomerDialog(Utils.GetString("LimitTips"), string.Format(Utils.GetString("CalendarConnectExceed"), 3), MessageBoxButton.OK, owner).ShowDialog();
			return false;
		}
		return true;
	}

	public static async Task<bool> CheckConnectProjectCount(string connectId, Window owner)
	{
		CalendarConnectInfo calendarConnectInfo = await CalendarConnectInfoDao.GetInfoByIdAsync(connectId);
		if (calendarConnectInfo != null)
		{
			calendarConnectInfo.GetItems();
			long limitByKey = LimitCache.GetLimitByKey(Constants.LimitKind.GoogleConnectNumber);
			if (calendarConnectInfo.relProjects?.Count >= limitByKey)
			{
				new CustomerDialog(Utils.GetString("LimitTips"), string.Format(Utils.GetString("CalendarConnectProjectExceed"), limitByKey), MessageBoxButton.OK, owner).ShowDialog();
				return false;
			}
		}
		return true;
	}

	public static async Task<bool> ExistConnects()
	{
		return (await CalendarConnectInfoDao.GetAllConnectsAsync()).Count > 0;
	}

	public static async Task<bool> Disintegration(string id)
	{
		List<CalendarConnectInfo> connects = await CalendarConnectInfoDao.GetAllConnectsAsync();
		CalendarConnectInfo connectInfo = connects.FirstOrDefault((CalendarConnectInfo connect) => connect.id == id);
		if (connectInfo != null && await Communicator.UnConnectCalendar(id))
		{
			await BaseDao<CalendarConnectInfo>.DeleteAsync(connectInfo);
			if (string.Equals(connectInfo.site, "notion", StringComparison.OrdinalIgnoreCase))
			{
				await TryClearDisconnectedNotionBlocks(connects.Where((CalendarConnectInfo connect) => connect.id != id));
			}
			await TaskGoogleEventInfoService.SyncForConnectionsAsync(await CalendarConnectInfoDao.GetAllConnectsAsync());
			DataChangedNotifier.NotifyCalendarConnectChanged();
			return true;
		}
		return false;
	}

	public static async Task<List<string>> GetAllConnectProjectIds()
	{
		HashSet<string> ids = new HashSet<string>();
		foreach (CalendarConnectInfo item in await CalendarConnectInfoDao.GetAllConnectsAsync())
		{
			item.GetItems();
			if (item.relProjects == null)
			{
				continue;
			}
			foreach (CalendarContentProject relProject in item.relProjects)
			{
				ids.Add(relProject.syncProjectScope);
				if (relProject.syncProjectScope == "all" && !string.IsNullOrEmpty(relProject.syncProjectId))
				{
					ids.Add(relProject.syncProjectId);
				}
			}
		}
		return ids.ToList();
	}
}
