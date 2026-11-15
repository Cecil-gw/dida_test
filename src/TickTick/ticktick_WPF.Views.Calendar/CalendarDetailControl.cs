using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using Newtonsoft.Json;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Util.DateParser;
using ticktick_WPF.Util.ReminderTime;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.CustomControl;
using ticktick_WPF.Views.Detail;
using ticktick_WPF.Views.MainListView;
using ticktick_WPF.Views.Misc;
using ticktick_WPF.Views.Project;
using ticktick_WPF.Views.QuickAdd;
using ticktick_WPF.Views.Search;
using ticktick_WPF.Views.Time;

namespace ticktick_WPF.Views.Calendar;

public class CalendarDetailControl : UserControl, IComponentConnector, IStyleConnector
{
	private string _originContent;

	private bool _creatingEvent;

	private CalendarDetailViewModel _titleParseModel;

	private TimeData _titleParseOriginalTimeData;

	private bool _titleParseDisabledByManualTime;

	internal CalendarDetailControl Root;

	internal ScrollViewer ScrollViewer;

	internal Grid BackGrid;

	internal DetailTextBox TitleText;

	internal TextBlock TitleTextHint;

	internal Grid DateGrid;

	internal Grid ReminderText;

	internal Run ReminderRun;

	internal Run SnoozeRun;

	internal DetailTextBox LocationText;

	internal TextBlock LocationTextHint;

	internal Grid DescriptionGrid;

	internal DetailTextBox NewContentText;

	internal TextBlock NewContentTextHint;

	internal Grid CalendarMovePanel;

	internal EscPopup SetProjectPopup;

	internal Button CreateButton;

	internal HoverIconButton MoreGrid;

	internal EscPopup MorePopup;

	private bool _contentLoaded;

	public string EventId => GetModel()?.Id;

	public bool Editable
	{
		get
		{
			if (base.DataContext is CalendarDetailViewModel calendarDetailViewModel)
			{
				return calendarDetailViewModel.Editable;
			}
			return false;
		}
	}

	public event EventHandler InOperate;

	public event EventHandler StopOperate;

	public event EventHandler HideDetail;

	public event EventHandler<bool> EventArchiveChanged;

	public event EventHandler<CalendarEventModel> EventCreated;

	public event EventHandler<CalendarDetailConvertEventArgs> ConvertNewEventToTaskKind;

	public CalendarDetailControl()
	{
		InitializeComponent();
		TitleText.SetMarkRegexText(unable: true);
		TitleText.QuickSetAccountIdProvider = GetCurrentCalendarAccountId;
		TitleText.QuickSetCalendarIdProvider = GetCurrentCalendarId;
		TitleText.QuickSetCalendarSelectorAccountIdProvider = GetCurrentCalendarSelectorAccountId;
		LocationText.QuickSetAccountIdProvider = GetCurrentCalendarAccountId;
		LocationText.QuickSetCalendarIdProvider = GetCurrentCalendarId;
		LocationText.QuickSetCalendarSelectorAccountIdProvider = GetCurrentCalendarSelectorAccountId;
		NewContentText.QuickSetAccountIdProvider = GetCurrentCalendarAccountId;
		NewContentText.QuickSetCalendarIdProvider = GetCurrentCalendarId;
		NewContentText.QuickSetCalendarSelectorAccountIdProvider = GetCurrentCalendarSelectorAccountId;
		RefreshQuickSetCalendarContext();
		base.Loaded += OnLoaded;
		base.Unloaded += OnUnloaded;
	}

	public void FocusTitleEnd()
	{
		TitleText.FocusEnd();
		TitleText.TextArea.Focus();
		Keyboard.Focus(TitleText.TextArea);
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		TitleText.LinkTextChange += OnTitleChanged;
		TitleText.DateParsed -= OnTitleDateParsed;
		TitleText.DateParsed += OnTitleDateParsed;
		TitleText.QuickItemSelected += OnQuickItemSelected;
		LocationText.QuickItemSelected += OnQuickItemSelected;
		NewContentText.QuickItemSelected += OnQuickItemSelected;
		CalendarEventChangeNotifier.TitleChanged += OnEventTitleChanged;
		CalendarEventChangeNotifier.RemoteChanged += OnEventChanged;
		DataChangedNotifier.CalendarChanged += OnCalendarChanged;
		ScrollViewer.PreviewMouseWheel += OnMouseScrollOnDetail;
	}

	private void OnUnloaded(object sender, RoutedEventArgs e)
	{
		TitleText.LinkTextChange -= OnTitleChanged;
		TitleText.DateParsed -= OnTitleDateParsed;
		TitleText.QuickItemSelected -= OnQuickItemSelected;
		LocationText.QuickItemSelected -= OnQuickItemSelected;
		NewContentText.QuickItemSelected -= OnQuickItemSelected;
		CalendarEventChangeNotifier.TitleChanged -= OnEventTitleChanged;
		CalendarEventChangeNotifier.RemoteChanged -= OnEventChanged;
		DataChangedNotifier.CalendarChanged -= OnCalendarChanged;
		ScrollViewer.PreviewMouseWheel -= OnMouseScrollOnDetail;
	}

