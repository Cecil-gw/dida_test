using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Emoji.Wpf;
using TickTickUtils;
using TickTickUtils.Lang;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Util.DragAndDrop;
using ticktick_WPF.Util.KotlinUtils;
using ticktick_WPF.Util.Sync;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Calendar.Week;
using ticktick_WPF.Views.Calendar.Year;
using ticktick_WPF.Views.CountDown.ViewModels;
using ticktick_WPF.Views.CountDown.Views;
using ticktick_WPF.Views.Detail;
using ticktick_WPF.Views.Habit;
using ticktick_WPF.Views.Misc;
using ticktick_WPF.Views.Pomo;
using ticktick_WPF.Views.Tag;
using ticktick_WPF.Views.TaskList;
using ticktick_WPF.Views.Widget;

namespace ticktick_WPF.Views.Calendar;

public class TaskBar : Border, IShowTaskDetailWindow
{
	public static readonly DependencyProperty BarMarginProperty = DependencyProperty.Register("BarMargin", typeof(Thickness), typeof(TaskBar), new PropertyMetadata(new Thickness(3.0, 1.0, 3.0, 1.0), OnMarginChangeCallback));

	private CalendarControl _calendarControl;

	private HabitCheckInWindow _checkInWindow;

	private Point _dragStart = new Point(0.0, 0.0);

	private IDragBarEvent _dragTarget;

	private IToastShowWindow _parentWindow;

	private bool _pressed;

	private bool _inArrange;

	private string _taskId;

	private bool _isWaitingDoubleClick;

	private DateTime _lastClickTime;

	private Point _firstClickPoint;

	private bool _detailShowing;

	private Path _foldPath;

	private bool _dragLeft;

	private IDragBarEvent _dragBarParent;

	private DateTime? _dragOverDate;

	private int _moveTag;

	private bool _showIcon;

	private int _row = -1;

	private bool _inMultiWeek;

	private Border _foldBorder;

	private double _iconScaleRatio = 1.0;

	private Grid _container;

	private Path _path;

	public TaskbarMode Mode { get; set; }

	public Thickness BarMargin
	{
		get
		{
			return (Thickness)GetValue(BarMarginProperty);
		}
		set
		{
			SetValue(BarMarginProperty, value);
		}
	}

	public bool InArrange
	{
		get
		{
			return _inArrange;
		}
		set
		{
			_inArrange = value;
			AddFoldPath();
		}
	}

	public bool InDragPopup { get; set; }

	public bool IsDragMode { get; set; }

	public TextBlock TimeText { get; set; }

	public EmjTextBlock TitleText { get; set; }

	public DockPanel TaskBarContainer { get; set; }

	public event EventHandler PopupClosed;

	public TaskBar()
		: this(isDragMode: false)
	{
	}

	public TaskBar(bool isDragMode)
	{
		IsDragMode = isDragMode;
		InitUi(isDragMode);
		base.DataContextChanged += OnDataBind;
		SetEvent();
		SetFontSize();
		base.Loaded += delegate
		{
			PropertyChangedEventManager.AddHandler(StaticPropertiesModel.Instance, OnDraggingTaskChanged, "DragOverCalendar");
		};
		base.Unloaded += delegate
		{
			PropertyChangedEventManager.RemoveHandler(StaticPropertiesModel.Instance, OnDraggingTaskChanged, "DragOverCalendar");
		};
	}

	private void OnDraggingTaskChanged(object sender, PropertyChangedEventArgs e)
	{
		if (base.DataContext is CalendarDisplayViewModel calendarDisplayViewModel && !(TickDragHelper.DragData is CalendarDisplayViewModel))
		{
			if (calendarDisplayViewModel.RepeatDiff == 0.0 && TickDragHelper.DragOverCalendar)
			{
				calendarDisplayViewModel.Dragging = !InDragPopup && (TickDragHelper.DraggingTaskIds?.Contains(calendarDisplayViewModel.TaskId) ?? false);
			}
			else if (calendarDisplayViewModel.Dragging && !TickDragHelper.DragOverCalendar)
			{
				calendarDisplayViewModel.Dragging = false;
			}
		}
	}

	private void SetEvent()
	{
		base.MouseLeftButtonUp += OnTaskClick;
		base.MouseRightButtonUp += OnShowOperation;
		base.PreviewMouseLeftButtonDown += OnTaskMouseDown;
	}

	private static void OnMarginChangeCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
	{
		if (sender is TaskBar taskBar && e.NewValue != null)
		{
			taskBar._container.Margin = (Thickness)e.NewValue;
		}
	}

	private IToastShowWindow GetToastWindow()
	{
		_parentWindow = _parentWindow ?? Utils.FindParent<IToastShowWindow>(this);
		return _parentWindow;
	}

	private void OnMove(object sender, MouseEventArgs e)
	{
		if (e.LeftButton != MouseButtonState.Pressed || !_pressed)
		{
			return;
		}
		if (base.DataContext == null)
		{
			return;
		}
		object dataContext = base.DataContext;
		CalendarDisplayViewModel model = dataContext as CalendarDisplayViewModel;
		if (model == null)
		{
			return;
		}
		if (model.CanDrag)
		{
			if (base.Cursor == Cursors.SizeWE && Mode == TaskbarMode.Normal)
			{
				TryDragChangeDue(e.GetPosition(_container).X < 8.0);
				return;
			}
			CalendarControl calendarParent = GetCalendarParent();
			if (calendarParent != null)
			{
				BlockingSet<string> batchSelected = calendarParent.BatchSelected;
				if ((batchSelected == null || batchSelected.Count != 0) && !model.BatchSelected)
				{
					return;
				}
			}
			TryStartDrag(e);
		}
		else
		{
			ProjectModel model2 = CacheManager.GetProjects().FirstOrDefault((ProjectModel p) => p.id == model.ProjectId);
			TryToastUnableString(model2);
			_container.MouseMove -= OnMove;
		}
	}

	private void TryDragChangeDue(bool left)
	{
		_dragLeft = left;
		_container.MouseMove -= OnMove;
		_dragBarParent = Utils.FindParent<IDragBarEvent>(this);
		if (_dragBarParent == null)
		{
			return;
		}
		if (_dragBarParent.GetBarDragging())
		{
			_dragBarParent = null;
			_dragBarParent?.SetBarDragging(dragging: false);
		}
		else if (_dragBarParent is FrameworkElement frameworkElement)
		{
			if (base.DataContext != null && base.DataContext is CalendarDisplayViewModel calendarDisplayViewModel)
			{
				calendarDisplayViewModel.Selected = true;
			}
			_dragBarParent.SetBarDragging(dragging: true);
			frameworkElement.CaptureMouse();
			frameworkElement.MouseMove -= OnDragSideMove;
			frameworkElement.MouseMove += OnDragSideMove;
			frameworkElement.MouseLeftButtonUp -= OnDragSideStop;
			frameworkElement.MouseLeftButtonUp += OnDragSideStop;
		}
	}

