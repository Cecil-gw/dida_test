using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Util;
using ticktick_WPF.Util.DragAndDrop;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Detail;
using ticktick_WPF.Views.Timeline;

namespace ticktick_WPF.Views.Calendar.Year;

public class YearCalendarControl : UserControl, INavigate, IComponentConnector
{
	internal YearCalendarControl Root;

	internal Grid Mask;

	private bool _contentLoaded;

	private bool _touchScrolling { get; set; }

	private bool isTouchMoveDown { get; set; }

	private CalendarControl _calendarControl { get; set; }

	internal YearCalendarViewModel ViewModel { get; set; }

	private MonthCalendarDraw lastMouseOverDraw { get; set; }

	public YearCalendarControl()
	{
		InitializeComponent();
		base.DataContext = (ViewModel = new YearCalendarViewModel(this));
	}

	private void Root_Loaded(object sender, RoutedEventArgs e)
	{
		TickDragHelper.DragMoving += OnDragOver;
		TickDragHelper.DragDrop += OnDragDrop;
		CalendarEventChangeNotifier.TitleChanged += OnEventTitleChanged;
	}

	private void Root_Unloaded(object sender, RoutedEventArgs e)
	{
		TickDragHelper.DragMoving -= OnDragOver;
		TickDragHelper.DragDrop -= OnDragDrop;
		CalendarEventChangeNotifier.TitleChanged -= OnEventTitleChanged;
		if (ViewModel != null)
		{
			ViewModel.Dispose();
		}
	}

	public DateTime Today()
	{
		ViewModel.LoadYear(DateTime.Today);
		return DateTime.Today;
	}

	public DateTime Next()
	{
		ViewModel.LoadNextYear();
		return ViewModel.SelectedDate;
	}

	public DateTime Last()
	{
		ViewModel.LoadPrevYear();
		return ViewModel.SelectedDate;
	}

	public DateTime GoTo(DateTime dateTime, bool checkStart = true, bool scrollToNow = false, bool delay = false)
	{
		ViewModel.isVirtual = GetParent()?.IsVirtual ?? false;
		if (ViewModel.isVirtual)
		{
			Mask.Visibility = Visibility.Visible;
			LoadVirtualModels();
			return dateTime;
		}
		Mask.Visibility = Visibility.Collapsed;
		ViewModel.LoadYear(dateTime);
		return dateTime;
	}

	public (DateTime, DateTime) GetTimeSpan()
	{
		return ViewModel.GetDateRange();
	}

	public (DateTime, DateTime) GetTaskTimeSpan()
	{
		return ViewModel.GetDateRange();
	}

	public void Reload(bool byThemeChange = false)
	{
		if (!byThemeChange)
		{
			ViewModel.LoadCount();
			YearViewPopup.TryRefresh();
		}
		else
		{
			ViewModel.OnForceRedraw?.Invoke();
		}
	}

	private void OnEventTitleChanged(object sender, TextExtra extra)
	{
		YearViewPopup.TryUpdateEventTitle(extra.Id, extra.Text);
	}

	public void ShowAddWindow(DateTime dateTime)
	{
	}

	public int ExistTasks(HashSet<string> taskIds)
	{
		return ViewModel._tasks?.Where((CalendarDisplayModel model) => taskIds.Contains(model.Id)).ToList().Count ?? 0;
	}

	public void RemoveItem(CalendarDisplayViewModel model)
	{
		YearViewPopup.TryRefresh();
	}

	public void LoadVirtualModels()
	{
		ViewModel.LoadVirtualModels();
	}

	public CalendarControl GetParent()
	{
		_calendarControl = _calendarControl ?? Utils.FindParent<CalendarControl>(this);
		return _calendarControl;
	}

	public void OnDoubleFingerTouch()
	{
		_touchScrolling = true;
		DelayActionHandlerCenter.TryDoAction("CalendarInputScroll", TryStopTouchScroll, 100);
	}

	private void TryStopTouchScroll(object sender, EventArgs e)
	{
		base.Dispatcher.Invoke((Func<Task>)async delegate
		{
			await OnStopTouchScroll();
		});
	}

	private async Task OnStopTouchScroll()
	{
		_touchScrolling = false;
		await ViewModel.OnTouchScrollEnd(isTouchMoveDown);
	}

	private void Root_SizeChanged(object sender, SizeChangedEventArgs e)
	{
		ViewModel.OnContainerSizeChanged(e.NewSize);
	}

	private async void Grid_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
	{
		YearViewPopup.TryClose();
		if (_touchScrolling)
		{
			if (Math.Abs(e.Delta) >= 2)
			{
				isTouchMoveDown = e.Delta < 0;
				ViewModel.OnTouchScroll(e.Delta);
			}
		}
		else
		{
			ViewModel.Scroll(e.Delta < 0);
		}
	}

	public void ResetNewSwitchViewFlag()
	{
		ViewModel.IsNewSwitchView = true;
	}

	public async Task OnSideVisibleAnimate(int duration)
	{
		try
		{
			ViewModel.IsDisableUpdateRect = true;
			await Task.Delay(duration + 20);
			ViewModel.IsDisableUpdateRect = false;
			ViewModel.UpdateRect();
		}
		catch (Exception)
		{
			ViewModel.IsDisableUpdateRect = false;
		}
	}

