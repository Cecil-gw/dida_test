using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Shapes;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Calendar;

namespace ticktick_WPF.Views.Time;

public class SetDateDurationControl : UserControl, IComponentConnector
{
	public static readonly DependencyProperty ShowTimeZoneOptionProperty = DependencyProperty.Register("ShowTimeZoneOption", typeof(bool), typeof(SetDateDurationControl), new PropertyMetadata(false, null));

	private List<Popup> _innerPopups;

	private TimeZoneSelectControl _durationTz;

	private SelectDateDialog _setDateDialog;

	public SetDateDialog ParentWindow;

	private bool _showPopupButtonMouseDown;

	private int _tabIndex;

	internal SetDateDurationControl Root;

	internal Grid StartDateText;

	internal Border StartDateBorder;

	internal TimeInputControl StartTimeInput;

	internal Grid EndDateText;

	internal Border DueDateBorder;

	internal TimeInputControl DueTimeInput;

	internal Popup SetDatePopup;

	internal CheckBox AllDayCheckBox;

	internal Rectangle AllDayBorder;

	internal Grid TimeZone;

	internal Grid TimeZoneGrid;

	private bool _contentLoaded;

	private TimeData TimeData => (TimeData)base.DataContext;

	public bool ShowTimeZoneOption
	{
		get
		{
			return (bool)GetValue(ShowTimeZoneOptionProperty);
		}
		set
		{
			SetValue(ShowTimeZoneOptionProperty, value);
		}
	}

	public bool DirectSelectTimeZone { get; set; }

	public event EventHandler PopupOpened;

	public event EventHandler PopupClosed;

	public SetDateDurationControl()
	{
		InitializeComponent();
		_innerPopups = new List<Popup> { SetDatePopup, StartTimeInput.DropdownPopup, DueTimeInput.DropdownPopup };
		foreach (Popup innerPopup in _innerPopups)
		{
			innerPopup.Opened += OnPopupOpened;
			innerPopup.Closed += OnPopupClosed;
		}
		StartTimeInput.ReminderPopup.HorizontalOffset = -150.0;
		DueTimeInput.ReminderPopup.HorizontalOffset = -150.0;
	}

	private void OnPopupOpened(object sender, EventArgs e)
	{
		this.PopupOpened?.Invoke(this, null);
	}

	private void OnPopupClosed(object sender, EventArgs e)
	{
		this.PopupClosed?.Invoke(this, null);
	}

	public void SetData()
	{
		InitTimeZoneControl();
		InitDurationTimeControl();
	}

	private void InitTimeZoneControl()
	{
		TimeZoneGrid.Children.Clear();
		if (_durationTz == null)
		{
			_durationTz = new TimeZoneSelectControl(TimeData.TimeZone);
			_durationTz.OnTimeZoneChanged += delegate(object obj, TimeZoneViewModel model)
			{
				TimeData.TimeZone = model;
			};
			_durationTz.OptionPopup.Opened += OnPopupOpened;
			_durationTz.OptionPopup.Closed += OnPopupClosed;
			_innerPopups.Add(_durationTz.OptionPopup);
			_durationTz.DirectTimeZonePopup.Opened += OnPopupOpened;
			_durationTz.DirectTimeZonePopup.Closed += OnPopupClosed;
			_innerPopups.Add(_durationTz.DirectTimeZonePopup);
		}
		else
		{
			_durationTz.SetData(TimeData.TimeZone);
		}
		_durationTz.DirectSelectTimeZone = DirectSelectTimeZone;
		TimeZoneGrid.Children.Add(_durationTz);
	}

	private void InitDurationTimeControl()
	{
		StartTimeInput.SelectedTimeChanged -= OnStartTimeSelected;
		StartTimeInput.SelectedTimeChanged += OnStartTimeSelected;
		DueTimeInput.SelectedTimeChanged -= OnDueTimeSelected;
		DueTimeInput.SelectedTimeChanged += OnDueTimeSelected;
		NotifyTimeChanged();
		DueTimeInput.BeginTime = TimeData.StartDate;
	}

	private void OnDueTimeSelected(object sender, DateTime time)
	{
		if (TimeData.DueDate.HasValue && TimeData.IsAllDay.HasValue && !TimeData.IsAllDay.Value)
		{
			DateTime value = DateUtils.SetHourAndMinuteOnly(time, time.Hour, time.Minute);
			TimeData.DueDate = ((TimeData.StartDate > value) ? TimeData.StartDate : new DateTime?(value));
			NotifyTimeChanged(exceptStart: true);
		}
	}

