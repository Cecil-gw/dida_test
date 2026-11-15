using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using TickTickUtils.Lang;
using ticktick_WPF.Cache;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Files;
using ticktick_WPF.Util.Provider;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.QuickAdd;

public class AddTaskViewModel : BaseViewModel
{
	private string _calendarId;

	private string _dayText = string.Empty;

	private string _detailDayText = string.Empty;

	private string _hint;

	private string _kind = "TEXT";

	private List<string> _originalTag = new List<string>();

	private int _priority;

	private string _projectId;

	private string _projectName;

	private string _assignee;

	private bool _selectProject;

	private bool _isNote;

	private bool _isCalendar;

	private DateTime? _startDate;

	private List<string> _tags = new List<string>();

	private TimeData _timeData;

	private static Section _section;

	private bool _useDetailStyle;

	private bool _taskKindSelected;

	private bool _allowUseDetailMode = true;

	private string _titleCountText;

	private AddTaskKindConvertCache _kindConvertCache;

	private QuickAddView.Scenario _scenario;

	private Thickness _borderThickness = new Thickness(1.0);

	private string _assigneeId;

	private bool _saveEnabled;

	private string _saveHint;

	private string _saveTooltip;

	private bool _useInKanban;

	private bool _showSaveHint;

	private string _contentText;

	private string _titleText;

	private int _displayTagCount;

	public ObservableCollection<AddTaskAttachmentInfo> Files { get; } = new ObservableCollection<AddTaskAttachmentInfo>();

	public bool ManuallySelectedPriority { get; set; }

	public List<string> OriginalTags
	{
		get
		{
			return _originalTag.ToList();
		}
		set
		{
			_originalTag = value;
		}
	}

	public QuickAddView.Scenario OriginalScenario { get; set; }

	public string OriginalProjectId { get; set; }

	private int OriginalPriority { get; set; }

	public TimeData OriginalTimeData { get; set; }

	private string OriginalProjectName { get; set; }

	public string OriginalAddToColumnId { get; set; }

	public string ColumnId { get; set; }

	public string AddToColumnId { get; set; }

	public string CalendarName { get; set; }

	public string OriginalCalendarName { get; set; }

	public bool IsOriginalNote { get; set; }

	public string OriginalCalendarId { get; set; }

	public bool IsOriginalCalendar { get; set; }

	public string AccountId { get; set; }

	public string OriginalAccountId { get; set; }

	public bool IsPin { get; set; }

	public bool IsComplete { get; set; }

	public bool TitleFocused { get; set; }

	public string TitleCountText
	{
		get
		{
			return _titleCountText;
		}
		set
		{
			_titleCountText = value;
			OnPropertyChanged("TitleCountText");
		}
	}

	public Thickness BorderThickness
	{
		get
		{
			return _borderThickness;
		}
		set
		{
			_borderThickness = value;
			OnPropertyChanged("BorderThickness");
		}
	}

	public QuickAddView.Scenario Scenario
	{
		get
		{
			return _scenario;
		}
		set
		{
			_scenario = value;
			OnPropertyChanged("Scenario");
		}
	}

	public string AssigneeId
	{
		get
		{
			return _assigneeId;
		}
		set
		{
			_assigneeId = value;
			OnPropertyChanged("AssigneeId");
		}
	}

	public bool AllowUseDetailMode
	{
		get
		{
			return _allowUseDetailMode;
		}
		set
		{
			_allowUseDetailMode = value;
			OnPropertyChanged("AllowUseDetailMode");
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
			OnPropertyChanged("CalendarId");
		}
	}

	public bool IsCalendar
	{
		get
		{
			return _isCalendar;
		}
		set
		{
			if (_isCalendar != value)
			{
				_isCalendar = value;
				OnPropertyChanged("IsCalendar");
				OnPropertyChanged("TaskKindIndicator");
				OnPropertyChanged("ShowTitleKindHintIcon");
				OnPropertyChanged("TitleFirstLineIndent");
				OnPropertyChanged("ShowProjectName");
				OnPropertyChanged("DisplayProjectName");
				OnPropertyChanged("Emoji");
				OnPropertyChanged("ProjectIcon");
				OnPropertyChanged("UseDetailStyle");
				OnPropertyChanged("ShowAssignee");
			}
		}
	}

