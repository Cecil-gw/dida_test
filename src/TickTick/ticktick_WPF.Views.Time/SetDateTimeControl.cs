using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Shapes;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Calendar;

namespace ticktick_WPF.Views.Time;

public class SetDateTimeControl : UserControl, IComponentConnector
{
	public static readonly DependencyProperty TimeTabSelectProperty = DependencyProperty.Register("TimeTabSelect", typeof(bool), typeof(SetDateTimeControl), new PropertyMetadata(false));

	public SetDateDialog ParentWindow;

	private bool _showPopupMouseDown;

	private int _index;

	internal SetDateTimeControl Root;

	internal TickDatePicker Calendar;

	internal StackPanel TimeInputPanel;

	internal Border DateBorder;

	internal TimeInputControl TimePointControl;

	internal Path RightIcon;

	private bool _contentLoaded;

	public bool TimeTabSelect
	{
		get
		{
			return (bool)GetValue(TimeTabSelectProperty);
		}
		set
		{
			SetValue(TimeTabSelectProperty, value);
		}
	}

	public TimeData TimeData => (TimeData)base.DataContext;

	public bool ForceShowTimeZone { get; set; }

	public bool DirectSelectTimeZone { get; set; }

	private bool IsChoosed { get; set; }

	public event EventHandler<DateTime> DateSelected;

	public SetDateTimeControl()
	{
		InitializeComponent();
		TimePointControl.SelectedTimeChanged -= OnTimeSelected;
		TimePointControl.SelectedTimeChanged += OnTimeSelected;
		TimePointControl.TimeZoneChanged += delegate(object sender, TimeZoneViewModel tzModel)
		{
			TimeData.TimeZone = tzModel;
			Calendar.TimeZone = (((TimeData.IsAllDay.HasValue && !TimeData.IsAllDay.Value) || !TimeData.TimeZone.IsFloat) ? TimeData.TimeZone.TimeZoneName : null);
		};
		TimePointControl.ReminderPopup.HorizontalOffset = -44.0;
		TimePointControl.ReminderPopup.VerticalOffset = -4.0;
	}

	public void SetData()
	{
		TimePointControl.ForceShowTimeZone = ForceShowTimeZone;
		TimePointControl.DirectSelectTimeZone = DirectSelectTimeZone;
		InitTime();
		InitSelectedDate();
	}

	private void InitSelectedDate()
	{
		bool num = (TimeData.StartDate.HasValue && TimeData.StartDate.Value.Date != DateTime.Today) || (!TimeData.IsDefault && TimeData.StartDate.HasValue);
		Calendar.TrySetDayCells();
		if (num)
		{
			ParentWindow.ReminderOrRepeatControl.IsDateSelected = true;
			Calendar.SelectedDate = TimeData.StartDate.Value;
		}
		else
		{
			ParentWindow.ReminderOrRepeatControl.IsDateSelected = false;
			if ((TimeData.IsAllDay.HasValue && !TimeData.IsAllDay.Value) || !TimeData.TimeZone.IsFloat)
			{
				Calendar.SelectedDate = TimeZoneUtils.LocalToTargetTzTime(DateTime.Now, TimeData.TimeZone.TimeZoneName).Date;
			}
			else
			{
				Calendar.SelectedDate = DateTime.Today;
			}
		}
		Calendar.RepeatFrom = TimeData.RepeatFrom;
		Calendar.RepeatFlag = TimeData.RepeatFlag;
		Calendar.ExDates = TimeData.ExDates;
		Calendar.TimeZone = (((TimeData.IsAllDay.HasValue && !TimeData.IsAllDay.Value) || !TimeData.TimeZone.IsFloat) ? TimeData.TimeZone.TimeZoneName : null);
		Calendar.IsChoosed = false;
		Calendar.RenderRepeat();
	}

	private void InitTime()
	{
		if (TimeData.IsAllDay.HasValue && !TimeData.IsAllDay.Value)
		{
			if (TimeData.StartDate.HasValue)
			{
				TimePointControl.SelectedTime = TimeData.StartDate.Value;
				TimePointControl.Visibility = Visibility.Visible;
			}
		}
		else
		{
			TimePointControl.SelectedTime = DateUtils.GetNextHour(TimeData.TimeZone.IsFloat ? "" : TimeData.TimeZone.TimeZoneName);
			TimePointControl.Visibility = Visibility.Collapsed;
		}
		TimePointControl.TimeZone = TimeData.TimeZone;
		ParentWindow?.BindRepeatChangedEvent(OnRepeatChanged);
	}