	private void OnStartTimeSelected(object sender, DateTime time)
	{
		if (TimeData.StartDate.HasValue && TimeData.DueDate.HasValue)
		{
			double totalMinutes = (TimeData.DueDate.Value - TimeData.StartDate.Value).TotalMinutes;
			TimeData.StartDate = DateUtils.SetHourAndMinuteOnly(TimeData.StartDate.Value, time.Hour, time.Minute);
			TimeData.DueDate = TimeData.StartDate.Value.AddMinutes(totalMinutes);
			DueTimeInput.BeginTime = TimeData.StartDate;
			NotifyTimeChanged(exceptStart: true);
		}
	}

	private void NotifyTimeChanged(bool exceptStart = false, bool exceptDue = false)
	{
		if (TimeData.StartDate.HasValue && !exceptStart)
		{
			StartTimeInput.SelectedTime = TimeData.StartDate.Value;
		}
		if (TimeData.DueDate.HasValue && !exceptDue)
		{
			DueTimeInput.SelectedTime = TimeData.DueDate.Value;
		}
		TimeData.CheckEndReminders();
	}

	private void StartDateClick(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
		if (_showPopupButtonMouseDown)
		{
			ShowStartDatePopup();
		}
	}

	private void ShowStartDatePopup(bool inTab = false)
	{
		PrepareOpenPopup();
		DurationModel model = new DurationModel
		{
			SelectedDate = TimeData.StartDate,
			SelectionStart = TimeData.StartDate,
			SelectionEnd = TimeData.DueDate
		};
		if (_setDateDialog == null)
		{
			_setDateDialog = new SelectDateDialog(SetDatePopup, model, isStart: true);
		}
		else
		{
			_setDateDialog.SetData(model, isStart: true);
		}
		_setDateDialog.SetCurrentTab(inTab);
		_setDateDialog.SelectDate -= SelectDueDate;
		_setDateDialog.SelectDate -= SelectStartDate;
		_setDateDialog.SelectDate += SelectStartDate;
		_setDateDialog.SelectDateAndSave -= SelectSetDateAndSave;
		_setDateDialog.SelectDateAndSave += SelectSetDateAndSave;
		SetDatePopup.PlacementTarget = StartDateText;
		_setDateDialog.Show();
		StartDateBorder.BorderThickness = new Thickness(1.0);
		_showPopupButtonMouseDown = false;
	}

	private void SelectSetDateAndSave(object sender, DateTime e)
	{
		OnStartDateSelected(e);
		ParentWindow.SaveClick(null, null);
	}

	private void SelectStartDate(object sender, DateTime date)
	{
		OnStartDateSelected(date);
	}

	private void OnStartDateSelected(DateTime date)
	{
		TimeData.StartDate = date;
		if (TimeData.StartDate.HasValue)
		{
			StartTimeInput.SelectedTime = TimeData.StartDate.Value;
		}
		if (_setDateDialog.SelectEnd.HasValue)
		{
			TimeData.DueDate = _setDateDialog.SelectEnd.Value;
		}
		if (TimeData.RepeatFlag != null)
		{
			TaskService.TryFixRepeatFlag(TimeData);
		}
		NotifyTimeChanged();
	}

	private void PrepareOpenPopup()
	{
		CloseInnerPopups();
	}

	private bool CloseInnerPopups()
	{
		bool result = false;
		foreach (Popup innerPopup in _innerPopups)
		{
			if (innerPopup.IsOpen)
			{
				innerPopup.IsOpen = false;
				result = true;
			}
		}
		return result;
	}

	private void StartTimeClick(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
		PrepareOpenPopup();
		StartTimeInput.ClearSelection();
	}

	private void EndDateClick(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
		if (_showPopupButtonMouseDown)
		{
			ShowEndDatePopup();
		}
	}

