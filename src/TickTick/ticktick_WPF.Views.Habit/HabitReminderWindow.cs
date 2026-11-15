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
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Emoji.Wpf;
using TickTickDao;
using TickTickModels;
using TickTickUtils;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Util.ReminderTime;
using ticktick_WPF.Views.CustomControl;
using ticktick_WPF.Views.Misc;
using ticktick_WPF.Views.Pomo;
using ticktick_WPF.Views.Remind;

namespace ticktick_WPF.Views.Habit;

public class HabitReminderWindow : MyWindow, IRemindPop, IComponentConnector
{
	private HabitModel _habit;

	private ReminderModel _reminder;

	private int _currentIndex = -1;

	private bool _isClosed;

	private bool _isSubmittingCheckIn;

	private const string CustomSnoozeOption = "custom";

	private static readonly HashSet<int> PresetSnoozeMinutes = new HashSet<int> { 15, 30, 60, 180 };

	internal Border ContainerBorder;

	internal Image WindowIcon;

	internal TextBlock TimeText;

	internal Border FocusIcon;

	internal StackPanel Container;

	internal Image HabitImage;

	internal EmjTextBlock NameText;

	internal StackPanel HabitProgress;

	internal Border BaseBar;

	internal TextBlock ProgressText;

	internal StackPanel ButtonPanel;

	internal Button AutoRecordButton;

	internal Button CompleteAllButton;

	internal Button ManualRecordButton;

	internal Button RecordButton;

	internal Grid ReminderGrid;

	internal CustomComboBox ChooseRemindLaterComboBox;

	internal EscPopup CustomSnoozePopup;

	internal CustomSnoozePopupControl CustomSnoozeControl;

	internal Button RemindLaterButton;

	internal Popup ManuallyCheckInPopup;

	internal ManualRecordCheckinControl CheckInControl;

	private bool _contentLoaded;

	private DateTime remindDate { get; set; }

	public string ReminderKey => _reminder?.GetReminderKey() ?? string.Empty;

	public bool IsClosed => _isClosed;

	public HabitReminderWindow()
	{
		InitializeComponent();
		remindDate = DateTime.Today;
		ReminderWindowPositionHelper.ResetToBottomRight(this);
		WindowIcon.Source = AppIconUtils.GetIconImage();
		ChooseRemindLaterComboBox.SetPopupPosition(PlacementMode.Top, -5, 2);
		DataChangedNotifier.HabitsSyncDone += OnHabitSyncDone;
		GlobalEventManager.RemindHandled += OnRemindHandled;
		TimeChangeNotifier.DayChanged += OnDayChanged;
	}

	private void OnDayChanged(object sender, EventArgs e)
	{
		if (_reminder != null)
		{
			DateTime date = _reminder.ReminderTime ?? _reminder.StartDate ?? DateTime.Now;
			TimeText.Text = ticktick_WPF.Util.DateUtils.FormatSmartDateShortString(date, ticktick_WPF.Util.DateUtils.FormatHourMinute(date));
		}
	}

	private void OnRemindHandled(object sender, RemindMessage e)
	{
		if (CheckRemindMatched(_reminder, e))
		{
			Utils.RunOnUiThread(base.Dispatcher, TryClose);
		}
	}

	private bool CheckRemindMatched(ReminderModel reminder, RemindMessage remindMessage)
	{
		if (reminder == null || remindMessage == null)
		{
			return false;
		}
		if (remindMessage.type != "habit")
		{
			return false;
		}
		if (!reminder.ReminderTime.HasValue || reminder.ReminderTime.Value - remindMessage.remindTime > TimeSpan.FromSeconds(30.0))
		{
			return false;
		}
		return reminder.HabitId == remindMessage.id;
	}

