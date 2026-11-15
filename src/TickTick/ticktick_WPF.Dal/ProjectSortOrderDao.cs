using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ticktick_WPF.Cache;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Sync.Model;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Dal;

public static class ProjectSortOrderDao
{
	public static long GetSiblingSortOrderInProject(string projectId, string taskId, bool previous)
	{
		List<TaskBaseViewModel> list = TaskCache.GetTasksInProject(projectId)?.ToList();
		if (list != null && list.Any())
		{
			List<TaskBaseViewModel> list2 = list.OrderBy((TaskBaseViewModel t) => t.SortOrder).ToList();
			TaskBaseViewModel taskBaseViewModel = list2.FirstOrDefault((TaskBaseViewModel t) => t.Id == taskId);
			if (taskBaseViewModel != null)
			{
				int num = list2.IndexOf(taskBaseViewModel);
				if (previous)
				{
					if (num <= 0)
					{
						return taskBaseViewModel.SortOrder - 268435456;
					}
					return (taskBaseViewModel.SortOrder + list2[num - 1].SortOrder) / 2;
				}
				if (num >= list2.Count - 1)
				{
					return taskBaseViewModel.SortOrder + 268435456;
				}
				return (taskBaseViewModel.SortOrder + list2[num + 1].SortOrder) / 2;
			}
		}
		return 0L;
	}

	public static long GetNextTaskSortOrderInProject(string projectId, long baseOrder, string parentId = null, bool top = false)
	{
		List<long> taskSortOrdersInProject = TaskCache.GetTaskSortOrdersInProject(projectId, parentId);
		if (taskSortOrdersInProject == null || taskSortOrdersInProject.Count == 0)
		{
			return 0L;
		}
		if (top)
		{
			taskSortOrdersInProject = taskSortOrdersInProject.OrderByDescending((long i) => i).ToList();
			long num = taskSortOrdersInProject.FirstOrDefault((long s) => s < baseOrder);
			if (num < baseOrder)
			{
				return num + (baseOrder - num) / 2;
			}
			return baseOrder - 268435456;
		}
		taskSortOrdersInProject.Sort();
		long num2 = taskSortOrdersInProject.FirstOrDefault((long s) => s > baseOrder);
		if (num2 > baseOrder)
		{
			return baseOrder + (num2 - baseOrder) / 2;
		}
		return baseOrder + 268435456;
	}

	public static long GetNewTaskSortOrderInProject(string projectId, string parentId = null, bool? isTop = null)
	{
		TaskDefaultModel defaultSafely = TaskDefaultDao.GetDefaultSafely();
		List<long> taskSortOrdersInProject = TaskCache.GetTaskSortOrdersInProject(projectId, parentId);
		if (taskSortOrdersInProject == null || taskSortOrdersInProject.Count == 0)
		{
			return 0L;
		}
		if (isTop ?? (defaultSafely.AddTo == 0))
		{
			long num = taskSortOrdersInProject.Min() - 268435456;
			if (num == 0L)
			{
				num = -1L;
			}
			return num;
		}
		long num2 = taskSortOrdersInProject.Max() + 268435456;
		if (num2 == 0L)
		{
			num2 = 1L;
		}
		return num2;
	}

	public static async Task<List<TaskSortOrderInProjectModel>> GetProjectSortOrdersInProjects(List<string> projects)
	{
		return await (from order in App.Connection.Table<TaskSortOrderInProjectModel>()
			where order.Deleted == 0 && projects.Contains(order.ProjectId) && order.UserId == LocalSettings.Settings.LoginUserId
			select order).ToListAsync();
	}

	public static async Task<List<TaskSortOrderInProjectModel>> GetProjectSortOrdersInIdentity()
	{
		return await (from order in App.Connection.Table<TaskSortOrderInProjectModel>()
			where order.Deleted == 0 && order.UserId == LocalSettings.Settings.LoginUserId
			select order).ToListAsync();
	}

	private static async Task<List<TaskSortOrderInProjectModel>> GetSortOrdersInProject(string projectId)
	{
		return await (from order in App.Connection.Table<TaskSortOrderInProjectModel>()
			where order.ProjectId == projectId && order.UserId == LocalSettings.Settings.LoginUserId
			orderby order.SortOrder
			select order).ToListAsync();
	}

