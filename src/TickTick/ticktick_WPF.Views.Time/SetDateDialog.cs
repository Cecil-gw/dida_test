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
using TickTickUtils.Lang;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Calendar;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Views.Time;

public class SetDateDialog : UserControl, ITabControl, IComponentConnector
{
	private enum DateMode
	{
		Date,
		Duration
	}

	private SetDateTimeControl _dateTimePanel;

	private SetDateDurationControl _durationPanel;

	private bool _calendarMode;

	private bool _noteMode;

	private static SetDateDialog _dialog;

	private bool _item;

	private bool _inOperation;

	private EscPopup _popup;

	private TimeData _originData;

	private bool _showQuickDate;

	private int _dateTabIndex;

	private bool _showRemind;

	private bool _showTimeZoneOption;

	internal ContentControl Container;

	internal TextBox EmptyBox;

	internal Grid SwitchTitle;

	internal GroupTitle2 DateOrDurationSwitch;

	internal TextBlock Title;

	internal Grid QuickDatePanel;

	internal ColumnDefinition SkipGridColumn;

	internal Border Today;

	internal Border Tomorrow;

	internal Border NextWeek;

	internal Border NextMonth;

	internal Border SkipGrid;

	internal Border DateTimeBorder;

	internal Border DurationBorder;

	internal Button BatchEditButton;

	internal Border ReminderBorder;

	internal SelectReminderOrRepeatControl ReminderOrRepeatControl;

	internal Grid LocalReminderTimeGrid;

	internal TextBlock LocalReminderTime;

	internal Button SaveButton;

	internal Button CancelButton;

	private bool _contentLoaded;

	private TimeData TimeData => base.DataContext as TimeData;

	public bool ShowTabSelected { get; set; }

	public event EventHandler<TimeData> Save;

	public event EventHandler Clear;

	public event EventHandler Hided;

	public event EventHandler SkipRecurrence;

	private void InitDateTimeControl()
	{
		_dateTimePanel = new SetDateTimeControl();
		_dateTimePanel.ParentWindow = this;
		_dateTimePanel.TimePointControl.DropdownPopup.Opened += OnPopupOpened;
		_dateTimePanel.TimePointControl.DropdownPopup.Closed += OnPopupClosed;
		_dateTimePanel.DateSelected -= OnDateSelected;
		_dateTimePanel.DateSelected += OnDateSelected;
		DateTimeBorder.Child = _dateTimePanel;
	}

	private void InitDurationControl()
	{
		_durationPanel = new SetDateDurationControl();
		_durationPanel.ParentWindow = this;
		_durationPanel.PopupOpened += OnPopupOpened;
		_durationPanel.PopupClosed += OnPopupClosed;
		DurationBorder.Child = _durationPanel;
	}

	private SetDateDialog(EscPopup popup = null)
	{
		InitializeComponent();
		InitPopupEvent();
		_popup = popup ?? new EscPopup
		{
			StaysOpen = false
		};
		_popup.Closed += async delegate
		{
			this.Hided?.Invoke(this, null);
			await Task.Delay(40);
			ClearEventHandle();
		};
		_popup.KeyUp += OnKeyUp;
		_popup.Child = this;
	}

	private void OnKeyUp(object sender, KeyEventArgs e)
	{
		if (_inOperation || !ShowTabSelected)
		{
			return;
		}
		Key key = e.Key;
		if (key != Key.Back && key != Key.Delete)
		{
			return;
		}
		switch (_dateTabIndex)
		{
		case 8:
			if (DateTimeBorder.Visibility == Visibility.Visible)
			{
				_dateTimePanel?.ClearTime();
			}
			break;
		case 9:
		case 10:
		case 11:
			ReminderOrRepeatControl.ClearReminderOrRepeat();
			break;
		}
	}