	private ObservableCollection<ComboBoxViewModel> BuildRemindLaterItems(string extra)
	{
		ObservableCollection<ComboBoxViewModel> observableCollection = new ObservableCollection<ComboBoxViewModel>
		{
			new ComboBoxViewModel("15m", "15 " + Utils.GetString("PublicMinutes"), 32.0),
			new ComboBoxViewModel("30m", "30 " + Utils.GetString("PublicMinutes"), 32.0),
			new ComboBoxViewModel("1h", "1 " + Utils.GetString("PublicHour"), 32.0),
			new ComboBoxViewModel("3h", "3 " + Utils.GetString("PublicHours"), 32.0)
		};
		if (!string.IsNullOrEmpty(extra))
		{
			observableCollection.Add(new ComboBoxViewModel(extra, Utils.GetString(extra), 32.0));
		}
		int lastSnoozeMinutes = GetLastSnoozeMinutes();
		if (lastSnoozeMinutes > 0 && !PresetSnoozeMinutes.Contains(lastSnoozeMinutes))
		{
			observableCollection.Add(new ComboBoxViewModel(string.Empty, Utils.GetString("RecentUsed"), 28.0)
			{
				CanSelect = false,
				IsEnable = false,
				Selectable = false,
				ShowSelected = false,
				ShowSelectedIcon = false,
				IsHeader = true,
				ShowTopSeparator = true
			});
			observableCollection.Add(new ComboBoxViewModel(FormatSnoozeDelayOption(lastSnoozeMinutes), FormatSnoozeDuration(lastSnoozeMinutes), 32.0));
		}
		observableCollection.Add(new ComboBoxViewModel("custom", Utils.GetString("Custom"), 32.0)
		{
			ShowTopSeparator = true
		});
		return observableCollection;
	}

	private static int GetLastSnoozeMinutes()
	{
		return Math.Max(0, LocalSettings.Settings.ExtraSettings.LastSnoozeMinutes);
	}

	private static async Task SaveLastSnoozeMinutes(int minutes)
	{
		LocalSettings.Settings.ExtraSettings.LastSnoozeMinutes = Math.Max(0, minutes);
		await LocalSettings.Settings.Save(force: true);
	}

	private static string FormatSnoozeDuration(int minutes)
	{
		minutes = Math.Max(0, minutes);
		int num = minutes / 60;
		int num2 = minutes % 60;
		List<string> list = new List<string>();
		if (num > 0)
		{
			list.Add(string.Format("{0} {1}", num, Utils.GetString((num > 1) ? "PublicHours" : "PublicHour")));
		}
		if (num2 > 0 || num == 0)
		{
			list.Add(string.Format("{0} {1}", num2, Utils.GetString((num2 > 1) ? "PublicMinutes" : "PublicMinute")));
		}
		return string.Join(" ", list);
	}

	private static string FormatSnoozeDelayOption(int minutes)
	{
		return $"{Math.Max(1, minutes)}m";
	}

	private static bool TryParseSnoozeDelayOption(string option, out int minutes)
	{
		minutes = 0;
		if (string.IsNullOrEmpty(option) || option.Length < 2)
		{
			return false;
		}
		char c = option[option.Length - 1];
		if (!int.TryParse(option.Substring(0, option.Length - 1), out var result) || result <= 0)
		{
			return false;
		}
		switch (c)
		{
		case 'm':
			minutes = result;
			return true;
		case 'h':
			minutes = result * 60;
			return true;
		default:
			return false;
		}
	}

	private async void OnHabitSyncDone(object sender, EventArgs e)
	{
		HabitCheckInModel habitCheckInModel = await HabitCheckInDao.GetHabitCheckInsByHabitIdAndStamp(_habit?.Id, DateTime.Today.ToString("yyyyMMdd", CultureInfo.InvariantCulture));
		if (habitCheckInModel != null && (habitCheckInModel.CheckStatus == 1 || habitCheckInModel.Value >= habitCheckInModel.Goal))
		{
			TryClose();
		}
	}

