using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;
using TickTickDao;
using TickTickModels;
using ticktick_WPF.Cache;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Calendar;

public class CalendarDetailViewModel : BaseViewModel
{
	public const int AttendeeCount = 5;

	private bool _attendeeExpand;

	private string _calendarId;

	private string _calendarName;

	private string _content;

	private List<CalendarAttendeeModel> _displayAttendees;

	private DateTime? _dueDate;

	private bool _isAllDay;

	private string _location;

	private List<int> _reminders;

	private string _repeatFlag;

	private string _repeatText;

	private DateTime? _startDate;

	private string _title;

	private bool _isNew;

	private bool _showQuickAddConvertOptions;

	private bool _useTaskPopupAlignment;

	private TaskBaseViewModel _sourceTaskViewModel;

	public bool Organizer;

	public CalendarAvailableType _availableType;

	public CalendarAvailableType AvailableType
	{
		get
		{
			return _availableType;
		}
		set
		{
			_availableType = value;
			OnPropertyChanged("AvailableType");
		}
	}

	public bool Editable { get; set; }

	public bool ShowAttendeeExpand { get; set; }

	public CalendarEventModel Data { get; }

	public string Id { get; set; }

	public bool IsNew
	{
		get
		{
			return _isNew;
		}
		set
		{
			_isNew = value;
			OnPropertyChanged("IsNew");
		}
	}

	public bool ShowQuickAddConvertOptions
	{
		get
		{
			return _showQuickAddConvertOptions;
		}
		set
		{
			_showQuickAddConvertOptions = value;
			OnPropertyChanged("ShowQuickAddConvertOptions");
		}
	}

	public bool UseTaskPopupAlignment
	{
		get
		{
			return _useTaskPopupAlignment;
		}
		set
		{
			_useTaskPopupAlignment = value;
			OnPropertyChanged("UseTaskPopupAlignment");
			OnPropertyChanged("CalendarMovePanelMargin");
		}
	}

	public Thickness CalendarMovePanelMargin
	{
		get
		{
			if (!UseTaskPopupAlignment)
			{
				return new Thickness(20.0, 0.0, 0.0, 0.0);
			}
			return new Thickness(16.0, 0.0, 0.0, 0.0);
		}
	}

	public TaskBaseViewModel SourceTaskViewModel
	{
		get
		{
			return _sourceTaskViewModel;
		}
		set
		{
			_sourceTaskViewModel = value;
			SyncSourceTaskPreview();
		}
	}

	public bool IsShowConference { get; set; }

	public string ConferenceName { get; set; }

	public string ConferenceType { get; set; }

	public string ConferenceUri { get; set; }

	public string Title
	{
		get
		{
			return _title;
		}
		set
		{
			_title = value;
			Data.Title = value;
			OnPropertyChanged("Title");
			SyncSourceTaskPreview();
		}
	}

	public string Content
	{
		get
		{
			return _content;
		}
		set
		{
			_content = value;
			Data.Content = value;
			OnPropertyChanged("Content");
			SyncSourceTaskPreview();
		}
	}

	public bool IsAllDay
	{
		get
		{
			return _isAllDay;
		}
		set
		{
			_isAllDay = value;
			Data.IsAllDay = value;
			OnPropertyChanged("IsAllDay");
			SyncSourceTaskPreview();
		}
	}

	public DateTime? StartDate
	{
		get
		{
			return _startDate;
		}
		set
		{
			_startDate = value;
			Data.DueStart = value;
			OnPropertyChanged("StartDate");
			SyncSourceTaskPreview();
		}
	}

	public DateTime? DueDate
	{
		get
		{
			return _dueDate;
		}
		set
		{
			_dueDate = value;
			Data.DueEnd = value;
			OnPropertyChanged("DueDate");
			SyncSourceTaskPreview();
		}
	}

	public string RepeatFlag
	{
		get
		{
			return _repeatFlag;
		}
		set
		{
			_repeatFlag = value;
			Data.RepeatFlag = value;
			OnPropertyChanged("RepeatFlag");
			SyncSourceTaskPreview();
		}
	}

	public string RepeatText
	{
		get
		{
			return _repeatText;
		}
		set
		{
			_repeatText = value;
			OnPropertyChanged("RepeatText");
		}
	}

	public string CalendarId
	{
		get
		{
			return _calendarId;
		}
		set
		{
			_calendarId = value;
			Data.CalendarId = value;
			OnPropertyChanged("CalendarId");
			SyncSourceTaskPreview();
		}
	}

	public string CalendarName
	{
		get
		{
			return _calendarName;
		}
		set
		{
			_calendarName = value;
			OnPropertyChanged("CalendarName");
		}
	}

