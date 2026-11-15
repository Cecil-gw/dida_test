using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Habit;

namespace ticktick_WPF.Views.Calendar.Agenda;

public class CalendarAgendaItemViewModel : BaseViewModel
{
	private double _height = 90.0;

	private double _top;

	private bool _selected;

	public int Index;

	public CalendarAgendaItemCanvas Parent;

	public CalendarAgendaItemViewModel FrontItem;

	public CalendarAgendaItemViewModel NextItem;

	private ObservableCollection<TagViewModel> _displayTags;

	private bool _inDark;

	private string _avatarUrl;

	private string _itemColor;

	public double Height
	{
		get
		{
			return Math.Max(_height, IsEmpty ? 48 : 56);
		}
		set
		{
			if (!(Math.Abs(_height - value) > 1.0))
			{
				return;
			}
			if (!Showing)
			{
				_height = value;
				return;
			}
			if (Index >= Parent?.StartIndex)
			{
				if (NextItem != null && NextItem.Showing)
				{
					NextItem.Top = value + Top;
				}
				if (Index == Parent?.StartIndex && FrontItem != null && FrontItem.Showing)
				{
					FrontItem.Top = Top - FrontItem.Height;
				}
			}
			else if (Index < Parent?.StartIndex && Showing)
			{
				Top = _top + _height - value;
			}
			_height = value;
		}
	}

	public double Top
	{
		get
		{
			if (_top < 0.0)
			{
				return 0.0;
			}
			return _top;
		}
		set
		{
			if (!(Math.Abs(_top - value) > 1.0))
			{
				return;
			}
			_top = value;
			OnPropertyChanged("Top");
			if (Index == Parent?.StartIndex)
			{
				if (NextItem != null && NextItem.Showing)
				{
					NextItem.Top = value + Height;
				}
				if (FrontItem != null && FrontItem.Showing)
				{
					FrontItem.Top = value - FrontItem.Height;
				}
			}
			else if (Index > Parent?.StartIndex)
			{
				if (NextItem != null && NextItem.Showing)
				{
					NextItem.Top = value + Height;
				}
			}
			else if (Index < Parent?.StartIndex && FrontItem != null && FrontItem.Showing)
			{
				FrontItem.Top = value - FrontItem.Height;
			}
		}
	}

	public TaskBaseViewModel SourceViewModel { get; set; }

	public string Id => SourceViewModel.Id;

	public string Title
	{
		get
		{
			if (!HideVirtualContent)
			{
				return SourceViewModel.TitleWithoutLink;
			}
			return "  ";
		}
	}

	public string Content => SourceViewModel.DisplayContent;

	public string ItemColor
	{
		get
		{
			return _itemColor;
		}
		set
		{
			string text = value;
			if (!string.IsNullOrEmpty(text) && (text.Length == 6 || text.Length == 8) && !text.StartsWith("#"))
			{
				text = "#" + text;
			}
			if (_itemColor != text)
			{
				_itemColor = text;
				OnPropertyChanged("ItemColor");
			}
		}
	}

	public bool Checkable
	{
		get
		{
			if ((SourceViewModel.IsTask || SourceViewModel.IsAgenda || SourceViewModel.IsCheckItem || SourceViewModel.IsHabit) && SourceViewModel.Editable && !IsEmpty && RepeatDiff == 0.0)
			{
				return string.IsNullOrEmpty(EmptyText);
			}
			return false;
		}
	}

	public bool IsEmpty { get; set; }

	public bool IsLoadAfter { get; set; }

	public bool IsLoadBefore { get; set; }

	public bool ShowRepeat => !string.IsNullOrEmpty(SourceViewModel.RepeatFlag);

	public bool ShowDescription
	{
		get
		{
			if (!string.IsNullOrEmpty(Content))
			{
				return SourceViewModel.Kind == "TEXT";
			}
			return false;
		}
	}

	public bool? IsAllDay => SourceViewModel.IsAllDay;