	private void OnCalendarChanged(object sender, EventArgs e)
	{
		base.Dispatcher.Invoke((Func<Task>)async delegate
		{
			CalendarDetailViewModel model = GetModel();
			if (model != null)
			{
				BindCalendarModel bindCalendarModel = CacheManager.GetBindCalendars().FirstOrDefault((BindCalendarModel cal) => cal.Id == model.CalendarId);
				if (bindCalendarModel != null)
				{
					model.CalendarName = bindCalendarModel.Name;
					RefreshQuickSetCalendarContext();
				}
			}
		});
	}

	private async void OnEventChanged(object sender, EventArgs e)
	{
		base.Dispatcher.Invoke((Func<Task>)async delegate
		{
			CalendarDetailViewModel model = GetModel();
			if (model != null)
			{
				CalendarEventModel calendarEventModel = await CalendarEventDao.GetEventById(model.Id);
				if (calendarEventModel == null)
				{
					calendarEventModel = await CalendarEventDao.GetEventByEventId(model.Id);
				}
				CalendarEventModel calendarEventModel2 = calendarEventModel;
				if (calendarEventModel2 != null)
				{
					model.Title = calendarEventModel2.Title ?? string.Empty;
					model.Content = calendarEventModel2.Content ?? string.Empty;
					model.Location = calendarEventModel2.Location;
					if (string.IsNullOrEmpty(calendarEventModel2.RepeatFlag))
					{
						model.StartDate = calendarEventModel2.DueStart;
						model.DueDate = calendarEventModel2.DueEnd;
						model.IsAllDay = calendarEventModel2.IsAllDay;
					}
					model.RepeatFlag = calendarEventModel2.RepeatFlag;
					model.RepeatText = RRuleUtils.RRule2String(string.Empty, string.IsNullOrEmpty(calendarEventModel2.RepeatFlag) ? calendarEventModel2.Recurrence : calendarEventModel2.RepeatFlag, calendarEventModel2.DueStart);
				}
				UpdateView(model);
			}
		});
	}

	private void OnMouseScrollOnDetail(object sender, MouseWheelEventArgs e)
	{
		if (e != null)
		{
			ScrollViewer.ScrollToVerticalOffset(ScrollViewer.VerticalOffset - (double)((float)e.Delta / 2f));
		}
	}

	private void OnEventTitleChanged(object sender, TextExtra extra)
	{
		CalendarDetailViewModel model = GetModel();
		if (model != null && extra.Id == model.Id && model.Title != extra.Text)
		{
			TitleText.LinkTextChange -= OnTitleChanged;
			model.Title = extra.Text;
			TitleText.SetText(extra.Text);
			TitleText.LinkTextChange += OnTitleChanged;
		}
	}

	private void OnBackClick(object sender, MouseButtonEventArgs e)
	{
		this.HideDetail?.Invoke(this, null);
	}

	private async void OnDataBind(object sender, DependencyPropertyChangedEventArgs e)
	{
		CalendarDetailViewModel model = GetModel();
		UpdateView(model);
	}

	private async Task UpdateView(CalendarDetailViewModel model)
	{
		if (model != null)
		{
			ResetTitleParseState(model);
			MoreGrid.Visibility = ((!model.Editable && !(model.DueDate > DateTime.Today) && !(model.StartDate >= DateTime.Today)) ? Visibility.Collapsed : Visibility.Visible);
			TitleText.SetTextOffset(model.Title, restoreOffset: true, clearUndo: true);
			TitleTextHint.Visibility = ((!string.IsNullOrEmpty(TitleText.Text)) ? Visibility.Collapsed : Visibility.Visible);
			NewContentText.SetText(model.Content);
			NewContentTextHint.Visibility = ((!string.IsNullOrEmpty(NewContentText.Text)) ? Visibility.Collapsed : Visibility.Visible);
			LocationText.SetText(model.Location);
			LocationTextHint.Visibility = ((!string.IsNullOrEmpty(LocationText.Text)) ? Visibility.Collapsed : Visibility.Visible);
			RefreshQuickSetCalendarContext();
			if (model.IsAllDay && model.DueDate.HasValue && model.StartDate.HasValue && model.StartDate == model.DueDate)
			{
				model.DueDate = model.DueDate.Value.AddDays(1.0);
			}
			_originContent = model.Content;
			if (model.IsNew)
			{
				ConfigureTitleDateParsing(model);
				MoreGrid.Visibility = ((!model.ShowQuickAddConvertOptions) ? Visibility.Collapsed : Visibility.Visible);
			}
			else
			{
				DisableTitleDateParsing();
			}
			ReminderRun.Text = ReminderUtils.GetCalendarReminderText(model.Reminders, model.IsAllDay);
			ReminderText.Visibility = (string.IsNullOrEmpty(ReminderRun.Text) ? Visibility.Collapsed : Visibility.Visible);
			string text = await model.GetSnoozeText();
			if (!string.IsNullOrEmpty(text))
			{
				SnoozeRun.Text = "  " + text;
			}
			else
			{
				SnoozeRun.Text = string.Empty;
			}
			CalendarSubscribeProfileModel calendarSubscribeProfileModel = await CalendarSubscribeProfileDao.GetProfileById(model.CalendarId);
			if (calendarSubscribeProfileModel != null && calendarSubscribeProfileModel.Kind == 2)
			{
				model.Content = null;
				model.AvailableType = CalendarAvailableType.NoAccess;
			}
			if (model.AvailableType != CalendarAvailableType.Editable)
			{
				TitleText.EnableSpellCheck = false;
			}
		}
	}

