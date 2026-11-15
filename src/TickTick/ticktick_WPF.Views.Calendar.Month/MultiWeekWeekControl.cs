using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using TickTickUtils;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Util.DragAndDrop;

namespace ticktick_WPF.Views.Calendar.Month;

public class MultiWeekWeekControl : Grid
{
	private string _uid;

	private List<WeekEventModel> _events;

	private List<TaskBar> _taskBars = new List<TaskBar>();

	private List<CalLoadMore> _loadMores = new List<CalLoadMore>();

	private Dictionary<int, MultiWeekDayControl> _dayControls = new Dictionary<int, MultiWeekDayControl>();

	private bool _eventsSetting;

	private readonly Dictionary<int, Line> _lines = new Dictionary<int, Line>();

	private Line _hLine;

	private readonly Grid _spanItems = new Grid
	{
		Margin = new Thickness(0.0, 28.0, 0.0, 0.0)
	};

	private Grid _monthGrid;

	private Storyboard _monthGridStory;

	private double _offset;

	private object _lock = new object();

	private Border _monthTextBorder;

	private VisualBrush _borderBrush;

	private double _monthTextWidth;

	private readonly ConcurrentDictionary<string, TaskBar> _dragItems = new ConcurrentDictionary<string, TaskBar>();

	private readonly ConcurrentDictionary<int, CalLoadMore> _dragMoreItems = new ConcurrentDictionary<int, CalLoadMore>();

	public DateTime StartDate { get; set; }

	public double Offset
	{
		get
		{
			return _offset;
		}
		set
		{
			_offset = value;
			if (_borderBrush != null)
			{
				Border windowBackground = App.Window.WindowBackground;
				_borderBrush.Viewbox = new Rect(54.0 / windowBackground.ActualWidth, (Offset + 90.0) / windowBackground.ActualHeight, 0.0, 0.0);
			}
		}
	}

	public MultiWeekWeekControl()
	{
		_uid = Utils.GetGuid();
		base.SizeChanged += OnSizeChanged;
		_hLine = new Line
		{
			X1 = 0.0,
			X2 = 1.0,
			Stretch = Stretch.Fill,
			StrokeThickness = 1.0,
			VerticalAlignment = VerticalAlignment.Bottom
		};
		_hLine.SetResourceReference(Shape.StrokeProperty, "BaseColorOpacity4");
		base.Children.Add(_hLine);
		base.Children.Add(_spanItems);
		_spanItems.SetValue(Panel.ZIndexProperty, 100);
		SetColumns(LocalSettings.Settings.ShowCalWeekend ? 7 : 5);
		AddMonthText();
	}

	private void AddMonthText()
	{
		_monthGrid = new Grid
		{
			RenderTransform = new TranslateTransform(0.0, -10.0),
			Margin = new Thickness(2.0, 0.0, 0.0, 0.0),
			Height = 40.0,
			VerticalAlignment = VerticalAlignment.Top,
			HorizontalAlignment = HorizontalAlignment.Left,
			Opacity = 0.0,
			IsHitTestVisible = false
		};
		_monthGrid.SetValue(Grid.ColumnSpanProperty, 5);
		_monthTextBorder = new Border
		{
			Height = 36.0
		};
		TextBlock textBlock = new TextBlock
		{
			FontSize = 24.0,
			VerticalAlignment = VerticalAlignment.Center,
			Margin = new Thickness(16.0, 0.0, 16.0, 0.0),
			FontWeight = FontWeights.Bold
		};
		_monthGrid.Children.Add(_monthTextBorder);
		Border child = new Border();
		_monthTextBorder.Child = child;
		_monthGrid.Children.Add(textBlock);
		textBlock.SetResourceReference(TextBlock.ForegroundProperty, "BaseColorOpacity100");
		base.Children.Add(_monthGrid);
	}