	public bool? ShowReminder { get; set; }

	public bool Selected
	{
		get
		{
			return _selected;
		}
		set
		{
			if (_selected != value)
			{
				_selected = value;
				OnPropertyChanged("Selected");
			}
		}
	}

	public bool Showing { get; set; }

	public string RepeatFrom => SourceViewModel.RepeatFrom;

	public string RepeatFlag => SourceViewModel.RepeatFlag;

	public DateTime? StartDate => SourceViewModel.StartDate;

	public DateTime? DueDate => SourceViewModel.DueDate;

	public int Priority => SourceViewModel.Priority;

	public DateTime? DisplayStartDate => SourceViewModel.StartDate?.AddDays(RepeatDiff);

	public DateTime? DisplayDueDate => SourceViewModel.DueDate?.AddDays(RepeatDiff);

	public double RepeatDiff { get; set; }

	public Visibility TopLineVisible { get; set; }

	public Visibility BotLineVisible { get; set; }

	public Visibility TimeSpanTextVisible { get; set; } = Visibility.Collapsed;

	public DisplayType Type => SourceViewModel.Type;

	public CourseDisplayModel Course => SourceViewModel.Course;

	public bool ShowAsAllDay
	{
		get
		{
			if (!DisplayStartDate.HasValue)
			{
				return true;
			}
			if (IsAllDay.HasValue && !IsAllDay.Value)
			{
				TimeSpan? timeSpan = DueDate - StartDate;
				if (!timeSpan.HasValue)
				{
					return false;
				}
				return timeSpan.GetValueOrDefault().TotalHours > 24.0;
			}
			return IsAllDay ?? true;
		}
	}

	public string AvatarUrl
	{
		get
		{
			return _avatarUrl ?? string.Empty;
		}
		set
		{
			_avatarUrl = value;
			OnPropertyChanged("AvatarUrl");
			SetAvatar();
		}
	}

	public BitmapImage Avatar { get; set; }

	public int Status
	{
		get
		{
			if (!SourceViewModel.IsPomo)
			{
				return SourceViewModel.Status;
			}
			return 2;
		}
	}

	public string Assignee => SourceViewModel.Assignee;

	public string ProjectId => SourceViewModel.ProjectId;

	public Geometry Icon { get; set; }

	public SolidColorBrush IconColor { get; set; }

	public string TimeSpanText { get; set; }

	public Visibility ShowBeforeDateText
	{
		get
		{
			if (!DisplayStartDate.HasValue || !(DisplayStartDate.Value.Date < Date) || ShowAsAllDay)
			{
				return Visibility.Collapsed;
			}
			return Visibility.Visible;
		}
	}

	public string BeforeDateText
	{
		get
		{
			if (DisplayStartDate.HasValue && DisplayStartDate.Value.Date < Date && !ShowAsAllDay)
			{
				return DateUtils.FormatMonthDay(DisplayStartDate.Value, DateFormatLen.Short);
			}
			return null;
		}
		set
		{
		}
	}

	public string DateText
	{
		get
		{
			if (IsEmpty)
			{
				return null;
			}
			if (ShowAsAllDay)
			{
				if (Utils.IsCn() || Utils.IsEn())
				{
					return Utils.GetString("AllDay");
				}
				return DateUtils.FormatMonthDay(Date, DateFormatLen.Short);
			}
			DateTime? dateTime = DisplayStartDate ?? SourceViewModel.CompletedTime;
			if (dateTime.HasValue)
			{
				return DateUtils.FormatHM(dateTime.Value);
			}
			return null;
		}
		set
		{
		}
	}

	public string AmPmText
	{
		get
		{
			if (IsEmpty)
			{
				return null;
			}
			if (ShowAsAllDay)
			{
				return null;
			}
			DateTime? dateTime = DisplayStartDate ?? SourceViewModel.CompletedTime;
			if (dateTime.HasValue)
			{
				if (LocalSettings.Settings.TimeFormat == "24Hour")
				{
					return null;
				}
				if (dateTime.Value.Hour >= 12)
				{
					return " PM";
				}
				return " AM";
			}
			return null;
		}
		set
		{
		}
	}

