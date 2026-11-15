using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Newtonsoft.Json;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Sync;
using ticktick_WPF.Views.Tag;

namespace ticktick_WPF.ViewModels;

public class TaskDetailViewModel : BaseViewModel, AgendaHelper.IAgenda
{
	private bool _isOwner;

	private bool _isNewAdd;

	private int _mode;

	private bool _checkBoxMouseOver;

	private string _tag;

	public AttachmentModel[] Attachments;

	public int Id;

	public TaskReminderModel[] Reminders;

	public BatchData BatchData;

	private NewTaskKindConvertCache _newTaskKindConvertCache;

	public string NavigateItemId;

	private int _imageMode;

	public List<string> GuideActions;

	public List<GuideProjectTaskResource> GuideResources;

	public string Label;

	public string Color => SourceViewModel.Color;

	public string CommentCount => SourceViewModel.CommentCount;

	public int Deleted => SourceViewModel.Deleted;

	public TaskBaseViewModel SourceViewModel { get; set; }

	public TaskDetailItemModel[] Items { get; set; }

	public string PendingTemplateId { get; set; }

	public List<DisplayItemModel> PendingTemplateSubtasks { get; private set; }

	public BlockingList<TaskBaseViewModel> CheckItems => SourceViewModel.CheckItems;

	public int? Progress => (!(RepeatDiff > 0.0)) ? SourceViewModel.Progress : 0;

	public int? AnnoyingAlert { get; set; }

	public bool Pinned => SourceViewModel.PinnedTime > 0;

	public bool IsAbandoned => Status == -1;

	public bool IsCompleted => Status == 2;

	public string Tag => SourceViewModel.Tag;

	public string TaskId => SourceViewModel.Id;

	public string Permission => SourceViewModel.Permission;

	public string TeamId => SourceViewModel.TeamId;

	public string ParentId => SourceViewModel.ParentId;

	public string ProjectId => SourceViewModel.ProjectId;

	public string Assignee => SourceViewModel.Assignee;

	public List<string> ExDates { get; set; }

	public string AttendId { get; set; }

	public string Title => SourceViewModel.Title;

	public int ImageMode => SourceViewModel.ImageMode;

	public string TaskContent => SourceViewModel.Content;

	public string Desc => SourceViewModel.Desc;

	public bool IsFloating => SourceViewModel.IsFloating;

	public string TimeZoneName => SourceViewModel.TimeZoneName;

	public DateTime? StartDate => SourceViewModel.StartDate;

	public DateTime? DueDate => SourceViewModel.DueDate;

	public string ColumnId => SourceViewModel.ColumnId;

	public double? RepeatDiff { get; set; }

	public DateTime? DisplayStartDate
	{
		get
		{
			if (ParseData != null)
			{
				return ParseData.StartDate;
			}
			if (RepeatDiff.HasValue)
			{
				return StartDate?.AddDays(RepeatDiff.Value);
			}
			return StartDate;
		}
	}

	public DateTime? DisplayDueDate
	{
		get
		{
			if (ParseData != null)
			{
				return ParseData.DueDate;
			}
			if (RepeatDiff.HasValue)
			{
				return DueDate?.AddDays(RepeatDiff.Value);
			}
			return DueDate;
		}
	}

	public bool? IsAllDay
	{
		get
		{
			if (ParseData != null)
			{
				return ParseData.IsAllDay;
			}
			return SourceViewModel.IsAllDay;
		}
	}

	public int Priority => SourceViewModel.Priority;

	public int Status => SourceViewModel.Status;

	public string Kind => SourceViewModel.Kind;

	public bool Enable => SourceViewModel.Editable;

	public Visibility ShowSnoozeText
	{
		get
		{
			if (!RemindTime.HasValue || !(RemindTime.Value > DateTime.Now) || !StartDate.HasValue || !(RemindTime.Value != StartDate.Value))
			{
				return Visibility.Collapsed;
			}
			return Visibility.Visible;
		}
	}

	public Visibility ShowTimeZoneText
	{
		get
		{
			if (!LocalSettings.Settings.EnableTimeZone || IsFloating || !(TimeZoneName != TimeZoneData.LocalTimeZoneModel?.TimeZoneName) || !DisplayStartDate.HasValue || (IsAllDay.HasValue && IsAllDay.Value))
			{
				return Visibility.Collapsed;
			}
			return Visibility.Visible;
		}
	}

