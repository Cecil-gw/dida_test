using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TickTickDao;
using TickTickUtils;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Service.SortOrder;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Sync;

namespace ticktick_WPF.Dal;

public class ColumnDao : BaseDao<ColumnModel>
{
	public static async Task<bool> TryInitColumns(string projectId)
	{
		List<ColumnModel> serverColumns = await Communicator.GetRemoteColumnsByProjectId(projectId);
		if (serverColumns != null && serverColumns.Any())
		{
			serverColumns.ForEach(delegate(ColumnModel column)
			{
				column.userId = Utils.GetCurrentUserIdInt().ToString();
				column.syncStatus = "done";
			});
			await App.Connection.InsertAllAsync(serverColumns);
			string id = serverColumns[0].id;
			ProjectDao.SetProjectDefaultColumn(projectId, id);
			return false;
		}
		await InitColumns(projectId);
		return true;
	}

	public static async Task InitColumns(string projectId)
	{
		List<ColumnModel> list = await GetColumnsByProjectId(projectId);
		if (list == null || !list.Any())
		{
			string defaultColumnId = Utils.GetGuid();
			ColumnModel item = new ColumnModel
			{
				id = defaultColumnId,
				userId = Utils.GetCurrentUserIdInt().ToString(),
				projectId = projectId,
				sortOrder = 0L,
				name = Utils.GetString("NotSectioned"),
				createdTime = DateTime.Now,
				modifiedTime = DateTime.Now,
				syncStatus = "init"
			};
			await App.Connection.InsertAsync(item);
			await TaskSortOrderService.OnColumnInit(projectId, defaultColumnId);
			ProjectDao.SetProjectDefaultColumn(projectId, defaultColumnId);
		}
	}

