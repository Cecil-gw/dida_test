using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SQLite;
using TickTickModels;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Service.SortOrder;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Sync.Model;

namespace ticktick_WPF.Dal;

public class SyncSortOrderDao : BaseModel
{
	public static async Task<int> InsertAllAsync(List<SyncSortOrderModel> models)
	{
		return await App.Connection.InsertAllAsync(models);
	}

	public static async Task<int> InsertAsync(SyncSortOrderModel model)
	{
		return await App.Connection.InsertAsync(model);
	}

	public static async Task<int> UpdateAllAsync(List<SyncSortOrderModel> models)
	{
		return await App.Connection.UpdateAllAsync(models);
	}

	public static async Task<int> UpdateAsync(SyncSortOrderModel model)
	{
		return await App.Connection.UpdateAsync(model);
	}

	public static async Task<int> DeleteAllAsync(List<SyncSortOrderModel> models)
	{
		return (await Task.WhenAll(models.Select(App.Connection.DeleteAsync))).Sum();
	}

	public static async Task<List<SyncSortOrderModel>> GetAllOrderBySortOrderType(string type, string groupId = null)
	{
		string text = "SELECT * FROM SyncSortOrderModel WHERE UserId=? AND SortOrderType=? AND Deleted=?";
		if (string.IsNullOrEmpty(groupId))
		{
			return await App.Connection.QueryAsync<SyncSortOrderModel>(text, new object[3]
			{
				LocalSettings.Settings.LoginUserId,
				type,
				0
			});
		}
		return await App.Connection.QueryAsync<SyncSortOrderModel>(text + " AND GroupId=?", new object[4]
		{
			LocalSettings.Settings.LoginUserId,
			type,
			0,
			groupId
		});
	}