	private void OnTimeSelected(object sender, DateTime time)
	{
		TimeData.StartDate = DateUtils.SetHourAndMinuteOnly(TimeData.StartDate ?? DateTime.Now, time.Hour, time.Minute);
		if (ParentWindow != null && ParentWindow.LocalReminderTimeGrid.IsVisible)
		{
			ParentWindow.ShowSubTaskLocalTime();
		}
	}

	private void OnRepeatChanged(object sender, EventArgs obj)
	{
		bool num = IsRepeatFlagChanged(Calendar.RepeatFlag, TimeData.RepeatFlag) || Calendar.RepeatFrom != TimeData.RepeatFrom;
		Calendar.RepeatFlag = TimeData.RepeatFlag;
		Calendar.RepeatFrom = TimeData.RepeatFrom;
		Calendar.SelectedDateChanged -= OnSelectedDateChanged;
		if (sender is CustomRepeatDialog)
		{
			Calendar.SelectedDate = TimeData.StartDate;
		}
		if (Utils.IsEmptyDate(Calendar.SelectedDate))
		{
			TimeData.StartDate = DateTime.Today;
			Calendar.SelectedDate = DateTime.Today;
		}
		Calendar.SelectedDateChanged += OnSelectedDateChanged;
		if (num)
		{
			Calendar.ExDates = new List<string>();
		}
		Calendar.RenderRepeat();
		IsChoosed = true;
	}

	private static bool IsRepeatFlagChanged(string original, string revised)
	{
		if (original == null)
		{
			original = string.Empty;
		}
		if (revised == null)
		{
			revised = string.Empty;
		}
		if (original == revised)
		{
			return false;
		}
		if (revised.StartsWith(original))
		{
			string text = revised.Substring(0, original.Length);
			if (text.ToUpper().StartsWith("COUNT") || text.ToUpper().StartsWith("UNTIL"))
			{
				return false;
			}
		}
		return true;
	}

	private void OnSelectedDateChanged(object sender, DateTime dateTime)
	{
		OnDateSelected(dateTime);
	}

	private void OnDateSelected(DateTime dateTime)
	{
		IsChoosed = Calendar.IsChoosed;
		ParentWindow.ReminderOrRepeatControl.IsDateSelected = true;
		if (TimeData.StartDate.HasValue)
		{
			TimeData.StartDate = DateUtils.SetDateOnly(TimeData.StartDate.Value, dateTime);
		}
		else
		{
			TimeData.StartDate = dateTime;
		}
		if (TimeData.RepeatFlag != null)
		{
			TaskService.TryFixRepeatFlag(TimeData, !IsChoosed);
		}
		this.DateSelected?.Invoke(this, dateTime);
	}

	private void OnDateSelectedAndSave(object sender, DateTime e)
	{
		OnDateSelected(e);
		OnSaveClick(null, null);
	}

	private async void OnSaveClick(object o, object o1)
	{
		if (TimePointControl.IsVisible)
		{
			TimePointControl.FocusEmpty();
			await Task.Delay(100);
		}
		if (!TimeData.StartDate.HasValue)
		{
			TimeData.StartDate = Calendar.SelectedDate;
		}
		if (Calendar.SelectedDate.HasValue && TimeData.StartDate.HasValue)
		{
			TimeData.StartDate = DateUtils.SetDateOnly(TimeData.StartDate.Value, Calendar.SelectedDate.Value);
			if (TimeData.IsAllDay == true)
			{
				TimeData.StartDate = DateUtils.CropHourAndMinute(TimeData.StartDate.Value);
			}
		}
		ParentWindow?.OnSaved(TimeData);
	}