	public static async Task<ColumnModel> GetColumnById(string columnId)
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		return await (from column in App.Connection.Table<ColumnModel>()
			where column.id == columnId && column.userId == userId
			select column).FirstOrDefaultAsync();
	}

	private static async Task<List<ColumnModel>> GetColumnsByIds(List<string> columnIds)
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		return await (from model in App.Connection.Table<ColumnModel>()
			where columnIds.Contains(model.id) && model.deleted != 1 && model.userId == userId
			orderby model.sortOrder
			select model).ToListAsync();
	}

	public static async Task<List<ColumnModel>> GetColumnsByProjectId(string projectId)
	{
		string userId = Utils.GetCurrentUserStr();
		List<ColumnModel> columns = await (from model in App.Connection.Table<ColumnModel>()
			where model.projectId == projectId && model.userId == userId
			orderby model.sortOrder
			select model).ToListAsync();
		Dictionary<string, ColumnModel> dictionary = new Dictionary<string, ColumnModel>();
		List<ColumnModel> list = columns;
		if (list != null && list.Count > 0)
		{
			List<ColumnModel> list2 = new List<ColumnModel>();
			foreach (ColumnModel item in columns)
			{
				if (string.IsNullOrEmpty(item.id))
				{
					continue;
				}
				if (dictionary.ContainsKey(item.id))
				{
					ColumnModel columnModel = dictionary[item.id];
					if (columnModel.deleted == 1 && columnModel.deleted != 1)
					{
						dictionary[item.id] = item;
						App.Connection.DeleteAsync(columnModel);
						list2.Remove(columnModel);
						list2.Add(item);
					}
					else
					{
						App.Connection.DeleteAsync(item);
					}
				}
				else
				{
					dictionary.Add(item.id, item);
					if (item.deleted != 1)
					{
						list2.Add(item);
					}
				}
			}
			columns = list2;
		}
		List<ColumnModel> list3 = columns;
		if (list3 != null && list3.Count > 0 && new HashSet<long>(columns.Select((ColumnModel c) => c.sortOrder.GetValueOrDefault())).Count < columns.Count)
		{
			long so = 0L;
			foreach (ColumnModel item2 in columns)
			{
				item2.sortOrder = so;
				await UpdateColumn(item2);
				so += 268435456;
			}
			SyncManager.TryDelaySync();
		}
		return columns;
	}

	public static async Task DeleteColumn(string columnId)
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		List<ColumnModel> columns = await (from m in App.Connection.Table<ColumnModel>()
			where m.id == columnId && m.userId == userId
			select m).ToListAsync();
		for (int i = 0; i < columns.Count; i++)
		{
			ColumnModel columnModel = columns[i];
			if (i == 0)
			{
				if (columnModel != null)
				{
					columnModel.syncStatus = "delete";
					columnModel.deleted = 1;
					await App.Connection.UpdateAsync(columnModel);
				}
			}
			else
			{
				await BaseDao<ColumnModel>.DeleteAsync(columnModel);
			}
		}
	}

	public static async Task SaveColumnName(string name, string columnId, bool isNew = false)
	{
		ColumnModel columnModel = await GetColumnById(columnId);
		if (columnModel != null)
		{
			columnModel.name = name;
			if (isNew)
			{
				columnModel.syncStatus = "new";
			}
			if (columnModel.syncStatus != "new" && columnModel.syncStatus != "init")
			{
				columnModel.syncStatus = "update";
			}
			await App.Connection.UpdateAsync(columnModel);
		}
	}

	public static async Task SaveColumnColor(string color, string columnId)
	{
		ColumnModel columnModel = await GetColumnById(columnId);
		if (columnModel != null)
		{
			columnModel.color = color;
			columnModel.modifiedTime = DateTime.Now;
			await UpdateColumn(columnModel);
		}
	}

	public static async Task<string> GetProjectDefaultColumnId(string projectId, bool ignoreFirst = false)
	{
		List<ColumnModel> list = await GetColumnsByProjectId(projectId);
		if (list != null && list.Any())
		{
			list = list.OrderBy((ColumnModel column) => column.sortOrder).ToList();
			if (ignoreFirst)
			{
				list.RemoveAt(0);
			}
			return list.FirstOrDefault()?.id;
		}
		return string.Empty;
	}

	public static async Task SaveAsLastSortOrder(string dragId)
	{
		ColumnModel dragColumn = await GetColumnById(dragId);
		if (dragColumn != null)
		{
			List<ColumnModel> obj = await GetColumnsByProjectId(dragColumn.projectId);
			long valueOrDefault = ((obj == null) ? ((long?)null) : (obj.Select((ColumnModel c) => c.sortOrder).Max() + 268435456)).GetValueOrDefault();
			dragColumn.sortOrder = valueOrDefault;
			dragColumn.modifiedTime = DateTime.Now;
			if (dragColumn.syncStatus != "new")
			{
				dragColumn.syncStatus = "update";
			}
			await App.Connection.UpdateAsync(dragColumn);
		}
	}

	public static async Task<long?> SaveSortOrder(string dragId, string dropId, bool left = false)
	{
		ColumnModel dragColumn = await GetColumnById(dragId);
		if (dragColumn != null)
		{
			List<ColumnModel> list = await GetColumnsByProjectId(dragColumn.projectId);
			if (list.Any())
			{
				ColumnModel columnModel = list.FirstOrDefault((ColumnModel item) => item.id == dropId);
				if (columnModel != null)
				{
					long valueOrDefault = columnModel.sortOrder.GetValueOrDefault();
					int num = list.IndexOf(columnModel);
					long value = (left ? ((num > 0) ? (valueOrDefault / 2 + list[num - 1].sortOrder.GetValueOrDefault() / 2) : (valueOrDefault - 268435456)) : ((num < list.Count - 1) ? (valueOrDefault / 2 + list[num + 1].sortOrder.GetValueOrDefault() / 2) : (valueOrDefault + 268435456)));
					dragColumn.sortOrder = value;
					dragColumn.modifiedTime = DateTime.Now;
					if (dragColumn.syncStatus != "new")
					{
						dragColumn.syncStatus = "update";
					}
					await App.Connection.UpdateAsync(dragColumn);
					return dragColumn.sortOrder;
				}
			}
		}
		return null;
	}

	public static async Task<Dictionary<string, string>> GetNeedPostColumns()
	{
		List<ColumnSyncStatusModel> list = await App.Connection.QueryAsync<ColumnSyncStatusModel>("select id as ColumnId, syncStatus as SyncStatus from ColumnModel where projectId is not null and syncStatus is not null and syncStatus <> 'done' and userId = '" + Utils.GetCurrentUserIdInt() + "'", Array.Empty<object>());
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		if (list != null && list.Any())
		{
			foreach (ColumnSyncStatusModel item in list)
			{
				if (!dictionary.ContainsKey(item.ColumnId))
				{
					dictionary.Add(item.ColumnId, item.ColumnId);
				}
			}
		}
		return dictionary;
	}

	public static async Task SaveCommitResultBackToDb(Dictionary<string, string> id2Etag, Dictionary<string, string> id2Error, LogModel logModel)
	{
		if (id2Etag != null)
		{
			logModel.Log += "Id2etag : ";
			foreach (string serverId2 in id2Etag.Keys)
			{
				ColumnModel columnModel = await GetColumnById(serverId2);
				if (columnModel != null)
				{
					columnModel.syncStatus = "done";
					columnModel.etag = id2Etag[serverId2];
					await App.Connection.UpdateAsync(columnModel);
					logModel.Log = logModel.Log + serverId2 + "  ";
				}
			}
		}
		if (id2Error == null)
		{
			return;
		}
		string serverId = string.Empty;
		foreach (string serverId2 in id2Error.Keys)
		{
			ColumnModel column = await GetColumnById(serverId2);
			if (column != null)
			{
				if (id2Error[serverId2] == "NOT_EXISTED")
				{
					column.syncStatus = ((column.deleted == 0) ? "new" : "done");
					await App.Connection.UpdateAsync(column);
				}
				if (id2Error[serverId2] == "EXISTED")
				{
					column.syncStatus = ((column.deleted == 0) ? "update" : "done");
					await App.Connection.UpdateAsync(column);
				}
			}
			serverId = serverId + serverId2 + " : " + id2Error[serverId2];
		}
		logModel.Log = logModel.Log + "  error " + serverId;
	}

	public static async Task<Dictionary<string, ColumnModel>> GetLocalSyncedColumnMap()
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		Dictionary<string, ColumnModel> dict = new Dictionary<string, ColumnModel>();
		List<ColumnModel> list = await (from v in App.Connection.Table<ColumnModel>()
			where v.userId.Equals(userId)
			select v).ToListAsync();
		if (list != null && list.Any())
		{
			foreach (ColumnModel item in list)
			{
				if (!string.IsNullOrEmpty(item.id) && !dict.ContainsKey(item.id))
				{
					dict.Add(item.id, item);
				}
				else
				{
					BaseDao<ColumnModel>.DeleteAsync(item);
				}
			}
		}
		return dict;
	}

	public static async Task<Dictionary<string, ColumnModel>> GetLocalSyncedColumnMap(string projectId)
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		Dictionary<string, ColumnModel> dict = new Dictionary<string, ColumnModel>();
		List<ColumnModel> list = await (from v in App.Connection.Table<ColumnModel>()
			where v.userId.Equals(userId) && v.projectId == projectId
			select v).ToListAsync();
		if (list != null && list.Any())
		{
			foreach (ColumnModel item in list)
			{
				if (!string.IsNullOrEmpty(item.id) && !dict.ContainsKey(item.id))
				{
					dict.Add(item.id, item);
				}
				else
				{
					BaseDao<ColumnModel>.DeleteAsync(item);
				}
			}
		}
		return dict;
	}

	public static async Task DeleteColumns(List<ColumnProjectModel> models)
	{
		List<ColumnModel> list = await GetColumnsByIds(models.Select((ColumnProjectModel model) => model.columnId).ToList());
		if (!list.Any())
		{
			return;
		}
		foreach (ColumnModel item in list)
		{
			await App.Connection.DeleteAsync(item);
		}
	}

	public static async Task SaveServerMergeData(List<ColumnModel> added, List<ColumnModel> updated, List<ColumnModel> deleted)
	{
		if (added.Count > 0)
		{
			await App.Connection.InsertAllAsync(added);
		}
		if (updated.Count > 0)
		{
			await App.Connection.UpdateAllAsync(updated);
		}
		if (deleted.Count <= 0)
		{
			return;
		}
		foreach (ColumnModel item in deleted)
		{
			await App.Connection.DeleteAsync(item);
		}
	}

	public static async Task<ColumnModel> AddColumn(string name, string projectId, long sortOrder, string id = null, string color = null)
	{
		ColumnModel column = new ColumnModel
		{
			id = (string.IsNullOrEmpty(id) ? Utils.GetGuid() : id),
			name = name,
			projectId = projectId,
			sortOrder = sortOrder,
			userId = Utils.GetCurrentUserIdInt().ToString(),
			createdTime = DateTime.Now,
			deleted = 0,
			syncStatus = "new",
			color = color
		};
		await App.Connection.InsertAsync(column);
		return column;
	}

	public static async Task UpdateColumn(ColumnModel column)
	{
		if (column.syncStatus != "new" && column.syncStatus != "init")
		{
			column.syncStatus = "update";
		}
		await App.Connection.UpdateAsync(column);
	}

	public static async Task<List<ColumnModel>> GetAllColumnsAsync()
	{
		return await Task.Run(async () => await (from c in Connection.DbConnection.Table<ColumnModel>()
			where c.userId == LocalSettings.Settings.LoginUserId && c.deleted != 1
			select c).ToListAsync());
	}

	public static async Task<List<ColumnModel>> CheckProjectColumns(string projectId)
	{
		List<ColumnModel> list = await GetColumnsByProjectId(projectId);
		if (list != null && list.Any())
		{
			await ColumnBatchHandler.MergeWithServer(projectId);
		}
		else
		{
			await TryInitColumns(projectId);
		}
		return await GetColumnsByProjectId(projectId);
	}

	public static async Task UpdateColumnProjectId(string columnId, string projectId)
	{
		await App.Connection.ExecuteAsync("Update ColumnModel SET projectId=? WHERE id=? and userId=?", projectId, columnId, LocalSettings.Settings.LoginUserId);
	}
}