	private void OnTabKeyUp(bool shift)
	{
		if (!_popup.IsOpen)
		{
			return;
		}
		if (ShowTabSelected)
		{
			_dateTabIndex += 14 + ((!shift) ? 1 : (-1));
			_dateTabIndex %= 14;
		}
		else
		{
			ShowTabSelected = true;
			if (_dateTabIndex < 0)
			{
				_dateTabIndex = 0;
			}
		}
		if (_dateTabIndex < 2 && (DateOrDurationSwitch.Visibility != Visibility.Visible || SwitchTitle.Visibility != Visibility.Visible))
		{
			OnTabKeyUp(shift);
			return;
		}
		if (DateOrDurationSwitch.GetSelectedIndex() == 1)
		{
			SetDateDurationControl durationPanel = _durationPanel;
			if (durationPanel == null || !durationPanel.SetTabIndex(_dateTabIndex - 2))
			{
				OnTabKeyUp(shift);
				return;
			}
		}
		else
		{
			if (QuickDatePanel.Visibility != Visibility.Visible && _dateTabIndex >= 2 && _dateTabIndex <= 6)
			{
				OnTabKeyUp(shift);
				return;
			}
			SetDateBorderStatus(_dateTabIndex);
			if (_dateTimePanel != null && !_dateTimePanel.SetTabSelect(_dateTabIndex - 7))
			{
				OnTabKeyUp(shift);
				return;
			}
			if (_dateTabIndex == 6 && !SkipGrid.IsVisible)
			{
				OnTabKeyUp(shift);
				return;
			}
		}
		if (_dateTabIndex == 9 && BatchEditButton.Visibility == Visibility.Visible)
		{
			UiUtils.SetCancelButtonTabSelected(BatchEditButton, tabSelected: true);
		}
		else
		{
			if (!ReminderOrRepeatControl.SetTabSelect(_dateTabIndex - 9))
			{
				OnTabKeyUp(shift);
				return;
			}
			UiUtils.SetCancelButtonTabSelected(BatchEditButton, tabSelected: false);
		}
		DateOrDurationSwitch.TabSelect(_dateTabIndex);
		UiUtils.SetSaveButtonTabSelected(SaveButton, _dateTabIndex == 12);
		UiUtils.SetCancelButtonTabSelected(CancelButton, _dateTabIndex == 13);
	}

	private void SetDateBorderStatus(int dateTabIndex)
	{
		Today.BorderBrush = Brushes.Transparent;
		Tomorrow.BorderBrush = Brushes.Transparent;
		NextWeek.BorderBrush = Brushes.Transparent;
		NextMonth.BorderBrush = Brushes.Transparent;
		SkipGrid.BorderBrush = Brushes.Transparent;
		switch (dateTabIndex)
		{
		case 2:
			Today.SetResourceReference(Control.BorderBrushProperty, "TabBorderColor");
			break;
		case 3:
			Tomorrow.SetResourceReference(Control.BorderBrushProperty, "TabBorderColor");
			break;
		case 4:
			NextWeek.SetResourceReference(Control.BorderBrushProperty, "TabBorderColor");
			break;
		case 5:
			NextMonth.SetResourceReference(Control.BorderBrushProperty, "TabBorderColor");
			break;
		case 6:
			SkipGrid.SetResourceReference(Control.BorderBrushProperty, "TabBorderColor");
			break;
		}
	}

	private void InitPopupEvent()
	{
		ReminderOrRepeatControl.PopupOpened += OnPopupOpened;
		ReminderOrRepeatControl.PopupClosed += OnPopupClosed;
	}

	public static SetDateDialog GetDialog(bool showTabSelected = false, EscPopup popup = null)
	{
		if (_dialog == null || popup != null)
		{
			_dialog = new SetDateDialog(popup);
		}
		_dialog.ShowTabSelected = showTabSelected;
		_dialog._inOperation = false;
		_dialog.InitTabStatus();
		_dialog._dateTabIndex = -1;
		return _dialog;
	}

	private void InitTabStatus()
	{
		DateOrDurationSwitch.ClearTabSelected();
		SetDateBorderStatus(-1);
		_dateTimePanel?.SetTabSelect(-1);
		_durationPanel?.SetTabIndex(-1);
		ReminderOrRepeatControl.SetTabSelect(-1);
		UiUtils.SetCancelButtonTabSelected(CancelButton, tabSelected: false);
		UiUtils.SetSaveButtonTabSelected(SaveButton, tabSelected: false);
	}

