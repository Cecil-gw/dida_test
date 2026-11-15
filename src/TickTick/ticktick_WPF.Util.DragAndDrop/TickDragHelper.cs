using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Effects;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Calendar;
using ticktick_WPF.Views.CheckList;
using ticktick_WPF.Views.Kanban.Item;
using ticktick_WPF.Views.Project;
using ticktick_WPF.Views.TaskList;
using ticktick_WPF.Views.Timeline;

namespace ticktick_WPF.Util.DragAndDrop;

public class TickDragHelper
{
	private static bool _dropped;

	private static bool _showDragWindow;

	private static Point _startDragPoint;

	private static Point _dragPoint;

	private static TimelineDragCell _timelineCell;

	private static Border _listSection;

	private static Border _kanbanDragItem;

	private static TaskPopupItem _listTaskDragPopup;

	private static Grid _taskBarGrid;

	private static TaskBar _taskBar;

	private static Grid _taskCellGrid;

	private static TaskCell _taskCell;

	private static TextBlock _numText;

	private static Popup _window;

	private static bool _dragging;

	private static TaskCompletionSource<bool> _dragCompletionSource;

	private static bool _preDragLeaveCalendar;

	private static Window _dragFromWindow;

	public static double DragWidth;

	public static FrameworkElement DragFrom { get; set; }

	public static HashSet<string> DraggingTaskIds { get; set; }

	public static bool Dragging => _dragging;

	public static bool Dropped => _dropped;

	private static Cursor cursorDrag { get; set; }

	private static Cursor cursorCopy { get; set; }

	private static bool isCopy { get; set; }

	public static object DragData { get; set; }

	public static bool DragOverCalendar { get; set; }

	public static event EventHandler<MouseEventArgs> DragDrop;

	public static event EventHandler<MouseEventArgs> DragMoving;

	public static Task WaitForDragCompletedAsync()
	{
		if (!_dragging)
		{
			return Task.CompletedTask;
		}
		return _dragCompletionSource.Task;
	}

	private static TaskCompletionSource<bool> CreateDragCompletionSource()
	{
		return new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
	}

	private static TaskCompletionSource<bool> CreateCompletedDragSource()
	{
		TaskCompletionSource<bool> taskCompletionSource = CreateDragCompletionSource();
		taskCompletionSource.SetResult(result: true);
		return taskCompletionSource;
	}

	private static void CompleteDrag()
	{
		_dragging = false;
		_dragCompletionSource.TrySetResult(result: true);
	}

	static TickDragHelper()
	{
		_dropped = true;
		_showDragWindow = true;
		_dragCompletionSource = CreateCompletedDragSource();
		try
		{
			cursorDrag = new Cursor(Application.GetResourceStream(new Uri("Resources/Cursor/Drag.cur", UriKind.Relative)).Stream);
			cursorCopy = new Cursor(Application.GetResourceStream(new Uri("Resources/Cursor/Drag Copy.cur", UriKind.Relative)).Stream);
		}
		catch (Exception)
		{
		}
	}

	public static async Task StartDragItem(FrameworkElement view, object item, List<string> draggingIds, double width, Point point = default(Point))
	{
		if (_dragging)
		{
			return;
		}
		_dragCompletionSource = CreateDragCompletionSource();
		_dragging = true;
		ClearUi();
		DraggingTaskIds = new HashSet<string>(draggingIds ?? new List<string>());
		DragWidth = width;
		FrameworkElement dragControl = GetDragControl(item, width);
		if (dragControl != null && DragData == null)
		{
			dragControl.Margin = new Thickness(0.0);
			SetDragProperties(view, item);
			_startDragPoint = point;
			_dragPoint = point;
			_window = GetDragWindow(dragControl);
			_dragFromWindow = Window.GetWindow(view);
			if (_dragFromWindow != null)
			{
				_window.PlacementTarget = _dragFromWindow;
				Popup window = _window;
				if (window != null)
				{
					Point position = Mouse.GetPosition(_dragFromWindow);
					window.VerticalOffset = position.Y - _dragPoint.Y;
					window.HorizontalOffset = position.X - _dragPoint.X;
				}
				isCopy = false;
				_dragFromWindow.Cursor = cursorDrag;
				if (_showDragWindow)
				{
					_window.IsOpen = true;
				}
				_dragFromWindow.CaptureMouse();
				_dropped = false;
				_dragFromWindow.MouseLeftButtonUp += OnDragMouseUp;
				_dragFromWindow.MouseMove += OnDragMouseMove;
				while (_dragging)
				{
					await Task.Delay(30);
				}
			}
		}
		CompleteDrag();
	}

