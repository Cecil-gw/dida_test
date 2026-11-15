using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TickTickUtils.Lang;
using ticktick_WPF.Cache;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Dal;

public static class TaskDetailItemDao
{
	public static async Task<List<TaskDetailItemModel>> GetCheckItemsByTaskId(string taskId)
	{
		return await (from item in App.Connection.Table<TaskDetailItemModel>()
			where item.TaskServerId == taskId
			select item).ToListAsync();
	}

	public static async Task<List<TaskDetailItemModel>> GetCheckItemsInTaskIds(ICollection<string> taskIds)
	{
		return await (from item in App.Connection.Table<TaskDetailItemModel>()
			where taskIds.Contains(item.TaskServerId)
			select item).ToListAsync();
	}

	public static async Task SaveChecklistItem(TaskDetailItemModel model, bool updateCache = true, bool checkCount = true)
	{
		if (model != null)
		{
			if (updateCache || !TaskDetailItemCache.ExistItem(model.id))
			{
				TaskDetailItemCache.OnCheckItemUpdated(model, checkCount);
			}
			TaskDetailItemModel taskDetailItemModel = await GetCheckItemById(model.id);
			if (taskDetailItemModel != null)
			{
				model._Id = taskDetailItemModel._Id;
				await App.Connection.UpdateAsync(model);
			}
			else
			{
				await App.Connection.InsertAsync(model);
			}
		}
	}

	public static async Task TrySaveChecklistItem(TaskDetailItemModel model, bool checkCount = true)
	{
		await SaveChecklistItem(model, updateCache: true, checkCount);
	}

	public static async Task<List<TaskDetailItemModel>> DeleteCheckItemsByTaskId(string taskId, List<string> ignore = null)
	{
		List<TaskDetailItemModel> items = await GetCheckItemsByTaskId(taskId);
		if (items != null && items.Any())
		{
			List<TaskBaseViewModel> vms = new List<TaskBaseViewModel>();
			foreach (TaskDetailItemModel item in items)
			{
				if ((ignore == null || !ignore.Contains(item.id)) && !string.IsNullOrEmpty(item.id))
				{
					await App.Connection.DeleteAsync(item);
					TaskDetailItemCache.LocalCheckItemViewModels.TryRemove(item.id, out var value);
					if (value != null)
					{
						vms.Add(value);
					}
				}
			}
			if (vms.Count > 0)
			{
				ProjectAndTaskIdsCache.OnCheckItemsRemoved(vms);
			}
		}
		return items;
	}

	public static async Task InsertChecklistItem(TaskDetailItemModel model)
	{
		if (model != null)
		{
			await App.Connection.InsertAsync(model);
			TaskDetailItemCache.OnCheckItemAdded(model);
		}
	}

	public static async Task DeleteById(string id)
	{
		TaskDetailItemModel taskDetailItemModel = await GetCheckItemById(id);
		if (taskDetailItemModel != null)
		{
			UtilLog.Info("DeleteTaskCheckItem taskId " + taskDetailItemModel.TaskServerId);
			await App.Connection.DeleteAsync(taskDetailItemModel);
			TaskDetailItemCache.DeleteCheckItemById(id);
		}
	}

	public static async Task BatchInsertChecklists(List<TaskDetailItemModel> models)
	{
		await App.Connection.InsertAllAsync(models);
		TaskDetailItemCache.BatchAddCheckItems(models);
	}

	public static async Task BatchUpdateChecklists(List<TaskDetailItemModel> models)
	{
		await App.Connection.UpdateAllAsync(models);
		TaskDetailItemCache.OnCheckItemsBatchUpdated(models);
	}

	public static async Task<List<TaskDetailItemModel>> BatchGetItems(List<string> itemIds)
	{
		if (itemIds == null || itemIds.Count <= 0)
		{
			return new List<TaskDetailItemModel>();
		}
		return await (from item in App.Connection.Table<TaskDetailItemModel>()
			where itemIds.Contains(item.id)
			select item).ToListAsync();
	}

	public static async Task<TaskDetailItemModel> GetChecklistItemById(string id)
	{
		return await GetCheckItemById(id);
	}