	public async Task InitData(ReminderModel reminder)
	{
		_reminder = reminder;
		HabitModel habit = await HabitDao.GetHabitById(reminder.HabitId);
		if (habit == null)
		{
			TryClose();
			return;
		}
		_habit = habit;
		string extra = "";
		DateTime dateTime = reminder.ReminderTime ?? DateTime.Now;
		int num = dateTime.Hour * 60 + dateTime.Minute;
		if (num <= 540)
		{
			extra = "TodayMorning";
		}
		else if (num <= 780)
		{
			extra = "TodayAfternoon";
		}
		else if (num <= 1020)
		{
			extra = "TodayEvening";
		}
		else if (num <= 1200)
		{
			extra = "TodayNight";
		}
		ObservableCollection<ComboBoxViewModel> items = BuildRemindLaterItems(extra);
		ChooseRemindLaterComboBox.Init(items);
		HabitDetailViewModel vm = new HabitDetailViewModel(_habit)
		{
			PivotDate = DateTime.Today
		};
		bool flag = LocalSettings.Settings.ExtraSettings.RemindDetail == 2;
		if (!flag)
		{
			bool flag2 = LocalSettings.Settings.ExtraSettings.RemindDetail == 1;
			if (flag2)
			{
				flag2 = await AppLockCache.GetAppLocked();
			}
			flag = flag2;
		}
		bool flag3 = flag;
		if (flag3)
		{
			vm.Name = Utils.GetString("ReminderWhenLock");
			ButtonPanel.Visibility = Visibility.Collapsed;
		}
		HabitProgress.Visibility = (flag3 ? Visibility.Collapsed : Visibility.Visible);
		FocusIcon.Visibility = ((flag3 || !LocalSettings.Settings.EnableFocus) ? Visibility.Collapsed : Visibility.Visible);
		List<HabitCheckInModel> list = await HabitCheckInDao.GetHabitCheckInsByHabitId(_habit.Id);
		if (list.Any())
		{
			HabitCheckInModel habitCheckInModel = (vm.TodayCheckIn = list.FirstOrDefault((HabitCheckInModel checkIn) => checkIn.CheckinStamp == DateTime.Today.ToString("yyyyMMdd")));
			if (habitCheckInModel != null)
			{
				vm.Value = habitCheckInModel.GetDisplayValue();
				vm.CompletedRatio = (int)(habitCheckInModel.GetDisplayValue() * 1.0 / _habit.Goal * 1.0 * 100.0);
				if (habitCheckInModel.Value >= _habit.Goal)
				{
					vm.Status = 2;
				}
			}
		}
		InitOperationButton(_habit);
		base.DataContext = vm;
		if (_habit.IsBooleanType())
		{
			if (habit.TargetDays > 0)
			{
				CompletedCycle obj = habit.CompletedCyclesList?.LastOrDefault();
				int num2 = obj?.Streak ?? 0;
				int? num3 = ((obj != null) ? new int?(obj.TargetDays) : habit.TargetDays);
				ProgressText.Text = string.Format(Utils.GetString("AchievedXDays"), $"{num2}/{num3}");
			}
			else
			{
				int totalCheckIns = HabitStatisticsUtils.CalculateHabitStatInfo(list, habit.RepeatRule).TotalCheckIns;
				ProgressText.Text = string.Format(Utils.GetString((totalCheckIns > 1) ? "AchievedXTotalCheckIns" : "AchievedXTotalCheckIn"), totalCheckIns);
			}
		}
		else
		{
			ProgressText.Text = vm.Value + " / " + vm.Goal + " " + vm.Unit;
		}
		DateTime date = reminder.ReminderTime ?? reminder.StartDate ?? DateTime.Now;
		TimeText.Text = ticktick_WPF.Util.DateUtils.FormatSmartDateShortString(date, ticktick_WPF.Util.DateUtils.FormatHourMinute(date));
		HabitImage.Source = ImageUtils.GetResourceImage(vm.ImageUrl, 40);
	}