	private async void OnDragSideStop(object sender, MouseButtonEventArgs e)
	{
		_moveTag = -1;
		if (sender is FrameworkElement frameworkElement)
		{
			frameworkElement.ReleaseMouseCapture();
			frameworkElement.MouseMove -= OnDragSideMove;
			frameworkElement.MouseLeftButtonUp -= OnDragSideStop;
		}
		if (sender is IDragBarEvent dragBarEvent)
		{
			dragBarEvent.SetBarDragging(dragging: false);
		}
		_dragBarParent = null;
		if (base.DataContext == null)
		{
			return;
		}
		object dataContext = base.DataContext;
		if (!(dataContext is CalendarDisplayViewModel { DisplayStartDate: not null } model))
		{
			return;
		}
		model.Selected = false;
		if (!_dragOverDate.HasValue)
		{
			return;
		}
		DateTime start = model.DisplayStartDate.Value;
		DateTime end = model.DisplayDueDate ?? model.DisplayStartDate.Value;
		if ((model.IsAllDay ?? true) && start == end)
		{
			end = end.AddDays(1.0);
		}
		if (_dragLeft)
		{
			if (_dragOverDate.Value.Date < end.Date)
			{
				start = ticktick_WPF.Util.DateUtils.SetDateOnly(start, _dragOverDate.Value.Date);
			}
			if (_dragOverDate.Value.Date >= end.Date)
			{
				start = ticktick_WPF.Util.DateUtils.SetDateOnly(start, end.Date);
				end = ticktick_WPF.Util.DateUtils.SetDateOnly(end, _dragOverDate.Value.Date.AddDays((model.IsAllDay ?? true) ? 1 : 0));
				if (start.Date == end.Date && start >= end)
				{
					end = default(DateTime);
				}
			}
			DateTime value = start;
			DateTime? displayStartDate = model.DisplayStartDate;
			if (value == displayStartDate)
			{
				return;
			}
		}
		else
		{
			if (_dragOverDate.Value.Date > start.Date)
			{
				end = ticktick_WPF.Util.DateUtils.SetDateOnly(end, _dragOverDate.Value.Date.AddDays((model.IsAllDay ?? true) ? 1 : 0));
			}
			if (_dragOverDate.Value.Date <= start.Date)
			{
				end = ticktick_WPF.Util.DateUtils.SetDateOnly(end, start);
				start = ticktick_WPF.Util.DateUtils.SetDateOnly(start, _dragOverDate.Value.Date);
				if (start.Date == end.Date && start >= end)
				{
					end = default(DateTime);
				}
			}
			DateTime value = end;
			DateTime? displayStartDate = model.DisplayDueDate;
			if (value == displayStartDate)
			{
				return;
			}
			value = end.Date;
			DateTime? obj = model.DisplayStartDate?.Date;
			if (value == obj && !model.DisplayDueDate.HasValue)
			{
				return;
			}
		}
		if (!string.IsNullOrEmpty(model.RepeatFlag))
		{
			List<TaskReminderModel> reminders = await TaskReminderDao.GetRemindersByTaskId(model.TaskId);
			TimeData reviseData = new TimeData
			{
				StartDate = start,
				DueDate = end,
				IsAllDay = model.IsAllDay,
				Reminders = reminders,
				RepeatFrom = model.RepeatFrom,
				RepeatFlag = model.RepeatFlag,
				ExDates = ((model.SourceViewModel.ExDates == null) ? new List<string>() : ExDateSerilizer.ToArray(model.SourceViewModel.ExDates)?.ToList()),
				TimeZone = new TimeZoneViewModel(model.SourceViewModel.IsFloating, model.SourceViewModel.TimeZoneName)
			};
			await ModifyRepeatHandler.TryUpdateDueDate(model.TaskId, model.DisplayStartDate, model.DisplayDueDate, reviseData, 1, 0);
		}
		else
		{
			model.SetDropTimeSpan(start, Utils.IsEmptyDate(end) ? ((DateTime?)null) : new DateTime?(end), Window.GetWindow(this), null);
		}
		SetDragBackRect(show: false);
		SetValue(Panel.ZIndexProperty, 5);
	}

	private async void OnDragSideMove(object sender, MouseEventArgs e)
	{
		if (base.DataContext == null)
		{
			return;
		}
		object dataContext = base.DataContext;
		if (dataContext is CalendarDisplayViewModel model)
		{
			int moveTag = (_moveTag = new Random().Next(0, 10000));
			while (moveTag == _moveTag)
			{
				_dragOverDate = _dragBarParent?.DragSideOver(e, model, _dragLeft);
				await Task.Delay(100);
			}
		}
	}

	private async void TryStartDrag(MouseEventArgs e)
	{
		if (!Utils.CheckMoveDelta(this, e, _dragStart, 1) || base.DataContext == null)
		{
			return;
		}
		object dataContext = base.DataContext;
		if (!(dataContext is CalendarDisplayViewModel model))
		{
			return;
		}
		CalendarEventDragHelper.CheckIfEventCanDrag(model, out var canDrag, out var needToast, out var toastStr);
		UtilLog.Info("TryStartDragTaskBar " + model.TaskId + ", " + canDrag);
		if (canDrag)
		{
			DateTime? start = model.StartDate;
			DelaySetDragging(model);
			double num = base.ActualWidth / (double)Math.Max(1, model.ColumnSpan);
			Point position = e.GetPosition(this);
			CalendarDisplayViewModel calendarDisplayViewModel = model.Clone();
			calendarDisplayViewModel.DragStartDate = (model.DisplayStartDate ?? model.StartDate)?.Date;
			if (Mode == TaskbarMode.LoadMore)
			{
				LoadMoreWindow loadMoreWindow = Utils.FindParent<LoadMoreWindow>(this);
				if (loadMoreWindow != null)
				{
					calendarDisplayViewModel.DragStartDate = loadMoreWindow.GetDate();
				}
				else
				{
					YearViewPopup yearViewPopup = Utils.FindParent<YearViewPopup>(this);
					if (yearViewPopup == null)
					{
						return;
					}
					calendarDisplayViewModel.DragStartDate = yearViewPopup.GetDate();
				}
			}
			if (_inArrange)
			{
				calendarDisplayViewModel.DragStartDate = calendarDisplayViewModel.DisplayStartDate;
			}
			Utils.FindParent<MultiDayView>(this)?.SetBatchDragStarting();
			await TickDragHelper.StartDragItem(GetCalendarParent(), calendarDisplayViewModel, (!model.BatchSelected) ? new List<string> { model.TaskId } : (GetCalendarParent()?.BatchSelected?.ToList() ?? new List<string> { model.TaskId }), num, new Point((position.X > num) ? (num / 2.0) : position.X, position.Y));
			await Task.Delay(100);
			while (ModifyRepeatHandler.DialogShowing)
			{
				await Task.Delay(100);
			}
			if (start == model.StartDate)
			{
				model.Dragging = false;
			}
		}
		else if (needToast)
		{
			GetToastWindow()?.TryToastString(null, toastStr);
		}
		_dragStart = default(Point);
		_pressed = false;
	}

	private async Task DelaySetDragging(CalendarDisplayViewModel model)
	{
		await Task.Delay(50);
		if (TickDragHelper.Dragging)
		{
			if (model.BatchSelected)
			{
				model.BatchDragging = true;
				GetCalendarParent()?.SetBatchDragging(dragging: true);
			}
			else
			{
				model.Dragging = true;
			}
		}
		LoadMoreWindow.TryClose();
		YearViewPopup.TryClose();
	}

	private IDragBarEvent GetDragTarget()
	{
		_dragTarget = _dragTarget ?? Utils.FindParent<IDragBarEvent>(this);
		return _dragTarget;
	}