	private void ShowEndDatePopup(bool inTab = false)
	{
		PrepareOpenPopup();
		DurationModel durationModel = new DurationModel
		{
			SelectedDate = TimeData.DueDate,
			SelectionStart = TimeData.StartDate,
			SelectionEnd = TimeData.DueDate
		};
		if (_setDateDialog == null)
		{
			Popup setDatePopup = SetDatePopup;
			DateTime? minDate = durationModel.SelectionStart?.AddDays(-1.0);
			_setDateDialog = new SelectDateDialog(setDatePopup, durationModel, isStart: false, null, minDate);
		}
		else
		{
			SelectDateDialog setDateDialog = _setDateDialog;
			DateTime? minDate = durationModel.SelectionStart?.AddDays(-1.0);
			setDateDialog.SetData(durationModel, isStart: false, null, minDate);
		}
		_setDateDialog.SetCurrentTab(inTab);
		_setDateDialog.SelectDate -= SelectDueDate;
		_setDateDialog.SelectDate += SelectDueDate;
		_setDateDialog.SelectDate -= SelectStartDate;
		_setDateDialog.SelectDateAndSave -= SelectSetDateAndSave;
		SetDatePopup.PlacementTarget = EndDateText;
		_setDateDialog.Show();
		DueDateBorder.BorderThickness = new Thickness(1.0);
		_showPopupButtonMouseDown = false;
	}

	private void SelectDueDate(object sender, DateTime date)
	{
		TimeData.DueDate = DateUtils.SetDateOnly(TimeData.DueDate ?? date, date);
		if (_setDateDialog.SelectStart.HasValue)
		{
			TimeData.StartDate = _setDateDialog.SelectStart.Value;
			DateTime? startDate = TimeData.StartDate;
			DateTime? dueDate = TimeData.DueDate;
			if ((startDate.HasValue & dueDate.HasValue) && startDate.GetValueOrDefault() >= dueDate.GetValueOrDefault() && TimeData.IsAllDay.HasValue && !TimeData.IsAllDay.Value)
			{
				TimeData.StartDate = TimeData.DueDate.Value.AddHours(-1.0);
			}
			if (TimeData.DueDate.HasValue && TimeData.IsAllDay.HasValue && TimeData.IsAllDay.Value && TimeData.DueDate < TimeData.StartDate)
			{
				TimeData.DueDate = TimeData.StartDate.Value.AddDays(1.0);
			}
		}
		NotifyTimeChanged();
	}

	private void EndTimeClick(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
		PrepareOpenPopup();
		DueTimeInput.ClearSelection();
	}

	private void OnAllDayClick(object sender, RoutedEventArgs e)
	{
		CheckAllDay(AllDayCheckBox.IsChecked.HasValue && AllDayCheckBox.IsChecked.Value);
	}

	private void CheckAllDay(bool check)
	{
		if (check)
		{
			TimeData.Reminders = TimeData.GetDefaultAllDayReminders();
			NotifyTimeChanged();
		}
		else
		{
			TrySetNextHour(TimeData);
			TimeData.Reminders = TimeData.GetDefaultTimeReminders();
			NotifyTimeChanged();
		}
	}

	private void TrySetNextHour(TimeData timeData)
	{
		if (timeData.StartDate.HasValue && timeData.DueDate.HasValue)
		{
			DateTime value = timeData.StartDate.Value;
			DateTime value2 = timeData.DueDate.Value;
			if (value.Hour == 0 && value.Minute == 0 && value2.Hour == 0 && value2.Minute == 0)
			{
				int hour = DateTime.Now.Hour;
				timeData.StartDate = DateUtils.SetHourAndMinuteOnly(timeData.StartDate.Value, hour, 0).AddHours(1.0);
				timeData.DueDate = DateUtils.SetHourAndMinuteOnly(timeData.DueDate.Value, hour, 0).AddHours(2.0);
			}
		}
	}

	private void ClosePopup(object sender, MouseButtonEventArgs e)
	{
		if (_durationTz == null || (!_durationTz.OptionPopup.IsOpen && !_durationTz.DirectTimeZonePopup.IsOpen))
		{
			CloseInnerPopups();
			StartTimeInput.ClearSelection();
			DueTimeInput.ClearSelection();
		}
	}

	public void TryClosePopup()
	{
		foreach (Popup innerPopup in _innerPopups)
		{
			innerPopup.IsOpen = false;
		}
	}

	private void DatePopupClosed(object sender, EventArgs e)
	{
		PopupStateManager.SetCanOpenTimePopup(canOpen: true);
		StartDateBorder.BorderThickness = new Thickness((_tabIndex == 0) ? 1 : 0);
		DueDateBorder.BorderThickness = new Thickness((_tabIndex == 2) ? 1 : 0);
	}

	private void DatePopupOpened(object sender, EventArgs e)
	{
		PopupStateManager.SetCanOpenTimePopup(canOpen: false);
	}

	private void OnShowPopupButtonMouseDown(object sender, MouseButtonEventArgs e)
	{
		if (PopupStateManager.CanOpenTimePopup)
		{
			_showPopupButtonMouseDown = true;
		}
		else
		{
			PopupStateManager.SetCanOpenTimePopup(canOpen: true);
		}
	}

