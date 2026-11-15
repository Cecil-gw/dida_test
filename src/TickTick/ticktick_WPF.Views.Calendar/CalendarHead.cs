using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using ticktick_WPF.Dal;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Misc;
using ticktick_WPF.Views.Project;
using ticktick_WPF.Views.Time;
using ticktick_WPF.Views.Widget;

namespace ticktick_WPF.Views.Calendar;

public class CalendarHead : UserControl, IComponentConnector
{
	private CalendarControl _calendarControl;

	private int _previousMode;

	private ProjectOrGroupPopup _popup;

	private WidgetMorePopup _morePopup;

	private string _mode = "0";

	private DateTime _startDate;

	private bool _moreMouseDown;

	private bool _addMouseDown;

	private bool _inParallel;

	internal CalendarHead Root;

	internal RowDefinition FirstRow;

	internal Rectangle DragPanel;

	internal StackPanel LeftPanel;

	internal Button ShowFilterButton;

	internal Path ShowCalendarSideImage;

	internal Border DateBorder;

	internal StackPanel DatePanel;

	internal TextBlock MonthText;

	internal Grid AddGrid;

	internal Rectangle AddBorder;

	internal Border AddBorderInParallel;

	internal Path AddPath;

	internal WeekMonthSwitch WeekMonthSwitch;

	internal Grid MiniDateSwitch;

	internal Path MiniLeftArrow;

	internal Border NextGrid;

	internal Path MiniRightArrow;

	internal Grid DateSwitch;

	internal Border LastBorder;

	internal Path LeftArrow;

	internal Border TodayBorder;

	internal TextBlock TodayText;

	internal Border NextBorder;

	internal Path RightArrow;

	internal StackPanel LockedOptionGrid;

	internal Button UnlockButton;

	internal Button LockedSyncButton;

	internal Image LockedSyncImage;

	internal StackPanel UnlockOptionGrid;

	internal Button MoreButton;

	internal Path UnlockedSyncPath;

	private bool _contentLoaded;

	public Debouncer yearLastNextDebouncer { get; set; }

	private EscPopup MonthPopup { get; set; }

	public string Mode
	{
		get
		{
			return _mode;
		}
		set
		{
			_mode = value;
		}
	}

	public DateTime StartDate
	{
		get
		{
			return _startDate;
		}
		set
		{
			_startDate = value;
			SetDateText();
			if (SelectedDate.HasValue && GetCurrentMonthDate().Month != SelectedDate?.Month)
			{
				SelectedDate = null;
			}
		}
	}

	public DateTime EndDate { get; set; }

	public DateTime? SelectedDate { get; set; }

	public event EventHandler<WidgetMoreAction> MoreAction;

	public event EventHandler<string> Action;

	public event EventHandler AddClick;

	public event WeekMonthSwitch.SwitchModeDelegate SwitchView;

	public event EventHandler Today;

	public event EventHandler Last;

	public event EventHandler Next;

	public event EventHandler<DateTime> DateSelect;

	public CalendarHead()
	{
		InitializeComponent();
		WeekMonthSwitch.SwitchMode += delegate(string from, string to, bool b)
		{
			Mode = to;
			this.SwitchView?.Invoke(from, to, b);
		};
		SetDateText();
		base.SizeChanged += OnSizeChanged;
		LeftArrow.RenderTransform = new RotateTransform(90.0);
		RightArrow.RenderTransform = new RotateTransform(-90.0);
		MiniLeftArrow.RenderTransform = new RotateTransform(0.0);
		MiniRightArrow.RenderTransform = new RotateTransform(0.0);
		yearLastNextDebouncer = new Debouncer(3000, OnYearLastNextFinish);
	}

