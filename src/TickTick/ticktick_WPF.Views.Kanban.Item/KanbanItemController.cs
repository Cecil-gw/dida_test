using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ticktick_WPF.Util;
using ticktick_WPF.Util.DragAndDrop;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Detail;
using ticktick_WPF.Views.TaskList;

namespace ticktick_WPF.Views.Kanban.Item;

public class KanbanItemController : DisplayItemController
{
	public KanbanItemController(UIElement element, DisplayItemModel model)
		: base(element, model)
	{
	}

	public void OnBatchSelectMouseUp()
	{
		DisplayItemModel model = Model;
		if (model != null && GetKanban() != null && !TickDragHelper.Dragging && model.Enable)
		{
			if (Utils.IfShiftPressed())
			{
				GetTaskList().BatchShiftSelected(model.TaskId);
			}
			else if (Utils.IfCtrlPressed())
			{
				GetTaskList().BatchCtrlSelected(model.TaskId);
			}
		}
	}

	public async Task<TaskDetailPopup> ShowDetailWindow()
	{
		DisplayItemModel model = Model;
		if (model == null)
		{
			return null;
		}
		IKanban kanban = GetKanban();
		if (kanban == null || kanban.IsInDragging())
		{
			return null;
		}
		return await SelectKanbanItem(model);
	}

	public async Task<TaskDetailPopup> SelectKanbanItem(DisplayItemModel model, bool focusTitle = false, bool show = true)
	{
		GetTaskListView()?.SetSelected(new List<string> { model.TaskId });
		TaskDetailPopup taskDetailPopup = await ShowTaskDetail(focusTitle);
		model.Selected = taskDetailPopup != null;
		return taskDetailPopup;
	}

	public override void ReloadOnDetailClosed()
	{
		if (GetKanban() == null || Utils.IfShiftPressed() || Utils.IfCtrlPressed())
		{
			return;
		}
		DisplayItemModel model = Model;
		if (model != null)
		{
			model.Selected = false;
			IKanbanColumn column = GetColumn();
			if (column != null && column.TaskChanged)
			{
				column.TaskChanged = false;
				column.ReloadColumn();
				SyncManager.TryDelaySync();
			}
		}
	}

	public void OnKanbanItemDrag(MouseEventArgs e)
	{
		try
		{
			TaskDetailPopup.TryCloseWindow();
			DoColumnItemDrag(e);
		}
		catch (Exception)
		{
		}
	}

	protected override void OnEventArchived()
	{
		GetKanban()?.Reload(forceReload: true);
	}

	private async void DoColumnItemDrag(MouseEventArgs e)
	{
		if (Model == null || Element == null)
		{
			return;
		}
		DisplayItemModel model = Model;
		if (GetKanban() is KanbanContainer kanban && kanban.IsEnable())
		{
			ITaskList taskList = GetTaskList();
			if (taskList != null)
			{
				taskList?.ResetDrag();
				string text = GetColumn()?.GetColumnId();
				kanban.SetStartDragColumnId(text);
				bool showBm = model.ShowBottomMargin;
				model.ShowBottomMargin = true;
				await taskList.StartDrag(model, e.GetPosition(Element), text);
				model.ShowBottomMargin = showBm;
				kanban.SetStartDragColumnId(null);
				await Task.Delay(300);
				kanban.Reload(forceReload: true);
			}
		}
	}

	private IKanban GetKanban()
	{
		return Utils.FindParent<IKanban>(Element);
	}

	private IKanbanColumn GetColumn()
	{
		return Utils.FindParent<IKanbanColumn>(Element);
	}
}