	public async void Show(TimeData timeData, SetDateDialogArgs args)
	{
		timeData = TimeData.Clone(timeData);
		SetPosition(args.Target, args.HOffset, args.VOffset, args.Placement);
		_popup.IsOpen = true;
		_showQuickDate = args.ShowQuickDate;
		if (args.ShowQuickDate)
		{
			bool flag = args.CanSkip && !RepeatUtils.IsNonRepeatTask(timeData.StartDate, timeData.IsAllDay, timeData.RepeatFlag, timeData.RepeatFrom, timeData.TimeZone.TimeZoneName, timeData.TimeZone.IsFloat, timeData.ExDates);
			SkipGrid.Visibility = ((!flag) ? Visibility.Collapsed : Visibility.Visible);
			SkipGridColumn.Width = (flag ? new GridLength(1.0, GridUnitType.Star) : new GridLength(0.0));
		}
		_calendarMode = args.CalendarMode;
		_showTimeZoneOption = args.CalendarMode || LocalSettings.Settings.EnableTimeZone;
		timeData = HandleTimeData(timeData, args.CalendarMode, args.IsNote, args.ItemMode);
		_originData = timeData;
		base.DataContext = TimeData.Clone(timeData);
		_noteMode = args.IsNote;
		_item = args.ItemMode;
		_showRemind = args.ShowRemind;
		ReminderOrRepeatControl.SetCalendarMode(_calendarMode, args.ShowRemind, args.ShowAnnoyingAlert);
		DateMode num = ((!_noteMode && !args.ItemMode) ? ModeFromDateData(TimeData) : DateMode.Date);
		InitShow(args.ItemMode, args.CalendarMode, args.IsNote);
		if (num == DateMode.Duration && timeData.IsTimeUnified)
		{
			DateOrDurationSwitch.SetSelectedIndex(1);
			ShowDurationDialog();
		}
		else
		{
			DateOrDurationSwitch.SetSelectedIndex(0);
			ShowDateDialog();
		}
		PopupStateManager.SetCanOpenTimePopup(canOpen: true);
	}

	private void OnPopupOpened(object sender, EventArgs e)
	{
		_inOperation = true;
	}

	private async void OnPopupClosed(object sender, EventArgs e)
	{
		_inOperation = false;
		TryFocusEmpty(null, null);
	}

	private void SetPosition(UIElement target, double hOffset, double vOffset, PlacementMode placement)
	{
		_popup.PlacementTarget = target;
		_popup.Placement = ((target == null) ? PlacementMode.Mouse : placement);
		_popup.VerticalOffset = vOffset;
		_popup.HorizontalOffset = hOffset;
	}

	private void InitShow(bool itemModeOrHideRepeat, bool calendarMode, bool noteMode)
	{
		if (itemModeOrHideRepeat)
		{
			ReminderOrRepeatControl.Visibility = Visibility.Collapsed;
			LocalReminderTimeGrid.Visibility = Visibility.Visible;
			ShowSubTaskLocalTime();
		}
		else
		{
			ReminderOrRepeatControl.Visibility = Visibility.Visible;
			LocalReminderTimeGrid.Visibility = Visibility.Collapsed;
		}
		ReminderOrRepeatControl.ShowRepeat(!noteMode);
		SwitchTitle.Visibility = ((itemModeOrHideRepeat || calendarMode) ? Visibility.Collapsed : Visibility.Visible);
		CancelButton.Content = Utils.GetString(calendarMode ? "Cancel" : "PublicClear");
		DateOrDurationSwitch.Visibility = ((calendarMode || noteMode) ? Visibility.Collapsed : Visibility.Visible);
		Title.Visibility = ((!noteMode) ? Visibility.Collapsed : Visibility.Visible);
		if (noteMode)
		{
			LocalReminderTimeGrid.Visibility = Visibility.Visible;
			ShowSubTaskLocalTime();
		}
	}

	private void ShowDateDialog()
	{
		if (_dateTimePanel == null)
		{
			InitDateTimeControl();
		}
		_dateTimePanel.ForceShowTimeZone = _showTimeZoneOption;
		_dateTimePanel.DirectSelectTimeZone = _calendarMode;
		TimeData timeData = ((!TimeData.StartDate.HasValue) ? TimeData.InitDefaultTime() : TimeData.Clone(TimeData));
		timeData.TimeZone = TimeData.TimeZone;
		timeData.BatchData = TimeData.BatchData;
		base.DataContext = timeData;
		QuickDatePanel.Visibility = ((!_showQuickDate) ? Visibility.Collapsed : Visibility.Visible);
		DateTimeBorder.Margin = (_showQuickDate ? new Thickness(0.0) : new Thickness(0.0, 10.0, 0.0, 0.0));
		if (timeData.ChangedDateOnly || !timeData.IsTimeUnified)
		{
			ReminderBorder.Visibility = Visibility.Collapsed;
			BatchEditButton.Visibility = Visibility.Visible;
			_dateTimePanel.SetTimeInputVisible(b: false);
		}
		else
		{
			ReminderBorder.Visibility = Visibility.Visible;
			BatchEditButton.Visibility = Visibility.Collapsed;
			_dateTimePanel.SetTimeInputVisible(b: true);
		}
		_dateTimePanel.TimePointControl.CloseReminderPopup();
		if (TimeData.DueDate.HasValue)
		{
			TimeData.DueDate = null;
		}
		TimeData.RemoveEndReminders();
		_dateTimePanel.TimePointControl.EnableEditTimeZone = !_item;
		DateTimeBorder.Visibility = Visibility.Visible;
		DurationBorder.Visibility = Visibility.Collapsed;
		_dateTimePanel.SetData();
		if (ShowTabSelected)
		{
			if (_dateTabIndex < 0)
			{
				_dateTabIndex = 0;
				DateOrDurationSwitch.TabSelectFirst();
				if (DateOrDurationSwitch.Visibility != Visibility.Visible)
				{
					OnTabKeyUp(shift: false);
				}
			}
			EmptyBox.Focus();
		}
		else
		{
			EmptyBox.Focus();
		}
	}

