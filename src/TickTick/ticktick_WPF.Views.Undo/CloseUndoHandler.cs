using System.Collections.Generic;
using ticktick_WPF.Models;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Undo;

public static class CloseUndoHandler
{
	private static CloseUndoModel _undoModel = new CloseUndoModel();

	public static void AddUndoModel(string taskId, TaskModel task)
	{
		_undoModel.Finished();
		_undoModel = new CloseUndoModel();
		_undoModel.InitModel(taskId, task);
	}

	public static void AddUndoCheckItem(string taskId, TaskDetailItemModel item)
	{
		_undoModel.Finished();
		_undoModel = new CloseUndoModel();
		_undoModel.InitModel(taskId, item);
	}

	public static void AddUndoOriginTasks(string taskId, List<TaskModel> tasks)
	{
		if (_undoModel.UndoId == taskId && !_undoModel.Fired)
		{
			_undoModel.AddOriginTasks(tasks);
		}
	}

	public static void AddUndoOriginTask(string taskId, TaskModel task)
	{
		if (_undoModel.UndoId == taskId && !_undoModel.Fired)
		{
			_undoModel.AddOriginTask(task);
		}
	}

	public static List<string> GetAffectedTaskIds()
	{
		return _undoModel.GetAffectedTaskIds();
	}

	public static void AddUndoNewTasks(string taskId, List<TaskModel> tasks)
	{
		if (_undoModel.UndoId == taskId && !_undoModel.Fired)
		{
			_undoModel.AddNewTasks(tasks);
		}
	}

	public static void TryToast(IToastShowWindow window, int closeStatus)
	{
		if (!_undoModel.Fired)
		{
			CloseUndoToast undo = new CloseUndoToast(_undoModel, closeStatus);
			window.TaskComplete(undo);
		}
	}

	public static bool TryUndoInDetail(string taskId)
	{
		if (_undoModel.UndoId == taskId && !_undoModel.Fired)
		{
			_undoModel.Undo();
			return true;
		}
		return false;
	}

	public static void TryStartTimer()
	{
		if (!_undoModel.Fired)
		{
			_undoModel.StartTimer();
		}
	}
}
