using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;
using ticktick_WPF.Models;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Calendar;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Views.Time;

public class SelectReminderOrRepeatControl : UserControl, IComponentConnector
{
	private bool _calendarMode;

	private SetRepeatDialog _repeatDialog;

	public bool IsDateSelected;

	private bool _showRemind = true;

	private bool _showAnnoyingAlert = true;

	private bool _showPopupButtonMouseDown;

	public static readonly DependencyProperty TabSelectedIndexProperty = DependencyProperty.Register("TabSelectedIndex", typeof(int), typeof(SelectReminderOrRepeatControl), new PropertyMetadata(-1, null));

	internal SelectReminderOrRepeatControl Root;

	internal Border SelectReminderBtn;

	internal TextBlock EmptyRemindText;

	internal TextBlock ReminderText;

	internal Path ReminderRightPath;

	internal EscPopup SelectReminderPopup;

	internal EscPopup SelectRepeatPopup;

	internal Border SelectRepeatButton;

	internal TextBlock EmptyRepeatText;

	internal TextBlock RepeatText;

	internal Path RepeatRightPath;

	internal Border RepeatEndBt;

	internal Grid RepeatEndGrid;

	internal TextBlock EmptyRepeatEndText;

	internal TextBlock RepeatEndText;

	internal Grid ClearPanel;

	internal Path RepeatEndRightPath;

	internal EscPopup SetRepeatEndPopup;

	internal SetRepeatEndControl SetRepeatEndControl;

	private bool _contentLoaded;

	public bool ShowRepeatEnd => RepeatEndGrid.IsVisible;

	public int TabSelectedIndex
	{
		get
		{
			return (int)GetValue(TabSelectedIndexProperty);
		}
		set
		{
			SetValue(TabSelectedIndexProperty, value);
		}
	}

	private TimeData TimeData => (TimeData)base.DataContext;

	public event EventHandler PopupOpened;

	public event EventHandler PopupClosed;

	public event EventHandler RepeatChanged;

	public SelectReminderOrRepeatControl()
	{
		InitializeComponent();
	}

	private void Root_Loaded(object sender, RoutedEventArgs e)
	{
		Path reminderRightPath = ReminderRightPath;
		Path repeatRightPath = RepeatRightPath;
		Transform transform = (RepeatEndRightPath.RenderTransform = new RotateTransform(-90.0));
		Transform renderTransform = (repeatRightPath.RenderTransform = transform);
		reminderRightPath.RenderTransform = renderTransform;
	}

	private void SelectReminderClick(object sender, MouseButtonEventArgs eventArgs)
	{
		if (_showPopupButtonMouseDown)
		{
			ShowReminderPopup(enterSelect: false);
		}
	}

	private void ShowReminderPopup(bool enterSelect)
	{
		DismissInnerPopups();
		bool flag = TimeData.IsAllDay ?? true;
		bool isSpan = false;
		if (TimeData.DueDate.HasValue && !_calendarMode)
		{
			isSpan = ((!flag) ? (TimeData.DueDate != TimeData.StartDate) : (TimeData.DueDate?.Date != TimeData.StartDate?.Date));
		}
		SetReminderControl setReminderControl = new SetReminderControl(flag, TimeData.Reminders ?? new List<TaskReminderModel>(), TimeData.StartDate ?? DateTime.Today, withNull: false, (!enterSelect) ? (-1) : 0, isSpan, TimeData.AnnoyingAlert, DisplayModule.Task, _showAnnoyingAlert);
		setReminderControl.OnCancel += delegate
		{
			SelectReminderPopup.IsOpen = false;
		};
		setReminderControl.OnSelected += OnReminderSelect;
		SelectReminderPopup.Child = setReminderControl;
		SelectReminderPopup.IsOpen = true;
		_showPopupButtonMouseDown = false;
	}

	private void OnReminderSelect(object sender, ReminderSelectedEventArgs e)
	{
		DismissInnerPopups();
		List<TaskReminderModel> list = new List<TaskReminderModel>();
		if (e.Reminders != null && e.Reminders.Count > 0)
		{
			list.AddRange(e.Reminders.Select((string reminder) => new TaskReminderModel
			{
				trigger = reminder,
				id = Utils.GetGuid()
			}));
		}
		TimeData.Reminders = list;
		TimeData.AnnoyingAlert = e.AnnoyingAlert;
	}