	public Visibility ProgressVisibility
	{
		get
		{
			if (Status != 0 || IsNewAdd || !IsOwner || !(Kind != "NOTE"))
			{
				return Visibility.Collapsed;
			}
			return Visibility.Visible;
		}
	}

	public SolidColorBrush CheckIconColor
	{
		get
		{
			if (Status == 0)
			{
				switch (Priority)
				{
				case 1:
					return ThemeUtil.GetColor("PriorityLowColor");
				case 3:
					return ThemeUtil.GetColor("PriorityMiddleColor");
				case 5:
					return ThemeUtil.GetColor("PriorityHighColor");
				}
			}
			return ThemeUtil.GetColor("BaseColorOpacity100", Element);
		}
	}

	public double CheckIconOpacity
	{
		get
		{
			if (Status == 0)
			{
				if (Priority != 0)
				{
					return 1.0;
				}
				return 0.4;
			}
			if (!_checkBoxMouseOver)
			{
				return 0.2;
			}
			return 0.4;
		}
	}

	public double CheckIconBackOpacity
	{
		get
		{
			if (Status != 0 || !_checkBoxMouseOver)
			{
				return 0.0;
			}
			return 0.15;
		}
	}

	public Geometry DateIcon => Utils.GetGeometry("Ic.TaskOM.DateRemind");

	public double IconWidth { get; set; } = 14.0;

	public Geometry Icon
	{
		get
		{
			if (Status == -1)
			{
				return Utils.GetGeometry("Ic.TaskType.Abandon");
			}
			if (Status != 0)
			{
				return Utils.GetGeometry("Ic.TaskType.Completed");
			}
			if (string.IsNullOrEmpty(AttendId))
			{
				return Utils.GetGeometry((Kind == "CHECKLIST") ? "Ic.TaskType.CheckitemTitle" : "Ic.TaskType.Uncompleted");
			}
			return Utils.GetGeometry("Ic.TaskType.Schedule");
		}
	}

	public Visibility ShowCheckIcon
	{
		get
		{
			if (IsNewAdd || !(Kind != "NOTE"))
			{
				return Visibility.Collapsed;
			}
			return Visibility.Visible;
		}
	}

	public bool IsNote => Kind == "NOTE";

	public SolidColorBrush DateIconColor
	{
		get
		{
			if (DisplayStartDate.HasValue)
			{
				if (!DateUtils.IsOutDated(DisplayStartDate, DisplayDueDate, IsAllDay))
				{
					return ThemeUtil.GetColor("DateColorPrimary", Element);
				}
				return ThemeUtil.GetColor("OutDateColor");
			}
			return ThemeUtil.GetColor((BatchData != null) ? "BaseColorOpacity80" : "BaseColorOpacity40", Element);
		}
	}

	public string TimeZoneText
	{
		get
		{
			string text = "";
			DateTime? date = DisplayStartDate;
			DateTime? dateTime = DisplayDueDate;
			string timeZoneName = TimeZoneName;
			if (IsAllDay != true && timeZoneName != TimeZoneData.LocalTimeZoneModel?.TimeZoneName && timeZoneName != null)
			{
				if (!string.IsNullOrEmpty(timeZoneName))
				{
					TimeZoneInfo timeZoneInfo = TimeZoneUtils.GetTimeZoneInfo(timeZoneName);
					date = TimeZoneUtils.LocalToTargetZoneTime(date, timeZoneName);
					dateTime = TimeZoneUtils.LocalToTargetZoneTime(dateTime, timeZoneName);
					text = TimeZoneUtils.GetTimeZoneDisplayName(timeZoneInfo);
				}
				if (date.HasValue)
				{
					return DateUtils.FormatDateString(date.Value, dateTime, IsAllDay, useSmartDate: false) + " " + text;
				}
			}
			return text;
		}
	}