	public static async Task<SyncSortOrderModel> GetSortOrderByIdAndType(string sortType, string id, int type)
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		return await (from v in App.Connection.Table<SyncSortOrderModel>()
			where v.SortOrderType == sortType && v.UserId == userId && v.Deleted == 0 && v.EntityId == id && v.Type == type
			select v).FirstOrDefaultAsync();
	}

	public static async Task<SyncSortOrderModel> GetSortOrderAsync(string sortOrderType, string groupId, string entityId, int type)
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		return await (from v in App.Connection.Table<SyncSortOrderModel>()
			where v.UserId == userId && v.Deleted == 0 && v.SortOrderType == sortOrderType && v.GroupId == groupId && v.EntityId == entityId && v.Type == type
			select v).FirstOrDefaultAsync();
	}

	public static async Task<List<SyncSortOrderModel>> SetAllTaskOrderInTag()
	{
		return await (from v in App.Connection.Table<SyncSortOrderModel>()
			where v.UserId == LocalSettings.Settings.LoginUserId && v.SortOrderType.StartsWith("taskByTag") && v.Deleted == 0
			select v).ToListAsync();
	}

	public static async Task<int> SetAllDeletedByGroupId(SortOption option, string id)
	{
		string sortKey = option.GetSortKey();
		if (string.IsNullOrEmpty(sortKey))
		{
			return 0;
		}
		return await App.Connection.ExecuteAsync("UPDATE SyncSortOrderModel" + string.Format(" SET {0}={1}", "Deleted", 1) + string.Format(", {0}={1}", "SyncStatus", 1) + " WHERE GroupId=? AND UserId=?  AND SortOrderType LIKE ?", id, LocalSettings.Settings.LoginUserId, sortKey.Replace("{0}", "%"));
	}

	public static void CopyLegacyMatrixOrders(SQLiteConnection connection, string userId, string profileId)
	{
		foreach (SyncSortOrderModel item in connection.Query<SyncSortOrderModel>("SELECT * FROM SyncSortOrderModel WHERE UserId=? AND Deleted=? AND GroupId IN (?,?,?,?) AND (SortOrderType=? OR SortOrderType LIKE ?)", new object[8] { userId, 0, "quadrant1", "quadrant2", "quadrant3", "quadrant4", "taskPinned", "taskBy#%" }))
		{
			if (connection.Insert(new SyncSortOrderModel
			{
				UserId = userId,
				SortOrderType = item.SortOrderType,
				GroupId = item.GroupId + "_" + profileId,
				EntityId = item.EntityId,
				Type = item.Type,
				SortOrder = item.SortOrder,
				ColumnId = item.ColumnId,
				SyncStatus = 0,
				Deleted = 0
			}) != 1)
			{
				throw new InvalidOperationException("Failed to copy legacy matrix task orders.");
			}
		}
	}

	public static Task<int> SetAllTaskOrdersDeletedByGroupId(string id)
	{
		return App.Connection.ExecuteAsync("UPDATE SyncSortOrderModel SET Deleted=?, SyncStatus=? WHERE GroupId=? AND UserId=? AND Deleted=? AND (SortOrderType=? OR SortOrderType LIKE ?)", 1, 1, id, LocalSettings.Settings.LoginUserId, 0, "taskPinned", "taskBy#%");
	}

	public static async Task<int> CountTagSortOrderByGroupId(string id)
	{
		return await (from model in App.Connection.Table<SyncSortOrderModel>()
			where model.SortOrderType.StartsWith("taskByTag") && model.GroupId == id && model.Deleted == 0
			select model).CountAsync();
	}

	public static async Task<List<SyncSortOrderModel>> GetLocalSortOrders()
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		return await (from v in App.Connection.Table<SyncSortOrderModel>()
			where v.UserId == userId
			select v).ToListAsync();
	}

	public static async Task<List<SyncSortOrderModel>> GetLocalSortOrdersByKeys(List<(string SortOrderType, string GroupId, string EntityId, int Type)> orderKeys)
	{
		if (orderKeys == null || orderKeys.Count == 0)
		{
			return new List<SyncSortOrderModel>();
		}
		string userId = Utils.GetCurrentUserIdInt().ToString();
		List<SyncSortOrderModel> result = new List<SyncSortOrderModel>();
		List<(string SortOrderType, string GroupId, string EntityId, int Type)> distinctKeys = orderKeys.Distinct().ToList();
		for (int offset = 0; offset < distinctKeys.Count; offset += 200)
		{
			List<(string, string, string, int)> list = distinctKeys.Skip(offset).Take(200).ToList();
			string text = string.Join(" OR ", Enumerable.Repeat("(SortOrderType=? AND GroupId=? AND EntityId=? AND Type=?)", list.Count));
			List<object> list2 = new List<object> { userId };
			foreach (var item in list)
			{
				list2.Add(item.Item1);
				list2.Add(item.Item2);
				list2.Add(item.Item3);
				list2.Add(item.Item4);
			}
			result.AddRange(await App.Connection.QueryAsync<SyncSortOrderModel>("SELECT * FROM SyncSortOrderModel WHERE UserId=? AND (" + text + ")", list2.ToArray()));
		}
		return result;
	}

	public static async Task SaveRemoteChangesToDb(SyncDataBean<SyncSortOrderModel> bean)
	{
		await InsertAllAsync(bean.Addeds);
		await UpdateAllAsync(bean.Updateds);
		await DeleteAllAsync(bean.Deleteds);
		HashSet<string> ids = new HashSet<string>();
		bean.Addeds?.ForEach(delegate(SyncSortOrderModel item)
		{
			if (item.SortOrderType != "projectPinned")
			{
				ids.Add(item.EntityId);
			}
		});
		bean.Updateds?.ForEach(delegate(SyncSortOrderModel item)
		{
			if (item.SortOrderType != "projectPinned")
			{
				ids.Add(item.EntityId);
			}
		});
		bean.Deleteds?.ForEach(delegate(SyncSortOrderModel item)
		{
			if (item.SortOrderType != "projectPinned")
			{
				ids.Add(item.EntityId);
			}
		});
		if (ids.Any())
		{
			await TaskSortOrderService.FetchAllAsync();
			TaskChangeNotifier.NotifyTaskBatchChanged(ids.ToList());
		}
		DataChangedNotifier.NotifyProjectPinRemoteChanged();
	}

	public static async Task<Dictionary<string, Dictionary<string, List<SyncSortOrderModel>>>> GetNeedPostMap()
	{
		Dictionary<string, Dictionary<string, List<SyncSortOrderModel>>> map = new Dictionary<string, Dictionary<string, List<SyncSortOrderModel>>>();
		foreach (SyncSortOrderModel item in await GetNeedPostSortOrders())
		{
			Dictionary<string, List<SyncSortOrderModel>> dictionary = null;
			if (map.ContainsKey(item.SortOrderType))
			{
				dictionary = map[item.SortOrderType];
			}
			if (dictionary == null)
			{
				dictionary = new Dictionary<string, List<SyncSortOrderModel>>();
				List<SyncSortOrderModel> value = new List<SyncSortOrderModel> { item };
				dictionary.Add(item.GroupId, value);
				map.Add(item.SortOrderType, dictionary);
			}
			else if (dictionary.ContainsKey(item.GroupId))
			{
				dictionary[item.GroupId].Add(item);
			}
			else
			{
				List<SyncSortOrderModel> value2 = new List<SyncSortOrderModel> { item };
				dictionary.Add(item.GroupId, value2);
			}
		}
		return map;
	}

	public static async Task<List<SyncSortOrderModel>> GetNeedPostSortOrders()
	{
		return await Task.Run(async () => await (from v in App.Connection.Table<SyncSortOrderModel>()
			where v.UserId == LocalSettings.Settings.LoginUserId && v.SyncStatus != 2
			select v).ToListAsync());
	}

	public static async Task SavePostResult(SyncDataBean<SyncSortOrderModel> dateBean)
	{
		if (dateBean != null)
		{
			if (dateBean.Deleteds != null && dateBean.Deleteds.Count > 0)
			{
				foreach (SyncSortOrderModel deleted in dateBean.Deleteds)
				{
					await App.Connection.DeleteAsync(deleted);
				}
			}
			if (dateBean.Updateds != null && dateBean.Updateds.Count > 0)
			{
				foreach (SyncSortOrderModel updated in dateBean.Updateds)
				{
					updated.SyncStatus = 2;
				}
				await App.Connection.UpdateAllAsync(dateBean.Updateds);
			}
		}
		await ClearDeletedOrders();
	}

	private static async Task ClearDeletedOrders()
	{
		string userId = LocalSettings.Settings.LoginUserId;
		List<SyncSortOrderModel> list = await (from v in App.Connection.Table<SyncSortOrderModel>()
			where v.Deleted != 0 && v.SyncStatus == 2 && v.UserId == userId
			select v).ToListAsync();
		if (list == null || list.Count <= 0)
		{
			return;
		}
		foreach (SyncSortOrderModel item in list)
		{
			await App.Connection.DeleteAsync(item);
		}
	}

	public static async Task<int> InsertOrUpdate(SyncSortOrderModel model)
	{
		string userId = LocalSettings.Settings.LoginUserId;
		SyncSortOrderModel syncSortOrderModel = await (from o in App.Connection.Table<SyncSortOrderModel>()
			where o.SortOrderType == model.SortOrderType && o.GroupId == model.GroupId && o.EntityId == model.EntityId && o.Type == model.Type && o.UserId == userId
			select o).FirstOrDefaultAsync();
		int result;
		if (syncSortOrderModel != null)
		{
			syncSortOrderModel.SortOrder = model.SortOrder;
			syncSortOrderModel.SyncStatus = model.SyncStatus;
			syncSortOrderModel.Deleted = model.Deleted;
			result = await UpdateAsync(syncSortOrderModel);
		}
		else
		{
			result = await InsertAsync(model);
		}
		SyncManager.TryDelaySync();
		return result;
	}

	public static async Task DeleteAsync(SyncSortOrderModel model)
	{
		string userId = LocalSettings.Settings.LoginUserId;
		SyncSortOrderModel syncSortOrderModel = await (from o in App.Connection.Table<SyncSortOrderModel>()
			where o.SortOrderType == model.SortOrderType && o.GroupId == model.GroupId && o.EntityId == model.EntityId && o.Type == model.Type && o.UserId == userId
			select o).FirstOrDefaultAsync();
		if (syncSortOrderModel == null && !string.IsNullOrEmpty(model.GroupId))
		{
			string groupId = model.GroupId.ToLowerInvariant();
			syncSortOrderModel = await (from o in App.Connection.Table<SyncSortOrderModel>()
				where o.SortOrderType == model.SortOrderType && o.GroupId.ToLower() == groupId && o.EntityId == model.EntityId && o.Type == model.Type && o.UserId == userId
				select o).FirstOrDefaultAsync();
		}
		if (syncSortOrderModel != null)
		{
			syncSortOrderModel.SyncStatus = 1;
			syncSortOrderModel.Deleted = 1;
			await UpdateAsync(syncSortOrderModel);
		}
		SyncManager.TryDelaySync();
	}

	public static async Task DealTagRenamedTask(string originalName, string newName)
	{
		string userId = LocalSettings.Settings.LoginUserId;
		string originalGroupId = "#" + originalName;
		string newGroupId = "#" + newName;
		List<SyncSortOrderModel> list = await (from o in App.Connection.Table<SyncSortOrderModel>()
			where o.UserId == userId
			where o.GroupId == originalGroupId
			select o).ToListAsync();
		if (list != null && list.Count > 0)
		{
			foreach (SyncSortOrderModel local in list)
			{
				local.SyncStatus = 1;
				local.Deleted = 1;
				await App.Connection.UpdateAsync(local);
				SyncSortOrderModel syncSortOrderModel = new SyncSortOrderModel();
				SyncSortOrderModel.Copy(syncSortOrderModel, local);
				syncSortOrderModel.GroupId = newGroupId;
				syncSortOrderModel.Deleted = 0;
				syncSortOrderModel.SyncStatus = 1;
				await InsertAsync(syncSortOrderModel);
			}
		}
		await TaskSortOrderService.FetchAllAsync();
		SyncManager.TryDelaySync();
	}

	public static async Task DealTagRenameProject(string originalName, string newName)
	{
		string userId = LocalSettings.Settings.LoginUserId;
		List<SyncSortOrderModel> list = await (from o in App.Connection.Table<SyncSortOrderModel>()
			where o.EntityId == originalName && o.Type == 7 && o.UserId == userId
			select o).ToListAsync();
		if (list != null && list.Count > 0)
		{
			foreach (SyncSortOrderModel local in list)
			{
				local.SyncStatus = 1;
				local.Deleted = 1;
				await UpdateAsync(local);
				await InsertAsync(new SyncSortOrderModel
				{
					UserId = local.UserId,
					EntityId = newName,
					SortOrderType = local.SortOrderType,
					SortOrder = local.SortOrder,
					SyncStatus = 1,
					Deleted = 0,
					GroupId = local.GroupId
				});
			}
		}
		SyncManager.TryDelaySync();
	}

	public static async Task OnTagRenamed(string originalName, string newName)
	{
		await DealTagRenameProject(originalName, newName);
		await DealTagRenamedTask(originalName, newName);
	}

	public static async Task DealTagDeletedTask(string tagName)
	{
		string userId = LocalSettings.Settings.LoginUserId;
		string typeTagName = "taskByTag_" + tagName;
		List<SyncSortOrderModel> list = await (from o in App.Connection.Table<SyncSortOrderModel>()
			where o.UserId == userId
			where o.GroupId == tagName || o.SortOrderType == typeTagName
			select o).ToListAsync();
		if (list != null && list.Count > 0)
		{
			foreach (SyncSortOrderModel item in list)
			{
				item.SyncStatus = 1;
				item.Deleted = 1;
				await UpdateAsync(item);
			}
		}
		await TaskSortOrderService.FetchAllAsync();
		SyncManager.TryDelaySync();
	}

	public static async Task DealTagDeletedProject(string tagName)
	{
		string userId = LocalSettings.Settings.LoginUserId;
		List<SyncSortOrderModel> list = await (from o in App.Connection.Table<SyncSortOrderModel>()
			where o.EntityId == tagName && o.Type == 7 && o.UserId == userId
			select o).ToListAsync();
		if (list != null && list.Count > 0)
		{
			foreach (SyncSortOrderModel item in list)
			{
				item.SyncStatus = 1;
				item.Deleted = 1;
				await UpdateAsync(item);
			}
		}
		SyncManager.TryDelaySync();
	}

	public static async Task OnTagDeleted(string tagName)
	{
		await DealTagDeletedProject(tagName);
		await DealTagDeletedTask(tagName);
	}

	public static async Task OnFilterDeleted(string filterId)
	{
		string userId = LocalSettings.Settings.LoginUserId;
		List<SyncSortOrderModel> list = await (from o in App.Connection.Table<SyncSortOrderModel>()
			where o.EntityId == filterId && o.Type == 8 && o.UserId == userId
			select o).ToListAsync();
		if (list != null && list.Count > 0)
		{
			foreach (SyncSortOrderModel item in list)
			{
				item.SyncStatus = 1;
				item.Deleted = 1;
				await UpdateAsync(item);
			}
		}
		SyncManager.TryDelaySync();
	}

	public static async Task OnProjectDeletedOrClosed(string projectId)
	{
		string userId = LocalSettings.Settings.LoginUserId;
		List<SyncSortOrderModel> list = await (from o in App.Connection.Table<SyncSortOrderModel>()
			where o.EntityId == projectId && o.Type == 5 && o.UserId == userId
			select o).ToListAsync();
		if (list != null && list.Count > 0)
		{
			foreach (SyncSortOrderModel item in list)
			{
				item.SyncStatus = 1;
				item.Deleted = 1;
				await UpdateAsync(item);
			}
		}
		SyncManager.TryDelaySync();
	}

	public static async Task<bool> ExistTaskListSortOrder(SortOption option, string catId)
	{
		string sortKey = option.GetSortKey();
		if (string.IsNullOrEmpty(sortKey))
		{
			return false;
		}
		List<int> obj = await App.Connection.QueryAsync<int>("Select _Id from SyncSortOrderModel WHERE GroupId=? AND Deleted != 1 AND UserId=?  AND SortOrderType LIKE ? limit 1", new object[3]
		{
			catId,
			LocalSettings.Settings.LoginUserId,
			sortKey.Replace("{0}", "%")
		});
		return obj != null && obj.Count > 0;
	}

	public static async Task<List<SyncSortOrderModel>> GetVersion2Models()
	{
		return await App.Connection.QueryAsync<SyncSortOrderModel>("Select * from SyncSortOrderModel  WHERE UserId='" + LocalSettings.Settings.LoginUserId + "' AND Deleted != 1  AND SortOrderType LIKE 'taskBy#%'", Array.Empty<object>());
	}

	public static async Task<List<SyncSortOrderModel>> GetNoColumnOrderInProject(string catId)
	{
		return await App.Connection.QueryAsync<SyncSortOrderModel>("Select * from SyncSortOrderModel WHERE GroupId='" + catId + "' AND Deleted != 1 AND UserId='" + LocalSettings.Settings.LoginUserId + "'  AND SortOrderType LIKE 'taskBy#sortOrder|none%'", Array.Empty<object>());
	}
}
