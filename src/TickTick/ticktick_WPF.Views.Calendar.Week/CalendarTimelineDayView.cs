using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using Emoji.Wpf;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Util.DragAndDrop;
using ticktick_WPF.Util.KotlinUtils;
using ticktick_WPF.Util.Sync;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Detail;
using ticktick_WPF.Views.Timeline;

namespace ticktick_WPF.Views.Calendar.Week;

public class CalendarTimelineDayView : Grid, IMultiDayGrid
{
	private bool _addPopupShowed;

	private bool _createCellShowed;

	private bool _doubleClick;

	private double _draggingStartY = -1.0;

	private bool _isDuration;

	private bool _mouseClick;

	private bool _loaded;

	private IToastShowWindow _parentWindow;

	private Grid _timePointer;

	private BlockingList<TaskCell> _cells = new BlockingList<TaskCell>();

	private TaskCell _addTaskCell;

	public double Offset;

	private TextBlock _topExtra1;

	private TextBlock _topExtra2;

	private TextBlock _botExtra1;

	private TextBlock _botExtra2;

	private DragAutoScrollHelper _autoScroller;

	private CalendarTimelineView _parent;

	private static ConcurrentBag<string> addedTempTaskIds { get; set; } = new ConcurrentBag<string>();

	private static TaskCompletionSource<bool> showtcs { get; set; }

	public CalendarTimelineDayView(CalendarTimelineView parent)
	{
		_parent = parent;
		base.Background = Brushes.Transparent;
		base.Loaded += delegate
		{
			BindEvents();
		};
		base.Unloaded += delegate
		{
			UnbindEvents();
		};
		SetPointerPosition();
		base.MouseLeftButtonDown += OnLeftButtonDown;
	}

	private void UnbindEvents()
	{
		DataChangedNotifier.PeriodicCheck -= NotifyPointer;
		CalendarGeoHelper.CalendarHourHeightChanged -= OnCalendarHeightChanged;
	}

	private void BindEvents()
	{
		DataChangedNotifier.PeriodicCheck += NotifyPointer;
		CalendarGeoHelper.CalendarHourHeightChanged += OnCalendarHeightChanged;
	}

	private void OnCalendarHeightChanged(object sender, double height)
	{
		ReloadCells(base.ActualWidth);
		NotifyPointer(sender, null);
	}

	private void NotifyPointer(object sender, EventArgs e)
	{
		Utils.RunOnUiThread(base.Dispatcher, delegate
		{
			SetPointerPosition();
		});
	}

	public void Reload(double? width = null)
	{
		width = width ?? base.ActualWidth;
		if (width > 0.0)
		{
			SetupCells(width.Value);
		}
		SetPointerPosition();
	}

	public void SetData()
	{
		if (base.DataContext is CalendarTimelineDayViewModel calendarTimelineDayViewModel)
		{
			SetupData(calendarTimelineDayViewModel.Cells);
		}
		SetPointerPosition();
	}

	private async Task SetPointerPosition()
	{
		if (!(base.DataContext is CalendarTimelineDayViewModel calendarTimelineDayViewModel))
		{
			return;
		}
		SetTimePointer(calendarTimelineDayViewModel.IsToday);
		List<TaskCellViewModel> list = calendarTimelineDayViewModel.Cells.Where((TaskCellViewModel c) => c.IsHabit).ToList();
		bool reloadCell = false;
		SetTopBotTask(calendarTimelineDayViewModel);
		foreach (TaskCellViewModel cell in list)
		{
			if (cell.StartDate >= DateTime.Now || cell.StartDate < DateTime.Now.AddMinutes(-5.0))
			{
				continue;
			}
			HabitModel habitModel = await HabitDao.GetHabitById(cell.HabitId);
			if (habitModel == null || !(habitModel.Reminders?.Length > 0))
			{
				continue;
			}
			List<string> list2 = habitModel.Reminders.ToList();
			list2.Sort();
			string text = list2.FirstOrDefault((string r) => string.Compare(r, DateTime.Now.ToString("HH:mm"), StringComparison.Ordinal) >= 0) ?? list2.LastOrDefault();
			if (text != null && int.TryParse(text.Substring(0, 2), out var result) && int.TryParse(text.Substring(3), out var result2))
			{
				DateTime value = DateTime.Today.AddHours(result).AddMinutes(result2);
				if (cell.StartDate != value)
				{
					cell.SourceViewModel.StartDate = value;
					reloadCell = true;
				}
			}
		}
		if (reloadCell)
		{
			ReloadCells(base.ActualWidth);
		}
	}