	private void OnTextLostFocus(object sender, RoutedEventArgs e)
	{
		SyncManager.TryDelaySync();
	}

	private void OnLocationChanged(object sender, EventArgs e)
	{
		CalendarDetailViewModel model = GetModel();
		if (string.IsNullOrEmpty(LocationText.Text))
		{
			LocationTextHint.Visibility = Visibility.Visible;
		}
		else
		{
			LocationTextHint.Visibility = Visibility.Collapsed;
		}
		if (model != null && model.Location != LocationText.Text)
		{
			model.Location = LocationText.Text;
			if (!model.IsNew)
			{
				CalendarService.SaveEventLocation(model.Id, model.Location);
			}
		}
	}

	private void OnCalendarContentChanged(object sender, EventArgs e)
	{
		CalendarDetailViewModel model = GetModel();
		if (string.IsNullOrEmpty(NewContentText.Text))
		{
			NewContentTextHint.Visibility = Visibility.Visible;
		}
		else
		{
			NewContentTextHint.Visibility = Visibility.Collapsed;
		}
		if (model != null && NewContentText.Text != _originContent)
		{
			model.Content = NewContentText.Text;
			if (!model.IsNew)
			{
				CalendarEventChangeNotifier.NotifySummaryChanged(model.Id, NewContentText.Text);
				CalendarService.SaveEventContent(model.Id, NewContentText.Text);
			}
		}
	}

	private async void OnTitleChanged(object sender, EventArgs e)
	{
		if (string.IsNullOrEmpty(TitleText.Text))
		{
			TitleTextHint.Visibility = Visibility.Visible;
		}
		else
		{
			TitleTextHint.Visibility = Visibility.Collapsed;
		}
		CalendarDetailViewModel model = GetModel();
		if (model == null)
		{
			return;
		}
		string text = TitleText.Text;
		if (!(model.Title != text))
		{
			return;
		}
		model.Title = text;
		if (model.IsNew)
		{
			if (model.SourceTaskViewModel != null)
			{
				model.SourceTaskViewModel.Title = text;
			}
		}
		else
		{
			CalendarEventChangeNotifier.NotifyTitleChanged(model.Id, text);
			await CalendarService.SaveEventTitle(model.Id, text, notifyChanged: false);
		}
	}

	private void ResetTitleParseState(CalendarDetailViewModel model)
	{
		if (_titleParseModel != model)
		{
			_titleParseModel = model;
			_titleParseOriginalTimeData = null;
			_titleParseDisabledByManualTime = false;
		}
	}

	private void ConfigureTitleDateParsing(CalendarDetailViewModel model)
	{
		TitleText.DateParseMode = ((!_titleParseDisabledByManualTime) ? TitleDateParseMode.TimeOnlyOnCurrentDate : TitleDateParseMode.Auto);
		TitleText.DateParseBaseDate = GetTitleParseBaseDate(model);
		TitleText.SetCanParseDate(!_titleParseDisabledByManualTime);
	}

	private void DisableTitleDateParsing()
	{
		_titleParseOriginalTimeData = null;
		TitleText.DateParseMode = TitleDateParseMode.Auto;
		TitleText.ClearParseDate();
	}

	private DateTime GetTitleParseBaseDate(CalendarDetailViewModel model)
	{
		return (model?.StartDate ?? DateTime.Today).Date;
	}

	private void OnTitleDateParsed(object sender, IPaserDueDate parsedData)
	{
		TryApplyParsedTitleTime(GetModel(), parsedData);
	}

	private void TryApplyParsedTitleTime(CalendarDetailViewModel model, IPaserDueDate parsedData)
	{
		if (model != null && model.IsNew && TitleText.CanParseDate)
		{
			List<string> list = parsedData?.GetRecognizeStrings();
			if (parsedData == null || list == null || !list.Any() || !parsedData.IsTimeSeted())
			{
				RestoreTitleParseOriginalTime(model);
				return;
			}
			CaptureTitleParseOriginalTime(model);
			ApplyParsedTitleTime(model, parsedData.ToTimeData(addDefaultReminder: true));
		}
	}

	private void CaptureTitleParseOriginalTime(CalendarDetailViewModel model)
	{
		if (_titleParseOriginalTimeData == null)
		{
			_titleParseOriginalTimeData = BuildTitleTimeData(model);
		}
	}

	private void RestoreTitleParseOriginalTime(CalendarDetailViewModel model)
	{
		if (_titleParseOriginalTimeData != null)
		{
			ApplyTitleTimeData(model, _titleParseOriginalTimeData, GetTitleReminderMinutes(_titleParseOriginalTimeData), fillDefaultDueDate: false);
			_titleParseOriginalTimeData = null;
		}
	}