	private void SelectTimeClick(object sender, MouseButtonEventArgs e)
	{
		if (!TimePointControl.IsMouseOver && !TimePointControl.PopupOpen && _showPopupMouseDown)
		{
			TimePointControl.Visibility = Visibility.Visible;
			TimePointControl.ToggleReminderPopup();
			TimePointControl.FoucusHourText();
			if (!TimeData.IsAllDay.HasValue || TimeData.IsAllDay.Value)
			{
				TimeData.IsAllDay = false;
				TimeData.Reminders = TimeData.GetDefaultTimeReminders();
				DateTime selectedTime = TimePointControl.SelectedTime;
				TimeData.StartDate = DateUtils.SetHourAndMinuteOnly(TimeData.StartDate ?? DateTime.Now, selectedTime.Hour, selectedTime.Minute);
			}
			SetDateDialog parentWindow = ParentWindow;
			if (parentWindow != null && parentWindow.LocalReminderTimeGrid.IsVisible)
			{
				ParentWindow?.ShowSubTaskLocalTime();
			}
			_showPopupMouseDown = false;
		}
	}

	private void OnClearTimeClick(object sender, MouseButtonEventArgs e)
	{
		if (!(TimeData.IsAllDay ?? true))
		{
			e.Handled = true;
			ClearTime();
		}
	}

	public void ClearTime()
	{
		if (!(TimeData.IsAllDay ?? true))
		{
			TimeData.IsAllDay = true;
			TimeData.Reminders = null;
			TimeData.AnnoyingAlert = -1;
			if (ParentWindow != null && !ParentWindow.LocalReminderTimeGrid.IsVisible)
			{
				TimeData.TimeZone = TimeZoneData.LocalTimeZoneModel;
			}
			TimePointControl.Visibility = Visibility.Collapsed;
			TimePointControl.HidePopup();
		}
	}

	public void TryClosePopup()
	{
		if (!TimePointControl.TryCloseTimezonePopup())
		{
			TimePointControl.DropdownPopup.IsOpen = false;
		}
	}

	public void SetTimeInputVisible(bool b)
	{
		TimeInputPanel.Visibility = ((!b) ? Visibility.Collapsed : Visibility.Visible);
	}

	private void OnShowPopupButtonMouseDown(object sender, MouseButtonEventArgs e)
	{
		if (PopupStateManager.CanOpenTimePopup)
		{
			_showPopupMouseDown = true;
		}
		else
		{
			PopupStateManager.SetCanOpenTimePopup(canOpen: true);
		}
	}

	public bool SetTabSelect(int index)
	{
		switch (index)
		{
		case 0:
			Calendar.TabSelectCurrent();
			TimeTabSelect = false;
			return true;
		case 1:
			TimeTabSelect = true;
			Calendar.ClearTabSelected();
			return TimeInputPanel.Visibility == Visibility.Visible;
		default:
			TimeTabSelect = false;
			Calendar.ClearTabSelected();
			return true;
		}
	}

	public void MoveTabSelectDate(int step = 1)
	{
		Calendar.MoveTabSelectDate(step);
	}

	public void SelectTabItem()
	{
		Calendar.SelectTabItem();
	}

	public void SelectTimeInput()
	{
		_showPopupMouseDown = true;
		SelectTimeClick(null, null);
	}

	public bool HandleEsc()
	{
		if (TimePointControl.TryCloseTimezonePopup())
		{
			return true;
		}
		if (TimePointControl.ReminderPopup.IsOpen)
		{
			TimePointControl.ReminderPopup.IsOpen = false;
			return true;
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
			Uri resourceLocator = new Uri("/TickTick;component/views/time/setdatetimecontrol.xaml", UriKind.Relative);
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
			Root = (SetDateTimeControl)target;
			break;
		case 2:
			Calendar = (TickDatePicker)target;
			break;
		case 3:
			TimeInputPanel = (StackPanel)target;
			break;
		case 4:
			DateBorder = (Border)target;
			DateBorder.MouseLeftButtonDown += OnShowPopupButtonMouseDown;
			DateBorder.MouseLeftButtonUp += SelectTimeClick;
			break;
		case 5:
			TimePointControl = (TimeInputControl)target;
			break;
		case 6:
			((Border)target).MouseLeftButtonUp += OnClearTimeClick;
			break;
		case 7:
			RightIcon = (Path)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