	private void SetTopBotTask(CalendarTimelineDayViewModel model)
	{
		if (!string.IsNullOrEmpty(model.Extra1))
		{
			if (_topExtra1 == null)
			{
				_topExtra1 = new EmjTextBlock
				{
					TextTrimming = TextTrimming.CharacterEllipsis,
					FontSize = 12.0,
					VerticalAlignment = VerticalAlignment.Top,
					Margin = new Thickness(4.0, 0.0, 4.0, 0.0),
					IsHitTestVisible = false
				};
				_topExtra1.SetResourceReference(TextBlock.ForegroundProperty, "BaseColorOpacity80");
				base.Children.Add(_topExtra1);
			}
			_topExtra1.Text = model.Extra1;
		}
		else if (_topExtra1 != null)
		{
			_topExtra1.Text = string.Empty;
		}
		if (!string.IsNullOrEmpty(model.Extra2))
		{
			if (_topExtra2 == null)
			{
				_topExtra2 = new EmjTextBlock
				{
					TextTrimming = TextTrimming.CharacterEllipsis,
					FontSize = 12.0,
					VerticalAlignment = VerticalAlignment.Top,
					Margin = new Thickness(4.0, 16.0, 4.0, 0.0),
					IsHitTestVisible = false
				};
				_topExtra2.SetResourceReference(TextBlock.ForegroundProperty, "BaseColorOpacity80");
				base.Children.Add(_topExtra2);
			}
			_topExtra2.Text = model.Extra2;
		}
		else if (_topExtra2 != null)
		{
			_topExtra2.Text = string.Empty;
		}
		if (!string.IsNullOrEmpty(model.Extra3))
		{
			if (_botExtra1 == null)
			{
				_botExtra1 = new EmjTextBlock
				{
					TextTrimming = TextTrimming.CharacterEllipsis,
					FontSize = 12.0,
					VerticalAlignment = VerticalAlignment.Bottom,
					Margin = new Thickness(4.0, 0.0, 4.0, 16.0)
				};
				_botExtra1.SetResourceReference(TextBlock.ForegroundProperty, "BaseColorOpacity80");
				base.Children.Add(_botExtra1);
			}
			_botExtra1.Text = model.Extra3;
		}
		else if (_botExtra1 != null)
		{
			_botExtra1.Text = string.Empty;
		}
		if (!string.IsNullOrEmpty(model.Extra4))
		{
			if (_botExtra2 == null)
			{
				_botExtra2 = new EmjTextBlock
				{
					TextTrimming = TextTrimming.CharacterEllipsis,
					FontSize = 12.0,
					VerticalAlignment = VerticalAlignment.Bottom,
					Margin = new Thickness(4.0, 0.0, 4.0, 0.0)
				};
				_botExtra2.SetResourceReference(TextBlock.ForegroundProperty, "BaseColorOpacity80");
				base.Children.Add(_botExtra2);
			}
			_botExtra2.Text = model.Extra4;
		}
		else if (_botExtra2 != null)
		{
			_botExtra2.Text = string.Empty;
		}
	}

	private void SetTimePointer(bool show)
	{
		int num = (show ? CalendarGeoHelper.GetPointOffset() : (-1));
		show = show && num >= (CalendarGeoHelper.TopFolded ? 32 : 0);
		if (show)
		{
			if (_timePointer == null)
			{
				_timePointer = new Grid
				{
					Height = 9.0,
					IsHitTestVisible = false,
					VerticalAlignment = VerticalAlignment.Top
				};
				_timePointer.SetValue(Panel.ZIndexProperty, 100);
				SolidColorBrush colorInString = ThemeUtil.GetColorInString("#ED6655");
				_timePointer.Children.Add(new Line
				{
					X1 = 0.0,
					X2 = 1.0,
					Stretch = Stretch.Fill,
					StrokeThickness = 1.0,
					Stroke = colorInString,
					VerticalAlignment = VerticalAlignment.Top,
					Margin = new Thickness(2.0, 4.0, 0.0, 0.0)
				});
				_timePointer.Children.Add(new Ellipse
				{
					Height = 9.0,
					Width = 9.0,
					StrokeThickness = 1.0,
					Fill = colorInString,
					VerticalAlignment = VerticalAlignment.Center,
					HorizontalAlignment = HorizontalAlignment.Left
				});
				base.Children.Add(_timePointer);
			}
			if (num >= 0)
			{
				_timePointer.Margin = new Thickness(0.0, num - 6, 0.0, 0.0);
			}
		}
		else
		{
			base.Children.Remove(_timePointer);
			_timePointer = null;
		}
	}

	private void SetupCells(double width)
	{
		ReloadCells(width);
	}

	private void ReloadCells(double width)
	{
		if (!(base.DataContext is CalendarTimelineDayViewModel calendarTimelineDayViewModel) || !(width > 0.0))
		{
			return;
		}
		if (TickDragHelper.Dragging)
		{
			CalendarGeoHelper.AssemblyCells(calendarTimelineDayViewModel.Cells.Where((TaskCellViewModel c) => c.IsDragDisplayItem).ToList(), calendarTimelineDayViewModel.InWeekControl ? width : (width - 10.0), calendarTimelineDayViewModel.Date);
			CalendarGeoHelper.AssemblyCells(calendarTimelineDayViewModel.Cells.Where((TaskCellViewModel c) => !c.IsDragDisplayItem).ToList(), calendarTimelineDayViewModel.InWeekControl ? width : (width - 10.0), calendarTimelineDayViewModel.Date);
		}
		else
		{
			CalendarGeoHelper.AssemblyCells(calendarTimelineDayViewModel.Cells, calendarTimelineDayViewModel.InWeekControl ? width : (width - 10.0), calendarTimelineDayViewModel.Date);
		}
		SetupData(calendarTimelineDayViewModel.Cells);
	}