	public void ShowSubTaskLocalTime()
	{
		if ((!TimeData.IsAllDay.HasValue || !TimeData.IsAllDay.Value) && TimeData.StartDate.HasValue)
		{
			LocalReminderTime.Visibility = Visibility.Visible;
			DateTime value = TimeData.StartDate.Value;
			string timeFormatString = DateUtils.GetTimeFormatString();
			if (LocalSettings.Settings.EnableTimeZone && TimeData.TimeZone.TimeZoneName != TimeZoneData.LocalTimeZoneModel?.TimeZoneName)
			{
				value = (TimeData.TimeZone.IsFloat ? TimeData.StartDate.Value : TimeZoneUtils.ToLocalTime(TimeData.StartDate.Value, TimeData.TimeZone.TimeZoneName));
				LocalReminderTime.Text = string.Format(Utils.GetString("RemindTime"), DateUtils.FormatMonthDay(value, DateFormatLen.Long), value.ToString(timeFormatString, App.Ci), Utils.GetString("LocalTimeZone"));
			}
			else
			{
				LocalReminderTime.Text = string.Format(Utils.GetString("RemindTime"), DateUtils.FormatMonthDay(value, DateFormatLen.Long), value.ToString(timeFormatString, App.Ci), "");
			}
		}
		else
		{
			LocalReminderTime.Visibility = Visibility.Collapsed;
		}
	}

	private void OnDateSelected(object sender, DateTime e)
	{
		ShowSubTaskLocalTime();
	}

	private TimeData HandleTimeData(TimeData timeData, bool calendarMode, bool isNote, bool itemMode)
	{
		if (timeData.IsAllDay != true && !timeData.TimeZone.IsFloat)
		{
			if (LocalSettings.Settings.EnableTimeZone || calendarMode)
			{
				timeData = TimeZoneUtils.TransferToSelectedTimeZoneTime(timeData);
			}
			else
			{
				timeData.TimeZone = TimeZoneData.LocalTimeZoneModel;
			}
		}
		if (timeData.IsAllDay == true && !itemMode)
		{
			timeData.TimeZone = TimeZoneData.LocalTimeZoneModel;
		}
		HandleAllDay(timeData, -1);
		if (!calendarMode)
		{
			HandleTaskDefault(timeData, itemMode);
		}
		else if (!timeData.DueDate.HasValue)
		{
			timeData.DueDate = timeData.StartDate;
		}
		if (timeData.IsDefault && calendarMode)
		{
			timeData.Reminders = ((timeData.IsAllDay ?? true) ? TimeData.GetDefaultAllDayReminders() : TimeData.GetDefaultTimeReminders());
		}
		if (isNote || itemMode)
		{
			timeData.DueDate = null;
		}
		if (timeData.BatchData != null)
		{
			if (!timeData.IsDefault)
			{
				timeData.StartDate = ((timeData.BatchData.IsDateUnified && timeData.BatchData.StartDate.HasValue) ? timeData.BatchData.StartDate : new DateTime?(TaskDefaultModel.GetDefaultDate(TaskDefaultDao.GetDefaultSafely()?.Date) ?? DateTime.Today));
			}
			if (timeData.BatchData.IsTimeUnified && timeData.BatchData.StartDate.HasValue)
			{
				timeData.TimeZone = new TimeZoneViewModel(timeData.BatchData.IsFloating, timeData.BatchData.TimeZone);
				timeData.IsAllDay = timeData.BatchData.IsAllDay;
				if (!(timeData.IsAllDay ?? true) && timeData.StartDate.HasValue)
				{
					timeData.StartDate = new DateTime(timeData.StartDate.Value.Year, timeData.StartDate.Value.Month, timeData.StartDate.Value.Day, timeData.BatchData.StartDate.Value.Hour, timeData.BatchData.StartDate.Value.Minute, 0);
				}
				if (timeData.BatchData.IsReminderUnified)
				{
					timeData.Reminders = timeData.BatchData.Reminders;
				}
				else
				{
					timeData.Reminders = ((timeData.IsAllDay ?? true) ? TimeData.GetDefaultAllDayReminders() : TimeData.GetDefaultTimeReminders());
				}
			}
			if (timeData.BatchData.IsRepeatUnified)
			{
				timeData.RepeatFlag = timeData.BatchData.RepeatFlag;
				timeData.RepeatFrom = timeData.BatchData.RepeatFrom;
			}
			else
			{
				timeData.RepeatFlag = null;
				timeData.RepeatFrom = null;
			}
		}
		return timeData;
	}

