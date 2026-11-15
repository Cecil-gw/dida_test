using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
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
using ticktick_WPF.Views.Detail;
using ticktick_WPF.Views.Print;
using ticktick_WPF.Views.Project;

namespace ticktick_WPF.Views.Calendar.Month;

public class MultiWeekControl : UserControl, INavigate, IDragBarEvent, IComponentConnector
{
	private CalendarControl _calendarControl;

	private string _copyId;

	private MultiWeekDayControl _dayControl;

	private CalendarDisplayViewModel _draggingModel;

	private DateTime? _selectedFirstDate;

	private int _maxCount;

	private Window _parentWindow;

	private List<WeekEventModel> _rowEvents = new List<WeekEventModel>();

	private List<CalendarDisplayModel> _tasks = new List<CalendarDisplayModel>();

	private List<MultiWeekWeekControl> _weekControls = new List<MultiWeekWeekControl>();

	private Dictionary<DateTime, List<WeekEventModel>> _rowData;

	private bool _dragFromArrange;

	private MonthGridViewModel _model;

	private bool _isMultiWeek;

	private bool _touchScrolling;

	private DateTime _lastScrollTime;

	private string _uid;

	private bool _upScroll;

	private DateTime _currentMonth;

	private string _mode;

	private bool _loading;

	private bool _needReload;

	private MultiWeekDayControl _dragOverGrid;

	private bool _autoHScroll;

	private DateTime? _dragSideDate;

	private bool _barSideDragging;

	private double _mouseScrollLeft;

	internal MultiWeekControl Root;

	internal WeekDayNameView DayNameView;

	internal Grid WeekPanel;

	internal Popup DragBarPopup;

	internal Grid PopTriangle;

	internal TaskDragBar DragBar;

	private bool _contentLoaded;

	public int DisplayWeeks { get; set; } = 5;

	public double WeekHeight { get; set; }

	public event EventHandler<(DateTime, DateTime)> DateRangeChanged;

	public MultiWeekControl()
	{
		InitializeComponent();
		base.Loaded += delegate
		{
			BindEvents();
		};
		base.Unloaded += delegate
		{
			UnbindEvents();
		};
		SetGridModel(DateTime.Today);
		base.Margin = new Thickness(1.0, 0.0, 0.0, 0.0);
	}

	public void SetWeeks(int weeks, bool isMultiWeek)
	{
		DisplayWeeks = weeks;
		_isMultiWeek = isMultiWeek;
		if (!(base.ActualHeight < 42.0) && weeks >= 1)
		{
			WeekHeight = Math.Max((base.ActualHeight - 33.0) / (double)weeks, 0.0);
			SetWeekViews();
		}
	}

	private void SetWeekViews()
	{
		int num = DisplayWeeks + 2;
		double weekHeight = WeekHeight;
		if (num > _weekControls.Count)
		{
			int count = _weekControls.Count;
			for (int i = 0; i < num - count; i++)
			{
				MultiWeekWeekControl multiWeekWeekControl = new MultiWeekWeekControl
				{
					VerticalAlignment = VerticalAlignment.Top
				};
				_weekControls.Add(multiWeekWeekControl);
				WeekPanel.Children.Add(multiWeekWeekControl);
			}
		}
		else
		{
			List<MultiWeekWeekControl> list = new List<MultiWeekWeekControl>();
			for (int j = num; j < _weekControls.Count; j++)
			{
				list.Add(_weekControls[j]);
			}
			list.ForEach(delegate(MultiWeekWeekControl d)
			{
				_weekControls.Remove(d);
				WeekPanel.Children.Remove(d);
			});
		}
		for (int num2 = 0; num2 < _weekControls.Count; num2++)
		{
			MultiWeekWeekControl multiWeekWeekControl2 = _weekControls[num2];
			multiWeekWeekControl2.RenderTransform = new TranslateTransform
			{
				Y = (double)(num2 - 1) * weekHeight
			};
			multiWeekWeekControl2.Height = weekHeight;
			multiWeekWeekControl2.Offset = (double)(num2 - 1) * weekHeight;
		}
	}

	public bool OnSelection()
	{
		CalendarControl parent = GetParent();
		if (parent != null && parent.IsLocked)
		{
			return false;
		}
		if (_dayControl != null)
		{
			KeyValuePair<DateTime, DateTime> selectDateSpan = GetSelectDateSpan();
			_dayControl.ShowAddTaskPopup(null, selectDateSpan.Key, selectDateSpan.Value);
			return true;
		}
		return false;
	}

	public void SetBarDragging(bool b)
	{
		_barSideDragging = b;
		if (!b)
		{
			StopAutoVerticalScroll();
			_dragSideDate = null;
		}
	}

	public bool GetBarDragging()
	{
		return _barSideDragging;
	}

	public DateTime? DragSideOver(MouseEventArgs e, CalendarDisplayViewModel model, bool dragLeft)
	{
		Point position = e.GetPosition(WeekPanel);
		DateTime? dateTime = GetHoverGrid(position)?.GetDate();
		if ((position.Y > 0.0 && position.Y < 24.0) || (position.Y > WeekPanel.ActualHeight - 24.0 && position.Y < WeekPanel.ActualHeight))
		{
			StartAutoVerticalScroll(position.Y < 24.0);
		}
		else
		{
			StopAutoVerticalScroll();
		}
		if (!dateTime.HasValue)
		{
			StopAutoVerticalScroll();
		}
		if (dateTime.HasValue && dateTime != _dragSideDate)
		{
			_dragSideDate = dateTime;
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
			SetDragSideBar(model, dateTime2.Date, dateTime3.Date, dragLeft);
		}
		return _dragSideDate;
	}

	private MultiWeekDayControl GetHoverGrid(Point position)
	{
		double actualHeight = WeekPanel.ActualHeight;
		if (DisplayWeeks <= 0 || !(actualHeight > 0.0) || double.IsInfinity(actualHeight) || !(position.Y >= 0.0) || !(position.Y < actualHeight))
		{
			return null;
		}
		double num = actualHeight / (double)DisplayWeeks;
		double num2 = Math.Floor(position.Y / num) + 1.0;
		if (num2 >= 0.0 && num2 < (double)_weekControls.Count)
		{
			return _weekControls[(int)num2].GetHoverGrid(position.X);
		}
		return null;
	}