	private async void OnTaskClick(object sender, MouseButtonEventArgs e)
	{
		if (!(base.DataContext is CalendarDisplayViewModel calendarDisplayViewModel))
		{
			return;
		}
		e.Handled = true;
		if (((GetCalendarParent()?.IsLocked ?? false) && !calendarDisplayViewModel.IsLoadMore) || (GetDragTarget()?.OnSelection() ?? false))
		{
			return;
		}
		GetCalendarParent()?.ClearCopiedTaskSelection(calendarDisplayViewModel.GetTaskId());
		if ((!Utils.IfCtrlPressed() && (GetCalendarParent()?.ExitBatchSelect() ?? false)) || PopupStateManager.IsInSelection() || PopupStateManager.IsInAdd() || !PopupStateManager.CanShowDetailPopup())
		{
			return;
		}
		if (_detailShowing)
		{
			_detailShowing = false;
			return;
		}
		if (TaskBarContainer.IsMouseOver)
		{
			DisplayType type = calendarDisplayViewModel.Type;
			if ((uint)type <= 3u)
			{
				return;
			}
		}
		_container.MouseMove -= OnMove;
		CalendarControl calendarParent = GetCalendarParent();
		if (Utils.IfCtrlPressed() && !_inArrange)
		{
			if (calendarDisplayViewModel.CanBatchSelected)
			{
				calendarDisplayViewModel.BatchSelected = !calendarDisplayViewModel.BatchSelected;
				calendarParent?.SetBatchSelected(calendarDisplayViewModel.GetTaskId(), calendarDisplayViewModel.BatchSelected, calendarDisplayViewModel.Row);
			}
			else
			{
				GetToastWindow()?.TryToastString(null, Utils.GetString("OptionNotSupport"));
			}
			return;
		}
		calendarParent?.AddItemClickEvent();
		if (calendarDisplayViewModel.IsCalendarEvent)
		{
			await ShowEventDetail(e, calendarDisplayViewModel);
		}
		else if (calendarDisplayViewModel.IsCountdown)
		{
			await ShowCountdownDetail(calendarDisplayViewModel);
		}
		else if (calendarDisplayViewModel.IsHabit)
		{
			await ShowHabitDetail(e, calendarDisplayViewModel);
		}
		else if (calendarDisplayViewModel.IsPomo)
		{
			ShowPomoDetail(calendarDisplayViewModel);
		}
		else if (calendarDisplayViewModel.Course != null)
		{
			ShowCourseDetail(calendarDisplayViewModel);
		}
		else
		{
			TryShowTaskDetail(calendarDisplayViewModel);
		}
		_pressed = false;
	}

	private async Task ShowCountdownDetail(CalendarDisplayViewModel model)
	{
		CountdownModel countdownModel = (model.SourceViewModel as CountDownBaseViewModel)?.Countdown;
		if (countdownModel != null)
		{
			model.Selected = true;
			_detailShowing = true;
			CountdownDetailWindow countdownDetailWindow = new CountdownDetailWindow();
			countdownDetailWindow.Disappear -= OnDetailClosed;
			countdownDetailWindow.Disappear += OnDetailClosed;
			countdownDetailWindow.Show(model: new CountDownViewModel(countdownModel, model.StartDate ?? DateTime.Now)
			{
				ScaleRatio = 1.2999999523162842
			}, target: this, targetWidth: base.ActualWidth + 6.0, addHeight: 0.0, byMouse: base.ActualWidth >= 350.0);
		}
	}

	private async void ShowPomoDetail(CalendarDisplayViewModel model)
	{
		PomodoroModel pomodoroModel = await PomoService.GetAdjustPomo(model.PomoId);
		if (pomodoroModel != null)
		{
			model.Selected = true;
			PomoDetailWindow pomoDetailWindow = new PomoDetailWindow(new PomoDisplayViewModel(pomodoroModel, pomodoroModel.Tasks), model);
			pomoDetailWindow.Closed += async delegate(object obj, EventArgs e)
			{
				model.Selected = false;
				OnDetailClosed(obj, string.Empty);
				await Task.Delay(200);
				_detailShowing = false;
			};
			_detailShowing = true;
			pomoDetailWindow.Show(this, base.ActualWidth, base.ActualWidth > 300.0);
			GetCalendarParent()?.SetEditting(editting: true);
		}
	}

	private async void TryShowTaskDetail(CalendarDisplayViewModel model)
	{
		_isWaitingDoubleClick = false;
		bool num = (DateTime.Now - _lastClickTime).TotalMilliseconds > 400.0;
		_lastClickTime = DateTime.Now;
		if (num)
		{
			_isWaitingDoubleClick = true;
			_firstClickPoint = PopupLocationCalculator.GetMousePoint(isDefault: false);
			await ShowTaskDetail(model);
			await Task.Delay(400);
			if (!_isWaitingDoubleClick)
			{
				UserActCollectUtils.AddClickEvent("calendar", "action", "click_task");
			}
		}
		else
		{
			UserActCollectUtils.AddClickEvent("calendar", "action", "double_click_task");
			model.Selected = false;
			TaskDetailWindows.ShowTaskWindows(model.TaskId);
		}
	}

	private async Task ShowHabitDetail(MouseEventArgs e, CalendarDisplayViewModel model)
	{
		HabitModel habitModel = await HabitDao.GetHabitById(model.HabitId);
		if (habitModel != null)
		{
			HabitCheckInWindow window = new HabitCheckInWindow(habitModel, model.Status, model.StartDate, GetToastWindow());
			window.Show(this, base.ActualWidth, 0.0, base.ActualWidth >= 350.0);
			GetCalendarParent()?.SetEditting(editting: true);
			window.OnAction += OnCheckInAction;
			window.Closed += delegate
			{
				HandleOnWindowClosed();
				window.OnAction -= OnCheckInAction;
			};
			model.Selected = true;
		}
	}

	private async void OnCheckInAction(object sender, EventArgs extra)
	{
		HandleOnWindowClosed();
	}

	private async void ShowCourseDetail(CalendarDisplayViewModel model)
	{
		if (model.Course != null)
		{
			CourseDetailViewModel viewModel = await CourseDetailViewModel.Build(model.Course);
			model.Selected = true;
			CourseDetailWindow courseDetailWindow = new CourseDetailWindow(viewModel);
			courseDetailWindow.Closed += async delegate(object obj, EventArgs e)
			{
				model.Selected = false;
				OnDetailClosed(obj, string.Empty);
				await Task.Delay(200);
				_detailShowing = false;
			};
			_detailShowing = true;
			courseDetailWindow.Show(this, base.ActualWidth, base.ActualWidth > 300.0);
			GetCalendarParent()?.SetEditting(editting: true);
		}
	}

	private async Task ShowEventDetail(MouseEventArgs e, CalendarDisplayViewModel calendarDisplayViewModel)
	{
		string eventId = calendarDisplayViewModel.EventId;
		if (string.IsNullOrEmpty(eventId))
		{
			return;
		}
		calendarDisplayViewModel.Selected = true;
		CalendarEventModel calendarEventModel = await CalendarEventDao.GetEventById(ArchivedDao.GetOriginalId(eventId));
		if (calendarEventModel != null)
		{
			if (base.DataContext != null && base.DataContext is CalendarDisplayViewModel calendarDisplayViewModel2)
			{
				calendarEventModel.DueStart = calendarDisplayViewModel2.StartDate;
				calendarEventModel.DueEnd = calendarDisplayViewModel2.DueDate;
			}
			CalendarDetailWindow calendarDetailWindow = new CalendarDetailWindow(GetWidgetThemeId());
			calendarDetailWindow.Disappear += OnDetailClosed;
			calendarDetailWindow.EventArchiveChanged += OnEventArchiveChanged;
			calendarDetailWindow.Show(this, base.ActualWidth, 0.0, base.ActualWidth > 350.0, new CalendarDetailViewModel(calendarEventModel));
			GetCalendarParent()?.SetEditting(editting: true);
		}
	}

	private void OnEventArchiveChanged(object sender, bool archive)
	{
		CalendarDisplayViewModel model = GetModel();
		if (model != null)
		{
			int status = (model.SourceViewModel.Status = (archive ? 2 : 0));
			model.Status = status;
		}
	}

