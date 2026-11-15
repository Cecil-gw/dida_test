using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TickTickDao;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Sync;

namespace ticktick_WPF.Dal;

public class SyncStatusDao : BaseDao<SyncStatusModel>
{
	private static readonly ConcurrentDictionary<string, byte> SetParentSyncTaskIds = new ConcurrentDictionary<string, byte>();

	private static async Task<int> Insert(SyncStatusModel syncStatus)
	{
		if (string.IsNullOrEmpty(syncStatus.UserId))
		{
			syncStatus.UserId = Utils.GetCurrentUserIdInt().ToString();
		}
		int result = await App.Connection.InsertAsync(syncStatus);
		CacheSetParentSyncStatus(syncStatus);
		return result;
	}

	private static async Task<int> Update(SyncStatusModel syncStatus)
	{
		if (string.IsNullOrEmpty(syncStatus.UserId))
		{
			syncStatus.UserId = Utils.GetCurrentUserIdInt().ToString();
		}
		int result = await App.Connection.UpdateAsync(syncStatus);
		CacheSetParentSyncStatus(syncStatus);
		return result;
	}

	public static bool HasSetParentSyncStatus(string taskId)
	{
		if (!string.IsNullOrEmpty(taskId))
		{
			return SetParentSyncTaskIds.ContainsKey(taskId);
		}
		return false;
	}

	public static async Task RefreshSetParentSyncStatusCache()
	{
		object obj = await GetSyncStatusByType(16);
		SetParentSyncTaskIds.Clear();
		if (obj == null)
		{
			obj = new List<SyncStatusModel>();
		}
		foreach (SyncStatusModel item in (List<SyncStatusModel>)obj)
		{
			CacheSetParentSyncStatus(item);
		}
	}

	private static void CacheSetParentSyncStatus(SyncStatusModel syncStatus)
	{
		if (syncStatus != null && syncStatus.Type == 16)
		{
			CacheSetParentSyncStatus(syncStatus.EntityId);
		}
	}

	private static void CacheSetParentSyncStatus(string taskId)
	{
		if (!string.IsNullOrEmpty(taskId))
		{
			SetParentSyncTaskIds[taskId] = 1;
		}
	}

	private static void RemoveSetParentSyncStatus(string taskId)
	{
		if (!string.IsNullOrEmpty(taskId))
		{
			SetParentSyncTaskIds.TryRemove(taskId, out var _);
		}
	}

	private static void RemoveSetParentSyncStatus(SyncStatusModel syncStatus)
	{
		if (syncStatus != null && syncStatus.Type == 16)
		{
			RemoveSetParentSyncStatus(syncStatus.EntityId);
		}
	}

	private static async Task RefreshSetParentSyncStatusCache(string taskId)
	{
		if (!string.IsNullOrEmpty(taskId))
		{
			string userId = Utils.GetCurrentUserIdInt().ToString();
			if (await (from v in App.Connection.Table<SyncStatusModel>()
				where v.EntityId == taskId && v.Type == 16 && v.UserId == userId
				select v).CountAsync() > 0)
			{
				CacheSetParentSyncStatus(taskId);
			}
			else
			{
				RemoveSetParentSyncStatus(taskId);
			}
		}
	}

