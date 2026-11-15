using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Habit;

public class HabitMonthGrid : UserControl, IComponentConnector
{
	private const int DayColumns = 7;

	private const int DayRows = 6;

	public static readonly DependencyProperty StartDateProperty = DependencyProperty.Register("StartDate", typeof(DateTime), typeof(HabitMonthGrid), new PropertyMetadata(DateTime.Today, null));

	private HabitModel _habit;

	private DateTime _manualSelectDate;

	private DatePickerViewModel _model;

	private List<HabitCheckInModel> _stamps = new List<HabitCheckInModel>();

	internal HabitMonthGrid RootView;

	internal StackPanel DayGrid;

	internal ItemsControl DayCells;

	internal Popup ManuallyCheckInPopup;

	internal ManualRecordCheckinControl CheckInControl;

	private bool _contentLoaded;

	public bool IsDataLoaded { get; private set; }

	private DateTime PivotDate => _model.PivotDate;

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

	public event EventHandler<KeyValuePair<string, DateTime>> CheckInChanged;

	public event EventHandler<DateTime> PivotDateChanged;

	public HabitMonthGrid()
		: this(null)
	{
	}

	private HabitMonthGrid(DateTime? selectedDate, DateTime? selectStart = null, DateTime? selectEnd = null)
	{
		InitData(selectedDate, selectStart, selectEnd);
		InitializeComponent();
		base.Loaded += delegate
		{
			DataChangedNotifier.WeekStartFromChanged -= Reload;
			DataChangedNotifier.WeekStartFromChanged += Reload;
		};
		base.Unloaded += delegate
		{
			DataChangedNotifier.WeekStartFromChanged -= Reload;
		};
	}

	public async Task Load(HabitDetailViewModel habit, DateTime pivotDate, List<HabitCheckInModel> checkIns)
	{
		_stamps = checkIns;
		_habit = habit.Habit;
		_model.PivotDate = pivotDate;
		await InitCells(habit.Habit);
		IsDataLoaded = true;
	}

	private void Reload(object sender, EventArgs e)
	{
		if (_habit != null)
		{
			InitCells(_habit);
		}
	}

	private async Task InitCells(HabitModel habit)
	{
		await SetDayCells(habit);
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
		base.DataContext = _model;
	}