	private void OnDragOver(object sender, MouseEventArgs e)
	{
		Point position = e.GetPosition(this);
		DependencyObject dependencyObject = VisualTreeHelper.HitTest(this, position)?.VisualHit;
		if (dependencyObject != null)
		{
			MonthCalendarDraw monthCalendarDraw = FindAncestor<MonthCalendarDraw>(dependencyObject);
			if (monthCalendarDraw != lastMouseOverDraw && lastMouseOverDraw != null)
			{
				lastMouseOverDraw.DragHighLight(highlight: false);
			}
			lastMouseOverDraw = monthCalendarDraw;
			monthCalendarDraw?.DragHighLight();
		}
	}

	private async void OnDragDrop(object sender, MouseEventArgs e)
	{
		if (!base.IsVisible || !(Window.GetWindow(this)?.IsMouseCaptured ?? false) || TaskDetailPopup.IsDragOver(e))
		{
			return;
		}
		object data = TickDragHelper.GetDragData();
		if (data == null)
		{
			return;
		}
		Point position = e.GetPosition(this);
		DependencyObject dependencyObject = VisualTreeHelper.HitTest(this, position)?.VisualHit;
		if (dependencyObject == null)
		{
			return;
		}
		MonthCalendarDraw monthCalendarDraw = FindAncestor<MonthCalendarDraw>(dependencyObject);
		if (monthCalendarDraw == null)
		{
			return;
		}
		DateTime? mouseoverDay = monthCalendarDraw.GetMouseoverDay();
		if (!mouseoverDay.HasValue)
		{
			return;
		}
		DateTime dropDate = mouseoverDay.Value;
		bool needAddEvent = false;
		CalendarControl calendar = Utils.FindParent<CalendarControl>(this);
		object obj = data;
		if (!(obj is DisplayItemModel displayItemModel))
		{
			if (!(obj is TimelineCellViewModel timelineCellViewModel))
			{
				if (!(obj is CheckItemViewModel checkItemViewModel))
				{
					if (obj is CalendarDisplayViewModel calendarDisplayViewModel)
					{
						DateTime value = dropDate;
						DateTime? obj2 = (calendarDisplayViewModel.DragStartDate ?? calendarDisplayViewModel.StartDate)?.Date;
						int? num = (int?)(value - obj2)?.TotalDays;
						if (calendarDisplayViewModel.BatchSelected)
						{
							if (num != 0)
							{
								await calendar.OnMultiWeekBatchDragDrop(num.GetValueOrDefault());
							}
							return;
						}
						await calendarDisplayViewModel.SaveDate((!num.HasValue) ? dropDate : (calendarDisplayViewModel.DisplayStartDate?.AddDays(num.Value) ?? dropDate), Window.GetWindow(this));
					}
				}
				else if (UserDao.IsPro())
				{
					needAddEvent = true;
					await checkItemViewModel.SourceViewModel.SetDropDate(dropDate, Window.GetWindow(this));
				}
			}
			else
			{
				needAddEvent = true;
				if (timelineCellViewModel.BatchSelected)
				{
					List<TaskBaseViewModel> tasksByIds = TaskCache.GetTasksByIds(TickDragHelper.DraggingTaskIds?.ToList());
					foreach (TaskBaseViewModel item in tasksByIds)
					{
						await item.SetDropDate(dropDate, Window.GetWindow(this));
					}
				}
				else
				{
					timelineCellViewModel.DisplayModel.SetDropDate(dropDate, Window.GetWindow(this));
				}
			}
		}
		else
		{
			needAddEvent = true;
			if (!displayItemModel.BatchMode)
			{
				await displayItemModel.SourceViewModel.SetDropDate(dropDate, Window.GetWindow(this));
			}
			else
			{
				List<TaskBaseViewModel> tasksByIds2 = TaskCache.GetTasksByIds(displayItemModel.BatchSelectedIds);
				foreach (TaskBaseViewModel item2 in tasksByIds2)
				{
					await item2.SetDropDate(dropDate, Window.GetWindow(this));
				}
			}
		}
		calendar?.CheckItemFiltered(data);
		if (needAddEvent && (calendar?.ParallelShow ?? false))
		{
			string ctype = ((calendar.ViewSourceType == CalendarViewSourceType.WithList) ? "task+calendar" : "calendar+task");
			UserActCollectUtils.AddClickEvent("split_view", ctype, "drag_to_calendar");
		}
	}

	public static T FindAncestor<T>(DependencyObject current) where T : DependencyObject
	{
		while (current != null)
		{
			if (current is T result)
			{
				return result;
			}
			current = VisualTreeHelper.GetParent(current);
		}
		return null;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/calendar/year/yearcalendarcontrol.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 1:
			Root = (YearCalendarControl)target;
			Root.Loaded += Root_Loaded;
			Root.Unloaded += Root_Unloaded;
			Root.SizeChanged += Root_SizeChanged;
			break;
		case 2:
			((Grid)target).PreviewMouseWheel += Grid_PreviewMouseWheel;
			break;
		case 3:
			Mask = (Grid)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