	private static void ClearUi()
	{
		if (_window != null)
		{
			_window.Child = null;
			_window.IsOpen = false;
			_window = null;
		}
		_timelineCell = null;
		_listSection = null;
		_kanbanDragItem = null;
		_listTaskDragPopup = null;
		_taskBarGrid = null;
		_taskBar = null;
		_taskCell = null;
		_taskCellGrid = null;
		_numText = null;
	}

	private static void OnDragMouseMove(object sender, MouseEventArgs e)
	{
		if (e.LeftButton == MouseButtonState.Released)
		{
			OnDragMouseUp(sender, e);
			return;
		}
		if (!_showDragWindow && _window != null)
		{
			_window.IsOpen = false;
		}
		TickDragHelper.DragMoving?.Invoke(sender, e);
		Point position = e.GetPosition(_dragFromWindow);
		Popup window = _window;
		if (window == null)
		{
			return;
		}
		window.VerticalOffset = position.Y - _dragPoint.Y;
		window.HorizontalOffset = position.X - _dragPoint.X;
		bool flag = Utils.IfCtrlPressed();
		if (flag != isCopy)
		{
			isCopy = flag;
			if (_dragFromWindow != null)
			{
				_dragFromWindow.Cursor = (isCopy ? cursorCopy : cursorDrag);
			}
		}
		if (_showDragWindow)
		{
			if (!window.IsOpen)
			{
				window.IsOpen = true;
			}
			else if (window.Child.Opacity == 0.0)
			{
				window.Child.Opacity = 1.0;
			}
		}
	}

	private static async void OnDragMouseUp(object sender, MouseEventArgs e)
	{
		if (!_dropped)
		{
			_dropped = true;
			if (_window != null)
			{
				_window.IsOpen = false;
			}
			TickDragHelper.DragDrop?.Invoke(sender, e);
			await Task.Delay(50);
			if (_dragFromWindow != null)
			{
				_dragFromWindow.Cursor = null;
				_dragFromWindow.ReleaseMouseCapture();
				_dragFromWindow.MouseLeftButtonUp -= OnDragMouseUp;
				_dragFromWindow.MouseMove -= OnDragMouseMove;
				_dragFromWindow = null;
			}
			SetDragProperties(null, null);
			ClearUi();
			DraggingTaskIds = new HashSet<string>();
			CompleteDrag();
		}
	}

	private static void SetDragProperties(FrameworkElement view, object item)
	{
		if (item != null)
		{
			DragFrom = view;
			DragData = item;
			_showDragWindow = !(item is TaskCellViewModel);
			StaticPropertiesModel.Instance.WindowAllowDrop = true;
		}
		else
		{
			DragFrom = null;
			DragData = null;
			StaticPropertiesModel.Instance.WindowAllowDrop = false;
		}
	}

	private static Popup GetDragWindow(UIElement ui)
	{
		if (_window != null)
		{
			_window.Child = null;
		}
		return new Popup
		{
			AllowsTransparency = true,
			StaysOpen = true,
			Placement = PlacementMode.Relative,
			MinHeight = 40.0,
			Child = ui
		};
	}

	private static void OnSourceInitialized(object sender, EventArgs e)
	{
		if (sender is Window window)
		{
			NativeUtils.SetWindowExTransparent(new WindowInteropHelper(window).Handle);
		}
	}