	private TimeData BuildTitleTimeData(CalendarDetailViewModel model)
	{
		return new TimeData
		{
			StartDate = model.StartDate,
			DueDate = model.DueDate,
			IsAllDay = model.IsAllDay,
			RepeatFlag = model.RepeatFlag,
			IsDefault = false,
			TimeZone = new TimeZoneViewModel(isFloat: false, model.TimeZone ?? Utils.GetLocalTimeZone()),
			Reminders = (model.Reminders?.Select((int minute) => new TaskReminderModel
			{
				trigger = TriggerUtils.ReminderToTrigger(minute)
			}).ToList() ?? new List<TaskReminderModel>())
		};
	}

	private void ApplyParsedTitleTime(CalendarDetailViewModel model, TimeData timeData)
	{
		List<int> parsedTitleReminderMinutes = GetParsedTitleReminderMinutes(timeData, timeData?.IsAllDay == true, model.CalendarId);
		ApplyTitleTimeData(model, timeData, parsedTitleReminderMinutes, fillDefaultDueDate: true);
	}

	private void ApplyTitleTimeData(CalendarDetailViewModel model, TimeData timeData, List<int> reminders, bool fillDefaultDueDate)
	{
		if (timeData == null || !timeData.StartDate.HasValue)
		{
			return;
		}
		model.IsAllDay = timeData.IsAllDay == true;
		if (model.IsAllDay)
		{
			model.StartDate = timeData.StartDate.Value.Date;
			model.DueDate = (timeData.DueDate ?? model.StartDate.Value).Date;
			if (fillDefaultDueDate && model.StartDate == model.DueDate)
			{
				model.DueDate = model.DueDate.Value.AddDays(1.0);
			}
		}
		else
		{
			model.StartDate = timeData.StartDate;
			model.DueDate = timeData.DueDate;
			if (fillDefaultDueDate && !model.DueDate.HasValue)
			{
				model.DueDate = model.StartDate.Value.AddMinutes(TaskDefaultDao.GetDefaultSafely()?.Duration ?? 60);
			}
		}
		model.RepeatFlag = timeData.RepeatFlag;
		model.RepeatText = RRuleUtils.RRule2String(string.Empty, model.RepeatFlag, model.StartDate);
		model.TimeZone = timeData.TimeZone?.TimeZoneName ?? Utils.GetLocalTimeZone();
		model.Reminders = reminders ?? new List<int>();
		RefreshReminderDisplay(model);
	}

	private List<int> GetTitleReminderMinutes(TimeData timeData)
	{
		return (from trigger in timeData?.Reminders?.Select((TaskReminderModel reminder) => reminder.trigger)
			where !string.IsNullOrEmpty(trigger)
			select trigger).Select(TriggerUtils.TriggerToReminder).ToList() ?? new List<int>();
	}

	private List<int> GetParsedTitleReminderMinutes(TimeData timeData, bool isAllDay, string calendarId)
	{
		if (timeData == null)
		{
			return new List<int>();
		}
		if (CalendarEventDao.GetCalendarAccount(calendarId)?.Kind == "icloud")
		{
			return new List<int>();
		}
		List<int> titleReminderMinutes = GetTitleReminderMinutes(timeData);
		if (titleReminderMinutes.Any() || !timeData.IsDefault)
		{
			return titleReminderMinutes;
		}
		string text = ((!isAllDay) ? TaskDefaultDao.GetDefaultSafely()?.TimeReminders : TaskDefaultDao.GetDefaultSafely()?.AllDayReminders);
		if (!string.IsNullOrEmpty(text))
		{
			return text.Split(',').Select(TriggerUtils.TriggerToReminder).ToList();
		}
		return new List<int>();
	}

	private void DisableTitleParseForManualTime(CalendarDetailViewModel model)
	{
		if (model != null && model.IsNew && !_titleParseDisabledByManualTime)
		{
			_titleParseDisabledByManualTime = true;
			_titleParseOriginalTimeData = null;
			TitleText.DateParseMode = TitleDateParseMode.Auto;
			TitleText.ClearParseDate();
		}
	}

	private void RefreshReminderDisplay(CalendarDetailViewModel model)
	{
		ReminderRun.Text = ReminderUtils.GetCalendarReminderText(model.Reminders, model.IsAllDay);
		ReminderText.Visibility = (string.IsNullOrEmpty(ReminderRun.Text) ? Visibility.Collapsed : Visibility.Visible);
		SnoozeRun.Text = string.Empty;
	}

	private CalendarDetailViewModel GetModel()
	{
		if (base.DataContext != null && base.DataContext is CalendarDetailViewModel result)
		{
			return result;
		}
		return null;
	}