	private async Task ShowAddTaskPopup()
	{
		if (base.Tag == null)
		{
			return;
		}
		object tag = base.Tag;
		if (!(tag is TaskCellViewModel { StartDate: var startDate } addModel))
		{
			return;
		}
		if (startDate.HasValue && CalendarGeoHelper.TopFolded)
		{
			int hour = addModel.StartDate.Value.Hour;
			if (hour <= CalendarGeoHelper.GetStartHourForTask() || hour >= CalendarGeoHelper.GetEndHour())
			{
				return;
			}
		}
		if (!addModel.DueDate.HasValue && addModel.StartDate.HasValue)
		{
			TaskDefaultModel defaultSafely = TaskDefaultDao.GetDefaultSafely();
			if (defaultSafely.DateMode == 1 && defaultSafely.Duration > 0)
			{
				addModel.SourceViewModel.DueDate = addModel.StartDate.Value.AddMinutes(defaultSafely.Duration);
				addModel.Height = (addModel.DueDate - addModel.StartDate).Value.TotalMinutes / CalendarGeoHelper.MinMinute * CalendarGeoHelper.MinHeight;
			}
		}
		ShowAddCell(addModel, isMove: false, hide: true);
		TaskDetailViewModel model = TaskDetailViewModel.BuildInitModel(addModel.SourceViewModel, isAllDay: false);
		TaskDetailPopup window = new TaskDetailPopup
		{
			IsCalendarContext = true
		};
		IToastShowWindow parentWindow = GetParentWindow();
		UIElement target = (UIElement)(((object)_addTaskCell) ?? ((object)this));
		if (parentWindow != null)
		{
			if (PopupStateManager.LastTarget == target)
			{
				RemoveAddCell();
				return;
			}
			window.DependentWindow = parentWindow;
		}
		window.Disappear -= OnAddPopupClosed;
		window.Disappear += OnAddPopupClosed;
		window.CalendarAddFlowClosed -= OnCalendarAddFlowClosed;
		window.CalendarAddFlowClosed += OnCalendarAddFlowClosed;
		window.TaskSaved += OnTaskAdded;
		if (showtcs != null)
		{
			await showtcs.Task;
		}
		window.Show(model, string.Empty, new TaskWindowDisplayArgs(target, base.ActualWidth - 6.0, PopupLocationCalculator.GetMousePoint(base.ActualWidth <= 400.0), 0.0));
		_mouseClick = false;
		GetCalendarParent()?.SetEditting(editting: true);
		_addPopupShowed = true;
		window.FocusTitle();
		AddOrRemoveTempTask(addModel.SourceViewModel);
	}

	private void AddOrRemoveTempTask(TaskBaseViewModel taskBase)
	{
		if (taskBase != null)
		{
			if (!addedTempTaskIds.Contains(taskBase.Id))
			{
				addedTempTaskIds.Add(taskBase.Id);
			}
			Utils.FindParent<MultiDayView>(this)?.ReloadWithTempTask(new CalendarDisplayModel(taskBase));
		}
		else if (addedTempTaskIds.Any())
		{
			addedTempTaskIds = new ConcurrentBag<string>();
			Utils.FindParent<MultiDayView>(this)?.ReloadWithoutTempTask();
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
		addedTempTaskIds = new ConcurrentBag<string>();
	}

	private IToastShowWindow GetParentWindow()
	{
		_parentWindow = _parentWindow ?? Utils.FindParent<IToastShowWindow>(this);
		return _parentWindow;
	}

	public void OnLeftButtonUp(object sender, MouseButtonEventArgs e)
	{
		CalendarControl calendarParent = GetCalendarParent();
		if (calendarParent != null && calendarParent.IsLocked)
		{
			return;
		}
		if (sender is Window window)
		{
			window.ReleaseMouseCapture();
			window.PreviewMouseMove -= OnMouseMove;
			window.PreviewMouseLeftButtonUp -= OnLeftButtonUp;
		}
		if (!_doubleClick && _mouseClick)
		{
			e.Handled = true;
			if (PopupStateManager.CanShowAddPopup())
			{
				ShowAddTaskPopup();
			}
		}
	}

	private void OnLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		CalendarControl calendarParent = GetCalendarParent();
		if ((calendarParent != null && calendarParent.IsLocked) || Utils.IfCtrlPressed() || !PopupStateManager.CanShowAddPopup())
		{
			return;
		}
		CalendarControl calendarParent2 = GetCalendarParent();
		if (calendarParent2 != null && calendarParent2.ClearCopiedTaskSelection())
		{
			return;
		}
		MultiDayView multiDayView = Utils.FindParent<MultiDayView>(this);
		if (multiDayView != null && multiDayView.ExitBatchSelect())
		{
			return;
		}
		_draggingStartY = e.GetPosition(this).Y;
		if (base.DataContext != null && base.DataContext is CalendarTimelineDayViewModel calendarTimelineDayViewModel)
		{
			CalendarControl calendarControl = Utils.FindParent<CalendarControl>(this);
			string calendarDefaultAddProjectId = CalendarUtils.GetCalendarDefaultAddProjectId(calendarControl != null && calendarControl.ViewSourceType == CalendarViewSourceType.WithList);
			TaskCellViewModel taskCellViewModel = CalendarGeoHelper.BuildAddTaskModel(calendarTimelineDayViewModel.Date, e.GetPosition(this).Y);
			taskCellViewModel.SourceViewModel.ProjectId = calendarDefaultAddProjectId;
			taskCellViewModel.GetColor((bool)FindResource("IsDarkTheme"));
			taskCellViewModel.Height = CalendarGeoHelper.CalculateCellHeight(taskCellViewModel.StartDate.Value, taskCellViewModel.DueDate);
			taskCellViewModel.Width = base.ActualWidth - 9.0;
			base.Tag = taskCellViewModel;
			_isDuration = false;
			_mouseClick = true;
			ShowAddCell(taskCellViewModel, isMove: false, hide: true);
			AddOrRemoveTempTask(taskCellViewModel.SourceViewModel);
			Window parentWindow = Utils.GetParentWindow(this);
			if (parentWindow != null)
			{
				parentWindow.CaptureMouse();
				parentWindow.PreviewMouseMove -= OnMouseMove;
				parentWindow.PreviewMouseMove += OnMouseMove;
				parentWindow.PreviewMouseLeftButtonUp -= OnLeftButtonUp;
				parentWindow.PreviewMouseLeftButtonUp += OnLeftButtonUp;
			}
		}
	}

