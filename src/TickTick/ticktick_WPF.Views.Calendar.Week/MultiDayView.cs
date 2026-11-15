using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Service.Display;
using ticktick_WPF.Util;
using ticktick_WPF.Util.DragAndDrop;
using ticktick_WPF.Util.Sync;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Calendar.Month;
using ticktick_WPF.Views.Detail;
using ticktick_WPF.Views.Print;
using ticktick_WPF.Views.Project;

namespace ticktick_WPF.Views.Calendar.Week;

public class MultiDayView : Grid, INavigate, IDragBarEvent, IDragCellEvent
{
	public int DisplayDays = 7;

	public double DayViewWidth;

	private bool _cellHandling;

	private readonly RowDefinition _firstRow;

	private readonly GridSplitter _splitter;

	private WeekGridViewModel _model = new WeekGridViewModel
	{
		StartDate = DateTime.Today
	};

	private List<CalendarDisplayModel> _tasks;

	private CalendarTimelineView _timelineView;

	private MultiDayAllDayView _alldayView;

	private readonly Line _leftLine;

	private List<CalendarTimelineDayViewModel> _pointVms;

	private CalendarControl _calendarControl;

	private List<CalendarDisplayViewModel> _assembles;

	private TextBlock _weekNum;

	private Border _flashBorder;

	private bool _leftScroll;

	private Grid _linesGrid = new Grid
	{
		Margin = new Thickness(57.0, 0.0, 1.0, 0.0),
		ClipToBounds = true
	};

	private List<WeekEventModel> _allDayVms;

	private List<MultiDayLine> _vLines = new List<MultiDayLine>();

	private bool _loading;

	private bool _needReload;

	private bool _dragFromArrange;

	private double _dragStartY;

	private bool _cellMoving;

	private TaskCellViewModel _dragCellModel;

	private DateTime? _dragDueTime;

	private DateTime? _dragStartTime;

	private double _mouseScrollLeft;

	private bool _scrolling;

	private bool _showWeekend = true;

	private BlockingSet<DateTime> _scrollLoadedDate = new BlockingSet<DateTime>();

	private string _weeks;

	private string _mode;

	private IMultiDayGrid _dragOverGrid;

	private bool _autoHScroll;

	private DateTime? _dragBarSideDate;

	private bool _barSideDragging;

	private readonly CalendarTimeZoneDisplayView _timezones;

	private double _timeZoneWidth = 56.0;

	private TimeSpan? _batchDragDiff;

	private int? _batchDragDays;

	private DateTime? _batchDragStartDate;

	private bool _toAllDay;

	private bool _toPoint;

	public bool IsLocked { get; set; }

	public CalendarTimelineView TimelineView => _timelineView;

	public event EventHandler<(DateTime, DateTime)> DateRangeChanged;

	public MultiDayView(CalendarControl calendarControl)
	{
		base.Background = Brushes.Transparent;
		_calendarControl = calendarControl;
		_firstRow = new RowDefinition
		{
			MinHeight = 88.0,
			Height = new GridLength(LocalSettings.Settings.WeekAllDayHeight)
		};
		base.RowDefinitions.Add(_firstRow);
		base.RowDefinitions.Add(new RowDefinition());
		Line line = new Line
		{
			X1 = 0.0,
			X2 = 1.0,
			Stretch = Stretch.Fill,
			StrokeThickness = 1.0,
			VerticalAlignment = VerticalAlignment.Top,
			Margin = new Thickness(0.0, 32.0, 0.0, 0.0)
		};
		line.SetResourceReference(Shape.StrokeProperty, "BaseColorOpacity4");
		base.Children.Add(line);
		Line line2 = new Line
		{
			X1 = 0.0,
			X2 = 1.0,
			Stretch = Stretch.Fill,
			StrokeThickness = 1.0,
			VerticalAlignment = VerticalAlignment.Bottom,
			IsHitTestVisible = false
		};
		line2.SetResourceReference(Shape.StrokeProperty, "BaseColorOpacity4");
		base.Children.Add(line2);
		_leftLine = new Line
		{
			Y1 = 0.0,
			Y2 = 1.0,
			Stretch = Stretch.Fill,
			StrokeThickness = 1.0,
			HorizontalAlignment = HorizontalAlignment.Left,
			Margin = new Thickness(56.0, 32.0, 0.0, 0.0),
			IsHitTestVisible = false
		};
		_leftLine.SetValue(Grid.RowSpanProperty, 2);
		_leftLine.SetResourceReference(Shape.StrokeProperty, "BaseColorOpacity4");
		base.Children.Add(_leftLine);
		_splitter = new GridSplitter
		{
			Height = 8.0,
			Background = Brushes.Transparent,
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment = VerticalAlignment.Bottom,
			IsTabStop = false,
			FocusVisualStyle = null
		};
		_splitter.SetBinding(UIElement.IsHitTestVisibleProperty, StaticPropertiesModel.HitTestBinding);
		_splitter.DragCompleted += OnSplitDragCompleted;
		_splitter.SetValue(Panel.ZIndexProperty, 16);
		base.Children.Add(_splitter);
		InitAddDayView();
		InitTimelineView();
		_linesGrid.SetValue(Grid.RowSpanProperty, 2);
		base.Children.Add(_linesGrid);
		_timezones = new CalendarTimeZoneDisplayView
		{
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Stretch
		};
		base.Children.Add(_timezones);
		_weekNum = new TextBlock
		{
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Margin = new Thickness(0.0, 11.0, 0.0, 0.0),
			Padding = new Thickness(0.0, 0.0, 9.0, 0.0),
			TextAlignment = TextAlignment.Right,
			Background = Brushes.Transparent,
			FontSize = 10.0
		};
		_weekNum.SetResourceReference(TextBlock.ForegroundProperty, "BaseColorOpacity60");
		_weekNum.SetBinding(FrameworkElement.WidthProperty, new Binding("Width")
		{
			Source = _timezones
		});
		base.Children.Add(_weekNum);
		base.SizeChanged += OnSizeChanged;
		base.Loaded += delegate
		{
			SetDisplayDays(DisplayDays, _showWeekend);
			BindEvents();
		};
		base.Unloaded += delegate
		{
			UnbindEvents();
		};
	}

	private void OnSizeChanged(object sender, SizeChangedEventArgs e)
	{
		if (Math.Abs(e.PreviousSize.Width - e.NewSize.Width) > 1.0 && !Utils.IsEmptyDate(_model?.StartDate))
		{
			SetDisplayDays(DisplayDays, _showWeekend);
		}
		double num = e.NewSize.Height * 0.5;
		if (Math.Abs(num - _firstRow.MaxHeight) > 4.0)
		{
			_firstRow.MaxHeight = num;
			_alldayView?.SetScrollMaxHeight(num);
		}
	}

	public void SetDisplayDays(int days, bool showWeekend = true)
	{
		bool flag = DisplayDays != days;
		DisplayDays = days;
		_showWeekend = showWeekend;
		int timeZoneCount = CalendarGeoHelper.GetTimeZoneCount();
		if (base.ActualWidth < (double)timeZoneCount * 56.0 + 2.0)
		{
			return;
		}
		DayViewWidth = Math.Max((base.ActualWidth - (double)timeZoneCount * 56.0) / (double)days, 0.0);
		SetDayLines();
		_timeZoneWidth = 56.0 * (double)timeZoneCount;
		_linesGrid.Margin = new Thickness(_timeZoneWidth + 1.0, 0.0, 1.0, 0.0);
		_leftLine.Margin = new Thickness(_timeZoneWidth, 32.0, 0.0, 0.0);
		_alldayView.Margin = new Thickness(_timeZoneWidth, 0.0, 0.0, 0.0);
		_timelineView.SetDisplayDays();
		_alldayView.SetDisplayDays();
		_model = WeekGridViewModel.Build(_model.StartDate, DisplayDays, showWeekend);
		for (int i = 0; i < _vLines.Count; i++)
		{
			MultiDayLine multiDayLine = _vLines[i];
			if (_model.Models.Count > i)
			{
				multiDayLine.Date = _model.Models[i].Date;
			}
			else
			{
				multiDayLine.Date = _model.StartDate.AddDays(i);
			}
		}
		_alldayView.SetData(_model.Models);
		if (flag)
		{
			LoadAllDayEvents();
			LoadPointEvents();
		}
	}