	public void SetColumns(int count)
	{
		lock (_lock)
		{
			if (count == base.ColumnDefinitions.Count)
			{
				return;
			}
			int count2 = base.ColumnDefinitions.Count;
			if (count > count2)
			{
				for (int i = count2; i < count; i++)
				{
					base.ColumnDefinitions.Add(new ColumnDefinition
					{
						Width = new GridLength(1.0, GridUnitType.Star)
					});
					MultiWeekDayControl multiWeekDayControl = new MultiWeekDayControl();
					multiWeekDayControl.SetValue(Grid.ColumnProperty, i);
					_dayControls[i] = multiWeekDayControl;
					base.Children.Add(multiWeekDayControl);
					if (i >= 1)
					{
						Line line = new Line
						{
							Y1 = 0.0,
							Y2 = 1.0,
							Stretch = Stretch.Fill,
							StrokeThickness = 1.0,
							HorizontalAlignment = HorizontalAlignment.Right
						};
						line.SetValue(Grid.ColumnProperty, i - 1);
						line.SetResourceReference(Shape.StrokeProperty, "BaseColorOpacity4");
						base.Children.Add(line);
						_lines[i - 1] = line;
					}
				}
			}
			else
			{
				base.ColumnDefinitions.RemoveRange(count, count2 - count);
				for (int j = count; j < count2; j++)
				{
					if (_dayControls.TryGetValue(j, out var value))
					{
						_dayControls.Remove(j);
						base.Children.Remove(value);
					}
					if (_lines.TryGetValue(j - 1, out var value2))
					{
						_lines.Remove(j - 1);
						base.Children.Remove(value2);
					}
				}
			}
			_hLine?.SetValue(Grid.ColumnSpanProperty, count);
			_spanItems?.SetValue(Grid.ColumnSpanProperty, count);
		}
	}

	public void SetData(Dictionary<int, List<MonthDayViewModel>> modelDict, int rowIndex)
	{
		if (!modelDict.TryGetValue(rowIndex, out var value))
		{
			return;
		}
		if (LocalSettings.Settings.ShowCalWeekend)
		{
			foreach (int key in _dayControls.Keys)
			{
				MultiWeekDayControl multiWeekDayControl = _dayControls[key];
				if (value.Count > key)
				{
					MonthDayViewModel dataContext = value[key];
					multiWeekDayControl.DataContext = dataContext;
				}
			}
		}
		else
		{
			DayOfWeek startWeek = LocalSettings.Settings.WeekStartFromType;
			if (startWeek == DayOfWeek.Saturday || startWeek == DayOfWeek.Sunday)
			{
				startWeek = DayOfWeek.Monday;
			}
			List<MonthDayViewModel> list = value.Where((MonthDayViewModel x) => !x.IsWeekend).ToList();
			int num = list.FindIndex((MonthDayViewModel m) => m.Date.DayOfWeek == startWeek);
			foreach (int key2 in _dayControls.Keys)
			{
				MultiWeekDayControl multiWeekDayControl2 = _dayControls[key2];
				if (list.Count > num + key2)
				{
					MonthDayViewModel dataContext2 = list[num + key2];
					multiWeekDayControl2.DataContext = dataContext2;
				}
			}
		}
		StartDate = value[0].Date;
		SetMonthText((StartDate.Day == 1 || StartDate.Month != StartDate.AddDays(6.0).Month) ? new DateTime?(StartDate.AddDays(6.0)) : ((DateTime?)null));
	}

