using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TickTickDao;
using TickTickModels;
using TickTickUtils;
using TickTickUtils.Lang;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Service;

public static class TaskGoogleEventInfoService
{
	private const string CheckpointDataType = "TASK_GOOGLE_EVENT_INFO_CHECKPOINT";

	private const string ScopeMappingVersionDataType = "TASK_GOOGLE_EVENT_INFO_SCOPE_MAPPING_V2";

	private const int PageLimit = 200;

	private static readonly SemaphoreSlim SyncLocker = new SemaphoreSlim(1, 1);

	public static async Task SyncForCurrentConnectionsAsync()
	{
		_ = 1;
		try
		{
			await SyncForConnectionsAsync(await CalendarConnectInfoDao.GetAllConnectsAsync());
		}
		catch (Exception ex)
		{
			UtilLog.Warn("SyncTaskGoogleEventInfo for current connections failed: " + ExceptionUtils.BuildExceptionMessage(ex));
		}
	}

	public static async Task SyncForConnectionsAsync(IEnumerable<CalendarConnectInfo> connects)
	{
		await SyncLocker.WaitAsync();
		bool changed = false;
		try
		{
			List<CalendarConnectInfo> googleConnections = GetGoogleConnections(connects);
			changed = await ReconcileLocalScopesAsync(googleConnections);
			List<CalendarConnectInfo> scopedConnections = googleConnections.Where(HasSyncScope).ToList();
			if (scopedConnections.Count == 0)
			{
				await DeleteCheckpointAsync();
			}
			else if (Utils.IsNetworkAvailable())
			{
				await EnsureScopeMappingVersionAsync();
				bool flag = changed;
				changed = flag | await PullIncrementalAsync(scopedConnections);
				flag = changed;
				changed = flag | await ReconcileLocalScopesAsync(googleConnections);
			}
		}
		catch (Exception ex)
		{
			UtilLog.Warn("SyncTaskGoogleEventInfo failed: " + ExceptionUtils.BuildExceptionMessage(ex));
		}
		finally
		{
			SyncLocker.Release();
			if (changed)
			{
				DataChangedNotifier.NotifyTaskGoogleEventInfoChanged();
			}
		}
	}