	private static void HandleAllDay(TimeData timeData, int offset)
	{
		if (timeData == null)
		{
			timeData = new TimeData();
		}
		if (timeData.IsAllDay.HasValue && timeData.IsAllDay.Value && timeData.DueDate.HasValue && timeData.StartDate.HasValue)
		{
			timeData.DueDate = timeData.DueDate.Value.AddDays(offset);
			if (timeData.DueDate.Value.Date == timeData.StartDate.Value.Date)
			{
				timeData.DueDate = null;
			}
		}
	}

	private static void HandleTaskDefault(TimeData timeData, bool itemMode)
	{
		if (!timeData.IsDefault)
		{
			return;
		}
		TaskDefaultModel defaultSafely = TaskDefaultDao.GetDefaultSafely();
		if (defaultSafely != null && defaultSafely.DateMode == 1 && UserDao.IsPro())
		{
			timeData.IsAllDay = Constants.DurationValue.IsAllDayValue(defaultSafely.Duration) || itemMode;
			timeData.StartDate = ((!timeData.IsAllDay.Value) ? new DateTime?(DateUtils.GetNextHour(timeData.StartDate ?? defaultSafely.GetDefaultDateTime() ?? DateTime.Today)) : (timeData.StartDate ?? defaultSafely.GetDefaultDateTime()));
			if (timeData.StartDate.HasValue)
			{
				timeData.DueDate = timeData.StartDate.Value.AddMinutes(defaultSafely.Duration);
			}
		}
		timeData.Reminders = ((timeData.IsAllDay.HasValue && timeData.IsAllDay.Value) ? TimeData.GetDefaultAllDayReminders() : TimeData.GetDefaultTimeReminders());
	}

	private static DateMode ModeFromDateData(TimeData timeData)
	{
		if (timeData.DueDate.HasValue && !Utils.IsEmptyDate(timeData.DueDate))
		{
			return DateMode.Duration;
		}
		if (timeData.IsDefault && TaskDefaultDao.GetDefaultSafely().DateMode == 1 && UserDao.IsPro())
		{
			if (!timeData.DueDate.HasValue)
			{
				timeData.DueDate = timeData.StartDate;
			}
			return DateMode.Duration;
		}
		return DateMode.Date;
	}

	private void ShowDurationDialog()
	{
		if (_durationPanel == null)
		{
			InitDurationControl();
		}
		_durationPanel.ShowTimeZoneOption = _showTimeZoneOption;
		_durationPanel.DirectSelectTimeZone = _calendarMode;
		TimeData timeData = ((!TimeData.StartDate.HasValue) ? TimeData.InitDefaultDuration() : TimeData.Clone(TimeData));
		HandleAllDayBeforeSave(timeData, 0);
		timeData.BatchData = TimeData.BatchData;
		if (timeData.ChangedDateOnly)
		{
			ReminderBorder.Visibility = Visibility.Collapsed;
			BatchEditButton.Visibility = Visibility.Visible;
		}
		else
		{
			ReminderBorder.Visibility = Visibility.Visible;
			BatchEditButton.Visibility = Visibility.Collapsed;
		}
		QuickDatePanel.Visibility = Visibility.Collapsed;
		base.DataContext = timeData;
		if (!TimeData.IsDefault)
		{
			if (TimeData.IsAllDay.HasValue && TimeData.IsAllDay.Value)
			{
				if (TimeData.StartDate.HasValue && !TimeData.DueDate.HasValue)
				{
					int hour = DateTime.Now.Hour;
					TimeData.StartDate = DateUtils.SetHourAndMinuteOnly(TimeData.StartDate.Value, hour, 0).AddHours(1.0);
					TimeData.DueDate = TimeData.StartDate.Value.AddHours(1.0);
				}
			}
			else if (TimeData.StartDate.HasValue && !TimeData.DueDate.HasValue)
			{
				TimeData.DueDate = TimeData.StartDate.Value.AddHours(1.0);
			}
		}
		ReminderOrRepeatControl.IsDateSelected = true;
		ReminderOrRepeatControl.SelectRepeatButton.Visibility = (_item ? Visibility.Collapsed : Visibility.Visible);
		DateTimeBorder.Visibility = Visibility.Collapsed;
		DurationBorder.Visibility = Visibility.Visible;
		_durationPanel.SetData();
		if (ShowTabSelected)
		{
			if (_dateTabIndex < 0)
			{
				_dateTabIndex = 1;
				DateOrDurationSwitch.TabSelectLast();
			}
			EmptyBox.Focus();
		}
		else
		{
			EmptyBox.Focus();
		}
	}