	private static FrameworkElement GetDragControl(object item, double width)
	{
		if (!(item is TimelineCellViewModel cellModel))
		{
			if (!(item is DisplayItemModel displayItemModel))
			{
				if (!(item is CheckItemViewModel))
				{
					if (!(item is ProjectItemViewModel))
					{
						CalendarDisplayViewModel calendarDisplayViewModel;
						if (!(item is TaskCellViewModel taskCellViewModel))
						{
							calendarDisplayViewModel = item as CalendarDisplayViewModel;
							if (calendarDisplayViewModel == null)
							{
								return null;
							}
						}
						else
						{
							if (!taskCellViewModel.BatchSelected)
							{
								if (_taskCellGrid == null)
								{
									_taskCellGrid = new Grid();
									_taskCell = new TaskCell(taskCellViewModel, isMove: true, width >= 80.0)
									{
										Margin = new Thickness(0.0, 4.0, 4.0, 0.0)
									};
									_taskCellGrid.Children.Add(_taskCell);
								}
								else
								{
									_taskCell.SetBinding(FrameworkElement.HeightProperty, "Height");
									_taskCell.DataContext = taskCellViewModel;
								}
								_dragPoint = new Point((_startDragPoint.X > width) ? width : _startDragPoint.X, (_startDragPoint.Y > taskCellViewModel.Height) ? (taskCellViewModel.Height + 4.0) : (_startDragPoint.Y + 4.0));
								return _taskCellGrid;
							}
							calendarDisplayViewModel = (CalendarDisplayViewModel)item;
						}
						if (_taskBarGrid == null)
						{
							_taskBarGrid = new Grid();
							_taskBar = new TaskBar
							{
								InDragPopup = true,
								Margin = new Thickness(10.0),
								Effect = new DropShadowEffect
								{
									BlurRadius = 12.0,
									Opacity = 0.2,
									ShadowDepth = 4.0,
									Direction = 270.0
								}
							};
							_taskBarGrid.Children.Add(_taskBar);
						}
						if (DraggingTaskIds != null && DraggingTaskIds.Count > 0 && calendarDisplayViewModel.BatchSelected)
						{
							_taskBar.TitleText.Text = string.Format(Utils.GetString("MultiTasks"), DraggingTaskIds.Count);
							calendarDisplayViewModel.ShowTimeText = false;
						}
						bool flag = !calendarDisplayViewModel.IsTaskIconType || calendarDisplayViewModel.WidthEnoughShowIcon;
						_taskBar.SetShowIcon(!calendarDisplayViewModel.BatchSelected && flag, !calendarDisplayViewModel.BatchSelected && calendarDisplayViewModel.ForceShowIcon);
						_taskBar.Width = width;
						_dragPoint = new Point((_startDragPoint.X > width) ? (width / 2.0) : _startDragPoint.X, (_startDragPoint.Y > 8.0) ? 18.0 : (_startDragPoint.Y + 10.0));
						_taskBar.DataContext = calendarDisplayViewModel;
						return _taskBarGrid;
					}
					return new ProjectDragPopupContent
					{
						Width = width,
						HorizontalAlignment = HorizontalAlignment.Left,
						DataContext = item
					};
				}
				Border border = new Border();
				border.CornerRadius = new CornerRadius(4.0);
				border.Effect = new DropShadowEffect
				{
					BlurRadius = 8.0,
					Opacity = 0.1,
					ShadowDepth = 2.0
				};
				border.Width = width;
				border.VerticalAlignment = VerticalAlignment.Top;
				border.Height = 40.0;
				border.SetResourceReference(Border.BackgroundProperty, "TaskDragPopupBackground");
				border.Child = new DragCheckItem
				{
					DataContext = item,
					VerticalAlignment = VerticalAlignment.Top
				};
				return border;
			}
			if (displayItemModel.IsSection)
			{
				if (_listSection == null)
				{
					_listSection = new Border
					{
						CornerRadius = new CornerRadius(4.0),
						Effect = new DropShadowEffect
						{
							BlurRadius = 12.0,
							Opacity = 0.1,
							ShadowDepth = 2.0,
							Direction = 300.0
						},
						VerticalAlignment = VerticalAlignment.Top
					};
					_listSection.SetResourceReference(Border.BackgroundProperty, "TaskDragPopupBackground");
					_listSection.Child = new SectionItemControl();
				}
				_listSection.Width = width;
				_listSection.DataContext = displayItemModel;
				return _listSection;
			}
			DisplayItemModel displayItemModel2 = displayItemModel;
			if (displayItemModel2.InKanban)
			{
				if (_kanbanDragItem == null)
				{
					_kanbanDragItem = new Border
					{
						Child = new KanbanItemPopupView
						{
							IsHitTestVisible = false,
							VerticalAlignment = VerticalAlignment.Stretch
						}
					};
					_kanbanDragItem.Margin = new Thickness(0.0, 0.0, 0.0, 0.0);
				}
				_kanbanDragItem.Width = width + 20.0;
				_kanbanDragItem.DataContext = displayItemModel2;
				_dragPoint = new Point((_startDragPoint.X > width - 30.0) ? (width - 30.0) : _startDragPoint.X, (_startDragPoint.Y > 50.0) ? 50.0 : _startDragPoint.Y);
				return _kanbanDragItem;
			}
			DisplayItemModel dataContext = displayItemModel;
			if (_listTaskDragPopup == null)
			{
				_listTaskDragPopup = new TaskPopupItem();
			}
			_listTaskDragPopup.Width = width;
			_listTaskDragPopup.DataContext = dataContext;
			_dragPoint = new Point((_startDragPoint.X > width) ? width : _startDragPoint.X, (_startDragPoint.Y > 40.0) ? 40.0 : _startDragPoint.Y);
			return _listTaskDragPopup;
		}
		if (_timelineCell == null)
		{
			_timelineCell = new TimelineDragCell(cellModel, width);
		}
		else
		{
			_timelineCell.Open(cellModel, width);
		}
		_dragPoint = new Point((_startDragPoint.X > width) ? width : _startDragPoint.X, (_startDragPoint.Y > 30.0) ? 30.0 : _startDragPoint.Y);
		return _timelineCell;
	}