	public DateTime Date { get; set; }

	public bool ShowTitleDate { get; set; }

	public bool ShowYearText { get; set; }

	public ObservableCollection<TagViewModel> DisplayTags
	{
		get
		{
			if (_displayTags == null)
			{
				_displayTags = BuildTagModels(SourceViewModel.Tags);
			}
			return _displayTags;
		}
		set
		{
			_displayTags = value;
		}
	}

	public bool IsVirtual { get; set; }

	public BitmapSource IconImage { get; set; }

	public string EmptyText { get; set; }

	public HabitCheckInModel HabitCheckIn { get; set; }

	public bool IsWorkDay { get; set; }

	public bool IsRestDay { get; set; }

	public double Progress => SourceViewModel.Progress;

	public bool ShowProgress
	{
		get
		{
			if (Progress > 0.0)
			{
				return Status == 0;
			}
			return false;
		}
	}

	public string ProgressContent => Progress + "%";

	public bool HideVirtualContent { get; set; }

	public async Task ResetAvatars()
	{
		_avatarUrl = AvatarHelper.GetCacheUrl(SourceViewModel.Assignee, SourceViewModel.ProjectId);
		await SetAvatar();
	}

	public async Task SetAvatar()
	{
		Avatar = AvatarHelper.GetAvatarByUrl(AvatarUrl, 64, getDefault: false);
		if (Avatar == null)
		{
			Avatar = await AvatarHelper.GetAvatarByUrlAsync(AvatarUrl);
			OnPropertyChanged("Avatar");
		}
		else
		{
			OnPropertyChanged("Avatar");
		}
	}

	public static ObservableCollection<TagViewModel> BuildTagModels(IReadOnlyList<string> tags)
	{
		return new ObservableCollection<TagViewModel>();
	}

	public double GetTop()
	{
		return _top;
	}

	public void SetTop(double top)
	{
		_top = top;
		OnPropertyChanged("Top");
	}

	public CalendarAgendaItemViewModel()
	{
	}

	public CalendarAgendaItemViewModel(CalendarDisplayModel model, bool? isDark = null, string emptyText = null, bool hideVirtualContent = false)
	{
		SourceViewModel = model.SourceViewModel;
		RepeatDiff = model.repeatDiff;
		HabitCheckIn = model.CheckIn;
		EmptyText = emptyText;
		IsVirtual = model.IsVirtual;
		if (IsVirtual && hideVirtualContent)
		{
			HideVirtualContent = true;
		}
		if (isDark.HasValue)
		{
			_inDark = isDark.Value;
		}
		SetIcon();
		SetColor();
		SetTimeSpan();
		_avatarUrl = ((!string.IsNullOrEmpty(SourceViewModel.Assignee)) ? AvatarHelper.GetCacheUrl(SourceViewModel.Assignee, SourceViewModel.ProjectId) : string.Empty);
	}

	private void SetRestWork()
	{
		if (ShowTitleDate)
		{
			List<HolidayModel> list = ((LocalSettings.Settings.EnableHoliday && Utils.IsDida()) ? HolidayManager.GetCacheHolidays() : new List<HolidayModel>());
			IsWorkDay = list.Exists((HolidayModel day) => day.date == Date && day.type == 1) && SettingsHelper.GetShowHoliday();
			IsRestDay = list.Exists((HolidayModel day) => day.date == Date && day.type == 0) && SettingsHelper.GetShowHoliday();
		}
	}