	private void SwitchDurationClick(object sender, MouseButtonEventArgs e)
	{
		if (DurationBorder.IsVisible || !ProChecker.CheckPro(ProType.Duration))
		{
			return;
		}
		int num = TaskDefaultDao.GetDefaultSafely().Duration;
		if (num == 0)
		{
			num = 60;
		}
		if (TimeData.IsAllDay.HasValue && TimeData.IsAllDay.Value)
		{
			int hour = DateTime.Now.Hour;
			int minute = DateTime.Now.Minute;
			DateTime modify = _dateTimePanel?.Calendar.SelectedDate ?? DateTime.Today;
			TimeData.StartDate = modify.Date;
			TimeData.StartDate = DateUtils.SetHourAndMinuteOnly(TimeData.StartDate.Value, hour, 0).AddMinutes((minute >= 30) ? 60 : 30);
			if (TimeData.StartDate.Value.Date != modify.Date && modify.Date != DateTime.Today)
			{
				TimeData.StartDate = DateUtils.SetDateOnly(TimeData.StartDate.Value, modify);
			}
			TimeData.DueDate = TimeData.StartDate.Value.AddMinutes(num);
			TimeData.Reminders = TimeData.GetDefaultTimeReminders();
		}
		else
		{
			TimeData timeData = TimeData;
			if (timeData != null && timeData.StartDate.HasValue)
			{
				TimeData.DueDate = TimeData.StartDate.Value.AddMinutes(num);
			}
		}
		TimeData.IsAllDay = false;
		TimeData.BatchData?.SetUnified();
		ShowDurationDialog();
		_dateTabIndex = 1;
	}

	private void SwitchDateClick(object sender, MouseButtonEventArgs e)
	{
		if (!DateTimeBorder.IsVisible)
		{
			TimeData.BatchData?.SetUnified();
			ShowDateDialog();
			_dateTabIndex = 0;
		}
	}

	private void CancelClick(object sender, RoutedEventArgs e)
	{
		this.Clear?.Invoke(this, null);
		_popup.IsOpen = false;
	}

	public async void SaveClick(object sender, RoutedEventArgs e)
	{
		await Task.Delay(50);
		TimeData timeData = TimeData.Clone(TimeData);
		if (!timeData.StartDate.HasValue && DateTimeBorder.Visibility == Visibility.Visible)
		{
			timeData.StartDate = _dateTimePanel.Calendar.SelectedDate;
		}
		if (timeData.IsAllDay == true)
		{
			timeData.TimeZone = TimeZoneData.LocalTimeZoneModel;
		}
		HandleAllDayBeforeSave(timeData, 1);
		if ((!timeData.IsAllDay.HasValue || !timeData.IsAllDay.Value) && !timeData.TimeZone.IsFloat)
		{
			timeData = TimeZoneUtils.TransferToLocalTime(timeData);
		}
		if (!LocalSettings.Settings.EnableTimeZone && !_calendarMode)
		{
			timeData.TimeZone = TimeZoneData.LocalTimeZoneModel;
		}
		if (!_showRemind)
		{
			timeData.Reminders = new List<TaskReminderModel>();
		}
		this.Save?.Invoke(this, timeData);
		UtilLog.Info("SetDateDialog.Save " + timeData);
		_popup.IsOpen = false;
	}

	private static void HandleAllDayBeforeSave(TimeData data, int addDayDiff)
	{
		if (data.IsAllDay.HasValue && data.IsAllDay.Value)
		{
			if (data.StartDate.HasValue)
			{
				data.StartDate = DateUtils.CropHourAndMinute(data.StartDate.Value);
			}
			if (data.DueDate.HasValue)
			{
				data.DueDate = data.DueDate.Value.AddDays(addDayDiff);
				data.DueDate = DateUtils.CropHourAndMinute(data.DueDate.Value);
			}
		}
	}

	public void OnSaved(TimeData timeData)
	{
		if (!_showRemind)
		{
			timeData.Reminders = null;
		}
		this.Save?.Invoke(this, timeData);
	}