	public bool IsNote
	{
		get
		{
			return _isNote;
		}
		set
		{
			_isNote = value;
			OnPropertyChanged("TaskKindIndicator");
			OnPropertyChanged("ShowTitleKindHintIcon");
			OnPropertyChanged("TitleFirstLineIndent");
			OnPropertyChanged("IsNote");
		}
	}

	public int Priority
	{
		get
		{
			return _priority;
		}
		set
		{
			_priority = value;
			OnPropertyChanged("Priority");
		}
	}

	public string ProjectId
	{
		get
		{
			return _projectId;
		}
		set
		{
			if (_projectId != value)
			{
				_projectId = value;
				OnPropertyChanged("ProjectId");
			}
		}
	}

	public Geometry TaskKindIndicator
	{
		get
		{
			if (!TaskKindSelected)
			{
				return null;
			}
			if (IsCalendar)
			{
				return Utils.GetGeometry("Ic.TaskType.Schedule");
			}
			return Utils.GetGeometry((!_isNote) ? "Ic.TaskType.Uncompleted" : "Ic.TaskType.Note");
		}
	}

	public bool TaskKindSelected
	{
		get
		{
			return _taskKindSelected;
		}
		set
		{
			_taskKindSelected = value;
			OnPropertyChanged("TaskKindIndicator");
			OnPropertyChanged("TaskKindSelected");
			OnPropertyChanged("ShowTitleKindHintIcon");
			OnPropertyChanged("TitleFirstLineIndent");
		}
	}

	public string Emoji
	{
		get
		{
			if (IsCalendar || !ShowProjectName || UseInKanban)
			{
				return null;
			}
			string emojiIcon = EmojiHelper.GetEmojiIcon(ProjectName);
			if (!string.IsNullOrEmpty(emojiIcon) && ProjectName.StartsWith(emojiIcon))
			{
				return emojiIcon;
			}
			return null;
		}
	}

	public bool SaveEnabled
	{
		get
		{
			return _saveEnabled;
		}
		set
		{
			_saveEnabled = value;
			OnPropertyChanged("SaveEnabled");
		}
	}

	public string SaveHint
	{
		get
		{
			return _saveHint;
		}
		set
		{
			_saveHint = value;
			OnPropertyChanged("SaveHint");
		}
	}

	public string SaveTooltip
	{
		get
		{
			return _saveTooltip;
		}
		set
		{
			_saveTooltip = value;
			OnPropertyChanged("SaveTooltip");
		}
	}

	public bool UseInKanban
	{
		get
		{
			return _useInKanban;
		}
		set
		{
			_useInKanban = value;
			OnPropertyChanged("UseInKanban");
		}
	}

	public string SaveHintKey { get; set; }

	public bool ShowSaveHint
	{
		get
		{
			return _showSaveHint;
		}
		set
		{
			_showSaveHint = value;
			OnPropertyChanged("ShowSaveHint");
		}
	}

	public string DisplayProjectName
	{
		get
		{
			if (!ShowProjectName || UseInKanban)
			{
				return string.Empty;
			}
			if (IsCalendar)
			{
				return CalendarName ?? ProjectName ?? string.Empty;
			}
			string emojiIcon = EmojiHelper.GetEmojiIcon(ProjectName);
			if (!string.IsNullOrEmpty(emojiIcon) && ProjectName.StartsWith(emojiIcon))
			{
				return ProjectName.Substring(emojiIcon.Length);
			}
			return ProjectName;
		}
	}

	public Geometry ProjectIcon
	{
		get
		{
			if (IsCalendar)
			{
				return Utils.GetGeometry("Ic.ProjectList.MoveTo");
			}
			if (UseInKanban)
			{
				return Utils.GetGeometry("Ic.ProjectList.MoveTo");
			}
			ProjectModel projectModel = CacheManager.GetProjects().FirstOrDefault((ProjectModel p) => p.id == ProjectId);
			if (projectModel == null)
			{
				return Utils.GetGeometry("Ic.ProjectList.List");
			}
			return NormalProjectIdentity.GetProjectIcon(projectModel);
		}
	}

	public string ContentText
	{
		get
		{
			return _contentText;
		}
		set
		{
			_contentText = value;
			OnPropertyChanged("ContentText");
			OnPropertyChanged("UseDetailStyle");
		}
	}