	private void SetDayLines()
	{
		int num = DisplayDays + 2;
		if (num > _vLines.Count)
		{
			int count = _vLines.Count;
			for (int i = 0; i < num - count; i++)
			{
				MultiDayLine multiDayLine = new MultiDayLine();
				_vLines.Add(multiDayLine);
				_linesGrid.Children.Add(multiDayLine);
			}
		}
		else
		{
			List<MultiDayLine> list = new List<MultiDayLine>();
			for (int j = num; j < _vLines.Count; j++)
			{
				list.Add(_vLines[j]);
			}
			list.ForEach(delegate(MultiDayLine l)
			{
				_vLines.Remove(l);
				_linesGrid.Children.Remove(l);
			});
		}
		for (int num2 = 0; num2 < _vLines.Count; num2++)
		{
			MultiDayLine multiDayLine2 = _vLines[num2];
			double num3 = (double)(num2 - 1) * DayViewWidth;
			multiDayLine2.RenderTransform = new TranslateTransform
			{
				X = num3
			};
			multiDayLine2.Tag = num3;
			multiDayLine2.Width = DayViewWidth;
		}
	}

	private void UnbindEvents()
	{
		GlobalEventManager.TimeLineTimeZonesChanged -= OnTimeLineTimeZoneChanged;
		CalendarEventChangeNotifier.TitleChanged -= NotifyEventTitleChanged;
		DataChangedNotifier.TimeFormatChanged -= ReloadOnTimeFormatChanged;
		LocalSettings.Settings.PropertyChanged -= OnSettingsChanged;
		_timelineView.ScrollChanged -= OnScrollChanged;
		TickDragHelper.DragDrop -= OnDragDrop;
		TickDragHelper.DragMoving -= OnDragOver;
	}

	private void OnDragOver(object sender, MouseEventArgs e)
	{
		if (!base.IsVisible)
		{
			return;
		}
		Window window = Window.GetWindow(this);
		if (window == null || !window.IsMouseCaptured || TaskDetailPopup.IsDragOver(e))
		{
			return;
		}
		Point position = e.GetPosition(this);
		if (position.X < _timeZoneWidth || position.X > base.ActualWidth || position.Y < 0.0 || position.Y > base.ActualHeight)
		{
			if (_batchDragDiff.HasValue)
			{
				OnDragDiffChanged(null);
			}
			_dragOverGrid?.OnDragLeave();
			TickDragHelper.SetShowDragWindow(show: true);
			TickDragHelper.RemoveDragOverCalendar();
			StopAutoHorizonScroll();
			_dragOverGrid = null;
			return;
		}
		if (position.X < _timeZoneWidth + 10.0 || position.X > base.ActualWidth - 10.0)
		{
			StartAutoHorizonScroll(position.X < _timeZoneWidth + 10.0);
		}
		else
		{
			StopAutoHorizonScroll();
		}
		int index = (int)((position.X - _timeZoneWidth) / DayViewWidth);
		bool num = position.Y > _firstRow.ActualHeight;
		IMultiDayGrid multiDayGrid = null;
		if (num)
		{
			multiDayGrid = _timelineView.GetDayView(index);
			TickDragHelper.SetShowDragWindow(show: false);
		}
		else
		{
			BlockingSet<string> selectedIds = GetSelectedIds();
			if (selectedIds != null && selectedIds.Count == 0)
			{
				OnDragDiffChanged(null);
			}
			multiDayGrid = _alldayView.GetDayView(index);
			TickDragHelper.SetShowDragWindow(show: true);
		}
		if (_dragOverGrid != multiDayGrid)
		{
			_dragOverGrid?.OnDragLeave();
			multiDayGrid?.OnDragEnter();
			_dragOverGrid = multiDayGrid;
		}
		multiDayGrid?.OnDragOver(e);
	}

	private void StopAutoHorizonScroll()
	{
		_autoHScroll = false;
	}

	private async void StartAutoHorizonScroll(bool isLeft)
	{
		if (_autoHScroll)
		{
			return;
		}
		_autoHScroll = true;
		await Task.Delay(300);
		while (_autoHScroll)
		{
			double offset = DayViewWidth / (double)(isLeft ? 10 : (-10));
			for (int i = 0; i < 10; i++)
			{
				await Task.Delay(10);
				OnTouchScroll(offset, force: true);
			}
			await Task.Delay(100);
		}
	}

	private async void OnDragDrop(object sender, MouseEventArgs e)
	{
		StopAutoHorizonScroll();
		if (_dragOverGrid != null)
		{
			if (_batchDragDiff.HasValue)
			{
				TimeSpan? batchDragDiff = _batchDragDiff;
				int? batchDragDays = _batchDragDays;
				_batchDragDiff = null;
				_batchDragDays = null;
				CalendarControl parent = GetParent();
				if (parent != null)
				{
					await parent.OnBatchDragCellDrop(batchDragDiff.Value, batchDragDays.GetValueOrDefault(), _toAllDay, _toPoint);
				}
				OnDragDiffChanged(null);
			}
			else
			{
				BlockingSet<string> selectedIds = GetSelectedIds();
				if (selectedIds != null && selectedIds.Count == 0)
				{
					_timelineView.RemoveDraggingItem();
					if (_dragOverGrid is CalendarTimelineDayView)
					{
						_alldayView.RemoveDraggingItem();
					}
					_alldayView.ClearDropTargets();
					_dragOverGrid.OnDragDrop();
				}
			}
		}
		else if (base.IsVisible)
		{
			GetParent()?.SetBatchDragging(dragging: false);
			OnDragDiffChanged(null);
		}
		_dragOverGrid = null;
	}

	private void BindEvents()
	{
		GlobalEventManager.TimeLineTimeZonesChanged += OnTimeLineTimeZoneChanged;
		CalendarEventChangeNotifier.TitleChanged += NotifyEventTitleChanged;
		DataChangedNotifier.TimeFormatChanged += ReloadOnTimeFormatChanged;
		LocalSettings.Settings.PropertyChanged += OnSettingsChanged;
		_timelineView.ScrollToVerticalOffset(GetParent()?.TimeScrollVerticalOffset ?? 0.0);
		_timelineView.ScrollChanged += OnScrollChanged;
		TickDragHelper.DragDrop += OnDragDrop;
		TickDragHelper.DragMoving += OnDragOver;
	}

	private void OnTimeLineTimeZoneChanged(object sender, bool countChanged)
	{
		RefreshTimeLine(countChanged);
	}

	public void RefreshTimeLine(bool countChanged)
	{
		base.Dispatcher?.Invoke(delegate
		{
			if (countChanged)
			{
				SetDisplayDays(DisplayDays, _showWeekend);
				LoadAllDayEvents();
				LoadPointEvents();
			}
			_timezones?.SetTimeZones(CalendarGeoHelper.GetDisplayTimelineTimeZones(withCityName: false));
			_timelineView.RefreshTimeBlocks();
		});
	}

	private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
	{
		CalendarControl parent = GetParent();
		if (parent != null)
		{
			parent.TimeScrollVerticalOffset = e.VerticalOffset;
		}
	}

	public void ScrollToVerticalOffset(double vOffset)
	{
		_timelineView?.ScrollToVerticalOffset(vOffset);
	}

	public bool OnSelection()
	{
		return false;
	}

	public void SetBarDragging(bool b)
	{
		_barSideDragging = b;
		if (!b)
		{
			StopAutoHorizonScroll();
			_dragBarSideDate = null;
		}
	}

	public bool GetBarDragging()
	{
		return _barSideDragging;
	}