	public void SetCalendarMode(bool calendarMode, bool showRemind, bool showAnnoyingAlert = true)
	{
		_calendarMode = calendarMode;
		_showRemind = showRemind;
		_showAnnoyingAlert = showAnnoyingAlert;
		SelectReminderBtn.Visibility = ((!showRemind) ? Visibility.Collapsed : Visibility.Visible);
	}

	private void SelectRepeatClick(object sender, MouseButtonEventArgs e)
	{
		if (_showPopupButtonMouseDown)
		{
			ShowRepeatPopup();
		}
	}

	private void ShowRepeatPopup()
	{
		_repeatDialog = new SetRepeatDialog(SelectRepeatPopup, new RepeatExtra
		{
			DefaultDate = (TimeData.StartDate ?? DateTime.Now),
			RepeatFrom = TimeData.RepeatFrom,
			RepeatFlag = TimeData.RepeatFlag
		}, _calendarMode);
		_repeatDialog.RepeatSelect += OnRepeatSelect;
		SelectRepeatPopup.IsOpen = true;
		_showPopupButtonMouseDown = false;
	}

	private void OnRepeatSelect(object sender, RepeatExtra repeat)
	{
		List<HolidayModel> cacheHolidays = HolidayManager.GetCacheHolidays();
		if (!IsDateSelected)
		{
			DateTime value = TimeData.StartDate ?? DateTime.Today;
			if (sender is CustomRepeatDialog)
			{
				value = RepeatUtils.RRule2NextDateTime(repeat.RepeatFlag, cacheHolidays, DateTime.Today.AddDays(-1.0), null, repeat.RepeatFrom).AddHours(value.Hour).AddMinutes(value.Minute);
			}
			TimeData.StartDate = value;
		}
		TimeData.RepeatFrom = repeat.RepeatFrom;
		TimeData.RepeatFlag = repeat.RepeatFlag;
		this.RepeatChanged?.Invoke(sender, null);
	}

	public void DismissInnerPopups()
	{
		SelectRepeatPopup.IsOpen = false;
		SelectReminderPopup.IsOpen = false;
	}

	private void OnPopupClick(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
	}

	private void SetRepeatEndClick(object sender, MouseButtonEventArgs e)
	{
		if (_showPopupButtonMouseDown)
		{
			ShowRepeatEndPopup(enter: false);
		}
	}

	private void ShowRepeatEndPopup(bool enter)
	{
		SetRepeatEndPopup.IsOpen = true;
		SetRepeatEndControl.Init(enter);
		_showPopupButtonMouseDown = false;
	}

	private void OnClearRepeatClick(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
		TimeData.RepeatFlag = string.Empty;
		this.RepeatChanged?.Invoke(this, null);
	}

	private void OnClearRemindClick(object sender, RoutedEventArgs e)
	{
		e.Handled = true;
		TimeData.Reminders = new List<TaskReminderModel>();
	}

	private void OnClearRepeatEndClick(object sender, RoutedEventArgs e)
	{
		e.Handled = true;
		ClearRepeatEnd();
	}

	private void ClearRepeatEnd()
	{
		SetRepeatEndPopup.IsOpen = false;
		TimeData.RepeatFlag = RepeatUtils.GetRepeatFlag(TimeData.RepeatFlag, default(DateTime));
		this.RepeatChanged?.Invoke(this, null);
	}

	public void ShowRepeat(bool show)
	{
		if (show)
		{
			SelectRepeatButton.Visibility = Visibility.Visible;
			RepeatEndBt.Visibility = Visibility.Visible;
		}
		else
		{
			SelectRepeatButton.Visibility = Visibility.Collapsed;
			RepeatEndBt.Visibility = Visibility.Collapsed;
		}
	}

	private void OnReminderPopupOpened(object sender, EventArgs e)
	{
		OnPopupOpened(sender, e);
		ReminderRightPath.RenderTransform = new RotateTransform(0.0);
	}

	private void OnReminderPopupClosed(object sender, EventArgs e)
	{
		OnPopupClosed(sender, e);
		ReminderRightPath.RenderTransform = new RotateTransform(-90.0);
	}

	private void OnRepeatPopupOpened(object sender, EventArgs e)
	{
		OnPopupOpened(sender, e);
		RepeatRightPath.RenderTransform = new RotateTransform(0.0);
	}

	private void OnRepeatPopupClosed(object sender, EventArgs e)
	{
		OnPopupClosed(sender, e);
		RepeatRightPath.RenderTransform = new RotateTransform(-90.0);
	}

