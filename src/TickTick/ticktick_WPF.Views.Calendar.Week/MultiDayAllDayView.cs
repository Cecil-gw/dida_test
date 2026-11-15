using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Util.DragAndDrop;
using ticktick_WPF.Util.TouchPad;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Calendar.Month;
using ticktick_WPF.Views.Detail;

namespace ticktick_WPF.Views.Calendar.Week;

public class MultiDayAllDayView : Grid
{
	public DateTime ScrollStartDate;

	private MultiDayView _parent;

	private readonly TouchpadScrollHelper _touchpadScroll;

	private List<MultiDayAllDayGrid> _dayViews = new List<MultiDayAllDayGrid>();

	private ScrollViewer _scroll = new ScrollViewer
	{
		Margin = new Thickness(0.0, 69.0, 0.0, 0.0),
		VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
		CanContentScroll = false
	};

	private readonly Grid _scrollContainer;

	private List<WeekEventModel> _events;

	private MultiDayAllDayItems _itemsContainer;

	private bool _mouseOnItem;

	private bool _dragging;

	private DateTime _dragStartDate;

	private DateTime _currentDragDate;

	private Point _batchStartPoint;

	private Point _prePoint;

	private bool _batchMouseDown;

	private Border _batchBorder;

	private double _checkedWidth;

	private double _checkedHeight;

	private readonly DispatcherTimer _moveViewTimer = new DispatcherTimer(DispatcherPriority.Render);

	public double CurrentOffset => _scroll?.HorizontalOffset ?? 4000.0;

	public MultiDayAllDayView(MultiDayView parent)
	{
		_parent = parent;
		_touchpadScroll = new TouchpadScrollHelper(_scroll);
		base.Margin = new Thickness(60.0, 0.0, 0.0, 0.0);
		base.ClipToBounds = true;
		_scroll.SetResourceReference(FrameworkElement.StyleProperty, "for_scrollviewer");
		_scroll.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;
		base.Children.Add(_scroll);
		_scrollContainer = new Grid();
		ContentControl contentControl = new ContentControl
		{
			Width = 8000.0
		};
		_scrollContainer.Children.Add(contentControl);
		_itemsContainer = new MultiDayAllDayItems(parent)
		{
			VerticalAlignment = VerticalAlignment.Top,
			RenderTransform = new TranslateTransform
			{
				X = 0.0
			}
		};
		base.Background = Brushes.Transparent;
		InitBatchEvent();
		_itemsContainer.Height = 200.0;
		contentControl.Content = _itemsContainer;
		_scroll.Content = _scrollContainer;
		_scroll.SetValue(Panel.ZIndexProperty, 10);
		_scroll.ScrollToHorizontalOffset(4000.0);
		_scroll.SizeChanged += OnScrollSizeChanged;
		_scroll.ScrollChanged += OnScrollChanged;
		_scroll.SetBinding(UIElement.IsHitTestVisibleProperty, StaticPropertiesModel.HitTestBinding);
		base.PreviewMouseLeftButtonDown += OnCellMouseDown;
		base.PreviewMouseLeftButtonUp += OnCellMouseUp;
		base.PreviewMouseWheel += OnPreviewMouseWheel;
		base.Loaded += OnLoaded;
		_scroll.Focusable = false;
	}

	private async void OnLoaded(object sender, RoutedEventArgs e)
	{
		await Task.Delay(50);
		if (Math.Abs(_scroll.HorizontalOffset - 4000.0) > 1.0)
		{
			_scroll.ScrollToHorizontalOffset(4000.0);
		}
	}

	private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
	{
		_itemsContainer.SetScrollOffset(_scroll.VerticalOffset, checkVisible: true);
	}

	private void OnScrollSizeChanged(object sender, SizeChangedEventArgs e)
	{
		_itemsContainer.SetSize(e.NewSize);
	}

	public void SetData(List<MonthDayViewModel> models)
	{
		double num = _parent?.DayViewWidth ?? 0.0;
		for (int i = 0; i < models.Count; i++)
		{
			if (_dayViews.Count > i)
			{
				MultiDayAllDayGrid multiDayAllDayGrid = _dayViews[i];
				MonthDayViewModel dataContext = models[i];
				multiDayAllDayGrid.DataContext = dataContext;
				multiDayAllDayGrid.RenderTransform = new TranslateTransform
				{
					X = (double)(i - 1) * num
				};
			}
		}
	}