	public void SetTimeSpan()
	{
		if (!DisplayStartDate.HasValue || !DisplayDueDate.HasValue)
		{
			goto IL_01e0;
		}
		if (IsAllDay ?? true)
		{
			TimeSpan? timeSpan = DueDate - StartDate;
			if (!timeSpan.HasValue || !(timeSpan.GetValueOrDefault().TotalDays > 1.0))
			{
				goto IL_01e0;
			}
		}
		TimeSpanTextVisible = Visibility.Visible;
		bool flag = !(IsAllDay ?? true);
		bool num = DisplayStartDate.Value.Date != DisplayDueDate.Value.Date;
		DateTime date = ((IsAllDay ?? true) ? DisplayDueDate.Value.AddMinutes(-1.0) : DisplayDueDate.Value);
		string text = (num ? (DateUtils.FormatMonthDay(DisplayStartDate.Value, DateFormatLen.Middle) + (flag ? " " : "")) : "") + (flag ? DateUtils.FormatHourMinuteText(DisplayStartDate.Value) : "");
		string text2 = (num ? (DateUtils.FormatMonthDay(date, DateFormatLen.Middle) + (flag ? " " : "")) : "") + (flag ? DateUtils.FormatHourMinuteText(date) : "");
		TimeSpanText = text + " - " + text2;
		goto IL_01e7;
		IL_01e0:
		TimeSpanTextVisible = Visibility.Collapsed;
		goto IL_01e7;
		IL_01e7:
		if (HideVirtualContent)
		{
			TimeSpanText = "";
		}
		OnPropertyChanged("TimeSpanText");
		OnPropertyChanged("TimeSpanTextVisible");
	}

	public void SetIcon()
	{
		if (!string.IsNullOrEmpty(EmptyText))
		{
			Icon = Utils.GetIcon("AgendaIconEmpty");
		}
		else
		{
			switch (SourceViewModel.Type)
			{
			case DisplayType.Task:
			case DisplayType.CheckItem:
			case DisplayType.Agenda:
				if (RepeatDiff > 0.0)
				{
					Icon = Utils.GetGeometry("Ic.Schedule.Repeat");
				}
				else
				{
					Icon = Utils.GetGeometry((Status == 0) ? "Ic.Schedule.Checkbox" : ((Status == -1) ? "Ic.Schedule.Abandon" : "Ic.Schedule.Complete"));
				}
				break;
			case DisplayType.Note:
				Icon = Utils.GetGeometry("Ic.Schedule.Note");
				break;
			case DisplayType.Countdown:
			{
				CountdownModel countdownModel = (SourceViewModel as CountDownBaseViewModel)?.Countdown;
				if (countdownModel != null)
				{
					IconImage = CountdownService.GetIcon(countdownModel.IconRes, countdownModel.Color);
				}
				break;
			}
			case DisplayType.Habit:
			{
				Icon = ((Status == 0) ? null : Utils.GetGeometry((Status == 2) ? "Ic.Schedule.HabitComplete" : "Ic.Schedule.HabitAbandon"));
				HabitModel habitModel = (SourceViewModel as HabitBaseViewModel)?.Habit;
				if (habitModel != null && Status == 0)
				{
					IconImage = HabitService.GetIcon(habitModel.IconRes, habitModel.Color);
				}
				break;
			}
			case DisplayType.Event:
				if (SourceViewModel.ResponseStatusType == ResponseStatusType.Declined)
				{
					Icon = Utils.GetGeometry("Ic.Schedule.Declined");
					break;
				}
				switch (SubscribeCalendarHelper.GetCalendarType(SourceViewModel.CalendarId))
				{
				case 1:
				{
					Geometry icon = (Icon = Utils.GetGeometry("Ic.Schedule.Google"));
					Icon = icon;
					break;
				}
				case 5:
				{
					Geometry icon = (Icon = Utils.GetGeometry("Ic.Schedule.Caldav"));
					Icon = icon;
					break;
				}
				case 2:
				{
					Geometry icon = (Icon = Utils.GetGeometry("Ic.Schedule.Outlook"));
					Icon = icon;
					break;
				}
				case 3:
				{
					Geometry icon = (Icon = Utils.GetGeometry("Ic.Schedule.Exchange"));
					Icon = icon;
					break;
				}
				case 4:
				{
					Geometry icon = (Icon = Utils.GetGeometry("Ic.Schedule.ICloud"));
					Icon = icon;
					break;
				}
				default:
				{
					Geometry icon = (Icon = Utils.GetGeometry("Ic.Schedule.Subscribe"));
					Icon = icon;
					break;
				}
				}
				break;
			case DisplayType.Pomo:
				Icon = Utils.GetGeometry((SourceViewModel.PomoType == 0) ? "Ic.Schedule.Pomo" : "Ic.Schedule.Stopwatch");
				break;
			case DisplayType.Course:
				Icon = Utils.GetGeometry("Ic.Schedule.Timetable");
				break;
			}
		}
		OnPropertyChanged("Icon");
		SetIconColor();
	}