	public string DateText
	{
		get
		{
			if (DisplayStartDate.HasValue)
			{
				return DateUtils.FormatDateString(DisplayStartDate.Value, DisplayDueDate, IsAllDay, useSmartDate: true, Status == 0);
			}
			if (Status != 0)
			{
				return Utils.GetString("DateNotSet");
			}
			if (!(Kind == "NOTE"))
			{
				return Utils.GetString("DateAndReminder");
			}
			return Utils.GetString("SetReminder");
		}
	}

	public string RepeatText
	{
		get
		{
			if (RepeatUtils.GetRepeatType(RepeatFrom, RepeatFlag) == RepeatFromType.Custom)
			{
				return Utils.GetString("RepeatByCustom");
			}
			if (string.IsNullOrEmpty(RepeatFlag) || RepeatFlag == "RRULE:FREQ=NONE")
			{
				return string.Empty;
			}
			if (!RepeatFlag.Contains("FREQ=DAILY") || !RepeatFlag.Contains("TT_SKIP=HOLIDAY,WEEKEND") || (RepeatFlag.Contains("INTERVAL") && !RepeatFlag.Contains("INTERVAL=1")))
			{
				return RRuleUtils.RRule2String(RepeatFrom, RepeatFlag, StartDate);
			}
			return Utils.GetString("OfficialWorkingDays");
		}
	}

	public Visibility ShowRepeatIcon
	{
		get
		{
			if (Status != 0)
			{
				return Visibility.Collapsed;
			}
			if (Utils.IsEmptyRepeatFlag(RepeatFlag))
			{
				return Visibility.Collapsed;
			}
			return Visibility.Visible;
		}
	}

	public DateTime? RemindTime
	{
		get
		{
			if (!(RepeatDiff > 0.0))
			{
				return SourceViewModel.RemindTime;
			}
			return null;
		}
	}

	public string RemindTimeText
	{
		get
		{
			DateTime valueOrDefault = RemindTime.GetValueOrDefault();
			if (valueOrDefault > DateTime.Now)
			{
				if (Math.Abs((DateTime.Now.Date - valueOrDefault.Date).TotalDays - -1.0) <= 0.001)
				{
					return string.Format(Utils.GetString("PreviewSnoozeText"), Utils.GetString("Tomorrow"));
				}
				if (Math.Abs((DateTime.Now.Date - valueOrDefault.Date).TotalDays) <= 0.001)
				{
					return string.Format(Utils.GetString("PreviewSnoozeText"), DateUtils.FormatHourMinute(valueOrDefault));
				}
				return string.Format(Utils.GetString("PreviewSnoozeText"), DateUtils.FormatYearMonthDay(valueOrDefault, DateFormatLen.Middle) + " " + DateUtils.FormatHourMinute(valueOrDefault));
			}
			return string.Empty;
		}
	}

	public string RepeatFlag
	{
		get
		{
			if (ParseData != null)
			{
				return ParseData.RepeatFlag;
			}
			return SourceViewModel.RepeatFlag;
		}
	}

	public string RepeatFrom => SourceViewModel.RepeatFrom;

	public string ProjectName
	{
		get
		{
			string emojiIcon = EmojiHelper.GetEmojiIcon(SourceViewModel.ProjectName);
			if (!string.IsNullOrEmpty(emojiIcon) && SourceViewModel.ProjectName.StartsWith(emojiIcon))
			{
				return SourceViewModel.ProjectName.Substring(emojiIcon.Length);
			}
			return SourceViewModel.ProjectName;
		}
	}

	public string Emoji
	{
		get
		{
			string emojiIcon = EmojiHelper.GetEmojiIcon(SourceViewModel.ProjectName);
			if (!string.IsNullOrEmpty(emojiIcon) && SourceViewModel.ProjectName.StartsWith(emojiIcon))
			{
				return emojiIcon;
			}
			return null;
		}
	}

	public bool IsNewAdd
	{
		get
		{
			return _isNewAdd;
		}
		set
		{
			OnPropertyChanged("IsNewAdd");
			OnPropertyChanged("ShowCheckIcon");
			OnPropertyChanged("ProgressVisibility");
			_isNewAdd = value;
		}
	}

	public int Mode
	{
		get
		{
			return _mode;
		}
		set
		{
			_mode = value;
			OnPropertyChanged("Mode");
		}
	}

	public bool ShowSetTime => !DisplayStartDate.HasValue;