	private async Task<TaskDetailPopup> ShowTaskDetail(CalendarDisplayViewModel model)
	{
		if (model == null)
		{
			return null;
		}
		TaskDetailViewModel taskDetailViewModel = await TaskDetailViewModel.Build(model.GetTaskId());
		if (taskDetailViewModel != null)
		{
			taskDetailViewModel.RepeatDiff = (string.IsNullOrEmpty(model.RepeatFlag) ? ((double?)null) : new double?(model.RepeatDiff));
			TaskDetailPopup taskDetailPopup = new TaskDetailPopup
			{
				IsCalendarContext = true
			};
			IToastShowWindow toastWindow = GetToastWindow();
			if (toastWindow != null)
			{
				if (PopupStateManager.LastTarget == this)
				{
					model.Selected = false;
					return null;
				}
				taskDetailPopup.DependentWindow = toastWindow;
			}
			model.Selected = true;
			taskDetailPopup.Disappear -= OnDetailClosed;
			taskDetailPopup.Disappear += OnDetailClosed;
			taskDetailPopup.Show(taskDetailViewModel, model.ItemId, new TaskWindowDisplayArgs(this, base.ActualWidth + 9.0, (base.ActualWidth < 350.0) ? default(Point) : _firstClickPoint, 0.0));
			GetCalendarParent()?.SetEditting(editting: true);
			return taskDetailPopup;
		}
		return null;
	}

	private string GetWidgetThemeId()
	{
		CalendarWidget calendarWidget = Utils.FindParent<CalendarWidget>(this);
		if (calendarWidget != null)
		{
			return calendarWidget.ThemeId;
		}
		return string.Empty;
	}

	private bool CheckIfDayMode()
	{
		return base.ActualWidth > 245.0;
	}

	private async void OnDetailClosed(object sender, string e)
	{
		PopupStateManager.OnViewPopupClosed();
		HandleOnWindowClosed();
		if (sender is TaskDetailPopup taskDetailPopup)
		{
			taskDetailPopup.Disappear -= OnDetailClosed;
		}
		await Task.Delay(200);
		_detailShowing = false;
	}

	private void HandleOnWindowClosed()
	{
		if (base.DataContext is CalendarDisplayViewModel calendarDisplayViewModel)
		{
			calendarDisplayViewModel.Selected = false;
		}
		Utils.FindParent<LoadMoreWindow>(this)?.GetFocus();
		GetCalendarParent()?.SetEditting(editting: false);
		GetCalendarParent()?.GetFocus(force: true);
		this.PopupClosed?.Invoke(this, null);
	}

	private CalendarControl GetCalendarParent()
	{
		if (Mode == TaskbarMode.LoadMore)
		{
			LoadMoreWindow loadMoreWindow = Utils.FindParent<LoadMoreWindow>(this);
			if (loadMoreWindow != null)
			{
				CalendarControl calendarControl = Utils.FindParent<CalendarControl>(loadMoreWindow?.Context);
				_calendarControl = calendarControl;
			}
			else
			{
				CalendarControl calendarControl2 = Utils.FindParent<CalendarControl>(Utils.FindParent<YearViewPopup>(this)?.Context);
				_calendarControl = calendarControl2;
			}
		}
		_calendarControl = _calendarControl ?? Utils.FindParent<CalendarControl>(this);
		return _calendarControl;
	}

	private void OnTaskMouseDown(object sender, MouseButtonEventArgs e)
	{
		CalendarControl calendarParent = GetCalendarParent();
		if (calendarParent == null || !calendarParent.IsLocked)
		{
			_pressed = true;
			_dragStart = e.GetPosition(this);
			_container.MouseMove -= OnMove;
			_container.MouseMove += OnMove;
		}
	}

	private bool TryToastUnableString(ProjectModel model)
	{
		if (model != null && !string.IsNullOrEmpty(model.permission))
		{
			IToastShowWindow toastWindow = GetToastWindow();
			string permission = model.permission;
			if (permission == "read")
			{
				string arg = Utils.GetString("ReadOnly");
				toastWindow?.TryToastString(null, string.Format(Utils.GetString("NoPermissionToEdit"), arg));
				return true;
			}
			if (permission == "comment")
			{
				string arg = Utils.GetString("CanComment");
				toastWindow?.TryToastString(null, string.Format(Utils.GetString("NoPermissionToEdit"), arg));
				return true;
			}
		}
		return false;
	}

	private async void OnShowOperation(object sender, MouseButtonEventArgs e)
	{
		if ((GetCalendarParent()?.IsLocked ?? false) || base.DataContext == null)
		{
			return;
		}
		object dataContext = base.DataContext;
		CalendarDisplayViewModel model = dataContext as CalendarDisplayViewModel;
		if (model == null)
		{
			return;
		}
		CalendarControl calendarParent = GetCalendarParent();
		calendarParent?.ClearCopiedTaskSelection(model.GetTaskId());
		if (calendarParent != null && !model.BatchSelected && calendarParent.ExitBatchSelect())
		{
			return;
		}
		if (model.Type == DisplayType.Task || model.Type == DisplayType.Note || model.Type == DisplayType.Derivative)
		{
			model.Selected = true;
			_taskId = model.TaskId;
		}
		else
		{
			_taskId = null;
		}
		if (model.Type == DisplayType.Event)
		{
			if (!model.SourceViewModel.OutDate())
			{
				model.Selected = true;
				CalendarOperationDialog calendarOperationDialog = new CalendarOperationDialog(new EventArchiveArgs(model.SourceViewModel), model.Status != 0);
				calendarOperationDialog.SetPlaceTarget(this);
				calendarOperationDialog.Closed += OnDialogClosed;
				GetCalendarParent()?.SetEditting(editting: true);
				calendarOperationDialog.Operated += delegate(object arg, KeyValuePair<string, ActionType> obj)
				{
					CalendarDisplayViewModel calendarDisplayViewModel = model;
					int status = (model.SourceViewModel.Status = ((obj.Value == ActionType.Archive) ? 2 : 0));
					calendarDisplayViewModel.Status = status;
				};
				calendarOperationDialog.Show();
			}
		}
		else if (model.Type == DisplayType.Course)
		{
			if (!(model.StartDate < DateTime.Today))
			{
				model.Selected = true;
				ArchiveOperationDialog archiveOperationDialog = new ArchiveOperationDialog(model.Course.UniqueId, ArchiveKind.Course, model.Course.Archived);
				archiveOperationDialog.SetPlaceTarget(this);
				archiveOperationDialog.Closed += OnDialogClosed;
				GetCalendarParent()?.SetEditting(editting: true);
				archiveOperationDialog.Operated += delegate(object arg, KeyValuePair<string, ActionType> obj)
				{
					model.Course.Archived = obj.Value == ActionType.Archive;
					CalendarDisplayViewModel calendarDisplayViewModel = model;
					int status = (model.SourceViewModel.Status = ((model.Course.Archived || model.Course.CourseStart.Date < DateTime.Today) ? 2 : 0));
					calendarDisplayViewModel.Status = status;
				};
				archiveOperationDialog.Show();
			}
		}
		else
		{
			ProjectModel model2 = CacheManager.GetProjects().FirstOrDefault((ProjectModel p) => p.id == model.ProjectId);
			if ((model.Type == DisplayType.Habit && model.Status == 2) || TryToastUnableString(model2))
			{
				model.Selected = false;
			}
			else if (model.Type == DisplayType.Habit)
			{
				ShowHabitOperation(model);
			}
			else
			{
				await ShowTaskOperation(model);
			}
		}
	}