	public string TitleText
	{
		get
		{
			return _titleText;
		}
		set
		{
			_titleText = value;
			TitleCountText = ((value != null && value.Length >= 1948) ? value.Length.ToString() : string.Empty);
			OnPropertyChanged("TitleText");
			OnPropertyChanged("ShowTitleKindHintIcon");
			OnPropertyChanged("TitleFirstLineIndent");
		}
	}

	public bool ForceShowProjectName { get; set; }

	public bool ShowTitleKindHintIcon => TaskKindSelected;

	public double TitleFirstLineIndent => ShowTitleKindHintIcon ? 24 : 0;

	public bool ShowProjectName
	{
		get
		{
			if (!IsCalendar && !(ProjectId != OriginalProjectId))
			{
				return ForceShowProjectName;
			}
			return true;
		}
	}

	public bool IsNoteProject => CacheManager.GetProjects().FirstOrDefault((ProjectModel p) => p.id == ProjectId)?.IsNote ?? false;

	public bool IsShareProject => CacheManager.GetProjects().FirstOrDefault((ProjectModel p) => p.id == ProjectId)?.IsShareList() ?? false;

	public bool ShowAssignee
	{
		get
		{
			if (!IsCalendar)
			{
				return IsShareProject;
			}
			return false;
		}
	}

	public string ProjectName
	{
		get
		{
			return _projectName;
		}
		set
		{
			if (_projectId != value)
			{
				_projectName = value;
				ResetHint();
				OnPropertyChanged("ShowProjectName");
				NotifyProjectChanged();
			}
		}
	}

	public string Assignee
	{
		get
		{
			return _assignee;
		}
		set
		{
			if (_assignee != value)
			{
				_assignee = value;
				OnPropertyChanged("Assignee");
			}
		}
	}

	public bool SelectProject
	{
		get
		{
			return true;
		}
		set
		{
			_selectProject = value;
			OnPropertyChanged("SelectProject");
		}
	}

	public string DetailDayText
	{
		get
		{
			return _detailDayText;
		}
		set
		{
			_detailDayText = value;
			OnPropertyChanged("DetailDayText");
		}
	}

	public int DisplayTagCount
	{
		get
		{
			return _displayTagCount;
		}
		set
		{
			_displayTagCount = value;
			NotifyDisplayTagsChanged();
		}
	}

	public bool ShowInitTag => DisplayTagCount == 0;

	public bool ShowOneTagSelected => DisplayTagCount == 1;

	public bool ShowMultipleTagSelected => DisplayTagCount > 1;

	public Geometry DayTextIcon
	{
		get
		{
			if (!StartDate.HasValue)
			{
				return Utils.GetGeometry("Ic.TaskOM.DateRemind");
			}
			return Utils.GetGeometry("Ic.ProjectList.Date" + StartDate.Value.Day);
		}
	}

	public string DayText
	{
		get
		{
			return _dayText;
		}
		set
		{
			_dayText = value;
			OnPropertyChanged("DayTextIcon");
		}
	}

	public DateTime? StartDate
	{
		get
		{
			return _timeData?.StartDate;
		}
		set
		{
			if (_timeData != null)
			{
				_timeData.StartDate = value;
				if (!value.HasValue)
				{
					_timeData.DueDate = null;
				}
			}
			OnPropertyChanged("StartDate");
			RefreshDayText(_timeData);
		}
	}

	public TimeData TimeData
	{
		get
		{
			return _timeData;
		}
		set
		{
			_timeData = value;
			StartDate = value?.StartDate;
			OnPropertyChanged("ShowRepeat");
		}
	}

	public bool ShowRepeat => !string.IsNullOrEmpty(_timeData?.RepeatFlag);

	public string Hint
	{
		get
		{
			return _hint;
		}
		set
		{
			_hint = value;
			OnPropertyChanged("Hint");
		}
	}

	public string Kind
	{
		get
		{
			return _kind;
		}
		set
		{
			_kind = value;
			OnPropertyChanged("Kind");
		}
	}

	public List<string> Tags
	{
		get
		{
			return _tags;
		}
		set
		{
			_tags = value;
			ResetHint();
		}
	}

	public bool UseDetailStyle
	{
		get
		{
			if (_useDetailStyle || !string.IsNullOrEmpty(ContentText))
			{
				return AllowUseDetailMode;
			}
			return false;
		}
		set
		{
			_useDetailStyle = value;
			OnPropertyChanged("UseDetailStyle");
		}
	}

	public bool UseGlobalStyle => Scenario == QuickAddView.Scenario.QuickAdd;