	public static async Task<List<SyncStatusModel>> GetSyncStatusByType(int type)
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		return await (from v in App.Connection.Table<SyncStatusModel>()
			where v.Type == type && v.UserId == userId
			select v).ToListAsync();
	}

	public static async Task<List<SyncStatusModel>> GetSyncStatusById(string id)
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		return await (from v in App.Connection.Table<SyncStatusModel>()
			where v.EntityId == id && v.UserId == userId
			select v).ToListAsync();
	}

	public static async Task AddCreateSyncStatus(string taskId)
	{
		await AddSyncStatus(taskId, 4);
	}

	public static async Task BatchAddModifySyncStatus(IEnumerable<string> taskIds)
	{
		foreach (string taskId in taskIds)
		{
			await AddModifySyncStatus(taskId);
		}
	}

	public static async Task BatchAddCreateTaskStatus(IEnumerable<string> taskIds)
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		List<SyncStatusModel> list = taskIds.Select((string id) => new SyncStatusModel
		{
			Type = 4,
			EntityId = id,
			UserId = userId,
			ModifyPoint = DateTime.Now.Ticks
		}).ToList();
		if (list.Any())
		{
			await App.Connection.InsertAllAsync(list);
		}
	}

	public static async Task BatchRemoveDeleteSyncStatus(List<string> taskIds)
	{
		if (taskIds == null || !taskIds.Any())
		{
			return;
		}
		foreach (string taskId in taskIds)
		{
			await DeleteSyncStatus(taskId, 5);
		}
	}

	public static async Task BatchRemoveAddSyncStatus(List<string> taskIds)
	{
		if (taskIds == null || !taskIds.Any())
		{
			return;
		}
		foreach (string taskId in taskIds)
		{
			await DeleteSyncStatus(taskId, 4);
		}
	}

	public static async Task BatchAddDeleteSyncStatus(IEnumerable<string> taskIds)
	{
		if (taskIds == null)
		{
			return;
		}
		foreach (string taskId in taskIds)
		{
			await AddDeleteSyncStatus(taskId);
		}
	}

	public static async Task BatchAddDeleteForeverSyncStatus(IEnumerable<string> taskIds)
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		List<SyncStatusModel> list = await (from v in App.Connection.Table<SyncStatusModel>()
			where taskIds.Contains(v.EntityId) && v.UserId == userId
			select v).ToListAsync();
		if (list != null && list.Count > 0)
		{
			foreach (SyncStatusModel status in list)
			{
				await App.Connection.DeleteAsync(status);
				RemoveSetParentSyncStatus(status);
			}
		}
		List<SyncStatusModel> list2 = taskIds.Select((string taskId) => new SyncStatusModel
		{
			Type = 6,
			EntityId = taskId,
			UserId = userId,
			ModifyPoint = DateTime.Now.Ticks
		}).ToList();
		if (list2.Count > 0)
		{
			await App.Connection.InsertAllAsync(list2);
		}
	}

	public static async Task AddClearTrashSyncStatus()
	{
		await AddSyncStatus("d4ae7f9fedd48aab729c2f9c1bccf46", 8);
	}

	public static async Task AddEventDeletedSyncStatus(string eventId)
	{
		await AddSyncStatus(eventId, 11);
	}

	public static async Task AddEventCreateSyncStatus(string eventId)
	{
		await AddEventSyncStatus(eventId, 10);
	}

	public static async Task AddEventModifySyncStatus(string eventId)
	{
		await AddEventSyncStatus(eventId, 9);
	}

	public static async Task AddModifySyncStatus(string taskId)
	{
		await AddSyncStatus(taskId, 0);
	}

	public static async Task AddDeleteSyncStatus(string taskId)
	{
		TaskModel taskModel = await TaskDao.GetTaskById(taskId);
		if (taskModel != null && taskModel.deleted != 0)
		{
			await AddSyncStatus(taskId, 5);
		}
	}

	public static async Task<bool> RemoveDeleteSyncStatus(string taskId)
	{
		return await DeleteSyncStatus(taskId, 5);
	}

	public static async Task RemoveDeleteForeverSyncStatus(string taskId)
	{
		await DeleteSyncStatus(taskId, 6);
	}

	private static async Task AddEventSyncStatus(string taskId, int type)
	{
		AddSyncStatus(taskId, type, 10);
	}

	public static async Task AddSyncStatus(string entityId, int type, int create = 4)
	{
		if (string.IsNullOrEmpty(entityId))
		{
			return;
		}
		string userId = Utils.GetCurrentUserIdInt().ToString();
		if (type == create)
		{
			List<SyncStatusModel> list = await (from v in App.Connection.Table<SyncStatusModel>()
				where v.EntityId == entityId && v.Type == create && v.UserId == userId
				select v).ToListAsync();
			if (list != null && list.Count == 1)
			{
				SyncStatusModel syncStatusModel = list[0];
				syncStatusModel.ModifyPoint = DateTime.Now.Ticks;
				await Update(syncStatusModel);
				return;
			}
		}
		else
		{
			List<SyncStatusModel> list2 = await (from v in App.Connection.Table<SyncStatusModel>()
				where v.EntityId == entityId && v.UserId == userId && v.Type != 2 && v.Type != 16
				select v).ToListAsync();
			if (list2 != null && list2.Count == 1)
			{
				SyncStatusModel syncStatusModel2 = list2[0];
				if (syncStatusModel2.Type == create || syncStatusModel2.Type == type)
				{
					SyncStatusModel syncStatusModel3 = list2[0];
					syncStatusModel3.ModifyPoint = DateTime.Now.Ticks;
					await Update(syncStatusModel3);
					return;
				}
			}
		}
		await Insert(new SyncStatusModel
		{
			Type = type,
			EntityId = entityId,
			UserId = userId,
			ModifyPoint = DateTime.Now.Ticks
		});
	}

	public static async Task AddMoveProjectStatus(string taskId, string moveFromId)
	{
		await AddMoveOrRestoreProjectStatus(taskId, moveFromId);
	}

	public static async Task AddMoveEventStatus(string eventId, string moveFromId)
	{
		await AddMoveOrRestoreProjectStatus(eventId, moveFromId, 12);
	}

	public static async Task AddMoveOrRestoreProjectStatus(string taskId, string moveFromId, int status)
	{
		if (!string.IsNullOrEmpty(taskId))
		{
			string userId = Utils.GetCurrentUserIdInt().ToString();
			SyncStatusModel syncStatusModel = await (from v in App.Connection.Table<SyncStatusModel>()
				where v.EntityId == taskId && v.Type == status && v.UserId == userId
				select v).FirstOrDefaultAsync();
			SyncStatusModel syncStatusModel2 = new SyncStatusModel
			{
				Type = status,
				EntityId = taskId,
				MoveFromId = moveFromId,
				UserId = userId,
				ModifyPoint = DateTime.Now.Ticks
			};
			if (syncStatusModel != null)
			{
				syncStatusModel2._Id = syncStatusModel._Id;
				syncStatusModel2.ModifyPoint = DateTime.Now.Ticks;
				syncStatusModel2.MoveFromId = syncStatusModel.MoveFromId;
				await Update(syncStatusModel2);
			}
			else
			{
				await Insert(syncStatusModel2);
			}
		}
	}

	public static async Task AddMoveOrRestoreProjectStatus(string taskId, string moveFromId, bool isRestore = false)
	{
		await AddMoveOrRestoreProjectStatus(taskId, moveFromId, isRestore ? 7 : 2);
	}

	public static async Task<bool> ExistTaskMoveStatus(string taskId)
	{
		return await (from v in App.Connection.Table<SyncStatusModel>()
			where v.EntityId == taskId && v.Type == 2
			select v).FirstOrDefaultAsync() != null;
	}

	public static async Task AddTrashTaskStatus(string taskId)
	{
		Dictionary<int, SyncStatusModel> dict = await GetSyncStatusMap(taskId);
		if (dict.Count > 0)
		{
			if (dict.ContainsKey(4))
			{
				await DeleteSyncStatus(taskId, 4);
			}
			if (dict.ContainsKey(7))
			{
				await DeleteSyncStatus(taskId, 7);
			}
			if (dict.ContainsKey(5) || dict.ContainsKey(6))
			{
				return;
			}
		}
		await Insert(new SyncStatusModel
		{
			Type = 5,
			EntityId = taskId,
			UserId = Utils.GetCurrentUserIdInt().ToString()
		});
	}

	public static async Task<List<string>> GetEntityIdsByType(int type)
	{
		return await BaseDao<SyncStatusModel>.GetEntityIds($"select EntityId as Id from SyncStatusModel Where Type = {type} and UserId = '{LocalSettings.Settings.LoginUserId}'");
	}

	public static async Task<HashSet<string>> GetEntityIdSetByType(int type)
	{
		return await BaseDao<SyncStatusModel>.GetEntityIdSet($"select EntityId as Id from SyncStatusModel Where Type = {type} and UserId = '{LocalSettings.Settings.LoginUserId}'");
	}

	public static async Task DeleteEmptySyncStatus()
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		List<SyncStatusModel> list = await (from v in App.Connection.Table<SyncStatusModel>()
			where string.IsNullOrEmpty(v.EntityId) && v.UserId == userId
			select v).ToListAsync();
		if (list.Count <= 0)
		{
			return;
		}
		foreach (SyncStatusModel item in list)
		{
			await App.Connection.DeleteAsync(item);
		}
	}

	public static async Task<bool> DeleteSyncStatus(string entityId, int type, bool checkRecommit = true)
	{
		long lastSyncStartPoint = LocalSettings.Settings.SyncPoint;
		List<SyncStatusModel> list = await (from v in App.Connection.Table<SyncStatusModel>()
			where v.EntityId == entityId && v.Type == type
			select v).ToListAsync();
		bool reChanged = false;
		if (list.Count > 0)
		{
			foreach (SyncStatusModel status in list)
			{
				if (status.ModifyPoint > lastSyncStartPoint && checkRecommit)
				{
					SyncService.NeedRecommit = true;
					continue;
				}
				await App.Connection.DeleteAsync(status);
				if (status.ModifyPoint > lastSyncStartPoint)
				{
					reChanged = true;
				}
			}
		}
		if (type == 16)
		{
			await RefreshSetParentSyncStatusCache(entityId);
		}
		return reChanged;
	}

	public static async Task<Dictionary<int, SyncStatusModel>> GetSyncStatusMap(string taskSid)
	{
		Dictionary<int, SyncStatusModel> dict = new Dictionary<int, SyncStatusModel>();
		List<SyncStatusModel> list = await (from v in App.Connection.Table<SyncStatusModel>()
			where v.EntityId == taskSid
			select v).ToListAsync();
		if (list.Count > 0)
		{
			foreach (SyncStatusModel item in list)
			{
				if (!dict.ContainsKey(item.Type))
				{
					dict.Add(item.Type, item);
				}
			}
		}
		return dict;
	}

	public static async Task<List<SyncStatusModel>> GetAllSyncStatus()
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		return await (from model in App.Connection.Table<SyncStatusModel>()
			where model.UserId == userId
			select model).ToListAsync();
	}

	public static async Task<bool> CheckUnSyncItem()
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		return await (from model in App.Connection.Table<SyncStatusModel>()
			where model.UserId == userId
			select model).CountAsync() == 0;
	}

	public static async Task BatchAddTaskParentStatus(List<string> ids)
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		List<string> existEntities = (await GetSyncStatusByType(16))?.Select((SyncStatusModel s) => s.EntityId).ToList();
		List<SyncStatusModel> list = (from id in ids
			where existEntities == null || !existEntities.Contains(id)
			select new SyncStatusModel
			{
				Type = 16,
				EntityId = id,
				UserId = userId,
				ModifyPoint = DateTime.Now.Ticks
			}).ToList();
		if (list.Any())
		{
			await App.Connection.InsertAllAsync(list);
		}
		foreach (string item in ids.Where((string id) => !string.IsNullOrEmpty(id)))
		{
			CacheSetParentSyncStatus(item);
		}
	}

	public static async Task AddSetParentSyncStatus(string taskId, string oldParent)
	{
		List<SyncStatusModel> exists = (await GetSyncStatusByType(16))?.Where((SyncStatusModel s) => s.EntityId == taskId).ToList();
		if (exists != null && exists.Count > 0)
		{
			for (int i = 0; i < exists.Count; i++)
			{
				SyncStatusModel item = exists[i];
				if (i != 0)
				{
					await App.Connection.DeleteAsync(item);
				}
			}
			CacheSetParentSyncStatus(taskId);
		}
		else
		{
			string userId = Utils.GetCurrentUserIdInt().ToString();
			await Insert(new SyncStatusModel
			{
				Type = 16,
				EntityId = taskId,
				UserId = userId,
				ModifyPoint = DateTime.Now.Ticks,
				OldParentId = oldParent
			});
		}
	}

	public static async Task AddColumnMoveProjectStatus(string columnId, string moveFromId)
	{
		await AddMoveOrRestoreProjectStatus(columnId, moveFromId, 32);
	}
}
