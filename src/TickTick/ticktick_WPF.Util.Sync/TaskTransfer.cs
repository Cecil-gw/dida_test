using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TickTickModels;
using TickTickUtils.Lang;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Service;
using ticktick_WPF.Util.Sync.Model;
using ticktick_WPF.Views.MarkDown;

namespace ticktick_WPF.Util.Sync;

public static class TaskTransfer
{
	public static async Task<List<SyncTaskBean>> DescribeSyncTaskBean(List<SyncStatusModel> addedTasks, List<SyncStatusModel> updatedTasks, List<SyncStatusModel> deletedTasks, List<SyncStatusModel> deletedForeverTasks)
	{
		List<SyncTaskBean> syncTaskBeans = new List<SyncTaskBean>();
		int num = new List<List<SyncStatusModel>> { addedTasks, updatedTasks, deletedTasks, deletedForeverTasks }.Max((List<SyncStatusModel> list) => list.Count);
		int batchCount = num / 30 + 1;
		List<TaskModel> needPullItemsTasks = new List<TaskModel>();
		for (int i = 0; i < batchCount; i++)
		{
			int start = i * 30;
			int end = (i + 1) * 30;
			SyncTaskBean syncTaskBean = new SyncTaskBean();
			if (start < addedTasks.Count)
			{
				int addEnd = Math.Min(end, addedTasks.Count);
				for (int j = start; j < addEnd; j++)
				{
					SyncStatusModel task = addedTasks[j];
					TaskModel taskModel = await TaskDao.GetFullTaskById(task.EntityId, checkTextCount: true);
					if (taskModel != null)
					{
						HandleLocalDateChanged(taskModel);
						syncTaskBean.Add.Add(PrepareTaskForUpload(taskModel));
					}
					else
					{
						UtilLog.Info("big sync: update task // add not found " + task.EntityId);
						App.Connection.DeleteAsync(task);
					}
				}
			}
			if (start < updatedTasks.Count)
			{
				int addEnd = Math.Min(end, updatedTasks.Count);
				for (int j = start; j < addEnd; j++)
				{
					SyncStatusModel task = updatedTasks[j];
					TaskModel taskModel2 = await TaskDao.GetFullTaskById(task.EntityId, checkTextCount: true);
					if (taskModel2 != null)
					{
						if (taskModel2.kind == "CHECKLIST" && (taskModel2.items == null || taskModel2.items.Length == 0))
						{
							needPullItemsTasks.Add(taskModel2);
							continue;
						}
						HandleLocalDateChanged(taskModel2);
						syncTaskBean.Update.Add(PrepareTaskForUpload(taskModel2));
					}
					else
					{
						UtilLog.Info("big sync: update task // update not found " + task.EntityId);
						App.Connection.DeleteAsync(task);
					}
				}
			}
			if (start < deletedTasks.Count)
			{
				int addEnd = Math.Min(end, deletedTasks.Count);
				for (int j = start; j < addEnd; j++)
				{
					SyncStatusModel task = deletedTasks[j];
					TaskModel taskModel3 = await TaskDao.GetTaskById(task.EntityId);
					if (taskModel3 != null)
					{
						TaskProject item = new TaskProject
						{
							taskId = taskModel3.id,
							projectId = taskModel3.projectId
						};
						syncTaskBean.Delete.Add(item);
					}
					else
					{
						App.Connection.DeleteAsync(task);
					}
				}
			}
			if (start < deletedForeverTasks.Count)
			{
				int addEnd = Math.Min(end, deletedForeverTasks.Count);
				for (int j = start; j < addEnd; j++)
				{
					SyncStatusModel task = deletedForeverTasks[j];
					TaskModel taskModel4 = await TaskDao.GetTaskById(task.EntityId);
					if (taskModel4 != null)
					{
						TaskProject item2 = new TaskProject
						{
							taskId = taskModel4.id,
							projectId = taskModel4.projectId
						};
						syncTaskBean.DeletedFromTrash.Add(item2);
					}
					else
					{
						UtilLog.Info("big sync: update task // deleteForever not found " + task.EntityId);
						App.Connection.DeleteAsync(task);
					}
				}
			}
			syncTaskBeans.Add(syncTaskBean);
			needPullItemsTasks.ForEach(delegate(TaskModel taskModel5)
			{
				TaskService.GetRemoteTaskCheckItems(taskModel5.projectId, taskModel5.id);
			});
		}
		return syncTaskBeans;
	}

	public static TaskModel PrepareTaskForUpload(TaskModel task)
	{
		if (task == null)
		{
			return null;
		}
		TaskModel taskModel = task.Copy();
		taskModel.content = RemoveInlineSemanticMetadata(taskModel.content);
		taskModel.desc = RemoveInlineSemanticMetadata(taskModel.desc);
		return taskModel;
	}

	private static string RemoveInlineSemanticMetadata(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return text;
		}
		string rawText = text;
		MarkdownInlineSemanticMetadata.Extract(ref rawText);
		return rawText;
	}

	private static void HandleLocalDateChanged(TaskModel task)
	{
		if ((task.isAllDay.HasValue && task.isAllDay.Value) || task.Floating)
		{
			if (task.startDate.HasValue)
			{
				DateTime dateTime = DateUtils.ConvertTimeZone(task.startDate.Value, task.timeZone);
				task.startDate = task.startDate.Value.Add(task.startDate.Value - dateTime);
			}
			if (task.dueDate.HasValue)
			{
				DateTime dateTime2 = DateUtils.ConvertTimeZone(task.dueDate.Value, task.timeZone);
				task.dueDate = task.dueDate.Value.Add(task.dueDate.Value - dateTime2);
			}
		}
		if (task.items == null || !task.items.Any())
		{
			return;
		}
		TaskDetailItemModel[] items = task.items;
		foreach (TaskDetailItemModel taskDetailItemModel in items)
		{
			if (((taskDetailItemModel.isAllDay.HasValue && taskDetailItemModel.isAllDay.Value) || task.Floating) && taskDetailItemModel.startDate.HasValue)
			{
				taskDetailItemModel.startDate = TimeZoneUtils.ToLocalTime(taskDetailItemModel.startDate.Value, task.timeZone);
				taskDetailItemModel.serverStartDate = taskDetailItemModel.startDate.Value.ToString(UtcDateTimeConverter.GetConverterValue(taskDetailItemModel.startDate.Value));
			}
		}
	}
}