	private void ShowHabitOperation(CalendarDisplayViewModel model)
	{
		if (model.Status != 0)
		{
			return;
		}
		List<OperationItemViewModel> list = new List<OperationItemViewModel>
		{
			new OperationItemViewModel(ActionType.EditHabitLog),
			new OperationItemViewModel(ActionType.Skip),
			new OperationItemViewModel(ActionType.UnComplete)
		};
		HabitBaseViewModel habitBaseViewModel = (model.IsHabit ? (model.SourceViewModel as HabitBaseViewModel) : null);
		HabitCheckInModel habitCheckInModel = habitBaseViewModel?.HabitCheckIn;
		if (habitCheckInModel != null && habitCheckInModel.Value > 0.0)
		{
			list.Add(new OperationItemViewModel(ActionType.Reset));
		}
		if (LocalSettings.Settings.EnableFocus)
		{
			list.Insert(0, new OperationItemViewModel(ActionType.StartFocus)
			{
				Selectable = false,
				SubActions = new List<OperationItemViewModel>
				{
					new OperationItemViewModel(ActionType.StartPomo)
					{
						Enable = (!TickFocusManager.Working || LocalSettings.Settings.PomoType == FocusConstance.Focus)
					},
					new OperationItemViewModel(ActionType.StartTiming)
					{
						Enable = (!TickFocusManager.Working || LocalSettings.Settings.PomoType == FocusConstance.Timing)
					}
				}
			});
		}
		HabitModel habit = habitBaseViewModel?.Habit;
		OperationDialog operationDialog = new OperationDialog(model.HabitId, list);
		operationDialog.SetPlaceTarget(this);
		operationDialog.Operated += async delegate(object obj, KeyValuePair<string, ActionType> kv)
		{
			switch (kv.Value)
			{
			case ActionType.StartPomo:
				TickFocusManager.TryStartFocusHabit(kv.Key, isPomo: true);
				break;
			case ActionType.StartTiming:
				TickFocusManager.TryStartFocusHabit(kv.Key, isPomo: false);
				break;
			case ActionType.Skip:
			{
				await HabitService.SkipHabit(model.HabitId);
				GetCalendarParent()?.RemoveItem(model);
				LoadMoreWindow loadMoreWindow = Utils.FindParent<LoadMoreWindow>(this);
				loadMoreWindow?.RemoveItem(this);
				IToastShowWindow toastShowWindow = GetToastWindow();
				if (toastShowWindow == null)
				{
					toastShowWindow = ((!(loadMoreWindow?.Owner is IToastShowWindow toastShowWindow2)) ? App.Window : toastShowWindow2);
				}
				toastShowWindow?.TryToastString(null, Utils.GetString("Skipped"));
				break;
			}
			case ActionType.EditHabitLog:
				HabitUtils.ShowHabitRecordWindow(model.HabitId, habit?.Name, DateTime.Today, isEdit: true, !model.IsCompleted);
				break;
			case ActionType.UnComplete:
				await HabitService.UncompleteHabit(model.HabitId, DateTime.Today);
				break;
			case ActionType.Reset:
				await HabitService.ResetCheckInHabit(model.HabitId, DateTime.Today, habit?.Type, this);
				break;
			}
		};
		operationDialog.Show();
	}

	private async Task ShowTaskOperation(CalendarDisplayViewModel model)
	{
		if (string.IsNullOrEmpty(_taskId))
		{
			return;
		}
		GetToastWindow();
		if (model.BatchSelected)
		{
			model.Selected = false;
			ShowBatchTaskOperation(model);
			return;
		}
		OperationExtra operationExtra = await TaskOperationHelper.GetTaskAccessInfo(_taskId, canShowAddSubtask: false);
		if (operationExtra.TimeModel == null)
		{
			return;
		}
		operationExtra.TimeModel.StartDate = model.DisplayStartDate ?? model.StartDate;
		operationExtra.TimeModel.DueDate = model.DisplayDueDate ?? model.DueDate;
		TaskOperationDialog dialog = new TaskOperationDialog(operationExtra, this);
		if (Window.GetWindow(this) is WidgetWindow widgetWindow)
		{
			dialog.Resources = widgetWindow.Resources;
		}
		dialog.Closed += OnDialogClosed;
		dialog.PrioritySelect += SetPriority;
		dialog.Copied += CopyTask;
		dialog.LinkCopied += CopyTaskLink;
		dialog.Deleted += DeleteTask;
		dialog.AbandonOrReopen += OnAbandonOrReopenTask;
		dialog.ProjectSelect += OnProjectSelect;
		dialog.AssigneeSelect += OnAssigneeSelect;
		dialog.TagsSelect += OnTagsSelect;
		dialog.SkipCurrentRecurrence += OnSkipRecurrence;
		dialog.SwitchTaskOrNote += OnSwitchTaskOrNoteClick;
		dialog.Toast += OnTaskOperationToast;
		dialog.CompleteDateChanged += async delegate(object o, DateTime date)
		{
			await TaskService.ChangeCompleteDate(model.TaskId, date);
			SyncManager.TryDelaySync();
		};
		dialog.TimeClear += async delegate
		{
			dialog.Dismiss();
			await ClearDate();
		};
		dialog.TimeSelect += async delegate(object arg, TimeData data)
		{
			dialog.Dismiss();
			if (string.IsNullOrEmpty(model.RepeatFlag) || !model.StartDate.HasValue || model.Status != 0)
			{
				await TaskService.SetDate(_taskId, data);
			}
			else
			{
				ModifyRepeatHandler.TryUpdateDueDate(model.TaskId, model.DisplayStartDate, model.DisplayDueDate, data, 1, 1);
			}
		};
		dialog.QuickDateSelect += async delegate(object arg, DateTime date)
		{
			dialog.Dismiss();
			if (!string.IsNullOrEmpty(model.RepeatFlag) && model.Status == 0)
			{
				TaskModel task = await TaskDao.GetThinTaskById(model.TaskId);
				if (task != null && model.StartDate.HasValue && task.startDate.HasValue)
				{
					List<TaskReminderModel> reminders = await TaskReminderDao.GetRemindersByTaskId(task.id);
					TimeData timeData = new TimeData
					{
						StartDate = model.StartDate,
						DueDate = model.DueDate,
						IsAllDay = (!model.StartDate.HasValue || (model.IsAllDay ?? true)),
						Reminders = reminders,
						RepeatFrom = model.RepeatFrom,
						RepeatFlag = model.RepeatFlag,
						ExDates = ((task.exDate != null) ? task.exDate.ToList() : new List<string>())
					};
					ModifyRepeatHandler.TryUpdateDueDateOnlyDate(model.TaskId, model.DisplayStartDate, model.DisplayDueDate, timeData, date, 1, 1);
				}
			}
			else
			{
				await TaskService.SetStartDate(_taskId, date);
				SyncManager.TryDelaySync();
			}
		};
		dialog.Show();
		GetCalendarParent()?.SetEditting(editting: true);
	}

