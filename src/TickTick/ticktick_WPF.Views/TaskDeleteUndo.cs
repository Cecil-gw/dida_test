using System.Collections.Generic;
using KotlinModels;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Notifier;
using ticktick_WPF.Service;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views;

public class TaskDeleteUndo : UndoController
{
	private readonly string _taskId;

	private readonly string _title;

	private readonly bool _isEmptyDelete;

	private readonly bool _isNote;

	private readonly List<TaskDeleteRecurrenceUndoEntity> _deleteRecurrenceUndo;

	public TaskDeleteUndo(string taskId, string title, bool isEmptyDelete = false, List<TaskDeleteRecurrenceUndoEntity> deleteRecurrenceUndo = null)
	{
		_taskId = taskId;
		_isNote = TaskCache.GetTaskById(taskId)?.Kind == "NOTE";
		_title = title;
		_isEmptyDelete = isEmptyDelete;
		UndoHelper.DeletingIds.Add(taskId);
		if (deleteRecurrenceUndo != null && deleteRecurrenceUndo.Count > 0)
		{
			foreach (TaskDeleteRecurrenceUndoEntity item in deleteRecurrenceUndo)
			{
				UndoHelper.DeletingIds.Add(item.sid);
			}
		}
		_deleteRecurrenceUndo = deleteRecurrenceUndo;
		TaskChangeNotifier.NotifyTaskDeleted(taskId);
	}

	public override string GetTitle()
	{
		return _title;
	}

	public override string GetContent()
	{
		return Utils.GetString(_isNote ? "NoteDeleted" : "TaskDeleted1");
	}

	public override async void Undo()
	{
		await TaskService.UndoDeletedTask(_taskId);
		UndoHelper.DeletingIds.Remove(_taskId);
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
		if (!_isEmptyDelete)
		{
			await TaskService.DeleteTask(_taskId);
		}
		else
		{
			await SyncStatusDao.BatchAddDeleteForeverSyncStatus(new List<string> { _taskId });
		}
		List<TaskDeleteRecurrenceUndoEntity> deleteRecurrenceUndo = _deleteRecurrenceUndo;
		if (deleteRecurrenceUndo != null && deleteRecurrenceUndo.Count > 0)
		{
			foreach (TaskDeleteRecurrenceUndoEntity item in _deleteRecurrenceUndo)
			{
				UndoHelper.DeletingIds.Remove(item.sid);
			}
		}
		UndoHelper.DeletingIds.Remove(_taskId);
		SyncManager.TryDelaySync();
	}
}