	public static object GetDragData()
	{
		return DragData;
	}

	public static void SetShowDragWindow(bool show)
	{
		_showDragWindow = show;
	}

	public static double GetStartDragLeft()
	{
		return _startDragPoint.X;
	}

	public static void ChangeDragContent(object item, double width)
	{
		if (_window != null)
		{
			FrameworkElement dragControl = GetDragControl(item, width);
			if (dragControl != null && !object.Equals(_window.Child, dragControl))
			{
				_window.Child = dragControl;
			}
		}
	}

	public static void ChangeDragContent(int type, double width, double? maxHeight = null)
	{
		if (_window == null)
		{
			return;
		}
		DisplayItemModel item;
		TaskBaseViewModel vm;
		TaskCellViewModel taskCellViewModel;
		FrameworkElement dragControl;
		FrameworkElement dragControl2;
		FrameworkElement dragControl3;
		switch (type)
		{
		case 0:
			if (object.Equals(_window.Child, _listTaskDragPopup) || object.Equals(_window.Child, _listSection))
			{
				break;
			}
			if (DragData is CalendarDisplayViewModel calendarDisplayViewModel4)
			{
				if (calendarDisplayViewModel4.BatchSelected)
				{
					HashSet<string> draggingTaskIds = DraggingTaskIds;
					if (draggingTaskIds != null && draggingTaskIds.Count > 0)
					{
						item = new DisplayItemModel(new TaskBaseViewModel
						{
							Type = DisplayType.Task,
							Title = string.Format(Utils.GetString("MultiTasks"), DraggingTaskIds.Count),
							Id = calendarDisplayViewModel4.TaskId,
							PinnedTime = calendarDisplayViewModel4.SourceViewModel.PinnedTime
						});
						goto IL_00e3;
					}
				}
				item = new DisplayItemModel(calendarDisplayViewModel4.SourceViewModel);
				goto IL_00e3;
			}
			goto IL_00f9;
		case 1:
			if (object.Equals(_window.Child, _kanbanDragItem))
			{
				_kanbanDragItem.Width = width + 20.0;
				break;
			}
			if (DragData is CalendarDisplayViewModel calendarDisplayViewModel5)
			{
				if (calendarDisplayViewModel5.BatchSelected)
				{
					HashSet<string> draggingTaskIds2 = DraggingTaskIds;
					if (draggingTaskIds2 != null && draggingTaskIds2.Count > 0)
					{
						vm = new TaskBaseViewModel
						{
							Type = DisplayType.Task,
							Title = string.Format(Utils.GetString("MultiTasks"), DraggingTaskIds.Count),
							Id = calendarDisplayViewModel5.TaskId,
							PinnedTime = calendarDisplayViewModel5.SourceViewModel.PinnedTime
						};
						goto IL_01d8;
					}
				}
				vm = calendarDisplayViewModel5.SourceViewModel;
				goto IL_01d8;
			}
			goto IL_01ff;
		case 2:
		{
			if (object.Equals(_window.Child, _taskBar))
			{
				_taskBar.Width = width;
				break;
			}
			FrameworkElement frameworkElement = null;
			if (DragData is DisplayItemModel displayItemModel3)
			{
				CalendarDisplayViewModel calendarDisplayViewModel = CalendarDisplayViewModel.Build(displayItemModel3.SourceViewModel.Copy());
				calendarDisplayViewModel.SourceViewModel.IsAllDay = true;
				frameworkElement = GetDragControl(calendarDisplayViewModel, width);
			}
			if (DragData is TimelineCellViewModel timelineCellViewModel2)
			{
				frameworkElement = GetDragControl(CalendarDisplayViewModel.Build(timelineCellViewModel2.DisplayModel), width);
			}
			if (DragData is CalendarDisplayViewModel calendarDisplayViewModel2)
			{
				CalendarDisplayViewModel calendarDisplayViewModel3 = CalendarDisplayViewModel.Build(calendarDisplayViewModel2.SourceViewModel);
				calendarDisplayViewModel3.BatchSelected = calendarDisplayViewModel2.BatchSelected;
				calendarDisplayViewModel3.ShowTimeText = false;
				frameworkElement = GetDragControl(calendarDisplayViewModel3, width);
			}
			if (frameworkElement != null)
			{
				_window.Child = frameworkElement;
			}
			break;
		}
		case 3:
			{
				if (object.Equals(_window.Child, _taskCell))
				{
					break;
				}
				taskCellViewModel = null;
				if (DragData is DisplayItemModel displayItemModel)
				{
					taskCellViewModel = TaskCellViewModel.Build(new CalendarDisplayModel(displayItemModel.SourceViewModel.Copy()), displayItemModel.StartDate.GetValueOrDefault());
				}
				if (DragData is TimelineCellViewModel timelineCellViewModel)
				{
					taskCellViewModel = TaskCellViewModel.Build(new CalendarDisplayModel(timelineCellViewModel.DisplayModel.Copy()), timelineCellViewModel.DisplayModel.StartDate.GetValueOrDefault());
				}
				if (taskCellViewModel == null)
				{
					break;
				}
				taskCellViewModel.Width = width;
				if (taskCellViewModel.StartDate.HasValue)
				{
					if (!(taskCellViewModel.IsAllDay ?? true) && taskCellViewModel.DueDate.HasValue)
					{
						TimeSpan? timeSpan = taskCellViewModel.DueDate - taskCellViewModel.StartDate;
						if (!timeSpan.HasValue || !(timeSpan.GetValueOrDefault().TotalHours > 24.0))
						{
							CalendarGeoHelper.ResetCellHeightAndVertical(taskCellViewModel, taskCellViewModel.StartDate);
							taskCellViewModel.VerticalOffset = 0.0;
							if (taskCellViewModel.Height == 0.0)
							{
								taskCellViewModel.Height = CalendarGeoHelper.MinPointHeight;
							}
							goto IL_0494;
						}
					}
					taskCellViewModel.Height = CalendarGeoHelper.MinPointHeight;
				}
				else
				{
					taskCellViewModel.Height = CalendarGeoHelper.MinPointHeight;
				}
				goto IL_0494;
			}
			IL_01ff:
			if (DragData is DisplayItemModel displayItemModel2)
			{
				displayItemModel2.InKanban = true;
				_window.Child = GetDragControl(displayItemModel2, width);
			}
			break;
			IL_01d8:
			dragControl = GetDragControl(new DisplayItemModel(vm, showProject: false, isKanbanMode: true), width + 10.0);
			_window.Child = dragControl;
			goto IL_01ff;
			IL_00e3:
			dragControl2 = GetDragControl(item, width);
			_window.Child = dragControl2;
			goto IL_00f9;
			IL_00f9:
			if (DragData is DisplayItemModel displayItemModel4)
			{
				displayItemModel4.InKanban = false;
				_window.Child = GetDragControl(displayItemModel4, width);
			}
			break;
			IL_0494:
			if (maxHeight.HasValue && taskCellViewModel.Height > maxHeight)
			{
				taskCellViewModel.Height = maxHeight.Value;
			}
			dragControl3 = GetDragControl(taskCellViewModel, width);
			_window.Child = dragControl3;
			break;
		}
	}

	public static void SetTaskCellDate(DateTime? startDate, DateTime? dueDate)
	{
		if (_taskCell != null && _taskCell.DataContext is TaskCellViewModel taskCellViewModel)
		{
			taskCellViewModel.SourceViewModel.StartDate = startDate;
			taskCellViewModel.SourceViewModel.DueDate = dueDate;
			taskCellViewModel.SetTimeText(inArrange: false, isSpan: true);
		}
	}

	public static async void RemoveDragOverCalendar()
	{
		if (DragOverCalendar)
		{
			_preDragLeaveCalendar = true;
			await Task.Delay(100);
			if (_preDragLeaveCalendar)
			{
				DragOverCalendar = false;
				StaticPropertiesModel.Instance.NotifyPropertyChanged("DragOverCalendar");
			}
		}
	}

	public static void SetDragOverCalendar()
	{
		if (!DragOverCalendar)
		{
			DragOverCalendar = true;
			StaticPropertiesModel.Instance.NotifyPropertyChanged("DragOverCalendar");
		}
		_preDragLeaveCalendar = false;
	}

	public static void SetDragOpacity(double d)
	{
		if (_window?.Child != null)
		{
			_window.Child.Opacity = d;
		}
	}
}