	public static async Task<List<TaskSortOrderInProjectModel>> GetLocalSortOrders()
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		return await (from v in App.Connection.Table<TaskSortOrderInProjectModel>()
			where v.UserId == userId && v.SyncStatus == 2 && v.Deleted == 0
			select v).ToListAsync();
	}

	public static async Task SaveRemoteChangesToDb(SyncDataBean<TaskSortOrderInProjectModel> bean)
	{
		if (bean.Addeds.Any())
		{
			await App.Connection.InsertAllAsync(bean.Addeds);
		}
		if (bean.Updateds.Any())
		{
			await App.Connection.UpdateAllAsync(bean.Updateds);
		}
	}

	public static async Task DeleteRemoteDeleted(List<string> projects)
	{
		if (!projects.Any())
		{
			return;
		}
		foreach (string project in projects)
		{
			List<TaskSortOrderInProjectModel> list = await GetSortOrdersInProject(project);
			if (list == null || !list.Any())
			{
				continue;
			}
			foreach (TaskSortOrderInProjectModel item in list)
			{
				if (item.SyncStatus != 0)
				{
					await App.Connection.DeleteAsync(item);
				}
			}
		}
	}

	public static async Task<List<TaskSortOrderInProjectModel>> GetNeedPostSortOrdersInProject()
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		return await (from v in App.Connection.Table<TaskSortOrderInProjectModel>()
			where v.UserId == userId && v.SyncStatus != 2
			select v).ToListAsync();
	}

	public static async Task<Dictionary<string, Dictionary<string, List<TaskSortOrderInProjectModel>>>> GetNeedPostSortOrderInProjectMap()
	{
		Dictionary<string, Dictionary<string, List<TaskSortOrderInProjectModel>>> result = new Dictionary<string, Dictionary<string, List<TaskSortOrderInProjectModel>>>();
		List<TaskSortOrderInProjectModel> list = await GetNeedPostSortOrdersInProject();
		if (list != null && list.Any())
		{
			foreach (TaskSortOrderInProjectModel item in list)
			{
				string projectId = item.ProjectId;
				string key = "all";
				if (!result.ContainsKey(projectId))
				{
					result.Add(projectId, new Dictionary<string, List<TaskSortOrderInProjectModel>>());
				}
				if (!result[projectId].ContainsKey(key))
				{
					result[projectId].Add(key, new List<TaskSortOrderInProjectModel>());
				}
				result[projectId][key].Add(item);
			}
		}
		return result;
	}

	public static async Task SavePostResult(SyncDataBean<TaskSortOrderInProjectModel> projectBean)
	{
		if (projectBean != null)
		{
			if (projectBean.Deleteds != null && projectBean.Deleteds.Count > 0)
			{
				foreach (TaskSortOrderInProjectModel deleted in projectBean.Deleteds)
				{
					await DeleteTaskSortOrder(deleted);
				}
			}
			if (projectBean.Updateds != null && projectBean.Updateds.Count > 0)
			{
				foreach (TaskSortOrderInProjectModel updated in projectBean.Updateds)
				{
					updated.SyncStatus = 2;
				}
				await App.Connection.UpdateAllAsync(projectBean.Updateds);
			}
		}
		ClearDeletedOrders();
	}

	private static async void ClearDeletedOrders()
	{
		List<TaskSortOrderInProjectModel> list = await (from v in App.Connection.Table<TaskSortOrderInProjectModel>()
			where v.Deleted != 0 && v.SyncStatus == 2
			select v).ToListAsync();
		if (list == null || list.Count <= 0)
		{
			return;
		}
		foreach (TaskSortOrderInProjectModel item in list)
		{
			await App.Connection.DeleteAsync(item);
		}
	}

	private static async Task DeleteTaskSortOrder(TaskSortOrderInProjectModel item)
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		List<TaskSortOrderInProjectModel> list = await (from v in App.Connection.Table<TaskSortOrderInProjectModel>()
			where v.ProjectId == item.ProjectId && v.EntityId == item.EntityId && v.UserId == userId
			select v).ToListAsync();
		if (list.Count <= 0)
		{
			return;
		}
		foreach (TaskSortOrderInProjectModel item2 in list)
		{
			await App.Connection.DeleteAsync(item2);
		}
	}

	public static async Task DeleteAllOrders()
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		List<TaskSortOrderInProjectModel> list = await (from order in App.Connection.Table<TaskSortOrderInProjectModel>()
			where order.UserId == userId
			select order).ToListAsync();
		if (list != null && list.Any())
		{
			list.ForEach(delegate(TaskSortOrderInProjectModel order)
			{
				order.SyncStatus = 1;
				order.Deleted = 1;
			});
			await App.Connection.UpdateAllAsync(list);
		}
	}

	public static async Task<List<TaskSortOrderInProjectModel>> BatchResetSortOrder(string projectId, List<string> ids)
	{
		List<TaskSortOrderInProjectModel> result = new List<TaskSortOrderInProjectModel>();
		if (ids == null)
		{
			return result;
		}
		int first = 0;
		long second = 268435456L * (long)ids.Count;
		for (int i = 0; i < ids.Count; i++)
		{
			string id = ids[i];
			TaskSortOrderInProjectModel item = await (from model in App.Connection.Table<TaskSortOrderInProjectModel>()
				where model.ProjectId == projectId && id == model.EntityId
				select model).FirstOrDefaultAsync();
			if (item != null)
			{
				item.SortOrder = Math.Min(first, second) + Math.Abs(first - second) * i / ids.Count;
				item.SyncStatus = 1;
				await App.Connection.UpdateAsync(item);
			}
			else
			{
				item = new TaskSortOrderInProjectModel
				{
					EntityId = id,
					ProjectId = projectId,
					SortOrder = Math.Min(first, second) + Math.Abs(first - second) * i / ids.Count,
					EntityType = "task",
					SyncStatus = 0,
					UserId = LocalSettings.Settings.LoginUserId
				};
				await App.Connection.InsertAsync(item);
			}
			result.Add(item);
		}
		return result;
	}

	public static async Task UpdateSortOrderInProject(string entityId, long sortOrder, string projectId, string type = "task")
	{
		TaskSortOrderInProjectModel taskSortOrderInProjectModel = await (from m in App.Connection.Table<TaskSortOrderInProjectModel>()
			where m.ProjectId == projectId && entityId == m.EntityId
			select m).FirstOrDefaultAsync();
		if (taskSortOrderInProjectModel != null)
		{
			taskSortOrderInProjectModel.SortOrder = sortOrder;
			taskSortOrderInProjectModel.SyncStatus = 1;
			await App.Connection.UpdateAsync(taskSortOrderInProjectModel);
			return;
		}
		TaskSortOrderInProjectModel item = new TaskSortOrderInProjectModel
		{
			EntityId = entityId,
			ProjectId = projectId,
			SortOrder = sortOrder,
			EntityType = type,
			SyncStatus = 0,
			UserId = LocalSettings.Settings.LoginUserId
		};
		await App.Connection.InsertAsync(item);
	}

	public static async Task NewTaskAdded(TaskModel task)
	{
		if (task == null)
		{
			return;
		}
		List<TaskSortOrderInProjectModel> list = await GetSortOrdersInProject(task.projectId);
		if (list == null || list.Count == 0)
		{
			return;
		}
		TaskDefaultModel defaultSafely = TaskDefaultDao.GetDefaultSafely();
		TaskSortOrderInProjectModel taskSortOrderInProjectModel = new TaskSortOrderInProjectModel
		{
			EntityId = task.id,
			ProjectId = task.projectId,
			EntityType = "task",
			SyncStatus = 0,
			UserId = LocalSettings.Settings.LoginUserId
		};
		if (defaultSafely.AddTo == 0)
		{
			long val = list.Select((TaskSortOrderInProjectModel o) => o.SortOrder).Min();
			val = Math.Min(0L, val);
			taskSortOrderInProjectModel.SortOrder = val - 268435456;
		}
		else
		{
			long val2 = list.Select((TaskSortOrderInProjectModel o) => o.SortOrder).Max();
			val2 = Math.Max(0L, val2);
			taskSortOrderInProjectModel.SortOrder = val2 + 268435456;
		}
		await App.Connection.InsertAsync(taskSortOrderInProjectModel);
	}

	public static async Task TryAddSplitTaskOrder(string projectId, string baseTaskId, string insertTaskId, bool below)
	{
		List<TaskSortOrderInProjectModel> list = await GetSortOrdersInProject(projectId);
		if (list == null || list.Count == 0)
		{
			return;
		}
		List<TaskSortOrderInProjectModel> list2 = list.OrderBy((TaskSortOrderInProjectModel o) => o.SortOrder).ToList();
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
			TaskSortOrderInProjectModel item = new TaskSortOrderInProjectModel
			{
				EntityId = insertTaskId,
				ProjectId = projectId,
				EntityType = "task",
				SyncStatus = 0,
				UserId = LocalSettings.Settings.LoginUserId,
				SortOrder = sortOrder
			};
			await App.Connection.InsertAsync(item);
		}
	}
}
