using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Service.Display;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Calendar;
using ticktick_WPF.Views.Project;

namespace ticktick_WPF.Views.Time;

public class TickDatePicker : UserControl, IComponentConnector
{
	private const int DayColumns = 7;

	private const int DayRows = 6;

	private const int MonthColumns = 4;

	private const int MonthRows = 3;

	public static readonly DependencyProperty StartDateProperty = DependencyProperty.Register("StartDate", typeof(DateTime), typeof(TickDatePicker), new PropertyMetadata(DateTime.Today, OnStartDateChangeCallback));

	public static readonly DependencyProperty ShowIndicatorProperty = DependencyProperty.Register("ShowIndicator", typeof(bool), typeof(TickDatePicker), new PropertyMetadata(false, OnStartDateChangeCallback));

	public static readonly DependencyProperty CanMultiSelectProperty = DependencyProperty.Register("CanMultiSelect", typeof(bool), typeof(TickDatePicker), new PropertyMetadata(false, OnStartDateChangeCallback));

	public static readonly DependencyProperty MiniModeProperty = DependencyProperty.Register("MiniMode", typeof(bool), typeof(TickDatePicker), new PropertyMetadata(false, OnMiniModeChangeCallback));

	public static readonly DependencyProperty UseInSlideMenuProperty = DependencyProperty.Register("UseInSlideMenu", typeof(bool), typeof(TickDatePicker), new PropertyMetadata(false, OnUseInSlideMenuChangeCallback));

	public static readonly DependencyProperty PrimaryColorProperty = DependencyProperty.Register("PrimaryColor", typeof(SolidColorBrush), typeof(TickDatePicker), new PropertyMetadata(Brushes.Black, null));

	public static readonly DependencyProperty SecondaryColorProperty = DependencyProperty.Register("SecondaryColor", typeof(SolidColorBrush), typeof(TickDatePicker), new PropertyMetadata(Brushes.Black, null));

	private bool _isStartDate;

	private bool _monthMode;

	private double _deltaInDays;

	private DatePickerViewModel _model;

	private DateTime _minDate;

	private DateTime? _maxDate;

	private List<DateTime> _selectedDays = new List<DateTime>();

	private Dictionary<DateTime, bool?> _fixedSelectedDays = new Dictionary<DateTime, bool?>();

	private bool _canClear;

	private DateTime _tabSelectedDate;

	private List<CalendarDisplayModel> _models = new List<CalendarDisplayModel>();

	internal TickDatePicker RootView;

	internal Grid TopGrid;

	internal TextBlock YearMonthButton;

	internal TextBlock YearButton;

	internal Grid NextOrLastGrid;

	internal Ellipse GotoCurrentBt;

	internal Border NextGrid;

	internal Grid MonthCellsGrid;

	internal StackPanel DayGrid;

	internal Grid WeekDaysGrid;

	internal ItemsControl DayCells;

	private bool _contentLoaded;

	public bool IsChooseDate { get; set; }

	private bool CanSwitchNext
	{
		get
		{
			if (_model == null)
			{
				return false;
			}
			DateTime dateTime = _maxDate ?? DateTime.MaxValue;
			if (YearMonthButton.Visibility != Visibility.Visible)
			{
				return _model.PivotDate.Year < dateTime.Year;
			}
			if (_model.PivotDate.Year >= dateTime.Year)
			{
				if (_model.PivotDate.Year == dateTime.Year)
				{
					return _model.PivotDate.Month < dateTime.Month;
				}
				return false;
			}
			return true;
		}
	}

	public DateTime? PiovtDate => _model?.PivotDate;

	private bool CanSwitchBefore
	{
		get
		{
			if (_model?.PivotDate.Year == _minDate.Year)
			{
				return _model?.PivotDate.Month != _minDate.Month;
			}
			return true;
		}
	}

	public bool IsChoosed { get; set; }

	public DateTime? SelectStart => _model.SelectStartDate;

	public DateTime? SelectEnd => _model.SelectEndDate;

	private DateTime PivotDate => _model.PivotDate;

	public bool AllowSelectMonth { get; set; } = true;

	public bool ShowInCal { get; set; }

	public bool ShowSelectedInCal { get; set; }

	public bool ShowIndicator
	{
		get
		{
			return (bool)GetValue(ShowIndicatorProperty);
		}
		set
		{
			SetValue(ShowIndicatorProperty, value);
		}
	}

	public bool CanMultiSelect
	{
		get
		{
			return (bool)GetValue(CanMultiSelectProperty);
		}
		set
		{
			SetValue(CanMultiSelectProperty, value);
		}
	}

	public DateTime? SelectedDate
	{
		get
		{
			return _model.SelectedDate;
		}
		set
		{
			_model.SelectedDate = value;
			ResetOnSelectedChanged();
		}
	}

	public string RepeatFlag { get; set; }

	public string RepeatFrom { get; set; }

	public List<string> ExDates { get; set; }

	public DateTime StartDate
	{
		get
		{
			return (DateTime)GetValue(StartDateProperty);
		}
		set
		{
			SetValue(StartDateProperty, value);
		}
	}

	public bool MiniMode
	{
		get
		{
			return (bool)GetValue(MiniModeProperty);
		}
		set
		{
			SetValue(MiniModeProperty, value);
		}
	}

	public bool UseInSlideMenu
	{
		get
		{
			return (bool)GetValue(UseInSlideMenuProperty);
		}
		set
		{
			SetValue(UseInSlideMenuProperty, value);
		}
	}