	private void SetDateText()
	{
		if (Utils.IsEmptyDate(StartDate))
		{
			EndDate = DateTime.Today;
			_startDate = DateTime.Today;
		}
		DateTime currentMonthDate = GetCurrentMonthDate(checkToday: false, forTitle: true);
		CalendarControl parent = GetParent();
		string text;
		if (Mode == "10")
		{
			text = DateUtils.FormatYear(currentMonthDate);
		}
		else
		{
			if (UserDao.IsPro())
			{
				CalendarControl parent2 = GetParent();
				if (parent2 == null || !parent2.IsVirtual)
				{
					text = DateUtils.FormatYearMonth(currentMonthDate, (base.ActualWidth < 500.0) ? DateFormatLen.Middle : DateFormatLen.Long);
					goto IL_00b5;
				}
			}
			text = ((parent == null || parent.ViewSourceType != CalendarViewSourceType.Widget) ? DateUtils.FormatMonth(new DateTime(2023, 9, 1), longMonth: true) : DateUtils.FormatYearMonth(DateTime.Today, DateFormatLen.Long));
		}
		goto IL_00b5;
		IL_00b5:
		MonthText.Text = text;
	}

	public CalendarControl GetParent()
	{
		_calendarControl = _calendarControl ?? Utils.FindParent<CalendarControl>(this);
		return _calendarControl;
	}

	private void OnTodayClick(object sender, RoutedEventArgs e)
	{
		this.Today?.Invoke(sender, null);
		UserActCollectUtils.AddClickEvent("calendar", "action", "back_to_today");
	}

	private void OnMoreClick(object sender, RoutedEventArgs e)
	{
		if (!_moreMouseDown)
		{
			return;
		}
		_moreMouseDown = false;
		CalendarControl parent = GetParent();
		if (parent != null && parent.ViewSourceType == CalendarViewSourceType.Widget)
		{
			parent?.SetEditting(editting: true);
			if (_morePopup == null)
			{
				_morePopup = new WidgetMorePopup(OnPopupClosed, DateSwitch.IsEnabled);
				_morePopup.SetPlaceTarget(MoreButton);
				_morePopup.MoreAction += OnMoreMoreAction;
				_morePopup.Action += OnActionSelected;
			}
			_morePopup.Show(new Point(-5.0, -10.0), WidgetType.Calendar);
			return;
		}
		CalendarControl calendarControl = Utils.FindParent<CalendarControl>(this);
		bool showClose = calendarControl != null && calendarControl.ViewSourceType == CalendarViewSourceType.WithList;
		bool showPrint = (calendarControl == null || !calendarControl.ParallelShow) && Mode != "8" && Mode != "10";
		CalMoreItems calMoreItems = new CalMoreItems(MoreButton, showClose, base.ActualWidth < 500.0, (base.ActualWidth < 450.0) ? Mode : null, showPrint);
		calMoreItems.Show();
		calMoreItems.SwitchMode += delegate(string from, string to, bool b)
		{
			Mode = to;
			if (WeekMonthSwitch.DataContext is WeekMonthViewModel weekMonthViewModel)
			{
				weekMonthViewModel.Mode = to;
			}
			this.SwitchView?.Invoke(from, to, b);
		};
		calMoreItems.Action += OnActionSelected;
		calMoreItems.Show();
	}

	private async void OnActionSelected(object sender, string e)
	{
		if (e == "AddTask")
		{
			await Task.Delay(200);
			this.AddClick?.Invoke(MoreButton, null);
		}
		else
		{
			this.Action?.Invoke(this, e);
		}
	}

	private void OnMoreMoreAction(object sender, WidgetMoreAction e)
	{
		this.MoreAction?.Invoke(this, e);
	}

	private void OnLastClick(object sender, RoutedEventArgs e)
	{
		this.Last?.Invoke(sender, null);
		UserActCollectUtils.AddClickEvent("calendar", "action", "back_btn");
		AddWeekMonthAction();
		yearLastNextDebouncer?.Trigger();
	}

	public void AddWeekMonthAction()
	{
		if (Mode == "1" || Mode == "0")
		{
			string mode = Mode;
			DelayActionHandlerCenter.TryDoAction("AddCalLastNextClickAction", delegate
			{
				UserActCollectUtils.AddClickEvent("calendar", (mode == "1") ? "week_action" : "month_action", (mode == "1") ? "swipe_vertical" : "swipe_horizontal");
			}, 3000);
		}
	}