	public string DayText => DisplayStartDate?.Day.ToString() ?? string.Empty;

	public bool IsOwner
	{
		get
		{
			return _isOwner;
		}
		set
		{
			_isOwner = value;
			OnPropertyChanged("IsOwner");
			OnPropertyChanged("ProgressVisibility");
		}
	}

	public bool CheckBoxMouseOver
	{
		get
		{
			return _checkBoxMouseOver;
		}
		set
		{
			_checkBoxMouseOver = value;
			OnPropertyChanged("CheckBoxMouseOver");
			OnPropertyChanged("CheckIconOpacity");
			OnPropertyChanged("CheckIconBackOpacity");
		}
	}

	public DateTime? CreateDate => SourceViewModel.CreatedTime;

	public DateTime? ModifiedDate => SourceViewModel.ModifiedTime;

	public TimeData ParseData { get; set; }

	public FrameworkElement Element { get; set; }

	public bool InCal { get; set; }

	public DrawingImage PriorityImage => Priority switch
	{
		0 => Utils.GetImageSource("NonePriorityThinDrawingImage", Element), 
		1 => Utils.GetImageSource("LowPriorityDrawingImage"), 
		3 => Utils.GetImageSource("MidPriorityDrawingImage"), 
		5 => Utils.GetImageSource("HighPriorityDrawingImage"), 
		_ => null, 
	};

	public string NotionBlock => SourceViewModel.NotionBlock;

	public BindCalendarModel KindConvertCalendar => _newTaskKindConvertCache?.Calendar;

	public string KindConvertLocation => _newTaskKindConvertCache?.Location;

	public TimeData KindConvertTimeData => _newTaskKindConvertCache?.CalendarTimeData;

	public TaskDetailViewModel(TaskBaseViewModel task)
	{
		SourceViewModel = task;
		AddVmPropertyChangedEvent();
		if (!string.IsNullOrEmpty(task.Actions))
		{
			GuideActions = (from a in JsonConvert.DeserializeObject<List<GuideProjectTaskAction>>(task.Actions)
				select a.url).ToList();
		}
		Label = task.Label;
		if (!string.IsNullOrEmpty(task.Resources))
		{
			GuideResources = JsonConvert.DeserializeObject<List<GuideProjectTaskResource>>(task.Resources).ToList();
		}
	}

	public TaskDetailViewModel(TaskModel task)
	{
		SourceViewModel = TaskCache.SafeGetTaskViewModel(task);
		AddVmPropertyChangedEvent();
		_imageMode = task.imgMode;
		Reminders = task.reminders;
		AnnoyingAlert = task.annoyingAlert;
		Attachments = task.Attachments;
		ExDates = ((task.exDate != null) ? task.exDate.ToList() : new List<string>());
		if (!string.IsNullOrEmpty(task.Actions))
		{
			GuideActions = (from a in JsonConvert.DeserializeObject<List<GuideProjectTaskAction>>(task.Actions)
				select a.url).ToList();
		}
		Label = task.label;
		if (!string.IsNullOrEmpty(task.Resources))
		{
			GuideResources = JsonConvert.DeserializeObject<List<GuideProjectTaskResource>>(task.Resources).ToList();
		}
	}

	public string GetTaskId()
	{
		return TaskId;
	}

	public string GetAttendId()
	{
		return AttendId;
	}

	public void SetTimeData(TimeData data)
	{
		if (!TaskCache.ExistTask(TaskId))
		{
			SourceViewModel.StartDate = data.StartDate;
			SourceViewModel.DueDate = data.DueDate;
			SourceViewModel.IsAllDay = data.IsAllDay;
			SourceViewModel.RepeatFlag = data.RepeatFlag;
			SourceViewModel.RepeatFrom = data.RepeatFrom;
			SourceViewModel.ExDates = ExDateSerilizer.ToString(data.ExDates?.ToArray());
			SourceViewModel.IsFloating = data.TimeZone?.IsFloat ?? false;
			SourceViewModel.TimeZoneName = data.TimeZone?.TimeZoneName;
		}
		Reminders = data.Reminders?.ToArray();
		AnnoyingAlert = data.AnnoyingAlert;
	}

