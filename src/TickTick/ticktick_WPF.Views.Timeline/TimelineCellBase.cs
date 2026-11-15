using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Calendar;
using ticktick_WPF.Views.Detail;
using ticktick_WPF.Views.MainListView;

namespace ticktick_WPF.Views.Timeline;

public class TimelineCellBase : UserControl, IShowTaskDetailWindow
{
	protected TimelineCellViewModel cellModel;

	private TimelineCellViewModel _detailData;

	private TimelineContainer _container;

	private bool _previewLeftClick;

	private bool _isWaitingDoubleClick;

	private DateTime _lastClickTime;

	protected TimelineViewModel timelineModel => GetContainer()?.GetViewModel() ?? TimelineViewModel.Instance;

	protected TimelineContainer Container => GetContainer();

	public TimelineCellBase()
	{
		base.DataContextChanged += OnDataContextChanged;
	}

	private TimelineContainer GetContainer()
	{
		if (_container == null)
		{
			_container = Utils.FindParent<TimelineContainer>(this);
		}
		return _container;
	}

	private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
	{
		if (e.NewValue is TimelineCellViewModel timelineCellViewModel)
		{
			cellModel = timelineCellViewModel;
			timelineCellViewModel.SetColorAndAvatar();
		}
		else
		{
			cellModel = null;
		}
	}

	protected bool ModelInvalid()
	{
		if (timelineModel != null && cellModel != null)
		{
			return Container == null;
		}
		return true;
	}

	protected void OnCellMouseLeave(object sender, MouseEventArgs e)
	{
		Container?.TimelineFloating?.TryClose(cellModel);
	}

	protected async void OnCellMouseEnter(object sender, MouseEventArgs e)
	{
		if (cellModel?.DisplayModel != null && !(cellModel?.IsNew ?? true))
		{
			Container?.TimelineFloating.Open(cellModel, this);
		}
	}

	public async Task OpenDetailWindow(bool inArrange = false)
	{
		if (!ModelInvalid())
		{
			TimelineCellViewModel timelineCellViewModel = cellModel;
			Container?.ClearCopiedTaskSelection(timelineCellViewModel.Id);
			timelineModel.ClearBatchSelect();
			Utils.FindParent<ListViewContainer>(this)?.AddItemClickEvent();
			if (timelineCellViewModel.DisplayModel.Type == DisplayType.Event)
			{
				ShowEventDetail(timelineCellViewModel);
			}
			else
			{
				TryShowTaskDetail(timelineCellViewModel);
			}
			Container?.TryCloseToolTips();
			Container?.StartIgnoreMouseUp();
		}
	}

	private async void TryShowTaskDetail(TimelineCellViewModel model)
	{
		_isWaitingDoubleClick = false;
		bool num = (DateTime.Now - _lastClickTime).TotalMilliseconds > 300.0;
		_lastClickTime = DateTime.Now;
		if (num)
		{
			_isWaitingDoubleClick = true;
			await ShowTaskDetail(model);
			await Task.Delay(300);
			if (!_isWaitingDoubleClick)
			{
				model.Operation = model.Operation.Remove(TimelineCellOperation.Edit);
				return;
			}
		}
		else
		{
			TaskDetailWindows.ShowTaskWindows(model.Id);
		}
		_previewLeftClick = false;
	}

	protected async Task<TaskDetailPopup> ShowTaskDetail(TimelineCellViewModel model, bool byMouse = true)
	{
		model.Operation = model.Operation.Add(TimelineCellOperation.Edit);
		timelineModel.Editing = true;
		TaskDetailPopup popup = new TaskDetailPopup();
		MainWindow window = App.Window;
		TimelineContainer container = Container;
		if (window != null)
		{
			if (PopupStateManager.LastTarget == this && popup.Detail.TaskId == model.Id)
			{
				model.Operation = model.Operation.Remove(TimelineCellOperation.Edit);
				return null;
			}
			popup.DependentWindow = window;
		}
		popup.Disappear -= OnWindowDisappear;
		popup.Disappear += OnWindowDisappear;
		bool flag = false;
		if (PresentationSource.FromVisual(container) != null)
		{
			Point point = (byMouse ? Mouse.GetPosition(container) : TranslatePoint(new Point(0.0, 20.0), container));
			Point point2 = container.PointToScreen(point);
			flag = await popup.Show(model.DisplayModel.GetTaskId(), (model.DisplayModel.Type == DisplayType.CheckItem) ? model.DisplayModel.Id : null, new TaskWindowDisplayArgs(this, 0.0, point2, 0.0), timelineModel.ProjectIdentity);
			UserActCollectUtils.AddClickEvent("timeline", "task_action", "task_detail");
		}
		if (!flag)
		{
			model.Operation = model.Operation.Remove(TimelineCellOperation.Edit);
			timelineModel.Editing = false;
			return null;
		}
		_detailData = model;
		UndoHelper.NeedToastFilteredTaskId = model.Id;
		return popup;
	}

	private async Task ShowEventDetail(TimelineCellViewModel model)
	{
		string id = model.Id;
		if (!string.IsNullOrEmpty(id))
		{
			model.Operation = model.Operation.Add(TimelineCellOperation.Edit);
			timelineModel.Editing = true;
			CalendarEventModel calendarEventModel = await CalendarEventDao.GetEventById(ArchivedDao.GetOriginalId(id));
			if (calendarEventModel != null)
			{
				calendarEventModel.DueStart = model.StartDate;
				calendarEventModel.DueEnd = model.EndDate;
				CalendarDetailWindow calendarDetailWindow = new CalendarDetailWindow();
				calendarDetailWindow.Disappear -= OnDetailClosed;
				calendarDetailWindow.Disappear += OnDetailClosed;
				Point position = Mouse.GetPosition(Container);
				position.Y = 36.0;
				calendarDetailWindow.Show(this, 40.0, 10.0, byMouse: true, new CalendarDetailViewModel(calendarEventModel));
			}
			else
			{
				model.Operation = model.Operation.Remove(TimelineCellOperation.Edit);
				timelineModel.Editing = false;
			}
		}
	}

	private void OnDetailClosed(object sender, string e)
	{
		if (sender is CalendarDetailWindow calendarDetailWindow)
		{
			calendarDetailWindow.Disappear -= OnDetailClosed;
		}
		TimelineCellViewModel timelineCellViewModel = cellModel;
		if (!ModelInvalid())
		{
			Container?.DelayEndIgnoreMouseUp();
			timelineCellViewModel.Operation = timelineCellViewModel.Operation.Remove(TimelineCellOperation.Edit);
			timelineModel.Editing = false;
		}
	}

	private async void OnWindowDisappear(object sender, string e)
	{
		if (sender is TaskDetailPopup taskDetailPopup)
		{
			taskDetailPopup.Disappear -= OnWindowDisappear;
		}
		TimelineCellViewModel detailData = _detailData;
		_detailData = null;
		if (detailData != null)
		{
			if (detailData.DisplayModel.Deleted == 0 && UndoHelper.NeedToastFilteredTaskId == e && !timelineModel.CheckTaskMatched(e))
			{
				Container?.TryToastString(Utils.GetString("TaskHasBeenFiltered"));
			}
			detailData.Operation = detailData.Operation.Remove(TimelineCellOperation.Edit);
			Container?.DelayEndIgnoreMouseUp();
			if (timelineModel != null)
			{
				timelineModel.UpdateCellTime(detailData, ignoreEdit: false, forceCheck: true);
				await timelineModel.UpdateCellLineAsync();
				timelineModel.Editing = false;
			}
		}
	}

	public void OnDetailKeyUp(Key key)
	{
	}
}