	private async void ShowBatchTaskOperation(CalendarDisplayViewModel model)
	{
		List<string> taskIds = GetCalendarParent()?.BatchSelected.ToList() ?? new List<string> { model.TaskId };
		OperationExtra extra = await BatchTaskEditHelper.GetBatchArgs(taskIds, isNote: false, useInList: false);
		if (extra == null)
		{
			return;
		}
		extra.InCal = true;
		TaskOperationDialog dialog = new TaskOperationDialog(extra, this);
		if (Window.GetWindow(this) is WidgetWindow widgetWindow)
		{
			dialog.Resources = widgetWindow.Resources;
		}
		dialog.Closed += delegate
		{
			GetCalendarParent()?.SetEditting(editting: false);
			PopupStateManager.OnViewPopupClosed(needWait: false);
		};
		dialog.OpenSticky += delegate
		{
			GetCalendarParent()?.BatchOpenSticky();
		};
		dialog.PrioritySelect += delegate(object o, int p)
		{
			GetCalendarParent()?.BatchSetPriority(p);
		};
		dialog.TextCopied += delegate
		{
			GetCalendarParent()?.CopyBatchTasksText(taskIds);
		};
		dialog.Copied += delegate
		{
			TaskService.CopyTasks(taskIds, GetCalendarParent());
		};
		dialog.Deleted += delegate
		{
			GetCalendarParent()?.OnBatchDelete();
		};
		dialog.AbandonOrReopen += delegate
		{
			GetCalendarParent()?.OnBatchComplete();
		};
		dialog.ProjectSelect += delegate(object o, SelectableItemViewModel p)
		{
			GetCalendarParent()?.OnBatchSetProject(p, extra.ProjectId, extra.ColumnId);
		};
		dialog.AssigneeSelect += delegate(object o, AvatarInfo a)
		{
			GetCalendarParent()?.OnBatchSetAssignee(a);
		};
		dialog.TagsSelect += delegate(object o, TagSelectData t)
		{
			GetCalendarParent()?.OnBatchSetTags(t);
		};
		dialog.SkipCurrentRecurrence += delegate
		{
			GetCalendarParent()?.OnBatchSkip();
		};
		dialog.SwitchTaskOrNote += delegate
		{
			GetCalendarParent()?.OnBatchSwitchTaskNote();
		};
		dialog.Toast += delegate(object o, string s)
		{
			GetToastWindow()?.TryToastString(o, s);
		};
		dialog.CompleteDateChanged += delegate(object o, DateTime date)
		{
			foreach (string item in taskIds)
			{
				TaskService.ChangeCompleteDate(item, date);
			}
			SyncManager.TryDelaySync();
		};
		dialog.TimeClear += async delegate
		{
			await ClearDate();
			dialog.Dismiss();
			GetCalendarParent()?.BatchClearDate();
		};
		dialog.TimeSelect += async delegate(object arg, TimeData data)
		{
			GetCalendarParent()?.BatchSetTimeData(data);
			dialog.Dismiss();
		};
		dialog.QuickDateSelect += async delegate(object arg, DateTime date)
		{
			GetCalendarParent()?.BatchSetQuickDate(date);
			dialog.Dismiss();
		};
		dialog.DelayOrAdvanced += delegate(object sender, long l)
		{
			GetCalendarParent()?.OnBatchDelay(l);
		};
		dialog.Show();
		PopupStateManager.OnViewPopupOpened();
		GetCalendarParent()?.SetEditting(editting: true);
	}

	private void OnTaskOperationToast(object sender, string e)
	{
		GetToastWindow()?.TryToastString(null, e);
	}

	private async void OnAbandonOrReopenTask(object sender, EventArgs e)
	{
		if (base.DataContext == null)
		{
			return;
		}
		object dataContext = base.DataContext;
		if (!(dataContext is CalendarDisplayViewModel { DisplayStartDate: var displayStartDate } model))
		{
			return;
		}
		if (displayStartDate.HasValue && model.StartDate != model.DisplayStartDate && model.Status == 0)
		{
			IToastShowWindow toastWindow = GetToastWindow();
			if (!(await ModifyRepeatHandler.CompleteOrSkipRecurrence(model.TaskId, model.DisplayStartDate, toastWindow)))
			{
				return;
			}
		}
		int num = ((!model.IsAbandoned) ? (-1) : 0);
		await TaskService.SetTaskStatus(model.TaskId, num, num != 0, GetToastWindow());
	}

	private async void OnSwitchTaskOrNoteClick(object sender, EventArgs e)
	{
		if (base.DataContext != null && base.DataContext is CalendarDisplayViewModel calendarDisplayViewModel && await TaskService.SwitchTaskOrNote(calendarDisplayViewModel.TaskId, toast: false))
		{
			GetToastWindow()?.TryToastString(null, Utils.GetString("Converted"));
			SyncManager.TryDelaySync();
		}
	}

	private async void CopyTaskLink(object sender, EventArgs e)
	{
		TaskModel taskModel = await TaskDao.GetThinTaskById(_taskId);
		if (taskModel != null)
		{
			TaskUtils.CopyTaskLink(taskModel.id, taskModel.projectId, taskModel.title);
		}
	}

	private async Task ClearDate()
	{
		TaskModel taskModel = await TaskDao.GetThinTaskById(_taskId);
		if (taskModel != null)
		{
			if (string.IsNullOrEmpty(taskModel.attendId))
			{
				await TaskService.ClearDate(_taskId);
				SyncManager.TryDelaySync();
			}
			else if (await TaskOperationHelper.CheckIfTaskAllowClearDate(_taskId, this))
			{
				await TaskService.ClearAgendaDate(_taskId);
				SyncManager.TryDelaySync();
			}
		}
	}

	private async void OnSkipRecurrence(object sender, EventArgs e)
	{
		CalendarDisplayViewModel model = GetModel();
		if (model == null || !model.DisplayStartDate.HasValue || !(model.DisplayStartDate != model.StartDate))
		{
			await TaskService.SkipCurrentRecurrence(_taskId, notify: true, GetToastWindow());
		}
		else
		{
			await TaskService.SkipSelectRecurrence(_taskId, model.DisplayStartDate.Value.Date, GetToastWindow());
		}
		SyncManager.TryDelaySync();
	}

	private async void OnTagsSelect(object sender, TagSelectData tags)
	{
		await TaskService.SetTags(_taskId, tags.OmniSelectTags);
		SyncManager.TryDelaySync();
	}

	private async void OnAssigneeSelect(object sender, AvatarInfo assignee)
	{
		await TaskService.SetAssignee(_taskId, assignee.UserId);
		SyncManager.TryDelaySync();
	}

	private async void OnProjectSelect(object sender, SelectableItemViewModel e)
	{
		(string, string, string) projectAndColumnId = e.GetProjectAndColumnId();
		string projectId = projectAndColumnId.Item1;
		string item = projectAndColumnId.Item2;
		string taskId = _taskId;
		string columnId = item;
		await TaskService.MoveProject(taskId, projectId, null, columnId);
		SyncManager.TryDelaySync();
		CalendarControl calendarParent = GetCalendarParent();
		if (!string.IsNullOrEmpty(CalendarUtils.CheckTaskCanShown(_taskId, calendarParent != null && calendarParent.ViewSourceType == CalendarViewSourceType.WithList)))
		{
			GetToastWindow()?.ToastMoveProjectControl(projectId);
		}
	}

	private async void DeleteTask(object sender, EventArgs e)
	{
		PopupStateManager.OnViewPopupClosed(needWait: false);
		TaskModel task = await TaskDao.GetThinTaskById(_taskId);
		if (task == null)
		{
			return;
		}
		if (string.IsNullOrEmpty(task.attendId))
		{
			if (!string.IsNullOrEmpty(task.repeatFlag) && task.startDate.HasValue && task.status == 0 && base.DataContext != null && base.DataContext is CalendarDisplayViewModel calendarDisplayViewModel && calendarDisplayViewModel.TaskId == task.id)
			{
				ModifyRepeatHandler.TryDeleteRecurrence(task.id, calendarDisplayViewModel.DisplayStartDate, calendarDisplayViewModel.DisplayDueDate, GetToastWindow());
				return;
			}
			List<TaskModel> list = await TaskService.GetAllSubTasksByIdAsync(_taskId, task.projectId);
			if (list != null && list.Count > 0)
			{
				list.Add(task);
				GetToastWindow()?.BatchDeleteTask(list);
			}
			else
			{
				GetToastWindow()?.TaskDeleted(_taskId);
			}
		}
		else if (await TaskOperationHelper.CheckIfAllowDeleteAgenda(task, this))
		{
			if (!string.IsNullOrEmpty(task.repeatFlag) && task.startDate.HasValue && task.status == 0)
			{
				ModifyRepeatHandler.TryDeleteRecurrence(task.id, task.startDate, task.dueDate, GetToastWindow());
				return;
			}
			await TaskService.DeleteAgenda(_taskId, task.projectId, task.attendId);
		}
		if (base.DataContext != null && base.DataContext is CalendarDisplayViewModel model)
		{
			GetCalendarParent()?.RemoveItem(model);
		}
	}

	private async void CopyTask(object sender, EventArgs e)
	{
		TaskModel taskModel = await TaskService.CopyTask(_taskId, this);
		if (taskModel != null)
		{
			GetCalendarParent()?.SelectCopiedTask(taskModel.id);
		}
	}