	public void SetParseData(TimeData data)
	{
		if (ParseData != null || data != null)
		{
			ParseData = data;
			OnPropertyChanged("DayText");
			OnPropertyChanged("DateText");
			OnPropertyChanged("DateIcon");
			OnPropertyChanged("DateIconColor");
			OnPropertyChanged("ShowSetTime");
			OnPropertyChanged("RepeatText");
			OnPropertyChanged("ShowRepeatIcon");
		}
	}

	public void SetPendingTemplate(TaskTemplateModel template)
	{
		PendingTemplateId = template?.Id;
		PendingTemplateSubtasks = BuildPendingTemplateSubtasks(template);
		OnPropertyChanged("PendingTemplateSubtasks");
	}

	public void ClearPendingTemplate()
	{
		PendingTemplateId = null;
		PendingTemplateSubtasks = null;
		OnPropertyChanged("PendingTemplateSubtasks");
	}

	private List<DisplayItemModel> BuildPendingTemplateSubtasks(TaskTemplateModel template)
	{
		if (template?.Chidlren == null || template.Chidlren.Count == 0)
		{
			return null;
		}
		List<DisplayItemModel> list = new List<DisplayItemModel>();
		for (int i = 0; i < template.Chidlren.Count; i++)
		{
			AddPendingTemplateSubtask(list, template.Chidlren[i], 0, i, TaskId);
		}
		return list;
	}

	private void AddPendingTemplateSubtask(List<DisplayItemModel> target, TaskTemplateModel template, int level, int index, string parentId)
	{
		DisplayItemModel displayItemModel = BuildPendingTemplateSubtask(template, level, index, parentId);
		target.Add(displayItemModel);
		List<TaskTemplateModel> chidlren = template.Chidlren;
		if (chidlren != null && chidlren.Count > 0)
		{
			for (int i = 0; i < template.Chidlren.Count; i++)
			{
				AddPendingTemplateSubtask(target, template.Chidlren[i], level + 1, i, displayItemModel.Id);
			}
		}
	}

	private DisplayItemModel BuildPendingTemplateSubtask(TaskTemplateModel template, int level, int index, string parentId)
	{
		bool flag = template.Items != null && template.Items.Any();
		DisplayItemModel displayItemModel = new DisplayItemModel(new TaskBaseViewModel
		{
			Id = Utils.GetGuid(),
			ParentId = parentId,
			ProjectId = ProjectId,
			ColumnId = ColumnId,
			Title = (template.Title ?? string.Empty),
			Content = template.Content,
			Desc = template.Desc,
			Kind = (flag ? "CHECKLIST" : "TEXT"),
			Type = DisplayType.Task,
			Status = 0,
			Priority = 0,
			SortOrder = (long)index * 268435456L,
			IsAllDay = true,
			CreatedTime = DateTime.Now,
			ModifiedTime = DateTime.Now,
			Editable = false,
			Tags = template.Tags?.ToArray(),
			Tag = TagSerializer.ToJsonContent(template.Tags)
		});
		displayItemModel.Level = level;
		displayItemModel.InDetail = true;
		displayItemModel.ShowDragBar = false;
		displayItemModel.HitVisible = false;
		displayItemModel.SetForceReadOnly(readOnly: true);
		return displayItemModel;
	}

	public static TaskDetailViewModel BuildInitModel(TaskBaseViewModel baseModel, bool isAllDay = true)
	{
		baseModel = baseModel ?? new TaskBaseViewModel
		{
			Id = Utils.GetGuid(),
			Title = string.Empty,
			ProjectId = Utils.GetInboxId(),
			Priority = TaskDefaultDao.GetDefaultSafely().Priority,
			Tag = TaskDefaultDao.GetDefaultSafely().TagString,
			Tags = TaskDefaultDao.GetDefaultSafely().Tags?.ToArray(),
			Kind = "TEXT",
			IsAllDay = isAllDay
		};
		return new TaskDetailViewModel(baseModel)
		{
			IsNewAdd = true,
			Reminders = ((!(baseModel.IsAllDay ?? true)) ? TimeData.GetDefaultTimeReminders().ToArray() : TimeData.GetDefaultAllDayReminders().ToArray())
		};
	}