	private void SetMonthText(DateTime? date)
	{
		if (date.HasValue)
		{
			_monthGrid.Visibility = Visibility.Visible;
			Grid monthGrid = _monthGrid;
			if (monthGrid != null && monthGrid.Children.Count == 2 && _monthGrid.Children[1] is TextBlock textBlock)
			{
				textBlock.Text = ticktick_WPF.Util.DateUtils.FormatYearMonth(date.Value, (base.ActualWidth < 400.0) ? DateFormatLen.Middle : DateFormatLen.Long);
				_monthTextWidth = Utils.MeasureStringWidth(textBlock.Text, 24.0, FontWeights.Bold);
			}
			_monthGridStory = new Storyboard();
			DoubleAnimation doubleAnimation = AnimationUtils.GetDoubleAnimation(null, 1.0, 40);
			DoubleAnimation doubleAnimation2 = AnimationUtils.GetDoubleAnimation(null, 0.0, 300);
			doubleAnimation2.BeginTime = TimeSpan.FromMilliseconds(500.0);
			Storyboard.SetTarget(doubleAnimation, _monthGrid);
			Storyboard.SetTarget(doubleAnimation2, _monthGrid);
			Storyboard.SetTargetProperty(doubleAnimation, new PropertyPath(UIElement.OpacityProperty));
			Storyboard.SetTargetProperty(doubleAnimation2, new PropertyPath(UIElement.OpacityProperty));
			_monthGridStory.Children.Add(doubleAnimation);
			_monthGridStory.Children.Add(doubleAnimation2);
			_monthGridStory.Completed += delegate
			{
				foreach (MultiWeekDayControl value in _dayControls.Values)
				{
					if (_monthGrid.Opacity <= 0.1)
					{
						value.ShowDateText();
					}
				}
			};
			return;
		}
		_monthGrid.Visibility = Visibility.Collapsed;
		_borderBrush = null;
		_monthTextBorder.Background = null;
		_monthGridStory = null;
		foreach (MultiWeekDayControl value2 in _dayControls.Values)
		{
			value2.ShowDateText();
		}
	}

	public void ShowMonthText()
	{
		if (_monthGridStory != null)
		{
			int num = ((_monthTextWidth > 5.0) ? ((int)((_monthTextWidth - 5.0) / (FindDayControlByIndex(3)?.ActualWidth ?? 10.0)) + 1) : 0);
			foreach (MultiWeekDayControl value in _dayControls.Values)
			{
				if (num > 0 && value.ActualWidth > 0.0)
				{
					value.HideDateText();
					num--;
				}
			}
		}
		_monthGridStory?.Begin();
	}

	private MultiWeekDayControl FindDayControlByIndex(int column)
	{
		if (_dayControls.TryGetValue(column, out var value))
		{
			return value;
		}
		return null;
	}

	public void ShowLoadMore(int column, bool locked, List<CalendarDisplayViewModel> children)
	{
		FindDayControlByIndex(column)?.ShowLoadMore(children, locked);
	}

	public void SetEvents(IEnumerable<WeekEventModel> events, bool wait = true)
	{
		_events = events?.ToList();
		if (_eventsSetting && wait)
		{
			DelayActionHandlerCenter.TryDoAction(_uid, SetEvents, 100);
		}
		else
		{
			SetEvents();
		}
	}

	private void SetEvents(object sender, EventArgs args)
	{
		ThreadUtil.DetachedRunOnUiThread(SetEvents);
	}

	private void SetEvents()
	{
		_eventsSetting = true;
		if (_events != null)
		{
			List<WeekEventModel> events = _events;
			List<WeekEventModel> taskBar = events.Where((WeekEventModel e) => !e.Data.IsLoadMore).ToList();
			List<WeekEventModel> loadMore = events.Where((WeekEventModel e) => e.Data.IsLoadMore).ToList();
			SetTaskBar(taskBar);
			SetLoadMore(loadMore);
		}
		_eventsSetting = false;
	}