	private void SetDragSideBar(CalendarDisplayViewModel model, DateTime start, DateTime end, bool dragLeft)
	{
		foreach (MultiWeekWeekControl weekControl in _weekControls)
		{
			if (DateUtils.DatesBetween(weekControl.StartDate, weekControl.StartDate.AddDays(7.0), start, end))
			{
				weekControl.TryAddItem(model, start, end, dragLeft);
			}
			else
			{
				weekControl.RemoveItem(model);
			}
		}
	}

	public DateTime GoTo(DateTime dateTime, bool checkStart = true, bool isScrollToNow = false, bool delay = false)
	{
		if (DisplayWeeks >= 5 && checkStart)
		{
			dateTime = DateUtils.GetMonthStart(dateTime);
			DateTime dateTime2 = dateTime.AddMonths(1);
			dateTime = Utils.GetWeekStart(dateTime);
			int num = (_isMultiWeek ? DisplayWeeks : (((dateTime2 - dateTime).TotalDays > 35.0) ? 6 : 5));
			if (num != DisplayWeeks)
			{
				SetWeeks(num, isMultiWeek: false);
			}
		}
		else
		{
			dateTime = Utils.GetWeekStart(dateTime);
		}
		Load(dateTime, delay);
		HabitSyncService.SyncHabitCheckInCalendar(dateTime);
		return dateTime;
	}

	private void TryReload()
	{
		if ((!TickDragHelper.Dragging || TickDragHelper.Dropped) && !_barSideDragging && base.IsVisible)
		{
			Load(null);
		}
	}

	public void Reload(bool byThemeChange = false)
	{
		if (string.IsNullOrEmpty(_uid))
		{
			_uid = Utils.GetGuid();
		}
		DelayActionHandlerCenter.TryDoAction(_uid, delegate
		{
			base.Dispatcher?.Invoke(TryReload);
		}, _loading ? 200 : 10);
	}

	public DateTime Today()
	{
		DateTime today = DateTime.Today;
		today = Utils.GetWeekStart((_isMultiWeek && DisplayWeeks < 5) ? today : DateUtils.GetMonthStart(today));
		if (today.AddDays(DisplayWeeks * 7) <= DateTime.Today)
		{
			today = today.AddDays(7.0);
		}
		Load(today);
		HabitSyncService.SyncHabitCheckInCalendar(today);
		return today;
	}

	public DateTime Next()
	{
		if (_isMultiWeek)
		{
			return GoTo(GetStartDate().AddDays(DisplayWeeks * 7), checkStart: false);
		}
		return GoTo(GetCurrentMonthDate().AddMonths(1));
	}

	public DateTime Last()
	{
		if (_isMultiWeek)
		{
			return GoTo(GetStartDate().AddDays(DisplayWeeks * -7), checkStart: false);
		}
		return GoTo(GetCurrentMonthDate().AddMonths(-1));
	}

	public async void NextWeek()
	{
		_touchScrolling = true;
		double diff = WeekHeight;
		diff /= 12.0;
		int times = 12;
		while (times > 0)
		{
			foreach (MultiWeekWeekControl weekControl in _weekControls)
			{
				if (weekControl.RenderTransform is TranslateTransform translateTransform)
				{
					weekControl.Offset = (translateTransform.Y -= diff);
				}
			}
			times--;
			await Task.Delay(10);
		}
		MultiWeekWeekControl item = _weekControls[0];
		_weekControls.Remove(item);
		_weekControls.Add(item);
		for (int i = 0; i < _weekControls.Count; i++)
		{
			MultiWeekWeekControl multiWeekWeekControl = _weekControls[i];
			if (multiWeekWeekControl.RenderTransform is TranslateTransform translateTransform2)
			{
				translateTransform2.Y = (double)(i - 1) * WeekHeight;
				multiWeekWeekControl.Offset = (double)(i - 1) * WeekHeight;
			}
		}
		LoadOnScroll(_model.StartDate.AddDays(7.0), isNext: true);
		_touchScrolling = false;
		DelayActionHandlerCenter.TryDoAction("CalendarInputScroll", TryStopTouchScroll, 50);
	}

	public async void LastWeek()
	{
		_touchScrolling = true;
		double diff = -1.0 * WeekHeight;
		diff /= 12.0;
		int times = 12;
		while (times > 0)
		{
			for (int i = 0; i < _weekControls.Count; i++)
			{
				MultiWeekWeekControl multiWeekWeekControl = _weekControls[i];
				if (multiWeekWeekControl.RenderTransform is TranslateTransform translateTransform)
				{
					multiWeekWeekControl.Offset = (translateTransform.Y -= diff);
				}
			}
			times--;
			await Task.Delay(10);
		}
		MultiWeekWeekControl item = _weekControls[_weekControls.Count - 1];
		_weekControls.Remove(item);
		_weekControls.Insert(0, item);
		for (int j = 0; j < _weekControls.Count; j++)
		{
			MultiWeekWeekControl multiWeekWeekControl2 = _weekControls[j];
			if (multiWeekWeekControl2.RenderTransform is TranslateTransform translateTransform2)
			{
				translateTransform2.Y = (double)(j - 1) * WeekHeight;
				multiWeekWeekControl2.Offset = (double)(j - 1) * WeekHeight;
			}
		}
		LoadOnScroll(_model.StartDate.AddDays(-7.0), isNext: false);
		_touchScrolling = false;
		DelayActionHandlerCenter.TryDoAction("CalendarInputScroll", TryStopTouchScroll, 50);
	}

	public CalendarControl GetParent()
	{
		_calendarControl = _calendarControl ?? Utils.FindParent<CalendarControl>(this);
		return _calendarControl;
	}

	private Window GetParentWindow()
	{
		_parentWindow = _parentWindow ?? Utils.GetParentWindow(this);
		return _parentWindow;
	}

	private void UnbindEvents()
	{
		CalendarEventChangeNotifier.TitleChanged -= NotifyEventTitleChanged;
		LocalSettings.Settings.PropertyChanged -= OnSettingsChanged;
		TickDragHelper.DragDrop -= OnDragDrop;
		TickDragHelper.DragMoving -= OnDragOver;
	}

