using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Cache;

public class TaskDetailItemCache
{
	public static readonly ConcurrentDictionary<string, TaskBaseViewModel> LocalCheckItemViewModels = new ConcurrentDictionary<string, TaskBaseViewModel>();

	public static async Task InitLocalCheckItems(HashSet<string> cleanedTaskIds)
	{
		(await TaskDetailItemDao.GetAllCheckItems()).ForEach(delegate(TaskDetailItemModel item)
		{
			if (cleanedTaskIds == null || !cleanedTaskIds.Contains(item.TaskServerId))
			{
				AddCheckItemToDict(item);
			}
		});
	}

	public static TaskBaseViewModel AddCheckItemToDict(TaskDetailItemModel item)
	{
		if (!string.IsNullOrEmpty(item?.id))
		{
			if (!LocalCheckItemViewModels.ContainsKey(item.id))
			{
				TaskBaseViewModel taskBaseViewModel = new TaskBaseViewModel(item);
				LocalCheckItemViewModels[item.id] = taskBaseViewModel;
				return taskBaseViewModel;
			}
			TaskBaseViewModel taskBaseViewModel2 = LocalCheckItemViewModels[item.id];
			if (taskBaseViewModel2.OwnerTask == null)
			{
				taskBaseViewModel2.SetOwnerTask(item.TaskServerId, setItem: true);
			}
			return taskBaseViewModel2;
		}
		return null;
	}

	public static List<TaskBaseViewModel> GetCheckItemsByTaskId(string taskId)
	{
		return TaskCache.GetTaskById(taskId)?.CheckItems?.ToList();
	}

	public static List<TaskBaseViewModel> GetAllCheckItems()
	{
		return LocalCheckItemViewModels.Values.ToList();
	}

	private static TaskBaseViewModel UpdateItem(TaskDetailItemModel item)
	{
		TaskBaseViewModel taskBaseViewModel;
		if (!string.IsNullOrEmpty(item.id) && LocalCheckItemViewModels.ContainsKey(item.id))
		{
			taskBaseViewModel = LocalCheckItemViewModels[item.id];
			taskBaseViewModel.BuildCheckItem(item);
		}
		else
		{
			taskBaseViewModel = AddCheckItemToDict(item);
		}
		return taskBaseViewModel;
	}

	public static void OnCheckItemUpdated(TaskDetailItemModel item, bool checkCount)
	{
		if (!string.IsNullOrEmpty(item?.id))
		{
			TaskBaseViewModel taskBaseViewModel = UpdateItem(item);
			if (taskBaseViewModel != null && checkCount)
			{
				ProjectAndTaskIdsCache.OnCheckItemsChanged(new List<TaskBaseViewModel> { taskBaseViewModel });
			}
		}
	}

	public static void OnCheckItemsBatchUpdated(List<TaskDetailItemModel> items)
	{
		if (items == null || items.Count <= 0)
		{
			return;
		}
		List<TaskBaseViewModel> list = new List<TaskBaseViewModel>();
		foreach (TaskDetailItemModel item in items.Where((TaskDetailItemModel i) => !string.IsNullOrEmpty(i?.id)))
		{
			TaskBaseViewModel taskBaseViewModel = UpdateItem(item);
			if (taskBaseViewModel != null)
			{
				list.Add(taskBaseViewModel);
			}
		}
		ProjectAndTaskIdsCache.OnCheckItemsChanged(list);
	}

	public static async Task DeleteCheckItemById(string id)
	{
		if (!string.IsNullOrEmpty(id) && LocalCheckItemViewModels.TryRemove(id, out var value))
		{
			value.OwnerTask?.RemoveCheckItem(value);
			ProjectAndTaskIdsCache.OnCheckItemsRemoved(new List<TaskBaseViewModel> { value });
		}
	}