	private async void SetPriority(object sender, int priority)
	{
		await TaskService.SetPriority(_taskId, priority);
	}

	private void OnDialogClosed(object sender, EventArgs e)
	{
		CalendarDisplayViewModel model = GetModel();
		if (model != null)
		{
			model.Selected = false;
		}
		GetCalendarParent()?.SetEditting(editting: false);
		PopupStateManager.OnViewPopupClosed();
	}

	public CalendarDisplayViewModel GetModel()
	{
		if (base.DataContext != null && base.DataContext is CalendarDisplayViewModel result)
		{
			return result;
		}
		return null;
	}

	private void OnDataBind(object sender, DependencyPropertyChangedEventArgs e)
	{
		(e.OldValue as CalendarDisplayViewModel)?.SetTaskBar(null);
		if (base.DataContext is CalendarDisplayViewModel calendarDisplayViewModel && base.Tag is bool theme)
		{
			calendarDisplayViewModel.SetTaskBar(this);
			calendarDisplayViewModel.SetTheme(theme);
			calendarDisplayViewModel.SetTimeText(InArrange, isSpan: false);
			if (_foldPath != null)
			{
				Path foldPath = _foldPath;
				List<CalendarDisplayViewModel> children = calendarDisplayViewModel.Children;
				foldPath.Visibility = ((children == null || children.Count <= 0) ? Visibility.Collapsed : Visibility.Visible);
				((RotateTransform)_foldPath.RenderTransform).Angle = ((!calendarDisplayViewModel.IsOpened) ? 90 : 0);
			}
			calendarDisplayViewModel.SetSourceModel(calendarDisplayViewModel.SourceViewModel, setEvent: true);
			calendarDisplayViewModel.SetIcon();
			SetIcon(_showIcon || calendarDisplayViewModel.ForceShowIcon);
		}
	}

	private void AddFoldPath()
	{
		_foldBorder = new Border();
		_foldBorder.SetValue(DockPanel.DockProperty, Dock.Right);
		_foldBorder.SetResourceReference(FrameworkElement.StyleProperty, "SmoothHoverBorderStyle40_60");
		_foldBorder.PreviewMouseLeftButtonUp += OnOpenClick;
		TaskBarContainer.Children.Insert(0, _foldBorder);
		_foldPath = new Path();
		_foldPath.Margin = new Thickness(2.0, 0.0, 2.0, 0.0);
		_foldPath.SetResourceReference(Shape.FillProperty, "BaseColorOpacity100");
		_foldPath.SetResourceReference(FrameworkElement.StyleProperty, "ArrowPathStyle");
		_foldPath.RenderTransformOrigin = new Point(0.5, 0.5);
		_foldPath.RenderTransform = new RotateTransform
		{
			Angle = 0.0
		};
		_foldBorder.Child = _foldPath;
	}

	private async void OnOpenClick(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
		_pressed = false;
		if (base.DataContext is CalendarDisplayViewModel calendarDisplayViewModel)
		{
			calendarDisplayViewModel.IsOpened = !calendarDisplayViewModel.IsOpened;
			((RotateTransform)_foldPath.RenderTransform).Angle = ((!calendarDisplayViewModel.IsOpened) ? 90 : 0);
			if (calendarDisplayViewModel.IsOpened)
			{
				ArrangeSectionStatusDao.DeleteSectionStatusModel(new ArrangeSectionStatusModel
				{
					Name = calendarDisplayViewModel.TaskId,
					Type = LocalSettings.Settings.ArrangeDisplayType
				});
			}
			else
			{
				ArrangeSectionStatusDao.AddSectionStatusModel(new ArrangeSectionStatusModel
				{
					Name = calendarDisplayViewModel.TaskId,
					Type = LocalSettings.Settings.ArrangeDisplayType,
					UserId = LocalSettings.Settings.LoginUserId
				});
			}
			CalendarControl calendarParent = GetCalendarParent();
			if (calendarParent != null)
			{
				await calendarParent.ArrangePanel.OnSectionStatusChanged(calendarDisplayViewModel.IsOpened, calendarDisplayViewModel);
			}
		}
	}

	public void SetTitlePosition(int column, int columnSpan, double dayWidth, bool isEvent)
	{
		if (column < 0 && column + columnSpan > 0)
		{
			TaskBarContainer.Margin = new Thickness(-1.0 * (double)column * dayWidth + 4.0 + (double)CalendarGeoHelper.TaskTitleLeft, 0.0, 6.0, 0.0);
		}
		else
		{
			TaskBarContainer.Margin = new Thickness(4 + CalendarGeoHelper.TaskTitleLeft, 0.0, 6.0, 0.0);
		}
	}

	private void OnMouseMove(object sender, MouseEventArgs e)
	{
		CalendarControl calendarParent = GetCalendarParent();
		if (calendarParent != null && (calendarParent.IsLocked || calendarParent.InOneDay()))
		{
			base.Cursor = Cursors.Hand;
			return;
		}
		Point position = e.GetPosition(this);
		if (position.X < 8.0 || position.X > _container.ActualWidth - 8.0)
		{
			if (Mode == TaskbarMode.Normal && base.DataContext is CalendarDisplayViewModel calendarDisplayViewModel && calendarDisplayViewModel.SourceViewModel.Editable && calendarDisplayViewModel.SourceViewModel.IsTask)
			{
				base.Cursor = Cursors.SizeWE;
			}
		}
		else
		{
			base.Cursor = Cursors.Hand;
		}
	}

	public void SetShowIcon(bool showIcon, bool forceShow)
	{
		_showIcon = showIcon;
		SetIcon(_showIcon || forceShow);
	}

	public void SetIconRatio(double ratio)
	{
		_iconScaleRatio = ratio;
	}

	private void OnPathClick(object sender, MouseButtonEventArgs e)
	{
		if (base.DataContext is CalendarDisplayViewModel calendarDisplayViewModel && (calendarDisplayViewModel.IsConfigShowIcon() || StaticPropertiesModel.Instance.AltKeyDown))
		{
			DisplayType type = calendarDisplayViewModel.Type;
			if ((uint)type <= 3u)
			{
				calendarDisplayViewModel.OnCheckBoxClick(GetToastWindow());
			}
			else
			{
				OnTaskClick(sender, e);
			}
		}
	}

	public void OnDetailKeyUp(Key key)
	{
	}

	public void SetMultiDayStyle(int row)
	{
		_row = row;
		base.Height = CalendarGeoHelper.TaskBarHeight;
		TitleText.FontSize = CalendarGeoHelper.CalTitleFontSize;
		TitleText.LineHeight = 1.3 * CalendarGeoHelper.CalTitleFontSize;
		SetValue(Canvas.TopProperty, (double)(row * CalendarGeoHelper.TaskBarHeight) * 1.0);
		if (_path != null)
		{
			_path.Width = CalendarGeoHelper.CalTitleIconSize * _iconScaleRatio;
			_path.Height = CalendarGeoHelper.CalTitleIconSize * _iconScaleRatio;
		}
	}

	public void SetFontSize()
	{
		base.Height = CalendarGeoHelper.TaskBarHeight;
		TitleText.FontSize = CalendarGeoHelper.CalTitleFontSize;
		TitleText.LineHeight = 1.3 * CalendarGeoHelper.CalTitleFontSize;
		if (_path != null)
		{
			_path.Width = CalendarGeoHelper.CalTitleIconSize * _iconScaleRatio;
			_path.Height = CalendarGeoHelper.CalTitleIconSize * _iconScaleRatio;
		}
		TaskBarContainer.Margin = new Thickness(4 + CalendarGeoHelper.TaskTitleLeft, 0.0, 6.0, 0.0);
	}

	public void OnTaskPropertyChanged(string propertyName)
	{
		switch (propertyName)
		{
		case "BackColor":
		case "BackBorderColor":
		case "BackBorderThickness":
			InvalidateVisual();
			break;
		}
	}