	private void OnDateClick(object sender, MouseButtonEventArgs e)
	{
		CalendarDetailViewModel model = GetModel();
		if (model == null || !model.Editable || !model.StartDate.HasValue)
		{
			return;
		}
		if (!model.DueDate.HasValue)
		{
			model.DueDate = (model.IsAllDay ? model.StartDate.Value.AddDays(1.0) : model.StartDate.Value.AddHours(1.0));
		}
		TimeData timeData = new TimeData
		{
			StartDate = model.StartDate,
			DueDate = model.DueDate,
			IsAllDay = model.IsAllDay,
			RepeatFlag = model.RepeatFlag,
			IsDefault = false
		};
		if (model.Reminders != null && model.Reminders.Any())
		{
			List<TaskReminderModel> list = new List<TaskReminderModel>();
			foreach (int reminder in model.Reminders)
			{
				list.Add(new TaskReminderModel
				{
					trigger = TriggerUtils.ReminderToTrigger(reminder)
				});
			}
			timeData.Reminders = list;
		}
		bool showRemind = CalendarEventDao.GetCalendarAccount(model.CalendarId)?.Kind != "icloud";
		SetDateDialog dialog = SetDateDialog.GetDialog();
		dialog.ClearEventHandle();
		dialog.Save += OnTimeSelect;
		dialog.Hided += OnPopupClosed;
		dialog.Show(timeData, new SetDateDialogArgs(calendarMode: true, isNote: false, itemMode: false, DateGrid, -4.0, -4.0, PlacementMode.Bottom, showQuickDate: false, canSkip: true, showRemind, showAnnoyingAlert: false));
		this.InOperate?.Invoke(this, null);
		e.Handled = true;
	}

	private async void OnTimeSelect(object sender, TimeData timeData)
	{
		CalendarDetailViewModel model = GetModel();
		if (model == null || !(timeData?.StartDate.HasValue ?? false))
		{
			return;
		}
		model.IsAllDay = timeData.IsAllDay ?? true;
		if (model.IsAllDay)
		{
			model.StartDate = timeData.StartDate.Value.Date;
			model.DueDate = timeData.DueDate ?? model.StartDate.Value.Date.AddDays(1.0);
		}
		else
		{
			model.StartDate = timeData.StartDate;
			model.DueDate = timeData.DueDate;
		}
		model.RepeatFlag = timeData.RepeatFlag;
		model.RepeatText = RRuleUtils.RRule2String(string.Empty, model.RepeatFlag, model.StartDate);
		if (timeData.Reminders.Any())
		{
			List<int> list = new List<int>();
			foreach (TaskReminderModel reminder in timeData.Reminders)
			{
				list.Add(TriggerUtils.TriggerToReminder(reminder.trigger));
			}
			model.Reminders = list;
		}
		else
		{
			model.Reminders = new List<int>();
		}
		timeData.StartDate = model.StartDate;
		timeData.DueDate = model.DueDate;
		DisableTitleParseForManualTime(model);
		if (!model.IsNew)
		{
			await CalendarService.SaveEventTime(model.Id, timeData);
			NotifyEventChanged();
		}
		RefreshReminderDisplay(model);
	}

	private void OnAttendeeClick(object sender, MouseButtonEventArgs e)
	{
		Utils.Toast(Utils.GetString("EditingIsNotSupportedYet"));
	}

	private async void OnCalendarDelete()
	{
		CalendarDetailViewModel model = GetModel();
		if (model != null)
		{
			string id = ArchivedDao.GetOriginalId(model.Id);
			await CalendarService.DeleteEvent(id);
			CalendarEventModel calendarEventModel = await CalendarEventDao.GetEventById(id);
			if (calendarEventModel != null)
			{
				UndoToast undoToast = new UndoToast();
				undoToast.InitEventUndo(calendarEventModel.EventId, model.Title);
				Utils.GetToastWindow().Toast(undoToast);
			}
		}
		Utils.FindParent<ListViewContainer>(this)?.TryExtractDetail();
	}

	private void OnSelectCalendarClick(object sender, MouseButtonEventArgs e)
	{
		if (SetProjectPopup.IsOpen)
		{
			return;
		}
		e.Handled = true;
		CalendarDetailViewModel model = GetModel();
		if (model == null || !model.Editable)
		{
			return;
		}
		if (!model.Organizer)
		{
			Utils.FindParent<IToastShowWindow>(this)?.TryToastString(null, Utils.GetString("NotOrganizerToMove"));
			return;
		}
		BindCalendarModel bindCalendarModel = CacheManager.GetBindCalendars().FirstOrDefault((BindCalendarModel cal) => cal.Id == model.CalendarId);
		if (bindCalendarModel != null)
		{
			ProjectOrGroupPopup projectOrGroupPopup = new ProjectOrGroupPopup(SetProjectPopup, new ProjectExtra
			{
				SubscribeCalendars = new List<string> { model.CalendarId }
			}, new ProjectSelectorExtra
			{
				isCalendarMode = true,
				batchMode = false,
				accountId = (model.IsNew ? SubscribeCalendarHelper.GetCalendarSelectorAccountIdForCreate(bindCalendarModel.AccountId) : bindCalendarModel.AccountId)
			});
			projectOrGroupPopup.ItemSelect -= CalendarItemSelect;
			projectOrGroupPopup.ItemSelect += CalendarItemSelect;
			projectOrGroupPopup.Show();
		}
	}