	public double IconWidth => UseGlobalStyle ? 20 : 18;

	public double IconHeight => IconWidth;

	public IProjectTaskDefault TaskDefault { get; set; }

	private bool UseTagHint { get; set; }

	public bool AutoAddTags { get; set; }

	public string KindConvertLocation => _kindConvertCache?.Location;

	public string KindConvertProjectTokenValue => _kindConvertCache?.ProjectTokenValue;

	public string KindConvertProjectId => _kindConvertCache?.ProjectId;

	public string KindConvertColumnId => _kindConvertCache?.ColumnId;

	public string KindConvertAddToColumnId => _kindConvertCache?.AddToColumnId;

	public AddTaskViewModel()
	{
	}

	public AddTaskViewModel(IProjectTaskDefault taskDefault)
	{
		_projectId = taskDefault.GetProjectId();
		_projectName = taskDefault.GetProjectName();
		_timeData = taskDefault.GetTimeData();
		_priority = taskDefault.GetPriority();
		_tags = taskDefault.GetTags();
		ColumnId = taskDefault.GetColumnId();
		IsCalendar = taskDefault.IsCalendar();
		AccountId = taskDefault.GetAccountId();
		CalendarName = taskDefault.GetProjectName();
		_originalTag = taskDefault.GetTags().ToList();
		_isNote = taskDefault.GetIsNote();
		_assignee = taskDefault.GetAssignee();
		ForceShowProjectName = taskDefault.GetForceShowProjectName();
		TaskDefault = taskDefault;
	}

	public void NotifyProjectChanged()
	{
		OnPropertyChanged("ProjectName");
		OnPropertyChanged("DisplayProjectName");
		OnPropertyChanged("Emoji");
		OnPropertyChanged("ProjectIcon");
		OnPropertyChanged("IsShareProject");
		OnPropertyChanged("ShowAssignee");
	}

	public void NotifyDisplayTagsChanged()
	{
		OnPropertyChanged("ShowInitTag");
		OnPropertyChanged("ShowOneTagSelected");
		OnPropertyChanged("ShowMultipleTagSelected");
	}

	public static AddTaskViewModel Build(IProjectTaskDefault taskDefault, Section section = null, bool useInKanban = false, bool allowUseDetailMode = true, QuickAddView.Scenario scenario = QuickAddView.Scenario.TaskList)
	{
		if (taskDefault == null)
		{
			return new AddTaskViewModel();
		}
		AddTaskViewModel addTaskViewModel = new AddTaskViewModel(taskDefault)
		{
			Scenario = scenario,
			UseInKanban = useInKanban,
			AllowUseDetailMode = allowUseDetailMode
		};
		DesktopQuickAdd desktopQuickAdd = LocalSettings.Settings.UserPreference.desktopQuickAdd;
		if (desktopQuickAdd != null)
		{
			addTaskViewModel.UseDetailStyle = desktopQuickAdd.style == 1;
		}
		_section = section;
		if (section != null)
		{
			SetSectionProperty(addTaskViewModel, section);
		}
		if (addTaskViewModel.IsNote && addTaskViewModel.TimeData != null)
		{
			addTaskViewModel.TimeData.DueDate = null;
			addTaskViewModel.TimeData.Reminders = null;
		}
		if (!string.IsNullOrEmpty(addTaskViewModel.AccountId))
		{
			BindCalendarModel defaultCalendar = SubscribeCalendarHelper.GetDefaultCalendar(addTaskViewModel.AccountId);
			if (defaultCalendar != null)
			{
				addTaskViewModel.CalendarId = defaultCalendar.Id;
			}
		}
		addTaskViewModel.UseTagHint = !taskDefault.UseDefaultTags();
		if (!string.IsNullOrEmpty(taskDefault.GetColumnId()))
		{
			addTaskViewModel.Hint = Utils.GetString(addTaskViewModel.IsNote ? "AddaNote" : "AddaTask");
		}
		else
		{
			addTaskViewModel.ResetHint();
		}
		return addTaskViewModel;
	}