	public async void SetEvents(List<WeekEventModel> events, bool resetOffset = true)
	{
		_events = events?.ToList();
		if (resetOffset)
		{
			double num = _parent?.DayViewWidth ?? 0.0;
			if (_itemsContainer.RenderTransform is TranslateTransform translateTransform)
			{
				translateTransform.X = 0.0;
				for (int i = 0; i < _dayViews.Count; i++)
				{
					if (_dayViews[i].RenderTransform is TranslateTransform translateTransform2)
					{
						translateTransform2.X = (double)(i - 1) * num;
					}
				}
			}
			_itemsContainer.SetScrollOffset(_scroll.VerticalOffset);
			_itemsContainer.SetSize(_scroll.RenderSize);
			_itemsContainer.SetDayWidth(num);
		}
		await _itemsContainer.SetItemsSource(events);
	}

	public void CheckDraggingOver()
	{
		if (!TickDragHelper.Dragging)
		{
			return;
		}
		Point position = Mouse.GetPosition(this);
		if (!(position.X > 0.0) || !(position.X < base.ActualWidth) || !(position.Y > 0.0) || !(position.Y < base.ActualHeight))
		{
			return;
		}
		foreach (MultiDayAllDayGrid dayView in _dayViews)
		{
			dayView.CheckDragging();
		}
	}

	public void TryShowAddWindow(DateTime dateTime)
	{
		foreach (MultiDayAllDayGrid dayView in _dayViews)
		{
			if (dayView.DataContext is MonthDayViewModel monthDayViewModel && monthDayViewModel.Date == dateTime)
			{
				monthDayViewModel.State = MonthCellState.Selected;
				_dragStartDate = dateTime;
				_currentDragDate = dateTime;
				ShowAddTaskPopup(dayView);
				break;
			}
		}
	}

	public void SetDisplayDays()
	{
		int num = (_parent?.DisplayDays ?? 7) + 2;
		double num2 = _parent?.DayViewWidth ?? 0.0;
		if (num > _dayViews.Count)
		{
			int count = _dayViews.Count;
			for (int i = 0; i < num - count; i++)
			{
				MultiDayAllDayGrid multiDayAllDayGrid = new MultiDayAllDayGrid
				{
					RenderTransform = new TranslateTransform(),
					Width = num2,
					HorizontalAlignment = HorizontalAlignment.Left
				};
				_dayViews.Add(multiDayAllDayGrid);
				base.Children.Add(multiDayAllDayGrid);
			}
		}
		else
		{
			List<MultiDayAllDayGrid> list = new List<MultiDayAllDayGrid>();
			for (int j = num; j < _dayViews.Count; j++)
			{
				list.Add(_dayViews[j]);
			}
			list.ForEach(delegate(MultiDayAllDayGrid d)
			{
				_dayViews.Remove(d);
				base.Children.Remove(d);
			});
		}
		for (int num3 = 0; num3 < _dayViews.Count; num3++)
		{
			MultiDayAllDayGrid multiDayAllDayGrid2 = _dayViews[num3];
			multiDayAllDayGrid2.RenderTransform = new TranslateTransform
			{
				X = (double)(num3 - 1) * num2
			};
			multiDayAllDayGrid2.Width = num2;
		}
	}

	public void ScrollToTop()
	{
		_scroll?.ScrollToTop();
	}

	public void NotifyEventTitleChanged(string eventId, string text)
	{
		_events?.Where((WeekEventModel model) => model.Data.EventId == eventId).ToList().ForEach(delegate(WeekEventModel item)
		{
			item.Data.SourceViewModel.Title = text;
		});
	}

	public void SetDragStart(DateTime date)
	{
		_dragging = true;
		_dragStartDate = date;
	}

	public bool IsDragging()
	{
		return _dragging;
	}

	public DateTime GetDragStart()
	{
		return _dragStartDate;
	}

	public DateTime GetCurrentDrag()
	{
		return _currentDragDate;
	}

	public void SetCurrentDragDate(DateTime date)
	{
		if (_dragging)
		{
			_currentDragDate = date;
			RenderSelected();
		}
	}