	private void OnMouseMove(object sender, MouseEventArgs e)
	{
		if (e.LeftButton != MouseButtonState.Pressed || !_mouseClick || _addPopupShowed || base.Tag == null || !(base.Tag is TaskCellViewModel { StartDate: var startDate } taskCellViewModel) || (startDate.HasValue && taskCellViewModel.StartDate.Value.Hour < CalendarGeoHelper.GetStartHour()))
		{
			return;
		}
		double y = e.GetPosition(this).Y;
		if (!_isDuration && y - _draggingStartY > 6.0)
		{
			_isDuration = true;
		}
		y += CalendarGeoHelper.GetTopFoldDiff();
		if (_isDuration)
		{
			double roundOffset = CalendarGeoHelper.GetRoundOffset(y);
			DateTime dateTime = CalendarGeoHelper.TranslateVerticalOffset(GetDate(), roundOffset);
			if (taskCellViewModel.StartDate.HasValue)
			{
				if (dateTime <= taskCellViewModel.StartDate.Value.AddMinutes(15.0))
				{
					dateTime = taskCellViewModel.StartDate.Value.AddMinutes(15.0);
				}
				taskCellViewModel.SourceViewModel.DueDate = dateTime;
				DateTime value = dateTime;
				DateTime? startDate2 = taskCellViewModel.StartDate;
				taskCellViewModel.Height = (value - startDate2).Value.TotalMinutes / 15.0 * CalendarGeoHelper.QuarterHourHeight;
			}
		}
		AddOrRemoveTempTask(null);
		ShowAddCell(taskCellViewModel);
	}

	private async void OnAddPopupClosed(object sender, string e)
	{
		if (sender is TaskDetailPopup taskDetailPopup)
		{
			if (taskDetailPopup.DelayCalendarAddCleanup)
			{
				return;
			}
			taskDetailPopup.Disappear -= OnAddPopupClosed;
			taskDetailPopup.CalendarAddFlowClosed -= OnCalendarAddFlowClosed;
			taskDetailPopup.TaskSaved -= OnTaskAdded;
		}
		bool keepSavedTask = TaskCache.ExistTask(e);
		CloseAddPopupPreview(keepSavedTask);
	}

	private void OnCalendarAddFlowClosed(object sender, CalendarEventModel e)
	{
		if (sender is TaskDetailPopup taskDetailPopup)
		{
			taskDetailPopup.CalendarAddFlowClosed -= OnCalendarAddFlowClosed;
			taskDetailPopup.Disappear -= OnAddPopupClosed;
			taskDetailPopup.TaskSaved -= OnTaskAdded;
		}
		CloseAddPopupPreview(keepSavedTask: false);
		ShowCalendarEventHiddenToast(e);
	}