	private async void CalendarItemSelect(object sender, SelectableItemViewModel selectableModel)
	{
		if (selectableModel is SubscribeCalendarViewModel subscribeCalendarViewModel)
		{
			SetProjectPopup.IsOpen = false;
			await SelectCalendar(subscribeCalendarViewModel.Id, subscribeCalendarViewModel.Title);
		}
	}

	private async void OnQuickItemSelected(object sender, QuickSetModel e)
	{
		if (e != null && e.Type == QuickSetType.CalId && !string.IsNullOrEmpty(e.CalId))
		{
			await SelectCalendar(e.CalId, e.Title);
		}
	}

	private async Task SelectCalendar(string calendarId, string title)
	{
		CalendarDetailViewModel model = GetModel();
		BindCalendarModel calendar = CacheManager.GetBindCalendars().FirstOrDefault((BindCalendarModel item) => item.Id == calendarId);
		if (model != null && calendar != null)
		{
			BindCalendarAccountModel bindCalendarAccountModel = CacheManager.GetBindCalendarAccounts().FirstOrDefault((BindCalendarAccountModel item) => item.Id == calendar.AccountId);
			if (model.IsNew)
			{
				model.CalendarId = calendar.Id;
				model.CalendarName = title ?? calendar.Name;
				RefreshQuickSetCalendarContext();
			}
			else if (bindCalendarAccountModel == null || !bindCalendarAccountModel.IsCalDav())
			{
				model.CalendarId = calendar.Id;
				model.CalendarName = title ?? calendar.Name;
				RefreshQuickSetCalendarContext();
				await CalendarService.MoveCalendar(model.Id, calendar.Id);
				NotifyEventChanged();
			}
			else
			{
				await CalendarService.MoveCalendar(model.Id, calendar.Id);
				SyncManager.TryDelaySync();
			}
		}
	}

	private async void NotifyEventChanged()
	{
		CalendarDetailViewModel model = GetModel();
		if (model != null)
		{
			CalendarEventChangeNotifier.NotifyEventChanged(model.Data);
			SyncManager.TryDelaySync();
		}
	}

	private void OnExpandClick(object sender, MouseButtonEventArgs e)
	{
		CalendarDetailViewModel model = GetModel();
		if (model != null)
		{
			model.AttendeeExpand = !model.AttendeeExpand;
			model.DisplayAttendees = ((!model.AttendeeExpand) ? model.Attendees : model.Attendees?.Take(5).ToList());
		}
	}

	private void OnPopupOpened(object sender, EventArgs e)
	{
		this.InOperate?.Invoke(this, null);
	}

	private void OnPopupClosed(object sender, EventArgs e)
	{
		this.StopOperate?.Invoke(this, null);
	}

	private void OnConferenceClick(object sender, MouseButtonEventArgs e)
	{
		CalendarDetailViewModel model = GetModel();
		if (model != null)
		{
			Utils.TryProcessStartUrl(model.ConferenceUri);
		}
	}

	public void ClearEvent()
	{
		this.HideDetail = null;
	}

	public void ShowBackMenu()
	{
		BackGrid.Visibility = Visibility.Visible;
	}

	public void HideBackMenu()
	{
		BackGrid.Visibility = Visibility.Collapsed;
	}

	private async void OnMoreClick(object sender, MouseButtonEventArgs e)
	{
		CalendarDetailViewModel model = GetModel();
		if (model == null)
		{
			return;
		}
		if (model.IsNew)
		{
			ShowNewEventConvertMenu(model);
			return;
		}
		EventArchiveArgs args = new EventArchiveArgs(model.Data);
		args.Id = ArchivedDao.GetOriginalId(args.Id);
		string archiveKey = ArchivedEventDao.GenerateEventKey(args);
		bool archived = await ArchivedDao.ExistArchivedModel(archiveKey);
		List<CustomMenuItemViewModel> list = new List<CustomMenuItemViewModel>();
		list.Add(new CustomMenuItemViewModel("archive", Utils.GetString(archived ? "CancelArchive" : "Archive"), Utils.GetGeometry(archived ? "Ic.Habit.Unarchive" : "Ic.Habit.Archive")));
		if (model.AvailableType != CalendarAvailableType.NoAccess)
		{
			list.Add(new CustomMenuItemViewModel("delete", Utils.GetString("Delete"), Utils.GetGeometry("Ic.ProjectList.Delete"))
			{
				IsDelete = true
			});
		}
		CustomMenuList customMenuList = new CustomMenuList(list, MorePopup);
		customMenuList.Operated += async delegate(object o, object key)
		{
			if (key.ToString() == "delete")
			{
				OnCalendarDelete();
			}
			if (key.ToString() == "archive")
			{
				if (!archived)
				{
					await ArchivedEventDao.AddArchivedModel(args);
				}
				else
				{
					await ArchivedEventDao.RemoveArchivedEvent(archiveKey);
				}
				await SearchHelper.OnEventArchiveChanged(model.Data.Id);
				EventArchiveSyncService.PushLocalArchivedModels();
				this.EventArchiveChanged?.Invoke(this, !archived);
			}
			if (ABTestManager.IsNewRemindCalculate())
			{
				await EventReminderCalculator.RecalEventReminders(await CalendarEventDao.GetEventByEventIdOrId(model.Data.Id));
			}
			else
			{
				ReminderCalculator.AssembleReminders();
			}
		};
		customMenuList.Show();
	}