	private void RenderSelected()
	{
		DateTime dateTime = _dragStartDate;
		DateTime dateTime2 = _dragStartDate;
		if (!Utils.IsEmptyDate(_currentDragDate))
		{
			dateTime = ((_currentDragDate.Date > _dragStartDate) ? _dragStartDate : _currentDragDate);
			dateTime2 = ((_currentDragDate.Date < _dragStartDate) ? _dragStartDate : _currentDragDate);
		}
		foreach (MultiDayAllDayGrid dayView in _dayViews)
		{
			if (dayView.DataContext is MonthDayViewModel { Date: var date } monthDayViewModel)
			{
				if (date.Date >= dateTime && monthDayViewModel.Date <= dateTime2)
				{
					monthDayViewModel.State = MonthCellState.Selected;
				}
				else
				{
					monthDayViewModel.State = MonthCellState.Normal;
				}
			}
		}
	}

	public void CancelDrag()
	{
		_dragging = false;
		foreach (MultiDayAllDayGrid dayView in _dayViews)
		{
			if (dayView.DataContext is MonthDayViewModel monthDayViewModel)
			{
				monthDayViewModel.State = MonthCellState.Normal;
			}
		}
	}

	private void OnCellMouseDown(object sender, MouseButtonEventArgs e)
	{
		MultiDayView parent = _parent;
		if (parent != null && parent.IsLocked)
		{
			return;
		}
		if (Utils.IfCtrlPressed())
		{
			OnBatchMouseDown(sender, e);
		}
		else
		{
			if (Utils.GetMousePointVisibleItem<TaskBar>(e, this) != null)
			{
				return;
			}
			CalendarControl calendarControl = Utils.FindParent<CalendarControl>(this);
			if (calendarControl != null && calendarControl.ClearCopiedTaskSelection())
			{
				return;
			}
			MultiDayView parent2 = _parent;
			if (parent2 == null || !parent2.ExitBatchSelect())
			{
				if (_scroll.IsMouseOver)
				{
					_scroll.IsHitTestVisible = false;
				}
				MultiDayAllDayGrid mousePointVisibleItem = Utils.GetMousePointVisibleItem<MultiDayAllDayGrid>(e, this);
				if (mousePointVisibleItem != null && PopupStateManager.CanShowAddPopup() && !mousePointVisibleItem.IsDateMouseOver)
				{
					base.MouseMove += OnCellMouseMove;
					CaptureMouse();
					e.Handled = true;
					SetDragStart(mousePointVisibleItem.GetDate());
					SetCurrentDragDate(default(DateTime));
					PopupStateManager.StartSelection();
				}
			}
		}
	}

	private void OnCellMouseUp(object sender, MouseButtonEventArgs e)
	{
		if (base.IsMouseCaptured)
		{
			ReleaseMouseCapture();
		}
		_scroll.SetBinding(UIElement.IsHitTestVisibleProperty, StaticPropertiesModel.HitTestBinding);
		base.MouseMove -= OnCellMouseMove;
		if (!PopupStateManager.IsInSelection() || !PopupStateManager.CanShowAddPopup())
		{
			return;
		}
		MultiDayAllDayGrid mousePointVisibleItem = Utils.GetMousePointVisibleItem<MultiDayAllDayGrid>(e, this);
		if (mousePointVisibleItem != null && mousePointVisibleItem.DataContext is MonthDayViewModel monthDayViewModel && (monthDayViewModel.State == MonthCellState.Selected || monthDayViewModel.Date == _dragStartDate))
		{
			e.Handled = true;
			monthDayViewModel.State = MonthCellState.Selected;
			ShowAddTaskPopup(mousePointVisibleItem);
			return;
		}
		for (int num = _dayViews.Count - 1; num >= 0; num--)
		{
			MultiDayAllDayGrid multiDayAllDayGrid = _dayViews[num];
			if (multiDayAllDayGrid.DataContext is MonthDayViewModel { State: MonthCellState.Selected })
			{
				ShowAddTaskPopup(multiDayAllDayGrid);
				e.Handled = true;
				break;
			}
		}
	}