	public DateTime? DragSideOver(MouseEventArgs e, CalendarDisplayViewModel model, bool dragLeft)
	{
		Point position = e.GetPosition(this);
		int num;
		if (!(position.X < 60.0) && !(position.X > base.ActualWidth) && !(position.Y < 0.0))
		{
			num = ((!(position.Y > _firstRow.ActualHeight)) ? 1 : 0);
			if (num != 0 && ((position.X > 60.0 && position.X < 90.0) || (position.X > base.ActualWidth - 30.0 && position.X < base.ActualWidth)))
			{
				StartAutoHorizonScroll(position.X < 90.0);
				goto IL_00c4;
			}
		}
		else
		{
			num = 0;
		}
		StopAutoHorizonScroll();
		goto IL_00c4;
		IL_00c4:
		int index = (int)((position.X - 60.0) / DayViewWidth);
		MultiDayAllDayGrid multiDayAllDayGrid = null;
		if (num != 0)
		{
			multiDayAllDayGrid = _alldayView.GetDayView(index);
		}
		DateTime? dateTime = multiDayAllDayGrid?.GetDate();
		if (dateTime.HasValue && dateTime != _dragBarSideDate)
		{
			_dragBarSideDate = dateTime;
			if (!model.DisplayStartDate.HasValue)
			{
				return dateTime;
			}
			DateTime dateTime2 = model.DisplayStartDate.Value;
			DateTime dateTime3 = model.DisplayDueDate ?? model.DisplayStartDate.Value;
			if (dateTime2.Date == dateTime3.Date)
			{
				dateTime3 = dateTime3.Date.AddDays(1.0);
			}
			if (dragLeft)
			{
				if (dateTime < dateTime3.Date)
				{
					dateTime2 = dateTime.Value;
				}
				else
				{
					dateTime2 = dateTime3.Date;
					dateTime3 = dateTime.Value.AddDays(1.0);
				}
			}
			else if (dateTime > dateTime2.Date)
			{
				dateTime3 = dateTime.Value.AddDays(1.0);
			}
			else
			{
				dateTime3 = dateTime2.Date;
				dateTime2 = dateTime.Value;
			}
			_alldayView.SetDragSideBar(model, dateTime2.Date, dateTime3.Date, dragLeft);
		}
		return _dragBarSideDate;
	}

