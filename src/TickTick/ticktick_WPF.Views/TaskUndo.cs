using System.Collections.Generic;
using System.Threading.Tasks;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Service;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views;

public class TaskUndo : UndoController
{
	private readonly TaskModel _task;

	private readonly string _title;

	private readonly string _content;

	private readonly List<TaskModel> _children;

	private readonly List<TaskDetailItemModel> _checkItems;

	public TaskUndo(TaskModel task, string title, string content)
	{
		UndoHelper.CanUndoIds.Clear();
		_task = task;
		_title = title;
		_content = content;
		if (task != null)
		{
			UndoHelper.CanUndoIds.Add(task.id);
		}
	}

	public TaskUndo(TaskModel task, string title, string content, List<TaskModel> children, List<TaskDetailItemModel> checkItems)
		: this(task, title, content)
	{
		_children = children;
		_checkItems = checkItems;
		children?.ForEach(delegate(TaskModel c)
		{
			if (c != null)
			{
				UndoHelper.CanUndoIds.Add(c.id);
			}
		});
	}

	public override string GetTitle()
	{
		return _title;
	}

	public override string GetContent()
	{
		return _content;
	}

	public override async void Undo()
	{
		UndoHelper.UndoingIds.AddRange(UndoHelper.CanUndoIds);
		UndoHelper.CanUndoIds.Clear();
		CheckSyncStatus(_task);
		TaskService.UndoTask(_task, _checkItems);
		_children?.ForEach(delegate(TaskModel t)
		{
			CheckSyncStatus(t);
			TaskService.UndoTask(t);
		});
		UndoHelper.UndoingIds.Clear();
		static async Task CheckSyncStatus(TaskModel task)
		{
			if (task != null)
			{
				foreach (SyncStatusModel item in await SyncStatusDao.GetSyncStatusById(task.id))
				{
					int type = item.Type;
					if (type != 2)
					{
						if (type != 16 || !(item.OldParentId == task.parentId))
						{
							continue;
						}
					}
					else if (!(item.MoveFromId == task.projectId))
					{
						continue;
					}
					await SyncStatusDao.DeleteSyncStatus(item.EntityId, item.Type, checkRecommit: false);
				}
			}
		}
	}

	public override void Finished()
	{
		UndoHelper.CanUndoIds.Clear();
		SyncManager.TryDelaySync();
	}
}