	private void OnCellMouseMove(object sender, MouseEventArgs e)
	{
		if (e.LeftButton != MouseButtonState.Pressed && PopupStateManager.IsInSelection())
		{
			PopupStateManager.StopSelection();
		}
		if (PopupStateManager.IsInSelection() && PopupStateManager.CanShowAddPopup())
		{
			MultiDayAllDayGrid mousePointVisibleItem = Utils.GetMousePointVisibleItem<MultiDayAllDayGrid>(e, this);
			if (mousePointVisibleItem != null)
			{
				SetCurrentDragDate(mousePointVisibleItem.GetDate());
			}
		}
	}

	private void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
	{
		if (!PopupStateManager.CanShowAddPopup())
		{
			e.Handled = true;
		}
		else if (Utils.IfShiftPressed())
		{
			_parent?.OnShiftMouseWheel(e.Delta);
			e.Handled = true;
		}
		else if (_scroll.IsMouseOver)
		{
			e.Handled = _touchpadScroll.TryScroll(e.Delta);
		}
	}

	public void OnDoubleFingerTouch()
	{
		_touchpadScroll.OnDoubleFingerTouch();
	}

	public void ShowAddTaskPopup(MultiDayAllDayGrid dayGrid)
	{
		PopupStateManager.StopSelection();
		if (!PopupStateManager.CanShowAddPopup())
		{
			return;
		}
		TaskDetailViewModel taskDetailViewModel = TaskDetailViewModel.BuildInitModel(null);
		DateTime dragStartDate = _dragStartDate;
		DateTime currentDragDate = _currentDragDate;
		if (dragStartDate == currentDragDate)
		{
			taskDetailViewModel.SourceViewModel.StartDate = dragStartDate;
		}
		else if (dragStartDate > currentDragDate)
		{
			taskDetailViewModel.SourceViewModel.StartDate = currentDragDate;
			taskDetailViewModel.SourceViewModel.DueDate = dragStartDate.Date.AddDays(1.0);
		}
		else
		{
			taskDetailViewModel.SourceViewModel.StartDate = dragStartDate;
			taskDetailViewModel.SourceViewModel.DueDate = currentDragDate.Date.AddDays(1.0);
		}
		CalendarControl calendarControl = Utils.FindParent<CalendarControl>(this);
		string calendarDefaultAddProjectId = CalendarUtils.GetCalendarDefaultAddProjectId(calendarControl != null && calendarControl.ViewSourceType == CalendarViewSourceType.WithList);
		taskDetailViewModel.SourceViewModel.ProjectId = calendarDefaultAddProjectId;
		TaskDetailPopup taskDetailPopup = new TaskDetailPopup
		{
			IsCalendarContext = true
		};
		IToastShowWindow parentWindow = GetParentWindow();
		if (parentWindow != null)
		{
			if (PopupStateManager.LastTarget == this)
			{
				return;
			}
			taskDetailPopup.DependentWindow = parentWindow;
		}
		taskDetailPopup.Disappear -= OnDetailClosed;
		taskDetailPopup.Disappear += OnDetailClosed;
		taskDetailPopup.CalendarAddFlowClosed -= OnCalendarAddFlowClosed;
		taskDetailPopup.CalendarAddFlowClosed += OnCalendarAddFlowClosed;
		taskDetailPopup.TaskSaved += OnTaskAdded;
		taskDetailPopup.Show(taskDetailViewModel, string.Empty, new TaskWindowDisplayArgs(dayGrid, dayGrid.ActualWidth + 8.0, PopupLocationCalculator.GetMousePoint(dayGrid.ActualWidth < 350.0), 36.0)
		{
			TitleParseMode = TitleDateParseMode.TimeOnlyOnCurrentDate
		});
		_parent?.SetEditing(editing: true);
	}

	private void OnDetailClosed(object sender, string e)
	{
		if (sender is TaskDetailPopup taskDetailPopup)
		{
			if (taskDetailPopup.DelayCalendarAddCleanup)
			{
				return;
			}
			taskDetailPopup.Disappear -= OnDetailClosed;
			taskDetailPopup.CalendarAddFlowClosed -= OnCalendarAddFlowClosed;
			taskDetailPopup.TaskSaved -= OnTaskAdded;
		}
		ResetAddPopupState();
	}

	private void OnCalendarAddFlowClosed(object sender, CalendarEventModel e)
	{
		if (sender is TaskDetailPopup taskDetailPopup)
		{
			taskDetailPopup.CalendarAddFlowClosed -= OnCalendarAddFlowClosed;
			taskDetailPopup.Disappear -= OnDetailClosed;
			taskDetailPopup.TaskSaved -= OnTaskAdded;
		}
		ResetAddPopupState();
		ShowCalendarEventHiddenToast(e);
	}

