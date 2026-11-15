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
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Calendar;

namespace ticktick_WPF.Views.Time;

public class SetAllDateDurationControl : UserControl, IComponentConnector, IStyleConnector
{
	private List<Popup> _innerPopups;

	private TimeZoneSelectControl _durationTz;

	private SelectDateDialog _setDateDialog;

	public SetDateDialog ParentWindow;

	private bool _showPopupButtonMouseDown;

	private int _tabIndex;

	internal Grid StartDateGrid;

	internal Grid EndDateGrid;

	internal Popup SetDatePopup;

	private bool _contentLoaded;

	private TimeData TimeData => (TimeData)base.DataContext;

	public event EventHandler PopupOpened;

	public event EventHandler PopupClosed;

	public SetAllDateDurationControl()
	{
		InitializeComponent();
		_innerPopups = new List<Popup> { SetDatePopup };
		foreach (Popup innerPopup in _innerPopups)
		{
			innerPopup.Opened += OnPopupOpened;
			innerPopup.Closed += OnPopupClosed;
		}
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
		}
		else
		{
			_durationTz.SetData(TimeData.TimeZone);
		}
		InitDurationTimeControl();
	}

	private void InitDurationTimeControl()
	{
		NotifyTimeChanged();
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
			NotifyTimeChanged(exceptStart: true);
		}
	}

	private void NotifyTimeChanged(bool exceptStart = false, bool exceptDue = false)
	{
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
		SetDatePopup.PlacementTarget = StartDateGrid;
		_setDateDialog.Show();
		_showPopupButtonMouseDown = false;
	}

	private void SelectSetDateAndSave(object sender, DateTime e)
	{
		OnStartDateSelected(e);
		ParentWindow?.SaveClick(null, null);
	}

	private void SelectStartDate(object sender, DateTime date)
	{
		OnStartDateSelected(date);
	}

	private void OnStartDateSelected(DateTime date)
	{
		TimeData.StartDate = date;
		if (_setDateDialog.SelectEnd.HasValue)
		{
			TimeData.DueDate = _setDateDialog.SelectEnd.Value;
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
		SetDatePopup.PlacementTarget = EndDateGrid;
		_setDateDialog.Show();
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
	}

	private void CheckAllDay(bool check)
	{
		if (check)
		{
			if (TimeData.StartDate.HasValue && TimeData.DueDate.HasValue && TimeData.StartDate.Value.Date == TimeData.DueDate.Value.Date)
			{
				TimeData.DueDate = TimeData.DueDate.Value.AddDays(1.0);
				NotifyTimeChanged();
			}
			TimeData.Reminders = TimeData.GetDefaultAllDayReminders();
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
		if (_durationTz == null || !_durationTz.OptionPopup.IsOpen)
		{
			CloseInnerPopups();
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
		if (_durationTz != null)
		{
			_durationTz.TabSelected = _tabIndex == 5;
		}
		if (_tabIndex == 6)
		{
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
			Uri resourceLocator = new Uri("/TickTick;component/views/time/setalldatedurationcontrol.xaml", UriKind.Relative);
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
			((StackPanel)target).MouseLeftButtonUp += ClosePopup;
			break;
		case 3:
			StartDateGrid = (Grid)target;
			StartDateGrid.MouseLeftButtonUp += StartDateClick;
			break;
		case 4:
			EndDateGrid = (Grid)target;
			EndDateGrid.MouseLeftButtonUp += EndDateClick;
			break;
		case 5:
			SetDatePopup = (Popup)target;
			SetDatePopup.Opened += DatePopupOpened;
			SetDatePopup.Closed += DatePopupClosed;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IStyleConnector.Connect(int connectionId, object target)
	{
		if (connectionId == 2)
		{
			EventSetter eventSetter = new EventSetter();
			eventSetter.Event = UIElement.MouseLeftButtonDownEvent;
			eventSetter.Handler = new MouseButtonEventHandler(OnShowPopupButtonMouseDown);
			((Style)target).Setters.Add(eventSetter);
		}
	}
}