	private void InitUi(bool isDragMode)
	{
		SetBinding(FrameworkElement.ToolTipProperty, "Tips");
		base.VerticalAlignment = VerticalAlignment.Top;
		base.HorizontalAlignment = HorizontalAlignment.Left;
		SetResourceReference(FrameworkElement.TagProperty, "IsDarkTheme");
		base.Height = 20.0;
		base.Cursor = Cursors.Hand;
		_container = new Grid
		{
			Margin = new Thickness(3.0, 1.0, 3.0, 1.0),
			Background = Brushes.Transparent
		};
		Child = _container;
		if (!isDragMode)
		{
			SetBinding(UIElement.OpacityProperty, "Opacity");
		}
		_container.MouseMove += OnMouseMove;
		InitTaskContainer();
	}

	private void InitTaskContainer()
	{
		TaskBarContainer = new DockPanel
		{
			VerticalAlignment = VerticalAlignment.Center,
			HorizontalAlignment = HorizontalAlignment.Stretch,
			Margin = new Thickness(4.0, 0.0, 6.0, 0.0)
		};
		_container.Children.Add(TaskBarContainer);
		InitText();
	}

	private void InitText()
	{
		TitleText = new EmjTextBlock
		{
			FontSize = 12.0,
			ClipToBounds = true,
			LineHeight = 16.0,
			Padding = new Thickness(0.0, 0.0, 0.0, 0.5),
			Margin = new Thickness(1.5, 0.0, 0.0, 0.0),
			VerticalAlignment = VerticalAlignment.Center,
			IsHitTestVisible = false
		};
		TitleText.SetBinding(EmjTextBlock.TextProperty, "Title");
		TitleText.SetBinding(TextBlock.ForegroundProperty, "TitleColor");
		ContentControl contentControl = new ContentControl();
		contentControl.SetValue(DockPanel.DockProperty, Dock.Right);
		contentControl.SetBinding(UIElement.VisibilityProperty, "TimeTextVisibility");
		TaskBarContainer.Children.Add(contentControl);
		TimeText = new TextBlock
		{
			Padding = new Thickness(4.0, 0.0, 0.0, 0.0),
			VerticalAlignment = VerticalAlignment.Center,
			HorizontalAlignment = HorizontalAlignment.Left,
			IsHitTestVisible = false
		};
		TimeText.SetBinding(TextBlock.TextProperty, "TimeText");
		TimeText.SetBinding(UIElement.OpacityProperty, new Binding("Opacity")
		{
			Source = TitleText
		});
		TimeText.SetBinding(TextBlock.FontSizeProperty, new Binding("FontSize")
		{
			Source = TitleText,
			Converter = (IValueConverter)FindResource("MathConverter"),
			ConverterParameter = "@VALUE-2"
		});
		TimeText.SetBinding(TextBlock.ForegroundProperty, "TimeColor");
		contentControl.Content = TimeText;
		TaskBarContainer.Children.Add(TitleText);
	}

	public void SetIcon(bool show)
	{
		if (show)
		{
			if (TaskBarContainer.Children.Count == 0 || !(TaskBarContainer.Children[0] is Border) || TaskBarContainer.Children[0] == _foldBorder)
			{
				Border border = new Border
				{
					VerticalAlignment = VerticalAlignment.Stretch,
					HorizontalAlignment = HorizontalAlignment.Left,
					Cursor = Cursors.Hand,
					Background = Brushes.Transparent
				};
				border.PreviewMouseLeftButtonUp += OnPathClick;
				border.SetValue(DockPanel.DockProperty, Dock.Left);
				TaskBarContainer.Children.Insert(0, border);
				_path = new Path
				{
					Stretch = Stretch.Uniform,
					Width = CalendarGeoHelper.CalTitleIconSize,
					Height = CalendarGeoHelper.CalTitleIconSize,
					VerticalAlignment = VerticalAlignment.Center,
					Margin = new Thickness(0.0, 0.0, 1.5, 0.5),
					Opacity = 1.0
				};
				_path.SetBinding(Shape.FillProperty, "IconColor");
				_path.SetBinding(Path.DataProperty, "Icon");
				_path.SetBinding(UIElement.VisibilityProperty, "IconVisible");
				border.Child = _path;
			}
		}
		else
		{
			_path = null;
			if (TaskBarContainer.Children.Count > 0 && TaskBarContainer.Children[0] is Border border2 && border2 != _foldBorder)
			{
				TaskBarContainer.Children.Remove(border2);
			}
		}
	}

	public void SetDragBackRect(bool show)
	{
		if (show)
		{
			if (_container.Children.Count == 0 || !(_container.Children[0] is Rectangle))
			{
				Rectangle element = new Rectangle
				{
					Opacity = 0.8,
					RadiusX = 3.0,
					RadiusY = 3.0
				};
				SetResourceReference(Shape.FillProperty, "WindowBackgroundSecondary");
				_container.Children.Insert(0, element);
			}
		}
		else if (_container.Children.Count > 0 && _container.Children[0] is Rectangle element2)
		{
			_container.Children.Remove(element2);
		}
	}

	protected override void OnRender(DrawingContext dc)
	{
		double top = BarMargin.Top;
		double bottom = BarMargin.Bottom;
		base.OnRender(dc);
		if (!(base.ActualWidth < 6.0) && !(base.ActualHeight < 6.0) && base.DataContext is CalendarDisplayViewModel calendarDisplayViewModel)
		{
			if (calendarDisplayViewModel.BatchSelected)
			{
				dc.DrawRoundedRectangle(ThemeUtil.GetColor(calendarDisplayViewModel._inDark ? "DarkBrush" : "LightBrush"), null, new Rect(3.0, top, base.ActualWidth - 6.0, base.ActualHeight - top - bottom), 4.0, 4.0);
			}
			dc.DrawRoundedRectangle(calendarDisplayViewModel.BackColor, null, new Rect(3.0, top, base.ActualWidth - 6.0, base.ActualHeight - top - bottom), 4.0, 4.0);
			LocalSettings settings = LocalSettings.Settings;
			if (settings != null && settings.UserPreference?.CalendarConfig?.ItemStyleType == CalendarConfig.CalendarItemStyle.Modern && calendarDisplayViewModel.BackBorderThickness.Top <= 0.0)
			{
				PathGeometry pathGeometry = new PathGeometry();
				Point startPoint = new Point(3.0, 3.0 + top);
				PathFigure pathFigure = new PathFigure
				{
					StartPoint = startPoint
				};
				pathFigure.Segments.Add(new ArcSegment
				{
					Point = new Point(5.0, top),
					Size = new Size(3.0, 3.0 + top),
					SweepDirection = SweepDirection.Clockwise
				});
				pathFigure.Segments.Add(new LineSegment
				{
					Point = new Point(5.0, base.ActualHeight - bottom)
				});
				pathFigure.Segments.Add(new ArcSegment
				{
					Point = new Point(3.0, base.ActualHeight - 3.0 - bottom),
					Size = new Size(3.0, 3.0 + top),
					SweepDirection = SweepDirection.Clockwise
				});
				pathGeometry.Figures.Add(pathFigure);
				dc.DrawGeometry(calendarDisplayViewModel.LeftRectColor, new Pen(), pathGeometry);
			}
			if (!InDragPopup && !calendarDisplayViewModel.Dragging && calendarDisplayViewModel.BackBorderColor != null && calendarDisplayViewModel.BackBorderThickness.Top > 0.0)
			{
				dc.DrawRoundedRectangle(null, new Pen(calendarDisplayViewModel.BackBorderColor, calendarDisplayViewModel.BackBorderThickness.Top), new Rect(3.5, top + 0.5, base.ActualWidth - 7.0, base.ActualHeight - top - bottom - 1.0), 4.0, 4.0);
			}
		}
	}
}