	private void SetLoadMore(List<WeekEventModel> events)
	{
		int num = events.Count - _loadMores.Count;
		if (num > 0)
		{
			for (int i = 0; i < num; i++)
			{
				CalLoadMore calLoadMore = new CalLoadMore
				{
					Visibility = Visibility.Hidden
				};
				_loadMores.Add(calLoadMore);
				_spanItems.Children.Add(calLoadMore);
			}
		}
		for (int j = 0; j < _loadMores.Count; j++)
		{
			CalLoadMore calLoadMore2 = _loadMores[j];
			if (calLoadMore2 == null)
			{
				continue;
			}
			if (j < events.Count && !CalendarGeoHelper.IsColumnHide(events[j].Column))
			{
				double num2 = base.ActualWidth / (double)CalendarGeoHelper.CalColumns;
				calLoadMore2.Margin = new Thickness(CalendarGeoHelper.GetBarLeft(num2, events[j].Column) + ((events[j].Data.LoadMoreWidth == 0.0) ? 0.0 : (num2 - events[j].Data.LoadMoreWidth)), events[j].Row * CalendarGeoHelper.TaskBarHeight, 0.0, 0.0);
				calLoadMore2.Width = ((events[j].Data.LoadMoreWidth == 0.0) ? CalendarGeoHelper.GetBarWidth(num2, events[j].Column, events[j].ColumnSpan) : events[j].Data.LoadMoreWidth);
				calLoadMore2.SetFontSize();
				calLoadMore2.Height = CalendarGeoHelper.TaskBarHeight;
				calLoadMore2.DataContext = events[j].Data;
				calLoadMore2.SetMoreBorderMargin(events[j].Data.LoadMoreWidth == 0.0);
				calLoadMore2.Visibility = Visibility.Visible;
			}
			else
			{
				calLoadMore2.DataContext = null;
				if (calLoadMore2.Visibility == Visibility.Visible)
				{
					calLoadMore2.Visibility = Visibility.Hidden;
				}
			}
		}
	}

	private void SetTaskBar(IReadOnlyList<WeekEventModel> events)
	{
		CalendarControl calendarControl = Utils.FindParent<CalendarControl>(this);
		BlockingSet<string> blockingSet = calendarControl?.BatchSelected;
		Dictionary<string, int> dictionary = calendarControl?.BatchSelectedRow;
		int num = ((LocalSettings.Settings.TimeFormat == "12Hour") ? 9 : 7);
		bool flag = base.ActualWidth / (double)CalendarGeoHelper.CalColumns >= CalendarGeoHelper.CalTitleFontSize * (double)(num + 3) - 4.0;
		int num2 = events.Count - _taskBars.Count;
		if (num2 > 0)
		{
			for (int i = 0; i < num2; i++)
			{
				TaskBar taskBar = new TaskBar
				{
					Visibility = Visibility.Hidden
				};
				_taskBars.Add(taskBar);
				_spanItems.Children.Add(taskBar);
			}
		}
		for (int j = 0; j < _taskBars.Count; j++)
		{
			TaskBar taskBar2 = _taskBars[j];
			if (taskBar2 == null)
			{
				continue;
			}
			taskBar2.SetDragBackRect(show: false);
			taskBar2.SetValue(Panel.ZIndexProperty, 5);
			if (j < events.Count)
			{
				WeekEventModel weekEventModel = events[j];
				double num3 = base.ActualWidth / (double)CalendarGeoHelper.CalColumns;
				taskBar2.Margin = new Thickness(CalendarGeoHelper.GetBarLeft(num3, weekEventModel.Column), weekEventModel.Row * CalendarGeoHelper.TaskBarHeight, 0.0, 0.0);
				taskBar2.Width = Math.Max(0.0, CalendarGeoHelper.GetBarWidth(num3 - weekEventModel.Data.LoadMoreWidth, weekEventModel.Column, weekEventModel.ColumnSpan) - 1.0);
				taskBar2.SetFontSize();
				taskBar2.DataContext = weekEventModel.Data;
				taskBar2.Visibility = Visibility.Visible;
				taskBar2.TimeText.Visibility = ((!(StaticPropertiesModel.Instance.AltKeyDown ? (taskBar2.Width > CalendarGeoHelper.CalTitleFontSize * (double)num + 12.0) : (taskBar2.Width > CalendarGeoHelper.CalTitleFontSize * (double)num - 4.0))) ? Visibility.Collapsed : Visibility.Visible);
				bool flag2 = !weekEventModel.Data.IsTaskIconType || flag;
				bool flag3 = StaticPropertiesModel.Instance.AltKeyDown || flag2;
				weekEventModel.Data.WidthEnoughShowIcon = flag3;
				weekEventModel.Data.BatchSelected = blockingSet != null && weekEventModel.Data.CanBatchSelected && blockingSet.Contains(weekEventModel.Data.TaskId);
				weekEventModel.Data.BatchDragging = weekEventModel.Data.BatchSelected && TickDragHelper.Dragging;
				taskBar2.SetShowIcon(flag3, weekEventModel.Data.ForceShowIcon);
				if (weekEventModel.Data.BatchSelected && dictionary != null)
				{
					dictionary[weekEventModel.Data.TaskId] = weekEventModel.Row;
				}
			}
			else
			{
				taskBar2.DataContext = null;
				if (taskBar2.Visibility == Visibility.Visible)
				{
					taskBar2.Visibility = Visibility.Hidden;
				}
			}
		}
	}