	private async Task SetDayCells(HabitModel habit)
	{
		if (habit == null)
		{
			return;
		}
		HabitRepeatInfo repeatInfo = HabitUtils.BuildHabitRepeatInfo(habit.RepeatRule, habit.ExDates);
		List<(DateTime, bool?)> checkDates = new List<(DateTime, bool?)>();
		StartDate = InitStartDate();
		DateTime lastDay = DateTime.Today;
		if (habit.Status != 0)
		{
			lastDay = habit.ModifiedTime.Date;
		}
		DateTime firstCheckDay = habit.GetStartDate().Date;
		List<HabitCheckInModel> checkIns = await HabitCheckInDao.GetHabitCheckInsByHabitId(habit.Id);
		if (firstCheckDay > StartDate)
		{
			List<HabitRecordModel> list = await HabitRecordDao.GetHabitRecordsByHabitId(habit.Id);
			if (list != null)
			{
				foreach (HabitRecordModel item in list)
				{
					if (DateTime.TryParseExact(item.Stamp.ToString(), "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var result) && result.Date < firstCheckDay)
					{
						firstCheckDay = result.Date;
					}
				}
			}
		}
		if (checkIns != null)
		{
			foreach (HabitCheckInModel item2 in checkIns.Where((HabitCheckInModel checkIn) => checkIn.CheckStatus != 0 || checkIn.Value > 0.0))
			{
				if (DateTime.TryParseExact(item2.CheckinStamp, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var result2))
				{
					if (result2.Date < firstCheckDay)
					{
						firstCheckDay = result2.Date;
					}
					if ((repeatInfo.Type == HabitRepeatType.TimesInWeek || (repeatInfo.Type == HabitRepeatType.Daily && repeatInfo.Interval > 1)) && (repeatInfo.Type != HabitRepeatType.TimesInWeek || (!(result2 < StartDate) && !(result2 > StartDate.AddDays(42.0)))))
					{
						checkDates.Add((result2, (item2.CheckStatus == 0) ? ((bool?)null) : new bool?(item2.CheckStatus == 2)));
					}
				}
			}
		}
		int num = 0;
		List<DayCellViewModel> list2 = new List<DayCellViewModel>();
		for (int num2 = 0; num2 < 6; num2++)
		{
			for (int num3 = 0; num3 < 7; num3++)
			{
				DateTime dateTime = StartDate.AddDays(num++);
				string safeIconRes = habit.GetSafeIconRes();
				bool flag = habit.IsBooleanType();
				HabitDayViewModel habitDayViewModel = new HabitDayViewModel
				{
					HabitId = habit.Id,
					Habit = habit,
					Date = dateTime,
					IsBooleanHabit = flag,
					UseIconStyle = (habit.Style == 2)
				};
				if (safeIconRes.StartsWith("txt"))
				{
					habitDayViewModel.Color = habit.Color;
					habitDayViewModel.IconText = safeIconRes.Split('_').LastOrDefault() ?? "";
				}
				else
				{
					habitDayViewModel.ImageUrl = "/Assets/Habits/" + safeIconRes.ToLower() + ".png";
				}
				InitShowMode(habitDayViewModel, lastDay);
				bool flag2 = dateTime >= firstCheckDay && dateTime <= lastDay && HabitUtils.InitShowHint(repeatInfo, dateTime, firstCheckDay, checkDates);
				habitDayViewModel.ShowBoolHint = flag && flag2;
				habitDayViewModel.ShowRealHint = !flag && flag2;
				list2.Add(new DayCellViewModel
				{
					Row = num2,
					Column = num3,
					Data = habitDayViewModel
				});
			}
		}
		foreach (DayCellViewModel item3 in list2)
		{
			string stamp = item3.Data.Date.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
			HabitCheckInModel habitCheckInModel = _stamps.FirstOrDefault((HabitCheckInModel dateStamp) => stamp == dateStamp.CheckinStamp && dateStamp.Value >= 0.0);
			if (habitCheckInModel != null)
			{
				item3.Data.Completed = habitCheckInModel.CheckStatus != 1 && habitCheckInModel.Value >= habitCheckInModel.Goal;
				item3.Data.UnCompleted = habitCheckInModel.CheckStatus == 1;
				item3.Data.Percent = ((!habit.IsBoolHabit()) ? Math.Min((int)(habitCheckInModel.GetDisplayValue() / habitCheckInModel.Goal * 100.0), 100) : 0);
				item3.Data.HasPercent = item3.Data.Percent > 0.0;
			}
			else
			{
				item3.Data.Completed = false;
				item3.Data.Percent = 0.0;
				item3.Data.HasPercent = false;
			}
		}
		ItemsSourceHelper.SetHidableItemsSource(DayCells, list2);
	}

	private void InitShowMode(HabitDayViewModel model, DateTime lastDate)
	{
		model.IsFutureDate = model.Date > lastDate;
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

	private DateTime InitStartDate()
	{
		if (Utils.IsEmptyDate(_model.PivotDate))
		{
			return DateTime.Today;
		}
		DateTime weekStart = Utils.GetWeekStart(_model.PivotDate.AddDays(1 - _model.PivotDate.Day));
		if (weekStart.Month == PivotDate.Month && weekStart.Day != 1)
		{
			return weekStart.AddDays(-7.0);
		}
		return weekStart;
	}

	private void OnClick(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
	}

	private void LastClick(object sender, MouseButtonEventArgs e)
	{
		NextMonth(-1);
	}

	private void NextClick(object sender, MouseButtonEventArgs e)
	{
		if (_model.PivotDate < DateTime.Today)
		{
			NextMonth(1);
		}
	}

	private async void NextMonth(int step)
	{
		_model.PivotDate = _model.PivotDate.AddMonths(step);
		await ReloadStamps();
		SetDayCells(_habit);
		NotifyPivotDateChanged(_model.PivotDate);
	}

	private async Task ReloadStamps()
	{
		StartDate = InitStartDate();
		int startDateStamp = DateUtils.GetDateNum(StartDate.AddDays(-14.0));
		int endDateStamp = DateUtils.GetDateNum(StartDate.AddDays(42.0).AddDays(14.0));
		List<HabitCheckInModel> obj = (await HabitCheckinCache.GetHabitCheckInsById(_habit.Id)) ?? new List<HabitCheckInModel>();
		List<HabitCheckInModel> list = new List<HabitCheckInModel>();
		foreach (HabitCheckInModel item in obj)
		{
			int num = int.Parse(item.CheckinStamp);
			if (num >= startDateStamp && num <= endDateStamp)
			{
				list.Add(item);
			}
		}
		_stamps = list;
	}

	private void NotifyPivotDateChanged(DateTime pivotDate)
	{
		this.PivotDateChanged?.Invoke(this, pivotDate);
	}

	private async void OnTodayClick(object sender, MouseButtonEventArgs e)
	{
		_model.PivotDate = DateTime.Today;
		await ReloadStamps();
		SetDayCells(_habit);
		NotifyPivotDateChanged(_model.PivotDate);
	}

	public async void ManuallyRecord(DateTime date, bool placeMouse)
	{
		HabitModel habitModel = ((_habit != null) ? (await HabitDao.GetHabitById(_habit.Id)) : null);
		HabitModel habitModel2 = habitModel;
		if (habitModel2 == null || habitModel2.IsBooleanType())
		{
			return;
		}
		_habit = habitModel2;
		_manualSelectDate = date;
		CheckInControl.DateText.Visibility = Visibility.Visible;
		CheckInControl.DateText.Text = DateUtils.FormatMonthDay(date, DateFormatLen.Long);
		HabitCheckInModel habitCheckInModel = _stamps.FirstOrDefault((HabitCheckInModel stamp) => stamp.CheckinStamp == date.ToString("yyyyMMdd", CultureInfo.InvariantCulture));
		CheckInControl.LeftLabel.Text = Utils.GetString("Total");
		CheckInControl.Init(habitCheckInModel?.Value ?? 0.0, _habit?.Unit ?? "Count", date == DateTime.Today);
		CheckInControl.Cancel -= OnCheckInPopupCancel;
		CheckInControl.Cancel += OnCheckInPopupCancel;
		CheckInControl.Save -= OnCheckInPopupSave;
		CheckInControl.Save += OnCheckInPopupSave;
		ManuallyCheckInPopup.Placement = PlacementMode.MousePoint;
		if (!placeMouse)
		{
			for (int num = 0; num < DayCells.Items.Count; num++)
			{
				ContentPresenter contentPresenter = (ContentPresenter)DayCells.ItemContainerGenerator.ContainerFromIndex(num);
				if (contentPresenter != null && contentPresenter.DataContext is DayCellViewModel dayCellViewModel && dayCellViewModel.Data.Date.Date.Equals(DateTime.Today))
				{
					ManuallyCheckInPopup.PlacementTarget = contentPresenter;
					ManuallyCheckInPopup.Placement = PlacementMode.Bottom;
					break;
				}
			}
		}
		ManuallyCheckInPopup.Tag = ((habitCheckInModel == null) ? ((bool?)null) : new bool?(habitCheckInModel.Value < habitCheckInModel.Goal));
		await Task.Delay(50);
		ManuallyCheckInPopup.IsOpen = true;
	}

	private async void OnCheckInPopupSave(object sender, double amount)
	{
		bool? newCheck = (bool?)ManuallyCheckInPopup.Tag;
		ManuallyCheckInPopup.IsOpen = false;
		if (newCheck.HasValue || !(Math.Abs(amount) < 0.001))
		{
			HabitModel habitModel = ((_habit != null) ? (await HabitDao.GetHabitById(_habit.Id)) : null);
			HabitModel habitModel2 = habitModel;
			if (habitModel2 != null && !habitModel2.IsBooleanType())
			{
				_habit = habitModel2;
				await HabitService.CheckInHabit(_habit.Id, _manualSelectDate, Math.Max(amount, 0.0), increment: false, !newCheck.HasValue || newCheck.Value);
				await ReloadStamps();
				SetDayCells(_habit);
			}
		}
	}

	private void OnCheckInPopupCancel(object sender, EventArgs e)
	{
		ManuallyCheckInPopup.IsOpen = false;
	}

	public void Dispose()
	{
		DataChangedNotifier.WeekStartFromChanged -= Reload;
		CheckInControl.Cancel -= OnCheckInPopupCancel;
		CheckInControl.Save -= OnCheckInPopupSave;
	}

	public bool AnyChecked()
	{
		return (DayCells.ItemsSource as ObservableCollection<DayCellViewModel>)?.Any((DayCellViewModel d) => d.Data.Completed) ?? false;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/habit/habitmonthgrid.xaml", UriKind.Relative);
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
			RootView = (HabitMonthGrid)target;
			RootView.MouseLeftButtonUp += OnClick;
			break;
		case 2:
			((ContentControl)target).MouseLeftButtonUp += LastClick;
			break;
		case 3:
			((TextBlock)target).MouseLeftButtonUp += OnTodayClick;
			break;
		case 4:
			((ContentControl)target).MouseLeftButtonUp += NextClick;
			break;
		case 5:
			DayGrid = (StackPanel)target;
			break;
		case 6:
			DayCells = (ItemsControl)target;
			break;
		case 7:
			ManuallyCheckInPopup = (Popup)target;
			break;
		case 8:
			CheckInControl = (ManualRecordCheckinControl)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