	public SolidColorBrush PrimaryColor
	{
		get
		{
			return (SolidColorBrush)GetValue(PrimaryColorProperty);
		}
		set
		{
			SetValue(PrimaryColorProperty, value);
		}
	}

	public SolidColorBrush SecondaryColor
	{
		get
		{
			return (SolidColorBrush)GetValue(SecondaryColorProperty);
		}
		set
		{
			SetValue(SecondaryColorProperty, value);
		}
	}

	public string TimeZone { get; set; }

	public event EventHandler<DateTime> SelectedDateChanged;

	public event EventHandler<DateTime> MonthSelected;

	public event EventHandler<DateTime> DateSelected;

	public event EventHandler<DateTime> StartDateChanged;

	public TickDatePicker()
		: this(null)
	{
	}

	public TickDatePicker(DateTime? selectedDate, DateTime? selectStart = null, DateTime? selectEnd = null, bool isStart = true, bool isMonthMode = false, DateTime? maxDate = null, DateTime? minDate = null)
	{
		InitializeComponent();
		SetData(selectedDate, selectStart, selectEnd, isStart, isMonthMode, maxDate, minDate);
	}

	public void SetData(DateTime? selectedDate, DateTime? selectStart = null, DateTime? selectEnd = null, bool isStart = true, bool isMonthMode = false, DateTime? maxDate = null, DateTime? minDate = null)
	{
		_isStartDate = isStart;
		_monthMode = isMonthMode;
		_maxDate = maxDate;
		_minDate = minDate.GetValueOrDefault();
		InitData(selectedDate, selectStart, selectEnd);
		InitCells();
		SetGotoCurrentEnable();
		PropertyChangedEventManager.AddHandler(LocalSettings.Settings, OnShowLunarChanged, "EnableLunar");
		PropertyChangedEventManager.AddHandler(LocalSettings.Settings, OnShowHolidayChanged, "EnableHoliday");
	}

	private void OnShowLunarChanged(object sender, PropertyChangedEventArgs e)
	{
		Reload();
	}

	private void OnShowHolidayChanged(object sender, PropertyChangedEventArgs e)
	{
		if (!(DayCells.ItemsSource is ObservableCollection<DayCellViewModel> observableCollection))
		{
			return;
		}
		foreach (DayCellViewModel item in observableCollection)
		{
			item.Data?.SetHoliday();
		}
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		if (ShowIndicator)
		{
			LoadTaskIndicator();
		}
		SetResourceReference(PrimaryColorProperty, GetPrimaryTextColor(UseInSlideMenu));
		SetResourceReference(SecondaryColorProperty, GetPrimaryTextColor(UseInSlideMenu));
	}

