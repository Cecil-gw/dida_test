using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
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
using System.Windows.Media.Imaging;
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
using ticktick_WPF.Util.Sync.Model;
using ticktick_WPF.Views.CustomControl;
using ticktick_WPF.Views.Misc;
using ticktick_WPF.Views.Pomo;
using ticktick_WPF.Views.Remind;

namespace ticktick_WPF.Views;

public class ReminderPopup : MyWindow, IRemindPop, IComponentConnector
{
	private readonly ReminderModel _reminder = new ReminderModel();

	private TaskModel _task;

	private int _currentIndex = -2;

	private const string CustomSnoozeOption = "custom";

	private static readonly HashSet<int> PresetSnoozeMinutes = new HashSet<int> { 15, 30, 60, 180 };

	internal Border ContainerBorder;

	internal Image WindowIcon;

	internal TextBlock TimeText;

	internal Image RepeatIcon;

	internal Border FocusIcon;

	internal Border CountdownIcon;

	internal Image CountDownImage;

	internal ScrollViewer Viewer;

	internal EmjTextBlock TitleTextBlock;

	internal StackPanel ContentPanel;

	internal Grid TaskOperationPanel;

	internal Grid RemindLaterGrid;

	internal CustomComboBox ChooseRemindLaterComboBox;

	internal EscPopup CustomSnoozePopup;

	internal CustomSnoozePopupControl CustomSnoozeControl;

	internal Button RemindLaterButton;

	internal Button CompleteButton;

	internal Button CloseButton;

	private bool _contentLoaded;

	public string ReminderKey => _reminder?.GetReminderKey() ?? string.Empty;

	public ReminderPopup()
	{
		InitializeComponent();
		TimeChangeNotifier.DayChanged += OnDayChanged;
	}

	private void OnDayChanged(object sender, EventArgs e)
	{
		if (_reminder != null)
		{
			TimeText.Text = ticktick_WPF.Util.DateUtils.FormatQuickAddDateString(_reminder.StartDate ?? _reminder.ReminderTime ?? DateTime.Now, _reminder.DueDate, _reminder.IsAllDay);
		}
	}

	public ReminderPopup(ReminderModel reminder)
	{
		ReminderWindowPositionHelper.ResetToBottomRight(this);
		InitializeComponent();
		_reminder = reminder;
		string text = "";
		DateTime dateTime = reminder.ReminderTime ?? DateTime.Now;
		int num = dateTime.Hour * 60 + dateTime.Minute;
		ObservableCollection<ComboBoxViewModel> items = BuildRemindLaterItems(reminder, (num < 540) ? "TodayMorning" : ((num < 780) ? "TodayAfternoon" : ((num < 1020) ? "TodayEvening" : ((num >= 1200) ? ((!reminder.CanDelayTomorrow()) ? "" : "TomorrowMorning") : "TodayNight"))));
		ChooseRemindLaterComboBox.Init(items);
		if (_reminder.Type == ReminderModel.RemindType.PayRemind)
		{
			LoadNormalData();
		}
		else
		{
			LoadData();
			TimeText.Text = ticktick_WPF.Util.DateUtils.FormatQuickAddDateString(reminder.StartDate ?? reminder.ReminderTime ?? DateTime.Now, reminder.DueDate, reminder.IsAllDay);
		}
		DataChangedNotifier.SyncDone += OnSyncDone;
		GlobalEventManager.RemindHandled += OnRemindHandled;
		SetWindowIcon();
		base.Visibility = Visibility.Hidden;
		ChooseRemindLaterComboBox.SetPopupPosition(PlacementMode.Top, -5, 2);
	}

	private void SetWindowIcon()
	{
		WindowIcon.Source = AppIconUtils.GetIconImage();
	}

