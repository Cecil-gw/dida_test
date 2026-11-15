using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Threading;
using TickTickUtils;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Util.DragAndDrop;
using ticktick_WPF.Util.TouchPad;
using ticktick_WPF.Views.Detail;

namespace ticktick_WPF.Views.Calendar.Week;

public class CalendarTimelineView : ScrollViewer
{
	private const int EdgeOffset = 24;

	private const int EdgeScrollStep = 36;

	private List<CalendarTimelineDayView> _dayViews = new List<CalendarTimelineDayView>();

	private readonly Timer _startScrollUpTimer = new Timer(650.0);

	private readonly Storyboard _scrollStoryboard = new Storyboard();

	private MultiDayView _parent;

	private readonly TouchpadScrollHelper _touchpadScroll;

	private readonly Border _topFoldBorder = new Border
	{
		Height = 32.0,
		Background = Brushes.Transparent,
		Cursor = Cursors.Hand,
		VerticalAlignment = VerticalAlignment.Top
	};

	private readonly Border _botFoldBorder = new Border
	{
		Height = 32.0,
		Background = Brushes.Transparent,
		Cursor = Cursors.Hand,
		VerticalAlignment = VerticalAlignment.Bottom
	};

	private readonly Grid _foldTooltipGrid = new Grid
	{
		VerticalAlignment = VerticalAlignment.Top,
		HorizontalAlignment = HorizontalAlignment.Left
	};

	private readonly Grid _container = new Grid();

	private readonly Grid _daysCanvas = new Grid
	{
		Margin = new Thickness(1.0, 0.0, 1.0, 0.0),
		ClipToBounds = true
	};

	private List<Line> _hLines = new List<Line>();

	private TimeLine _timeLine;

	private double _currentOffset;

	private bool _scrollingUp;

	private bool _scrollingDown;

	private bool _scrolling;

	private List<CalendarTimelineDayViewModel> _data;

	private bool _foldBorderMouseDown;

	private Grid _timePointer;

	private TextBlock _currentTime;

	private StackPanel _currentTimeStack;

	private Point _batchStartPoint;

	private Point _prePoint;

	private bool _batchMouseDown;

	private Border _batchBorder;

	private double _checkedWidth;

	private double _checkedHeight;

	private readonly DispatcherTimer _moveViewTimer = new DispatcherTimer(DispatcherPriority.Render);