	private void InitOperationButton(HabitModel habit)
	{
		_habit = habit;
		CompleteAllButton.Visibility = Visibility.Collapsed;
		AutoRecordButton.Visibility = Visibility.Collapsed;
		ManualRecordButton.Visibility = Visibility.Collapsed;
		RecordButton.Visibility = Visibility.Collapsed;
		ReminderGrid.Width = 132.0;
		switch (HabitUtils.GetHabitCheckInType(0, _habit.Type, _habit.Step, _habit.Goal))
		{
		case HabitCheckInType.BoolCompleted:
		case HabitCheckInType.BoolUnCompleted:
		case HabitCheckInType.CompletedAllCompleted:
		case HabitCheckInType.CompletedAllUnCompleted:
			CompleteAllButton.Visibility = Visibility.Visible;
			break;
		case HabitCheckInType.AutoCompleted:
		case HabitCheckInType.AutoUncompleted:
			RecordButton.Visibility = Visibility.Visible;
			AutoRecordButton.Visibility = Visibility.Visible;
			ManuallyCheckInPopup.PlacementTarget = RecordButton;
			AutoRecordButton.Content = "+" + habit.Step + " " + habit.Unit;
			ReminderGrid.Width = 85.0;
			break;
		case HabitCheckInType.ManualCompleted:
		case HabitCheckInType.ManualUnCompleted:
			ManualRecordButton.Visibility = Visibility.Visible;
			ManuallyCheckInPopup.PlacementTarget = ManualRecordButton;
			break;
		}
	}

	private async void OnCheckInPopupSave(object sender, double amount)
	{
		if (_isSubmittingCheckIn)
		{
			return;
		}
		_isSubmittingCheckIn = true;
		ManuallyCheckInPopup.IsOpen = false;
		try
		{
			HabitModel habitModel = await HabitDao.GetHabitById(_habit.Id);
			if (habitModel == null)
			{
				TryClose();
				return;
			}
			HabitCheckInType habitCheckInType = HabitUtils.GetHabitCheckInType(0, habitModel.Type, habitModel.Step, habitModel.Goal);
			if (habitCheckInType != HabitCheckInType.AutoCompleted && habitCheckInType != HabitCheckInType.AutoUncompleted && habitCheckInType != HabitCheckInType.ManualCompleted && habitCheckInType != HabitCheckInType.ManualUnCompleted)
			{
				InitOperationButton(habitModel);
				return;
			}
			_habit = habitModel;
			AnnoyingReminderManager.OnUserHandled(_reminder, "button");
			AddClickEvent("manual_record");
			Utils.PlayCompletionSound();
			await HabitService.CheckInHabit(_habit.Id, remindDate, amount, increment: true, checkShowRecord: true, 0, null, null, HabitAction.ManualRecord);
			TryClose();
		}
		finally
		{
			_isSubmittingCheckIn = false;
		}
	}

	private void OnCheckInPopupCancel(object sender, EventArgs e)
	{
		ManuallyCheckInPopup.IsOpen = false;
	}

	private void OnDismissClick(object sender, RoutedEventArgs e)
	{
		AnnoyingReminderManager.OnUserHandled(_reminder, "button");
		TryClose();
	}

	private void OnViewDetailClick(object sender, MouseButtonEventArgs e)
	{
		AnnoyingReminderManager.OnUserHandled(_reminder, "content");
		AddClickEvent("click_content");
		if (!string.IsNullOrEmpty(_habit?.Id))
		{
			App.NavigateHabit(_habit.Id);
		}
		TryClose();
	}

	private void OnCloseIconClick(object sender, MouseButtonEventArgs e)
	{
		AnnoyingReminderManager.OnUserHandled(_reminder, "x");
		AddClickEvent("x_btn");
		TryClose();
	}