	public string Location
	{
		get
		{
			return _location;
		}
		set
		{
			_location = value;
			Data.Location = value;
			OnPropertyChanged("Location");
		}
	}

	public string TimeZone
	{
		get
		{
			return Data.TimeZone;
		}
		set
		{
			Data.TimeZone = value;
			OnPropertyChanged("TimeZone");
		}
	}

	public int Mode { get; set; }

	public List<CalendarAttendeeModel> Attendees { get; set; }

	public List<CalendarAttendeeModel> DisplayAttendees
	{
		get
		{
			return _displayAttendees;
		}
		set
		{
			_displayAttendees = value;
			OnPropertyChanged("DisplayAttendees");
		}
	}

	public string AttendeeGuestsCount => string.Format(Utils.GetString("XGuests"), Attendees?.Count ?? 0);

	public bool ShowAttend { get; set; }

	public List<int> Reminders
	{
		get
		{
			return _reminders;
		}
		set
		{
			_reminders = value;
			Data.ReminderList = value;
			Data.Reminders = JsonConvert.SerializeObject(value);
			OnPropertyChanged("Reminders");
		}
	}

	public bool AttendeeExpand
	{
		get
		{
			return _attendeeExpand;
		}
		set
		{
			_attendeeExpand = value;
			OnPropertyChanged("AttendeeExpand");
		}
	}

	public CalendarDetailViewModel(BindCalendarModel calendar)
	{
		Data = new CalendarEventModel();
		CalendarName = calendar.Name;
		CalendarId = calendar.Id;
		IsNew = true;
		ShowQuickAddConvertOptions = true;
		Editable = true;
		Organizer = true;
		AvailableType = CalendarAvailableType.Editable;
		TimeZone = Utils.GetLocalTimeZone();
	}

	public CalendarDetailViewModel(CalendarEventModel model)
	{
		Data = model;
		Id = model.Id;
		Title = model.Title ?? string.Empty;
		Content = model.Content ?? string.Empty;
		StartDate = model.DueStart;
		DueDate = model.DueEnd;
		IsAllDay = model.IsAllDay;
		Location = model.Location;
		RepeatFlag = model.RepeatFlag;
		RepeatText = RRuleUtils.RRule2String(string.Empty, string.IsNullOrEmpty(model.RepeatFlag) ? model.Recurrence : model.RepeatFlag, model.DueStart);
		Editable = model.Editable;
		AvailableType = model.AvailableType;
		Organizer = true;
		if (model.Attendees != null && model.Attendees.Any())
		{
			ShowAttend = model.Attendees.Any();
			AttendeeExpand = ShowAttend && model.Attendees.Count >= 5;
			ShowAttendeeExpand = AttendeeExpand;
			Attendees = model.Attendees.OrderByDescending((CalendarAttendeeModel attendee) => attendee.Organizer).ToList();
			DisplayAttendees = Attendees.Take(5).ToList();
			CalendarAttendeeModel calendarAttendeeModel = model.Attendees.FirstOrDefault((CalendarAttendeeModel a) => a.Self);
			if (calendarAttendeeModel != null)
			{
				Organizer = calendarAttendeeModel.Organizer;
			}
		}
		if (ShowAttend)
		{
			Attendees = Attendees?.OrderByDescending((CalendarAttendeeModel attendee) => attendee.Organizer).ToList();
		}
		CalendarAttendeeModel calendarAttendeeModel2 = Attendees?.FirstOrDefault((CalendarAttendeeModel attendee) => !attendee.Organizer);
		if (calendarAttendeeModel2 != null)
		{
			calendarAttendeeModel2.FirstGuest = true;
		}
		Reminders = model.ReminderList ?? new List<int>();
		if (model.Type == 0)
		{
			BindCalendarModel bindCalendarModel = CacheManager.GetBindCalendars().FirstOrDefault((BindCalendarModel cal) => cal.Id == model.CalendarId);
			if (bindCalendarModel != null)
			{
				CalendarName = bindCalendarModel.Name;
				CalendarId = bindCalendarModel.Id;
			}
		}
		else
		{
			CalendarSubscribeProfileModel calendarSubscribeProfileModel = CacheManager.GetSubscribeCalendars().FirstOrDefault((CalendarSubscribeProfileModel cal) => cal.Id == model.CalendarId);
			if (calendarSubscribeProfileModel != null)
			{
				CalendarName = calendarSubscribeProfileModel.Name;
				CalendarId = calendarSubscribeProfileModel.Id;
			}
		}
		if (model.Conference == null)
		{
			return;
		}
		IsShowConference = true;
		if (model.Conference.EntryPoints == null || (model.Conference.EntryPoints.Count == 1 && model.Conference.EntryPoints.First().EntryPointType == "more"))
		{
			IsShowConference = false;
			return;
		}
		ConferenceName = model.Conference.Name;
		EntryPointsModel entryPointsModel = model.Conference.EntryPoints.FirstOrDefault((EntryPointsModel entity) => entity.EntryPointType == "video");
		if (entryPointsModel == null)
		{
			entryPointsModel = model.Conference.EntryPoints.FirstOrDefault((EntryPointsModel entity) => entity.EntryPointType == "phone");
		}
		if (entryPointsModel == null)
		{
			entryPointsModel = model.Conference.EntryPoints.FirstOrDefault((EntryPointsModel entity) => entity.EntryPointType == "more");
		}
		if (entryPointsModel == null)
		{
			entryPointsModel = model.Conference.EntryPoints.FirstOrDefault();
		}
		ConferenceType = entryPointsModel.EntryPointType;
		ConferenceUri = entryPointsModel.Uri;
	}

