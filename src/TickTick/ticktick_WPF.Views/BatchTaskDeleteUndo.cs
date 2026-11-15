using System.Collections.Generic;
using KotlinModels;
using ticktick_WPF.Dal;
using ticktick_WPF.Notifier;
using ticktick_WPF.Service;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views;

public class BatchTaskDeleteUndo : UndoController
{
	private readonly List<string> _taskIds;

	private readonly List<string> _deleteForeverTaskIds;

	private readonly List<TaskDeleteRecurrenceUndoEntity> _deleteRecurrenceUndo;

	public BatchTaskDeleteUndo(List<string> taskIds, List<string> deleteForeverTaskIds = null, List<TaskDeleteRecurrenceUndoEntity> deleteRecurrenceUndo = null)
	{
		_taskIds = taskIds;
		_deleteForeverTaskIds = deleteForeverTaskIds;
		UndoHelper.DeletingIds.AddRange(taskIds);
		TaskChangeNotifier.NotifyTaskBatchDeletedChanged(taskIds, needSync: false);
		_deleteRecurrenceUndo = deleteRecurrenceUndo;
		if (deleteForeverTaskIds != null)
		{
			UndoHelper.DeletingIds.AddRange(_deleteForeverTaskIds);
			TaskChangeNotifier.NotifyTaskBatchDeletedChanged(_deleteForeverTaskIds, needSync: false);
		}
	}

	public override string GetTitle()
	{
		return _taskIds.Count + " " + Utils.GetString("CountTasks");
	}

	public override string GetContent()
	{
		return Utils.GetString("Deleted");
	}

	public override async void Undo()
	{
		await TaskService.UndoBatchDeletedTasks(_taskIds);
		UndoHelper.DeletingIds.RemoveAll((string id) => _taskIds.Contains(id));
		if (_deleteForeverTaskIds != null)
		{
			await TaskService.UndoBatchDeletedTasks(_deleteForeverTaskIds);
			UndoHelper.DeletingIds.RemoveAll((string id) => _deleteForeverTaskIds.Contains(id));
		}
		List<TaskDeleteRecurrenceUndoEntity> deleteRecurrenceUndo = _deleteRecurrenceUndo;
		if (deleteRecurrenceUndo == null || deleteRecurrenceUndo.Count <= 0)
		{
			return;
		}
		foreach (TaskDeleteRecurrenceUndoEntity undo in _deleteRecurrenceUndo)
		{
			await TaskService.UndoDeleteTaskRecurrence(undo);
			UndoHelper.DeletingIds.Remove(undo.sid);
		}
	}

	public override async void Finished()
	{
		await SyncStatusDao.BatchAddDeleteSyncStatus(_taskIds);
		UndoHelper.DeletingIds.RemoveAll((string id) => _taskIds.Contains(id));
		if (_deleteForeverTaskIds != null)
		{
			await SyncStatusDao.BatchAddDeleteForeverSyncStatus(_deleteForeverTaskIds);
			UndoHelper.DeletingIds.RemoveAll((string id) => _deleteForeverTaskIds.Contains(id));
		}
		List<TaskDeleteRecurrenceUndoEntity> deleteRecurrenceUndo = _deleteRecurrenceUndo;
		if (deleteRecurrenceUndo != null && deleteRecurrenceUndo.Count > 0)
		{
			foreach (TaskDeleteRecurrenceUndoEntity item in _deleteRecurrenceUndo)
			{
				UndoHelper.DeletingIds.Remove(item.sid);
			}
		}
		SyncManager.Sync();
	}
}