	private void ShowNewEventConvertMenu(CalendarDetailViewModel model)
	{
		if (model.ShowQuickAddConvertOptions)
		{
			CustomMenuList customMenuList = new CustomMenuList(new List<CustomMenuItemViewModel>
			{
				new CustomMenuItemViewModel("switch_task", Utils.GetString("ConvertToTask"), Utils.GetGeometry("Ic.TaskOM.ConvertToTask")),
				new CustomMenuItemViewModel("switch_note", Utils.GetString("ConvertToNote"), Utils.GetGeometry("Ic.TaskOM.ConvertToNote"))
			}, MorePopup);
			customMenuList.AutoClose = false;
			customMenuList.Operated += OnNewEventConvertSelected;
			customMenuList.Show();
		}
	}

	private void RefreshQuickSetCalendarContext()
	{
		string currentCalendarAccountId = GetCurrentCalendarAccountId();
		TitleText.QuickSetUsedInCalendar = true;
		LocationText.QuickSetUsedInCalendar = true;
		NewContentText.QuickSetUsedInCalendar = true;
		TitleText.QuickSetAccountId = currentCalendarAccountId;
		LocationText.QuickSetAccountId = currentCalendarAccountId;
		NewContentText.QuickSetAccountId = currentCalendarAccountId;
		string currentCalendarId = GetCurrentCalendarId();
		TitleText.QuickSetCalendarId = currentCalendarId;
		LocationText.QuickSetCalendarId = currentCalendarId;
		NewContentText.QuickSetCalendarId = currentCalendarId;
	}

	private string GetCurrentCalendarId()
	{
		return GetModel()?.CalendarId ?? string.Empty;
	}

	private string GetCurrentCalendarAccountId()
	{
		return GetCalendarAccountId(GetCurrentCalendarId());
	}

	private string GetCurrentCalendarSelectorAccountId()
	{
		string currentCalendarAccountId = GetCurrentCalendarAccountId();
		CalendarDetailViewModel model = GetModel();
		if (model == null || !model.IsNew)
		{
			return currentCalendarAccountId;
		}
		return SubscribeCalendarHelper.GetCalendarSelectorAccountIdForCreate(currentCalendarAccountId);
	}

	private static string GetCalendarAccountId(string calendarId)
	{
		if (string.IsNullOrEmpty(calendarId))
		{
			return string.Empty;
		}
		return CacheManager.GetBindCalendars().FirstOrDefault((BindCalendarModel calendar) => calendar.Id == calendarId)?.AccountId ?? string.Empty;
	}

	private void OnNewEventConvertSelected(object sender, object key)
	{
		if (key != null)
		{
			CalendarDetailViewModel model = GetModel();
			if (model != null)
			{
				SyncNewEventModelFromInput(model);
				MorePopup.RestoreFocus = false;
				MorePopup.IsOpen = false;
				this.ConvertNewEventToTaskKind?.Invoke(this, new CalendarDetailConvertEventArgs(model, key.ToString() == "switch_note", _titleParseDisabledByManualTime));
			}
		}
	}

	private void SyncNewEventModelFromInput(CalendarDetailViewModel model)
	{
		model.Title = GetTitleTextFromInput(trim: false);
		model.Content = NewContentText.Text?.Trim() ?? string.Empty;
		model.Location = LocationText.Text?.Trim() ?? string.Empty;
		if (model.SourceTaskViewModel != null)
		{
			model.SourceTaskViewModel.Title = model.Title;
		}
	}

	private string GetTitleTextFromInput(bool trim)
	{
		string text = (TitleText.ParsingDate ? TitleText.GetParsedText() : TitleText.Text);
		object obj;
		if (trim)
		{
			obj = text?.Trim() ?? string.Empty;
		}
		else
		{
			obj = text;
			if (obj == null)
			{
				return string.Empty;
			}
		}
		return (string)obj;
	}

	private async void OnCreateClick(object sender, RoutedEventArgs e)
	{
		await TryCreateNewEventAsync();
	}

	public async Task<bool> TryCreateNewEventOnLostFocusAsync()
	{
		return await TryCreateNewEventAsync();
	}