	public CalendarTimelineView(MultiDayView parent)
	{
		_parent = parent;
		_touchpadScroll = new TouchpadScrollHelper(this);
		SetResourceReference(FrameworkElement.StyleProperty, "for_scrollviewer");
		base.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
		base.Content = _container;
		_container.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		_container.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(1.0, GridUnitType.Star)
		});
		_timeLine = new TimeLine
		{
			HorizontalAlignment = HorizontalAlignment.Left
		};
		_timeLine.SetValue(Grid.ColumnProperty, 0);
		_container.Children.Add(_timeLine);
		base.PreviewMouseWheel += OnPreviewMouseWheel;
		InitDaysCanvas();
		SetLines();
		base.Background = Brushes.Transparent;
		InitBatchEvent();
		CalendarGeoHelper.TopFoldedChanged += OnTopFoldedChanged;
		base.SizeChanged += OnSizeChanged;
		_timePointer = new Grid
		{
			Height = 19.0,
			IsHitTestVisible = false,
			VerticalAlignment = VerticalAlignment.Top
		};
		_timePointer.SetValue(Grid.ColumnSpanProperty, 2);
		SetPointerPosition();
		base.Loaded += delegate
		{
			BindEvents();
		};
		base.Unloaded += delegate
		{
			UnbindEvents();
		};
		InitFoldBorder();
	}

	private void BindEvents()
	{
		CalendarGeoHelper.CalendarHourHeightChanged += OnCalendarHeightChanged;
		CalendarGeoHelper.TopFoldedChanged += OnTopFoldedChanged;
		CalendarGeoHelper.ExpandTop += OnExpandTop;
		DataChangedNotifier.PeriodicCheck += NotifyPointer;
		DataChangedNotifier.ThemeModeChanged += DataChangedNotifier_ThemeModeChanged;
		if (CalendarGeoHelper.TopFolded)
		{
			double hourHeight = CalendarGeoHelper.HourHeight;
			int num = 24 - CalendarGeoHelper.GetCollapsedHours();
			double num2 = hourHeight * (double)num + (double)(CalendarGeoHelper.TopFolded ? 32 : 0);
			_botFoldBorder.Height = Math.Max(32.0, base.ActualHeight - num2);
		}
		FocusManager.SetFocusedElement(this, this);
		Keyboard.Focus(this);
	}

	private void UnbindEvents()
	{
		CalendarGeoHelper.CalendarHourHeightChanged -= OnCalendarHeightChanged;
		CalendarGeoHelper.TopFoldedChanged -= OnTopFoldedChanged;
		CalendarGeoHelper.ExpandTop -= OnExpandTop;
		DataChangedNotifier.PeriodicCheck -= NotifyPointer;
		DataChangedNotifier.ThemeModeChanged -= DataChangedNotifier_ThemeModeChanged;
	}

	private void DataChangedNotifier_ThemeModeChanged(object sender, EventArgs e)
	{
		bool flag = ThemeUtil.IsDark();
		foreach (object child in _container.Children)
		{
			if (child is Line line)
			{
				line.SetResourceReference(Shape.StrokeProperty, flag ? "BaseColorOpacity2" : "BaseColorOpacity4");
			}
		}
	}

	private void NotifyPointer(object sender, EventArgs e)
	{
		SetPointer();
	}

	public void SetPointer()
	{
		Utils.RunOnUiThread(base.Dispatcher, SetPointerPosition);
	}

	private void OnExpandTop(object sender, bool e)
	{
		if (e)
		{
			ScrollToHome();
		}
		else
		{
			ScrollToEnd();
		}
	}

	public bool ShowPointer()
	{
		if (_parent.GetTimeSpan().Item1 <= DateTime.Today)
		{
			return _parent.GetTimeSpan().Item2 > DateTime.Today;
		}
		return false;
	}

	private void OnTopFoldedChanged(object sender, EventArgs e)
	{
		_topFoldBorder.Visibility = ((!CalendarGeoHelper.TopFolded) ? Visibility.Collapsed : Visibility.Visible);
		_botFoldBorder.Visibility = ((!CalendarGeoHelper.TopFolded) ? Visibility.Collapsed : Visibility.Visible);
		Changed();
		SetPointer();
	}

	public void Changed()
	{
		InitBoard();
		SetLines();
		if (base.IsVisible)
		{
			_parent?.ReloadPointEvent();
		}
	}

	private void OnCalendarHeightChanged(object sender, double e)
	{
		int num = (CalendarGeoHelper.TopFolded ? 32 : 0);
		for (int i = 0; i < _hLines.Count; i++)
		{
			_hLines[i].Margin = new Thickness(0.0, (double)num + CalendarGeoHelper.HourHeight * (double)(i + ((!CalendarGeoHelper.TopFolded) ? 1 : 0)), 0.0, 0.0);
		}
		SetPointer();
	}

	private void InitDaysCanvas()
	{
		_daysCanvas.SetValue(Panel.ZIndexProperty, 2);
		_daysCanvas.SetValue(Grid.ColumnProperty, 1);
		_container.Children.Add(_daysCanvas);
	}

	private void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
	{
		if (Utils.IfCtrlPressed())
		{
			ZoomHelper.Zoom(e.Delta, base.ActualHeight, this);
			e.Handled = true;
		}
		else if (!PopupStateManager.CanShowAddPopup())
		{
			e.Handled = true;
		}
		else if (Utils.IfShiftPressed())
		{
			_parent?.OnShiftMouseWheel(e.Delta);
			e.Handled = true;
		}
		else
		{
			e.Handled = _touchpadScroll.TryScroll(e.Delta);
		}
	}

	public void OnDoubleFingerTouch()
	{
		_touchpadScroll.OnDoubleFingerTouch();
	}

	public void SetDisplayDays()
	{
		SetDayViews();
	}

	private void SetDayViews()
	{
		int num = (_parent?.DisplayDays ?? 7) + 2;
		double num2 = _parent?.DayViewWidth ?? 0.0;
		if (num > _dayViews.Count)
		{
			int count = _dayViews.Count;
			for (int i = 0; i < num - count; i++)
			{
				CalendarTimelineDayView calendarTimelineDayView = new CalendarTimelineDayView(this)
				{
					RenderTransform = new TranslateTransform(),
					Width = num2,
					HorizontalAlignment = HorizontalAlignment.Left
				};
				_dayViews.Add(calendarTimelineDayView);
				calendarTimelineDayView.SetValue(Panel.ZIndexProperty, 5);
				_daysCanvas.Children.Add(calendarTimelineDayView);
			}
		}
		else
		{
			List<CalendarTimelineDayView> list = new List<CalendarTimelineDayView>();
			for (int j = num; j < _dayViews.Count; j++)
			{
				list.Add(_dayViews[j]);
			}
			list.ForEach(delegate(CalendarTimelineDayView d)
			{
				_dayViews.Remove(d);
				_daysCanvas.Children.Remove(d);
			});
		}
		for (int num3 = 0; num3 < _dayViews.Count; num3++)
		{
			CalendarTimelineDayView calendarTimelineDayView2 = _dayViews[num3];
			calendarTimelineDayView2.RenderTransform = new TranslateTransform
			{
				X = (double)(num3 - 1) * num2
			};
			calendarTimelineDayView2.Width = num2;
		}
	}

	private void SetLines()
	{
		int timeBlockCount = CalendarGeoHelper.GetTimeBlockCount();
		int num = (CalendarGeoHelper.TopFolded ? 32 : 0);
		bool flag = ThemeUtil.IsDark();
		if (timeBlockCount > _hLines.Count)
		{
			int count = _hLines.Count;
			for (int i = 0; i < timeBlockCount - count; i++)
			{
				Line line = new Line
				{
					X1 = 0.0,
					X2 = 1.0,
					Stretch = Stretch.Fill,
					VerticalAlignment = VerticalAlignment.Top,
					StrokeThickness = 1.0,
					IsHitTestVisible = false
				};
				line.SetValue(Grid.ColumnProperty, 1);
				line.SetValue(Panel.ZIndexProperty, 1);
				line.SetResourceReference(Shape.StrokeProperty, flag ? "BaseColorOpacity2" : "BaseColorOpacity4");
				_hLines.Add(line);
				_container.Children.Add(line);
			}
		}
		else
		{
			List<Line> list = new List<Line>();
			for (int j = timeBlockCount; j < _hLines.Count; j++)
			{
				list.Add(_hLines[j]);
			}
			list.ForEach(delegate(Line l)
			{
				_hLines.Remove(l);
				_container.Children.Remove(l);
			});
		}
		for (int num2 = 0; num2 < _hLines.Count; num2++)
		{
			_hLines[num2].Margin = new Thickness(0.0, (double)num + CalendarGeoHelper.HourHeight * (double)(num2 + ((!CalendarGeoHelper.TopFolded) ? 1 : 0)), 0.0, 0.0);
		}
	}

	public void Setup(List<CalendarTimelineDayViewModel> datas, double width, bool showWeekend)
	{
		DateTime item = _parent.GetTimeSpan().Item1;
		_data = datas;
		double num = _parent?.DayViewWidth ?? 0.0;
		for (int i = 0; i < _dayViews.Count; i++)
		{
			if (_dayViews[i].RenderTransform is TranslateTransform translateTransform)
			{
				translateTransform.X = (double)(i - 1) * num;
			}
		}
		DateTime date = item.AddDays(-1.0);
		if (!showWeekend)
		{
			while (TickTickUtils.DateUtils.IsWeekends(date))
			{
				date = date.AddDays(-1.0);
			}
		}
		for (int j = 0; j < _dayViews.Count && j < datas.Count; j++)
		{
			if (j > 0)
			{
				date = date.AddDays(1.0);
			}
			CalendarTimelineDayView calendarTimelineDayView = _dayViews[j];
			if (!showWeekend)
			{
				while (TickTickUtils.DateUtils.IsWeekends(date))
				{
					date = date.AddDays(1.0);
				}
			}
			calendarTimelineDayView.RenderTransform = new TranslateTransform
			{
				X = (double)(j - 1) * num
			};
			CalendarTimelineDayViewModel calendarTimelineDayViewModel = datas.FirstOrDefault((CalendarTimelineDayViewModel d) => d.Date == date);
			if (calendarTimelineDayViewModel != null)
			{
				calendarTimelineDayView.DataContext = calendarTimelineDayViewModel;
				calendarTimelineDayView.Reload(width);
			}
			else
			{
				calendarTimelineDayView.DataContext = new CalendarTimelineDayViewModel
				{
					Date = date
				};
			}
		}
	}

	public void ResetTimeLine()
	{
		InitBoard();
	}

	public void NotifyEventTitleChanged(string eventId, string text)
	{
		foreach (CalendarTimelineDayView dayView in _dayViews)
		{
			if (dayView != null && dayView.DataContext is CalendarTimelineDayViewModel calendarTimelineDayViewModel)
			{
				calendarTimelineDayViewModel.Cells.Where((TaskCellViewModel i) => i.EventId == eventId).ToList().ForEach(delegate(TaskCellViewModel item)
				{
					item.SourceViewModel.Title = text;
				});
			}
		}
	}

	public void TryScrollOnEdge(MouseEventArgs e)
	{
		double y = e.GetPosition(this).Y;
		if (y > _currentOffset)
		{
			if (_scrollingUp)
			{
				StopScrollUp();
			}
		}
		else if (y < _currentOffset && _scrollingDown)
		{
			StopScrollDown();
		}
		if (_currentOffset >= 0.0 && _currentOffset <= 24.0)
		{
			if (y < _currentOffset)
			{
				TryStartScrollUp();
			}
		}
		else
		{
			StopScrollUp();
		}
		if (_currentOffset >= base.ActualHeight - 24.0 && _currentOffset <= base.ActualHeight)
		{
			if (y > _currentOffset)
			{
				StartScrollDown();
			}
		}
		else
		{
			StopScrollDown();
		}
		_currentOffset = y;
	}

	private void TryStartScrollUp()
	{
		_scrollingUp = true;
		_startScrollUpTimer.Start();
		_startScrollUpTimer.Elapsed -= OnStartScrollElapsed;
		_startScrollUpTimer.Elapsed += OnStartScrollElapsed;
	}

	private void OnStartScrollElapsed(object sender, ElapsedEventArgs e)
	{
		_startScrollUpTimer.Elapsed -= OnStartScrollElapsed;
		_startScrollUpTimer.Stop();
		if (_scrollingUp)
		{
			Utils.RunOnUiThread(base.Dispatcher, StartScrollUp);
		}
	}

	private void StartScrollUp()
	{
		_scrollingUp = true;
		Scroll(isUp: true, ScrollUpCompletedHandler);
	}

	public void ScrollToCurrentTime()
	{
		double num = ((DateTime.Now - DateTime.Today).TotalMinutes - (double)((float)CalendarGeoHelper.GetStartHour() * 60f)) / (double)((float)(CalendarGeoHelper.GetEndHour() - CalendarGeoHelper.GetStartHour()) * 60f);
		double num2 = CalendarGeoHelper.HourHeight * (double)(CalendarGeoHelper.GetEndHour() - CalendarGeoHelper.GetStartHour());
		double offset = num * num2 - base.ActualHeight * 0.5;
		ScrollToVerticalOffset(offset);
	}

	private void Scroll(bool isUp, EventHandler completedHandler)
	{
		if (!_scrolling && Mouse.LeftButton == MouseButtonState.Pressed)
		{
			DoubleAnimation doubleAnimation = new DoubleAnimation
			{
				From = base.VerticalOffset,
				To = base.VerticalOffset + (double)(isUp ? (-36) : 36),
				Duration = new Duration(TimeSpan.FromMilliseconds(32.0))
			};
			_scrollStoryboard.Children.Clear();
			_scrollStoryboard.Children.Add(doubleAnimation);
			Storyboard.SetTarget(doubleAnimation, this);
			Storyboard.SetTargetProperty(doubleAnimation, new PropertyPath(ScrollAnimationBehavior.VerticalOffsetProperty));
			_scrollStoryboard.Completed -= completedHandler;
			_scrollStoryboard.Completed += completedHandler;
			_scrollStoryboard.Begin();
			_scrolling = true;
		}
	}

	private void StopScrollUp()
	{
		_scrollingUp = false;
		_startScrollUpTimer.Elapsed -= OnStartScrollElapsed;
	}

	private void ScrollUpCompletedHandler(object sender, EventArgs e)
	{
		_scrolling = false;
		if (_scrollingUp)
		{
			Scroll(isUp: true, ScrollUpCompletedHandler);
		}
	}

	private void StartScrollDown()
	{
		_scrollingDown = true;
		Scroll(isUp: false, ScrollDownCompletedHandler);
	}

	private void ScrollDownCompletedHandler(object sender, EventArgs e)
	{
		_scrolling = false;
		if (_scrollingDown)
		{
			Scroll(isUp: false, ScrollDownCompletedHandler);
		}
	}

	private void StopScrollDown()
	{
		_scrollingDown = false;
	}

	public void RemoveAddCell()
	{
		foreach (CalendarTimelineDayView dayView in _dayViews)
		{
			dayView.RemoveAddCell();
		}
	}

	public DateTime GetHoverDate(double currentX)
	{
		double dayViewWidth = _parent.DayViewWidth;
		int num = (int)((currentX - 56.0) / dayViewWidth);
		DateTime item = _parent.GetTimeSpan().Item1;
		if (num >= 0 && num < _parent.DisplayDays && _dayViews.Count > num + 1)
		{
			return _dayViews[num + 1].GetDate();
		}
		return item;
	}

	public double GetHorizontalOffset(double currentX)
	{
		return CalendarGeoHelper.CalHorizontalOffset(currentX, CalendarGeoHelper.GetWeekColumnWidth(base.ActualWidth, _parent.DisplayDays)) - 1.0;
	}

	public void ClearItems(bool onlyData)
	{
		foreach (CalendarTimelineDayView dayView in _dayViews)
		{
			if (dayView != null)
			{
				dayView.ClearItems(onlyData);
				dayView.DataContext = null;
			}
		}
	}

	public void InitBoard()
	{
		_timeLine?.InitBoard(ShowPointer(), _parent?.GetStartDate());
	}

	private void InitFoldBorder()
	{
		_topFoldBorder.SetValue(Panel.ZIndexProperty, 3);
		_topFoldBorder.SetValue(Grid.ColumnSpanProperty, 2);
		_botFoldBorder.SetValue(Panel.ZIndexProperty, 3);
		_botFoldBorder.SetValue(Grid.ColumnSpanProperty, 2);
		_container.Children.Add(_topFoldBorder);
		_container.Children.Add(_botFoldBorder);
		_topFoldBorder.PreviewMouseLeftButtonDown += OnFoldBorderMouseDown;
		_botFoldBorder.PreviewMouseLeftButtonDown += OnFoldBorderMouseDown;
		_topFoldBorder.MouseLeftButtonUp += OnTopFoldClick;
		_botFoldBorder.MouseLeftButtonUp += OnBotFoldClick;
		_topFoldBorder.SetBinding(UIElement.IsHitTestVisibleProperty, StaticPropertiesModel.HitTestBinding);
		_botFoldBorder.SetBinding(UIElement.IsHitTestVisibleProperty, StaticPropertiesModel.HitTestBinding);
		Border border = new Border
		{
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			CornerRadius = new CornerRadius(2.0)
		};
		border.SetResourceReference(Control.BackgroundProperty, "ToolTipColor");
		Border border2 = new Border
		{
			CornerRadius = new CornerRadius(2.0),
			BorderThickness = new Thickness(0.0),
			Effect = new DropShadowEffect
			{
				Opacity = 0.12,
				BlurRadius = 11.0,
				ShadowDepth = 2.0,
				Direction = 280.0
			}
		};
		border2.SetResourceReference(Control.BorderBrushProperty, "DarkBorderColor");
		border.Child = border2;
		TextBlock textBlock = new TextBlock
		{
			Padding = new Thickness(6.0),
			TextTrimming = TextTrimming.CharacterEllipsis,
			Text = Utils.GetString("DragToAdjustHiddenTime"),
			TextAlignment = TextAlignment.Center
		};
		textBlock.SetResourceReference(FrameworkElement.StyleProperty, "Tag01");
		textBlock.Foreground = Brushes.White;
		border2.Child = textBlock;
		_foldTooltipGrid.Children.Add(border);
		Geometry data = Geometry.Parse("M0,4 4,8,4,0z");
		Path path = new Path
		{
			Data = data,
			Margin = new Thickness(-4.0, 0.0, 0.0, 0.0),
			Height = 8.0,
			VerticalAlignment = VerticalAlignment.Center,
			HorizontalAlignment = HorizontalAlignment.Left
		};
		path.SetResourceReference(Shape.FillProperty, "ToolTipColor");
		Path path2 = new Path
		{
			Data = data,
			Margin = new Thickness(-4.0, 0.0, 0.0, 0.0),
			Height = 8.0,
			VerticalAlignment = VerticalAlignment.Center,
			HorizontalAlignment = HorizontalAlignment.Left
		};
		path2.SetResourceReference(Shape.FillProperty, "DarkBorderColor");
		_foldTooltipGrid.Children.Add(path2);
		_foldTooltipGrid.Children.Add(path);
		_foldTooltipGrid.Visibility = Visibility.Collapsed;
		_foldTooltipGrid.SetValue(Panel.ZIndexProperty, 100);
		_foldTooltipGrid.SetValue(Grid.ColumnProperty, 1);
		_container.Children.Add(_foldTooltipGrid);
	}

	public void ShowOrHideFoldTooltip(bool show)
	{
		if (show && (_timeLine.TopFoldHover || _timeLine.BottomFoldHover))
		{
			_foldTooltipGrid.Margin = new Thickness(6.0, (double)(_timeLine.TopFoldHover ? LocalSettings.Settings.CollapsedStart : LocalSettings.Settings.CollapsedEnd) * LocalSettings.Settings.CalendarHourHeight - 12.0, 0.0, 0.0);
			_foldTooltipGrid.Visibility = Visibility.Visible;
		}
		else
		{
			_foldTooltipGrid.Visibility = Visibility.Collapsed;
		}
	}

	private void OnFoldBorderMouseDown(object sender, MouseButtonEventArgs e)
	{
		_foldBorderMouseDown = true;
	}

	private void OnTopFoldClick(object sender, MouseButtonEventArgs e)
	{
		if (_foldBorderMouseDown)
		{
			_foldBorderMouseDown = false;
			CalendarGeoHelper.TopFolded = false;
			CalendarGeoHelper.NotifyTopUnFolded(isTop: true);
		}
	}

	private void OnBotFoldClick(object sender, MouseButtonEventArgs e)
	{
		if (_foldBorderMouseDown)
		{
			_foldBorderMouseDown = false;
			CalendarGeoHelper.TopFolded = false;
			CalendarGeoHelper.NotifyTopUnFolded(isTop: false);
		}
	}

	private void OnSizeChanged(object sender, SizeChangedEventArgs e)
	{
		if (e.HeightChanged)
		{
			double hourHeight = CalendarGeoHelper.HourHeight;
			int num = 24 - CalendarGeoHelper.GetCollapsedHours();
			if (hourHeight * (double)num + (double)(CalendarGeoHelper.TopFolded ? 64 : 0) < base.ActualHeight)
			{
				ZoomHelper.Zoom(0.0, base.ActualHeight, this);
			}
		}
		if (CalendarGeoHelper.TopFolded)
		{
			double hourHeight2 = CalendarGeoHelper.HourHeight;
			int num2 = 24 - CalendarGeoHelper.GetCollapsedHours();
			double num3 = hourHeight2 * (double)num2 + (double)(CalendarGeoHelper.TopFolded ? 32 : 0);
			_botFoldBorder.Height = Math.Max(32.0, base.ActualHeight - num3);
		}
	}

	public void OnTouchScroll(double offset, bool showWeekend)
	{
		if (_data == null)
		{
			return;
		}
		bool flag = false;
		double num = _parent?.DayViewWidth ?? base.ActualWidth;
		int num2 = _parent?.DisplayDays ?? 0;
		foreach (CalendarTimelineDayView dayView in _dayViews)
		{
			if (!(dayView.RenderTransform is TranslateTransform translateTransform))
			{
				continue;
			}
			double num3 = translateTransform.X + offset;
			DateTime date = dayView.GetDate();
			if (Utils.IsEmptyDate(date))
			{
				continue;
			}
			DateTime newDate = date;
			if (num3 <= -1.5 * num)
			{
				num3 += (double)(num2 + 2) * num;
				flag = true;
				newDate = date.AddDays(num2 + 2);
				if (!showWeekend)
				{
					newDate = newDate.AddDays((date.DayOfWeek == DayOfWeek.Friday || date.DayOfWeek == DayOfWeek.Thursday) ? 4 : 2);
				}
			}
			if (num3 >= ((double)num2 + 0.5) * num + 2.0)
			{
				num3 -= (double)(num2 + 2) * num;
				flag = true;
				newDate = date.AddDays(-2 - num2);
				if (!showWeekend)
				{
					newDate = newDate.AddDays((date.DayOfWeek == DayOfWeek.Monday || date.DayOfWeek == DayOfWeek.Tuesday) ? (-4) : (-2));
				}
			}
			dayView.Offset = num3;
			translateTransform.X = num3;
			if (newDate != date)
			{
				CalendarTimelineDayViewModel calendarTimelineDayViewModel = _data.FirstOrDefault((CalendarTimelineDayViewModel d) => d.Date == newDate);
				if (calendarTimelineDayViewModel != null)
				{
					dayView.DataContext = calendarTimelineDayViewModel;
					dayView.Reload(num);
					continue;
				}
				dayView.DataContext = new CalendarTimelineDayViewModel
				{
					Date = newDate
				};
				dayView.Reload(num);
				LoadModelsWhenScrolling(newDate, newDate > date);
			}
		}
		if (flag)
		{
			_dayViews.Sort((CalendarTimelineDayView a, CalendarTimelineDayView b) => a.Offset.CompareTo(b.Offset));
		}
	}

	private async Task LoadModelsWhenScrolling(DateTime newDate, bool next)
	{
		List<CalendarTimelineDayViewModel> list = ((_parent != null) ? (await _parent.LoadModelsWhenScrolling(newDate, next)) : null);
		List<CalendarTimelineDayViewModel> list2 = list;
		if (list2 == null)
		{
			return;
		}
		_data = list2;
		foreach (CalendarTimelineDayView dayView in _dayViews)
		{
			object dataContext = dayView.DataContext;
			CalendarTimelineDayViewModel m = dataContext as CalendarTimelineDayViewModel;
			if (m != null)
			{
				CalendarTimelineDayViewModel calendarTimelineDayViewModel = _data.FirstOrDefault((CalendarTimelineDayViewModel d) => d.Date == m.Date);
				if (calendarTimelineDayViewModel != m && calendarTimelineDayViewModel != null)
				{
					dayView.DataContext = calendarTimelineDayViewModel;
					dayView.Reload(_parent?.DayViewWidth);
				}
			}
		}
	}

	private void SetPointerPosition()
	{
		int pointOffset = CalendarGeoHelper.GetPointOffset();
		bool flag = pointOffset >= (CalendarGeoHelper.TopFolded ? 32 : 0) && (_parent?.ShowTimePoint() ?? false);
		_timeLine?.InitBoard(flag, _parent?.GetStartDate());
		if (flag)
		{
			if (_timePointer.Children.Count == 0)
			{
				_timePointer.SetValue(Panel.ZIndexProperty, 10);
				_timePointer.ColumnDefinitions.Add(new ColumnDefinition
				{
					Width = new GridLength(56.0)
				});
				_timePointer.ColumnDefinitions.Add(new ColumnDefinition
				{
					Width = new GridLength(1.0, GridUnitType.Star)
				});
				Line line = new Line
				{
					X1 = 0.0,
					X2 = 1.0,
					Stretch = Stretch.Fill,
					StrokeThickness = 1.0,
					Stroke = ThemeUtil.GetColorInString("#33E03131"),
					VerticalAlignment = VerticalAlignment.Top,
					Margin = new Thickness(0.0, 9.0, 0.0, 0.0),
					IsHitTestVisible = false
				};
				line.SetValue(Grid.ColumnProperty, 1);
				_timePointer.Children.Add(line);
				_container.Children.Add(_timePointer);
				_currentTimeStack = new StackPanel
				{
					Height = 18.0,
					HorizontalAlignment = HorizontalAlignment.Right,
					VerticalAlignment = VerticalAlignment.Center,
					Orientation = Orientation.Horizontal
				};
				_currentTime = new TextBlock
				{
					Foreground = ThemeUtil.GetColorInString("#E03131"),
					FontSize = 10.0,
					VerticalAlignment = VerticalAlignment.Center,
					HorizontalAlignment = HorizontalAlignment.Center,
					Margin = new Thickness(4.0, 0.0, 4.0, 0.0)
				};
				Border border = new Border
				{
					Height = 18.0,
					CornerRadius = new CornerRadius(4.0),
					Background = ThemeUtil.GetColorInString("#1AE03131"),
					Child = _currentTime,
					MinWidth = 36.0
				};
				Path element = new Path
				{
					Data = Geometry.Parse("F0 M4,10z M0,0z M0,0.283029556291895L0,9.71701004669189C0,8.33785004669189,0.71048,7.05597004669189,1.88,6.32502004669189L4,5.00002004669189 1.88,3.67502004669189C0.71048,2.94407004669189,0,1.66219004669189,0,0.283029556291895z"),
					Width = 4.0,
					Height = 10.0,
					Fill = border.Background
				};
				_currentTimeStack.Children.Add(border);
				_currentTimeStack.Children.Add(element);
				_timePointer.Children.Add(_currentTimeStack);
			}
			_timePointer.Visibility = Visibility.Visible;
			_timePointer.Margin = new Thickness((double)(CalendarGeoHelper.GetTimeZoneCount() - 1) * 56.0, pointOffset - 11, 0.0, 0.0);
			_currentTime.Text = ((LocalSettings.Settings.TimeFormat == "24Hour") ? ticktick_WPF.Util.DateUtils.FormatHourMinute(DateTime.Now, longTime: true) : DateTime.Now.ToString("h:mm"));
			_currentTimeStack.Margin = new Thickness(0.0, 0.0, (!(LocalSettings.Settings.TimeFormat == "24Hour")) ? 4 : 0, 0.0);
		}
		else if (_timePointer != null)
		{
			_timePointer.Visibility = Visibility.Collapsed;
		}
	}

	public void SetTimePointVisible(bool b)
	{
		if (b)
		{
			b = CalendarGeoHelper.GetPointOffset() >= 32;
		}
		if (_timePointer != null)
		{
			_timePointer.Visibility = ((!b) ? Visibility.Collapsed : Visibility.Visible);
		}
		SetPointerPosition();
	}

	public IMultiDayGrid GetDayView(int index)
	{
		if (index + 1 < _dayViews.Count)
		{
			return _dayViews[index + 1];
		}
		return null;
	}

	public void RemoveDraggingItem()
	{
		foreach (CalendarTimelineDayView dayView in _dayViews)
		{
			dayView.RemoveDraggingItem();
		}
	}

	public void RefreshTimeBlocks()
	{
		SetPointerPosition();
		_timeLine.Refresh();
	}

	public void ScrollOnHeightChanged(double current, double next)
	{
		ScrollToVerticalOffset((base.VerticalOffset + base.ActualHeight / 2.0) / current * next - base.ActualHeight / 2.0);
	}

	public void SetShowIcon()
	{
		foreach (CalendarTimelineDayView dayView in _dayViews)
		{
			dayView.SetShowIcon();
		}
	}

	public void SetColor()
	{
		foreach (CalendarTimelineDayView dayView in _dayViews)
		{
			dayView.SetColor();
		}
	}

	private void InitBatchEvent()
	{
		base.PreviewMouseLeftButtonDown += OnCalendarBodyMouseDown;
		_moveViewTimer.Interval = TimeSpan.FromMilliseconds(20.0);
		_moveViewTimer.Tick += OnMoveViewTimerOnElapsed;
	}

	private void OnCalendarBodyMouseDown(object sender, MouseButtonEventArgs e)
	{
		if (!Utils.IfCtrlPressed() || _timeLine.IsMouseOver)
		{
			return;
		}
		IShowTaskDetailWindow mousePointItem = Utils.GetMousePointItem<IShowTaskDetailWindow>(e.GetPosition(_daysCanvas), _daysCanvas);
		if (mousePointItem == null || !(mousePointItem is FrameworkElement { Visibility: Visibility.Visible }))
		{
			_batchMouseDown = true;
			_batchStartPoint = e.GetPosition(_daysCanvas);
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
			_batchBorder.Opacity = 1.0;
		}
	}

	private void OnMoveViewTimerOnElapsed(object sender, EventArgs e)
	{
		Point position = Mouse.GetPosition(this);
		if (position.Y > ActualHeight)
		{
			ScrollToVerticalOffset(VerticalOffset + (position.Y - ActualHeight) * 0.6);
			ShowBatchSelectBox();
		}
		else if (position.Y < 0.0)
		{
			ScrollToVerticalOffset(VerticalOffset + position.Y * 0.6);
			ShowBatchSelectBox();
		}
	}

	private void OnMouseUp(object sender, MouseButtonEventArgs e)
	{
		_batchMouseDown = false;
		StopMoveView();
		Mouse.Capture(null);
		_batchBorder.Width = 0.0;
		_batchBorder.Height = 0.0;
		_batchBorder.Margin = new Thickness(0.0);
		_batchBorder.Opacity = 0.0;
		Window window = Window.GetWindow(this);
		if (window != null)
		{
			window.MouseLeftButtonUp -= OnMouseUp;
			window.MouseMove -= OnMouseMove;
		}
		Dictionary<string, int> dictionary = new Dictionary<string, int>();
		foreach (CalendarTimelineDayView dayView in _dayViews)
		{
			foreach (string item in dayView.SetBatchSelect())
			{
				dictionary[item] = -1;
			}
		}
		_parent?.AddBatchSelected(dictionary);
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
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Top,
				BorderThickness = new Thickness(1.0),
				CornerRadius = new CornerRadius(4.0),
				Width = 0.0,
				Height = 0.0
			};
			_batchBorder.SetResourceReference(Control.BackgroundProperty, "BaseColorOpacity5");
			_batchBorder.SetResourceReference(Border.BorderBrushProperty, "BaseColorOpacity20");
			_batchBorder.SetValue(Grid.RowSpanProperty, 2);
			_batchBorder.SetValue(Panel.ZIndexProperty, 100);
			_daysCanvas.Children.Add(_batchBorder);
		}
	}

	private void ShowBatchSelectBox()
	{
		Point position = Mouse.GetPosition(_daysCanvas);
		position.X = Math.Min(Math.Max(0.0, position.X), _daysCanvas.ActualWidth);
		position.Y = Math.Min(Math.Max(0.0, position.Y), _daysCanvas.ActualHeight);
		double value = _prePoint.X - position.X;
		if (!(Math.Abs(_prePoint.Y - position.Y) < 2.0) || !(Math.Abs(value) < 2.0))
		{
			_prePoint = position;
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
		if (Math.Abs(rect.Width - _checkedWidth) < 5.0 && Math.Abs(rect.Height - _checkedHeight) < 5.0)
		{
			return;
		}
		_checkedWidth = rect.Width;
		_checkedHeight = rect.Height;
		foreach (CalendarTimelineDayView dayView in _dayViews)
		{
			if (dayView.RenderTransform is TranslateTransform translateTransform)
			{
				dayView.TryBatchSelectInRect(rect, translateTransform.X);
			}
		}
	}

	public void ClearBatchSelected()
	{
		foreach (CalendarTimelineDayView dayView in _dayViews)
		{
			dayView.ClearBatchSelected();
		}
	}

	public void SetBatchDragging(bool dragging)
	{
		foreach (CalendarTimelineDayView dayView in _dayViews)
		{
			dayView.SetBatchDragging(dragging);
		}
	}

	public MultiDayView GetParent()
	{
		return _parent;
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
		foreach (CalendarTimelineDayView dayView in _dayViews)
		{
			dayView.CheckDragging();
		}
	}

	public void SetBatchSelected(BlockingSet<string> batchSelected)
	{
		foreach (CalendarTimelineDayView dayView in _dayViews)
		{
			dayView.SetBatchSelected(batchSelected);
		}
	}
}