	public void BindRepeatChangedEvent(EventHandler onRepeatChanged)
	{
		ReminderOrRepeatControl.RepeatChanged -= onRepeatChanged;
		ReminderOrRepeatControl.RepeatChanged += onRepeatChanged;
	}

	public void ClearEventHandle()
	{
		this.Save = null;
		this.Clear = null;
		this.Hided = null;
		this.SkipRecurrence = null;
	}

	private void TryFocusEmpty(object sender, MouseButtonEventArgs e)
	{
		if ((_durationPanel == null || (!_durationPanel.DueTimeInput.IsMouseOver && !_durationPanel.StartTimeInput.IsMouseOver)) && (_dateTimePanel == null || !_dateTimePanel.TimePointControl.IsMouseOver))
		{
			EmptyBox.Focus();
		}
	}

	private void OnGridClick(object sender, MouseButtonEventArgs e)
	{
		if (sender is FrameworkElement { Tag: string tag })
		{
			QuickSetDate(tag);
		}
		_popup.IsOpen = false;
	}

	private void QuickSetDate(string tag)
	{
		DateTime date = DateTime.Today;
		switch (tag)
		{
		case "Tomorrow":
			date = date.AddDays(1.0);
			break;
		case "NextWeek":
			date = date.AddDays(7.0);
			break;
		case "NextMonth":
			date = date.AddMonths(1);
			break;
		case "SkipCurrentRecurrence":
			this.SkipRecurrence?.Invoke(this, null);
			_popup.IsOpen = false;
			return;
		}
		TimeData timeData = TimeData.Clone(TimeData);
		timeData.SetDate(date);
		this.Save?.Invoke(this, timeData);
	}

	public bool IsOpen()
	{
		return _popup?.IsOpen ?? false;
	}

	public void TryClose()
	{
		if (_popup.IsOpen)
		{
			_popup.IsOpen = false;
			ClearEventHandle();
			_dialog = null;
		}
	}

	private void BatchSetClick(object sender, RoutedEventArgs e)
	{
		TimeData.BatchData?.SetUnified();
		ReminderBorder.Visibility = Visibility.Visible;
		BatchEditButton.Visibility = Visibility.Collapsed;
		_dateTimePanel?.SetTimeInputVisible(b: true);
	}

	private void OnDateOrDurationSwitchChanged(object sender, GroupTitleViewModel e)
	{
		if ("Duration".Equals(e.Title))
		{
			SwitchDurationClick(null, null);
		}
		else
		{
			SwitchDateClick(null, null);
		}
	}

	private void OnMouseUp(object sender, MouseButtonEventArgs e)
	{
		if (ShowTabSelected)
		{
			ShowTabSelected = false;
			InitTabStatus();
		}
	}

	public bool HandleTab(bool shift)
	{
		if (ReminderOrRepeatControl.TabHandled(shift))
		{
			return true;
		}
		if (_inOperation)
		{
			return true;
		}
		OnTabKeyUp(shift);
		EmptyBox.Focus();
		return true;
	}

	public bool HandleEnter()
	{
		if (ReminderOrRepeatControl.HandleEnter())
		{
			return true;
		}
		if (_inOperation && !ShowTabSelected)
		{
			_dateTimePanel?.TryClosePopup();
			_durationPanel?.TryClosePopup();
		}
		else if (ShowTabSelected)
		{
			if (_dateTabIndex >= 2 && _dateTabIndex <= 7 && DateOrDurationSwitch.GetSelectedIndex() == 1)
			{
				_durationPanel?.HandleEnter();
				return true;
			}
			switch (_dateTabIndex)
			{
			case 0:
				if (DateOrDurationSwitch.IsVisible)
				{
					DateOrDurationSwitch.SetSelectedIndex(0);
					SwitchDateClick(null, null);
				}
				break;
			case 1:
				if (DateOrDurationSwitch.IsVisible)
				{
					DateOrDurationSwitch.SetSelectedIndex(1);
					SwitchDurationClick(null, null);
				}
				break;
			case 2:
				QuickSetDate("Today");
				_popup.IsOpen = false;
				break;
			case 3:
				QuickSetDate("Tomorrow");
				_popup.IsOpen = false;
				break;
			case 4:
				QuickSetDate("NextWeek");
				_popup.IsOpen = false;
				break;
			case 5:
				QuickSetDate("NextMonth");
				_popup.IsOpen = false;
				break;
			case 6:
				QuickSetDate("SkipCurrentRecurrence");
				_popup.IsOpen = false;
				break;
			case 7:
				_dateTimePanel?.SelectTabItem();
				break;
			case 8:
				_dateTimePanel?.SelectTimeInput();
				break;
			case 9:
				if (BatchEditButton.Visibility == Visibility.Visible)
				{
					BatchSetClick(BatchEditButton, null);
					if (DurationBorder.Visibility == Visibility.Visible)
					{
						_dateTabIndex = 9;
						ReminderOrRepeatControl.SetTabSelect(0);
					}
					else
					{
						_dateTabIndex = 8;
						_dateTimePanel?.SetTabSelect(1);
					}
					break;
				}
				goto case 10;
			case 10:
			case 11:
				if (ReminderOrRepeatControl.IsVisible)
				{
					ReminderOrRepeatControl.HandleKeyUp(Key.Return);
				}
				break;
			case 12:
				SaveClick(this, null);
				break;
			case 13:
				this.Clear?.Invoke(this, null);
				_popup.IsOpen = false;
				break;
			}
			return true;
		}
		return false;
	}