	public void SyncSourceTaskPreview()
	{
		if (_sourceTaskViewModel != null && IsNew)
		{
			_sourceTaskViewModel.Type = DisplayType.Event;
			_sourceTaskViewModel.CalendarId = CalendarId;
			_sourceTaskViewModel.Color = GetPreviewCalendarColor(CalendarId);
			_sourceTaskViewModel.Title = Title ?? string.Empty;
			_sourceTaskViewModel.Content = Content ?? string.Empty;
			_sourceTaskViewModel.StartDate = StartDate;
			_sourceTaskViewModel.DueDate = DueDate;
			_sourceTaskViewModel.IsAllDay = IsAllDay;
			_sourceTaskViewModel.RepeatFlag = RepeatFlag;
			_sourceTaskViewModel.Status = 0;
		}
	}

	private static string GetPreviewCalendarColor(string calendarId)
	{
		BindCalendarModel bindCalendarModel = CacheManager.GetBindCalendars().FirstOrDefault((BindCalendarModel calendar) => calendar.Id == calendarId);
		if (!string.IsNullOrEmpty(bindCalendarModel?.Color) && bindCalendarModel.Color.ToLower() != "transparent")
		{
			return bindCalendarModel.Color;
		}
		CalendarSubscribeProfileModel calendarSubscribeProfileModel = CacheManager.GetSubscribeCalendars().FirstOrDefault((CalendarSubscribeProfileModel calendar) => calendar.Id == calendarId);
		if (!string.IsNullOrEmpty(calendarSubscribeProfileModel?.Color) && calendarSubscribeProfileModel.Color.ToLower() != "transparent")
		{
			return calendarSubscribeProfileModel.Color;
		}
		return ThemeUtil.GetColorString("ColorPrimary", inDark: false);
	}

	public async Task<string> GetSnoozeText()
	{
		List<int> reminders = Reminders;
		if (reminders != null && reminders.Count > 0 && StartDate.HasValue)
		{
			string text = Id;
			if (!string.IsNullOrEmpty(text) && text.Contains("@"))
			{
				int length = text.IndexOf("@", StringComparison.Ordinal);
				text = text.Substring(0, length);
			}
			List<ReminderDelayModel> list = (await ReminderDelayDao.GetDelayModelById(text, "calendar")).Where((ReminderDelayModel delay) => delay.SyncStatus != -1).ToList();
			if (list != null && list.Count > 0)
			{
				ReminderDelayModel reminderDelayModel = list[0];
				if (reminderDelayModel != null && reminderDelayModel.NextTime.HasValue && reminderDelayModel.NextTime.Value > DateTime.Now)
				{
					bool flag = false;
					foreach (int reminder in Reminders)
					{
						DateTime value = StartDate.Value.AddMinutes(reminder);
						DateTime? remindTime = reminderDelayModel.RemindTime;
						if (value == remindTime)
						{
							flag = true;
							break;
						}
					}
					if (flag)
					{
						DateTime value2 = reminderDelayModel.NextTime.Value;
						if (value2 > DateTime.Now)
						{
							if (Math.Abs((DateTime.Now.Date - value2.Date).TotalDays - -1.0) <= 0.001)
							{
								return string.Format(Utils.GetString("PreviewSnoozeText"), Utils.GetString("Tomorrow"));
							}
							if (Math.Abs((DateTime.Now.Date - value2.Date).TotalDays) <= 0.001)
							{
								return string.Format(Utils.GetString("PreviewSnoozeText"), DateUtils.FormatHourMinute(value2));
							}
							return string.Format(Utils.GetString("PreviewSnoozeText"), DateUtils.FormatYearMonthDay(value2, DateFormatLen.Middle) + " " + DateUtils.FormatHourMinute(value2));
						}
						return string.Empty;
					}
				}
			}
		}
		return null;
	}
}