	private async Task<bool> TryCreateNewEventAsync()
	{
		CalendarDetailViewModel model = GetModel();
		if (model == null || !model.IsNew || _creatingEvent)
		{
			return false;
		}
		_creatingEvent = true;
		try
		{
			model.Title = GetTitleTextFromInput(trim: true);
			model.Content = NewContentText.Text?.Trim() ?? string.Empty;
			model.Location = LocationText.Text?.Trim() ?? string.Empty;
			if (string.IsNullOrWhiteSpace(model.Title) && string.IsNullOrWhiteSpace(model.Content))
			{
				return false;
			}
			if (model.Title.Length > 2048)
			{
				Utils.Toast(Utils.GetString("CharactersNumberLimit"));
				return false;
			}
			BindCalendarModel calendar = CacheManager.GetBindCalendars().FirstOrDefault((BindCalendarModel cal) => cal.Id == model.CalendarId);
			if (calendar == null)
			{
				return false;
			}
			BindCalendarAccountModel accountCalById = CacheManager.GetAccountCalById(calendar.AccountId);
			string text = ((accountCalById != null && accountCalById.IsCalDav()) ? Guid.NewGuid().ToString() : Utils.GetGuid());
			CalendarEventModel calEvent = model.Data;
			calEvent.UserId = Utils.GetCurrentUserIdInt().ToString();
			calEvent.Id = text + "@" + calendar.Id;
			calEvent.Uid = text;
			calEvent.Title = model.Title;
			calEvent.Content = model.Content;
			calEvent.Location = model.Location;
			calEvent.EventId = text;
			calEvent.CalendarId = calendar.Id;
			calEvent.Type = 0;
			calEvent.TimeZone = model.TimeZone ?? Utils.GetLocalTimeZone();
			calEvent.IsAllDay = model.IsAllDay;
			calEvent.DueStart = model.StartDate ?? DateTime.Today;
			calEvent.DueEnd = model.DueDate ?? (model.IsAllDay ? calEvent.DueStart.Value.Date.AddDays(1.0) : calEvent.DueStart.Value.AddMinutes(TaskDefaultDao.GetDefaultSafely()?.Duration ?? 60));
			if (calEvent.IsAllDay && calEvent.DueStart.Value.Date == calEvent.DueEnd.Value.Date)
			{
				calEvent.DueEnd = calEvent.DueEnd.Value.Date.AddDays(1.0);
			}
			calEvent.RepeatFlag = model.RepeatFlag;
			if (model.Reminders != null && model.Reminders.Any() && accountCalById?.Kind != "icloud")
			{
				calEvent.ReminderList = model.Reminders;
				calEvent.Reminders = JsonConvert.SerializeObject(model.Reminders);
			}
			else
			{
				calEvent.ReminderList = null;
				calEvent.Reminders = null;
			}
			await CalendarService.AddEvent(calEvent);
			model.Id = calEvent.Id;
			model.IsNew = false;
			model.CalendarId = calendar.Id;
			model.CalendarName = calendar.Name;
			model.AvailableType = CalendarAvailableType.Editable;
			DataChangedNotifier.NotifyCalendarChanged(200);
			SyncManager.TryDelaySync();
			this.EventCreated?.Invoke(this, calEvent);
			return true;
		}
		finally
		{
			_creatingEvent = false;
		}
	}

	public async Task<bool> IsArchived()
	{
		CalendarDetailViewModel model = GetModel();
		if (model != null)
		{
			EventArchiveArgs eventArchiveArgs = new EventArchiveArgs(model.Data);
			eventArchiveArgs.Id = ArchivedDao.GetOriginalId(eventArchiveArgs.Id);
			return await ArchivedDao.ExistArchivedModel(ArchivedEventDao.GenerateEventKey(eventArchiveArgs));
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
			Uri resourceLocator = new Uri("/TickTick;component/views/calendar/calendardetailcontrol.xaml", UriKind.Relative);
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
			Root = (CalendarDetailControl)target;
			Root.DataContextChanged += OnDataBind;
			break;
		case 2:
			ScrollViewer = (ScrollViewer)target;
			break;
		case 3:
			BackGrid = (Grid)target;
			BackGrid.MouseLeftButtonUp += OnBackClick;
			break;
		case 4:
			TitleText = (DetailTextBox)target;
			break;
		case 5:
			TitleTextHint = (TextBlock)target;
			break;
		case 6:
			((Image)target).MouseLeftButtonUp += OnConferenceClick;
			break;
		case 7:
			DateGrid = (Grid)target;
			DateGrid.MouseLeftButtonUp += OnDateClick;
			break;
		case 8:
			ReminderText = (Grid)target;
			break;
		case 9:
			ReminderRun = (Run)target;
			break;
		case 10:
			SnoozeRun = (Run)target;
			break;
		case 12:
			((Grid)target).MouseLeftButtonUp += OnExpandClick;
			break;
		case 13:
			LocationText = (DetailTextBox)target;
			break;
		case 14:
			LocationTextHint = (TextBlock)target;
			break;
		case 15:
			DescriptionGrid = (Grid)target;
			break;
		case 16:
			NewContentText = (DetailTextBox)target;
			break;
		case 17:
			NewContentTextHint = (TextBlock)target;
			break;
		case 18:
			CalendarMovePanel = (Grid)target;
			CalendarMovePanel.MouseLeftButtonUp += OnSelectCalendarClick;
			break;
		case 19:
			SetProjectPopup = (EscPopup)target;
			break;
		case 20:
			CreateButton = (Button)target;
			CreateButton.Click += OnCreateClick;
			break;
		case 21:
			MoreGrid = (HoverIconButton)target;
			break;
		case 22:
			MorePopup = (EscPopup)target;
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
		if (connectionId == 11)
		{
			((StackPanel)target).MouseLeftButtonUp += OnAttendeeClick;
		}
	}
}