	private void OnFocusClick(object sender, MouseButtonEventArgs e)
	{
		AnnoyingReminderManager.OnUserHandled(_reminder, "button");
		AddClickEvent("start_focus");
		if (!string.IsNullOrEmpty(_habit.Id))
		{
			if (!TickFocusManager.ConfirmSwitch(_habit.Id, _habit.Name, Window.GetWindow(this)))
			{
				return;
			}
			TickFocusManager.StartFocus(_habit.Id, isHabit: true, checkTimer: true);
			if (LocalSettings.Settings.PomoLocalSetting.AutoShowWidget)
			{
				TickFocusManager.HideOrShowFocusWidget(force: true);
			}
		}
		TryClose();
	}

	private void OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
	{
		if (Mouse.LeftButton == MouseButtonState.Pressed)
		{
			base.LocationChanged -= OnLocationChanged;
			base.LocationChanged += OnLocationChanged;
			DragMove();
		}
	}

	private void OnLocationChanged(object sender, EventArgs e)
	{
		ReminderWindowManager.OnWindowMoved(this);
	}

	private async void ReminderSelectionChanged(object sender, ComboBoxViewModel e)
	{
		if (!(e.Value is string text) || string.IsNullOrEmpty(text))
		{
			return;
		}
		ChooseRemindLaterComboBox.Close();
		if (text == "custom")
		{
			ShowCustomSnoozePopup();
			return;
		}
		ReminderModel reminder = _reminder.Copy();
		if (TrySetSnoozeTime(reminder, text, out var eventLabel, out var lastSnoozeMinutes))
		{
			AnnoyingReminderManager.OnUserHandled(_reminder, "button");
			UserActCollectUtils.AddClickEvent("reminder_data", "snooze", eventLabel);
			await ApplySnoozeReminder(reminder);
			if (lastSnoozeMinutes > 0 && !PresetSnoozeMinutes.Contains(lastSnoozeMinutes))
			{
				await SaveLastSnoozeMinutes(lastSnoozeMinutes);
			}
		}
	}

	private bool TrySetSnoozeTime(ReminderModel reminder, string option, out string eventLabel, out int lastSnoozeMinutes)
	{
		eventLabel = option;
		lastSnoozeMinutes = 0;
		if (TryParseSnoozeDelayOption(option, out var minutes))
		{
			lastSnoozeMinutes = minutes;
			if (option == FormatSnoozeDelayOption(GetLastSnoozeMinutes()) && !PresetSnoozeMinutes.Contains(minutes))
			{
				eventLabel = "recent_used";
			}
			reminder.ReminderTime = DateTime.Now.AddMinutes(minutes);
			return true;
		}
		switch (option)
		{
		case "TodayMorning":
			eventLabel = "smart_time";
			reminder.ReminderTime = (reminder.ReminderTime ?? DateTime.Now).Date.AddHours(9.0);
			return true;
		case "TodayAfternoon":
			eventLabel = "smart_time";
			reminder.ReminderTime = (reminder.ReminderTime ?? DateTime.Now).Date.AddHours(13.0);
			return true;
		case "TodayEvening":
			eventLabel = "smart_time";
			reminder.ReminderTime = (reminder.ReminderTime ?? DateTime.Now).Date.AddHours(17.0);
			return true;
		case "TodayNight":
			eventLabel = "smart_time";
			reminder.ReminderTime = (reminder.ReminderTime ?? DateTime.Now).Date.AddHours(20.0);
			return true;
		default:
			return false;
		}
	}

	private async Task ApplySnoozeReminder(ReminderModel reminder)
	{
		AddClickEvent("snooze");
		App.AddLegacyReminder(reminder);
		await ReminderDelayDao.AddModelAsync(new ReminderDelayModel
		{
			UserId = LocalSettings.Settings.LoginUserId,
			ObjId = _reminder.GetObjId(),
			Type = _reminder.GetObjType(),
			RemindTime = _reminder.StartDate,
			NextTime = reminder.ReminderTime,
			SyncStatus = 0
		});
		await HabitReminderCalculator.RecalHabitReminder(_reminder.GetObjId());
		SyncManager.TryDelaySync();
		TryClose();
	}