	private ObservableCollection<ComboBoxViewModel> BuildRemindLaterItems(ReminderModel reminder, string extra)
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
		if (reminder.CanDelayTomorrow())
		{
			observableCollection.Add(new ComboBoxViewModel("Tomorrow", Utils.GetString("Tomorrow"), 32.0));
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
		if (remindMessage.type == "habit")
		{
			return false;
		}
		if (!reminder.ReminderTime.HasValue || reminder.ReminderTime.Value - remindMessage.remindTime > TimeSpan.FromSeconds(30.0))
		{
			return false;
		}
		return remindMessage.type switch
		{
			"calendar" => reminder.EventId == remindMessage.id, 
			"countdown" => reminder.Id == remindMessage.id, 
			"course" => reminder.Id == remindMessage.id, 
			"checklist" => reminder.CheckItemId == remindMessage.id, 
			"task" => reminder.TaskId == remindMessage.id, 
			_ => false, 
		};
	}

	private async void OnSyncDone(object sender, SyncResult result)
	{
		if (await ReminderCalculator.IsReminderExpired(_reminder))
		{
			TryClose();
		}
	}

	private async Task LoadData()
	{
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
			TaskOperationPanel.Visibility = Visibility.Collapsed;
		}
		if (_reminder.Type == ReminderModel.RemindType.Calendar)
		{
			LoadEventData(flag3);
		}
		else if (_reminder.Type == ReminderModel.RemindType.Course)
		{
			LoadCourseData();
		}
		else if (_reminder.Type != ReminderModel.RemindType.Countdown)
		{
			await LoadTaskData(flag3);
		}
		else
		{
			LoadCountdownData();
		}
		if (string.IsNullOrEmpty(TitleTextBlock.Text))
		{
			TitleTextBlock.Opacity = 0.6;
			TitleTextBlock.Text = Utils.GetString("NoTitle");
		}
	}

	private async Task LoadCountdownData()
	{
		CompleteButton.Visibility = Visibility.Collapsed;
		CloseButton.Visibility = Visibility.Visible;
		TitleTextBlock.Text = _reminder.Title;
		CountdownIcon.Visibility = Visibility.Visible;
		CountdownModel countdownModel = await CountdownDao.GetCountdownById(_reminder.Id);
		if (countdownModel != null)
		{
			BitmapSource icon = CountdownService.GetIcon(countdownModel.IconRes, countdownModel.Color);
			CountDownImage.Source = icon;
		}
		if (string.IsNullOrWhiteSpace(_reminder.Content))
		{
			ContentPanel.Visibility = Visibility.Collapsed;
			return;
		}
		List<RemindContentItem> list = new List<RemindContentItem>();
		list.Add(new RemindContentItem(_reminder.Content, 12.0, 0.4));
		if (countdownModel != null && !string.IsNullOrEmpty(countdownModel.Remark))
		{
			list.Add(new RemindContentItem(Utils.GetFirstLine(countdownModel.Remark), 12.0, 0.4)
			{
				Wrap = false
			});
		}
		AddContentText(list);
	}

	private void LoadEventData(bool hideDetail)
	{
		CompleteButton.Visibility = Visibility.Collapsed;
		CloseButton.Visibility = Visibility.Visible;
		if (hideDetail)
		{
			TitleTextBlock.Text = Utils.GetString("ReminderWhenLock");
			ContentPanel.Visibility = Visibility.Collapsed;
			return;
		}
		TitleTextBlock.Text = _reminder.Title;
		RepeatIcon.Visibility = (string.IsNullOrEmpty(_reminder.RepeatFlag) ? Visibility.Collapsed : Visibility.Visible);
		if (string.IsNullOrWhiteSpace(_reminder.Content))
		{
			ContentPanel.Visibility = Visibility.Collapsed;
			return;
		}
		AddContentText(new List<string> { _reminder.Content });
	}

	private async Task LoadNormalData()
	{
		CompleteButton.Visibility = Visibility.Collapsed;
		CloseButton.Visibility = Visibility.Visible;
		RemindLaterGrid.Visibility = Visibility.Collapsed;
		TitleTextBlock.Visibility = Visibility.Collapsed;
		TimeText.Text = _reminder.Title;
		if (string.IsNullOrWhiteSpace(_reminder.Content))
		{
			ContentPanel.Visibility = Visibility.Collapsed;
			return;
		}
		AddContentText(new List<string> { _reminder.Content });
	}

	private void AddContentText(List<string> texts, int highlightIndex = -1)
	{
		for (int i = 0; i < texts.Count; i++)
		{
			string text = MarkdownUtils.RemoveFoldedMetadataComments(texts[i]);
			if (i != 0 || !string.IsNullOrWhiteSpace(text))
			{
				EmjTextBlock emjTextBlock = new EmjTextBlock
				{
					TextWrapping = TextWrapping.Wrap,
					FontSize = 12.0,
					LineHeight = 20.0,
					HorizontalAlignment = HorizontalAlignment.Left,
					VerticalAlignment = VerticalAlignment.Top,
					Text = text,
					Margin = new Thickness(0.0, 0.0, 0.0, (i == 0) ? 2 : 0)
				};
				emjTextBlock.SetResourceReference(Control.ForegroundProperty, "BaseSolidColorOpacity50");
				if (i == highlightIndex)
				{
					Grid grid = new Grid();
					emjTextBlock.SetResourceReference(TextBlock.ForegroundProperty, "PrimaryColor");
					grid.Children.Add(emjTextBlock);
					Border border = new Border
					{
						CornerRadius = new CornerRadius(4.0),
						RenderTransform = new TranslateTransform(0.0, -1.0),
						Margin = new Thickness(0.0),
						VerticalAlignment = VerticalAlignment.Stretch,
						HorizontalAlignment = HorizontalAlignment.Stretch
					};
					border.SetResourceReference(Control.BackgroundProperty, "PrimaryColor10");
					grid.Children.Add(border);
					ContentPanel.Children.Add(grid);
					UpdateLayout();
					Point point = grid.TranslatePoint(default(Point), Viewer);
					Viewer.ScrollToVerticalOffset(point.Y);
				}
				else
				{
					ContentPanel.Children.Add(emjTextBlock);
				}
			}
		}
	}

	private void AddContentText(List<RemindContentItem> items)
	{
		int num = 0;
		foreach (RemindContentItem item in items)
		{
			EmjTextBlock emjTextBlock = new EmjTextBlock
			{
				FontSize = ((item.FontSize > 0.0) ? item.FontSize : 12.0),
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Top,
				Text = MarkdownUtils.RemoveFoldedMetadataComments(item.Text),
				Margin = new Thickness(0.0, 0.0, 0.0, (num == 0) ? 2 : 0),
				Opacity = item.Opacity,
				LineHeight = item.LineHeight
			};
			if (item.Wrap)
			{
				emjTextBlock.TextWrapping = TextWrapping.Wrap;
			}
			else
			{
				emjTextBlock.TextTrimming = TextTrimming.CharacterEllipsis;
			}
			emjTextBlock.SetResourceReference(Control.ForegroundProperty, "BaseSolidColorOpacity100");
			ContentPanel.Children.Add(emjTextBlock);
			num++;
		}
	}

	private void LoadCourseData()
	{
		CompleteButton.Visibility = Visibility.Collapsed;
		CloseButton.Visibility = Visibility.Visible;
		TitleTextBlock.Text = _reminder.Title;
		if (string.IsNullOrWhiteSpace(_reminder.Content))
		{
			ContentPanel.Visibility = Visibility.Collapsed;
			return;
		}
		AddContentText(new List<string> { _reminder.Content });
	}

	private async Task LoadTaskData(bool hideDetail)
	{
		ProjectModel project = CacheManager.GetProjects().FirstOrDefault((ProjectModel p) => p.id == _reminder.ProjectId);
		if (hideDetail || !(project?.IsProjectPermit() ?? true))
		{
			TaskOperationPanel.Visibility = Visibility.Collapsed;
		}
		TaskModel task = await TaskDao.GetThinTaskById(_reminder.TaskId);
		if (task != null)
		{
			_task = task;
			if (hideDetail)
			{
				TitleTextBlock.Text = Utils.GetString("ReminderWhenLock");
				ContentPanel.Visibility = Visibility.Collapsed;
				return;
			}
			_reminder.Title = task.title;
			TitleTextBlock.Text = _reminder.Title;
			if (task.kind == "CHECKLIST")
			{
				List<TaskDetailItemModel> list = await TaskDetailItemDao.GetCheckItemsByTaskId(_reminder.TaskId);
				List<string> list2 = new List<string>();
				list2.Add(task.desc?.Trim() ?? string.Empty);
				int highlightIndex = -1;
				if (list != null && list.Count > 0)
				{
					list.Sort((TaskDetailItemModel a, TaskDetailItemModel b) => a.sortOrder.CompareTo(b.sortOrder));
					for (int num = 0; num < list.Count; num++)
					{
						TaskDetailItemModel taskDetailItemModel = list[num];
						list2.Add("- " + taskDetailItemModel.title.Trim());
						if (taskDetailItemModel.id == _reminder.CheckItemId)
						{
							highlightIndex = num + 1;
						}
					}
				}
				_ = _reminder.Type;
				_ = 1;
				AddContentText(list2, highlightIndex);
			}
			else
			{
				_reminder.Content = TaskUtils.GetContentDisplayText(task.content);
				if (string.IsNullOrWhiteSpace(_reminder.Content))
				{
					ContentPanel.Visibility = Visibility.Collapsed;
				}
				else
				{
					AddContentText(new List<string> { _reminder.Content });
				}
			}
			if (!string.IsNullOrEmpty(_task.attendId) && !AgendaHelper.CanAccessAgenda(_task))
			{
				CloseButton.Visibility = Visibility.Visible;
				CompleteButton.Visibility = Visibility.Collapsed;
			}
			if (_task.kind == "NOTE")
			{
				CloseButton.Visibility = Visibility.Visible;
				CompleteButton.Visibility = Visibility.Collapsed;
			}
		}
		if (LocalSettings.Settings.EnableFocus)
		{
			FocusIcon.Visibility = ((!(project?.IsProjectPermit() ?? false) || hideDetail || _reminder.Type == ReminderModel.RemindType.Checklist) ? Visibility.Collapsed : Visibility.Visible);
		}
		RepeatIcon.Visibility = (string.IsNullOrEmpty(_task.repeatFlag) ? Visibility.Collapsed : Visibility.Visible);
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

	private async void CompleteClick(object sender, RoutedEventArgs e)
	{
		if (_reminder != null)
		{
			AnnoyingReminderManager.OnUserHandled(_reminder, "button");
			App.Instance.StopReminderTimer();
			switch (_reminder.Type)
			{
			case ReminderModel.RemindType.Task:
			{
				TaskModel taskModel = await TaskDao.GetThinTaskById(_reminder.TaskId);
				if (taskModel != null && taskModel.status == 0 && _task != null && taskModel.status == 0)
				{
					await TaskService.SetTaskStatus(_task, 2);
				}
				break;
			}
			case ReminderModel.RemindType.Checklist:
				await TaskDetailItemService.CompleteCheckItem(_reminder.CheckItemId, handleTask: true, completeOnly: true);
				break;
			}
			AddClickEvent("done");
			SyncManager.TryDelaySync();
			App.Instance.StartReminderTimer();
		}
		TryClose();
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
		case "Tomorrow":
			eventLabel = "tomorrow";
			reminder.ReminderTime = DateTime.Now.AddDays(1.0);
			return true;
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
		case "TomorrowMorning":
			eventLabel = "smart_time";
			reminder.ReminderTime = (reminder.ReminderTime ?? DateTime.Now).Date.AddDays(1.0).AddHours(9.0);
			return true;
		default:
			return false;
		}
	}

	private async Task ApplySnoozeReminder(ReminderModel reminder)
	{
		AddClickEvent("snooze");
		if (reminder.Type == ReminderModel.RemindType.Task)
		{
			if (reminder.ReminderTime.HasValue)
			{
				await TaskService.SetSnoozeTime(reminder.TaskId, reminder.ReminderTime.Value);
			}
		}
		else if (reminder.Type == ReminderModel.RemindType.Checklist)
		{
			TaskDetailItemModel checkItem = await TaskDetailItemDao.GetChecklistItemById(reminder.CheckItemId);
			if (checkItem != null && reminder.ReminderTime.HasValue)
			{
				checkItem.snoozeReminderTime = reminder.ReminderTime.Value;
				await TaskDetailItemDao.SaveChecklistItem(checkItem);
				await CheckItemReminderCalculator.ResetCheckItemReminder(checkItem.id);
				await SyncStatusDao.AddSyncStatus(reminder.TaskId, 0);
			}
		}
		if (reminder.Type != ReminderModel.RemindType.Checklist)
		{
			ReminderDelayModel reminderDelayModel = new ReminderDelayModel
			{
				UserId = LocalSettings.Settings.LoginUserId,
				ObjId = reminder.GetObjId(),
				Type = reminder.GetObjType(),
				RemindTime = ((reminder.Type != ReminderModel.RemindType.Countdown) ? _reminder.StartDate : _reminder.ReminderTime),
				NextTime = reminder.ReminderTime,
				SyncStatus = 0
			};
			if (reminder.Type == ReminderModel.RemindType.Calendar && !string.IsNullOrEmpty(reminder.EventArchivedId))
			{
				reminderDelayModel.ObjId = reminder.EventArchivedId;
			}
			await ReminderDelayDao.AddModelAsync(reminderDelayModel);
			if (reminder.Type == ReminderModel.RemindType.Calendar)
			{
				CalendarEventModel calendarEventModel = await CalendarEventDao.GetEventByEventIdOrId(reminder.GetObjId());
				if (calendarEventModel == null)
				{
					calendarEventModel = await CalendarEventDao.GetEventByStartTimeArchiveId(_reminder.StartDate, reminder.EventArchivedId);
				}
				await EventReminderCalculator.RecalEventReminders(calendarEventModel);
			}
			if (reminder.Type == ReminderModel.RemindType.Course)
			{
				await CourseReminderCalculator.InitCourseReminders();
			}
		}
		if (reminder.ReminderTime.HasValue)
		{
			App.AddLegacyReminder(reminder);
		}
		SyncManager.TryDelaySync();
		TryClose();
	}

	private void AddClickEvent(string label)
	{
		string ctype = "task_reminder_dialog";
		if (_reminder.Type == ReminderModel.RemindType.Calendar)
		{
			ctype = "calendar_reminder_dialog";
		}
		else if (_reminder.Type == ReminderModel.RemindType.Course)
		{
			ctype = "timetable_reminder_dialog";
		}
		UserActCollectUtils.AddClickEvent("reminder", ctype, label);
	}

	private void OnPopupClick(object sender, MouseButtonEventArgs e)
	{
		AnnoyingReminderManager.OnUserHandled(_reminder, "content");
		AddClickEvent("click_content");
		if (_reminder.Type == ReminderModel.RemindType.Calendar)
		{
			App.NavigateEvent(_reminder.EventId);
		}
		else if (_reminder.Type == ReminderModel.RemindType.Countdown)
		{
			App.NavigateCountdown();
		}
		else if (_reminder.Type == ReminderModel.RemindType.PayRemind)
		{
			SettingDialog.ShowSettingDialog(SettingsType.Premium, App.Window);
		}
		else
		{
			App.ShowMainWindow(_reminder.TaskId);
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
		if (!string.IsNullOrEmpty(_reminder.TaskId))
		{
			AnnoyingReminderManager.OnUserHandled(_reminder, "button");
			AddClickEvent("start_focus");
			if (!TickFocusManager.ConfirmSwitch(_reminder.TaskId, _reminder.Title, Window.GetWindow(this)))
			{
				return;
			}
			TickFocusManager.StartFocus(_reminder.TaskId, isHabit: false, checkTimer: true);
		}
		TryClose();
	}

	private void OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
	{
		if (Mouse.LeftButton == MouseButtonState.Pressed)
		{
			base.LocationChanged -= OnLocationChanged;
			base.LocationChanged += OnLocationChanged;
			SetDisplayStyle(0);
			DragMove();
		}
	}

	private void OnLocationChanged(object sender, EventArgs e)
	{
		ReminderWindowManager.OnWindowMoved(this);
	}

	public bool IsSameReminder(ReminderModel reminder)
	{
		if (reminder.Id != _reminder.Id)
		{
			return false;
		}
		if (!string.IsNullOrEmpty(reminder.TaskId))
		{
			if (_reminder.TaskId == reminder.TaskId)
			{
				return _reminder.CheckItemId == reminder.CheckItemId;
			}
			return false;
		}
		if (!string.IsNullOrEmpty(reminder.EventId))
		{
			return _reminder.EventId == reminder.EventId;
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

	public void TryClose()
	{
		Communicator.TryNotifyCloseReminder(new RemindMessage(_reminder));
		Close();
	}

	protected override void OnActivated(EventArgs e)
	{
		if (_currentIndex != 0 && _currentIndex != -2)
		{
			SetDisplayStyle(0);
		}
		base.OnActivated(e);
	}

	protected override void OnClosing(CancelEventArgs e)
	{
		DataChangedNotifier.SyncDone -= OnSyncDone;
		GlobalEventManager.RemindHandled -= OnRemindHandled;
		TimeChangeNotifier.DayChanged -= OnDayChanged;
		base.OnClosing(e);
	}

	private void OnCloseClick(object sender, RoutedEventArgs e)
	{
		AnnoyingReminderManager.OnUserHandled(_reminder, "button");
		AddClickEvent("got_it_btn");
		TryClose();
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
			_reminder.StartDate = reminder.StartDate;
			_reminder.DueDate = reminder.DueDate;
			_reminder.ReminderTime = reminder.ReminderTime;
			_reminder.Title = reminder.Title;
			_reminder.Content = reminder.Content;
			_reminder.IsAnnoyingAlert = reminder.IsAnnoyingAlert;
			_reminder.HasResolvedAnnoyingAlert = reminder.HasResolvedAnnoyingAlert;
			TimeText.Text = ticktick_WPF.Util.DateUtils.FormatQuickAddDateString(_reminder.StartDate ?? _reminder.ReminderTime ?? DateTime.Now, _reminder.DueDate, _reminder.IsAllDay);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/reminderpopup.xaml", UriKind.Relative);
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
			RepeatIcon = (Image)target;
			break;
		case 5:
			((Border)target).PreviewMouseDown += OnPreviewMouseDown;
			break;
		case 6:
			FocusIcon = (Border)target;
			FocusIcon.MouseLeftButtonUp += OnFocusClick;
			break;
		case 7:
			((Border)target).MouseLeftButtonUp += OnCloseIconClick;
			break;
		case 8:
			CountdownIcon = (Border)target;
			break;
		case 9:
			CountDownImage = (Image)target;
			break;
		case 10:
			Viewer = (ScrollViewer)target;
			Viewer.MouseLeftButtonUp += OnPopupClick;
			break;
		case 11:
			TitleTextBlock = (EmjTextBlock)target;
			break;
		case 12:
			ContentPanel = (StackPanel)target;
			break;
		case 13:
			TaskOperationPanel = (Grid)target;
			break;
		case 14:
			RemindLaterGrid = (Grid)target;
			break;
		case 15:
			ChooseRemindLaterComboBox = (CustomComboBox)target;
			break;
		case 16:
			CustomSnoozePopup = (EscPopup)target;
			break;
		case 17:
			CustomSnoozeControl = (CustomSnoozePopupControl)target;
			break;
		case 18:
			RemindLaterButton = (Button)target;
			RemindLaterButton.Click += RemindLaterButtonClick;
			break;
		case 19:
			CompleteButton = (Button)target;
			CompleteButton.Click += CompleteClick;
			break;
		case 20:
			CloseButton = (Button)target;
			CloseButton.Click += OnCloseClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
