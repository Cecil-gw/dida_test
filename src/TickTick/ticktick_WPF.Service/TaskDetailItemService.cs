using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Service;

public class TaskDetailItemService
{
	public static async Task SaveModel(string itemId, DateTime? startDate, DateTime? completeDate, int status)
	{
		TaskBaseViewModel viewModel = null;
		if (!string.IsNullOrEmpty(itemId) && TaskDetailItemCache.LocalCheckItemViewModels.ContainsKey(itemId))
		{
			TaskBaseViewModel taskBaseViewModel = TaskDetailItemCache.LocalCheckItemViewModels[itemId];
			if (taskBaseViewModel.IsCheckItem)
			{
				taskBaseViewModel.StartDate = startDate;
				taskBaseViewModel.CompletedTime = completeDate;
				taskBaseViewModel.Status = status;
				viewModel = taskBaseViewModel;
			}
		}
		TaskDetailItemModel taskDetailItemModel = await TaskDetailItemDao.GetCheckItemById(itemId);
		if (taskDetailItemModel != null)
		{
			taskDetailItemModel.startDate = startDate;
			taskDetailItemModel.completedTime = completeDate;
			taskDetailItemModel.status = status;
			TaskDetailItemDao.SaveChecklistItem(taskDetailItemModel);
			if (viewModel == null)
			{
				TaskDetailItemCache.AddCheckItemToDict(taskDetailItemModel);
			}
			ProjectAndTaskIdsCache.OnCheckItemsChanged(new List<TaskBaseViewModel> { viewModel });
		}
	}

	public static async Task<bool> UncheckDetailItemsByTaskId(string taskId)
	{
		List<TaskBaseViewModel> checkItemsByTaskId = TaskDetailItemCache.GetCheckItemsByTaskId(taskId);
		bool changed = false;
		if (checkItemsByTaskId != null && checkItemsByTaskId.Count > 0)
		{
			foreach (TaskBaseViewModel item in checkItemsByTaskId)
			{
				if (item.Status != 0)
				{
					item.Status = 0;
					changed = true;
				}
			}
			ProjectAndTaskIdsCache.OnCheckItemsChanged(checkItemsByTaskId);
		}
		List<TaskDetailItemModel> list = await TaskDetailItemDao.GetCheckItemsByTaskId(taskId);
		if (list != null && list.Count > 0)
		{
			foreach (TaskDetailItemModel item2 in list)
			{
				if (item2.status != 0)
				{
					item2.status = 0;
				}
			}
		}
		await TaskDetailItemDao.BatchUpdateChecklists(list);
		return changed;
	}

	public static async Task CompleteCheckItem(string itemId, bool handleTask = true, bool completeOnly = false, bool needUndo = false, IToastShowWindow window = null)
	{
		TaskDetailItemModel item = await TaskDetailItemDao.GetChecklistItemById(itemId);
		if (item != null && (!completeOnly || item.status != 1))
		{
			item.status = ((item.status != 1) ? 1 : 0);
			item.completedTime = ((item.status == 1) ? new DateTime?(DateTime.Now) : ((DateTime?)null));
			await TaskDetailItemDao.SaveChecklistItem(item);
			await TaskService.SyncTaskChange(item, handleTask, needUndo, window);
			ItemChangeNotifier.NotifyItemStatusChanged(item.id);
		}
	}
}