	private static void SetSectionProperty(AddTaskViewModel model, Section section)
	{
		if (section is NoteSection)
		{
			model._isNote = true;
		}
		if (model.IsNote)
		{
			model._priority = 0;
		}
		if (section is PrioritySection && !model._isNote)
		{
			int priority = section.GetPriority();
			model._priority = priority;
		}
		if (section is ProjectSection)
		{
			model._projectId = section.GetProjectId();
			model._projectName = section.Name;
		}
		if (section is TagSection { SectionId: var sectionId })
		{
			model.Tags = new List<string>();
			if (sectionId != "notag")
			{
				model.Tags.Add(sectionId);
				model.NotifyDisplayTagsChanged();
			}
		}
		DateTime? startDate = section?.GetStartDate();
		if (startDate.HasValue)
		{
			model.StartDate = startDate;
		}
		if (section is NodateSection)
		{
			model.StartDate = null;
		}
		if (section is PinnedSection)
		{
			model.IsPin = true;
		}
		if (section is AssigneeSection assigneeSection)
		{
			model.Assignee = assigneeSection.GetAssignee();
		}
		if (section is CompletedSection && !model._isNote)
		{
			model.IsComplete = true;
		}
	}

	public void Init(QuickAddView.Scenario scenario = QuickAddView.Scenario.TaskList)
	{
		OriginalProjectId = _projectId;
		OriginalPriority = _priority;
		OriginalTimeData = TimeData.Clone(_timeData);
		OriginalProjectName = _projectName;
		OriginalAddToColumnId = GetProjectColumnId();
		OriginalCalendarId = _calendarId;
		OriginalCalendarName = CalendarName;
		OriginalAccountId = AccountId;
		OriginalTags = _tags?.ToList() ?? new List<string>();
		OriginalScenario = scenario;
		IsOriginalNote = IsNote;
		IsOriginalCalendar = IsCalendar;
		SelectProject = false;
		AutoAddTags = true;
		StartDate = _timeData?.StartDate;
	}

	public void Reset(IProjectTaskDefault taskDefault, bool keepKindConvertCache = false)
	{
		if (!keepKindConvertCache)
		{
			ClearKindConvertCache();
		}
		ProjectId = (IsOriginalCalendar ? OriginalCalendarId : OriginalProjectId);
		AddToColumnId = OriginalAddToColumnId;
		ProjectName = (IsOriginalCalendar ? OriginalCalendarName : OriginalProjectName);
		Priority = OriginalPriority;
		CalendarId = OriginalCalendarId;
		CalendarName = OriginalCalendarName;
		AccountId = OriginalAccountId;
		Tags = OriginalTags;
		IsCalendar = IsOriginalCalendar;
		AllowUseDetailMode = true;
		IsNote = IsOriginalNote;
		Scenario = OriginalScenario;
		ResetTimeData(taskDefault);
		Assignee = taskDefault?.GetAssignee();
		TaskKindSelected = false;
		TitleText = string.Empty;
		ContentText = string.Empty;
		TitleCountText = string.Empty;
	}

	public void CacheTaskFieldsBeforeKindConvert(bool keepExisting = false, string calendarId = null, string calendarName = null, string accountId = null, string location = null, string projectTokenValue = null)
	{
		AddTaskKindConvertCache kindConvertCache = _kindConvertCache;
		if (keepExisting && _kindConvertCache != null)
		{
			_kindConvertCache.SetCalendarFields(calendarId, calendarName, accountId, location);
			_kindConvertCache.UpdateSharedTaskFields(this, projectTokenValue);
			return;
		}
		_kindConvertCache = AddTaskKindConvertCache.From(this);
		_kindConvertCache.KeepCalendarFieldsFrom(kindConvertCache);
		_kindConvertCache.SetCalendarFields(calendarId, calendarName, accountId, location);
		_kindConvertCache.SetProjectTokenValue(projectTokenValue);
	}

	public void RestoreTaskFieldsAfterKindConvert(bool includeTags = false, bool includeSharedFields = true)
	{
		_kindConvertCache?.ApplyTo(this, includeTags, includeSharedFields);
	}

	public string RestoreAttachmentMarkdownAfterKindConvert(string content)
	{
		return _kindConvertCache?.RestoreAttachmentMarkdown(content) ?? content;
	}

	public void CacheAttachmentContentForCalendar(string originalContent, string calendarContent)
	{
		_kindConvertCache?.CacheAttachmentContentForCalendar(originalContent, calendarContent);
	}

	public void ClearRepeatForNote()
	{
		if (_timeData != null)
		{
			_kindConvertCache?.CacheRepeatHiddenForNote(_timeData);
			_timeData.RepeatFlag = null;
			_timeData.RepeatFrom = "2";
			_timeData.ExDates = new List<string>();
			OnPropertyChanged("ShowRepeat");
		}
	}