	private void CloseAddPopupPreview(bool keepSavedTask)
	{
		if (base.DataContext is CalendarTimelineDayViewModel calendarTimelineDayViewModel)
		{
			calendarTimelineDayViewModel.State = MonthCellState.Normal;
		}
		GetCalendarParent()?.SetEditting(editting: false);
		RemoveAddCell();
		if (!keepSavedTask && addedTempTaskIds.Any())
		{
			AddOrRemoveTempTask(null);
		}
		_addPopupShowed = false;
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

	private CalendarControl GetCalendarParent()
	{
		return Utils.FindParent<CalendarControl>(this);
	}

	public void SetupData(List<TaskCellViewModel> data)
	{
		int num = data.Count - _cells.Count;
		if (num > 0)
		{
			BatchInitCells(num);
		}
		BlockingSet<string> blockingSet = Utils.FindParent<CalendarControl>(this)?.BatchSelected;
		bool showIcon = StaticPropertiesModel.Instance.AltKeyDown || base.Width >= CalendarGeoHelper.CalTitleFontSize * 7.0 - 4.0;
		List<TaskCell> list = new List<TaskCell>();
		for (int i = 0; i < _cells.Count; i++)
		{
			if (i < data.Count)
			{
				TaskCellViewModel taskCellViewModel = data[i];
				if (blockingSet != null && taskCellViewModel.CanBatchSelected && blockingSet.Contains(taskCellViewModel.TaskId))
				{
					taskCellViewModel.BatchSelected = true;
				}
				TaskCell taskCell = _cells[i];
				if (taskCell == null)
				{
					continue;
				}
				if (taskCellViewModel.StartDate.HasValue)
				{
					taskCell.DataContext = taskCellViewModel;
					taskCell.SetShowIcon(showIcon);
					taskCell.SetValue(Panel.ZIndexProperty, taskCellViewModel.BatchSelected ? 10 : 5);
					if (taskCell.Visibility != Visibility.Visible)
					{
						taskCell.Visibility = Visibility.Visible;
					}
				}
				else
				{
					list.Add(taskCell);
					taskCell.DataContext = null;
					if (taskCell.Visibility != Visibility.Collapsed)
					{
						taskCell.Visibility = Visibility.Collapsed;
					}
				}
				continue;
			}
			TaskCell taskCell2 = _cells[i];
			if (taskCell2 != null)
			{
				list.Add(taskCell2);
				taskCell2.DataContext = null;
				if (taskCell2.Visibility == Visibility.Visible)
				{
					taskCell2.Visibility = Visibility.Collapsed;
				}
			}
		}
		foreach (TaskCell item in list)
		{
			_cells.Remove(item);
			base.Children.Remove(item);
		}
		RemoveAddCell();
	}

	private void BatchInitCells(int count)
	{
		for (int i = 0; i < count; i++)
		{
			TaskCell taskCell = new TaskCell(null)
			{
				Visibility = Visibility.Collapsed
			};
			taskCell.SetValue(Panel.ZIndexProperty, 5);
			_cells.Add(taskCell);
			base.Children.Add(taskCell);
		}
	}

	public void ShowAddCell(TaskCellViewModel model, bool isMove = false, bool hide = false)
	{
		if (model == null)
		{
			return;
		}
		if (isMove)
		{
			model = model.Clone();
			model.Selected = true;
		}
		model.Width = base.ActualWidth - 12.0;
		if (_addTaskCell == null)
		{
			showtcs = new TaskCompletionSource<bool>();
			_addTaskCell = new TaskCell(model, isMove: true, base.ActualWidth >= 80.0);
			_addTaskCell.Loaded += _addTaskCell_Loaded;
			_addTaskCell.SetValue(Panel.ZIndexProperty, 20);
			if (hide)
			{
				_addTaskCell.Visibility = Visibility.Hidden;
			}
			base.Children.Add(_addTaskCell);
		}
		else
		{
			if (hide)
			{
				_addTaskCell.Visibility = Visibility.Hidden;
			}
			_addTaskCell.DataContext = model;
		}
	}

	private void _addTaskCell_Loaded(object sender, RoutedEventArgs e)
	{
		showtcs?.TrySetResult(result: true);
	}

	public async void RemoveAddCell(bool toCell = false)
	{
		TaskCell addTaskCell = _addTaskCell;
		if (addTaskCell?.DataContext is TaskCellViewModel taskCellViewModel && addedTempTaskIds.Contains(taskCellViewModel.TaskId))
		{
			return;
		}
		_addTaskCell = null;
		if (addTaskCell == null)
		{
			return;
		}
		if (toCell)
		{
			addTaskCell.SetValue(Panel.ZIndexProperty, 5);
			_cells.Add(addTaskCell);
			if (addTaskCell.DataContext is TaskCellViewModel taskCellViewModel2)
			{
				taskCellViewModel2.RepeatDiff = 0.0;
				taskCellViewModel2.Selected = false;
				if (base.DataContext is CalendarTimelineDayViewModel calendarTimelineDayViewModel)
				{
					calendarTimelineDayViewModel.Cells.Add(taskCellViewModel2);
				}
			}
			ReloadCells(base.ActualWidth);
		}
		else
		{
			base.Children.Remove(addTaskCell);
			addTaskCell.Loaded -= _addTaskCell_Loaded;
			showtcs?.TrySetResult(result: false);
		}
	}

	public void ClearItems(bool onlyData)
	{
		if (onlyData)
		{
			if (_cells.Count > 5)
			{
				List<TaskCell> list = new List<TaskCell>();
				for (int i = 4; i < _cells.Count; i++)
				{
					list.Add(_cells[i]);
				}
				foreach (TaskCell item in list)
				{
					_cells.Remove(item);
					base.Children.Remove(item);
				}
			}
			{
				foreach (TaskCell item2 in _cells.Value)
				{
					item2.DataContext = null;
					item2.Visibility = Visibility.Collapsed;
				}
				return;
			}
		}
		foreach (TaskCell item3 in _cells.Value)
		{
			base.Children.Remove(item3);
		}
		_cells.Clear();
	}

	public DateTime GetDate()
	{
		return (base.DataContext as CalendarTimelineDayViewModel)?.Date ?? default(DateTime);
	}

	public void OnDragEnter()
	{
		double num = Utils.FindParent<CalendarTimelineView>(this)?.ActualHeight ?? 500.0;
		object dragData = TickDragHelper.GetDragData();
		if (!(dragData is DisplayItemModel) && !(dragData is TimelineCellViewModel))
		{
			if (!(dragData is TaskCellViewModel taskCellViewModel))
			{
				if (dragData is CalendarDisplayViewModel calendarDisplayViewModel)
				{
					TaskCellViewModel taskCellViewModel2 = TaskCellViewModel.Build(new CalendarDisplayModel(calendarDisplayViewModel.SourceViewModel), calendarDisplayViewModel.StartDate.GetValueOrDefault());
					taskCellViewModel2.Height = CalendarGeoHelper.MinPointHeight;
					taskCellViewModel2.RepeatDiff = calendarDisplayViewModel.RepeatDiff;
					taskCellViewModel2.Width = base.ActualWidth - 12.0;
					taskCellViewModel2.Height = Math.Min(taskCellViewModel2.Height, num);
					taskCellViewModel2.BatchSelected = calendarDisplayViewModel.BatchSelected;
					TickDragHelper.ChangeDragContent(taskCellViewModel2, base.ActualWidth - 12.0);
				}
			}
			else
			{
				taskCellViewModel.Width = base.ActualWidth - 12.0;
				TaskCellViewModel taskCellViewModel3 = TaskCellViewModel.Build(new CalendarDisplayModel(taskCellViewModel.SourceViewModel), taskCellViewModel.StartDate.GetValueOrDefault());
				taskCellViewModel3.Height = Math.Min(taskCellViewModel.Height, 500.0);
				taskCellViewModel3.RepeatDiff = taskCellViewModel.RepeatDiff;
				taskCellViewModel3.Width = base.ActualWidth - 12.0;
				taskCellViewModel3.Height = Math.Min(taskCellViewModel3.Height, num);
				taskCellViewModel3.BatchSelected = taskCellViewModel.BatchSelected;
				TickDragHelper.ChangeDragContent(taskCellViewModel3, base.ActualWidth - 12.0);
			}
		}
		else
		{
			TickDragHelper.ChangeDragContent(3, base.ActualWidth - 12.0, num);
			TickDragHelper.SetDragOverCalendar();
		}
	}

	public async void OnDragDrop()
	{
		_autoScroller?.SetAutoScrolling(autoScroll: false);
		object obj = _addTaskCell?.DataContext;
		if (!(obj is TaskCellViewModel cellModel))
		{
			return;
		}
		object data = TickDragHelper.GetDragData();
		bool needAddEvent = false;
		obj = data;
		if (!(obj is DisplayItemModel displayItemModel))
		{
			if (!(obj is TimelineCellViewModel timelineCellViewModel))
			{
				if (!(obj is CheckItemViewModel checkItemViewModel))
				{
					if (obj is CalendarDisplayViewModel barItem)
					{
						if (string.IsNullOrEmpty(barItem.RepeatFlag))
						{
							await barItem.SetDropTimeSpan(cellModel.DisplayStartDate, cellModel.DisplayDueDate, Window.GetWindow(this), false);
						}
						else
						{
							List<TaskReminderModel> reminders = await TaskReminderDao.GetRemindersByTaskId(barItem.TaskId);
							TimeData reviseData = new TimeData
							{
								StartDate = cellModel.StartDate,
								DueDate = cellModel.DueDate,
								IsAllDay = false,
								Reminders = reminders,
								RepeatFrom = barItem.RepeatFrom,
								RepeatFlag = barItem.RepeatFlag,
								ExDates = ((barItem.SourceViewModel.ExDates == null) ? new List<string>() : ExDateSerilizer.ToArray(barItem.SourceViewModel.ExDates)?.ToList()),
								TimeZone = new TimeZoneViewModel(barItem.SourceViewModel.IsFloating, barItem.SourceViewModel.TimeZoneName)
							};
							await ModifyRepeatHandler.TryUpdateDueDate(barItem.TaskId, barItem.DisplayStartDate, barItem.DisplayDueDate, reviseData, 1, 0);
						}
					}
				}
				else if (UserDao.IsPro())
				{
					needAddEvent = true;
					await checkItemViewModel.SourceViewModel.SetDropTimeSpan(cellModel.DisplayStartDate, cellModel.DisplayDueDate, Window.GetWindow(this));
				}
			}
			else
			{
				needAddEvent = true;
				if (!timelineCellViewModel.BatchSelected)
				{
					await timelineCellViewModel.DisplayModel.SetDropTimeSpan(cellModel.DisplayStartDate, cellModel.DisplayDueDate, Window.GetWindow(this));
				}
				else
				{
					List<TaskBaseViewModel> tasksByIds = TaskCache.GetTasksByIds(TickDragHelper.DraggingTaskIds?.ToList());
					foreach (TaskBaseViewModel item in tasksByIds)
					{
						await item.SetDropTimeSpan(cellModel.DisplayStartDate, cellModel.DisplayDueDate, Window.GetWindow(this));
					}
				}
			}
		}
		else
		{
			needAddEvent = true;
			if (!displayItemModel.BatchMode)
			{
				await displayItemModel.SourceViewModel.SetDropTimeSpan(cellModel.DisplayStartDate, cellModel.DisplayDueDate, Window.GetWindow(this));
			}
			else
			{
				List<TaskBaseViewModel> tasksByIds2 = TaskCache.GetTasksByIds(displayItemModel.BatchSelectedIds);
				foreach (TaskBaseViewModel item2 in tasksByIds2)
				{
					await item2.SetDropTimeSpan(cellModel.DisplayStartDate, cellModel.DisplayDueDate, Window.GetWindow(this));
				}
			}
		}
		Utils.FindParent<MultiDayView>(this)?.Reload();
		RemoveAddCell(toCell: true);
		CalendarControl calendarControl = Utils.FindParent<CalendarControl>(this);
		calendarControl?.CheckItemFiltered(data);
		if (needAddEvent && calendarControl != null && calendarControl.ParallelShow)
		{
			string ctype = ((calendarControl.ViewSourceType == CalendarViewSourceType.WithList) ? "task+calendar" : "calendar+task");
			UserActCollectUtils.AddClickEvent("split_view", ctype, "drag_to_calendar");
		}
	}

	public void OnDragLeave()
	{
		_autoScroller?.SetAutoScrolling(autoScroll: false);
		RemoveAddCell();
	}

	public void OnDragOver(MouseEventArgs e)
	{
		if (_autoScroller == null)
		{
			_autoScroller = new DragAutoScrollHelper(Utils.FindParent<CalendarTimelineView>(this));
		}
		if (!_autoScroller.TryAutoScroll(e))
		{
			OnDragMouseOver();
		}
	}

	private void OnDragMouseOver()
	{
		object data = TickDragHelper.GetDragData();
		DateTime date = GetDate();
		TaskCellViewModel cellModel = _addTaskCell?.DataContext as TaskCellViewModel;
		bool exist = cellModel != null;
		if (!exist)
		{
			object obj = data;
			if (!(obj is DisplayItemModel displayItemModel))
			{
				if (!(obj is CheckItemViewModel checkItemViewModel))
				{
					if (!(obj is TimelineCellViewModel timelineCellViewModel))
					{
						if (!(obj is TaskCellViewModel taskCellViewModel))
						{
							if (obj is CalendarDisplayViewModel calendarDisplayViewModel)
							{
								cellModel = calendarDisplayViewModel.ToDragTaskCellViewModel();
								calendarDisplayViewModel.DragVOffset = ((calendarDisplayViewModel.DragVOffset > 0.0) ? calendarDisplayViewModel.DragVOffset : 6.0);
								cellModel.BaseOnStart = true;
							}
						}
						else
						{
							cellModel = taskCellViewModel.Clone();
						}
					}
					else if (timelineCellViewModel.DisplayModel.IsTaskOrNote)
					{
						cellModel = TaskCellViewModel.Build(new CalendarDisplayModel(timelineCellViewModel.DisplayModel.Copy()), date);
						cellModel.DragVOffset = 6.0;
						cellModel.BaseOnStart = true;
					}
				}
				else if (UserDao.IsPro())
				{
					cellModel = TaskCellViewModel.Build(new CalendarDisplayModel(checkItemViewModel.SourceViewModel.Copy()), date);
					cellModel.DragVOffset = 6.0;
					cellModel.BaseOnStart = true;
				}
			}
			else if (displayItemModel.SourceViewModel.IsTaskOrNote || displayItemModel.SourceViewModel.IsCheckItem)
			{
				cellModel = TaskCellViewModel.Build(new CalendarDisplayModel(displayItemModel.SourceViewModel.Copy()), date);
				cellModel.DragVOffset = 6.0;
				cellModel.BaseOnStart = true;
			}
			if (cellModel == null)
			{
				return;
			}
			if (cellModel.IsAllDay == true)
			{
				cellModel.SourceViewModel.DueDate = null;
			}
			cellModel.Width = base.ActualWidth - 8.0;
		}
		double num = GetMinutesByOffset(Mouse.GetPosition(this).Y - cellModel.DragVOffset, cellModel.BaseOnStart);
		if (!cellModel.BaseOnStart && cellModel.OriginalStart.HasValue)
		{
			num -= (cellModel.OriginalStart.Value.Date.AddDays(1.0) - cellModel.OriginalStart.Value).TotalMinutes;
		}
		DateTime value = date.AddMinutes(num);
		if (cellModel.DisplayDueDate.HasValue && cellModel.DisplayStartDate.HasValue)
		{
			cellModel.SourceViewModel.DueDate = value.AddMinutes((cellModel.DisplayDueDate.Value - cellModel.DisplayStartDate.Value).TotalMinutes);
		}
		cellModel.SourceViewModel.StartDate = value;
		base.Dispatcher.Invoke(delegate
		{
			CalendarGeoHelper.GetCellPosition(cellModel);
			if (!exist)
			{
				if (cellModel.BatchSelected)
				{
					if (data is TaskCellViewModel taskCellViewModel2)
					{
						TimeSpan? diff = cellModel.DisplayStartDate - taskCellViewModel2.DisplayStartDate;
						_parent?.GetParent()?.OnDragDiffChanged(diff, ((int?)(cellModel.DisplayStartDate?.Date - taskCellViewModel2.DisplayStartDate?.Date)?.TotalDays).GetValueOrDefault());
					}
					else if (data is CalendarDisplayViewModel calendarDisplayViewModel2)
					{
						TimeSpan? diff2 = cellModel.DisplayStartDate - (calendarDisplayViewModel2.DisplayStartDate ?? calendarDisplayViewModel2.SourceViewModel.CompletedTime)?.Date;
						int valueOrDefault = ((int?)(cellModel.DisplayStartDate?.Date - (calendarDisplayViewModel2.DisplayStartDate ?? calendarDisplayViewModel2.SourceViewModel.CompletedTime)?.Date)?.TotalDays).GetValueOrDefault();
						_parent?.GetParent()?.OnDragDiffChanged(diff2, valueOrDefault, toAllDay: false, toPoint: true);
					}
				}
				else
				{
					ShowAddCell(cellModel, isMove: true);
				}
			}
		}, DispatcherPriority.Send);
	}

	private double GetMinutesByOffset(double offset, bool baseOnStart)
	{
		if (CalendarGeoHelper.TopFolded)
		{
			offset -= 32.0;
		}
		offset = CalendarGeoHelper.GetRoundOffset(offset);
		int num = CalendarGeoHelper.TranslateMinute(offset);
		if (CalendarGeoHelper.TopFolded && baseOnStart)
		{
			num += CalendarGeoHelper.GetStartHour() * 60;
		}
		return num;
	}

	public void RemoveDraggingItem()
	{
		foreach (TaskCell item in _cells.ToList())
		{
			if (!(item.DataContext is TaskCellViewModel { Dragging: not false } taskCellViewModel))
			{
				continue;
			}
			_cells.Remove(item);
			base.Children.Remove(item);
			if (base.DataContext is CalendarTimelineDayViewModel calendarTimelineDayViewModel)
			{
				calendarTimelineDayViewModel.Cells.Remove(taskCellViewModel);
				if (_addTaskCell == null)
				{
					ReloadCells(base.ActualWidth);
				}
			}
			break;
		}
	}

	public void SetShowIcon()
	{
		bool showIcon = StaticPropertiesModel.Instance.AltKeyDown || base.Width >= CalendarGeoHelper.CalTitleFontSize * 7.0 - 4.0;
		foreach (TaskCell item in _cells.ToList())
		{
			if (item.DataContext is TaskCellViewModel taskCellViewModel)
			{
				item.SetShowIcon(showIcon);
				taskCellViewModel.SetIcon(notify: true);
			}
		}
	}

	public void SetColor()
	{
		if (StaticPropertiesModel.Instance.AltKeyDown)
		{
			_ = 1;
		}
		else
			_ = base.Width >= CalendarGeoHelper.CalTitleFontSize * 7.0 - 4.0;
		foreach (TaskCell item in _cells.ToList())
		{
			if (item.DataContext is TaskCellViewModel taskCellViewModel)
			{
				taskCellViewModel.SetColor();
			}
		}
	}

	public void TryBatchSelectInRect(Rect rect, double transformX)
	{
		if (!(base.DataContext is CalendarTimelineDayViewModel calendarTimelineDayViewModel))
		{
			return;
		}
		foreach (TaskCellViewModel cell in calendarTimelineDayViewModel.Cells)
		{
			if (!cell.CanBatchSelected)
			{
				continue;
			}
			Rect rect2 = new Rect(transformX + cell.Margin.Left, cell.Margin.Top, cell.Width, cell.Height - 1.0);
			if (rect2.IntersectsWith(rect))
			{
				if (!cell.TempBatchSelect && !cell.BatchSelected)
				{
					cell.TempBatchSelect = true;
				}
			}
			else if (cell.TempBatchSelect)
			{
				cell.TempBatchSelect = false;
			}
		}
	}

	public List<string> SetBatchSelect()
	{
		List<string> list = new List<string>();
		if (base.DataContext is CalendarTimelineDayViewModel calendarTimelineDayViewModel)
		{
			foreach (TaskCellViewModel item in calendarTimelineDayViewModel.Cells.Where((TaskCellViewModel c) => c.TempBatchSelect))
			{
				item.BatchSelected = true;
				item.TempBatchSelect = false;
				list.Add(item.TaskId);
			}
		}
		return list;
	}

	public void ClearBatchSelected()
	{
		if (!(base.DataContext is CalendarTimelineDayViewModel calendarTimelineDayViewModel))
		{
			return;
		}
		foreach (TaskCellViewModel cell in calendarTimelineDayViewModel.Cells)
		{
			cell.BatchSelected = false;
			cell.TempBatchSelect = false;
		}
	}

	public void SetBatchDragging(bool batchDragging)
	{
		if (!(base.DataContext is CalendarTimelineDayViewModel calendarTimelineDayViewModel))
		{
			return;
		}
		foreach (TaskCellViewModel cell in calendarTimelineDayViewModel.Cells)
		{
			if (cell.BatchSelected)
			{
				cell.BatchDragging = batchDragging;
			}
		}
	}

	public void CheckDragging()
	{
		Point position = Mouse.GetPosition(this);
		if (position.X > 0.0 && position.X < base.ActualWidth && position.Y > 0.0 && position.Y < base.ActualHeight)
		{
			OnDragMouseOver();
		}
	}

	public void SetBatchSelected(BlockingSet<string> batchSelected)
	{
		if (!(base.DataContext is CalendarTimelineDayViewModel calendarTimelineDayViewModel))
		{
			return;
		}
		foreach (TaskCellViewModel cell in calendarTimelineDayViewModel.Cells)
		{
			cell.BatchSelected = cell.CanBatchSelected && batchSelected.Contains(cell.TaskId);
			cell.TempBatchSelect = false;
		}
	}
}