	private void RemindLaterButtonClick(object sender, RoutedEventArgs e)
	{
		CustomSnoozePopup.IsOpen = false;
		ChooseRemindLaterComboBox.IsOpen = true;
	}

	private void ShowCustomSnoozePopup()
	{
		CustomSnoozeControl.Reset();
		CustomSnoozePopup.IsOpen = true;
	}

	private void OnCustomSnoozeCanceled(object sender, EventArgs e)
	{
		CustomSnoozePopup.IsOpen = false;
		ChooseRemindLaterComboBox.ClearSelected();
	}

	private async void OnCustomSnoozeConfirmed(object sender, CustomSnoozeConfirmedEventArgs e)
	{
		AnnoyingReminderManager.OnUserHandled(_reminder, "button");
		int customMinutes = e.Minutes;
		CustomSnoozePopup.IsOpen = false;
		ReminderModel reminderModel = _reminder.Copy();
		reminderModel.ReminderTime = DateTime.Now.AddMinutes(customMinutes);
		UserActCollectUtils.AddClickEvent("reminder_data", "snooze", "custom");
		await ApplySnoozeReminder(reminderModel);
		await SaveLastSnoozeMinutes(customMinutes);
	}

	private async void CompleteAllClick(object sender, RoutedEventArgs e)
	{
		if (_isSubmittingCheckIn)
		{
			return;
		}
		_isSubmittingCheckIn = true;
		try
		{
			HabitModel habitModel = await HabitDao.GetHabitById(_habit.Id);
			if (habitModel == null)
			{
				TryClose();
				return;
			}
			HabitCheckInType habitCheckInType = HabitUtils.GetHabitCheckInType(0, habitModel.Type, habitModel.Step, habitModel.Goal);
			if (habitCheckInType != HabitCheckInType.BoolCompleted && habitCheckInType != HabitCheckInType.BoolUnCompleted && habitCheckInType != HabitCheckInType.CompletedAllCompleted && habitCheckInType != HabitCheckInType.CompletedAllUnCompleted)
			{
				InitOperationButton(habitModel);
				if (habitCheckInType == HabitCheckInType.ManualCompleted || habitCheckInType == HabitCheckInType.ManualUnCompleted)
				{
					OnManuallyRecordClick(sender, e);
				}
				return;
			}
			_habit = habitModel;
			AnnoyingReminderManager.OnUserHandled(_reminder, "button");
			AddClickEvent(_habit.IsBooleanType() ? "done" : "complete_all");
			Utils.PlayCompletionSound();
			await HabitService.CheckInHabit(_habit.Id, remindDate, -1.0, increment: true, checkShowRecord: true, 0, null, null, HabitAction.CheckIn);
			await AnimateRatio(0.0, 1.0);
			await Task.Delay(100);
			TryClose();
		}
		finally
		{
			_isSubmittingCheckIn = false;
		}
	}

	private async Task AnimateRatio(double from, double to, double milliseconds = 400.0)
	{
		if (!(base.DataContext is HabitDetailViewModel vm))
		{
			return;
		}
		CubicEase cubicEase = new CubicEase
		{
			EasingMode = EasingMode.EaseInOut
		};
		DateTime startTime = DateTime.Now;
		TimeSpan duration = TimeSpan.FromMilliseconds(milliseconds);
		while (true)
		{
			double val = (DateTime.Now - startTime).TotalMilliseconds / duration.TotalMilliseconds;
			val = Math.Min(Math.Max(val, 0.0), 1.0);
			double num = cubicEase.Ease(val);
			vm.CompletedRatio = (int)((from + (to - from) * num) * 100.0);
			if (val >= 1.0)
			{
				break;
			}
			await Task.Delay(16);
		}
		vm.CompletedRatio = (int)(to * 100.0);
	}