	private void OnNextClick(object sender, RoutedEventArgs e)
	{
		this.Next?.Invoke(sender, null);
		UserActCollectUtils.AddClickEvent("calendar", "action", "forward_btn");
		AddWeekMonthAction();
		yearLastNextDebouncer?.Trigger();
	}

	private void OnYearLastNextFinish()
	{
		UserActCollectUtils.AddClickEvent("calendar", "year_action", "swipe_horizontal");
	}

	private void OnDateSelect(object sender, MouseButtonEventArgs e)
	{
		if (MonthPopup != null)
		{
			return;
		}
		MonthPicker monthPicker = new MonthPicker(DateUtils.GetCurrentMonthDate(StartDate, EndDate));
		GetParent()?.SetEditting(editting: true);
		Mouse.GetPosition(MonthText);
		EscPopup monthPopup = new EscPopup
		{
			StaysOpen = false,
			PopupAnimation = PopupAnimation.Fade,
			Placement = PlacementMode.Bottom,
			PlacementTarget = DatePanel,
			HorizontalOffset = -5.0,
			VerticalOffset = -2.0
		};
		MonthPopup = monthPopup;
		monthPopup.Child = monthPicker;
		monthPopup.Closed += MonthPopup_Closed;
		monthPicker.MonthSelected += delegate(object o, DateTime time)
		{
			if (MonthPopup == monthPopup)
			{
				monthPopup.IsOpen = false;
				if (time.Month == DateTime.Today.Month && time.Year == DateTime.Today.Year)
				{
					time = DateTime.Today;
				}
				this.DateSelect?.Invoke(this, time);
				UserActCollectUtils.AddClickEvent("calendar", "action", "switch_month");
			}
		};
		monthPopup.IsOpen = true;
	}

	private void MonthPopup_Closed(object sender, EventArgs e)
	{
		OnPopupClosed();
	}

	private void OnCalendarHeadClick(object sender, MouseButtonEventArgs e)
	{
	}

	private void OnPopupClosed()
	{
		GetParent()?.SetEditting(editting: false);
		if (MonthPopup != null)
		{
			MonthPopup.Closed -= MonthPopup_Closed;
		}
		MonthPopup = null;
	}

	public void BeginSyncStory()
	{
		LockedSyncButton.IsHitTestVisible = false;
		MoreButton.Visibility = Visibility.Collapsed;
		((Storyboard)FindResource(LockedOptionGrid.IsVisible ? "LockedSyncStory" : "UnlockedSyncStory")).Begin();
	}

	private void SyncStoryCompleted(object sender, EventArgs e)
	{
		MoreButton.Visibility = Visibility.Visible;
		LockedSyncButton.IsHitTestVisible = true;
	}

	public void OnSwitchKeyUp(Key key)
	{
		WeekMonthSwitch.QuickSwitch(key);
	}

	public void SetEnable(bool enable, bool inWidget)
	{
		WeekMonthSwitch.IsEnabled = enable;
		DateSwitch.IsEnabled = enable;
		AddGrid.IsEnabled = enable;
		LeftPanel.IsEnabled = enable;
		MoreButton.IsEnabled = enable || inWidget;
		SetDateText(null);
	}

	private void OnShowFilterClick(object sender, RoutedEventArgs e)
	{
		GetParent()?.ShowOrHideCalSide();
	}

	public void SetMode(string mode)
	{
		Mode = mode;
		if (WeekMonthSwitch.DataContext is WeekMonthViewModel weekMonthViewModel)
		{
			weekMonthViewModel.Mode = mode;
		}
		else
		{
			WeekMonthSwitch.DataContext = new WeekMonthViewModel
			{
				Mode = mode
			};
		}
		SetDateText();
	}