	private static List<CalendarConnectInfo> GetGoogleConnections(IEnumerable<CalendarConnectInfo> connects)
	{
		List<CalendarConnectInfo> list = connects?.Where((CalendarConnectInfo connect) => connect != null && string.Equals(connect.site, "google", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(connect.id)).ToList() ?? new List<CalendarConnectInfo>();
		foreach (CalendarConnectInfo item in list.Where((CalendarConnectInfo connection) => connection.relProjects == null))
		{
			item.GetItems();
		}
		return list;
	}

	private static bool HasSyncScope(CalendarConnectInfo connection)
	{
		return connection.relProjects?.Any((CalendarContentProject project) => !string.IsNullOrWhiteSpace(project?.syncProjectScope)) ?? false;
	}

	private static async Task<bool> ReconcileLocalScopesAsync(IReadOnlyCollection<CalendarConnectInfo> googleConnections)
	{
		bool flag = await TaskGoogleEventInfoDao.DeleteConnectionsExceptAsync(googleConnections.Select((CalendarConnectInfo connection) => connection.id).ToList());
		foreach (CalendarConnectInfo googleConnection in googleConnections)
		{
			List<string> list = (from project in googleConnection.relProjects?.Where((CalendarContentProject project) => !string.IsNullOrWhiteSpace(project?.syncProjectScope))
				select project.syncProjectScope).Distinct().ToList() ?? new List<string>();
			if (list.Count == 0)
			{
				bool flag2 = flag;
				flag = flag2 | await TaskGoogleEventInfoDao.DeleteByConnectionIdAsync(googleConnection.id);
			}
			else if (!list.Any((string scope) => string.Equals(scope, "all", StringComparison.OrdinalIgnoreCase)))
			{
				bool flag2 = flag;
				flag = flag2 | await TaskGoogleEventInfoDao.DeleteProjectsExceptAsync(googleConnection.id, list);
			}
		}
		return flag;
	}

	private static async Task<bool> PullIncrementalAsync(IReadOnlyCollection<CalendarConnectInfo> scopedConnections)
	{
		long checkpoint = await GetCheckpointAsync();
		string cursor = null;
		bool changed = false;
		bool hasAmbiguousItems = false;
		while (true)
		{
			TaskGoogleEventInfoSyncResult response = await Communicator.GetTaskGoogleEventInfos(checkpoint, cursor, 200);
			if (response == null)
			{
				return changed;
			}
			bool hasAmbiguousItems2;
			List<TaskGoogleEventInfo> list = MapItemsToCurrentScopes(response.items, scopedConnections, out hasAmbiguousItems2);
			hasAmbiguousItems = hasAmbiguousItems || hasAmbiguousItems2;
			UtilLog.Info("SyncTaskGoogleEventInfo page remote={0}, accepted={1}, deleted={2}", response.items?.Count ?? 0, list.Count, response.deletedIds?.Count ?? 0);
			bool flag = changed;
			changed = flag | await TaskGoogleEventInfoDao.MergeSyncPageAsync(list, response.deletedIds);
			if (!response.hasMore)
			{
				if (hasAmbiguousItems)
				{
					UtilLog.Warn("SyncTaskGoogleEventInfo checkpoint was not saved because some items matched multiple Google connections.");
					return changed;
				}
				await SaveCheckpointAsync(response.checkpoint);
				return changed;
			}
			if (string.IsNullOrEmpty(response.nextCursor) || string.Equals(cursor, response.nextCursor, StringComparison.Ordinal))
			{
				break;
			}
			cursor = response.nextCursor;
		}
		UtilLog.Warn("SyncTaskGoogleEventInfo stopped because nextCursor is invalid.");
		return changed;
	}

	private static List<TaskGoogleEventInfo> MapItemsToCurrentScopes(IEnumerable<TaskGoogleEventInfo> items, IReadOnlyCollection<CalendarConnectInfo> scopedConnections, out bool hasAmbiguousItems)
	{
		hasAmbiguousItems = false;
		List<TaskGoogleEventInfo> list = new List<TaskGoogleEventInfo>();
		foreach (TaskGoogleEventInfo item in items?.Where((TaskGoogleEventInfo taskGoogleEventInfo) => taskGoogleEventInfo != null) ?? Enumerable.Empty<TaskGoogleEventInfo>())
		{
			List<CalendarConnectInfo> list2 = scopedConnections.Where((CalendarConnectInfo connection) => IsItemInScope(item, connection)).ToList();
			CalendarConnectInfo calendarConnectInfo;
			if (!string.IsNullOrEmpty(item.connectId))
			{
				calendarConnectInfo = list2.FirstOrDefault((CalendarConnectInfo connection) => string.Equals(connection.id, item.connectId, StringComparison.Ordinal));
			}
			else
			{
				List<CalendarConnectInfo> list3 = (string.IsNullOrEmpty(item.calendarId) ? new List<CalendarConnectInfo>() : list2.Where((CalendarConnectInfo connection) => ConnectionContainsCalendar(connection, item.calendarId)).ToList());
				List<CalendarConnectInfo> list4 = (string.IsNullOrEmpty(item.projectId) ? new List<CalendarConnectInfo>() : list2.Where((CalendarConnectInfo connection) => ConnectionContainsProject(connection, item.projectId)).ToList());
				calendarConnectInfo = ((list3.Count == 1) ? list3[0] : ((list4.Count == 1) ? list4[0] : ((list2.Count == 1) ? list2[0] : null)));
				if (calendarConnectInfo == null && list2.Count > 1)
				{
					hasAmbiguousItems = true;
				}
			}
			if (calendarConnectInfo != null)
			{
				item.connectId = calendarConnectInfo.id;
				list.Add(item);
			}
		}
		return list;
	}

	private static bool IsItemInScope(TaskGoogleEventInfo item, CalendarConnectInfo connection)
	{
		return connection.relProjects?.Any((CalendarContentProject project) => !string.IsNullOrWhiteSpace(project?.syncProjectScope) && (string.Equals(project.syncProjectScope, "all", StringComparison.OrdinalIgnoreCase) || (!string.IsNullOrEmpty(item.projectId) && string.Equals(project.syncProjectScope, item.projectId, StringComparison.Ordinal)))) ?? false;
	}

	private static bool ConnectionContainsCalendar(CalendarConnectInfo connection, string calendarId)
	{
		return connection.relProjects?.Any((CalendarContentProject project) => !string.IsNullOrEmpty(project?.calendarId) && string.Equals(project.calendarId, calendarId, StringComparison.Ordinal)) ?? false;
	}

	private static bool ConnectionContainsProject(CalendarConnectInfo connection, string projectId)
	{
		return connection.relProjects?.Any((CalendarContentProject project) => !string.IsNullOrEmpty(project?.syncProjectScope) && !string.Equals(project.syncProjectScope, "all", StringComparison.OrdinalIgnoreCase) && string.Equals(project.syncProjectScope, projectId, StringComparison.Ordinal)) ?? false;
	}

	private static async Task EnsureScopeMappingVersionAsync()
	{
		string userId = LocalSettings.Settings.LoginUserId;
		if (!string.IsNullOrEmpty(userId) && await TempDataDao.QueryTempDataModelListDbByTypeAndUserIdAsync("TASK_GOOGLE_EVENT_INFO_SCOPE_MAPPING_V2", userId) == null)
		{
			await DeleteCheckpointAsync();
			await TempDataDao.UpdateOrInsertTempDataModelListDbByTypeAndUserIdAsync(new TempDataModel
			{
				User_Id = userId,
				DataType = "TASK_GOOGLE_EVENT_INFO_SCOPE_MAPPING_V2",
				Data = "1",
				ModifyTime = Utils.GetNowTimeStamp()
			});
		}
	}

	private static async Task<long> GetCheckpointAsync()
	{
		string loginUserId = LocalSettings.Settings.LoginUserId;
		if (string.IsNullOrEmpty(loginUserId))
		{
			return 0L;
		}
		long result;
		return long.TryParse((await TempDataDao.QueryTempDataModelListDbByTypeAndUserIdAsync("TASK_GOOGLE_EVENT_INFO_CHECKPOINT", loginUserId))?.Data, NumberStyles.Integer, CultureInfo.InvariantCulture, out result) ? result : 0;
	}

	private static async Task SaveCheckpointAsync(long checkpoint)
	{
		string loginUserId = LocalSettings.Settings.LoginUserId;
		if (!string.IsNullOrEmpty(loginUserId))
		{
			await TempDataDao.UpdateOrInsertTempDataModelListDbByTypeAndUserIdAsync(new TempDataModel
			{
				User_Id = loginUserId,
				DataType = "TASK_GOOGLE_EVENT_INFO_CHECKPOINT",
				Data = checkpoint.ToString(CultureInfo.InvariantCulture),
				ModifyTime = Utils.GetNowTimeStamp()
			});
		}
	}

	private static async Task DeleteCheckpointAsync()
	{
		string loginUserId = LocalSettings.Settings.LoginUserId;
		if (!string.IsNullOrEmpty(loginUserId))
		{
			TempDataModel tempDataModel = await TempDataDao.QueryTempDataModelListDbByTypeAndUserIdAsync("TASK_GOOGLE_EVENT_INFO_CHECKPOINT", loginUserId);
			if (tempDataModel != null)
			{
				await App.Connection.DeleteAsync(tempDataModel);
			}
		}
	}
}