	public void SetShowIcon()
	{
		int num = ((LocalSettings.Settings.TimeFormat == "12Hour") ? 9 : 7);
		bool flag = base.ActualWidth / (double)CalendarGeoHelper.CalColumns >= CalendarGeoHelper.CalTitleFontSize * (double)(num + 3) - 4.0;
		foreach (TaskBar taskBar in _taskBars)
		{
			taskBar.TimeText.Visibility = ((!(taskBar.Width > CalendarGeoHelper.CalTitleFontSize * (double)num - 4.0)) ? Visibility.Collapsed : Visibility.Visible);
			CalendarDisplayViewModel model = taskBar.GetModel();
			if (model != null)
			{
				model.SetIcon(notify: true);
				taskBar.TimeText.Visibility = ((!(StaticPropertiesModel.Instance.AltKeyDown ? (taskBar.Width > CalendarGeoHelper.CalTitleFontSize * (double)num + 12.0) : (taskBar.Width > CalendarGeoHelper.CalTitleFontSize * (double)num - 4.0))) ? Visibility.Collapsed : Visibility.Visible);
				bool flag2 = !model.IsTaskIconType || flag;
				bool showIcon = StaticPropertiesModel.Instance.AltKeyDown || flag2;
				taskBar.SetShowIcon(showIcon, model.ForceShowIcon);
			}
		}
	}

	public void SetColor()
	{
		foreach (TaskBar taskBar in _taskBars)
		{
			taskBar.GetModel()?.SetColor();
		}
	}

	public void SetDropTarget(MouseEventArgs arg)
	{
		bool flag = IsDragHover(arg);
		foreach (MultiWeekDayControl value in _dayControls.Values)
		{
			if (flag && value.IsDragHover(arg))
			{
				value.SetDropTarget();
			}
			else
			{
				value.ClearDropTarget();
			}
		}
	}

	public bool IsDragHover(MouseEventArgs e)
	{
		Point position = e.GetPosition(this);
		double x = position.X;
		double y = position.Y;
		if (x > 0.0 && x < base.ActualWidth && y > 0.0)
		{
			return y < base.ActualHeight;
		}
		return false;
	}

	public void ClearDropTarget()
	{
		foreach (MultiWeekDayControl value in _dayControls.Values)
		{
			value.ClearDropTarget();
		}
	}

	private void OnSizeChanged(object sender, SizeChangedEventArgs e)
	{
		if (Math.Abs(e.PreviousSize.Width - e.NewSize.Width) > 0.0)
		{
			SetEvents(_events);
		}
	}

	public void ClearItems(bool onlyData)
	{
		_taskBars.Clear();
		if (onlyData)
		{
			foreach (CalLoadMore loadMore in _loadMores)
			{
				_spanItems.Children.Remove(loadMore);
			}
			if (_spanItems.Children.Count > 14)
			{
				_spanItems.Children.RemoveRange(14, _spanItems.Children.Count - 14);
			}
			foreach (object child in _spanItems.Children)
			{
				if (child is TaskBar taskBar)
				{
					taskBar.DataContext = null;
					taskBar.Visibility = Visibility.Hidden;
					_taskBars.Add(taskBar);
				}
			}
		}
		else
		{
			_spanItems.Children.Clear();
		}
		foreach (MultiWeekDayControl value in _dayControls.Values)
		{
			value.ClearData();
		}
		_events?.Clear();
		_loadMores.Clear();
	}