	private void OnManuallyRecordClick(object sender, RoutedEventArgs e)
	{
		ManuallyCheckInPopup.IsOpen = true;
		CheckInControl.Init(_habit.Unit);
		CheckInControl.Cancel -= OnCheckInPopupCancel;
		CheckInControl.Cancel += OnCheckInPopupCancel;
		CheckInControl.Save -= OnCheckInPopupSave;
		CheckInControl.Save += OnCheckInPopupSave;
	}

	private async void OnAutoRecordClick(object sender, RoutedEventArgs e)
	{
		if (_isSubmittingCheckIn)
		{
			return;
		}
		_isSubmittingCheckIn = true;
		try
		{
			HabitModel habitModel = await HabitDao.GetHabitById(_habit.Id);
			if (habitModel == null)
			{
				TryClose();
				return;
			}
			HabitCheckInType habitCheckInType = HabitUtils.GetHabitCheckInType(0, habitModel.Type, habitModel.Step, habitModel.Goal);
			if (habitCheckInType != HabitCheckInType.AutoCompleted && habitCheckInType != HabitCheckInType.AutoUncompleted)
			{
				InitOperationButton(habitModel);
				if (habitCheckInType == HabitCheckInType.ManualCompleted || habitCheckInType == HabitCheckInType.ManualUnCompleted)
				{
					OnManuallyRecordClick(sender, e);
				}
				return;
			}
			_habit = habitModel;
			AnnoyingReminderManager.OnUserHandled(_reminder, "button");
			Utils.PlayCompletionSound();
			AddClickEvent("auto_record");
			await HabitService.CheckInHabit(_habit.Id, remindDate, _habit.Step, increment: true, checkShowRecord: true, 0, null, null, HabitAction.AutoRecord);
			if (base.DataContext is HabitDetailViewModel habitDetailViewModel)
			{
				double num = (double)habitDetailViewModel.CompletedRatio / 100.0;
				double num2 = _habit.Step / _habit.Goal;
				await AnimateRatio(num, num + num2);
				await Task.Delay(100);
			}
			TryClose();
		}
		finally
		{
			_isSubmittingCheckIn = false;
		}
	}

	public bool IsSameReminder(ReminderModel reminder)
	{
		if (!string.IsNullOrEmpty(reminder.HabitId))
		{
			return _reminder.HabitId == reminder.HabitId;
		}
		return false;
	}