	private static void OnMiniModeChangeCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is TickDatePicker tickDatePicker && e.NewValue is bool flag)
		{
			tickDatePicker._model.MiniMode = flag;
			tickDatePicker._model.RowCount = (flag ? 1 : 6);
			tickDatePicker.DayCells.Height = 34 * tickDatePicker._model.RowCount;
			tickDatePicker.SetDayCells();
		}
	}

	private static void OnUseInSlideMenuChangeCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is TickDatePicker tickDatePicker && e.NewValue is bool useInSlideMenu)
		{
			tickDatePicker.SetResourceReference(PrimaryColorProperty, GetPrimaryTextColor(useInSlideMenu));
			tickDatePicker.SetResourceReference(SecondaryColorProperty, GetSecondaryTextColor(useInSlideMenu));
			tickDatePicker.SetDayCells();
		}
	}

	private static string GetTertiaryTextColor(bool useInSlideMenu)
	{
		if (!useInSlideMenu)
		{
			return "BaseColorOpacity40";
		}
		return "ProjectMenuColorOpacity40";
	}

	private static string GetSecondaryTextColor(bool useInSlideMenu)
	{
		if (!useInSlideMenu)
		{
			return "BaseColorOpacity60";
		}
		return "ProjectMenuColorOpacity60";
	}

	private static string GetPrimaryTextColor(bool useInSlideMenu)
	{
		if (!useInSlideMenu)
		{
			return "BaseColorOpacity100";
		}
		return "ProjectMenuColorOpacity80";
	}

	private static void OnStartDateChangeCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is TickDatePicker tickDatePicker && e.NewValue is DateTime e2)
		{
			tickDatePicker.StartDateChanged?.Invoke(tickDatePicker, e2);
		}
	}

	public void InitEvents()
	{
		DataChangedNotifier.WeekStartFromChanged -= ReSetDayCells;
		DataChangedNotifier.WeekStartFromChanged += ReSetDayCells;
	}

	private async void ReSetDayCells(object sender, EventArgs e)
	{
		SetDayCells();
	}

	public void SelectRange(DateTime? start, DateTime? end)
	{
		if (!start.HasValue || !end.HasValue)
		{
			_model.SelectStartDate = null;
			_model.SelectEndDate = null;
		}
		else if (start == end)
		{
			_model.SelectedDate = start;
			_model.SelectStartDate = null;
			_model.SelectEndDate = null;
		}
		else
		{
			_model.SelectedDate = null;
			_model.SelectStartDate = start;
			_model.SelectEndDate = end;
		}
		SetDayCells(reloadIndicator: false);
	}

	public void SetInCalMode()
	{
		ShowInCal = true;
		TopGrid.Height = 44.0;
		TopGrid.Margin = new Thickness(0.0, 0.0, 0.0, 5.0);
		YearMonthButton.FontSize = 14.0;
		foreach (UIElement child in WeekDaysGrid.Children)
		{
			if (child is TextBlock textBlock)
			{
				textBlock.FontSize = 12.0;
			}
		}
	}

	public async Task ReloadColor()
	{
		if (!(DayCells.ItemsSource is ObservableCollection<DayCellViewModel> observableCollection))
		{
			return;
		}
		foreach (DayCellViewModel item in observableCollection)
		{
			item.Data?.NotifyColorChanged();
		}
	}

	public async Task LoadTaskIndicator(bool reloadModels = true)
	{
		IEnumerable itemsSource = DayCells.ItemsSource;
		if (!(itemsSource is ObservableCollection<DayCellViewModel> items))
		{
			return;
		}
		if (reloadModels)
		{
			DateTime startDate = StartDate;
			DateTime endDate = AddDaysWithinRange(startDate, 42);
			CalendarControl calendarControl = Utils.FindParent<CalendarControl>(this);
			ProjectExtra projectFilter = ((calendarControl != null && calendarControl.ViewSourceType == CalendarViewSourceType.WithList) ? LocalSettings.Settings.ExtraSettings.CalendarParallelSelectProject : ProjectExtra.Deserialize(LocalSettings.Settings.CalendarFilterData));
			_models = await CalendarDisplayService.GetDisplayModels(startDate, endDate, withCourses: false, withFocus: false, projectFilter, pullRemote: false);
		}
		if (_models == null || !_models.Any())
		{
			return;
		}
		items.ToList().ForEach(delegate(DayCellViewModel dayCellViewModel)
		{
			dayCellViewModel.Data.HasTasks = false;
		});
		foreach (CalendarDisplayModel model in _models.Where((CalendarDisplayModel calendarDisplayModel) => !calendarDisplayModel.SourceViewModel.IsPomo))
		{
			items.Where((DayCellViewModel i) => !i.Data.HasTasks && model.Status == 0 && ((!model.DueDate.HasValue && model.DisplayStartDate.HasValue && model.DisplayStartDate.Value.Date == i.Data.Date) || (model.DisplayStartDate.HasValue && model.DisplayStartDate.Value.Date <= i.Data.Date && model.DisplayDueDate.HasValue && model.DisplayDueDate.Value.Date >= i.Data.Date))).ToList().ForEach(delegate(DayCellViewModel item)
			{
				item.Data.HasTasks = true;
			});
		}
	}

	public List<DateTime> GetSelectedDays()
	{
		return _selectedDays.ToList();
	}

	public void SetSelectedDays(List<DateTime> dayList)
	{
		_selectedDays.Clear();
		dayList.ForEach(delegate(DateTime item)
		{
			_selectedDays.Add(item.Date);
		});
		TryUpdateViewDays();
	}

	public void SetFixedSelectedDay(DateTime time, bool? isSelected)
	{
		if (isSelected.HasValue)
		{
			_fixedSelectedDays[time.Date] = isSelected.Value;
		}
		else
		{
			_fixedSelectedDays.Remove(time.Date);
		}
		TryUpdateViewDays();
	}

	public DateTime? GetHoverDate()
	{
		if (DayCells.ItemsSource is ObservableCollection<DayCellViewModel> source)
		{
			return source.FirstOrDefault((DayCellViewModel item) => item.Data.Hover)?.Data.Date;
		}
		return null;
	}

	public void SetHoverCell(int row, int column)
	{
		if (DayCells.ItemsSource is ObservableCollection<DayCellViewModel> source)
		{
			_canClear = true;
			source.ToList().ForEach(delegate(DayCellViewModel model)
			{
				model.Data.Hover = false;
			});
			DayCellViewModel dayCellViewModel = source.FirstOrDefault((DayCellViewModel item) => item.Row == row && item.Column == column);
			if (dayCellViewModel != null)
			{
				dayCellViewModel.Data.Hover = true;
			}
		}
	}

	public void ClearHoverCell()
	{
		if (!_canClear)
		{
			return;
		}
		if (DayCells.ItemsSource is ObservableCollection<DayCellViewModel> source)
		{
			source.ToList().ForEach(delegate(DayCellViewModel model)
			{
				model.Data.Hover = false;
			});
		}
		_canClear = false;
	}

	private void InitCells()
	{
		if (_monthMode)
		{
			SwitchMonthMode();
		}
		else
		{
			SetDayCells();
		}
	}

	private void InitData(DateTime? selectedDate, DateTime? selectStart, DateTime? selectEnd)
	{
		_model = new DatePickerViewModel
		{
			PivotDate = (selectedDate ?? DateTime.Now.Date),
			SelectedDate = selectedDate,
			SelectStartDate = selectStart,
			SelectEndDate = selectEnd
		};
		InitDelta();
		base.DataContext = _model;
	}

	private void ResetOnSelectedChanged()
	{
		if (!_model.SelectedDate.HasValue)
		{
			return;
		}
		DateTime pivotDate = _model.PivotDate;
		DateTime? selectedDate = _model.SelectedDate;
		if (pivotDate != selectedDate)
		{
			_model.PivotDate = _model.SelectedDate.Value;
		}
		_model.PivotDate = _model.SelectedDate.Value.Date;
		if (DayCells.ItemsSource is ObservableCollection<DayCellViewModel> observableCollection)
		{
			ClearLastSelectedIfNeeds(_model.PivotDate.Date);
			TryUpdateViewDays();
			DateTime monthStartDate = GetMonthStartDate(_model.PivotDate);
			if (observableCollection[0].Data.Date != monthStartDate && !MiniMode)
			{
				InitCells();
			}
			DayCellViewModel dayCellViewModel = ((ObservableCollection<DayCellViewModel>)DayCells.ItemsSource).FirstOrDefault((DayCellViewModel m) => m.Data.Date == _model.PivotDate.Date);
			if (dayCellViewModel != null)
			{
				dayCellViewModel.Data.Selected = true;
			}
		}
	}

	private void TryUpdateViewDays()
	{
		if (!(DayCells.ItemsSource is ObservableCollection<DayCellViewModel> observableCollection))
		{
			return;
		}
		foreach (DayCellViewModel item in observableCollection)
		{
			if (CanMultiSelect && _selectedDays.Contains(item.Data.Date))
			{
				item.Data.Selected = true;
			}
			if (_fixedSelectedDays.Any() && _fixedSelectedDays.ContainsKey(item.Data.Date))
			{
				if (_fixedSelectedDays[item.Data.Date].HasValue)
				{
					item.Data.Selected = _fixedSelectedDays[item.Data.Date].Value;
					item.Data.IsFixed = true;
				}
				else
				{
					item.Data.IsFixed = false;
					_fixedSelectedDays.Remove(item.Data.Date);
				}
			}
		}
	}

	public void Reload()
	{
		SetDayCells();
	}

	public void SetDayCells(bool reloadIndicator = true, bool reset = false)
	{
		DateTime dateTime = GetMonthStartDate(_model.PivotDate);
		if (MiniMode)
		{
			dateTime = Utils.GetWeekStart(DateTime.Today);
		}
		reloadIndicator = reloadIndicator || dateTime != StartDate;
		StartDate = dateTime;
		int num = 0;
		List<DayCellViewModel> list = new List<DayCellViewModel>();
		int weekStartFromType = (int)LocalSettings.Settings.WeekStartFromType;
		DateTime? dateTime2 = null;
		DateTime? dateTime3 = null;
		AlternativeCalendar alternativeCalendar = LocalSettings.Settings.UserPreference?.alternativeCalendar;
		if (alternativeCalendar != null)
		{
			(dateTime2, dateTime3) = CalendarConverter.GetLimitDate(alternativeCalendar.calendar);
		}
		for (int i = 0; i < _model.RowCount; i++)
		{
			for (int j = 0; j < 7; j++)
			{
				int num2 = num++;
				if (num2 > (DateTime.MaxValue.Date - StartDate.Date).Days)
				{
					continue;
				}
				DayViewModel dayViewModel = new DayViewModel(StartDate.AddDays(num2), this);
				InitShowMode(dayViewModel);
				InitSelection(dayViewModel, weekStartFromType);
				InitSelected(dayViewModel);
				if (dateTime2.HasValue)
				{
					DateTime date = dayViewModel.Date;
					DateTime? dateTime4 = dateTime2;
					if (!(date >= dateTime4))
					{
						goto IL_0182;
					}
				}
				if (dateTime3.HasValue)
				{
					DateTime date = dayViewModel.Date;
					DateTime? dateTime4 = dateTime3;
					if (!(date <= dateTime4))
					{
						goto IL_0182;
					}
				}
				InitLunarText(dayViewModel);
				goto IL_0182;
				IL_0182:
				int canSelect;
				if (_maxDate.HasValue)
				{
					DateTime date = dayViewModel.Date.Date;
					DateTime? dateTime4 = _maxDate;
					if (!(date <= dateTime4))
					{
						canSelect = 0;
						goto IL_01e5;
					}
				}
				canSelect = ((dayViewModel.Date.Date >= _minDate) ? 1 : 0);
				goto IL_01e5;
				IL_01e5:
				dayViewModel.CanSelect = (byte)canSelect != 0;
				dayViewModel.CanDoubleSelect = !ShowInCal;
				dayViewModel.UseInSlideMenu = UseInSlideMenu;
				list.Add(new DayCellViewModel
				{
					Row = i,
					Column = j,
					Data = dayViewModel
				});
			}
		}
		if (reset || MiniMode)
		{
			DayCells.ItemsSource = new ObservableCollection<DayCellViewModel>(list);
		}
		else
		{
			ItemsSourceHelper.SetItemsSource(DayCells, list);
		}
		if (ShowIndicator)
		{
			LoadTaskIndicator(reloadIndicator);
		}
		TryUpdateViewDays();
		SetGotoCurrentEnable();
		UpdateNavigationButtons();
	}

	private bool IsInSameWeek(DateTime dtmS, DateTime dtmE)
	{
		double totalDays = (dtmE - dtmS).TotalDays;
		int num = Convert.ToInt32(dtmE.DayOfWeek);
		if (num == 0)
		{
			num = 7;
		}
		if (totalDays >= 7.0 || totalDays >= (double)num)
		{
			return false;
		}
		return true;
	}

	private void SetGotoCurrentEnable()
	{
	}

	private void InitLunarText(DayViewModel day)
	{
		AlternativeCalendar alternativeCalendar = LocalSettings.Settings.UserPreference?.alternativeCalendar;
		if ((!Utils.IsTickPackage() && LocalSettings.Settings.EnableLunar) || (Utils.IsTickPackage() && alternativeCalendar != null && alternativeCalendar.calendar == "lunar"))
		{
			InitChineseLunar(day);
		}
		else if (Utils.IsTickPackage() && alternativeCalendar != null && CalendarConverter.IsValidCalendarType(alternativeCalendar.calendar))
		{
			CalendarDisplay calendarDisplay = new CalendarDisplay(day.Date, alternativeCalendar.calendar);
			if (!calendarDisplay.OutLimit)
			{
				day.LunarMonthFirstDay = calendarDisplay.Day == 1;
				day.LunarText = calendarDisplay.DisplayText();
			}
		}
	}

	private static void InitChineseLunar(DayViewModel day)
	{
		LunarUtils.ChineseCalendar chineseCalendar = new LunarUtils.ChineseCalendar(day.Date);
		if (Utils.IsEmptyDate(chineseCalendar.Date))
		{
			return;
		}
		try
		{
			if (LocalSettings.Settings.EnableHoliday && !string.IsNullOrEmpty(chineseCalendar.ChineseCalendarHoliday) && (day.Date.Month != 10 || day.Date.Day != 1))
			{
				string chineseCalendarHoliday = chineseCalendar.ChineseCalendarHoliday;
				if (chineseCalendarHoliday.Contains("除夕") || chineseCalendarHoliday.Contains("春节") || chineseCalendarHoliday.Contains("中秋") || chineseCalendarHoliday.Contains("端午"))
				{
					day.LunarText = chineseCalendarHoliday;
					return;
				}
			}
			if (!string.IsNullOrEmpty(chineseCalendar.DateHoliday))
			{
				_ = chineseCalendar.DateHoliday;
				day.LunarText = chineseCalendar.DateHoliday;
				return;
			}
			if (!string.IsNullOrEmpty(chineseCalendar.ChineseCalendarHoliday))
			{
				string chineseCalendarHoliday2 = chineseCalendar.ChineseCalendarHoliday;
				day.LunarText = chineseCalendarHoliday2;
				return;
			}
			if (!string.IsNullOrEmpty(chineseCalendar.WeekDayHoliday))
			{
				day.LunarText = chineseCalendar.WeekDayHoliday;
				return;
			}
			if (!string.IsNullOrEmpty(chineseCalendar.ChineseTwentyFourDay))
			{
				string chineseTwentyFourDay = chineseCalendar.ChineseTwentyFourDay;
				if (LocalSettings.Settings.EnableLunar || (LocalSettings.Settings.EnableHoliday && chineseTwentyFourDay.Contains("清明")))
				{
					day.LunarText = chineseTwentyFourDay;
					return;
				}
			}
			day.LunarText = chineseCalendar.ChineseDayString;
			if (chineseCalendar.ChineseDay == 1)
			{
				day.LunarText = chineseCalendar.ChineseMonthString;
				day.LunarMonthFirstDay = true;
			}
		}
		catch (Exception)
		{
		}
	}

	public async Task RenderRepeat()
	{
		DateTime dateTime = ((SelectedDate.HasValue && !Utils.IsEmptyDate(SelectedDate)) ? SelectedDate.Value : DateTime.Today);
		if (DayCells.ItemsSource == null || !(DayCells.ItemsSource is ObservableCollection<DayCellViewModel> observableCollection))
		{
			return;
		}
		DateTime date = observableCollection[0].Data.Date;
		List<DateTime> validRepeatDates = RepeatUtils.GetValidRepeatDates(TaskService.TryFixRepeatFlag(dateTime, RepeatFlag, RepeatFrom, !IsChoosed), RepeatFrom, dateTime, AddDaysWithinRange(date, -1), AddDaysWithinRange(date, 43), ExDates, TimeZone);
		for (int i = 0; i < validRepeatDates.Count; i++)
		{
			validRepeatDates[i] = TimeZoneUtils.LocalToTargetTzTime(validRepeatDates[i], TimeZone);
		}
		foreach (DayCellViewModel item in observableCollection)
		{
			item.Data.IsRepeat = validRepeatDates.Exists((DateTime model) => model.Date.Date == item.Data.Date);
		}
	}

	private void InitSelected(DayViewModel day)
	{
		day.TabBorderThickness = ((day.Date.Date == _tabSelectedDate.Date) ? 1 : 0);
		if (!CanMultiSelect && _model.SelectedDate.HasValue && day.Date.Date == _model.SelectedDate.Value.Date)
		{
			day.Selected = true;
		}
	}

	private void InitDelta()
	{
		if (_model.SelectStartDate.HasValue && _model.SelectEndDate.HasValue)
		{
			_deltaInDays = (_model.SelectEndDate.Value.Date - _model.SelectStartDate.Value.Date).TotalDays;
		}
	}

	private void InitMonthRowsAndColumns()
	{
		for (int i = 0; i < 3; i++)
		{
			MonthCellsGrid.RowDefinitions.Add(new RowDefinition());
		}
		for (int j = 0; j < 4; j++)
		{
			MonthCellsGrid.ColumnDefinitions.Add(new ColumnDefinition());
		}
	}

	private void InitSelection(DayViewModel day, int weekStart)
	{
		if (!_model.SelectStartDate.HasValue || !_model.SelectEndDate.HasValue)
		{
			return;
		}
		DateTime date = _model.SelectStartDate.Value.Date;
		DateTime date2 = _model.SelectEndDate.Value.Date;
		DateTime date3 = day.Date.Date;
		if (date3 >= date.Date && date3 <= date2.Date)
		{
			bool flag = day.Date.DayOfWeek == (DayOfWeek)weekStart;
			bool flag2 = day.Date.DayOfWeek == (DayOfWeek)((weekStart + 6) % 7);
			if (date2 == date)
			{
				day.Selection = SelectionMode.Full;
			}
			else if (date3 == date)
			{
				day.Selection = ((!flag2) ? SelectionMode.Start : SelectionMode.Full);
			}
			else if (date3 == date2)
			{
				day.Selection = (flag ? SelectionMode.Full : SelectionMode.End);
			}
			else if (flag2)
			{
				day.Selection = SelectionMode.End;
			}
			else
			{
				day.Selection = (flag ? SelectionMode.Start : SelectionMode.Middle);
			}
		}
		else
		{
			day.Selection = SelectionMode.None;
		}
	}

	private void InitShowMode(DayViewModel model)
	{
		if (model.Date.Month == _model.PivotDate.Date.Month)
		{
			model.ShowMode = ShowMode.CurrentMonth;
		}
		else if (model.Date.Month < _model.PivotDate.Date.Month)
		{
			model.ShowMode = ShowMode.LastMonth;
		}
		else
		{
			model.ShowMode = ShowMode.NextMonth;
		}
		if (model.Date.Date == DateTime.Now.Date)
		{
			model.ShowMode = ShowMode.Today;
		}
	}

	private void OnDateChanged(DateTime date)
	{
		_model.SelectedDate = date;
		ClearLastSelectedIfNeeds(date);
		ResetOnSelectedChanged();
		TryAdjustSelection(date);
		RenderRepeat();
	}

	private void TryAdjustSelection(DateTime date)
	{
		if (_model.SelectStartDate.HasValue && _model.SelectEndDate.HasValue)
		{
			if (_isStartDate)
			{
				_model.SelectStartDate = DateUtils.SetDateOnly(_model.SelectStartDate.Value, date.Date);
				_model.SelectEndDate = DateUtils.SetDateOnly(_model.SelectEndDate.Value, _model.SelectStartDate.Value);
				_model.SelectEndDate = _model.SelectEndDate.Value.AddDays(_deltaInDays);
			}
			else
			{
				_model.SelectEndDate = DateUtils.SetDateOnly(_model.SelectEndDate.Value, date.Date);
				if (_model.SelectStartDate > date.Date)
				{
					_model.SelectStartDate = DateUtils.SetDateOnly(_model.SelectStartDate.Value, date.Date);
				}
			}
		}
		RenderSelection(date);
	}

	private void RenderSelection(DateTime date)
	{
		if (DayCells.ItemsSource == null || !(DayCells.ItemsSource is ObservableCollection<DayCellViewModel> observableCollection))
		{
			return;
		}
		int weekStartFromType = (int)LocalSettings.Settings.WeekStartFromType;
		foreach (DayCellViewModel item in observableCollection)
		{
			InitSelection(item.Data, weekStartFromType);
		}
	}

	private void ClearLastSelectedIfNeeds(DateTime selectedDate)
	{
		if (CanMultiSelect || DayCells.ItemsSource == null || !(DayCells.ItemsSource is ObservableCollection<DayCellViewModel> observableCollection))
		{
			return;
		}
		foreach (DayCellViewModel item in observableCollection)
		{
			if (item.Data.Selected && item.Data.Date != selectedDate.Date)
			{
				item.Data.Selected = false;
				break;
			}
		}
	}

	private DateTime GetMonthStartDate(DateTime date)
	{
		if (Utils.IsEmptyDate(date))
		{
			return DateTime.Today;
		}
		DateTime weekStart = Utils.GetWeekStart(date.AddDays(1 - date.Day));
		if (weekStart.Month == PivotDate.Month && weekStart.Day != 1)
		{
			return weekStart.AddDays(-7.0);
		}
		return weekStart;
	}

	private DateTime InitStartMonth()
	{
		return _model.PivotDate.AddDays(_model.PivotDate.DayOfYear * -1 + 1);
	}

	private void OnClick(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
	}

	private void LastClick(object sender, MouseButtonEventArgs e)
	{
		NextMonthOrYear(-1);
	}

	private void NextClick(object sender, MouseButtonEventArgs e)
	{
		NextMonthOrYear(1);
	}

	private static DateTime AddDaysWithinRange(DateTime date, int days)
	{
		if (days > (DateTime.MaxValue - date).Days)
		{
			return DateTime.MaxValue;
		}
		if (days < -(date - DateTime.MinValue).Days)
		{
			return DateTime.MinValue;
		}
		return date.AddDays(days);
	}

	private void UpdateNavigationButtons()
	{
		NextGrid.MouseLeftButtonUp -= NextClick;
		NextGrid.Cursor = (CanSwitchNext ? Cursors.Hand : Cursors.Arrow);
		if (CanSwitchNext)
		{
			NextGrid.MouseLeftButtonUp += NextClick;
		}
	}

	public void NextMonthOrYear(int step)
	{
		DateTime pivotDate;
		try
		{
			DateTime dateTime = _model.PivotDate;
			if (YearMonthButton.Visibility == Visibility.Visible && dateTime.Year < 1970)
			{
				dateTime = DateTime.Today;
			}
			pivotDate = ((YearMonthButton.Visibility == Visibility.Visible) ? dateTime.AddMonths(step) : dateTime.AddYears(step));
		}
		catch (ArgumentOutOfRangeException)
		{
			return;
		}
		if (YearMonthButton.Visibility == Visibility.Visible)
		{
			_model.PivotDate = pivotDate;
			SetDayCells();
		}
		else
		{
			_model.PivotDate = pivotDate;
			ClearMonthCells();
			SetMonthCells();
		}
		RenderRepeat();
		TryUpdateViewDays();
	}

	private void MonthClick(object sender, RoutedEventArgs e)
	{
		if (AllowSelectMonth && MonthCellsGrid.Visibility != Visibility.Visible)
		{
			SwitchMonthMode();
		}
	}

	private void SwitchMonthMode()
	{
		DayGrid.Visibility = Visibility.Collapsed;
		MonthCellsGrid.Visibility = Visibility.Visible;
		YearButton.Visibility = Visibility.Visible;
		YearMonthButton.Visibility = Visibility.Collapsed;
		ClearMonthCells();
		SetMonthCells();
	}

	private void ClearMonthCells()
	{
		MonthCellsGrid.Children.Clear();
		MonthCellsGrid.RowDefinitions.Clear();
		MonthCellsGrid.ColumnDefinitions.Clear();
	}

	private void SetMonthCells()
	{
		InitMonthRowsAndColumns();
		DateTime dateTime = InitStartMonth();
		int num = 0;
		for (int i = 0; i < 3; i++)
		{
			for (int j = 0; j < 4; j++)
			{
				DateTime date = dateTime.AddMonths(num++);
				bool selected = date.Year == _model.PivotDate.Year && date.Month == _model.PivotDate.Month;
				MonthCellControl monthCellControl = new MonthCellControl(new MonthViewModel
				{
					Date = date,
					Selected = selected
				});
				MonthCellsGrid.Children.Add(monthCellControl);
				Grid.SetRow(monthCellControl, i);
				Grid.SetColumn(monthCellControl, j);
				monthCellControl.Select += OnMonthSelected;
			}
		}
		SetGotoCurrentEnable();
		UpdateNavigationButtons();
	}

	private void OnMonthSelected(object sender, DateTime date)
	{
		_model.PivotDate = _model.PivotDate.AddDays((date.Date - _model.PivotDate.Date).TotalDays);
		SetMonthSelected();
		this.MonthSelected?.Invoke(this, _model.PivotDate);
		if (!_monthMode)
		{
			DayGrid.Visibility = Visibility.Visible;
			MonthCellsGrid.Visibility = Visibility.Collapsed;
			YearButton.Visibility = Visibility.Collapsed;
			YearMonthButton.Visibility = Visibility.Visible;
			SetDayCells();
		}
		UpdateNavigationButtons();
		RenderRepeat();
	}

	private void SetMonthSelected()
	{
		foreach (MonthCellControl child in MonthCellsGrid.Children)
		{
			MonthViewModel model = child.Model;
			model.Selected = model.Date.Year == _model.PivotDate.Year && model.Date.Month == _model.PivotDate.Month;
		}
	}

	private void GotoCurrentMonth(object sender, MouseButtonEventArgs e)
	{
		if (YearMonthButton.Visibility == Visibility.Visible)
		{
			_model.PivotDate = DateTime.Today;
			if (!IsChooseDate)
			{
				if (_model.SelectedDate != DateTime.Today)
				{
					IsChoosed = true;
				}
				_model.SelectedDate = DateTime.Today;
				this.SelectedDateChanged?.Invoke(this, DateTime.Today);
			}
			SetDayCells();
		}
		else
		{
			_model.PivotDate = DateTime.Today;
			ClearMonthCells();
			SetMonthCells();
		}
		RenderRepeat();
	}

	public void TrySetDayCells()
	{
		if (!_monthMode)
		{
			DayGrid.Visibility = Visibility.Visible;
			MonthCellsGrid.Visibility = Visibility.Collapsed;
			YearButton.Visibility = Visibility.Collapsed;
			YearMonthButton.Visibility = Visibility.Visible;
			SetDayCells();
		}
	}

	private void OnMonthTextMouseMove(object sender, MouseEventArgs e)
	{
		if (AllowSelectMonth)
		{
			YearMonthButton.MouseLeftButtonUp += MonthClick;
			YearMonthButton.Cursor = Cursors.Hand;
		}
		YearMonthButton.MouseMove -= OnMonthTextMouseMove;
	}

	private void OnDayClicked(object sender, DayViewModel e)
	{
		IsChoosed = true;
		if (!ShowInCal)
		{
			if (CanMultiSelect)
			{
				e.Selected = !e.Selected;
			}
			else
			{
				e.Selected = true;
			}
			if (IsChooseDate && !e.Selected)
			{
				e.Selected = true;
				return;
			}
			if (!e.Selected)
			{
				_selectedDays.RemoveAll((DateTime item) => item.Date == e.Date.Date);
				return;
			}
			_selectedDays.Add(e.Date.Date);
			OnDateChanged(e.Date);
		}
		else
		{
			_model.SelectedDate = (ShowSelectedInCal ? new DateTime?(e.Date) : ((DateTime?)null));
		}
		this.SelectedDateChanged?.Invoke(this, e.Date);
		SetGotoCurrentEnable();
	}

	private void OnDayDoubleClicked(object sender, DayViewModel e)
	{
		if (!e.Selected)
		{
			_selectedDays.RemoveAll((DateTime item) => item.Date == e.Date.Date);
		}
		else
		{
			_selectedDays.Add(e.Date.Date);
			OnDateChanged(e.Date);
			this.DateSelected?.Invoke(this, e.Date);
		}
	}

	public void TabSelectCurrent()
	{
		if (DayGrid.Visibility == Visibility.Visible && DayCells.ItemsSource is ObservableCollection<DayCellViewModel> source)
		{
			DayCellViewModel dayCellViewModel = source.FirstOrDefault((DayCellViewModel item) => (item.Data.ShowMode == ShowMode.CurrentMonth || item.Data.ShowMode == ShowMode.Today) && item.Data.Date == _tabSelectedDate) ?? source.FirstOrDefault((DayCellViewModel item) => (item.Data.ShowMode == ShowMode.CurrentMonth || item.Data.ShowMode == ShowMode.Today) && item.Data.Selected) ?? source.FirstOrDefault((DayCellViewModel item) => (item.Data.ShowMode == ShowMode.CurrentMonth || item.Data.ShowMode == ShowMode.Today) && item.Data.CanSelect) ?? source.FirstOrDefault((DayCellViewModel item) => item.Data.ShowMode == ShowMode.CurrentMonth || item.Data.ShowMode == ShowMode.Today);
			if (dayCellViewModel != null)
			{
				dayCellViewModel.Data.TabBorderThickness = 1.0;
				_tabSelectedDate = dayCellViewModel.Data.Date;
			}
		}
	}

	public void ClearTabSelected()
	{
		if (!(DayCells.ItemsSource is ObservableCollection<DayCellViewModel> observableCollection))
		{
			return;
		}
		foreach (DayCellViewModel item in observableCollection)
		{
			item.Data.TabBorderThickness = 0.0;
		}
		_tabSelectedDate = default(DateTime);
	}

	public void MoveTabSelectDate(int step = 1)
	{
		if (!DayGrid.IsVisible || !(DayCells.ItemsSource is ObservableCollection<DayCellViewModel> observableCollection))
		{
			return;
		}
		DayCellViewModel dayCellViewModel = observableCollection.FirstOrDefault((DayCellViewModel i) => Math.Abs(i.Data.TabBorderThickness - 1.0) < 0.1);
		if (dayCellViewModel == null)
		{
			TabSelectCurrent();
			return;
		}
		DateTime date = AddDaysWithinRange(dayCellViewModel.Data.Date, step).Date;
		DateTime monthStartDate = GetMonthStartDate(date);
		_tabSelectedDate = date;
		if (monthStartDate.Date != observableCollection[0].Data.Date.Date)
		{
			_model.PivotDate = date;
			SetDayCells();
			return;
		}
		DayCellViewModel dayCellViewModel2 = observableCollection.FirstOrDefault((DayCellViewModel i) => i.Data.Date == date);
		if (dayCellViewModel2 != null)
		{
			dayCellViewModel.Data.TabBorderThickness = 0.0;
			dayCellViewModel2.Data.TabBorderThickness = 1.0;
		}
	}

	public void SelectTabItem()
	{
		if (DayGrid.IsVisible && DayCells.ItemsSource is ObservableCollection<DayCellViewModel> source)
		{
			DayCellViewModel dayCellViewModel = source.FirstOrDefault((DayCellViewModel i) => Math.Abs(i.Data.TabBorderThickness - 1.0) < 0.1);
			if (dayCellViewModel != null && dayCellViewModel.Data.CanSelect)
			{
				OnDayClicked(null, dayCellViewModel.Data);
			}
		}
	}

	public void SetMinDate(DateTime? date)
	{
		_minDate = date.GetValueOrDefault();
	}

	public void SetPivotDate(DateTime monthDate)
	{
		if (monthDate.Month != _model.PivotDate.Month || monthDate.Year != _model.PivotDate.Year)
		{
			_model.PivotDate = monthDate;
		}
	}

	public void SetCellHover(DayViewModel dayViewModel)
	{
		if (DayCells.ItemsSource == null || !(DayCells.ItemsSource is ObservableCollection<DayCellViewModel> observableCollection))
		{
			return;
		}
		foreach (DayCellViewModel item in observableCollection)
		{
			item.Data.Hover = item.Data == dayViewModel;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/time/tickdatepicker.xaml", UriKind.Relative);
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
			RootView = (TickDatePicker)target;
			RootView.Loaded += OnLoaded;
			RootView.MouseLeftButtonUp += OnClick;
			break;
		case 2:
			TopGrid = (Grid)target;
			break;
		case 3:
			YearMonthButton = (TextBlock)target;
			YearMonthButton.MouseMove += OnMonthTextMouseMove;
			break;
		case 4:
			YearButton = (TextBlock)target;
			break;
		case 5:
			NextOrLastGrid = (Grid)target;
			break;
		case 6:
			((Border)target).MouseLeftButtonUp += LastClick;
			break;
		case 7:
			((Border)target).MouseLeftButtonUp += GotoCurrentMonth;
			break;
		case 8:
			GotoCurrentBt = (Ellipse)target;
			GotoCurrentBt.MouseLeftButtonUp += GotoCurrentMonth;
			break;
		case 9:
			NextGrid = (Border)target;
			NextGrid.MouseLeftButtonUp += NextClick;
			break;
		case 10:
			MonthCellsGrid = (Grid)target;
			break;
		case 11:
			DayGrid = (StackPanel)target;
			break;
		case 12:
			WeekDaysGrid = (Grid)target;
			break;
		case 13:
			DayCells = (ItemsControl)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