	public static async Task<TaskDetailViewModel> Build(string taskId)
	{
		TaskModel task = await TaskDao.GetThinTaskById(taskId);
		if (task == null)
		{
			return null;
		}
		task.content = Regex.Replace(task.content ?? string.Empty, "(?<!\r)\n", "\r\n");
		await TaskDao.AssembleFullTask(task);
		return new TaskDetailViewModel(task);
	}

	public static async Task<TaskModel> ToTaskModel(TaskDetailViewModel viewModel)
	{
		TaskModel taskModel = (string.IsNullOrEmpty(viewModel?.TaskId) ? new TaskModel() : ((await TaskDao.GetThinTaskById(viewModel.TaskId)) ?? new TaskModel
		{
			id = viewModel.TaskId
		}));
		if (viewModel != null)
		{
			taskModel.title = viewModel.Title;
			taskModel.content = viewModel.TaskContent;
			taskModel.desc = viewModel.Desc;
			taskModel.content = viewModel.TaskContent;
			taskModel.startDate = viewModel.StartDate;
			taskModel.dueDate = viewModel.DueDate;
			taskModel.remindTime = viewModel.RemindTime;
			taskModel.isAllDay = viewModel.IsAllDay;
			taskModel.status = viewModel.Status;
			taskModel.kind = viewModel.Kind;
			taskModel.priority = viewModel.Priority;
			taskModel.repeatFlag = viewModel.RepeatFlag;
			taskModel.repeatFrom = viewModel.RepeatFrom;
			taskModel.projectId = viewModel.ProjectId;
			taskModel.assignee = viewModel.Assignee;
			taskModel.progress = viewModel.Progress;
			taskModel.deleted = viewModel.Deleted;
			taskModel.commentCount = viewModel.CommentCount;
			taskModel.reminders = viewModel.Reminders;
			taskModel.Attachments = viewModel.Attachments;
			taskModel.Color = viewModel.Color;
			taskModel.tag = viewModel.Tag;
			taskModel.exDates = ((viewModel.ExDates != null) ? ExDateSerilizer.ToString(viewModel.ExDates.ToArray()) : null);
			taskModel.attendId = viewModel.AttendId;
			taskModel.isFloating = viewModel.IsFloating;
			taskModel.timeZone = viewModel.TimeZoneName;
			taskModel.imgMode = viewModel.ImageMode;
			taskModel.columnId = viewModel.ColumnId;
			taskModel.sortOrder = viewModel.SourceViewModel?.SortOrder ?? 0;
			taskModel.annoyingAlert = viewModel.AnnoyingAlert;
		}
		return taskModel;
	}

	private void AddVmPropertyChangedEvent()
	{
		if (SourceViewModel != null)
		{
			PropertyChangedEventManager.AddHandler(SourceViewModel, OnTaskPropertyChanged, string.Empty);
		}
	}