	public bool SetTabIndex(int index)
	{
		_tabIndex = index;
		StartDateBorder.BorderThickness = new Thickness((_tabIndex == 0) ? 1 : 0);
		StartTimeInput.TabSelected = _tabIndex == 1;
		DueDateBorder.BorderThickness = new Thickness((_tabIndex == 2) ? 1 : 0);
		DueTimeInput.TabSelected = _tabIndex == 3;
		AllDayBorder.Opacity = ((_tabIndex == 4) ? 1 : 0);
		if (_durationTz != null)
		{
			_durationTz.TabSelected = _tabIndex == 5;
		}
		switch (_tabIndex)
		{
		case 1:
			if (StartTimeInput.IsVisible)
			{
				break;
			}
			goto case 6;
		case 3:
			if (DueTimeInput.IsVisible)
			{
				break;
			}
			goto case 6;
		case 5:
			if (TimeZone.IsVisible)
			{
				break;
			}
			goto case 6;
		case 6:
			return false;
		}
		return true;
	}

	public bool HandleEnter()
	{
		switch (_tabIndex)
		{
		case 0:
			if (SetDatePopup.IsOpen)
			{
				_setDateDialog?.EnterSelect();
			}
			else
			{
				ShowStartDatePopup(inTab: true);
			}
			break;
		case 1:
			StartTimeInput.ClearSelection();
			StartTimeInput.ToggleReminderPopup();
			StartTimeInput.FocusHour();
			break;
		case 2:
			if (SetDatePopup.IsOpen)
			{
				_setDateDialog?.EnterSelect();
			}
			else
			{
				ShowEndDatePopup(inTab: true);
			}
			break;
		case 3:
			DueTimeInput.ClearSelection();
			DueTimeInput.ToggleReminderPopup();
			DueTimeInput.FocusHour();
			break;
		case 4:
		{
			bool flag = AllDayCheckBox.IsChecked != true;
			AllDayCheckBox.IsChecked = flag;
			CheckAllDay(flag);
			break;
		}
		case 5:
			if (TimeZone.IsVisible)
			{
				_durationTz?.ShowPopup();
			}
			break;
		}
		if (_tabIndex >= 0)
		{
			return _tabIndex <= 5;
		}
		return false;
	}

	public bool HandleEsc()
	{
		return CloseInnerPopups();
	}

	public void HandleLeftRight(bool isLeft)
	{
		if (SetDatePopup.IsOpen)
		{
			_setDateDialog?.HandleLeftRight(isLeft);
		}
	}

	public void HandleUpDown(bool isUp)
	{
		if (SetDatePopup.IsOpen)
		{
			_setDateDialog?.HandleUpDown(isUp);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/time/setdatedurationcontrol.xaml", UriKind.Relative);
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
			Root = (SetDateDurationControl)target;
			break;
		case 2:
			((StackPanel)target).MouseLeftButtonUp += ClosePopup;
			break;
		case 3:
			StartDateText = (Grid)target;
			StartDateText.MouseLeftButtonUp += StartDateClick;
			StartDateText.MouseLeftButtonDown += OnShowPopupButtonMouseDown;
			break;
		case 4:
			StartDateBorder = (Border)target;
			break;
		case 5:
			((Grid)target).MouseLeftButtonUp += StartTimeClick;
			break;
		case 6:
			StartTimeInput = (TimeInputControl)target;
			break;
		case 7:
			EndDateText = (Grid)target;
			EndDateText.MouseLeftButtonUp += EndDateClick;
			EndDateText.MouseLeftButtonDown += OnShowPopupButtonMouseDown;
			break;
		case 8:
			DueDateBorder = (Border)target;
			break;
		case 9:
			((Grid)target).MouseLeftButtonUp += EndTimeClick;
			break;
		case 10:
			DueTimeInput = (TimeInputControl)target;
			break;
		case 11:
			SetDatePopup = (Popup)target;
			SetDatePopup.Opened += DatePopupOpened;
			SetDatePopup.Closed += DatePopupClosed;
			break;
		case 12:
			AllDayCheckBox = (CheckBox)target;
			AllDayCheckBox.Click += OnAllDayClick;
			break;
		case 13:
			AllDayBorder = (Rectangle)target;
			break;
		case 14:
			TimeZone = (Grid)target;
			break;
		case 15:
			TimeZoneGrid = (Grid)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