	private void ResetAddPopupState()
	{
		foreach (MultiDayAllDayGrid dayView in _dayViews)
		{
			if (dayView.DataContext is MonthDayViewModel monthDayViewModel)
			{
				monthDayViewModel.State = MonthCellState.Normal;
			}
		}
		CancelDrag();
		_parent?.SetEditing(editing: false);
	}

	private void ShowCalendarEventHiddenToast(CalendarEventModel calEvent)
	{
		CalendarControl calendarControl = Utils.FindParent<CalendarControl>(this);
		string text = CalendarUtils.CheckCalendarEventCanShown(calEvent, calendarControl != null && calendarControl.ViewSourceType == CalendarViewSourceType.WithList);
		if (!string.IsNullOrEmpty(text))
		{
			GetParentWindow()?.TryToastString(null, text);
		}
	}

	private async void OnTaskAdded(object sender, string taskId)
	{
		if (sender is TaskDetailWindow taskDetailWindow)
		{
			taskDetailWindow.TaskSaved -= OnTaskAdded;
		}
		CalendarControl calendarControl = Utils.FindParent<CalendarControl>(this);
		string text = CalendarUtils.CheckTaskCanShown(taskId, calendarControl != null && calendarControl.ViewSourceType == CalendarViewSourceType.WithList);
		if (!string.IsNullOrEmpty(text))
		{
			GetParentWindow()?.TryToastString(null, text);
		}
	}

	private IToastShowWindow GetParentWindow()
	{
		return Utils.FindParent<IToastShowWindow>(this);
	}

	public void ClearDragging()
	{
		_events.ForEach(delegate(WeekEventModel e)
		{
			e.Data.Dragging = false;
		});
	}

	public void ClearItems(bool onlyData)
	{
		_itemsContainer.Clear();
		_events?.Clear();
	}

	public void OnTouchScroll(double offset, bool showWeekend)
	{
		if (_itemsContainer.RenderTransform is TranslateTransform translateTransform)
		{
			translateTransform.X += offset;
			_itemsContainer.SetScrollOffset(_scroll.VerticalOffset, checkVisible: true);
		}
		bool flag = false;
		double num = _parent?.DayViewWidth ?? base.ActualWidth;
		int num2 = _parent?.DisplayDays ?? 0;
		foreach (MultiDayAllDayGrid dayView in _dayViews)
		{
			if (!(dayView.RenderTransform is TranslateTransform translateTransform2))
			{
				continue;
			}
			double num3 = translateTransform2.X + offset;
			DateTime date = dayView.GetDate();
			DateTime dateTime = date;
			if (num3 <= -1.5 * num)
			{
				num3 += (double)(num2 + 2) * num;
				flag = true;
				dateTime = date.AddDays(num2 + 2);
				if (!showWeekend)
				{
					dateTime = dateTime.AddDays((date.DayOfWeek == DayOfWeek.Friday || date.DayOfWeek == DayOfWeek.Thursday) ? 4 : 2);
				}
			}
			if (num3 >= ((double)num2 + 0.5) * num + 2.0)
			{
				num3 -= (double)(num2 + 2) * num;
				flag = true;
				dateTime = date.AddDays(-2 - num2);
				if (!showWeekend)
				{
					dateTime = dateTime.AddDays((date.DayOfWeek == DayOfWeek.Monday || date.DayOfWeek == DayOfWeek.Tuesday) ? (-4) : (-2));
				}
			}
			if (dateTime != date && dayView.DataContext is MonthDayViewModel monthDayViewModel)
			{
				monthDayViewModel.SetDate(dateTime);
			}
			dayView.Offset = num3;
			translateTransform2.X = num3;
		}
		if (!flag)
		{
			return;
		}
		_dayViews.Sort((MultiDayAllDayGrid a, MultiDayAllDayGrid b) => a.Offset.CompareTo(b.Offset));
		DateTime? startDate = GetStartDate();
		if (!startDate.HasValue)
		{
			return;
		}
		DateTime currentMonthDate = DateUtils.GetCurrentMonthDate(startDate.Value, startDate.Value.AddDays(num2 - 1));
		foreach (MultiDayAllDayGrid dayView2 in _dayViews)
		{
			dayView2.SetMonth(currentMonthDate);
		}
	}