	public DateTime GetCurrentMonthDate(bool checkToday = false, bool forTitle = false)
	{
		CalendarControl parent = GetParent();
		if (parent != null && parent.IsVirtual)
		{
			return new DateTime(2023, 9, 6);
		}
		DateTime currentMonthDate = DateUtils.GetCurrentMonthDate(StartDate, EndDate);
		if (SelectedDate.HasValue && SelectedDate.Value >= StartDate && SelectedDate.Value <= EndDate && (!forTitle || SelectedDate.Value.Month == currentMonthDate.Month))
		{
			return SelectedDate.Value;
		}
		if ((DateTime.Today.Month == currentMonthDate.Month || checkToday) && DateTime.Today >= StartDate && DateTime.Today <= EndDate)
		{
			return DateTime.Today;
		}
		if (Mode == "10" && (DateTime.Today.Year == currentMonthDate.Year || checkToday) && DateTime.Today >= StartDate && DateTime.Today <= EndDate)
		{
			return DateTime.Today;
		}
		if (currentMonthDate.Month != StartDate.Month)
		{
			return currentMonthDate;
		}
		return StartDate;
	}

	public List<DateTime> GetRangeDates()
	{
		int days = (EndDate - _startDate).Days;
		List<DateTime> list = new List<DateTime>();
		for (int i = 0; i <= days; i++)
		{
			list.Add(_startDate.AddDays(i));
		}
		return list;
	}

	public void SetShowCalendarSide(bool isShow)
	{
		ShowCalendarSideImage.ToolTip = Utils.GetString(isShow ? "CollapseCal" : "OpenCal");
	}

	public void SetDateText(DateTime? date)
	{
		if (!date.HasValue)
		{
			SetDateText();
		}
		else
		{
			MonthText.Text = DateUtils.FormatYearMonth(date.Value, DateFormatLen.Short);
		}
	}

	private void OnAddButtonClick(object sender, MouseButtonEventArgs e)
	{
		if (_addMouseDown)
		{
			e.Handled = true;
			_addMouseDown = false;
			if (PopupStateManager.CanShowAddPopup())
			{
				this.AddClick?.Invoke(sender, e);
			}
		}
	}

	private void OnSizeChanged(object sender, SizeChangedEventArgs e)
	{
		AddGrid.Visibility = ((e.NewSize.Width < 450.0) ? Visibility.Collapsed : Visibility.Visible);
		WeekMonthSwitch.Visibility = ((e.NewSize.Width < 400.0) ? Visibility.Collapsed : Visibility.Visible);
		DateSwitch.Visibility = ((_inParallel || e.NewSize.Width < 350.0) ? Visibility.Collapsed : Visibility.Visible);
		MiniDateSwitch.Visibility = ((!_inParallel && !(e.NewSize.Width < 350.0)) ? Visibility.Collapsed : Visibility.Visible);
		SetDateText();
		SetParallelStyle();
	}

	private void OnMorePreviewDown(object sender, MouseButtonEventArgs e)
	{
		_moreMouseDown = true;
	}

	private void OnAddMouseDown(object sender, MouseButtonEventArgs e)
	{
		if (PopupStateManager.CanShowAddPopup())
		{
			_addMouseDown = true;
		}
	}

	public void SetShowInParallel(bool inParallel)
	{
		_inParallel = inParallel;
		SetParallelStyle();
	}