	public void SetIconColor()
	{
		ResourceDictionary dict = (_inDark ? ThemeUtil.DarkResource : ThemeUtil.LightResource);
		if (!string.IsNullOrEmpty(EmptyText))
		{
			IconColor = ThemeUtil.GetColor("BaseColorOpacity8", dict);
		}
		else
		{
			switch (SourceViewModel.Type)
			{
			case DisplayType.Task:
			case DisplayType.CheckItem:
			case DisplayType.Agenda:
				if (Status != 0)
				{
					IconColor = ThemeUtil.GetColor("BaseColorOpacity20", dict);
				}
				else
				{
					IconColor = Utils.GetPriorityColor(Priority, dict);
				}
				break;
			case DisplayType.Event:
			case DisplayType.Note:
			case DisplayType.Pomo:
			case DisplayType.Course:
				IconColor = ThemeUtil.GetColor("BaseColorOpacity20", dict);
				break;
			case DisplayType.Habit:
				if (Status == 2 && !string.IsNullOrEmpty(SourceViewModel.Color) && SourceViewModel.Color.ToLower() != "transparent")
				{
					IconColor = ThemeUtil.GetColorInString(SourceViewModel.Color);
					return;
				}
				IconColor = ((Status == 0) ? null : ThemeUtil.GetColor((Status == 2) ? "HabitCheckBlueColor" : "HabitRedColor", dict));
				break;
			}
		}
		OnPropertyChanged("IconColor");
	}

	public void SetTitleDate()
	{
		ShowTitleDate = true;
		TopLineVisible = Visibility.Hidden;
		SetRestWork();
	}