	public DateTime? GetStartDate()
	{
		if (_dayViews.Count > 1)
		{
			return _dayViews[1].GetDate();
		}
		return null;
	}

	public double GetCurrentOffset()
	{
		if (_itemsContainer.RenderTransform is TranslateTransform translateTransform)
		{
			return -1.0 * translateTransform.X;
		}
		return 0.0;
	}

	public void SetScrollMaxHeight(double newSizeHeight)
	{
		if (newSizeHeight > 70.0)
		{
			_scroll.MaxHeight = newSizeHeight - 70.0;
		}
	}

	public MultiDayAllDayGrid GetDayView(int index)
	{
		if (index + 1 < _dayViews.Count)
		{
			return _dayViews[index + 1];
		}
		return null;
	}

	public void RemoveDraggingItem()
	{
		_itemsContainer.RemoveDraggingItems();
	}

	public void SetDragSideBar(CalendarDisplayViewModel model, DateTime startDate, DateTime endDate, bool dragLeft)
	{
		_itemsContainer.SetDragSideBar(model, startDate, endDate, dragLeft);
	}

	public void OnFontSizeChanged()
	{
		_itemsContainer?.OnFontSizeChanged();
	}

	public void SetShowIcon()
	{
		_itemsContainer?.SetShowIcon();
	}

	public void SetColor()
	{
		_itemsContainer?.SetColor();
	}

	private void InitBatchEvent()
	{
		_moveViewTimer.Interval = TimeSpan.FromMilliseconds(20.0);
		_moveViewTimer.Tick += OnMoveViewTimerOnElapsed;
	}

	private void OnBatchMouseDown(object sender, MouseButtonEventArgs e)
	{
		IShowTaskDetailWindow mousePointItem = Utils.GetMousePointItem<IShowTaskDetailWindow>(e.GetPosition(_itemsContainer), _itemsContainer);
		if (mousePointItem == null || !(mousePointItem is FrameworkElement { Visibility: Visibility.Visible }))
		{
			_batchMouseDown = true;
			_batchStartPoint = e.GetPosition(_scrollContainer);
			if (_batchStartPoint.Y < 0.0)
			{
				_batchStartPoint.Y = 0.0;
			}
			_prePoint = _batchStartPoint;
			Window window = Window.GetWindow(this);
			if (window != null)
			{
				Mouse.Capture(window);
				window.MouseLeftButtonUp -= OnMouseUp;
				window.MouseLeftButtonUp += OnMouseUp;
				window.MouseMove -= OnMouseMove;
				window.MouseMove += OnMouseMove;
			}
			StartMoveView();
			SetBatchSelectRect();
		}
	}

	private void OnMoveViewTimerOnElapsed(object sender, EventArgs e)
	{
		ScrollViewer scroll = _scroll;
		Point position = Mouse.GetPosition(scroll);
		if (position.Y > scroll.ActualHeight)
		{
			scroll.ScrollToVerticalOffset(scroll.VerticalOffset + (position.Y - scroll.ActualHeight) * 0.6);
			ShowBatchSelectBox();
		}
		else if (position.Y < 0.0)
		{
			scroll.ScrollToVerticalOffset(scroll.VerticalOffset + position.Y * 0.6);
			ShowBatchSelectBox();
		}
	}

	private void OnMouseUp(object sender, MouseButtonEventArgs e)
	{
		_batchMouseDown = false;
		StopMoveView();
		Mouse.Capture(null);
		_batchBorder.Visibility = Visibility.Collapsed;
		_batchBorder.Width = 0.0;
		_batchBorder.Height = 0.0;
		_batchBorder.Margin = new Thickness(0.0);
		Window window = Window.GetWindow(this);
		if (window != null)
		{
			window.MouseLeftButtonUp -= OnMouseUp;
			window.MouseMove -= OnMouseMove;
		}
		Dictionary<string, int> selected = _itemsContainer.SetBatchSelectInTemp();
		_parent?.AddBatchSelected(selected);
	}

