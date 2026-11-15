using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SQLite;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Sync.Model;

namespace ticktick_WPF.Dal;

public static class TaskSortOrderInPriorityDao
{
	public static async Task UpdateAsync(TaskSortOrderInPriorityModel model)
	{
		await App.Connection.UpdateAsync(model);
	}

	public static async Task InsertAsync(TaskSortOrderInPriorityModel model)
	{
		await App.Connection.InsertAsync(model);
	}

	public static async Task DeleteAsync(TaskSortOrderInPriorityModel model)
	{
		await DeleteTaskSortOrder(model);
	}

	public static async Task<List<TaskSortOrderInPriorityModel>> GetSortOrders(string catId = null, int? priority = null, string entityId = null, bool containDelete = false)
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		AsyncTableQuery<TaskSortOrderInPriorityModel> asyncTableQuery = from order in App.Connection.Table<TaskSortOrderInPriorityModel>()
			where order.UserId == userId
			select order;
		if (!string.IsNullOrEmpty(catId))
		{
			asyncTableQuery = asyncTableQuery.Where((TaskSortOrderInPriorityModel order) => order.CatId == catId);
		}
		if (priority.HasValue)
		{
			asyncTableQuery = asyncTableQuery.Where((TaskSortOrderInPriorityModel order) => (int?)order.Priority == priority);
		}
		if (!containDelete)
		{
			asyncTableQuery = asyncTableQuery.Where((TaskSortOrderInPriorityModel order) => order.Deleted == 0);
		}
		if (!string.IsNullOrEmpty(entityId))
		{
			asyncTableQuery = asyncTableQuery.Where((TaskSortOrderInPriorityModel order) => order.EntityId == entityId);
		}
		return await asyncTableQuery.ToListAsync();
	}

	public static async Task SaveRemoteChanges(SyncDataBean<TaskSortOrderInPriorityModel> bean)
	{
		if (bean.Addeds.Any())
		{
			await App.Connection.InsertAllAsync(bean.Addeds);
		}
		if (bean.Updateds.Any())
		{
			await App.Connection.UpdateAllAsync(bean.Updateds);
		}
		if (!bean.Deleteds.Any())
		{
			return;
		}
		foreach (TaskSortOrderInPriorityModel deleted in bean.Deleteds)
		{
			App.Connection.DeleteAsync(deleted);
		}
	}

	private static async Task<int> DeleteTaskSortOrder(TaskSortOrderInPriorityModel item)
	{
		Utils.GetCurrentUserIdInt().ToString();
		return await App.Connection.ExecuteAsync("DELETE FROM TaskSortOrderInPriorityModel WHERE CatId=? and EntityId=? and UserId=? and Priority=?", item.CatId, item.EntityId, item.UserId, item.Priority);
	}

	public static async Task SavePostResult(SyncDataBean<TaskSortOrderInPriorityModel> dateBean)
	{
		if (dateBean != null)
		{
			if (dateBean.Deleteds != null && dateBean.Deleteds.Count > 0)
			{
				foreach (TaskSortOrderInPriorityModel deleted in dateBean.Deleteds)
				{
					await DeleteAsync(deleted);
				}
			}
			if (dateBean.Updateds != null && dateBean.Updateds.Count > 0)
			{
				foreach (TaskSortOrderInPriorityModel updated in dateBean.Updateds)
				{
					updated.SyncStatus = 2;
				}
				await App.Connection.UpdateAllAsync(dateBean.Updateds);
			}
		}
		await ClearDeletedOrders();
	}

	private static async Task<int> ClearDeletedOrders()
	{
		return await App.Connection.ExecuteAsync("DELETE FROM TaskSortOrderInPriorityModel WHERE Deleted!=? and SyncStatus=?", 0, 2);
	}

	public static async Task<List<TaskSortOrderInPriorityModel>> GetAllSortOrders()
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		return await (from v in App.Connection.Table<TaskSortOrderInPriorityModel>()
			where v.UserId == userId && v.SyncStatus == 2 && v.Deleted == 0
			select v).ToListAsync();
	}

	public static async Task<Dictionary<string, Dictionary<string, List<TaskSortOrderInPriorityModel>>>> GetNeedPostOrdersMap()
	{
		Dictionary<string, Dictionary<string, List<TaskSortOrderInPriorityModel>>> result = new Dictionary<string, Dictionary<string, List<TaskSortOrderInPriorityModel>>>();
		List<TaskSortOrderInPriorityModel> list = await GetNeedPostSortOrders();
		if (list != null && list.Any())
		{
			foreach (TaskSortOrderInPriorityModel item in list)
			{
				string key = item.Priority.ToString();
				string catId = item.CatId;
				if (!result.ContainsKey(key))
				{
					result.Add(key, new Dictionary<string, List<TaskSortOrderInPriorityModel>>());
				}
				if (!result[key].ContainsKey(catId))
				{
					result[key].Add(catId, new List<TaskSortOrderInPriorityModel>());
				}
				result[key][catId].Add(item);
			}
		}
		return result;
	}

	public static async Task<List<TaskSortOrderInPriorityModel>> GetNeedPostSortOrders()
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		return await (from v in App.Connection.Table<TaskSortOrderInPriorityModel>()
			where v.UserId == userId && v.SyncStatus != 2
			select v).ToListAsync();
	}

	public static async Task UpdateAsyncByCategoryIdSetToDelete(string categoryId)
	{
		List<TaskSortOrderInPriorityModel> list = await GetSortOrders(categoryId);
		if (list != null && list.Any())
		{
			list.ForEach(delegate(TaskSortOrderInPriorityModel order)
			{
				order.Deleted = 1;
				order.SyncStatus = 1;
			});
			await App.Connection.UpdateAllAsync(list);
		}
	}

	public static async Task BatchUpdateAffectedTaskOnTagChanged(string original, string newLabel)
	{
		if (!(original != newLabel.ToLower()))
		{
			return;
		}
		List<TaskSortOrderInPriorityModel> orders = await GetSortOrders("#" + original);
		if (orders == null || !orders.Any())
		{
			return;
		}
		List<TaskSortOrderInPriorityModel> list = new List<TaskSortOrderInPriorityModel>();
		foreach (TaskSortOrderInPriorityModel item in orders)
		{
			list.Add(new TaskSortOrderInPriorityModel
			{
				UserId = item.UserId,
				CatId = "#" + newLabel.ToLower(),
				EntityId = item.EntityId,
				EntityType = item.EntityType,
				Priority = item.Priority,
				SortOrder = item.SortOrder,
				SyncStatus = 0,
				Deleted = item.Deleted
			});
		}
		await App.Connection.InsertAllAsync(list);
		orders.ForEach(delegate(TaskSortOrderInPriorityModel order)
		{
			order.Deleted = 1;
			order.SyncStatus = 1;
		});
		await App.Connection.UpdateAllAsync(orders);
	}

	public static async Task DeleteRemoteDeleted(List<string> projects)
	{
		if (!projects.Any())
		{
			return;
		}
		foreach (string project in projects)
		{
			List<TaskSortOrderInPriorityModel> list = await GetSortOrders(project);
			if (list == null || !list.Any())
			{
				continue;
			}
			foreach (TaskSortOrderInPriorityModel item in list)
			{
				await DeleteAsync(item);
			}
		}
	}

	public static async Task SyncOnEventCalendarChanged(string originalId, string revisedId)
	{
		await App.Connection.ExecuteAsync("UPDATE $TaskSortOrderInPriorityModel SET EntityId=?, SyncStatus=? WHERE EntityId=?", revisedId, 1, originalId);
	}

	public static async Task NewTaskAdded(TaskModel task, string catId)
	{
		if (task == null)
		{
			return;
		}
		List<TaskSortOrderInPriorityModel> list = await GetSortOrders(catId, task.priority);
		if (list == null || list.Count == 0)
		{
			return;
		}
		TaskDefaultModel defaultSafely = TaskDefaultDao.GetDefaultSafely();
		TaskSortOrderInPriorityModel taskSortOrderInPriorityModel = new TaskSortOrderInPriorityModel
		{
			EntityId = task.id,
			EntityType = "task",
			Priority = task.priority,
			CatId = catId,
			SyncStatus = 0,
			UserId = LocalSettings.Settings.LoginUserId
		};
		if (defaultSafely.AddTo == 0)
		{
			long val = list.Select((TaskSortOrderInPriorityModel o) => o.SortOrder).Min();
			val = Math.Min(0L, val);
			taskSortOrderInPriorityModel.SortOrder = val - 268435456;
		}
		else
		{
			long val2 = list.Select((TaskSortOrderInPriorityModel o) => o.SortOrder).Max();
			val2 = Math.Max(0L, val2);
			taskSortOrderInPriorityModel.SortOrder = val2 + 268435456;
		}
		await App.Connection.InsertAsync(taskSortOrderInPriorityModel);
	}

	public static async Task TryAddSplitTaskOrder(string projectId, int priority, string baseTaskId, string insertTaskId, bool below)
	{
		List<TaskSortOrderInPriorityModel> list = await GetSortOrders(projectId, priority);
		if (list == null || list.Count == 0)
		{
			return;
		}
		List<TaskSortOrderInPriorityModel> list2 = list.OrderBy((TaskSortOrderInPriorityModel o) => o.SortOrder).ToList();
		int num = -1;
		for (int num2 = 0; num2 < list2.Count; num2++)
		{
			if (list2[num2].EntityId == baseTaskId)
			{
				num = num2;
				break;
			}
		}
		if (num >= 0)
		{
			long sortOrder;
			if (num == 0 && !below)
			{
				sortOrder = list2[0].SortOrder - 268435456;
			}
			else if (num == list.Count - 1 && below)
			{
				sortOrder = list2[list.Count - 1].SortOrder + 268435456;
			}
			else
			{
				long sortOrder2 = list2[num].SortOrder;
				long sortOrder3 = list2[below ? (num + 1) : (num - 1)].SortOrder;
				sortOrder = MathUtil.LongAvg(sortOrder2, sortOrder3);
			}
			TaskSortOrderInPriorityModel item = new TaskSortOrderInPriorityModel
			{
				EntityId = insertTaskId,
				EntityType = "task",
				Priority = priority,
				CatId = projectId,
				SyncStatus = 0,
				UserId = LocalSettings.Settings.LoginUserId,
				SortOrder = sortOrder
			};
			await App.Connection.InsertAsync(item);
		}
	}
}