	private void SetParallelStyle()
	{
		bool flag = _inParallel && base.ActualWidth < 600.0;
		WeekMonthSwitch.SetParallelShow(flag);
		WeekMonthSwitch.Margin = new Thickness(0.0, 0.0, flag ? 4 : 16, 0.0);
		DateSwitch.Visibility = ((flag || base.ActualWidth < 400.0) ? Visibility.Collapsed : Visibility.Visible);
		MiniDateSwitch.Visibility = ((!flag && !(base.ActualWidth < 400.0)) ? Visibility.Collapsed : Visibility.Visible);
		AddBorder.Visibility = (flag ? Visibility.Collapsed : Visibility.Visible);
		AddBorderInParallel.Visibility = ((!flag) ? Visibility.Collapsed : Visibility.Visible);
		AddGrid.Margin = new Thickness(6.0, 0.0, flag ? 4 : 16, 0.0);
		AddPath.SetResourceReference(FrameworkElement.StyleProperty, flag ? "AddPathInParallelStyle" : "AddPathStyle");
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/calendar/calendarhead.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	internal Delegate _CreateDelegate(Type delegateType, string handler)
	{
		return Delegate.CreateDelegate(delegateType, this, handler);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 1:
			Root = (CalendarHead)target;
			break;
		case 2:
			((Storyboard)target).Completed += SyncStoryCompleted;
			break;
		case 3:
			((Storyboard)target).Completed += SyncStoryCompleted;
			break;
		case 4:
			FirstRow = (RowDefinition)target;
			break;
		case 5:
			((Grid)target).PreviewMouseLeftButtonUp += OnCalendarHeadClick;
			break;
		case 6:
			DragPanel = (Rectangle)target;
			break;
		case 7:
			LeftPanel = (StackPanel)target;
			break;
		case 8:
			ShowFilterButton = (Button)target;
			ShowFilterButton.Click += OnShowFilterClick;
			break;
		case 9:
			ShowCalendarSideImage = (Path)target;
			break;
		case 10:
			DateBorder = (Border)target;
			DateBorder.PreviewMouseLeftButtonUp += OnDateSelect;
			break;
		case 11:
			DatePanel = (StackPanel)target;
			break;
		case 12:
			MonthText = (TextBlock)target;
			break;
		case 13:
			AddGrid = (Grid)target;
			AddGrid.PreviewMouseLeftButtonUp += OnAddMouseDown;
			AddGrid.MouseLeftButtonUp += OnAddButtonClick;
			break;
		case 14:
			AddBorder = (Rectangle)target;
			break;
		case 15:
			AddBorderInParallel = (Border)target;
			break;
		case 16:
			AddPath = (Path)target;
			break;
		case 17:
			WeekMonthSwitch = (WeekMonthSwitch)target;
			break;
		case 18:
			MiniDateSwitch = (Grid)target;
			break;
		case 19:
			((Border)target).MouseLeftButtonUp += OnLastClick;
			break;
		case 20:
			MiniLeftArrow = (Path)target;
			break;
		case 21:
			((Border)target).MouseLeftButtonUp += OnTodayClick;
			break;
		case 22:
			NextGrid = (Border)target;
			NextGrid.MouseLeftButtonUp += OnNextClick;
			break;
		case 23:
			MiniRightArrow = (Path)target;
			break;
		case 24:
			DateSwitch = (Grid)target;
			break;
		case 25:
			LastBorder = (Border)target;
			LastBorder.PreviewMouseLeftButtonUp += OnLastClick;
			break;
		case 26:
			LeftArrow = (Path)target;
			break;
		case 27:
			TodayBorder = (Border)target;
			TodayBorder.PreviewMouseLeftButtonUp += OnTodayClick;
			break;
		case 28:
			TodayText = (TextBlock)target;
			break;
		case 29:
			NextBorder = (Border)target;
			NextBorder.PreviewMouseLeftButtonUp += OnNextClick;
			break;
		case 30:
			RightArrow = (Path)target;
			break;
		case 31:
			LockedOptionGrid = (StackPanel)target;
			break;
		case 32:
			UnlockButton = (Button)target;
			break;
		case 33:
			LockedSyncButton = (Button)target;
			break;
		case 34:
			LockedSyncImage = (Image)target;
			break;
		case 35:
			UnlockOptionGrid = (StackPanel)target;
			break;
		case 36:
			MoreButton = (Button)target;
			MoreButton.PreviewMouseLeftButtonDown += OnMorePreviewDown;
			MoreButton.Click += OnMoreClick;
			break;
		case 37:
			UnlockedSyncPath = (Path)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