	public void SetColor()
	{
		if (IsEmpty)
		{
			return;
		}
		if (IsVirtual)
		{
			if (ItemColor != SourceViewModel.Color)
			{
				ItemColor = SourceViewModel.Color;
			}
		}
		else if (SourceViewModel.IsHabit || SourceViewModel.IsEvent || SourceViewModel.IsCourse || SourceViewModel.IsCountdown)
		{
			CalTaskColorMode taskColorMode = CalendarUtils.GetTaskColorMode();
			string text = null;
			text = ((taskColorMode != CalTaskColorMode.List || string.IsNullOrEmpty(SourceViewModel.Color) || !(SourceViewModel.Color.ToLower() != "transparent")) ? ThemeUtil.GetColorString("ColorPrimary", _inDark) : SourceViewModel.Color);
			if (SourceViewModel.IsHabit)
			{
				CalendarColorConfigItem calendarColorConfigItem = LocalSettings.Settings?.UserPreference?.CalendarColorConfig?.Habit;
				if (calendarColorConfigItem != null && Enum.TryParse<CalHabitColorMode>(calendarColorConfigItem.ColorType, ignoreCase: true, out var result))
				{
					switch (result)
					{
					case CalHabitColorMode.Shared:
						if (!string.IsNullOrEmpty(calendarColorConfigItem.SharedColor))
						{
							text = calendarColorConfigItem.SharedColor;
						}
						break;
					case CalHabitColorMode.Custom:
						if (calendarColorConfigItem?.ColorMap != null && calendarColorConfigItem.ColorMap.Any())
						{
							HabitModel habitModel = (SourceViewModel as HabitBaseViewModel)?.Habit;
							if (habitModel != null && calendarColorConfigItem.ColorMap.TryGetValue(habitModel.Id, out var value) && !string.IsNullOrEmpty(value))
							{
								text = value;
							}
						}
						break;
					}
				}
			}
			else if (SourceViewModel.IsCountdown)
			{
				CalendarColorConfigItem calendarColorConfigItem2 = LocalSettings.Settings?.UserPreference?.CalendarColorConfig?.Countdown;
				if (calendarColorConfigItem2 != null && Enum.TryParse<CalCountdownColorMode>(calendarColorConfigItem2.ColorType, ignoreCase: true, out var result2))
				{
					switch (result2)
					{
					case CalCountdownColorMode.Shared:
						if (!string.IsNullOrEmpty(calendarColorConfigItem2.SharedColor))
						{
							text = calendarColorConfigItem2.SharedColor;
						}
						break;
					case CalCountdownColorMode.Custom:
						if (calendarColorConfigItem2?.ColorMap != null && calendarColorConfigItem2.ColorMap.Any())
						{
							CountdownModel countdownModel = (SourceViewModel as CountDownBaseViewModel)?.Countdown;
							if (countdownModel != null && calendarColorConfigItem2.ColorMap.TryGetValue(countdownModel.Id, out var value2) && !string.IsNullOrEmpty(value2))
							{
								text = value2;
							}
						}
						break;
					}
				}
			}
			if (ItemColor != text)
			{
				ItemColor = text;
			}
		}
		else
		{
			string text2 = CalendarDisplayViewModel.TryGetColor(SourceViewModel.Color, SourceViewModel.Tag, Priority);
			if (text2 == ThemeUtil.GetColor("BaseColorOpacity40").Color.ToString())
			{
				text2 = (_inDark ? "#66FFFFFF" : "#66191919");
			}
			string text3 = text2;
			if (string.IsNullOrEmpty(text2) || text2.ToLower() == "transparent")
			{
				text3 = ThemeUtil.GetColorString("ColorPrimary", _inDark);
			}
			if (ItemColor != text3)
			{
				ItemColor = text3;
			}
		}
	}

	public static CalendarAgendaItemViewModel BuildEmptyItem(DateTime date, bool showYear)
	{
		return new CalendarAgendaItemViewModel
		{
			IsEmpty = true,
			SourceViewModel = new TaskBaseViewModel(),
			Date = date,
			ShowYearText = showYear,
			TopLineVisible = Visibility.Collapsed,
			BotLineVisible = Visibility.Collapsed
		};
	}

	public static CalendarAgendaItemViewModel BuildDueItem(DateTime date, bool isDueEnd = true)
	{
		return new CalendarAgendaItemViewModel
		{
			IsEmpty = true,
			IsLoadAfter = isDueEnd,
			IsLoadBefore = !isDueEnd,
			SourceViewModel = new TaskBaseViewModel(),
			Date = date,
			TopLineVisible = Visibility.Collapsed,
			BotLineVisible = Visibility.Collapsed
		};
	}

	public CalendarAgendaItemViewModel GetFirstShowingFrontItem()
	{
		CalendarAgendaItemViewModel result = null;
		CalendarAgendaItemViewModel frontItem = FrontItem;
		while (frontItem != null && frontItem.Showing)
		{
			result = frontItem;
			frontItem = frontItem.FrontItem;
		}
		return result;
	}

	public CalendarAgendaItemViewModel GetLastShowingNextItem()
	{
		CalendarAgendaItemViewModel result = null;
		CalendarAgendaItemViewModel nextItem = NextItem;
		while (nextItem != null && nextItem.Showing)
		{
			result = nextItem;
			nextItem = nextItem.NextItem;
		}
		return result;
	}

	public CalendarAgendaItemViewModel Clone()
	{
		return MemberwiseClone() as CalendarAgendaItemViewModel;
	}