	private void OnTaskPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		string propertyName = e.PropertyName;
		if (propertyName == null)
		{
			return;
		}
		switch (propertyName.Length)
		{
		case 7:
			switch (propertyName[0])
			{
			case 'C':
				if (propertyName == "Content")
				{
					OnPropertyChanged("TaskContent");
				}
				break;
			case 'D':
				if (propertyName == "DueDate")
				{
					OnPropertyChanged("DisplayDueDate");
					OnPropertyChanged("DateText");
					OnPropertyChanged("TimeZoneText");
				}
				break;
			}
			break;
		case 4:
			switch (propertyName[0])
			{
			case 'D':
				if (propertyName == "Desc")
				{
					OnPropertyChanged("Desc");
				}
				break;
			case 'T':
				if (propertyName == "Type")
				{
					OnPropertyChanged("Type");
				}
				break;
			case 'K':
				if (propertyName == "Kind")
				{
					OnPropertyChanged("Kind");
					OnPropertyChanged("IsNote");
					OnPropertyChanged("DateText");
					OnPropertyChanged("ShowCheckIcon");
					OnPropertyChanged("Icon");
					OnPropertyChanged("ProgressVisibility");
				}
				break;
			}
			break;
		case 9:
			switch (propertyName[0])
			{
			case 'S':
				if (propertyName == "StartDate")
				{
					OnPropertyChanged("DisplayStartDate");
					OnPropertyChanged("DayText");
					OnPropertyChanged("DateText");
					OnPropertyChanged("DateIcon");
					OnPropertyChanged("DateIconColor");
					OnPropertyChanged("ShowSetTime");
					OnPropertyChanged("TimeZoneText");
					OnPropertyChanged("RepeatText");
				}
				break;
			case 'P':
				if (propertyName == "ProjectId")
				{
					OnPropertyChanged("ProjectId");
				}
				break;
			case 'I':
				if (propertyName == "ImageMode")
				{
					OnPropertyChanged("ImageMode");
				}
				break;
			}
			break;
		case 8:
			switch (propertyName[3])
			{
			case 'l':
				if (propertyName == "IsAllDay")
				{
					OnPropertyChanged("IsAllDay");
					OnPropertyChanged("DateText");
					OnPropertyChanged("TimeZoneText");
				}
				break;
			case 'g':
				if (propertyName == "Progress")
				{
					OnPropertyChanged("Progress");
				}
				break;
			case 'o':
				if (propertyName == "Priority")
				{
					OnPropertyChanged("Priority");
					OnPropertyChanged("PriorityImage");
					OnPropertyChanged("CheckIconColor");
					OnPropertyChanged("CheckIconOpacity");
				}
				break;
			case 'm':
				if (propertyName == "Comments")
				{
					OnPropertyChanged("Comments");
				}
				break;
			case 't':
				if (propertyName == "Editable")
				{
					OnPropertyChanged("Enable");
				}
				break;
			case 'i':
				if (propertyName == "Assignee")
				{
					OnPropertyChanged("Assignee");
				}
				break;
			case 'u':
				if (propertyName == "ColumnId")
				{
					OnPropertyChanged("ColumnId");
				}
				break;
			}
			break;
		case 10:
			switch (propertyName[0])
			{
			case 'R':
				if (propertyName == "RepeatFlag")
				{
					OnPropertyChanged("ShowRepeatIcon");
					OnPropertyChanged("RepeatText");
				}
				break;
			case 'C':
				if (propertyName == "CheckItems")
				{
					OnPropertyChanged("CheckItems");
				}
				break;
			case 'I':
				if (propertyName == "IsFloating")
				{
					OnPropertyChanged("IsFloating");
				}
				break;
			}
			break;
		case 11:
			switch (propertyName[0])
			{
			case 'P':
				if (propertyName == "ProjectName")
				{
					OnPropertyChanged("ProjectName");
					OnPropertyChanged("Emoji");
				}
				break;
			case 'C':
				if (propertyName == "CreatedTime")
				{
					OnPropertyChanged("CreateDate");
				}
				break;
			}
			break;
		case 12:
			switch (propertyName[0])
			{
			case 'T':
				if (propertyName == "TimeZoneName")
				{
					OnPropertyChanged("TimeZoneName");
					OnPropertyChanged("ShowTimeZoneText");
					OnPropertyChanged("TimeZoneText");
				}
				break;
			case 'M':
				if (propertyName == "ModifiedTime")
				{
					OnPropertyChanged("ModifiedDate");
				}
				break;
			}
			break;
		case 5:
			if (propertyName == "Title")
			{
				OnPropertyChanged("Title");
			}
			break;
		case 6:
			if (propertyName == "Status")
			{
				OnPropertyChanged("Status");
				OnPropertyChanged("IsAbandoned");
				OnPropertyChanged("IsCompleted");
				OnPropertyChanged("CheckIconColor");
				OnPropertyChanged("Icon");
				OnPropertyChanged("CheckIconOpacity");
				OnPropertyChanged("CheckIconBackOpacity");
				OnPropertyChanged("ShowRepeatIcon");
				OnPropertyChanged("ProgressVisibility");
				OnPropertyChanged("DateText");
			}
			break;
		case 3:
			if (propertyName == "Tag")
			{
				OnPropertyChanged("Tag");
			}
			break;
		}
	}

	public void CheckBoxMouseEnterCommand()
	{
		CheckBoxMouseOver = true;
	}

	public void CheckBoxMouseLeaveCommand()
	{
		CheckBoxMouseOver = false;
	}

	public void OnThemeChanged()
	{
		OnPropertyChanged("CheckIconColor");
		OnPropertyChanged("DateIconColor");
		OnPropertyChanged("Priority");
	}

	public void SetPropertyChanged(string property)
	{
		OnPropertyChanged(property);
	}

	public void CacheTaskFieldsBeforeKindConvert(bool keepExisting = false)
	{
		if (keepExisting && _newTaskKindConvertCache != null)
		{
			_newTaskKindConvertCache.UpdateAttachments(TaskContent, Attachments, TaskId);
			return;
		}
		NewTaskKindConvertCache newTaskKindConvertCache = _newTaskKindConvertCache;
		_newTaskKindConvertCache = NewTaskKindConvertCache.From(this);
		_newTaskKindConvertCache.KeepCalendarFieldsFrom(newTaskKindConvertCache);
	}

	public void CopyKindConvertCacheFrom(TaskDetailViewModel other)
	{
		_newTaskKindConvertCache = other?._newTaskKindConvertCache?.Copy();
	}

	public void CacheCalendarFieldsBeforeKindConvert(string calendarId, string location, TimeData calendarTimeData = null)
	{
		if (_newTaskKindConvertCache == null)
		{
			_newTaskKindConvertCache = NewTaskKindConvertCache.From(this);
		}
		_newTaskKindConvertCache.SetCalendarFields(calendarId, location, calendarTimeData);
	}

	public void RestoreTaskFieldsAfterKindConvert()
	{
		_newTaskKindConvertCache?.ApplyTo(this);
	}

	public void RestoreAttachmentsAfterKindConvert()
	{
		_newTaskKindConvertCache?.ApplyAttachmentsTo(this);
	}

	public string RestoreAttachmentMarkdownAfterKindConvert(string content)
	{
		return _newTaskKindConvertCache?.RestoreAttachmentMarkdown(content) ?? content;
	}

	public void CacheAttachmentContentForCalendar(string originalContent, string calendarContent)
	{
		_newTaskKindConvertCache?.CacheAttachmentContentForCalendar(originalContent, calendarContent);
	}

	public void CacheRepeatHiddenForNote(TimeData timeData)
	{
		_newTaskKindConvertCache?.CacheRepeatHiddenForNote(timeData);
	}

	public void RestoreRepeatForCalendar(TimeData timeData)
	{
		_newTaskKindConvertCache?.ApplyStandardRepeatTo(timeData);
	}

	public void ClearRepeatForNote()
	{
		if (SourceViewModel != null)
		{
			CacheRepeatHiddenForNote(GetTimeData());
			SourceViewModel.RepeatFlag = null;
			SourceViewModel.RepeatFrom = null;
			ExDates = new List<string>();
			if (ParseData != null)
			{
				ParseData.RepeatFlag = null;
				ParseData.RepeatFrom = null;
				ParseData.ExDates = new List<string>();
			}
			OnPropertyChanged("RepeatText");
			OnPropertyChanged("ShowRepeatIcon");
		}
	}

	public TimeData MergeCalendarTimeForEvent(TimeData timeData)
	{
		return _newTaskKindConvertCache?.MergeCalendarTimeForEvent(timeData) ?? timeData;
	}

	public TimeData GetTimeData()
	{
		TimeData timeData = new TimeData
		{
			StartDate = DisplayStartDate,
			DueDate = DisplayDueDate,
			IsAllDay = (!DisplayStartDate.HasValue || (IsAllDay ?? true)),
			Reminders = Reminders?.ToList(),
			AnnoyingAlert = AnnoyingAlert,
			RepeatFrom = RepeatFrom,
			RepeatFlag = RepeatFlag,
			ExDates = ExDates
		};
		if (!string.IsNullOrEmpty(TimeZoneName))
		{
			timeData.TimeZone = new TimeZoneViewModel(IsFloating, TimeZoneName);
		}
		if (timeData.IsAllDay.Value && timeData.StartDate.HasValue && timeData.DueDate.HasValue && timeData.StartDate.Value.Date == timeData.DueDate.Value.Date)
		{
			timeData.DueDate = null;
		}
		if (timeData.StartDate.HasValue)
		{
			timeData.IsDefault = false;
		}
		return timeData;
	}
}