	private void OnSettingsChanged(object sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == "StartWeekOfYear" || e.PropertyName == "ShowWeek")
		{
			SetWeekNum();
		}
		if (e.PropertyName == "EnableLunar" || e.PropertyName == "EnableHoliday")
		{
			_model = WeekGridViewModel.Build(_model.StartDate, DisplayDays, _showWeekend);
			_alldayView.SetData(_model.Models);
		}
		if (e.PropertyName == "TimeFormat")
		{
			_timelineView.SetPointer();
		}
	}

	private void ReloadOnTimeFormatChanged(object sender, EventArgs e)
	{
		_timelineView?.ResetTimeLine();
		LoadPointEvents();
	}

	private void NotifyEventTitleChanged(object sender, TextExtra extra)
	{
		_timelineView.NotifyEventTitleChanged(extra.Id, extra.Text);
		if (_tasks != null && _tasks.Any())
		{
			_tasks.Where((CalendarDisplayModel m) => m.SourceViewModel.Id == extra.Id && m.SourceViewModel.Type == DisplayType.Event).ToList().ForEach(delegate(CalendarDisplayModel m)
			{
				m.SourceViewModel.Title = extra.Text;
			});
		}
	}

	public DateTime Today()
	{
		return GoTo(DateTime.Today);
	}

	public DateTime Next()
	{
		int num = DisplayDays + ((!_showWeekend) ? 2 : 0);
		RemoveFlashBorder();
		DateTime dateTime = _model.StartDate.AddDays((num > 7 && num < 14) ? 7 : num);
		Load(dateTime);
		_alldayView.ScrollToTop();
		HabitSyncService.SyncHabitCheckInCalendar(dateTime);
		_cellHandling = false;
		return dateTime;
	}

	public DateTime Last()
	{
		int num = DisplayDays + ((!_showWeekend) ? 2 : 0);
		RemoveFlashBorder();
		DateTime dateTime = _model.StartDate.AddDays(-1 * ((num > 7 && num < 14) ? 7 : num));
		Load(dateTime);
		_alldayView.ScrollToTop();
		HabitSyncService.SyncHabitCheckInCalendar(dateTime);
		_cellHandling = false;
		return dateTime;
	}

	public DateTime GoTo(DateTime dateTime, bool checkStart = true, bool isScrollToNow = false, bool delay = false)
	{
		bool flag = dateTime == DateTime.Today;
		RemoveFlashBorder();
		dateTime = (checkStart ? ParseStartDate(dateTime) : dateTime);
		Load(dateTime, flag || isScrollToNow, delay);
		_alldayView.ScrollToTop();
		HabitSyncService.SyncHabitCheckInCalendar(dateTime);
		_cellHandling = false;
		return dateTime;
	}

	private DateTime ParseStartDate(DateTime dateTime)
	{
		if (DisplayDays >= 7 || !_showWeekend)
		{
			dateTime = Utils.GetWeekStart(dateTime);
		}
		return dateTime;
	}

	public void SetCellHandling(bool value)
	{
		_cellHandling = value;
	}

	private async void TryReload()
	{
		if ((!TickDragHelper.Dragging || TickDragHelper.Dropped) && !_barSideDragging && base.IsVisible)
		{
			await Load(null);
		}
	}

	public void Reload(bool byThemeChange = false)
	{
		base.Dispatcher?.Invoke(TryReload);
	}

	public CalendarControl GetParent()
	{
		if (_calendarControl == null)
		{
			_calendarControl = Utils.FindParent<CalendarControl>(this);
		}
		return _calendarControl;
	}

	private void InitAddDayView()
	{
		_alldayView = new MultiDayAllDayView(this);
		base.Children.Add(_alldayView);
	}

	private void InitTimelineView()
	{
		_timelineView = new CalendarTimelineView(this);
		_timelineView.SetValue(Grid.RowProperty, 1);
		base.Children.Add(_timelineView);
	}

	private async void SmoothMouthScroll(int eDelta)
	{
		_scrolling = true;
		DelayActionHandlerCenter.RemoveAction("CalendarInputScroll");
		if (eDelta >= 0)
		{
			if (eDelta < 3)
			{
				eDelta = 3;
			}
			if (_mouseScrollLeft < 0.0)
			{
				_mouseScrollLeft = ((eDelta > 40) ? DayViewWidth : ((double)eDelta));
			}
			else
			{
				_mouseScrollLeft += ((eDelta > 40) ? DayViewWidth : ((double)eDelta));
			}
			double delta = _mouseScrollLeft / 325.0;
			double left = _mouseScrollLeft;
			_leftScroll = false;
			for (int i = 25; i >= 1; i--)
			{
				if (Math.Abs(_mouseScrollLeft - left) > 0.1)
				{
					return;
				}
				double num = delta * (double)i;
				left = (_mouseScrollLeft -= num);
				TouchScroll(num);
				_timelineView.OnTouchScroll(num, _showWeekend);
				_alldayView.OnTouchScroll(num, _showWeekend);
				await Task.Delay(8);
			}
		}
		else
		{
			if (eDelta > -3)
			{
				eDelta = -3;
			}
			if (_mouseScrollLeft > 0.0)
			{
				_mouseScrollLeft = ((eDelta < -40) ? (0.0 - DayViewWidth) : ((double)eDelta));
			}
			else
			{
				_mouseScrollLeft += ((eDelta < -40) ? (0.0 - DayViewWidth) : ((double)eDelta));
			}
			double left = _mouseScrollLeft / 325.0;
			double delta = _mouseScrollLeft;
			_leftScroll = true;
			for (int i = 25; i >= 1; i--)
			{
				if (Math.Abs(_mouseScrollLeft - delta) > 0.1)
				{
					return;
				}
				double num2 = left * (double)i;
				delta = (_mouseScrollLeft -= num2);
				TouchScroll(num2);
				_timelineView.OnTouchScroll(num2, _showWeekend);
				_alldayView.OnTouchScroll(num2, _showWeekend);
				await Task.Delay(8);
			}
		}
		DelayActionHandlerCenter.TryDoAction("CalendarInputScroll", TryStopInputScroll, 50);
	}

	private async Task MoveDay(int addDays)
	{
		if (!_scrolling)
		{
			_scrolling = true;
			SetStartDate(_model.StartDate.AddDays(addDays));
			double diff = (double)addDays * DayViewWidth;
			diff /= 6.0;
			int times = 6;
			while (times > 0)
			{
				TouchScroll(-1.0 * diff);
				_timelineView.OnTouchScroll(-1.0 * diff, _showWeekend);
				_alldayView.OnTouchScroll(-1.0 * diff, _showWeekend);
				times--;
				await Task.Delay(10);
			}
			_scrolling = false;
			Reload();
		}
	}

	public async Task Load(DateTime? startDate, bool positionNow = false, bool delay = false)
	{
		if (_scrolling)
		{
			return;
		}
		if (PopupStateManager.IsInAdd() || _cellHandling)
		{
			await Task.Delay(500);
			if (PopupStateManager.IsInAdd() || _cellHandling)
			{
				return;
			}
		}
		_scrollLoadedDate.Clear();
		CalendarControl parent = GetParent();
		bool isVirtual = parent?.IsVirtual ?? false;
		bool num = (parent == null || parent.ViewSourceType != CalendarViewSourceType.Widget) && (isVirtual || !UserDao.IsPro());
		DateTime? dateTime = _model?.StartDate;
		if (!num)
		{
			startDate = startDate ?? _model?.StartDate ?? DateTime.Today;
		}
		else
		{
			_firstRow.Height = new GridLength(_firstRow.MinHeight);
			startDate = ((DisplayDays == 7) ? Utils.GetWeekStart(new DateTime(2023, 9, 6)) : new DateTime(2023, 9, 6));
			_showWeekend = true;
			_timelineView.ScrollToTop();
		}
		_model = WeekGridViewModel.Build(startDate.Value, DisplayDays, _showWeekend);
		if (num)
		{
			MonthDayViewModel monthDayViewModel = _model.Models.FirstOrDefault((MonthDayViewModel m) => m.Date == new DateTime(2023, 9, 6));
			if (monthDayViewModel != null)
			{
				monthDayViewModel.IsToday = true;
			}
		}
		bool flag = dateTime != _model.StartDate;
		if (_loading && !flag)
		{
			_needReload = true;
			return;
		}
		_needReload = false;
		_loading = true;
		try
		{
			_timelineView.SetTimePointVisible(DateTime.Today >= _model.StartDate && DateTime.Today <= _model.EndDate);
			for (int num2 = 0; num2 < _vLines.Count; num2++)
			{
				MultiDayLine multiDayLine = _vLines[num2];
				if (_vLines[num2].RenderTransform is TranslateTransform translateTransform)
				{
					translateTransform.X = (double)(num2 - 1) * DayViewWidth;
					multiDayLine.Tag = translateTransform.X;
				}
				if (_model.Models.Count > num2)
				{
					multiDayLine.Date = _model.Models[num2].Date;
				}
				else
				{
					multiDayLine.Date = _model.StartDate.AddDays(num2);
				}
			}
			this.DateRangeChanged?.Invoke(null, (_model.StartDate, _model.EndDate));
			SetWeekNum();
			InitTimeLineBoard();
			_alldayView.SetData(_model.Models);
			UpdateLayout();
			if (delay)
			{
				await Task.Delay(100);
			}
			ProjectExtra projectFilter = ((parent != null && parent.ViewSourceType == CalendarViewSourceType.WithList) ? LocalSettings.Settings.ExtraSettings.CalendarParallelSelectProject : ProjectExtra.Deserialize(LocalSettings.Settings.CalendarFilterData));
			if (isVirtual)
			{
				_tasks = CalendarDisplayModel.GetVirtualModels(_model.StartDate, _model.EndDate);
			}
			else
			{
				List<CalendarDisplayModel> tasks = ((!UserDao.IsPro() && (parent == null || parent.ViewSourceType != CalendarViewSourceType.Widget)) ? new List<CalendarDisplayModel>() : (await CalendarDisplayService.GetDisplayModels(_model.StartDate.AddDays(-1.0), _model.StartDate.AddDays(DisplayDays + 2), withCourses: true, withFocus: false, projectFilter)));
				_tasks = tasks;
			}
			int waitTimes = 200;
			while (base.ActualWidth == 0.0 && waitTimes > 0)
			{
				await Task.Delay(5);
				waitTimes--;
			}
			LoadAllDayEvents();
			LoadPointEvents();
			if (positionNow)
			{
				_timelineView.ScrollToCurrentTime();
			}
		}
		finally
		{
			await Task.Delay(100);
			_loading = false;
			if (_needReload)
			{
				_needReload = false;
				Load(null);
			}
		}
	}

	public async Task ReloadWithTempTask(CalendarDisplayModel model)
	{
		CalendarControl parent = GetParent();
		ProjectExtra projectFilter = ((parent != null && parent.ViewSourceType == CalendarViewSourceType.WithList) ? LocalSettings.Settings.ExtraSettings.CalendarParallelSelectProject : ProjectExtra.Deserialize(LocalSettings.Settings.CalendarFilterData));
		List<CalendarDisplayModel> tasks = ((!UserDao.IsPro() && (parent == null || parent.ViewSourceType != CalendarViewSourceType.Widget)) ? new List<CalendarDisplayModel>() : (await CalendarDisplayService.GetDisplayModels(_model.StartDate.AddDays(-1.0), _model.StartDate.AddDays(DisplayDays + 2), withCourses: true, withFocus: false, projectFilter)));
		_tasks = tasks;
		_tasks.Add(model);
		LoadPointEvents();
	}

	public async Task ReloadWithoutTempTask()
	{
		CalendarControl parent = GetParent();
		ProjectExtra projectFilter = ((parent != null && parent.ViewSourceType == CalendarViewSourceType.WithList) ? LocalSettings.Settings.ExtraSettings.CalendarParallelSelectProject : ProjectExtra.Deserialize(LocalSettings.Settings.CalendarFilterData));
		List<CalendarDisplayModel> tasks = ((!UserDao.IsPro() && (parent == null || parent.ViewSourceType != CalendarViewSourceType.Widget)) ? new List<CalendarDisplayModel>() : (await CalendarDisplayService.GetDisplayModels(_model.StartDate.AddDays(-1.0), _model.StartDate.AddDays(DisplayDays + 2), withCourses: true, withFocus: false, projectFilter)));
		_tasks = tasks;
		LoadPointEvents();
	}

	private void LoadPointEvents(List<CalendarDisplayModel> extraTasks = null, HashSet<string> ignoreIds = null, bool checkDragOver = true, bool toPoint = false)
	{
		_pointVms = new List<CalendarTimelineDayViewModel>();
		List<CalendarDisplayModel> list = _tasks ?? new List<CalendarDisplayModel>();
		if (extraTasks != null && extraTasks.Any())
		{
			list = list.ToList();
			list.AddRange(extraTasks);
		}
		BlockingSet<string> batchSelectedIds = GetSelectedIds();
		for (int i = -1; i < DisplayDays + 1; i++)
		{
			DateTime currentDate = _model.StartDate.AddDays(i);
			List<CalendarDisplayModel> list2 = new List<CalendarDisplayModel>();
			if (list.Count > 0)
			{
				list2 = list.Where(delegate(CalendarDisplayModel task)
				{
					task.ExtraDiff = null;
					if (_batchDragDiff.HasValue && batchSelectedIds != null && batchSelectedIds.Any())
					{
						if (ignoreIds != null && ignoreIds.Contains(task.Id))
						{
							return false;
						}
						if (task.CanBatchSelect && batchSelectedIds.Contains(task.Id) && task.IsDragDisplayItem)
						{
							task.ExtraDiff = ((toPoint && !task.ToPoint) ? new TimeSpan?(TimeSpan.FromDays(_batchDragDays.GetValueOrDefault())) : _batchDragDiff);
						}
					}
					return DateUtils.IsTaskInDay(currentDate, task.DisplayStartDate, task.DisplayDueDate, task.IsAllDay);
				}).ToList();
			}
			List<TaskCellViewModel> list3 = new List<TaskCellViewModel>();
			if (list2.Count > 0)
			{
				list3.AddRange(list2.Select(delegate(CalendarDisplayModel task)
				{
					TaskCellViewModel taskCellViewModel = TaskCellViewModel.Build(task, currentDate);
					taskCellViewModel.IsDragDisplayItem = task.IsDragDisplayItem;
					if (batchSelectedIds != null && batchSelectedIds.Any() && taskCellViewModel.CanBatchSelected && batchSelectedIds.Contains(taskCellViewModel.TaskId))
					{
						taskCellViewModel.BatchSelected = true;
						taskCellViewModel.BatchDragging = TickDragHelper.Dragging && !taskCellViewModel.IsDragDisplayItem;
						taskCellViewModel.DragDiff = ((!taskCellViewModel.IsDragDisplayItem) ? ((TimeSpan?)null) : ((toPoint && !task.ToPoint) ? new TimeSpan?(TimeSpan.FromDays(_batchDragDays.GetValueOrDefault())) : _batchDragDiff));
					}
					return taskCellViewModel;
				}));
			}
			CalendarTimelineDayViewModel calendarTimelineDayViewModel = new CalendarTimelineDayViewModel
			{
				Date = currentDate,
				Cells = list3,
				InWeekControl = true
			};
			if (CalendarGeoHelper.TopFolded)
			{
				List<CalendarDisplayModel> topTasks = (from t in list2
					where t.DisplayStartDate.HasValue && (t.DisplayStartDate.Value.Hour < CalendarGeoHelper.GetStartHour() || t.DisplayStartDate.Value.Date < currentDate) && (!t.DisplayDueDate.HasValue || (t.DisplayDueDate.Value - currentDate.Date).TotalHours <= (double)CalendarGeoHelper.GetStartHour())
					orderby t.DisplayDueDate
					select t).ToList();
				List<string> extraTasks2 = GetExtraTasks(topTasks);
				calendarTimelineDayViewModel.Extra1 = extraTasks2[0];
				calendarTimelineDayViewModel.Extra2 = extraTasks2[1];
				calendarTimelineDayViewModel.TopTasks = topTasks;
				List<CalendarDisplayModel> list4 = (from t in list2
					where !topTasks.Contains(t) && t.DisplayStartDate.HasValue && t.DisplayStartDate.Value.Hour >= CalendarGeoHelper.GetEndHour() && t.DisplayStartDate.Value.Date >= currentDate
					orderby t.DisplayStartDate
					select t).ToList();
				List<string> extraTasks3 = GetExtraTasks(list4);
				calendarTimelineDayViewModel.Extra3 = extraTasks3[0];
				calendarTimelineDayViewModel.Extra4 = extraTasks3[1];
				calendarTimelineDayViewModel.BotTasks = list4;
			}
			_pointVms.Add(calendarTimelineDayViewModel);
		}
		SetCellEvents(checkDragOver);
	}

	private static List<string> GetExtraTasks(IReadOnlyList<CalendarDisplayModel> tasks)
	{
		if (tasks.Any())
		{
			int count = tasks.Count;
			if (count == 1)
			{
				return new List<string>
				{
					GetDisplayTitle(tasks[0].DisplayStartDate, tasks[0].Title),
					""
				};
			}
			if (count == 2)
			{
				return new List<string>
				{
					GetDisplayTitle(tasks[0].DisplayStartDate, tasks[0].Title),
					GetDisplayTitle(tasks[1].DisplayStartDate, tasks[1].Title)
				};
			}
			if (count > 2)
			{
				return new List<string>
				{
					GetDisplayTitle(tasks[0].DisplayStartDate, tasks[0].Title),
					string.Format(Utils.GetString("MoreTasks"), (count - 1).ToString())
				};
			}
		}
		return new List<string> { "", "" };
	}

	private static string GetDisplayTitle(DateTime? startDate, string title)
	{
		string text = string.Empty;
		if (startDate.HasValue)
		{
			text += DateUtils.FormatHourMinuteText(startDate.Value);
		}
		return text + " " + title;
	}

	private void SetCellEvents(bool checkDragging = true)
	{
		_timelineView.Setup(_pointVms, DayViewWidth, _showWeekend);
		if (checkDragging && TickDragHelper.Dragging)
		{
			_alldayView.CheckDraggingOver();
			_timelineView.CheckDraggingOver();
		}
	}

	private void SetWeekNum()
	{
		string weekNumStrOfYear = DateUtils.GetWeekNumStrOfYear(_model.StartDate);
		if (weekNumStrOfYear == _weeks)
		{
			_weekNum.Text = ((!LocalSettings.Settings.ShowWeek) ? string.Empty : _weeks);
			return;
		}
		if (_scrolling && _mode == "1")
		{
			DelayActionHandlerCenter.TryDoAction("AddWeekScrollAction", delegate
			{
				UserActCollectUtils.AddClickEvent("calendar", "week_action", "swipe_horizontal");
			}, 3000);
		}
		_weeks = weekNumStrOfYear;
		_weekNum.Text = ((!LocalSettings.Settings.ShowWeek) ? string.Empty : _weeks);
	}

	public (DateTime, DateTime) GetTimeSpan()
	{
		return (_model.StartDate, _model.EndDate.AddDays(1.0));
	}

	public (DateTime, DateTime) GetTaskTimeSpan()
	{
		return (_model.StartDate, _model.EndDate.AddDays(1.0));
	}

	public void RemoveItem(CalendarDisplayViewModel model)
	{
		if (model != null)
		{
			_tasks.RemoveAll((CalendarDisplayModel m) => m.Id == model.SourceViewModel.Id && Math.Abs(m.repeatDiff - model.RepeatDiff) < 0.1);
			LoadAllDayEvents();
			LoadPointEvents();
		}
	}

	public void LoadVirtualModels()
	{
		Load(Utils.GetWeekStart(new DateTime(2023, 9, 6)));
	}

	public int ExistTasks(HashSet<string> taskIds)
	{
		return (_tasks?.Where((CalendarDisplayModel model) => taskIds.Contains(model.Id)).ToList() ?? new List<CalendarDisplayModel>()).Count;
	}

	public void ShowAddWindow(DateTime dateTime)
	{
		_alldayView.TryShowAddWindow(dateTime);
	}

	private void LoadAllDayEvents()
	{
		if (_tasks == null)
		{
			return;
		}
		List<CalendarDisplayModel> tasks = _tasks.Where((CalendarDisplayModel t) => GridGeoAssembler.IsValidSpanModel(t, weekMode: true) || t.IsAllDay == true || (!t.IsAllDay.HasValue && t.StartDate.HasValue && t.StartDate.Value.TimeOfDay == TimeSpan.Zero && t.DueDate.HasValue && t.DueDate.Value.TimeOfDay == TimeSpan.Zero) || (t.CompletedTime.HasValue && !t.StartDate.HasValue)).ToList();
		_assembles = GridGeoAssembler.AssemblyMultiDayEvents(tasks, _model.StartDate, _showWeekend).ToList();
		_allDayVms = _assembles.Select((CalendarDisplayViewModel item) => new WeekEventModel
		{
			Row = item.Row,
			Column = item.Column,
			ColumnSpan = item.ColumnSpan,
			Data = item,
			TargetDate = _model.StartDate
		}).ToList();
		if (LocalSettings.Settings.WeekAllDayHeight == 0.0)
		{
			int num = 0;
			foreach (CalendarDisplayViewModel assemble in _assembles)
			{
				if (assemble.Row > num && num < 6)
				{
					num = assemble.Row;
				}
				LocalSettings.Settings.WeekAllDayHeight = 63 + (num + 1) * 20;
				GetParent()?.NotifyAllDayHeightChanged();
			}
		}
		_alldayView.ScrollStartDate = _model.StartDate;
		SetAllDayEvents();
	}

	private void SetAllDayEvents()
	{
		_alldayView.SetEvents(_allDayVms);
	}

	public void Resize()
	{
		LoadAllDayEvents();
		_timelineView.Changed();
	}

	private void OnSplitDragCompleted(object sender, DragCompletedEventArgs e)
	{
		LocalSettings.Settings.WeekAllDayHeight = _firstRow.ActualHeight;
		GetParent()?.NotifyAllDayHeightChanged();
	}

	public void PrintWeekCalendar(bool isAll)
	{
		new PrintPreviewWindow(GetPrintAllDayModel(isAll), _pointVms.Where((CalendarTimelineDayViewModel d) => d.Date >= _model.StartDate && d.Date <= _model.EndDate).Select(CalendarTimelineDayViewModel.Copy).ToList(), _model.StartDate, DisplayDays + ((!_showWeekend) ? 2 : 0)).Show();
	}

	private List<WeekEventModel> GetPrintAllDayModel(bool isAll)
	{
		List<WeekEventModel> list = (from item in GridGeoAssembler.AssemblyMultiDayEvents(_tasks.Where((CalendarDisplayModel t) => GridGeoAssembler.IsValidSpanModel(t, weekMode: true) || (t.IsAllDay.HasValue && t.IsAllDay.Value) || (t.CompletedTime.HasValue && !t.StartDate.HasValue)).ToList(), _model.StartDate, showWeekend: true).ToList()
			where item.Row <= 8 || isAll
			select new WeekEventModel
			{
				Row = item.Row,
				Column = item.Column,
				ColumnSpan = item.ColumnSpan,
				Data = item
			}).ToList();
		if (!isAll)
		{
			int j;
			for (j = 0; j < 7; j++)
			{
				CalendarDisplayViewModel calendarDisplayViewModel = (from task in _assembles
					where task.Column == j
					orderby task.Row descending
					select task).FirstOrDefault();
				if (calendarDisplayViewModel != null)
				{
					int row = calendarDisplayViewModel.Row;
					if (row > 9)
					{
						list.Add(new WeekEventModel
						{
							Column = j,
							Row = 10,
							ColumnSpan = 1,
							Data = new CalendarDisplayViewModel
							{
								Column = j,
								Row = 9,
								ColumnSpan = 1,
								IsLoadMore = true,
								SourceViewModel = new TaskBaseViewModel
								{
									Type = DisplayType.LoadMore,
									Title = string.Format(Utils.GetString("MoreItems"), row - 9)
								}
							}
						});
					}
				}
			}
		}
		return list;
	}

	public async Task ReloadPointEvent()
	{
		LoadPointEvents();
		SetCellEvents();
	}

	public void ClearItems(bool onlyData = false)
	{
		_allDayVms?.Clear();
		_assembles?.Clear();
		_pointVms?.Clear();
		_tasks?.Clear();
	}

	public void OnShowWeekChanged(bool? showWeekend = null)
	{
		if (showWeekend.HasValue)
		{
			SetDisplayDays(showWeekend.Value ? 7 : 5, showWeekend.Value);
		}
		LoadAllDayEvents();
		LoadPointEvents();
		GoTo(DateTime.Today);
	}

	public void InitTimeLineBoard()
	{
		_timelineView?.InitBoard();
	}

	public void TryShowTooltip()
	{
	}

	public void SetFirstRowHeight()
	{
		_firstRow.Height = new GridLength(LocalSettings.Settings.WeekAllDayHeight);
	}

	public void SetEditing(bool editing)
	{
		_calendarControl?.SetEditting(editing);
	}

	private async void OnStopInputScroll()
	{
		if (!_scrolling)
		{
			return;
		}
		if (_vLines.Count > 1)
		{
			double diff = _vLines[1].Offset;
			if (Math.Abs(diff) < 0.5)
			{
				_scrolling = false;
				await Task.Delay(10);
				DateTime dateTime = _alldayView.GetStartDate() ?? _model.StartDate;
				if (dateTime != _model.StartDate)
				{
					SetStartDate(dateTime);
				}
				Reload();
				return;
			}
			if (Math.Abs(diff) > 20.0)
			{
				diff = ((!(diff < 0.0)) ? (_leftScroll ? diff : (diff - DayViewWidth)) : (_leftScroll ? (diff + DayViewWidth) : diff));
			}
			diff /= -10.0;
			int times = 10;
			while (times > 0)
			{
				TouchScroll(diff);
				_timelineView.OnTouchScroll(diff, _showWeekend);
				_alldayView.OnTouchScroll(diff, _showWeekend);
				times--;
				await Task.Delay(10);
			}
			DateTime dateTime2 = _alldayView.GetStartDate() ?? _model.StartDate;
			if (dateTime2 != _model.StartDate)
			{
				SetStartDate(dateTime2);
			}
		}
		_scrolling = false;
		await Task.Delay(10);
		Reload();
	}

	public void OnTouchScroll(double offset, bool force = false)
	{
		if ((base.IsMouseOver || force) && !_loading)
		{
			if (!_scrolling)
			{
				_scrolling = true;
			}
			RemoveFlashBorder();
			OnDoubleFingerTouch();
			_leftScroll = offset < 0.0;
			TouchScroll(offset);
			_timelineView.OnTouchScroll(offset, _showWeekend);
			_alldayView.OnTouchScroll(offset, _showWeekend);
			DateTime dateTime = _alldayView.GetStartDate() ?? _model.StartDate;
			if (dateTime != _model.StartDate)
			{
				SetStartDate(dateTime);
			}
		}
	}

	private void TouchScroll(double offset)
	{
		TaskDetailPopup.TryCloseWindow();
		bool flag = false;
		for (int i = 0; i < _vLines.Count; i++)
		{
			MultiDayLine multiDayLine = _vLines[i];
			if (!(multiDayLine.RenderTransform is TranslateTransform translateTransform))
			{
				continue;
			}
			double x = translateTransform.X + offset;
			translateTransform.X = x;
			if (translateTransform.X >= ((double)DisplayDays + 0.5) * DayViewWidth)
			{
				flag = true;
				translateTransform.X -= (double)(DisplayDays + 2) * DayViewWidth;
				DateTime date = multiDayLine.Date.AddDays(-2 - DisplayDays);
				if (!_showWeekend)
				{
					date = date.AddDays((multiDayLine.Date.DayOfWeek == DayOfWeek.Monday || multiDayLine.Date.DayOfWeek == DayOfWeek.Tuesday) ? (-4) : (-2));
				}
				multiDayLine.Date = date;
			}
			if (translateTransform.X <= -1.5 * DayViewWidth)
			{
				flag = true;
				translateTransform.X += (double)(DisplayDays + 2) * DayViewWidth;
				DateTime date2 = multiDayLine.Date.AddDays(2 + DisplayDays);
				if (!_showWeekend)
				{
					date2 = date2.AddDays((multiDayLine.Date.DayOfWeek == DayOfWeek.Friday || multiDayLine.Date.DayOfWeek == DayOfWeek.Thursday) ? 4 : 2);
				}
				multiDayLine.Date = date2;
			}
			multiDayLine.Offset = translateTransform.X;
		}
		if (flag)
		{
			_vLines.Sort((MultiDayLine a, MultiDayLine b) => a.Offset.CompareTo(b.Offset));
		}
	}

	private void SetStartDate(DateTime start)
	{
		_model.StartDate = start;
		_model.EndDate = _model.StartDate.AddDays(DisplayDays - 1 + ((!_showWeekend) ? 2 : 0));
		this.DateRangeChanged?.Invoke(null, (_model.StartDate, _model.EndDate));
		SetWeekNum();
	}

	public DateTime GetStartDate()
	{
		return _model.StartDate;
	}

	public void OnDoubleFingerTouch()
	{
		_timelineView.OnDoubleFingerTouch();
		_alldayView.OnDoubleFingerTouch();
		if (_scrolling)
		{
			DelayActionHandlerCenter.TryDoAction("CalendarInputScroll", TryStopInputScroll, 100);
		}
	}

	private void TryStopInputScroll(object sender, EventArgs e)
	{
		base.Dispatcher.Invoke(OnStopInputScroll);
	}

	public async Task<List<CalendarTimelineDayViewModel>> LoadModelsWhenScrolling(DateTime newDate, bool next)
	{
		if (_scrollLoadedDate.Contains(newDate))
		{
			return null;
		}
		for (int i = ((!next) ? (-3) : 0); i <= (next ? 3 : 0); i++)
		{
			_scrollLoadedDate.Add(newDate);
		}
		CalendarControl calendarControl = Utils.FindParent<CalendarControl>(this);
		ProjectExtra projectFilter = ((calendarControl != null && calendarControl.ViewSourceType == CalendarViewSourceType.WithList) ? LocalSettings.Settings.ExtraSettings.CalendarParallelSelectProject : ProjectExtra.Deserialize(LocalSettings.Settings.CalendarFilterData));
		List<CalendarDisplayModel> list = await CalendarDisplayService.GetDisplayModels(next ? newDate : newDate.AddDays(-3.0), next ? newDate.AddDays(4.0) : newDate.AddDays(1.0), withCourses: true, withFocus: false, projectFilter);
		if (list != null)
		{
			for (int j = ((!next) ? (-3) : 0); j <= (next ? 3 : 0); j++)
			{
				DateTime date = newDate.AddDays(j);
				if (_pointVms.Any((CalendarTimelineDayViewModel m) => m.Date == date))
				{
					continue;
				}
				List<CalendarDisplayModel> list2 = list.Where((CalendarDisplayModel calendarDisplayModel) => DateUtils.IsTaskInDay(date, calendarDisplayModel.DisplayStartDate, calendarDisplayModel.DisplayDueDate, calendarDisplayModel.IsAllDay)).ToList();
				List<TaskCellViewModel> list3 = new List<TaskCellViewModel>();
				if (list2.Count > 0)
				{
					list3.AddRange(list2.Select((CalendarDisplayModel model) => TaskCellViewModel.Build(model, date)));
				}
				CalendarTimelineDayViewModel calendarTimelineDayViewModel = new CalendarTimelineDayViewModel
				{
					Date = date,
					Cells = list3,
					InWeekControl = true
				};
				if (CalendarGeoHelper.TopFolded)
				{
					List<CalendarDisplayModel> topTasks = (from t in list2
						where t.DisplayStartDate.HasValue && (t.DisplayStartDate.Value.Hour < CalendarGeoHelper.GetStartHour() || t.DisplayStartDate.Value.Date < date) && (!t.DisplayDueDate.HasValue || (t.DisplayDueDate.Value - date.Date).TotalHours <= (double)CalendarGeoHelper.GetStartHour())
						orderby t.DisplayDueDate
						select t).ToList();
					List<string> extraTasks = GetExtraTasks(topTasks);
					calendarTimelineDayViewModel.Extra1 = extraTasks[0];
					calendarTimelineDayViewModel.Extra2 = extraTasks[1];
					calendarTimelineDayViewModel.TopTasks = topTasks;
					List<CalendarDisplayModel> list4 = (from t in list2
						where !topTasks.Contains(t) && t.DisplayStartDate.HasValue && t.DisplayStartDate.Value.Hour >= CalendarGeoHelper.GetEndHour() && t.DisplayStartDate.Value.Date >= date
						orderby t.DisplayStartDate
						select t).ToList();
					List<string> extraTasks2 = GetExtraTasks(list4);
					calendarTimelineDayViewModel.Extra3 = extraTasks2[0];
					calendarTimelineDayViewModel.Extra4 = extraTasks2[1];
					calendarTimelineDayViewModel.BotTasks = list4;
				}
				_pointVms.Add(calendarTimelineDayViewModel);
			}
			foreach (CalendarDisplayModel task in list)
			{
				if (!_tasks.Any(delegate(CalendarDisplayModel t)
				{
					if (t.Id == task.Id)
					{
						DateTime? displayStartDate = t.DisplayStartDate;
						DateTime? displayStartDate2 = task.DisplayStartDate;
						if (displayStartDate.HasValue != displayStartDate2.HasValue)
						{
							return false;
						}
						if (!displayStartDate.HasValue)
						{
							return true;
						}
						return displayStartDate.GetValueOrDefault() == displayStartDate2.GetValueOrDefault();
					}
					return false;
				}))
				{
					_tasks.Add(task);
				}
			}
			SetScrollingAllDayEvents();
			return _pointVms;
		}
		_pointVms.Add(new CalendarTimelineDayViewModel
		{
			Date = newDate,
			InWeekControl = true
		});
		return _pointVms;
	}

	private async Task SetScrollingAllDayEvents()
	{
		List<CalendarDisplayModel> tasks = _tasks.Where((CalendarDisplayModel t) => GridGeoAssembler.IsValidSpanModel(t, weekMode: true) || (t.IsAllDay.HasValue && t.IsAllDay.Value) || (t.CompletedTime.HasValue && !t.StartDate.HasValue)).ToList();
		_assembles = GridGeoAssembler.AssemblyMultiDayEvents(tasks, _alldayView.ScrollStartDate, _showWeekend).ToList();
		_allDayVms = _assembles.Select((CalendarDisplayViewModel item) => new WeekEventModel
		{
			Row = item.Row,
			Column = item.Column,
			ColumnSpan = item.ColumnSpan,
			Data = item,
			TargetDate = _alldayView.ScrollStartDate
		}).ToList();
		if (LocalSettings.Settings.WeekAllDayHeight == 0.0)
		{
			int num = 0;
			foreach (CalendarDisplayViewModel assemble in _assembles)
			{
				if (assemble.Row > num && num < 6)
				{
					num = assemble.Row;
				}
				LocalSettings.Settings.WeekAllDayHeight = 63 + (num + 1) * 20;
				GetParent()?.NotifyAllDayHeightChanged();
			}
		}
		if (!_barSideDragging)
		{
			_alldayView.SetEvents(_allDayVms, resetOffset: false);
		}
	}

	public async Task FlashNaviDate(DateTime date, bool delay = false)
	{
		if (DisplayDays < 7 && _showWeekend)
		{
			return;
		}
		if (!base.IsLoaded)
		{
			await Task.Delay(240);
		}
		double num = ((!_showWeekend && DisplayDays == 5) ? (((date - _model.StartDate).TotalDays - (double)(CalendarGeoHelper.IsColumnHide(0) ? 1 : 0) - (double)(CalendarGeoHelper.IsColumnHide(1) ? 1 : 0)) * DayViewWidth) : ((date - _model.StartDate).TotalDays * DayViewWidth));
		if (!(num < 0.0))
		{
			Border border = new Border
			{
				Width = DayViewWidth,
				Margin = new Thickness((double)CalendarGeoHelper.GetTimeZoneCount() * 56.0 + num, 32.0, 0.0, 0.0),
				VerticalAlignment = VerticalAlignment.Stretch,
				HorizontalAlignment = HorizontalAlignment.Left,
				Opacity = 0.0,
				IsHitTestVisible = false
			};
			border.SetResourceReference(Border.BackgroundProperty, "BaseColorOpacity5");
			border.SetValue(Grid.RowSpanProperty, 2);
			_flashBorder = border;
			base.Children.Add(_flashBorder);
			for (int i = 0; i < 10; i++)
			{
				await Task.Delay(delay ? 20 : 5);
				border.Opacity = (double)i / 10.0;
			}
			for (int i = 0; i < 25; i++)
			{
				await Task.Delay(20);
				border.Opacity = 1.0 - (double)i / 25.0;
			}
			base.Children.Remove(border);
			if (_flashBorder == border)
			{
				RemoveFlashBorder();
			}
		}
	}

	private void RemoveFlashBorder()
	{
		base.Children.Remove(_flashBorder);
		_flashBorder = null;
	}

	public void SetMode(string mode)
	{
		_mode = mode;
	}

	public bool ShowTimePoint()
	{
		if (DateTime.Today >= _model.StartDate)
		{
			return DateTime.Today <= _model.EndDate;
		}
		return false;
	}

	public void OnShiftMouseWheel(int eDelta)
	{
		SmoothMouthScroll(eDelta);
	}

	public async void AddShowCheckIconUserEvent()
	{
		await Task.Delay(200);
		if (UserDao.IsPro() && LocalSettings.Settings.IsShowCalendarIcon(DisplayType.Task) && DayViewWidth >= 80.0)
		{
			UserActCollectUtils.AddClickEvent("calendar", "element_show", "task_checkbox");
		}
	}

	public void OnFontSizeChanged()
	{
		_alldayView?.OnFontSizeChanged();
	}

	public void ScrollOnHourHeightChanged(double current, double next)
	{
		_timelineView.ScrollOnHeightChanged(current, next);
	}

	public void SetShowIcon()
	{
		_alldayView.SetShowIcon();
		_timelineView.SetShowIcon();
	}

	public void SetColor()
	{
		_alldayView.SetColor();
		_timelineView.SetColor();
	}

	public bool ExitBatchSelect()
	{
		return _calendarControl?.ExitBatchSelect() ?? false;
	}

	public void ClearBatchSelected()
	{
		_alldayView.ClearBatchSelected();
		_timelineView.ClearBatchSelected();
	}

	public void AddBatchSelected(Dictionary<string, int> selected)
	{
		_calendarControl?.AddBatchSelected(selected);
	}

	public BlockingSet<string> GetSelectedIds()
	{
		return _calendarControl?.BatchSelected;
	}

	public Dictionary<string, int> GetSelectedIdsRow()
	{
		return _calendarControl?.BatchSelectedRow;
	}

	public void SetBatchDragging(bool dragging)
	{
		_alldayView?.SetBatchDragging(dragging);
		_timelineView?.SetBatchDragging(dragging);
	}

	public void SetBatchDragStarting()
	{
		_batchDragStartDate = _model.StartDate;
	}

	public void OnDragDiffChanged(TimeSpan? diff, int? days = null, bool toAllDay = false, bool toPoint = false)
	{
		List<string> list = GetSelectedIds()?.ToList();
		TimeSpan? timeSpan = diff;
		TimeSpan? batchDragDiff = _batchDragDiff;
		bool flag = timeSpan.HasValue != batchDragDiff.HasValue || (timeSpan.HasValue && timeSpan.GetValueOrDefault() != batchDragDiff.GetValueOrDefault()) || toAllDay != _toAllDay || _toPoint != toPoint || _batchDragDays != days;
		_batchDragDays = days;
		_toAllDay = toAllDay;
		_toPoint = toPoint;
		_batchDragDiff = diff;
		if (list != null && flag)
		{
			List<CalendarDisplayModel> list2 = new List<CalendarDisplayModel>();
			HashSet<string> ignoreIds = new HashSet<string>();
			foreach (string item in list)
			{
				TaskBaseViewModel taskById = TaskCache.GetTaskById(item);
				if (taskById == null)
				{
					continue;
				}
				DateTime? dateTime = taskById.StartDate ?? taskById.CompletedTime?.Date;
				if (!dateTime.HasValue || !diff.HasValue)
				{
					continue;
				}
				bool flag2 = false;
				if (taskById.IsAllDay ?? true)
				{
					goto IL_01de;
				}
				if (taskById.DueDate.HasValue)
				{
					batchDragDiff = taskById.DueDate - dateTime;
					if (batchDragDiff.HasValue && batchDragDiff.GetValueOrDefault().TotalHours > 24.0)
					{
						goto IL_01de;
					}
				}
				goto IL_01e8;
				IL_01de:
				if (!toPoint)
				{
					continue;
				}
				flag2 = true;
				goto IL_01e8;
				IL_01e8:
				if (!toAllDay)
				{
					CalendarDisplayModel calendarDisplayModel = new CalendarDisplayModel(taskById);
					calendarDisplayModel.ToPoint = flag2;
					DateTime value = ((!toPoint || !flag2) ? dateTime.Value.Add(diff.Value) : dateTime.Value.AddDays(days.GetValueOrDefault()));
					DateTime? end = ((toPoint && flag2) ? calendarDisplayModel.DueDate?.AddDays(days.GetValueOrDefault()) : calendarDisplayModel.DueDate?.Add(diff.Value));
					if (DateUtils.DatesBetween(GetStartDate(), _model.EndDate.AddDays(1.0), value, end))
					{
						calendarDisplayModel.IsDragDisplayItem = true;
						list2.Add(calendarDisplayModel);
					}
				}
			}
			LoadPointEvents(list2, ignoreIds, checkDragOver: false, toPoint);
		}
		if (!days.HasValue || toPoint)
		{
			_alldayView?.SetDragTasks(null);
		}
		else
		{
			if (!flag || list == null)
			{
				return;
			}
			if (!_batchDragStartDate.HasValue)
			{
				_batchDragStartDate = _model.StartDate;
			}
			List<WeekEventModel> list3 = new List<WeekEventModel>();
			foreach (string item2 in list)
			{
				TaskBaseViewModel taskById2 = TaskCache.GetTaskById(item2);
				DateTime? dateTime2 = taskById2.StartDate ?? taskById2.CompletedTime?.Date;
				if (!dateTime2.HasValue)
				{
					continue;
				}
				int? num = null;
				if (!(taskById2.IsAllDay ?? true))
				{
					if (taskById2.DueDate.HasValue)
					{
						batchDragDiff = taskById2.DueDate - dateTime2;
						if (!batchDragDiff.HasValue || !(batchDragDiff.GetValueOrDefault().TotalHours <= 24.0))
						{
							goto IL_0498;
						}
					}
					if (!toAllDay)
					{
						continue;
					}
					num = 1;
				}
				goto IL_0498;
				IL_0498:
				CalendarDisplayViewModel calendarDisplayViewModel = CalendarDisplayViewModel.Build(taskById2);
				Dictionary<string, int> batchSelectedRow = _calendarControl.BatchSelectedRow;
				if (!batchSelectedRow.TryGetValue(taskById2.Id, out var value2))
				{
					batchSelectedRow[taskById2.Id] = -1;
					value2 = -1;
				}
				calendarDisplayViewModel.Row = value2;
				calendarDisplayViewModel.BatchSelected = true;
				calendarDisplayViewModel.ShowTimeText = false;
				calendarDisplayViewModel.ColumnSpan = num ?? GridGeoAssembler.GetDateSpan(dateTime2.Value, taskById2.DueDate, !_showWeekend);
				calendarDisplayViewModel.Column = GridGeoAssembler.GetDateSpan(_model.StartDate.Date, dateTime2.Value.Date, !_showWeekend);
				list3.Add(new WeekEventModel
				{
					DragDiffDays = days.Value,
					Row = calendarDisplayViewModel.Row,
					ColumnSpan = calendarDisplayViewModel.ColumnSpan,
					Column = calendarDisplayViewModel.Column,
					TargetDate = dateTime2.Value,
					Data = calendarDisplayViewModel
				});
			}
			List<WeekEventModel> list4 = list3.Where((WeekEventModel t) => t.Row == -1).ToList();
			if (list4.Any())
			{
				list4.Sort((WeekEventModel a, WeekEventModel b) => (a.Data.DisplayStartDate.HasValue && b.Data.DisplayStartDate.HasValue) ? a.Data.DisplayStartDate.Value.CompareTo(b.Data.DisplayStartDate.Value) : 0);
			}
			foreach (WeekEventModel task in list4)
			{
				List<int> list5 = (from t in list3
					where t.Column <= task.Column && t.Column + t.ColumnSpan - 1 >= task.Column
					select t.Row).ToList();
				task.Row = ((list5.Count > 0) ? (list5.Max() + 1) : (-1));
			}
			_alldayView?.SetDragTasks(list3);
		}
	}

	public void SetItemBatchSelected(BlockingSet<string> batchSelected)
	{
		_alldayView.SetBatchSelected(batchSelected);
		_timelineView.SetBatchSelected(batchSelected);
	}
}