	public void RestoreFilesAfterKindConvert(bool includeTags = false)
	{
		_kindConvertCache?.ApplyFilesTo(this, includeTags);
	}

	public void RestoreCalendarFieldsAfterKindConvert()
	{
		_kindConvertCache?.ApplyCalendarTo(this);
	}

	public void RestoreRepeatForCalendar()
	{
		_kindConvertCache?.ApplyStandardRepeatTo(_timeData);
		OnPropertyChanged("ShowRepeat");
	}

	public void EnsureTaskProjectAfterCalendarConvert(bool forceDefault = false)
	{
		EnsureProjectForCreate(forceDefault);
	}

	public void EnsureProjectForCreate(bool forceDefault = false)
	{
		ProjectModel projectModel = CacheManager.GetProjects().FirstOrDefault((ProjectModel p) => p.id == ProjectId);
		if (forceDefault || projectModel == null || !projectModel.IsEnable())
		{
			NormalProjectIdentity defaultProject = ProjectIdentity.GetDefaultProject();
			ProjectId = defaultProject.GetProjectId();
			ProjectName = defaultProject.GetProjectName() ?? Utils.GetString("Inbox");
			ColumnId = defaultProject.GetColumnId();
			AddToColumnId = defaultProject.GetColumnId();
			NotifyProjectChanged();
		}
	}

	public void ClearKindConvertCache()
	{
		_kindConvertCache = null;
	}

	public void ResetProject()
	{
		if (_projectId != OriginalProjectId)
		{
			if (IsOriginalCalendar && !IsCalendar)
			{
				EnsureTaskProjectAfterCalendarConvert(forceDefault: true);
				return;
			}
			ProjectId = OriginalProjectId;
			AddToColumnId = OriginalAddToColumnId;
			ProjectName = OriginalProjectName;
			IsNote = IsOriginalNote;
		}
	}

	public void ResetTimeData(IProjectTaskDefault taskDefault)
	{
		TimeData timeData = taskDefault?.GetTimeData() ?? TimeData.Clone(OriginalTimeData);
		if (_section != null)
		{
			DateTime? startDate = _section?.GetStartDate();
			if (startDate.HasValue)
			{
				timeData.StartDate = startDate;
			}
			if (_section is NodateSection)
			{
				timeData.StartDate = null;
			}
		}
		TimeData = timeData;
	}

	public void ResetPriority()
	{
		Priority = OriginalPriority;
		ManuallySelectedPriority = false;
	}

	private string GetHint(bool focused, bool withProject = false)
	{
		if ((focused && UseDetailStyle) || _scenario == QuickAddView.Scenario.QuickAdd)
		{
			return Utils.GetString(IsNote ? "record_inspiration_and_time" : "what_would_you_like_to_do");
		}
		string arg = (IsNote ? Utils.GetString("single_note") : Utils.GetString("Task"));
		bool flag = CheckNeedShowEnterHint();
		if (withProject)
		{
			ProjectModel projectModel = CacheManager.GetProjects().FirstOrDefault((ProjectModel p) => p.id == ProjectId);
			if (projectModel != null)
			{
				return string.Format(Utils.GetString("add_note_or_task_to_list"), projectModel.name, arg) + (flag ? Utils.GetString("press_enter_to_save") : string.Empty);
			}
			return Utils.GetString(IsNote ? "AddaNote" : "AddaTask");
		}
		return Utils.GetString(IsNote ? "AddaNote" : "AddaTask") + (flag ? Utils.GetString("press_enter_to_save") : string.Empty);
	}

	private static bool CheckNeedShowEnterHint()
	{
		ExtraSettings extraSettings = LocalSettings.Settings.ExtraSettings;
		if (!LocalSettings.Settings.Common.FirstInstall)
		{
			return false;
		}
		return extraSettings.EnterTaskCount < 3;
	}

