using System;
using System.Collections.Generic;
using System.Linq;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Service;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Undo;

public class TaskDragUndoModel : IUndoModel
{
	private class TaskDragModel
	{
		public string TaskId { get; set; }

		public string OldParentId { get; set; }

		public string NewParentId { get; set; }

		public string OldProjectId { get; set; }

		public string NewProjectId { get; set; }

		public string OldAssignee { get; set; }

		public long OldSortOrder { get; set; }
	}

	private string _undoId;

	private List<TaskDragModel> _dragModels = new List<TaskDragModel>();

	private bool _fired;

	public static List<TaskDragUndoModel> DragUndoModels = new List<TaskDragUndoModel>();

	private TaskDragUndoModel()
	{
	}

	private void TryDo()
	{
		Finished();
	}

	public static bool TryUndo(string undoId)
	{
		TaskDragUndoModel taskDragUndoModel = DragUndoModels.FirstOrDefault((TaskDragUndoModel m) => m._undoId == undoId && !m._fired);
		taskDragUndoModel?.Undo();
		return taskDragUndoModel != null;
	}

	public static void AddDragModel(string undoId, string taskId, string oldParentId, string newParentId, string oldProjectId, string newProjectId, string oldAssign, long oldSort)
	{
		TaskDragUndoModel taskDragUndoModel = DragUndoModels.FirstOrDefault((TaskDragUndoModel m) => m._undoId == undoId);
		if (taskDragUndoModel != null && taskDragUndoModel._fired)
		{
			DragUndoModels.Remove(taskDragUndoModel);
			taskDragUndoModel = null;
		}
		if (taskDragUndoModel == null)
		{
			taskDragUndoModel = new TaskDragUndoModel
			{
				_undoId = undoId
			};
			DragUndoModels.Add(taskDragUndoModel);
		}
		TaskDragModel item = new TaskDragModel
		{
			TaskId = taskId,
			OldParentId = oldParentId,
			OldProjectId = oldProjectId,
			NewProjectId = newProjectId,
			NewParentId = newParentId,
			OldAssignee = oldAssign,
			OldSortOrder = oldSort
		};
		taskDragUndoModel._dragModels.Add(item);
	}

	public async void Undo()
	{
		if (_fired)
		{
			return;
		}
		_fired = true;
		DragUndoModels.Remove(this);
		string projectName = "";
		HashSet<string> taskIds = new HashSet<string>();
		foreach (TaskDragModel model in _dragModels)
		{
			TaskModel task = await TaskDao.GetThinTaskById(model.TaskId);
			if (task == null)
			{
				continue;
			}
			taskIds.Add(task.id);
			task.title.Trim();
			if (!string.IsNullOrEmpty(model.OldProjectId) && model.OldProjectId != model.NewProjectId)
			{
				task.projectId = model.OldProjectId;
				task.assignee = model.OldAssignee;
				task.sortOrder = model.OldSortOrder;
				projectName = ((!string.IsNullOrEmpty(projectName)) ? projectName : CacheManager.GetProjects().FirstOrDefault((ProjectModel p) => p.id == model.OldProjectId)?.name);
				CommentDao.ChangeCommentProjectId(task, model.OldParentId);
			}
			if (model.OldParentId != model.NewParentId)
			{
				task.parentId = model.OldParentId;
				if (!string.IsNullOrEmpty(model.NewParentId))
				{
					await TaskDao.AddOrRemoveTaskChildIds(model.NewParentId, new List<string> { model.TaskId }, addChild: false);
				}
			}
			await TaskService.UpdateTaskOnMoveUndo(task, CheckMatchedType.All);
		}
		Utils.Toast(string.Format(arg0: EmojiHelper.StartWithEmoji(projectName) ? projectName : ("\"" + projectName + "\""), format: Utils.GetString("MovedTo")));
	}

	public async void Finished()
	{
		if (_fired)
		{
			return;
		}
		_fired = true;
		DragUndoModels.Remove(this);
		foreach (TaskDragModel model in _dragModels)
		{
			TaskModel task = await TaskDao.GetThinTaskById(model.TaskId);
			if (task == null)
			{
				continue;
			}
			if (!string.IsNullOrEmpty(model.OldProjectId) && model.OldProjectId != model.NewProjectId)
			{
				await SyncStatusDao.AddMoveOrRestoreProjectStatus(task.id, model.OldProjectId);
				await SyncStatusDao.AddModifySyncStatus(task.id);
			}
			if (model.OldParentId != model.NewParentId)
			{
				await SyncStatusDao.AddSetParentSyncStatus(task.id, model.OldParentId);
				if (!string.IsNullOrEmpty(model.OldParentId))
				{
					await TaskDao.AddOrRemoveTaskChildIds(model.OldParentId, new List<string> { model.TaskId }, addChild: false);
				}
			}
		}
		SyncManager.Sync();
	}

	public static bool CheckUndoIdExist(string undoId)
	{
		return DragUndoModels.FirstOrDefault((TaskDragUndoModel m) => m._undoId == undoId && !m._fired) != null;
	}

	public static void TryFinishAll()
	{
		try
		{
			foreach (TaskDragUndoModel dragUndoModel in DragUndoModels)
			{
				dragUndoModel.Finished();
			}
			SyncManager.TryDelaySync();
		}
		catch (Exception)
		{
		}
	}
}