	public void TryShowAddWindow(DateTime startDate)
	{
		foreach (MultiWeekDayControl value in _dayControls.Values)
		{
			if (value.DataContext is MonthDayViewModel monthDayViewModel && monthDayViewModel.Date == startDate)
			{
				value.ShowAddTaskPopup(null, startDate, startDate);
			}
		}
	}

	public void AddDays(int days)
	{
		StartDate = StartDate.AddDays(days);
		foreach (MultiWeekDayControl value in _dayControls.Values)
		{
			if (value.DataContext is MonthDayViewModel monthDayViewModel)
			{
				monthDayViewModel.AddDays(days);
			}
		}
		SetMonthText((StartDate.Day == 1 || StartDate.Month != StartDate.AddDays(6.0).Month) ? new DateTime?(StartDate.AddDays(6.0)) : ((DateTime?)null));
	}

	public void SetCurrentMonth(DateTime currentMonth)
	{
		foreach (MultiWeekDayControl value in _dayControls.Values)
		{
			if (value.DataContext is MonthDayViewModel monthDayViewModel)
			{
				monthDayViewModel.SetCurrentMonth(currentMonth);
			}
		}
	}

	public void FlashNaviDates(List<DateTime> navDates, bool delay = false)
	{
		foreach (MultiWeekDayControl value in _dayControls.Values)
		{
			if (value.DataContext is MonthDayViewModel monthDayViewModel && navDates.Contains(monthDayViewModel.Date))
			{
				value.Flash(delay);
			}
		}
	}

	public MultiWeekDayControl GetHoverGrid(double positionX)
	{
		double num = base.ActualWidth / (double)_dayControls.Count;
		int num2 = (int)(positionX / num);
		if (num2 >= 0 && num2 < _dayControls.Count)
		{
			return _dayControls[num2];
		}
		return null;
	}

	public void RemoveDraggingTask()
	{
		foreach (TaskBar taskBar in _taskBars)
		{
			object dataContext = taskBar.DataContext;
			CalendarDisplayViewModel model = dataContext as CalendarDisplayViewModel;
			if (model != null && model.Dragging)
			{
				_taskBars.Remove(taskBar);
				_spanItems.Children.Remove(taskBar);
				_events.RemoveAll((WeekEventModel e) => e.Data == model);
				break;
			}
		}
	}

	public bool ExistModel(CalendarDisplayViewModel model)
	{
		return _taskBars.FirstOrDefault((TaskBar t) => model.IsEqualTask(t.DataContext as CalendarDisplayViewModel)) != null;
	}

	public void RemoveItem(CalendarDisplayViewModel model)
	{
		TaskBar taskBar = _taskBars.FirstOrDefault((TaskBar t) => model.IsEqualTask(t.DataContext as CalendarDisplayViewModel));
		_taskBars.Remove(taskBar);
		_spanItems.Children.Remove(taskBar);
	}