	~CalendarAgendaItemViewModel()
	{
		Parent = null;
	}

	public void SetSourceModel(TaskBaseViewModel sourceViewModel, bool b)
	{
	}

	public void RefreshHeight()
	{
		double height = _height;
		_height -= 2.0;
		Height = height;
	}

	public async Task SetShowReminder()
	{
		if (ShowReminder.HasValue)
		{
			return;
		}
		if (SourceViewModel is HabitBaseViewModel habitBaseViewModel)
		{
			ShowReminder = (habitBaseViewModel.HabitCheckIn == null || (habitBaseViewModel.HabitCheckIn.CheckStatus == 0 && habitBaseViewModel.HabitCheckIn.Value < habitBaseViewModel.HabitCheckIn.Goal)) && HabitUtils.IsReminderValid(habitBaseViewModel.Habit.Reminder);
		}
		if (SourceViewModel == null)
		{
			return;
		}
		if (SourceViewModel.IsCheckItem)
		{
			if (StartDate.HasValue && StartDate > DateTime.Now && IsAllDay.HasValue && !IsAllDay.Value)
			{
				ShowReminder = true;
			}
		}
		else if (SourceViewModel.IsEvent || SourceViewModel.IsCourse)
		{
			if (StartDate >= DateTime.Now)
			{
				ShowReminder = !string.IsNullOrEmpty(SourceViewModel.ReminderString);
			}
		}
		else
		{
			if (!SourceViewModel.IsTaskOrNote)
			{
				return;
			}
			DateTime? date = DisplayStartDate;
			if (Status != 0 || !date.HasValue || ((IsAllDay ?? true) && date < DateTime.Today) || (!(IsAllDay ?? true) && date < DateTime.Now) || SourceViewModel.Deleted != 0)
			{
				ShowReminder = false;
				return;
			}
			if (SourceViewModel.RemindTime.HasValue && SourceViewModel.RemindTime > DateTime.Now)
			{
				ShowReminder = true;
				return;
			}
			List<TaskReminderModel> list = await TaskReminderDao.GetRemindersByTaskId(Id);
			if (list != null && list.Count > 0)
			{
				ShowReminder = list.Any((TaskReminderModel r) => DateTime.Now <= date.Value - TriggerUtils.ParseTrigger(r.trigger));
			}
			else
			{
				ShowReminder = false;
			}
		}
	}

	public async Task SetIconsVisible()
	{
		await SetShowReminder();
		SetAvatar();
	}

	public async Task<ImageSource> GetAvatar()
	{
		if (Avatar == null)
		{
			await SetAvatar();
		}
		return Avatar;
	}

	public void NotifyPropertyChanged(string propertyName)
	{
		OnPropertyChanged(propertyName);
	}

	public void SetHeight(double actualHeight)
	{
		_height = actualHeight;
	}

	public CalendarAgendaItemViewModel GetLastItemInSameDay()
	{
		CalendarAgendaItemViewModel calendarAgendaItemViewModel = this;
		while (calendarAgendaItemViewModel.NextItem != null && calendarAgendaItemViewModel.NextItem.Date == Date)
		{
			calendarAgendaItemViewModel = calendarAgendaItemViewModel.NextItem;
		}
		return calendarAgendaItemViewModel;
	}

	public CalendarAgendaItemViewModel GetFirstItemInSameDay()
	{
		CalendarAgendaItemViewModel calendarAgendaItemViewModel = this;
		if (calendarAgendaItemViewModel.IsEmpty)
		{
			return calendarAgendaItemViewModel.NextItem;
		}
		while (calendarAgendaItemViewModel.FrontItem != null && !calendarAgendaItemViewModel.FrontItem.IsEmpty && calendarAgendaItemViewModel.FrontItem.Date == Date)
		{
			calendarAgendaItemViewModel = calendarAgendaItemViewModel.FrontItem;
		}
		return calendarAgendaItemViewModel;
	}
}