	private void BindEvents()
	{
		CalendarEventChangeNotifier.TitleChanged += NotifyEventTitleChanged;
		LocalSettings.Settings.PropertyChanged += OnSettingsChanged;
		TickDragHelper.DragDrop += OnDragDrop;
		TickDragHelper.DragMoving += OnDragOver;
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
		Point position = e.GetPosition(WeekPanel);
		if (position.X < 0.0 || position.X > WeekPanel.ActualWidth || position.Y < 0.0 || position.Y > WeekPanel.ActualHeight)
		{
			_dragOverGrid?.OnDragLeave();
			_dragOverGrid = null;
			ClearDragBar();
			TickDragHelper.SetShowDragWindow(show: true);
			TickDragHelper.RemoveDragOverCalendar();
			StopAutoVerticalScroll();
			return;
		}
		if (position.Y < 10.0 || position.Y > base.ActualHeight - 10.0)
		{
			StartAutoVerticalScroll(position.Y < 10.0);
		}
		else
		{
			StopAutoVerticalScroll();
		}
		MultiWeekDayControl hoverGrid = GetHoverGrid(position);
		if (hoverGrid == null)
		{
			StopAutoVerticalScroll();
		}
		if (_dragOverGrid != hoverGrid)
		{
			_dragOverGrid?.OnDragLeave();
			hoverGrid?.OnDragEnter();
			_dragOverGrid = hoverGrid;
		}
	}

	private async void OnDragDrop(object sender, MouseEventArgs e)
	{
		if (_dragOverGrid != null)
		{
			await _dragOverGrid.OnDragDrop();
			ClearDropTargets();
			_dragOverGrid = null;
		}
		else
		{
			ClearDragAfterDrop();
		}
		StopAutoVerticalScroll();
	}

	private void StopAutoVerticalScroll()
	{
		_autoHScroll = false;
	}

	private async void StartAutoVerticalScroll(bool isTop)
	{
		if (_autoHScroll)
		{
			return;
		}
		_autoHScroll = true;
		await Task.Delay(200);
		bool scroll = false;
		while (_autoHScroll)
		{
			scroll = true;
			double offset = WeekHeight / (double)(isTop ? 20 : (-20));
			for (int i = 0; i < 20; i++)
			{
				await Task.Delay(10);
				TouchScroll(offset);
			}
			await Task.Delay(50);
		}
		if (scroll)
		{
			OnStopTouchScroll();
		}
	}

	private void OnSettingsChanged(object sender, PropertyChangedEventArgs e)
	{
		if (base.IsVisible && _model != null && (e.PropertyName == "StartWeekOfYear" || e.PropertyName == "EnableLunar" || e.PropertyName == "ShowWeek" || e.PropertyName == "EnableHoliday"))
		{
			SetGridModel(_model.StartDate);
		}
	}

	private void NotifyEventTitleChanged(object sender, TextExtra extra)
	{
		if (base.Visibility != Visibility.Visible)
		{
			return;
		}
		foreach (CalendarDisplayModel item in _tasks.Where((CalendarDisplayModel task) => task.Id == extra.Id))
		{
			item.SourceViewModel.Title = extra.Text;
		}
		_rowEvents.Where((WeekEventModel m) => m.Data.EventId == extra.Id).ToList().ForEach(delegate(WeekEventModel model)
		{
			model.Data.SourceViewModel.Title = extra.Text;
		});
	}

	public void ShowAddWindow(DateTime dateTime)
	{
		_selectedFirstDate = dateTime;
		foreach (MultiWeekWeekControl weekControl in _weekControls)
		{
			weekControl.TryShowAddWindow(dateTime);
		}
	}

	public int ExistTasks(HashSet<string> taskIds)
	{
		return (_tasks?.Where((CalendarDisplayModel model) => taskIds.Contains(model.Id)).ToList() ?? new List<CalendarDisplayModel>()).Count;
	}

	public void RemoveItem(CalendarDisplayViewModel model)
	{
		if (model != null)
		{
			_tasks.RemoveAll((CalendarDisplayModel m) => m.Id == model.SourceViewModel.Id && Math.Abs(m.repeatDiff - model.RepeatDiff) < 0.1);
			SetupEvents();
		}
	}

	public void RemoveTaskById(string taskId)
	{
		if (!string.IsNullOrEmpty(taskId))
		{
			int num = _tasks?.RemoveAll((CalendarDisplayModel model) => model.Id == taskId) ?? 0;
			bool flag = _rowEvents?.Any((WeekEventModel model) => model.Data?.TaskId == taskId) ?? false;
			if (num > 0 || flag)
			{
				SetupEvents();
			}
		}
	}

	public (DateTime, DateTime) GetTimeSpan()
	{
		MonthGridViewModel model = GetModel();
		if (model != null)
		{
			return (model.StartDate, model.EndDate);
		}
		return (DateTime.Today, DateTime.Today);
	}

	public (DateTime, DateTime) GetTaskTimeSpan()
	{
		MonthGridViewModel model = GetModel();
		if (model != null)
		{
			return (model.StartDate, model.EndDate);
		}
		return (DateTime.Today, DateTime.Today);
	}