	public void TryAddItem(CalendarDisplayViewModel model, DateTime start, DateTime end, bool dragLeft)
	{
		TaskBar taskBar = _taskBars.FirstOrDefault((TaskBar t) => model.IsEqualTask(t.DataContext as CalendarDisplayViewModel));
		if (taskBar == null)
		{
			taskBar = new TaskBar();
			_taskBars.Add(taskBar);
			_spanItems.Children.Add(taskBar);
			double num = base.ActualWidth / (double)CalendarGeoHelper.CalColumns;
			int column = ((!(start <= StartDate)) ? ((int)(start.Date - StartDate.Date).TotalDays) : 0);
			DateTime dateTime = ((start <= StartDate) ? StartDate.Date : start.Date);
			DateTime dateTime2 = ((end <= StartDate.AddDays(7.0)) ? end.Date : StartDate.AddDays(7.0).Date);
			int range = (int)Math.Max(1.0, (dateTime2 - dateTime).TotalDays);
			TaskBar taskBar2 = taskBar;
			double barLeft = CalendarGeoHelper.GetBarLeft(num, column);
			_ = CalendarGeoHelper.TaskBarHeight;
			taskBar2.Margin = new Thickness(barLeft, 0.0, 0.0, 0.0);
			taskBar.Width = Math.Max(0.0, CalendarGeoHelper.GetBarWidth(num, column, range) - 1.0);
			taskBar.DataContext = model;
			int num2 = ((LocalSettings.Settings.TimeFormat == "12Hour") ? 9 : 7);
			taskBar.TimeText.Visibility = ((!(taskBar.Width > CalendarGeoHelper.CalTitleFontSize * (double)num2 - 4.0)) ? Visibility.Collapsed : Visibility.Visible);
			model.WidthEnoughShowIcon = num >= CalendarGeoHelper.CalTitleFontSize * (double)(num2 + 3) - 4.0;
			bool showIcon = !model.IsTaskIconType || model.WidthEnoughShowIcon;
			taskBar.SetShowIcon(showIcon, model.ForceShowIcon);
		}
		else
		{
			double dayWidth = base.ActualWidth / (double)CalendarGeoHelper.CalColumns;
			int column2 = ((!(start <= StartDate)) ? ((int)(start.Date - StartDate.Date).TotalDays) : 0);
			DateTime dateTime3 = ((start <= StartDate) ? StartDate.Date : start.Date);
			DateTime dateTime4 = ((end <= StartDate.AddDays(7.0)) ? end.Date : StartDate.AddDays(7.0).Date);
			int range2 = (int)Math.Max(1.0, (dateTime4 - dateTime3).TotalDays);
			taskBar.Margin = new Thickness(CalendarGeoHelper.GetBarLeft(dayWidth, column2), taskBar.Margin.Top, 0.0, 0.0);
			taskBar.Width = Math.Max(0.0, CalendarGeoHelper.GetBarWidth(dayWidth, column2, range2) - 1.0);
		}
		taskBar.SetValue(Panel.ZIndexProperty, 10);
		taskBar.SetDragBackRect(show: true);
	}

	public void SetItemBatchSelected(BlockingSet<string> batchSelected)
	{
		if (_events == null || batchSelected == null)
		{
			return;
		}
		foreach (WeekEventModel @event in _events)
		{
			@event.Data.BatchSelected = @event.Data.CanBatchSelected && batchSelected.Contains(@event.Data.TaskId);
			@event.Data.BatchDragging = @event.Data.BatchSelected && TickDragHelper.Dragging;
		}
	}

	public void SetBatchDragging(bool dragging)
	{
		if (_events == null)
		{
			return;
		}
		foreach (WeekEventModel @event in _events)
		{
			@event.Data.BatchDragging = @event.Data.BatchSelected && dragging;
		}
	}