	private void OnRepeatEndPopupOpened(object sender, EventArgs e)
	{
		OnPopupOpened(sender, e);
		RepeatEndRightPath.RenderTransform = new RotateTransform(0.0);
	}

	private void OnRepeatEndPopupClosed(object sender, EventArgs e)
	{
		OnPopupClosed(sender, e);
		RepeatEndRightPath.RenderTransform = new RotateTransform(-90.0);
	}

	private void OnPopupOpened(object sender, EventArgs e)
	{
		PopupStateManager.SetCanOpenTimePopup(canOpen: false);
		this.PopupOpened?.Invoke(this, null);
	}

	private void OnPopupClosed(object sender, EventArgs e)
	{
		PopupStateManager.SetCanOpenTimePopup(canOpen: true, delay: true);
		this.PopupClosed?.Invoke(this, null);
		SelectRepeatPopup.Child = null;
		_repeatDialog = null;
	}

	public void TryClosePopup()
	{
		SelectReminderPopup.IsOpen = false;
		SetRepeatEndPopup.IsOpen = false;
		SelectRepeatPopup.IsOpen = false;
		_repeatDialog?.ClosePopup();
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

	public bool SetTabSelect(int index)
	{
		if (index > 2 || index < 0)
		{
			index = -1;
		}
		else if (!base.IsVisible)
		{
			return false;
		}
		if (index != TabSelectedIndex)
		{
			TabSelectedIndex = index;
		}
		return TabSelectedIndex switch
		{
			0 => SelectReminderBtn.Visibility == Visibility.Visible, 
			1 => SelectRepeatButton.Visibility == Visibility.Visible, 
			2 => ShowRepeatEnd, 
			_ => true, 
		};
	}

	public bool HandleKeyUp(Key key)
	{
		if (key == Key.Return)
		{
			switch (TabSelectedIndex)
			{
			case 0:
				if (SelectReminderBtn.Visibility == Visibility.Visible)
				{
					ShowReminderPopup(enterSelect: true);
				}
				break;
			case 1:
				if (SelectRepeatButton.Visibility == Visibility.Visible)
				{
					ShowRepeatPopup();
				}
				break;
			case 2:
				if (ShowRepeatEnd)
				{
					ShowRepeatEndPopup(enter: true);
				}
				break;
			}
		}
		return false;
	}

	public bool TabHandled(bool shift)
	{
		if (SelectReminderPopup.IsOpen)
		{
			SelectReminderPopup.HandleTab(shift);
		}
		if (SelectRepeatPopup.IsOpen)
		{
			SelectRepeatPopup.HandleTab(shift);
		}
		if (SetRepeatEndPopup.IsOpen)
		{
			SetRepeatEndPopup.HandleTab(shift);
		}
		if (!SelectReminderPopup.IsOpen && !SelectRepeatPopup.IsOpen)
		{
			return SetRepeatEndPopup.IsOpen;
		}
		return true;
	}

	public bool HandleEnter()
	{
		if (SelectReminderPopup.IsOpen)
		{
			SelectReminderPopup.HandleEnter();
		}
		if (SelectRepeatPopup.IsOpen)
		{
			SelectRepeatPopup.HandleEnter();
		}
		if (SetRepeatEndPopup.IsOpen)
		{
			SetRepeatEndPopup.HandleEnter();
		}
		if (!SelectReminderPopup.IsOpen && !SelectRepeatPopup.IsOpen)
		{
			return SetRepeatEndPopup.IsOpen;
		}
		return true;
	}

	public bool HandleUpDownSelect(bool isUp)
	{
		if (SelectReminderPopup.IsOpen)
		{
			return SelectReminderPopup.HandleUpDown(isUp);
		}
		if (SelectRepeatPopup.IsOpen)
		{
			return SelectRepeatPopup.HandleUpDown(isUp);
		}
		if (SetRepeatEndPopup.IsOpen)
		{
			return SetRepeatEndPopup.HandleUpDown(isUp);
		}
		return true;
	}

	public bool HandleLeftRightSelect(bool isLeft)
	{
		if (SelectReminderPopup.IsOpen)
		{
			return SelectReminderPopup.HandleLeftRight(isLeft);
		}
		if (SelectRepeatPopup.IsOpen)
		{
			return SelectRepeatPopup.HandleLeftRight(isLeft);
		}
		if (SetRepeatEndPopup.IsOpen)
		{
			return SetRepeatEndPopup.HandleLeftRight(isLeft);
		}
		return true;
	}

	public bool HandleEsc()
	{
		if (SelectReminderPopup.IsOpen)
		{
			SelectReminderPopup.HandleEsc();
		}
		if (SelectRepeatPopup.IsOpen)
		{
			SelectRepeatPopup.HandleEsc();
		}
		if (SetRepeatEndPopup.IsOpen)
		{
			SetRepeatEndPopup.HandleEsc();
		}
		if (!SelectReminderPopup.IsOpen && !SelectRepeatPopup.IsOpen)
		{
			return SetRepeatEndPopup.IsOpen;
		}
		return true;
	}

	private void OnRepeatEndCancel(object sender, EventArgs e)
	{
		SetRepeatEndPopup.IsOpen = false;
	}

	private void OnRepeatEndChanged(int count, DateTime until)
	{
		if (count < 0 && until == default(DateTime))
		{
			ClearRepeatEnd();
			return;
		}
		TimeData.RepeatFlag = RepeatUtils.GetRepeatFlag(TimeData.RepeatFlag, until, (count < 0) ? (-1) : Math.Min(200, Math.Max(1, count)));
		this.RepeatChanged?.Invoke(this, null);
		SetRepeatEndPopup.IsOpen = false;
	}

	public void ClearReminderOrRepeat()
	{
		if (!SelectReminderPopup.IsOpen && !SelectRepeatPopup.IsOpen && !SetRepeatEndPopup.IsOpen)
		{
			switch (TabSelectedIndex)
			{
			case 0:
				TimeData.Reminders = new List<TaskReminderModel>();
				TimeData.AnnoyingAlert = -1;
				break;
			case 1:
				TimeData.RepeatFlag = string.Empty;
				this.RepeatChanged?.Invoke(this, null);
				break;
			case 2:
				ClearRepeatEnd();
				break;
			}
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/time/selectreminderorrepeatcontrol.xaml", UriKind.Relative);
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
			Root = (SelectReminderOrRepeatControl)target;
			Root.Loaded += Root_Loaded;
			break;
		case 2:
			((StackPanel)target).MouseLeftButtonUp += OnPopupClick;
			break;
		case 3:
			SelectReminderBtn = (Border)target;
			SelectReminderBtn.MouseLeftButtonDown += OnShowPopupButtonMouseDown;
			SelectReminderBtn.MouseLeftButtonUp += SelectReminderClick;
			break;
		case 4:
			EmptyRemindText = (TextBlock)target;
			break;
		case 5:
			ReminderText = (TextBlock)target;
			break;
		case 6:
			((Border)target).MouseLeftButtonUp += OnClearRemindClick;
			break;
		case 7:
			ReminderRightPath = (Path)target;
			break;
		case 8:
			SelectReminderPopup = (EscPopup)target;
			break;
		case 9:
			SelectRepeatPopup = (EscPopup)target;
			break;
		case 10:
			SelectRepeatButton = (Border)target;
			SelectRepeatButton.MouseLeftButtonDown += OnShowPopupButtonMouseDown;
			SelectRepeatButton.MouseLeftButtonUp += SelectRepeatClick;
			break;
		case 11:
			EmptyRepeatText = (TextBlock)target;
			break;
		case 12:
			RepeatText = (TextBlock)target;
			break;
		case 13:
			((Border)target).MouseLeftButtonUp += OnClearRepeatClick;
			break;
		case 14:
			RepeatRightPath = (Path)target;
			break;
		case 15:
			RepeatEndBt = (Border)target;
			break;
		case 16:
			RepeatEndGrid = (Grid)target;
			RepeatEndGrid.MouseLeftButtonDown += OnShowPopupButtonMouseDown;
			RepeatEndGrid.MouseLeftButtonUp += SetRepeatEndClick;
			break;
		case 17:
			EmptyRepeatEndText = (TextBlock)target;
			break;
		case 18:
			RepeatEndText = (TextBlock)target;
			break;
		case 19:
			ClearPanel = (Grid)target;
			break;
		case 20:
			((Grid)target).MouseLeftButtonUp += OnClearRepeatEndClick;
			break;
		case 21:
			RepeatEndRightPath = (Path)target;
			break;
		case 22:
			SetRepeatEndPopup = (EscPopup)target;
			break;
		case 23:
			SetRepeatEndControl = (SetRepeatEndControl)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