	private async Task Load(DateTime? startDate, bool delay = false)
	{
		if (PopupStateManager.IsInAdd() || _touchScrolling)
		{
			await Task.Delay(500);
			if (PopupStateManager.IsInAdd() || _touchScrolling)
			{
				return;
			}
		}
		startDate = startDate ?? GetModel()?.StartDate ?? DateTime.Today;
		DayNameView.ResetColumns();
		DateTime? obj = _model?.StartDate;
		SetGridModel(startDate.Value);
		bool flag = obj != _model.StartDate;
		if (_loading && !flag)
		{
			if (!delay)
			{
				_needReload = true;
			}
			return;
		}
		_needReload = false;
		_loading = true;
		UpdateLayout();
		if (delay)
		{
			await Task.Delay(100);
		}
		try
		{
			this.DateRangeChanged?.Invoke(this, (_model.StartDate, _model.EndDate));
			_currentMonth = DateUtils.GetCurrentMonthDate(_model.StartDate, _model.EndDate);
			if (GetParent()?.IsVirtual ?? false)
			{
				LoadVirtualModels();
				return;
			}
			await LoadRowData();
			if (!TickDragHelper.Dragging)
			{
				ClearDragBar();
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

	private void LoadOnScroll(DateTime startDate, bool isNext)
	{
		SetGridModel(startDate);
		this.DateRangeChanged?.Invoke(this, (_model.StartDate, _model.EndDate));
		DateTime currentMonthDate = DateUtils.GetCurrentMonthDate(_model.StartDate, _model.EndDate);
		foreach (MultiWeekWeekControl weekControl in _weekControls)
		{
			weekControl.SetCurrentMonth(currentMonthDate);
		}
		if (_mode == "0" && _currentMonth != currentMonthDate)
		{
			_currentMonth = currentMonthDate;
			DelayActionHandlerCenter.TryDoAction("AddMonthScrollAction", delegate
			{
				UserActCollectUtils.AddClickEvent("calendar", "month_action", "swipe_vertical");
			}, 3000);
		}
		DateTime weekStart = startDate.AddDays(isNext ? (DisplayWeeks * 7) : (-7));
		SetupWeekEvents(_rowData, weekStart);
	}

	private void SetGridModel(DateTime startDate)
	{
		CalendarControl parent = GetParent();
		bool flag = (parent == null || parent.ViewSourceType != CalendarViewSourceType.Widget) && (!UserDao.IsPro() || (parent?.IsVirtual ?? false));
		MonthGridViewModel monthGridViewModel = MonthGridViewModel.Build(flag ? Utils.GetWeekStart(new DateTime(2023, 9, (DisplayWeeks != 2) ? 1 : 6)) : startDate, (DisplayWeeks > 5 && flag) ? 5 : DisplayWeeks);
		if (flag)
		{
			MonthDayViewModel monthDayViewModel = monthGridViewModel.Models.FirstOrDefault((MonthDayViewModel m) => m.Date == new DateTime(2023, 9, 6));
			if (monthDayViewModel != null)
			{
				monthDayViewModel.IsToday = true;
			}
		}
		_model = monthGridViewModel;
		SetupRows(monthGridViewModel);
	}

	private async Task LoadRowData()
	{
		await GetTasks();
		SetupEvents();
	}

	private async Task GetTasks()
	{
		MonthGridViewModel model = GetModel();
		CalendarControl calendarControl = Utils.FindParent<CalendarControl>(this);
		ProjectExtra projectFilter = ((calendarControl != null && calendarControl.ViewSourceType == CalendarViewSourceType.WithList) ? LocalSettings.Settings.ExtraSettings.CalendarParallelSelectProject : ProjectExtra.Deserialize(LocalSettings.Settings.CalendarFilterData));
		CalendarControl parent = GetParent();
		List<CalendarDisplayModel> tasks = (((parent == null || parent.ViewSourceType != CalendarViewSourceType.Widget) && !UserDao.IsPro()) ? new List<CalendarDisplayModel>() : (await CalendarDisplayService.GetDisplayModels(model.StartDate.AddDays(-7.0), model.EndDate.AddDays(8.0), withCourses: true, withFocus: false, projectFilter, pullRemote: true, forcePullPomo: true)));
		_tasks = tasks;
		if (_tasks != null && _tasks.Any())
		{
			_tasks = _tasks.OrderBy((CalendarDisplayModel task) => task.Status).ToList();
		}
	}

	public void LoadVirtualModels()
	{
		MonthGridViewModel model = GetModel();
		_tasks = CalendarDisplayModel.GetVirtualModels(model.StartDate, model.EndDate);
		SetupEvents();
	}

	private void SetupEvents()
	{
		if (GetModel() != null)
		{
			int num = GetMaxCount();
			if (num < 0)
			{
				num = 1;
			}
			_rowData = GetRowData(num, isPrint: false);
			SetupEvents(_rowData);
			_rowEvents = _rowData.Values.SelectMany((List<WeekEventModel> m) => m).ToList();
		}
	}

	private List<WeekEventModel> GetRowModels(int depth, bool isPrint, List<CalendarDisplayViewModel> tasks, DateTime date)
	{
		List<WeekEventModel> list = (from calendarDisplayViewModel4 in tasks
			where calendarDisplayViewModel4.Row <= depth || depth < 0
			select new WeekEventModel
			{
				Row = calendarDisplayViewModel4.Row,
				Column = calendarDisplayViewModel4.Column,
				ColumnSpan = calendarDisplayViewModel4.ColumnSpan,
				Data = calendarDisplayViewModel4
			}).ToList();
		if (depth >= 0)
		{
			Dictionary<int, WeekEventModel> dictionary = new Dictionary<int, WeekEventModel>();
			int j;
			for (j = 0; j < 7; j++)
			{
				List<CalendarDisplayViewModel> list2 = (from task in tasks
					where task.Column <= j && task.Column + task.ColumnSpan > j
					orderby task.Row descending
					select task).ToList();
				LoadMoreWindow.TrySetModels(date.AddDays(j), list2);
				CalendarDisplayViewModel calendarDisplayViewModel = list2.FirstOrDefault();
				if (calendarDisplayViewModel == null)
				{
					continue;
				}
				int row = calendarDisplayViewModel.Row;
				if (row < depth)
				{
					continue;
				}
				string text = "+" + (row - depth);
				double num = 0.0;
				CalendarDisplayViewModel lastOne = list2.FirstOrDefault((CalendarDisplayViewModel m) => m.Row == depth);
				if ((lastOne == null || lastOne.ColumnSpan == 1) && row == depth)
				{
					continue;
				}
				if (lastOne != null)
				{
					if (lastOne.ColumnSpan > 1)
					{
						WeekEventModel weekEventModel = list.FirstOrDefault((WeekEventModel v) => v.Data == lastOne);
						if (row == depth)
						{
							if (weekEventModel != null)
							{
								dictionary.Add(j, weekEventModel);
								continue;
							}
							text = "+1";
						}
						else
						{
							text = "+" + (row + 1 - depth);
							list.Remove(weekEventModel);
						}
					}
					else if (isPrint)
					{
						text = "+" + (row + 1 - depth);
						WeekEventModel item = list.FirstOrDefault((WeekEventModel v) => v.Data == lastOne);
						list.Remove(item);
					}
					else
					{
						num = Utils.MeasureString(text, new Typeface(LocalSettings.Settings.FontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal), CalendarGeoHelper.CalTitleFontSize).Width + 8.0;
						lastOne.LoadMoreWidth = num + 1.0;
					}
				}
				CalendarDisplayViewModel calendarDisplayViewModel2 = new CalendarDisplayViewModel
				{
					Column = j,
					Row = depth,
					ColumnSpan = 1,
					IsLoadMore = true,
					LoadMoreWidth = num,
					SourceViewModel = new TaskBaseViewModel
					{
						Type = DisplayType.LoadMore,
						Title = text
					}
				};
				calendarDisplayViewModel2.Children.AddRange(list2);
				list.Add(new WeekEventModel
				{
					Column = j,
					Row = depth,
					ColumnSpan = 1,
					Data = calendarDisplayViewModel2
				});
			}
			if (dictionary.Any())
			{
				foreach (int column in dictionary.Keys)
				{
					WeekEventModel item2 = dictionary[column];
					if (!list.Contains(item2))
					{
						List<CalendarDisplayViewModel> collection = (from task in tasks
							where task.Column <= column && task.Column + task.ColumnSpan > column
							orderby task.Row descending
							select task).ToList();
						CalendarDisplayViewModel calendarDisplayViewModel3 = new CalendarDisplayViewModel
						{
							Column = column,
							Row = depth,
							ColumnSpan = 1,
							IsLoadMore = true,
							LoadMoreWidth = 0.0,
							SourceViewModel = new TaskBaseViewModel
							{
								Type = DisplayType.LoadMore,
								Title = "+1"
							}
						};
						calendarDisplayViewModel3.Children.AddRange(collection);
						list.Add(new WeekEventModel
						{
							Column = column,
							Row = depth,
							ColumnSpan = 1,
							Data = calendarDisplayViewModel3
						});
					}
				}
			}
		}
		return list;
	}

	private Dictionary<DateTime, List<WeekEventModel>> GetRowData(int depth, bool isPrint)
	{
		int num = ((!isPrint) ? 1 : 0);
		Dictionary<DateTime, List<WeekEventModel>> dictionary = new Dictionary<DateTime, List<WeekEventModel>>();
		int displayWeeks = DisplayWeeks;
		for (int i = -1 * num; i < displayWeeks + num; i++)
		{
			DateTime dateTime = GetModel().StartDate.AddDays(i * 7);
			List<CalendarDisplayViewModel> tasks = GridGeoAssembler.AssemblyEvents(_tasks, dateTime).ToList();
			List<WeekEventModel> rowModels = GetRowModels(depth, isPrint, tasks, dateTime);
			dictionary.Add(dateTime, rowModels);
		}
		return dictionary;
	}

	private MonthGridViewModel GetModel()
	{
		if (_model != null)
		{
			return _model;
		}
		DateTime displayStartDate = MonthGridViewModel.GetDisplayStartDate(DateTime.Today);
		bool flag = displayStartDate.AddDays(35.0).Month == DateTime.Today.Month;
		return MonthGridViewModel.Build(displayStartDate, flag ? 6 : 5);
	}

	private int GetMaxCount()
	{
		double num = 0.0;
		if (_weekControls.Count > 0)
		{
			num = _weekControls[0].ActualHeight;
		}
		if (num == 0.0)
		{
			Window window = Window.GetWindow(this);
			int displayWeeks = DisplayWeeks;
			num = ((window != null) ? ((window.ActualHeight - 100.0) / (double)displayWeeks) : 0.0);
		}
		double max = num - 28.0;
		return GetCount(CalendarGeoHelper.TaskBarHeight, max) - 1;
	}

	private static int GetCount(int item, double max)
	{
		for (int i = 1; i < 100; i++)
		{
			if ((double)(i * item + 1) > max)
			{
				return i - 1;
			}
		}
		return 1;
	}

	public KeyValuePair<DateTime, DateTime> GetSelectDateSpan()
	{
		if (_model != null)
		{
			List<MonthDayViewModel> source = (from model in _model.Models
				where model.State == MonthCellState.Selected
				orderby model.Date
				select model).ToList();
			MonthDayViewModel monthDayViewModel = source.FirstOrDefault();
			MonthDayViewModel monthDayViewModel2 = source.LastOrDefault();
			if (monthDayViewModel != null && monthDayViewModel2 != null)
			{
				return new KeyValuePair<DateTime, DateTime>(monthDayViewModel.Date, monthDayViewModel2.Date);
			}
		}
		return default(KeyValuePair<DateTime, DateTime>);
	}

	public void SetSelection(DateTime date, MultiWeekDayControl dayControl)
	{
		if (!_selectedFirstDate.HasValue || _model == null)
		{
			return;
		}
		DateTime start = _selectedFirstDate.Value;
		DateTime end = date;
		if (start > end)
		{
			end = start;
			start = date;
		}
		_model.Models.ForEach(delegate(MonthDayViewModel model)
		{
			if (model.Date >= start.Date && model.Date <= end.Date)
			{
				model.State = MonthCellState.Selected;
			}
			else
			{
				model.State = MonthCellState.Normal;
			}
		});
		_dayControl = dayControl;
	}

	public void ClearSelection()
	{
		if (_selectedFirstDate.HasValue && _model != null)
		{
			_model.Models.Where((MonthDayViewModel model) => model.State != MonthCellState.Normal).ToList().ForEach(delegate(MonthDayViewModel model)
			{
				model.State = MonthCellState.Normal;
			});
			_selectedFirstDate = null;
			_dayControl = null;
		}
	}

	private void SetupRows(MonthGridViewModel model)
	{
		for (int i = 0; i < _weekControls.Count; i++)
		{
			_weekControls[i].SetData(model.ModelDict, i - 1);
		}
	}

	private async Task SetupWeekEvents(Dictionary<DateTime, List<WeekEventModel>> rowData, DateTime weekStart)
	{
		if (rowData == null)
		{
			return;
		}
		foreach (MultiWeekWeekControl item in _weekControls.Where((MultiWeekWeekControl weekControl) => weekControl.StartDate == weekStart))
		{
			if (rowData.TryGetValue(item.StartDate, out var value))
			{
				item.SetEvents(value);
			}
			else
			{
				item.SetEvents(new List<WeekEventModel>());
			}
		}
	}

	private void SetupEvents(Dictionary<DateTime, List<WeekEventModel>> rowData)
	{
		if (rowData == null)
		{
			return;
		}
		for (int i = 0; i < _weekControls.Count; i++)
		{
			MultiWeekWeekControl multiWeekWeekControl = _weekControls[i];
			if (rowData.TryGetValue(multiWeekWeekControl.StartDate, out var value))
			{
				multiWeekWeekControl.SetEvents(value);
			}
			else
			{
				multiWeekWeekControl.SetEvents(new List<WeekEventModel>());
			}
		}
	}

	private DateTime GetStartDate()
	{
		if (_model != null)
		{
			return _model.StartDate;
		}
		return DateTime.Today;
	}

	private DateTime GetCurrentMonthDate()
	{
		if (_model != null)
		{
			return _model.GetCurrentMonthDate();
		}
		return DateTime.Today;
	}

	public void SetFirstDate(DateTime vmDate)
	{
		_selectedFirstDate = vmDate;
	}

	private void OnSizeChanged(object sender, SizeChangedEventArgs e)
	{
		SetWeeks(DisplayWeeks, _isMultiWeek);
		if (Math.Abs(e.NewSize.Height - e.PreviousSize.Height) > 0.0)
		{
			Reload();
		}
	}

	public async void PrintMonthCalendar(bool isAll)
	{
		if (GetModel() != null)
		{
			await GetTasks();
			new PrintPreviewWindow(GetRowData(isAll ? (-1) : 8, isPrint: true).Values.ToList(), GetModel().StartDate).Show();
			SetupEvents();
		}
	}

	private async void OnMouseScroll(object sender, MouseWheelEventArgs e)
	{
		LoadMoreWindow.TryClose();
		if (_touchScrolling)
		{
			DelayActionHandlerCenter.TryDoAction("CalendarInputScroll", TryStopTouchScroll, 100);
			_upScroll = e.Delta < 0;
			TouchScroll(e.Delta);
		}
		else
		{
			if ((DateTime.Now - _lastScrollTime).TotalMilliseconds < 10.0)
			{
				return;
			}
			TaskDetailPopup.TryCloseWindow();
			_lastScrollTime = DateTime.Now;
			foreach (MultiWeekWeekControl weekControl in _weekControls)
			{
				weekControl.ShowMonthText();
			}
			if (!_touchScrolling)
			{
				SmoothMouthScroll(e.Delta);
			}
		}
	}

	private async void SmoothMouthScroll(int eDelta)
	{
		DelayActionHandlerCenter.RemoveAction("CalendarInputScroll");
		if (eDelta >= 0)
		{
			if (eDelta < 3)
			{
				eDelta = 3;
			}
			if (_mouseScrollLeft < 0.0)
			{
				_mouseScrollLeft = ((eDelta > 40) ? WeekHeight : ((double)eDelta));
			}
			else
			{
				_mouseScrollLeft += ((eDelta > 40) ? WeekHeight : ((double)eDelta));
			}
			double delta = _mouseScrollLeft / 325.0;
			double left = _mouseScrollLeft;
			_upScroll = false;
			for (int i = 25; i >= 1; i--)
			{
				if (Math.Abs(_mouseScrollLeft - left) > 0.1)
				{
					return;
				}
				double num = delta * (double)i;
				left = (_mouseScrollLeft -= num);
				TouchScroll(num);
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
				_mouseScrollLeft = ((eDelta < -40) ? (0.0 - WeekHeight) : ((double)eDelta));
			}
			else
			{
				_mouseScrollLeft += ((eDelta < -40) ? (0.0 - WeekHeight) : ((double)eDelta));
			}
			double left = _mouseScrollLeft / 325.0;
			double delta = _mouseScrollLeft;
			_upScroll = true;
			for (int i = 25; i >= 1; i--)
			{
				if (Math.Abs(_mouseScrollLeft - delta) > 0.1)
				{
					return;
				}
				double num2 = left * (double)i;
				delta = (_mouseScrollLeft -= num2);
				TouchScroll(num2);
				await Task.Delay(8);
			}
		}
		DelayActionHandlerCenter.TryDoAction("CalendarInputScroll", TryStopTouchScroll, 50);
	}

	private void TouchScroll(double delta)
	{
		bool flag = false;
		TaskDetailPopup.TryCloseWindow();
		foreach (MultiWeekWeekControl weekControl in _weekControls)
		{
			if (weekControl.RenderTransform is TranslateTransform translateTransform)
			{
				double num = translateTransform.Y + delta;
				if (num <= -1.5 * WeekHeight)
				{
					num += (double)(DisplayWeeks + 2) * WeekHeight;
					flag = true;
					_model.StartDate = _model.StartDate.AddDays(7.0);
					_model.EndDate = _model.EndDate.AddDays(7.0);
					weekControl.AddDays((DisplayWeeks + 2) * 7);
					SetupWeekEvents(_rowData, weekControl.StartDate);
				}
				if (num >= ((double)DisplayWeeks + 0.5) * WeekHeight + 2.0)
				{
					num -= (double)(DisplayWeeks + 2) * WeekHeight;
					flag = true;
					_model.StartDate = _model.StartDate.AddDays(-7.0);
					_model.EndDate = _model.EndDate.AddDays(-7.0);
					weekControl.AddDays((DisplayWeeks + 2) * -7);
					SetupWeekEvents(_rowData, weekControl.StartDate);
				}
				weekControl.Offset = num;
				translateTransform.Y = num;
			}
			weekControl.ShowMonthText();
		}
		if (!flag)
		{
			return;
		}
		_weekControls.Sort((MultiWeekWeekControl a, MultiWeekWeekControl b) => a.Offset.CompareTo(b.Offset));
		this.DateRangeChanged?.Invoke(this, (_model.StartDate, _model.EndDate));
		DateTime currentMonthDate = DateUtils.GetCurrentMonthDate(_model.StartDate, _model.EndDate);
		foreach (MultiWeekWeekControl weekControl2 in _weekControls)
		{
			weekControl2.SetCurrentMonth(currentMonthDate);
		}
		if (_mode == "0" && _currentMonth != currentMonthDate)
		{
			_currentMonth = currentMonthDate;
			DelayActionHandlerCenter.TryDoAction("AddMonthScrollAction", delegate
			{
				UserActCollectUtils.AddClickEvent("calendar", "month_action", "swipe_vertical");
			}, 3000);
		}
	}

	public void ClearItems(bool onlyData = false)
	{
		_rowEvents?.Clear();
		_tasks?.Clear();
	}

	public void OnShowWeekChanged(bool checkWeekStart = false)
	{
		if (_model == null)
		{
			return;
		}
		if (checkWeekStart)
		{
			DateTime startDate = _model.StartDate;
			int num = (startDate.DayOfWeek - LocalSettings.Settings.WeekStartFromType + 7) % 7;
			startDate = startDate.AddDays(-num);
			if (startDate != _model.StartDate)
			{
				SetGridModel(startDate);
				this.DateRangeChanged?.Invoke(this, (_model.StartDate, _model.EndDate));
			}
		}
		DayNameView.ResetColumns(force: true);
		foreach (MultiWeekWeekControl weekControl in _weekControls)
		{
			weekControl.SetColumns(LocalSettings.Settings.ShowCalWeekend ? 7 : 5);
		}
		SetupRows(_model);
		SetupEvents();
	}

	public void OnDoubleFingerTouch()
	{
		_touchScrolling = true;
		DelayActionHandlerCenter.TryDoAction("CalendarInputScroll", TryStopTouchScroll, 50);
	}

	private void TryStopTouchScroll(object sender, EventArgs e)
	{
		base.Dispatcher.Invoke(OnStopTouchScroll);
	}

	private async void OnStopTouchScroll()
	{
		double diff = 0.0;
		if (_weekControls[0].RenderTransform is TranslateTransform translateTransform)
		{
			diff = translateTransform.Y + WeekHeight;
		}
		if (Math.Abs(diff) > 0.1)
		{
			if (Math.Abs(diff) > 12.0)
			{
				diff = ((!(diff < 0.0)) ? (_upScroll ? diff : (diff - WeekHeight)) : (_upScroll ? (diff + WeekHeight) : diff));
			}
			diff /= 6.0;
			int times = 6;
			while (times > 0)
			{
				TouchScroll(-1.0 * diff);
				times--;
				await Task.Delay(10);
			}
		}
		for (int i = 0; i < _weekControls.Count; i++)
		{
			if (_weekControls[i].RenderTransform is TranslateTransform translateTransform2)
			{
				translateTransform2.Y = (double)(i - 1) * WeekHeight;
			}
		}
		_touchScrolling = false;
		Reload();
	}

	public async Task FlashNaviDates(List<DateTime> navDates, bool delay = false)
	{
		if (!base.IsLoaded)
		{
			await Task.Delay(240);
		}
		foreach (MultiWeekWeekControl weekControl in _weekControls)
		{
			weekControl.FlashNaviDates(navDates, delay);
		}
	}

	public void SetMode(string mode)
	{
		_mode = mode;
	}

	public async void AddShowCheckIconUserEvent()
	{
		await Task.Delay(200);
		if (UserDao.IsPro() && LocalSettings.Settings.IsShowCalendarIcon(DisplayType.Task) && base.ActualWidth / (double)DisplayWeeks >= 116.0)
		{
			UserActCollectUtils.AddClickEvent("calendar", "element_show", "task_checkbox");
		}
	}

	public void OnFontSizeChanged()
	{
		SetupEvents();
	}

	public void SetShowIcon()
	{
		foreach (MultiWeekWeekControl weekControl in _weekControls)
		{
			weekControl.SetShowIcon();
		}
	}

	public void SetColor()
	{
		foreach (MultiWeekWeekControl weekControl in _weekControls)
		{
			weekControl.SetColor();
		}
	}

	public void SetItemBatchSelected(BlockingSet<string> batchSelected)
	{
		foreach (MultiWeekWeekControl weekControl in _weekControls)
		{
			weekControl.SetItemBatchSelected(batchSelected);
		}
	}

	public void SetBatchDragging(bool dragging)
	{
		foreach (MultiWeekWeekControl weekControl in _weekControls)
		{
			weekControl.SetBatchDragging(dragging);
		}
	}

	public void SetDragBar(DateTime date, CalendarDisplayViewModel model)
	{
		CalendarControl calendarControl = _calendarControl;
		if (calendarControl == null)
		{
			return;
		}
		DateTime value = date;
		DateTime? dragStartDate = model.DragStartDate;
		int num = (value - dragStartDate)?.Days ?? 0;
		List<string> list = calendarControl.BatchSelected.ToList();
		if (calendarControl.BatchSelected.Count == 0)
		{
			return;
		}
		Dictionary<string, int> batchSelectedRow = calendarControl.BatchSelectedRow;
		foreach (MultiWeekWeekControl weekControl in _weekControls)
		{
			DateTime startDate = weekControl.StartDate;
			List<WeekEventModel> list2 = new List<WeekEventModel>();
			foreach (string item in list)
			{
				TaskBaseViewModel taskById = TaskCache.GetTaskById(item);
				DateTime? dateTime = taskById.StartDate ?? taskById.CompletedTime?.Date;
				if (!dateTime.HasValue)
				{
					continue;
				}
				int num2 = GridGeoAssembler.GetDateSpan(startDate.Date, dateTime.Value.Date.AddDays(num), !LocalSettings.Settings.ShowCalWeekend);
				int num3 = GridGeoAssembler.GetDateSpan(dateTime.Value.AddDays(num), taskById.DueDate?.AddDays(num), !LocalSettings.Settings.ShowCalWeekend);
				if (num2 >= 7 || num2 + num3 <= 0 || num3 <= 0)
				{
					continue;
				}
				if (num2 < 0)
				{
					num3 += num2;
					num2 = 0;
				}
				if (num3 + num2 > (LocalSettings.Settings.ShowCalWeekend ? 7 : 5))
				{
					num3 = (LocalSettings.Settings.ShowCalWeekend ? 7 : 5) - num2;
				}
				CalendarDisplayViewModel calendarDisplayViewModel = CalendarDisplayViewModel.Build(taskById);
				if (!batchSelectedRow.TryGetValue(taskById.Id, out var value2))
				{
					if (model.TaskId == taskById.Id)
					{
						value2 = model.Row;
					}
					else
					{
						batchSelectedRow[taskById.Id] = 0;
						value2 = 0;
					}
				}
				calendarDisplayViewModel.Row = value2;
				calendarDisplayViewModel.BatchSelected = true;
				calendarDisplayViewModel.ColumnSpan = num3;
				calendarDisplayViewModel.Column = num2;
				list2.Add(new WeekEventModel
				{
					Row = calendarDisplayViewModel.Row,
					ColumnSpan = calendarDisplayViewModel.ColumnSpan,
					Column = calendarDisplayViewModel.Column,
					TargetDate = dateTime.Value,
					Data = calendarDisplayViewModel
				});
			}
			int num4 = GetMaxCount();
			if (num4 < 0)
			{
				num4 = 1;
			}
			Dictionary<int, int> dictionary = new Dictionary<int, int>();
			Dictionary<int, WeekEventModel> dictionary2 = new Dictionary<int, WeekEventModel>();
			Dictionary<int, int> dictionary3 = new Dictionary<int, int>();
			List<WeekEventModel> list3 = new List<WeekEventModel>();
			list2.Sort((WeekEventModel a, WeekEventModel b) => a.Row.CompareTo(b.Row));
			foreach (WeekEventModel item2 in list2)
			{
				if (item2.Row <= num4)
				{
					for (int num5 = item2.Column; num5 < item2.Column + item2.ColumnSpan; num5++)
					{
						dictionary[num5] = item2.Row;
						dictionary2[num5] = item2;
					}
				}
				else
				{
					list3.Add(item2);
				}
			}
			if (list3.Count == 0)
			{
				weekControl.SetDragTasks(list2);
				continue;
			}
			foreach (WeekEventModel item3 in list3)
			{
				int num6 = -1;
				for (int num7 = item3.Column; num7 < item3.Column + item3.ColumnSpan; num7++)
				{
					if (dictionary.TryGetValue(num7, out var value3))
					{
						num6 = Math.Max(num6, value3);
					}
				}
				if (num6 < num4)
				{
					item3.Row = num6 + 1;
					item3.Data.Row = num6 + 1;
					for (int num8 = item3.Column; num8 < item3.Column + item3.ColumnSpan; num8++)
					{
						dictionary[num8] = item3.Row;
						dictionary2[num8] = item3;
					}
					continue;
				}
				list2.Remove(item3);
				for (int num9 = item3.Column; num9 < item3.Column + item3.ColumnSpan; num9++)
				{
					if (dictionary3.ContainsKey(num9))
					{
						dictionary3[num9]++;
					}
					else
					{
						dictionary3[num9] = 1;
					}
				}
			}
			for (int num10 = 0; num10 < 7; num10++)
			{
				if (!dictionary3.TryGetValue(num10, out var value4))
				{
					continue;
				}
				dictionary2.TryGetValue(num10, out var value5);
				if (value5 != null && value5.ColumnSpan > 1)
				{
					list2.Remove(value5);
					for (int num11 = value5.Column; num11 < value5.Column + value5.ColumnSpan; num11++)
					{
						if (dictionary3.ContainsKey(num11))
						{
							dictionary3[num11]++;
						}
						else
						{
							dictionary3[num11] = 1;
						}
						CalendarDisplayViewModel data = new CalendarDisplayViewModel
						{
							Column = num11,
							Row = num4,
							ColumnSpan = 1,
							IsLoadMore = true,
							LoadMoreWidth = 0.0,
							SourceViewModel = new TaskBaseViewModel
							{
								Type = DisplayType.LoadMore,
								Title = "+" + dictionary3[num11]
							}
						};
						list2.Add(new WeekEventModel
						{
							Column = num11,
							Row = num4,
							ColumnSpan = 1,
							Data = data
						});
					}
					num10 = value5.Column + value5.ColumnSpan - 1;
				}
				else
				{
					string text = "+" + value4;
					double num12 = ((value5 == null) ? 0.0 : (Utils.MeasureString(text, new Typeface(LocalSettings.Settings.FontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal), CalendarGeoHelper.CalTitleFontSize).Width + 8.0));
					CalendarDisplayViewModel data2 = new CalendarDisplayViewModel
					{
						Column = num10,
						Row = num4,
						ColumnSpan = 1,
						IsLoadMore = true,
						LoadMoreWidth = num12,
						SourceViewModel = new TaskBaseViewModel
						{
							Type = DisplayType.LoadMore,
							Title = "+" + dictionary3[num10]
						}
					};
					list2.Add(new WeekEventModel
					{
						Column = num10,
						Row = num4,
						ColumnSpan = 1,
						Data = data2
					});
					if (value5 != null)
					{
						value5.Data.LoadMoreWidth = num12 + 1.0;
					}
				}
			}
			weekControl.SetDragTasks(list2);
		}
	}

	private void ClearDragBar()
	{
		foreach (MultiWeekWeekControl weekControl in _weekControls)
		{
			weekControl.SetDragTasks(null);
		}
	}

	public void ClearDragAfterDrop()
	{
		SetBatchDragging(dragging: false);
		ClearDragBar();
	}

	public void SetDropTargets(DateTime start, DateTime end)
	{
		foreach (MultiWeekWeekControl weekControl in _weekControls)
		{
			weekControl.SetDropTargets(start, end);
		}
	}

	private void ClearDropTargets()
	{
		foreach (MultiWeekWeekControl weekControl in _weekControls)
		{
			weekControl.ClearDropTargets();
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/calendar/month/multiweekcontrol.xaml", UriKind.Relative);
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
			Root = (MultiWeekControl)target;
			Root.SizeChanged += OnSizeChanged;
			Root.MouseWheel += OnMouseScroll;
			break;
		case 2:
			DayNameView = (WeekDayNameView)target;
			break;
		case 3:
			WeekPanel = (Grid)target;
			break;
		case 4:
			DragBarPopup = (Popup)target;
			break;
		case 5:
			PopTriangle = (Grid)target;
			break;
		case 6:
			DragBar = (TaskDragBar)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