	private void OnMouseMove(object sender, MouseEventArgs e)
	{
		if (_batchMouseDown && e.LeftButton == MouseButtonState.Pressed)
		{
			ShowBatchSelectBox();
		}
	}

	public void StartMoveView()
	{
		_moveViewTimer.Start();
	}

	public void StopMoveView()
	{
		_moveViewTimer.Stop();
	}

	private void SetBatchSelectRect()
	{
		if (_batchBorder == null)
		{
			_batchBorder = new Border
			{
				Visibility = Visibility.Collapsed,
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Top,
				BorderThickness = new Thickness(1.0),
				CornerRadius = new CornerRadius(4.0),
				Width = 0.0,
				Height = 0.0
			};
			_batchBorder.SetResourceReference(Panel.BackgroundProperty, "BaseColorOpacity5");
			_batchBorder.SetResourceReference(Border.BorderBrushProperty, "BaseColorOpacity20");
			_batchBorder.SetValue(Grid.RowSpanProperty, 2);
			_batchBorder.SetValue(Panel.ZIndexProperty, 100);
			_scrollContainer.Children.Add(_batchBorder);
		}
	}

	private void ShowBatchSelectBox()
	{
		Point position = Mouse.GetPosition(_scrollContainer);
		position.X = Math.Min(Math.Max(4000.0, position.X), 4000.0 + _scroll.ActualWidth);
		position.Y = Math.Min(Math.Max(0.0, position.Y), _scrollContainer.ActualHeight);
		double value = _prePoint.X - position.X;
		if (!(Math.Abs(_prePoint.Y - position.Y) < 2.0) || !(Math.Abs(value) < 2.0))
		{
			_prePoint = position;
			if (_batchBorder.Visibility == Visibility.Collapsed)
			{
				_batchBorder.Visibility = Visibility.Visible;
				_batchBorder.Margin = new Thickness(_batchStartPoint.X, _batchStartPoint.Y, 0.0, 0.0);
			}
			double num = position.X - _batchStartPoint.X;
			double num2 = position.Y - _batchStartPoint.Y;
			if (num < 0.0 || num2 < 0.0)
			{
				_batchBorder.Margin = new Thickness((num < 0.0) ? Math.Max(0.0, _batchStartPoint.X + num) : _batchStartPoint.X, (num2 < 0.0) ? Math.Max(0.0, _batchStartPoint.Y + num2) : _batchStartPoint.Y, 0.0, 0.0);
			}
			else
			{
				_batchBorder.Margin = new Thickness(_batchStartPoint.X, _batchStartPoint.Y, 0.0, 0.0);
			}
			_batchBorder.Width = Math.Abs(num);
			_batchBorder.Height = Math.Abs(num2);
			if (_batchBorder.Width > 0.0)
			{
				TryBatchSelectOnMove(new Rect(_batchBorder.Margin.Left, _batchBorder.Margin.Top, _batchBorder.Width, _batchBorder.Height));
			}
		}
	}

	public void TryBatchSelectOnMove(Rect rect)
	{
		if (!(Math.Abs(rect.Width - _checkedWidth) < 5.0) || !(Math.Abs(rect.Height - _checkedHeight) < 5.0))
		{
			_checkedWidth = rect.Width;
			_checkedHeight = rect.Height;
			_itemsContainer.TryBatchSelectInRect(rect);
		}
	}

	public void ClearBatchSelected()
	{
		_itemsContainer.ClearBatchSelected();
	}

	public void SetBatchDragging(bool dragging)
	{
		_itemsContainer.SetBatchDragging(dragging);
	}

	public void SetDragTasks(List<WeekEventModel> dragTasks)
	{
		_itemsContainer.SetDragItems(dragTasks);
	}

	public void SetBatchSelected(BlockingSet<string> batchSelected)
	{
		_itemsContainer.SetBatchSelected(batchSelected);
	}

	public void SetDropTargets(DateTime start, DateTime end)
	{
		foreach (MultiDayAllDayGrid dayView in _dayViews)
		{
			DateTime date = dayView.GetDate();
			if (date >= start && date <= end)
			{
				dayView.SetDropTarget();
			}
			else
			{
				dayView.ClearDropTarget();
			}
		}
	}

	public void ClearDropTargets()
	{
		foreach (MultiDayAllDayGrid dayView in _dayViews)
		{
			dayView.ClearDropTarget();
		}
	}
}
