using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using TickTickUtils;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Service.Display;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Sort;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Habit;
using ticktick_WPF.Views.Project;

namespace ticktick_WPF.Views.Calendar.Agenda;

public class CalendarAgendaView : Grid, INavigate
{
	private readonly CalendarAgendaItemCanvas _itemCanvas;

	private DateTime SelectedDate = DateTime.Today;

	private DateTime _earliest;

	private DateTime _endDateTime;

	private DateTime _startDateTime;

	private DateTime _startDate;

	private DateTime _endDate;

	private Popup _recordPopup;

	private Popup _checkInPopup;

	private bool? _inDark;

	private readonly SemaphoreSlim _loadTaskSemaphoreSlim = new SemaphoreSlim(1, 1);

	private readonly CalendarControl _parentCalendar;

	public DateTime? CurrentDate { get; set; }

	public event EventHandler<(DateTime, DateTime)> DateRangeChanged;

	public CalendarAgendaView(CalendarControl parent)
	{
		_parentCalendar = parent;
		base.Background = Brushes.Transparent;
		base.VerticalAlignment = VerticalAlignment.Stretch;
		base.ClipToBounds = true;
		_itemCanvas = new CalendarAgendaItemCanvas(this);
		base.PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
		base.PreviewMouseWheel += _itemCanvas.OnMouseWheel;
		base.Children.Add(_itemCanvas);
		base.SizeChanged += OnSizeChanged;
	}