	public static async Task<TaskDetailItemModel> GetCheckItemById(string id)
	{
		return await (from v in App.Connection.Table<TaskDetailItemModel>()
			where v.id == id
			select v).FirstOrDefaultAsync();
	}

	public static async Task<Dictionary<string, long>> GetCheckItemSortOrderInTask(List<string> taskIds)
	{
		Dictionary<string, long> dict = new Dictionary<string, long>();
		List<TaskModel> list = await TaskDao.GetThinTasksInBatch(taskIds);
		if (list == null || list.Count == 0)
		{
			return dict;
		}
		foreach (TaskModel task in list)
		{
			List<TaskDetailItemModel> list2 = (await GetCheckItemsByTaskId(task.id))?.OrderBy((TaskDetailItemModel v) => v.sortOrder).ToList();
			if (list2 == null || list2.Count == 0)
			{
				continue;
			}
			for (int num = 0; num < list2.Count; num++)
			{
				if (list2[num] != null && !string.IsNullOrEmpty(list2[num].id) && !dict.ContainsKey(list2[num].id))
				{
					dict.Add(list2[num].id, task.sortOrder + num + 1);
				}
			}
		}
		return dict;
	}

	public static async Task<TaskDetailItemModel> GetPrimarySubtaskInTask(string taskId)
	{
		List<TaskDetailItemModel> list = await GetCheckItemsByTaskId(taskId);
		if (list != null && list.Any())
		{
			return (from item in list
				orderby item.status, item.sortOrder
				select item).FirstOrDefault();
		}
		return null;
	}

	public static async Task<Dictionary<string, TaskDetailItemModel>> GetPrimarySubtaskInTaskIds(List<string> taskIds)
	{
		Dictionary<string, TaskDetailItemModel> result = new Dictionary<string, TaskDetailItemModel>();
		if (taskIds != null && taskIds.Any())
		{
			List<TaskDetailItemModel> list = await GetCheckItemsInTaskIds(taskIds);
			if (list != null && list.Any())
			{
				foreach (string taskId in taskIds)
				{
					TaskDetailItemModel taskDetailItemModel = (from item in list
						where item.TaskServerId == taskId
						orderby item.status, item.sortOrder
						select item).FirstOrDefault();
					if (taskDetailItemModel != null && !result.ContainsKey(taskId))
					{
						result.Add(taskId, taskDetailItemModel);
					}
				}
			}
		}
		return result;
	}

	public static async void ClearSameItemById(string itemId)
	{
		if (string.IsNullOrEmpty(itemId))
		{
			return;
		}
		List<TaskDetailItemModel> items = await (from v in App.Connection.Table<TaskDetailItemModel>()
			where v.id == itemId
			select v).ToListAsync();
		if (items != null && items.Count > 1)
		{
			for (int i = 1; i < items.Count; i++)
			{
				await App.Connection.DeleteAsync(items[i]);
			}
		}
	}

	public static async Task<TaskDetailItemModel> CopyItem(string originItemId)
	{
		TaskDetailItemModel item = await GetChecklistItemById(originItemId);
		item.sortOrder += 24L;
		item.id = Utils.GetGuid();
		await InsertChecklistItem(item);
		return item;
	}

	public static async Task<List<TaskDetailItemModel>> GetAllCheckItems()
	{
		string loginUserId = LocalSettings.Settings.LoginUserId;
		return await App.Connection.QueryAsync<TaskDetailItemModel>("select * from TaskDetailItemModel where taskserverid in (select id from taskModel where userid='" + loginUserId + "')", Array.Empty<object>());
	}

	public static void SortItems(List<TaskBaseViewModel> items)
	{
		items.Sort(delegate(TaskBaseViewModel a, TaskBaseViewModel b)
		{
			if (a.Status == 0 && b.Status != 0)
			{
				return -1;
			}
			if (a.Status != 0 && b.Status == 0)
			{
				return 1;
			}
			return (a.Status != 0 && b.Status != 0 && a.CompletedTime.HasValue && b.CompletedTime.HasValue) ? a.CompletedTime.Value.CompareTo(b.CompletedTime.Value) : a.SortOrder.CompareTo(b.SortOrder);
		});
	}
}