	public bool HandleEsc()
	{
		if (ReminderOrRepeatControl.HandleEsc())
		{
			return true;
		}
		SetDateTimeControl dateTimePanel = _dateTimePanel;
		if (dateTimePanel != null && dateTimePanel.HandleEsc())
		{
			return true;
		}
		SetDateDurationControl durationPanel = _durationPanel;
		if (durationPanel != null && durationPanel.HandleEsc())
		{
			return true;
		}
		if (_inOperation)
		{
			_dateTimePanel?.TryClosePopup();
			ReminderOrRepeatControl.TryClosePopup();
			EmptyBox.Focus();
		}
		return false;
	}

	public bool UpDownSelect(bool isUp)
	{
		_durationPanel?.HandleUpDown(isUp);
		if (ShowTabSelected && _dateTabIndex == 7)
		{
			_dateTimePanel?.MoveTabSelectDate(isUp ? (-7) : 7);
		}
		if (!ShowTabSelected || _dateTabIndex >= 9)
		{
			return ReminderOrRepeatControl.HandleUpDownSelect(isUp);
		}
		return true;
	}

	public bool LeftRightSelect(bool isLeft)
	{
		_durationPanel?.HandleLeftRight(isLeft);
		if (ShowTabSelected && _dateTabIndex == 7)
		{
			_dateTimePanel?.MoveTabSelectDate((!isLeft) ? 1 : (-1));
			return true;
		}
		if (!ShowTabSelected || _dateTabIndex >= 9)
		{
			return ReminderOrRepeatControl.HandleLeftRightSelect(isLeft);
		}
		return false;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/time/setdatedialog.xaml", UriKind.Relative);
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
			((SetDateDialog)target).MouseLeftButtonDown += OnMouseUp;
			break;
		case 2:
			Container = (ContentControl)target;
			Container.PreviewMouseLeftButtonDown += TryFocusEmpty;
			break;
		case 3:
			EmptyBox = (TextBox)target;
			break;
		case 4:
			SwitchTitle = (Grid)target;
			break;
		case 5:
			DateOrDurationSwitch = (GroupTitle2)target;
			break;
		case 6:
			Title = (TextBlock)target;
			break;
		case 7:
			QuickDatePanel = (Grid)target;
			break;
		case 8:
			SkipGridColumn = (ColumnDefinition)target;
			break;
		case 9:
			Today = (Border)target;
			Today.MouseLeftButtonUp += OnGridClick;
			break;
		case 10:
			Tomorrow = (Border)target;
			Tomorrow.MouseLeftButtonUp += OnGridClick;
			break;
		case 11:
			NextWeek = (Border)target;
			NextWeek.MouseLeftButtonUp += OnGridClick;
			break;
		case 12:
			NextMonth = (Border)target;
			NextMonth.MouseLeftButtonUp += OnGridClick;
			break;
		case 13:
			SkipGrid = (Border)target;
			SkipGrid.MouseLeftButtonUp += OnGridClick;
			break;
		case 14:
			DateTimeBorder = (Border)target;
			break;
		case 15:
			DurationBorder = (Border)target;
			break;
		case 16:
			BatchEditButton = (Button)target;
			BatchEditButton.Click += BatchSetClick;
			break;
		case 17:
			ReminderBorder = (Border)target;
			break;
		case 18:
			ReminderOrRepeatControl = (SelectReminderOrRepeatControl)target;
			break;
		case 19:
			LocalReminderTimeGrid = (Grid)target;
			break;
		case 20:
			LocalReminderTime = (TextBlock)target;
			break;
		case 21:
			SaveButton = (Button)target;
			SaveButton.Click += SaveClick;
			break;
		case 22:
			CancelButton = (Button)target;
			CancelButton.Click += CancelClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