	private void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		if (!IsTaskActionSource(e.OriginalSource as DependencyObject))
		{
			CalendarControl parentCalendar = _parentCalendar;
			if (parentCalendar != null && parentCalendar.ClearCopiedTaskSelection())
			{
				e.Handled = true;
			}
		}
	}

	private static bool IsTaskActionSource(DependencyObject source)
	{
		for (DependencyObject dependencyObject = source; dependencyObject != null; dependencyObject = GetParentObject(dependencyObject))
		{
			if (dependencyObject is CalendarAgendaContent || dependencyObject is AgendaCheckIcon)
			{
				return true;
			}
		}
		return false;
	}

	private static DependencyObject GetParentObject(DependencyObject child)
	{
		if (child is Visual || child is Visual3D)
		{
			return VisualTreeHelper.GetParent(child);
		}
		object obj = (child as FrameworkElement)?.Parent;
		if (obj == null)
		{
			FrameworkContentElement obj2 = child as FrameworkContentElement;
			if (obj2 == null)
			{
				return null;
			}
			obj = obj2.Parent;
		}
		return (DependencyObject)obj;
	}

	private void OnSizeChanged(object sender, SizeChangedEventArgs e)
	{
		double num = ((e.NewSize.Width > 1440.0) ? 1030.0 : ((e.NewSize.Width > 970.0) ? (1030.0 - (1440.0 - e.NewSize.Width) * 9.0 / 13.0) : ((e.NewSize.Width < 685.0) ? (e.NewSize.Width - 60.0) : 635.0)));
		if (num > 0.0)
		{
			_itemCanvas.Width = num;
		}
		_itemCanvas.Margin = ((e.NewSize.Width > 1440.0) ? new Thickness(140.0, -384.0, 270.0, 20.0) : ((e.NewSize.Width > 970.0) ? new Thickness(140.0 - (1440.0 - e.NewSize.Width) * 2.0 / 13.0, -384.0, 270.0 - (1440.0 - e.NewSize.Width) * 2.0 / 13.0, 20.0) : new Thickness(30.0, -384.0, 30.0, 20.0)));
		if (Math.Abs(e.NewSize.Height - e.PreviousSize.Height) > 1.0)
		{
			_itemCanvas.DelayReloadItems(e.NewSize.Height);
		}
		_itemCanvas.SetDateTitleVisible(e.NewSize.Width <= 970.0);
	}

	public async Task LoadTask(DateTime dateTime, bool isReload = false)
	{
		_loadTaskSemaphoreSlim.WaitAsync();
		try
		{
			if (!UserDao.IsPro() && (_parentCalendar == null || _parentCalendar.ViewSourceType != CalendarViewSourceType.Widget))
			{
				dateTime = new DateTime(2023, 9, 6);
			}
			_inDark = FindResource("IsDarkTheme") as bool?;
			isReload = isReload && CurrentDate.HasValue;
			if (!isReload)
			{
				this.DateRangeChanged?.Invoke(this, (dateTime, dateTime));
				SelectedDate = dateTime;
				CurrentDate = dateTime;
				_startDate = dateTime.AddDays(-10.0);
				_endDate = dateTime.AddDays(10.0).AddMonths(1);
				_endDateTime = new DateTime(SelectedDate.AddDays(60.0).Year + 1, 1, 1);
				_startDateTime = new DateTime(SelectedDate.AddDays(-60.0).Year, 1, 1);
				_startDate = _startDate.AddDays(1 - _startDate.Day);
				_endDate = _endDate.AddDays(1 - _endDate.Day);
			}
			ProjectExtra projectFilter = ((_parentCalendar != null && _parentCalendar.ViewSourceType == CalendarViewSourceType.WithList) ? LocalSettings.Settings.ExtraSettings.CalendarParallelSelectProject : ProjectExtra.Deserialize(LocalSettings.Settings.CalendarFilterData));
			bool isVirtual = _parentCalendar?.IsVirtual ?? false;
			bool fakeData = (_parentCalendar == null || _parentCalendar.ViewSourceType != CalendarViewSourceType.Widget) && (isVirtual || !UserDao.IsPro());
			List<CalendarDisplayModel> list = ((!fakeData) ? (await CalendarDisplayService.GetDisplayModels(_startDate, _endDate, withCourses: true, withFocus: false, projectFilter)) : CalendarDisplayModel.GetVirtualModels(_startDate, _endDate));
			List<CalendarDisplayModel> tasks = list;
			if (tasks.Count < 100)
			{
				HashSet<(string Id, DateTime? StartDate)> existingKeys = new HashSet<(string, DateTime?)>(tasks.Select((CalendarDisplayModel t) => (Id: t.Id, StartDate: t.StartDate)));
				tasks.AddRange((await LoadTasksAfter(100 - tasks.Count)).Where((CalendarDisplayModel x) => !existingKeys.Contains((x.Id, x.StartDate))));
			}
			List<CalendarAgendaItemViewModel> models = BuildAgendaViewModels(tasks, checkSelectedDate: true, !isVirtual && fakeData);
			if (_endDate == _endDateTime)
			{
				models.Add(CalendarAgendaItemViewModel.BuildDueItem(_endDateTime));
			}
			if (_startDate == _startDateTime)
			{
				models.Insert(0, CalendarAgendaItemViewModel.BuildDueItem(_startDateTime, isDueEnd: false));
			}
			while (base.ActualHeight <= 0.0)
			{
				await Task.Delay(50);
			}
			_itemCanvas.SetItems(models, CurrentDate.Value, base.ActualHeight, isReload);
		}
		finally
		{
			_loadTaskSemaphoreSlim.Release();
		}
	}

	private int SortAgendaViewModel(CalendarAgendaItemViewModel left, CalendarAgendaItemViewModel right)
	{
		return CalendarSortHelper.CompareForAgendaView(left, right, DateTime.Today);
	}

	public async Task LoadMore(bool isEnd, bool nextYear = false)
	{
		_loadTaskSemaphoreSlim.WaitAsync();
		try
		{
			List<CalendarDisplayModel> list;
			if (isEnd)
			{
				if (_endDateTime <= _endDate)
				{
					if (!nextYear)
					{
						return;
					}
					_endDateTime = _endDateTime.AddYears(1);
				}
				list = await LoadTasksAfter(30);
			}
			else
			{
				if (_startDateTime >= _startDate)
				{
					if (!nextYear)
					{
						return;
					}
					_startDateTime = _startDateTime.AddYears(-1);
				}
				list = await LoadTasksBefore(30);
			}
			List<CalendarAgendaItemViewModel> list2 = ((list != null && list.Count != 0) ? BuildAgendaViewModels(list, checkSelectedDate: false) : new List<CalendarAgendaItemViewModel>());
			if (isEnd && _endDate == _endDateTime)
			{
				list2.Add(CalendarAgendaItemViewModel.BuildDueItem(_endDateTime));
			}
			if (!isEnd && _startDate == _startDateTime)
			{
				list2.Insert(0, CalendarAgendaItemViewModel.BuildDueItem(_startDateTime, isDueEnd: false));
			}
			await _itemCanvas.AddItems(list2, isEnd);
		}
		finally
		{
			_loadTaskSemaphoreSlim.Release();
		}
	}

	private Dictionary<DateTime, List<CalendarAgendaItemViewModel>> BuildAgendaViewModelDict(List<CalendarDisplayModel> tasks, bool hideVirtualContent = false)
	{
		List<CalendarAgendaItemViewModel> list = tasks.Select((CalendarDisplayModel t) => new CalendarAgendaItemViewModel(t, _inDark, null, hideVirtualContent)).ToList();
		Dictionary<DateTime, List<CalendarAgendaItemViewModel>> dictionary = new Dictionary<DateTime, List<CalendarAgendaItemViewModel>>();
		foreach (CalendarAgendaItemViewModel item in list)
		{
			DateTime? dateTime = item.DisplayStartDate ?? item.SourceViewModel.CompletedTime;
			if (!dateTime.HasValue)
			{
				continue;
			}
			int val = 1;
			if (item.DisplayDueDate.HasValue)
			{
				val = ((item.IsAllDay ?? true) ? (item.DisplayDueDate.Value.Date - dateTime.Value.Date).Days : ((item.DisplayDueDate.Value.AddMinutes(-1.0).Date - dateTime.Value.Date).Days + 1));
			}
			for (int num = 0; num < Math.Max(1, val); num++)
			{
				DateTime dateTime2 = dateTime.Value.Date.AddDays(num);
				if (!(dateTime2 < _startDate) && !(dateTime2 >= _endDate))
				{
					CalendarAgendaItemViewModel calendarAgendaItemViewModel = item.Clone();
					calendarAgendaItemViewModel.Date = dateTime2;
					if (dictionary.ContainsKey(dateTime2))
					{
						dictionary[dateTime2].Add(calendarAgendaItemViewModel);
						continue;
					}
					dictionary[dateTime2] = new List<CalendarAgendaItemViewModel> { calendarAgendaItemViewModel };
				}
			}
		}
		return dictionary;
	}

	private List<CalendarAgendaItemViewModel> BuildAgendaViewModels(List<CalendarDisplayModel> tasks, bool checkSelectedDate, bool hideVirtualContent = false)
	{
		Dictionary<DateTime, List<CalendarAgendaItemViewModel>> dictionary = BuildAgendaViewModelDict(tasks, hideVirtualContent);
		List<DateTime> list = dictionary.Keys.ToList();
		DateTime dateTime = CurrentDate ?? SelectedDate;
		if (checkSelectedDate && !dictionary.ContainsKey(dateTime))
		{
			list.Add(dateTime);
			dictionary[dateTime] = new List<CalendarAgendaItemViewModel>();
		}
		list.Sort();
		List<CalendarAgendaItemViewModel> list2 = new List<CalendarAgendaItemViewModel>();
		if (list.Count > 0)
		{
			DateTime dateTime2 = default(DateTime);
			foreach (DateTime item in list)
			{
				List<CalendarAgendaItemViewModel> list3 = dictionary[item];
				if (list3.Count > 0)
				{
					list3.Sort((CalendarAgendaItemViewModel a, CalendarAgendaItemViewModel b) => SortAgendaViewModel(a, b));
					list3[0].SetTitleDate();
					list3[list3.Count - 1].BotLineVisible = Visibility.Hidden;
				}
				else
				{
					list3.Add(new CalendarAgendaItemViewModel(new CalendarDisplayModel(new TaskBaseViewModel()), _inDark, Utils.GetString((item == DateTime.Today) ? "TodayA1" : "NoTask"))
					{
						BotLineVisible = Visibility.Hidden,
						Date = item
					});
					list3[0].SetTitleDate();
				}
				list3.Insert(0, CalendarAgendaItemViewModel.BuildEmptyItem(item, item.Year != dateTime2.Year || item.Month != dateTime2.Month));
				dateTime2 = item;
				list2.AddRange(list3);
			}
		}
		return list2;
	}

	private async Task<List<CalendarDisplayModel>> LoadTasksBefore(int count)
	{
		List<CalendarDisplayModel> tasks = new List<CalendarDisplayModel>();
		CalendarControl calendarControl = Utils.FindParent<CalendarControl>(this);
		ProjectExtra projectFilter = ((calendarControl != null && calendarControl.ViewSourceType == CalendarViewSourceType.WithList) ? LocalSettings.Settings.ExtraSettings.CalendarParallelSelectProject : ProjectExtra.Deserialize(LocalSettings.Settings.CalendarFilterData));
		while (tasks.Count < count && _startDateTime < _startDate)
		{
			tasks.AddRange(await CalendarDisplayService.GetDisplayModels(_startDate.AddMonths(-1), _startDate, withCourses: true, withFocus: false, projectFilter));
			_startDate = _startDate.AddMonths(-1);
			if (_startDate <= _startDateTime)
			{
				break;
			}
		}
		if (_startDate < _startDateTime)
		{
			_startDate = _startDateTime;
		}
		return tasks;
	}

	private async Task<List<CalendarDisplayModel>> LoadTasksAfter(int count)
	{
		List<CalendarDisplayModel> tasks = new List<CalendarDisplayModel>();
		CalendarControl calendarControl = Utils.FindParent<CalendarControl>(this);
		ProjectExtra projectFilter = ((calendarControl != null && calendarControl.ViewSourceType == CalendarViewSourceType.WithList) ? LocalSettings.Settings.ExtraSettings.CalendarParallelSelectProject : ProjectExtra.Deserialize(LocalSettings.Settings.CalendarFilterData));
		while (tasks.Count < count && _endDateTime > _endDate)
		{
			tasks.AddRange(await CalendarDisplayService.GetDisplayModels(_endDate, _endDate.AddMonths(1), withCourses: true, withFocus: false, projectFilter));
			_endDate = _endDate.AddMonths(1);
			if (_endDate >= _endDateTime)
			{
				break;
			}
		}
		if (_endDate > _endDateTime)
		{
			_endDate = _endDateTime;
		}
		return tasks;
	}

	public DateTime Today()
	{
		LoadTask(DateTime.Today);
		return SelectedDate;
	}

	public DateTime Next()
	{
		LoadTask(SelectedDate.AddDays(1.0));
		return SelectedDate;
	}

	public DateTime Last()
	{
		LoadTask(SelectedDate.AddDays(-1.0));
		return SelectedDate;
	}

	public DateTime GoTo(DateTime dateTime, bool checkStart = true, bool scrollToNow = false, bool delay = false)
	{
		LoadTask(dateTime);
		return SelectedDate;
	}

	public (DateTime, DateTime) GetTimeSpan()
	{
		return (CurrentDate ?? SelectedDate, CurrentDate ?? SelectedDate);
	}

	public (DateTime, DateTime) GetTaskTimeSpan()
	{
		return (_startDate, _endDate);
	}

	public void Reload(bool byThemeChange = false)
	{
		LoadTask(CurrentDate ?? SelectedDate, isReload: true);
	}

	public void ShowAddWindow(DateTime dateTime)
	{
	}

	public int ExistTasks(HashSet<string> taskIds)
	{
		return _itemCanvas.GetTasksByIds(taskIds)?.Count ?? 0;
	}

	public void RemoveItem(CalendarDisplayViewModel model)
	{
	}

	public void SetCurrentDate(DateTime date)
	{
		CurrentDate = date;
		this.DateRangeChanged?.Invoke(this, (date, date));
	}

	public async void ShowRecordPopup(UIElement element, double step, string unit)
	{
		if (_recordPopup != null)
		{
			_recordPopup.BeginAnimation(Popup.VerticalOffsetProperty, null);
			_recordPopup.IsOpen = false;
			_recordPopup = null;
		}
		if (element != null)
		{
			Point point = element.TranslatePoint(new Point(0.0, 0.0), this);
			Popup popup = new Popup
			{
				Placement = PlacementMode.Relative,
				VerticalOffset = point.Y - 20.0,
				StaysOpen = true,
				HorizontalOffset = point.X - 14.0,
				AllowsTransparency = true,
				Opacity = 0.0
			};
			popup.PlacementTarget = this;
			StackPanel stackPanel = new StackPanel();
			Border border = new Border
			{
				Height = 30.0,
				CornerRadius = new CornerRadius(4.0),
				Effect = new DropShadowEffect
				{
					BlurRadius = 10.0,
					Direction = 300.0,
					ShadowDepth = 2.0,
					Opacity = 0.2
				}
			};
			border.SetResourceReference(Panel.BackgroundProperty, "PrimaryColor");
			border.Child = new TextBlock
			{
				FontSize = 13.0,
				Foreground = Brushes.White,
				Opacity = 0.8,
				VerticalAlignment = VerticalAlignment.Center,
				TextTrimming = TextTrimming.CharacterEllipsis,
				HorizontalAlignment = HorizontalAlignment.Center,
				Margin = new Thickness(8.0, 0.0, 8.0, 0.0),
				Text = "+" + step + " " + HabitUtils.GetUnitText(unit)
			};
			stackPanel.Children.Add(border);
			Path path = UiUtils.CreatePath("IcHabitAddStep", "PrimaryColor", "");
			path.Stretch = Stretch.None;
			path.HorizontalAlignment = HorizontalAlignment.Center;
			path.Margin = new Thickness(0.0, -2.0, 0.0, 0.0);
			stackPanel.Children.Add(path);
			popup.Child = stackPanel;
			popup.IsOpen = true;
			_recordPopup = popup;
			BeginRecordAnimation(popup);
		}
	}

	private async void BeginRecordAnimation(Popup popup)
	{
		Storyboard storyboard = new Storyboard();
		popup.BeginAnimation(UIElement.OpacityProperty, AnimationUtils.GetDoubleAnimation(0.0, 1.0, 100));
		DoubleAnimation doubleAnimation = AnimationUtils.GetDoubleAnimation(1.0, 0.0, 800);
		doubleAnimation.BeginTime = TimeSpan.FromMilliseconds(600.0);
		DoubleAnimation doubleAnimation2 = AnimationUtils.GetDoubleAnimation(popup.VerticalOffset, popup.VerticalOffset - 50.0, 1000);
		doubleAnimation2.BeginTime = TimeSpan.FromMilliseconds(300.0);
		storyboard.Children.Add(doubleAnimation);
		storyboard.Children.Add(doubleAnimation2);
		Storyboard.SetTarget(doubleAnimation, popup.Child);
		Storyboard.SetTarget(doubleAnimation2, popup);
		Storyboard.SetTargetProperty(doubleAnimation, new PropertyPath(UIElement.OpacityProperty));
		Storyboard.SetTargetProperty(doubleAnimation2, new PropertyPath(Popup.VerticalOffsetProperty));
		storyboard.Completed += delegate
		{
			popup.IsOpen = false;
			_recordPopup = null;
		};
		storyboard.Begin();
	}

	public void ManuallyRecordCheckIn(FrameworkElement element, HabitModel model, EventHandler<double> action)
	{
		if (_checkInPopup != null)
		{
			_checkInPopup.IsOpen = false;
			return;
		}
		_checkInPopup = new Popup
		{
			PlacementTarget = element,
			Placement = PlacementMode.Top,
			VerticalOffset = 8.0,
			StaysOpen = false,
			AllowsTransparency = true
		};
		_checkInPopup.Closed += delegate
		{
			_checkInPopup = null;
		};
		ManualRecordCheckinControl manualRecordCheckinControl = new ManualRecordCheckinControl();
		_checkInPopup.Child = manualRecordCheckinControl;
		_checkInPopup.IsOpen = true;
		_checkInPopup.Focus();
		manualRecordCheckinControl.Init(model.Unit);
		manualRecordCheckinControl.Save += delegate(object o, double e)
		{
			action(o, e);
			if (_checkInPopup != null)
			{
				_checkInPopup.IsOpen = false;
			}
		};
		manualRecordCheckinControl.Cancel += delegate
		{
			if (_checkInPopup != null)
			{
				_checkInPopup.IsOpen = false;
			}
		};
	}

	public async void CheckTaskDisplay(string taskId, bool isAdd)
	{
		await Task.Delay(240);
		if (_itemCanvas.GetShowingTask(taskId) == null)
		{
			string text = Utils.GetString(isAdd ? "Added" : "TimeChanged");
			Utils.GetToastWindow()?.Toast(new MoveToastControl(text, delegate
			{
				_itemCanvas.MoveToTask(taskId);
			}));
		}
	}

	public void LoadVirtualModels()
	{
		LoadTask(Utils.GetWeekStart(new DateTime(2023, 9, 6)));
	}
}