	public async void SetDragTasks(List<WeekEventModel> items)
	{
		SetDragLoadMores(items);
		List<WeekEventModel> list = items?.Where((WeekEventModel i) => !i.Data.IsLoadMore).ToList();
		if (list == null || list.Count == 0)
		{
			if (_dragItems.Count <= 0)
			{
				return;
			}
			foreach (string item in _dragItems.Keys.ToList())
			{
				if (_dragItems.TryGetValue(item, out var value))
				{
					_spanItems.Children.Remove(value);
				}
			}
			_dragItems.Clear();
			return;
		}
		List<string> list2 = _dragItems.Keys.ToList();
		int num = ((LocalSettings.Settings.TimeFormat == "12Hour") ? 9 : 7);
		bool flag = base.ActualWidth / (double)CalendarGeoHelper.CalColumns >= CalendarGeoHelper.CalTitleFontSize * (double)(num + 3) - 4.0;
		foreach (WeekEventModel item2 in list)
		{
			CalendarDisplayViewModel data = item2.Data;
			list2.Remove(item2.Data.TaskId);
			if (!_dragItems.TryGetValue(item2.Data.TaskId, out var value2))
			{
				value2 = new TaskBar(isDragMode: true);
				value2.SetValue(Panel.ZIndexProperty, 20);
				_spanItems.Children.Add(value2);
				_dragItems[item2.Data.TaskId] = value2;
			}
			value2.Visibility = Visibility.Visible;
			value2.DataContext = data;
			double num2 = base.ActualWidth / (double)CalendarGeoHelper.CalColumns;
			value2.Margin = new Thickness(num2 * (double)item2.Column, data.Row * CalendarGeoHelper.TaskBarHeight, 0.0, 0.0);
			value2.Width = Math.Max(0.0, (double)data.ColumnSpan * num2 - 1.0 - data.LoadMoreWidth);
			value2.TimeText.Visibility = ((!(value2.Width > CalendarGeoHelper.CalTitleFontSize * (double)num - 4.0)) ? Visibility.Collapsed : Visibility.Visible);
			data.WidthEnoughShowIcon = flag;
			bool showIcon = !data.IsTaskIconType || flag;
			value2.SetShowIcon(showIcon, data.ForceShowIcon);
			value2.SetFontSize();
		}
		foreach (string item3 in list2)
		{
			if (_dragItems.TryRemove(item3, out var value3))
			{
				_spanItems.Children.Remove(value3);
			}
		}
	}

	public async void SetDragLoadMores(List<WeekEventModel> items)
	{
		List<WeekEventModel> list = items?.Where((WeekEventModel i) => i.Data.IsLoadMore).ToList();
		if (list == null || list.Count == 0)
		{
			if (_dragMoreItems.Count <= 0)
			{
				return;
			}
			foreach (int item in _dragMoreItems.Keys.ToList())
			{
				if (_dragMoreItems.TryGetValue(item, out var value))
				{
					_spanItems.Children.Remove(value);
				}
			}
			_dragMoreItems.Clear();
			return;
		}
		List<int> list2 = _dragMoreItems.Keys.ToList();
		foreach (WeekEventModel item2 in list)
		{
			list2.Remove(item2.Column);
			if (!_dragMoreItems.TryGetValue(item2.Column, out var value2))
			{
				value2 = new CalLoadMore(isDragItem: true);
				value2.SetValue(Panel.ZIndexProperty, 20);
				_spanItems.Children.Add(value2);
				_dragMoreItems[item2.Column] = value2;
			}
			double num = base.ActualWidth / (double)CalendarGeoHelper.CalColumns;
			value2.Margin = new Thickness(CalendarGeoHelper.GetBarLeft(num, item2.Column) + ((item2.Data.LoadMoreWidth == 0.0) ? 0.0 : (num - item2.Data.LoadMoreWidth)), item2.Row * CalendarGeoHelper.TaskBarHeight, 0.0, 0.0);
			value2.Width = ((item2.Data.LoadMoreWidth == 0.0) ? CalendarGeoHelper.GetBarWidth(num, item2.Column, item2.ColumnSpan) : item2.Data.LoadMoreWidth);
			value2.SetFontSize();
			value2.Height = CalendarGeoHelper.TaskBarHeight;
			value2.DataContext = item2.Data;
			value2.SetMoreBorderMargin(item2.Data.LoadMoreWidth == 0.0);
			value2.Visibility = Visibility.Visible;
		}
		foreach (int item3 in list2)
		{
			if (_dragMoreItems.TryRemove(item3, out var value3))
			{
				_spanItems.Children.Remove(value3);
			}
		}
	}

	public void SetDropTargets(DateTime start, DateTime end)
	{
		foreach (MultiWeekDayControl value in _dayControls.Values)
		{
			DateTime? date = value.GetDate();
			if (!date.HasValue)
			{
				value.ClearDropTarget();
			}
			else if (date.Value >= start && date.Value <= end)
			{
				value.SetDropTarget();
			}
			else
			{
				value.ClearDropTarget();
			}
		}
	}

	public void ClearDropTargets()
	{
		foreach (MultiWeekDayControl value in _dayControls.Values)
		{
			value.ClearDropTarget();
		}
	}
}
