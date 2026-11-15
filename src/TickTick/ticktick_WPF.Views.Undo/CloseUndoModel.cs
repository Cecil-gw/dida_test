using System.Collections.Generic;
using System.Linq;
using System.Timers;
using System.Windows;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Undo;

public class CloseUndoModel : IUndoModel
{
	private List<TaskModel> _originTasks = new List<TaskModel>();

	private List<TaskModel> _newTasks = new List<TaskModel>();

	public string UndoId;

	private TaskDetailItemModel _originItem;

	private Timer _timer;

	private bool _handled;

	public bool Fired { get; set; }

	public void InitModel(string taskId, TaskModel task)
	{
		_originTasks.Clear();
		_originTasks.Add(task);
		UndoId = taskId;
	}

	public void InitModel(string taskId, TaskDetailItemModel item)
	{
		_originTasks.Clear();
		_originItem = item;
		UndoId = taskId;
	}

	public void AddOriginTasks(List<TaskModel> tasks)
	{
		_originTasks.AddRange(tasks);
	}

	public void AddOriginTask(TaskModel task)
	{
		_originTasks.Add(task);
	}

	public void AddNewTasks(List<TaskModel> tasks)
	{
		_newTasks.AddRange(tasks);
	}

	public async void Undo()
	{
		if (Fired)
		{
			return;
		}
		TryFire();
		List<string> newTaskIds = _newTasks.Select((TaskModel t) => t.id).ToList();
		foreach (TaskModel newTask in _newTasks)
		{
			if (!string.IsNullOrEmpty(newTask.parentId))
			{
				if (!newTaskIds.Contains(newTask.parentId))
				{
					await TaskDao.AddOrRemoveTaskChildIds(newTask.parentId, new List<string> { newTask.id }, addChild: false);
				}
				await SyncStatusDao.DeleteSyncStatus(newTask.id, 16, checkRecommit: false);
			}
			if (newTask.kind == "CHECKLIST")
			{
				await TaskDetailItemDao.DeleteCheckItemsByTaskId(newTask.repeatTaskId);
				await ProjectCopyManager.CopyChecklistItems(newTask.id, newTask.repeatTaskId);
				await TaskDetailItemDao.DeleteCheckItemsByTaskId(newTask.id);
			}
			await TaskDao.DeleteTaskInDb(newTask.id);
		}
		if (_originItem != null)
		{
			TaskDetailItemModel taskDetailItemModel = await TaskDetailItemDao.GetCheckItemById(_originItem.id);
			if (taskDetailItemModel != null)
			{
				taskDetailItemModel.status = 0;
				taskDetailItemModel.completedTime = null;
				await TaskDetailItemDao.SaveChecklistItem(taskDetailItemModel);
			}
		}
		List<TaskBaseViewModel> vms = new List<TaskBaseViewModel>();
		foreach (TaskModel newTask in _originTasks)
		{
			TaskModel task = await TaskDao.GetThinTaskById(newTask.id);
			task.status = newTask.status;
			task.completedTime = newTask.completedTime;
			task.startDate = newTask.startDate;
			task.dueDate = newTask.dueDate;
			task.repeatFlag = newTask.repeatFlag;
			task.repeatFrom = newTask.repeatFrom;
			task.repeatFirstDate = newTask.repeatFirstDate;
			if (newTask.parentId != task.parentId)
			{
				await TaskDao.UpdateParent(task.id, newTask.parentId);
				task.parentId = newTask.parentId;
			}
			if (task.id == _originItem?.TaskServerId)
			{
				task.progress = TaskService.CalculateProgress(await TaskDetailItemDao.GetCheckItemsByTaskId(task.id));
			}
			vms.Add(await TaskService.UpdateTaskOnCompleteUndo(task));
		}
		ProjectAndTaskIdsCache.OnTasksChanged(vms, CheckMatchedType.All);
		TasksChangeEventArgs e = new TasksChangeEventArgs();
		e.DeletedChangedIds.AddRange(newTaskIds);
		List<string> models = vms.Select((TaskBaseViewModel v) => v.GetTaskId()).ToList();
		e.DateChangedIds.AddRange(models);
		e.StatusChangedIds.AddRange(models);
		TaskChangeNotifier.NotifyTaskChanged(e);
		_handled = true;
	}

	public async void Finished()
	{
		if (string.IsNullOrEmpty(UndoId) || Fired)
		{
			return;
		}
		TryFire();
		foreach (TaskModel originTask in _originTasks)
		{
			await SyncStatusDao.AddSyncStatus(originTask.id, 0);
		}
		foreach (TaskModel newTask in _newTasks)
		{
			await SyncStatusDao.AddSyncStatus(newTask.id, 4);
		}
		SyncManager.Sync();
		_handled = true;
	}

	private void TryFire()
	{
		Fired = true;
		_timer?.Close();
	}

	public string GetUndoTitle()
	{
		if (_originItem != null)
		{
			return _originItem.title;
		}
		if (_originTasks.Count > 0 && _originTasks.Count == 1)
		{
			return _originTasks[0].title;
		}
		return string.Empty;
	}

	public void StartTimer()
	{
		_timer = new Timer(6000.0);
		_timer.Elapsed += TryDo;
		_timer.Start();
	}

	private void TryDo(object sender, ElapsedEventArgs e)
	{
		Utils.RunOnUiThread(Application.Current?.Dispatcher, Finished);
		_timer?.Stop();
		_timer?.Close();
	}

	public List<string> GetAffectedTaskIds()
	{
		if (!_handled)
		{
			List<string> list = _originTasks.Select((TaskModel t) => t.id).ToList();
			list.AddRange(_newTasks.Select((TaskModel t) => t.id).ToList());
			return list;
		}
		return new List<string>();
	}
}