	public static async Task BatchAddCheckItems(List<TaskDetailItemModel> models)
	{
		if (models == null || models.Count <= 0)
		{
			return;
		}
		List<TaskBaseViewModel> list = new List<TaskBaseViewModel>();
		foreach (TaskDetailItemModel model in models)
		{
			TaskBaseViewModel taskBaseViewModel = AddCheckItemToDict(model);
			if (taskBaseViewModel != null)
			{
				list.Add(taskBaseViewModel);
			}
		}
		ProjectAndTaskIdsCache.OnCheckItemsChanged(list);
	}

	public static TaskBaseViewModel GetCheckItemById(string id)
	{
		if (string.IsNullOrEmpty(id) || !LocalCheckItemViewModels.ContainsKey(id))
		{
			return null;
		}
		return LocalCheckItemViewModels[id];
	}

	public static List<TaskBaseViewModel> GetCheckItemsInTaskIds(List<string> taskIds)
	{
		List<TaskBaseViewModel> list = new List<TaskBaseViewModel>();
		if (taskIds == null || !taskIds.Any())
		{
			return list;
		}
		foreach (string taskId in taskIds)
		{
			List<TaskBaseViewModel> checkItemsByTaskId = GetCheckItemsByTaskId(taskId);
			if (checkItemsByTaskId != null)
			{
				list.AddRange(checkItemsByTaskId);
			}
		}
		return list;
	}

	public static bool ExistItem(string modelId)
	{
		if (!string.IsNullOrEmpty(modelId))
		{
			return LocalCheckItemViewModels.ContainsKey(modelId);
		}
		return false;
	}

	public static TaskBaseViewModel GetPrimarySubtaskInTask(string taskId)
	{
		List<TaskBaseViewModel> checkItemsByTaskId = GetCheckItemsByTaskId(taskId);
		if (checkItemsByTaskId != null && checkItemsByTaskId.Any())
		{
			return (from item in checkItemsByTaskId
				orderby item?.Status ?? 2, item?.SortOrder ?? long.MaxValue
				select item).FirstOrDefault();
		}
		return null;
	}

	public static TaskBaseViewModel SafeGetViewModel(TaskDetailItemModel item)
	{
		if (item == null)
		{
			throw new Exception("item is null");
		}
		if (string.IsNullOrEmpty(item.id))
		{
			throw new Exception("itemId is null");
		}
		if (!LocalCheckItemViewModels.ContainsKey(item.id))
		{
			AddCheckItemToDict(item);
		}
		return LocalCheckItemViewModels[item.id];
	}

	public static void OnCheckItemAdded(TaskDetailItemModel model)
	{
		TaskBaseViewModel taskBaseViewModel = AddCheckItemToDict(model);
		if (taskBaseViewModel != null)
		{
			ProjectAndTaskIdsCache.OnCheckItemsChanged(new List<TaskBaseViewModel> { taskBaseViewModel });
		}
	}

	public static List<TaskBaseViewModel> GetCheckItemByIds(List<string> ids)
	{
		List<TaskBaseViewModel> list = new List<TaskBaseViewModel>();
		if (ids != null && ids.Count > 0)
		{
			list.AddRange(from id in ids
				where !string.IsNullOrEmpty(id) && LocalCheckItemViewModels.ContainsKey(id)
				select LocalCheckItemViewModels[id]);
		}
		return list;
	}

	public static void Clear()
	{
		LocalCheckItemViewModels.Clear();
	}

	public static List<TaskBaseViewModel> GetAllOutDateItems()
	{
		return LocalCheckItemViewModels.Values.ToList().Where(delegate(TaskBaseViewModel t)
		{
			TaskBaseViewModel ownerTask = t.OwnerTask;
			if (ownerTask == null || !ownerTask.Editable || ownerTask.Status != 0 || ownerTask.Deleted != 0)
			{
				return false;
			}
			return t.Status == 0 && t.Deleted == 0 && t.OutDate();
		}).ToList();
	}
}