	public void ResetHint()
	{
		try
		{
			if (IsCalendar)
			{
				Hint = (UseDetailStyle ? Utils.GetString("WhatsOnAgenda") : string.Format(Utils.GetString("AddAgendaTo"), CalendarName));
				return;
			}
			if (TitleFocused || UseGlobalStyle)
			{
				Hint = GetHint(focused: true);
				return;
			}
			if (!string.IsNullOrEmpty(ColumnId))
			{
				Hint = Utils.GetString(IsNote ? "AddaNote" : "AddaTask");
				return;
			}
			if (!UseTagHint || _tags == null || _tags.Count == 0)
			{
				Hint = GetHint(focused: false, ForceShowProjectName);
				return;
			}
			List<string> source = _tags.Select((string t) => CacheManager.GetTagByName(t.ToLower())?.GetDisplayName() ?? t).ToList();
			Hint = string.Join(" ", source.Select((string tag) => "#" + tag?.Replace("#", string.Empty)).ToList());
		}
		catch (Exception)
		{
			Hint = "";
		}
	}

	public void RefreshDayText(TimeData timeData)
	{
		if (timeData != null && timeData.StartDate.HasValue && !Utils.IsEmptyDate(timeData.StartDate))
		{
			DateTime? due = timeData.DueDate;
			if (_isNote)
			{
				due = null;
			}
			DetailDayText = DateUtils.FormatQuickAddDateString(timeData.StartDate.Value, due, timeData.IsAllDay);
		}
		else
		{
			DetailDayText = string.Empty;
		}
		OnPropertyChanged("DayTextIcon");
		OnPropertyChanged("ShowRepeat");
	}

	public bool ProjectChanged()
	{
		return _projectId != OriginalProjectId;
	}

	public string GetProjectColumnId()
	{
		if (string.IsNullOrEmpty(AddToColumnId))
		{
			if (ColumnId == null || ColumnId.Contains(":"))
			{
				return string.Empty;
			}
			return ColumnId;
		}
		return AddToColumnId;
	}

	public string GetColumnIdForCreate()
	{
		if (!string.IsNullOrEmpty(AddToColumnId))
		{
			return AddToColumnId;
		}
		if (!(TaskDefault is ColumnProjectIdentity))
		{
			return string.Empty;
		}
		return GetProjectColumnId();
	}

	public void AddFile(string path)
	{
		Constants.AttachmentKind fileType = AttachmentProvider.GetFileType(path);
		FileUtils.CollectFileSize(path);
		if (FileUtils.FileOverSize(path) && fileType != Constants.AttachmentKind.IMAGE)
		{
			Utils.Toast(LocalSettings.Settings.IsPro ? Utils.GetString("AttachmentSizeLimitPro") : Utils.GetString("AttachmentSizeLimit"));
		}
		else if (Files.All((AddTaskAttachmentInfo f) => f.Path != path))
		{
			Files.Add(new AddTaskAttachmentInfo(path));
		}
	}

	internal void Remove(AddTaskAttachmentInfo info)
	{
		Files.Remove(info);
		try
		{
			FileInfo fileInfo = new FileInfo(info.Path);
			fileInfo.Attributes = FileAttributes.Normal;
			fileInfo.Delete();
		}
		catch (Exception ex)
		{
			UtilLog.Info(ex.Message);
		}
	}

	public void ClearFiles(bool needDelete)
	{
		List<AddTaskAttachmentInfo> list = Files.ToList();
		Application.Current?.Dispatcher?.Invoke(delegate
		{
			Files.Clear();
		});
		if (!needDelete)
		{
			return;
		}
		foreach (AddTaskAttachmentInfo item in list)
		{
			try
			{
				FileInfo fileInfo = new FileInfo(item.Path);
				fileInfo.Attributes = FileAttributes.Normal;
				fileInfo.Delete();
			}
			catch (Exception ex)
			{
				UtilLog.Info(ex.Message);
			}
		}
	}

	public List<AddTaskAttachmentInfo> ClearAndGetFiles()
	{
		List<AddTaskAttachmentInfo> result = Files.ToList();
		ClearFiles(needDelete: false);
		return result;
	}

	internal void MergeFiles(IEnumerable<string> paths)
	{
		if (paths == null)
		{
			return;
		}
		foreach (string path in paths)
		{
			if (!string.IsNullOrEmpty(path) && !Files.Any((AddTaskAttachmentInfo file) => string.Equals(file.Path, path, StringComparison.OrdinalIgnoreCase)))
			{
				Files.Add(new AddTaskAttachmentInfo(path));
			}
		}
	}

	~AddTaskViewModel()
	{
		if (Files.Count > 0)
		{
			ClearFiles(needDelete: true);
		}
	}
}