	public void SetDisplayStyle(int index)
	{
		bool flag = true;
		if (_currentIndex < 0 && index == 2)
		{
			_currentIndex = index - 1;
			flag = false;
		}
		int num = ((_currentIndex >= 0) ? _currentIndex : ((index > 0) ? (index + 1) : index));
		if (ContainerBorder.RenderTransform is ScaleTransform scaleTransform)
		{
			if (flag)
			{
				DoubleAnimation doubleAnimation = AnimationUtils.GetDoubleAnimation(1.0 - 0.05 * (double)num, 1.0 - 0.05 * (double)index, 180);
				scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, doubleAnimation);
				scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, doubleAnimation);
			}
			else
			{
				scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, null);
				scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, null);
				scaleTransform.ScaleX = 1.0 - 0.05 * (double)index;
				scaleTransform.ScaleY = 1.0 - 0.05 * (double)index;
			}
		}
		double baseTop = ReminderWindowPositionHelper.GetBaseTop(this);
		DoubleAnimation doubleAnimation2 = AnimationUtils.GetDoubleAnimation(flag ? (baseTop - (double)(8 * num)) : (baseTop - (double)(8 * index) + 2.0), baseTop - (double)(8 * index), 180);
		BeginAnimation(Window.TopProperty, doubleAnimation2);
		_currentIndex = index;
		base.Opacity = 1.0;
	}

	public void TryClose()
	{
		Communicator.TryNotifyCloseReminder(new RemindMessage(_reminder));
		Close();
	}

	protected override void OnClosing(CancelEventArgs e)
	{
		DataChangedNotifier.HabitsSyncDone -= OnHabitSyncDone;
		GlobalEventManager.RemindHandled -= OnRemindHandled;
		TimeChangeNotifier.DayChanged -= OnDayChanged;
		base.OnClosing(e);
	}

	protected override void OnClosed(EventArgs e)
	{
		_isClosed = true;
		base.OnClosed(e);
	}

	public void TryHide()
	{
		_currentIndex = -1;
		base.Opacity = 0.0;
		if (ContainerBorder.RenderTransform is ScaleTransform scaleTransform)
		{
			scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, null);
			scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, null);
			scaleTransform.ScaleX = 0.9;
			scaleTransform.ScaleY = 0.9;
			BeginAnimation(Window.TopProperty, null);
			base.Top = ReminderWindowPositionHelper.GetHiddenTop(this);
		}
	}

	private void AddClickEvent(string label)
	{
		UserActCollectUtils.AddClickEvent("reminder", "habit_reminder_dialog", label);
	}

	public void ShowWindow()
	{
		IntPtr handle = new WindowInteropHelper(this).Handle;
		base.Opacity = 1.0;
		base.Visibility = Visibility.Visible;
		if (handle != IntPtr.Zero)
		{
			base.Topmost = false;
			base.Topmost = true;
			NativeUtils.ShowWindow(handle, 4);
		}
		else
		{
			base.ShowActivated = false;
			Show();
		}
	}

	public void UpdateReminder(ReminderModel reminder)
	{
		if (reminder != null)
		{
			_reminder = reminder;
			DateTime date = reminder.ReminderTime ?? reminder.StartDate ?? DateTime.Now;
			TimeText.Text = ticktick_WPF.Util.DateUtils.FormatSmartDateShortString(date, ticktick_WPF.Util.DateUtils.FormatHourMinute(date));
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/habit/habitreminderwindow.xaml", UriKind.Relative);
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
			ContainerBorder = (Border)target;
			break;
		case 2:
			WindowIcon = (Image)target;
			break;
		case 3:
			TimeText = (TextBlock)target;
			break;
		case 4:
			((Border)target).PreviewMouseDown += OnPreviewMouseDown;
			break;
		case 5:
			FocusIcon = (Border)target;
			FocusIcon.MouseLeftButtonUp += OnFocusClick;
			break;
		case 6:
			((Border)target).MouseLeftButtonUp += OnCloseIconClick;
			break;
		case 7:
			Container = (StackPanel)target;
			Container.MouseLeftButtonUp += OnViewDetailClick;
			break;
		case 8:
			HabitImage = (Image)target;
			break;
		case 9:
			NameText = (EmjTextBlock)target;
			break;
		case 10:
			HabitProgress = (StackPanel)target;
			break;
		case 11:
			BaseBar = (Border)target;
			break;
		case 12:
			ProgressText = (TextBlock)target;
			break;
		case 13:
			ButtonPanel = (StackPanel)target;
			break;
		case 14:
			AutoRecordButton = (Button)target;
			AutoRecordButton.Click += OnAutoRecordClick;
			break;
		case 15:
			CompleteAllButton = (Button)target;
			CompleteAllButton.Click += CompleteAllClick;
			break;
		case 16:
			ManualRecordButton = (Button)target;
			ManualRecordButton.Click += OnManuallyRecordClick;
			break;
		case 17:
			RecordButton = (Button)target;
			RecordButton.Click += OnManuallyRecordClick;
			break;
		case 18:
			ReminderGrid = (Grid)target;
			break;
		case 19:
			ChooseRemindLaterComboBox = (CustomComboBox)target;
			break;
		case 20:
			CustomSnoozePopup = (EscPopup)target;
			break;
		case 21:
			CustomSnoozeControl = (CustomSnoozePopupControl)target;
			break;
		case 22:
			RemindLaterButton = (Button)target;
			RemindLaterButton.Click += RemindLaterButtonClick;
			break;
		case 23:
			ManuallyCheckInPopup = (Popup)target;
			break;
		case 24:
			CheckInControl = (ManualRecordCheckinControl)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
