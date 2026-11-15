using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Provider;
using ticktick_WPF.Views.Habit;
using ticktick_WPF.Views.Search;
using ticktick_WPF.Views.Tag;
using ticktick_WPF.Views.TaskList;

namespace ticktick_WPF.ViewModels;

[Serializable]
public class DisplayItemModel : BaseHidableViewModel, AgendaHelper.IAgenda, INode
{
	private string _avatarUrl;

	private string _content;

	private int _calendarType;

	private bool _dragging;

	private bool _dropHover;

	private bool _isOpen = true;

	private int _num;

	private bool _selected;

	private bool _showProject;

	private bool _canShowProject = true;

	private bool _lineVisible = true;

	private int _level;

	private bool _showTopMargin = true;

	private bool _showBottomMargin = true;

	private bool _isLastInKanbanColumn;

	private long _special;

	public List<string> DragTaskIds;

	private bool _inOperation;

	private bool _inBatchSelected;

	private bool _underTaskItem = true;

	private bool _inGantt;

	private bool _hitVisible = true;

	private bool _titleHitVisible = true;

	public bool SortByCTime;

	private string _completionRate;

	private bool _eventAdded;

	private List<TagViewModel> _displayTags;

	public TaskBaseViewModel SourceViewModel { get; set; }

	public long SpecialOrder
	{
		get
		{
			return _special;
		}
		set
		{
			_special = value;
		}
	}

	public string[] Tags => SourceViewModel.Tags;

	public string TimeZoneName => SourceViewModel.TimeZoneName;

	public bool TitleHitVisible
	{
		get
		{
			if (!InMatrix && !IsHabit && !IsCourse && !IsCountdown)
			{
				return _titleHitVisible;
			}
			return false;
		}
	}

	public Section Section { get; set; }

	public string Id => SourceViewModel.Id;

	public string TaskId => SourceViewModel.GetTaskId();

	public string Assignee => SourceViewModel.Assignee;

	public string RepeatFrom => SourceViewModel.RepeatFrom;

	public string ProjectId => SourceViewModel.ProjectId;

	public long SortOrder => SourceViewModel.SortOrder;

	public string EntityId => SourceViewModel.EntityId ?? SourceViewModel.Id;

	public DateTime? RemindTime => SourceViewModel.RemindTime;

	public DateTime ModifiedTime => SourceViewModel.ModifiedTime ?? DateTime.Now;

	public DateTime CreatedTime => SourceViewModel.CreatedTime ?? DateTime.Now;

	public long ProjectOrder => SourceViewModel.ProjectOrder;

	public long ParentOrder => SourceViewModel.ParentOrder;

	public bool ShowLocation => SourceViewModel.HasLocation;

	public string Title => SourceViewModel.Title;

	public string Color
	{
		get
		{
			if (!string.IsNullOrEmpty(DisplayColor))
			{
				return DisplayColor;
			}
			if (!string.IsNullOrEmpty(SourceViewModel.Color))
			{
				return SourceViewModel.Color;
			}
			return "Transparent";
		}
	}

	public bool ShowProjectColor
	{
		get
		{
			if (InKanban && !string.IsNullOrEmpty(Color) && Color.ToLower() != "transparent" && !IsCourse)
			{
				return !IsCountdown;
			}
			return false;
		}
	}

	public string DisplayColor { get; set; }

	public string CalendarId => SourceViewModel.CalendarId;

	public bool ShowDragBar { get; set; } = true;

	public bool BatchMode { get; set; }

	public string AttendId => SourceViewModel.AttendId;

	public string ColumnId => SourceViewModel.ColumnId;

	public bool InKanban { get; set; }

	public bool InMatrix { get; set; }

	public string TeamId { get; set; }

	public string Permission { get; set; }

	public bool IsFloating => SourceViewModel.IsFloating;

	public HabitModel Habit
	{
		get
		{
			if (!IsHabit)
			{
				return null;
			}
			return (SourceViewModel as HabitBaseViewModel)?.Habit;
		}
	}

	public int OriginLevel { get; set; }

	public int DraggingLevel { get; set; }

	public bool IsPinned => PinnedTime > 0;

	public DisplayItemModel Parent { get; set; }

	public string ReminderString { get; set; }

	public string CompletionRate
	{
		get
		{
			return _completionRate;
		}
		set
		{
			_completionRate = value;
			OnPropertyChanged("CompletionRate");
		}
	}

	public ProjectIdentity CurrentProjectIdentity { get; set; }

	public bool InGantt
	{
		get
		{
			return _inGantt;
		}
		set
		{
			_inGantt = value;
			OnPropertyChanged("InGantt");
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
			OnPropertyChanged("Content");
			OnPropertyChanged("ShowContent");
		}
	}

	public bool HitVisible
	{
		get
		{
			return _hitVisible;
		}
		set
		{
			if (_hitVisible != value)
			{
				_hitVisible = value;
				OnPropertyChanged("HitVisible");
			}
		}
	}

	public long PinnedTime => SourceViewModel.PinnedTime;

	public string ProjectName => SourceViewModel.ProjectName;

	public bool ShowTopMargin
	{
		get
		{
			return _showTopMargin;
		}
		set
		{
			if (_showTopMargin != value)
			{
				_showTopMargin = value;
				OnPropertyChanged("ShowTopMargin");
			}
		}
	}

	public bool ShowBottomMargin
	{
		get
		{
			return _showBottomMargin;
		}
		set
		{
			if (_showBottomMargin != value)
			{
				_showBottomMargin = value;
				OnPropertyChanged("ShowBottomMargin");
			}
		}
	}

	public bool IsLastInKanbanColumn
	{
		get
		{
			return _isLastInKanbanColumn;
		}
		set
		{
			if (_isLastInKanbanColumn != value)
			{
				_isLastInKanbanColumn = value;
				OnPropertyChanged("IsLastInKanbanColumn");
			}
		}
	}

	public bool Enable => SourceViewModel.Editable;

	public bool LineVisible
	{
		get
		{
			return _lineVisible;
		}
		set
		{
			_lineVisible = value;
			OnPropertyChanged("LineVisible");
		}
	}

	public HabitCheckInModel HabitCheckIn
	{
		get
		{
			if (!IsHabit)
			{
				return null;
			}
			return (SourceViewModel as HabitBaseViewModel)?.HabitCheckIn;
		}
	}

	public string HabitProgressText
	{
		get
		{
			if (Habit == null || Habit.Type?.ToLower() == "boolean")
			{
				return string.Empty;
			}
			if (HabitCheckIn != null)
			{
				return HabitCheckIn.Value + "/" + HabitCheckIn.Goal + " " + Habit.Unit;
			}
			return 0 + "/" + Habit.Goal + " " + Habit.Unit;
		}
	}

	public int CalendarType
	{
		get
		{
			return _calendarType;
		}
		set
		{
			_calendarType = value;
			OnPropertyChanged("CalendarType");
		}
	}

	public bool DropHover
	{
		get
		{
			return _dropHover;
		}
		set
		{
			if (_dropHover != value)
			{
				_dropHover = value;
				OnPropertyChanged("DropHover");
			}
		}
	}

	public bool Dragging
	{
		get
		{
			return _dragging;
		}
		set
		{
			_dragging = value;
			OnPropertyChanged("Dragging");
		}
	}

	public List<TagViewModel> DisplayTags
	{
		get
		{
			if (_displayTags == null)
			{
				int maxCount = 2;
				if (InKanban)
				{
					switch (LocalSettings.Settings.ExtraSettings.KbSize)
					{
					case 0:
						maxCount = 2;
						break;
					case 1:
						maxCount = 3;
						break;
					case 2:
						maxCount = 4;
						break;
					}
				}
				_displayTags = BuildTagModels(SourceViewModel.Tags, InKanban, InMatrix, maxCount);
			}
			return _displayTags;
		}
	}

	public int Progress => SourceViewModel.Progress;

	public bool ShowTag
	{
		get
		{
			if (SourceViewModel.Tags != null)
			{
				return SourceViewModel.Tags.Any();
			}
			return false;
		}
	}

	public bool ShowProgress
	{
		get
		{
			if (Progress > 0)
			{
				return Status == 0;
			}
			return false;
		}
	}

	public string ProgressContent => Progress + "%";

	public bool IsCustomizedSection
	{
		get
		{
			if (Type == DisplayType.Section)
			{
				return Section.Customized;
			}
			return false;
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

	public int Deleted => SourceViewModel.Deleted;

	public string RepeatFlag => SourceViewModel.RepeatFlag;

	public string Kind => SourceViewModel.Kind;

	public DateTime? DueDate
	{
		get
		{
			if (ParseData != null)
			{
				return ParseData.DueDate;
			}
			return SourceViewModel.DueDate;
		}
	}

	public DateTime? StartDate
	{
		get
		{
			if (ParseData != null)
			{
				return ParseData.StartDate;
			}
			return SourceViewModel.StartDate;
		}
	}

	public DateTime? CompletedTime => SourceViewModel.CompletedTime;

	public int Num
	{
		get
		{
			return _num;
		}
		set
		{
			_num = value;
			OnPropertyChanged("Num");
		}
	}

	public int Priority => SourceViewModel.Priority;

	public bool IsOpen
	{
		get
		{
			return _isOpen;
		}
		set
		{
			_isOpen = value;
			OnPropertyChanged("IsOpen");
		}
	}

	public bool IsAbandoned => Status == -1;

	public bool IsCompleted => Status == 2;

	public int Status
	{
		get
		{
			if (!Linked)
			{
				return SourceViewModel.Status;
			}
			return 2;
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

	public DateTime? NoteDisplayDate
	{
		get
		{
			if (IsNote)
			{
				if (!SortByCTime)
				{
					return SourceViewModel.ModifiedTime;
				}
				return SourceViewModel.CreatedTime;
			}
			return null;
		}
	}

	public bool ShowNoteDate
	{
		get
		{
			if (LocalSettings.Settings.ShowDetails)
			{
				return !InMatrix;
			}
			return false;
		}
	}

	public bool? ShowAttachment { get; set; }

	public bool? ShowReminder { get; set; }

	public bool ShowDescription
	{
		get
		{
			if (!string.IsNullOrEmpty(SourceViewModel.Content) && Kind != "CHECKLIST" && !SettingsHelper.GetShowDetail())
			{
				return !IsCourse;
			}
			return false;
		}
	}

	public bool ShowComment
	{
		get
		{
			if (SourceViewModel.CommentCount != null)
			{
				return SourceViewModel.CommentCount != "0";
			}
			return false;
		}
	}

	public bool ShowPomo { get; set; }

	public bool ShowRepeat
	{
		get
		{
			if (!string.IsNullOrEmpty(RepeatFlag) && !RepeatFlag.Contains("NONE"))
			{
				return StartDate.HasValue;
			}
			return false;
		}
	}

	public bool ShowContent
	{
		get
		{
			if (!string.IsNullOrEmpty(Content?.Trim()))
			{
				return SettingsHelper.GetShowDetail();
			}
			return false;
		}
	}

	public bool IsLoadMore => Type == DisplayType.LoadMore;

	public bool IsTaskOrNote
	{
		get
		{
			if (!IsTask)
			{
				return IsNote;
			}
			return true;
		}
	}

	public bool IsAgenda => Type == DisplayType.Agenda;

	public bool IsTask
	{
		get
		{
			if (Type != DisplayType.Task)
			{
				return Type == DisplayType.Agenda;
			}
			return true;
		}
	}

	public bool IsSection => Type == DisplayType.Section;

	public bool IsSplit => Type == DisplayType.Split;

	public bool IsItem => Type == DisplayType.CheckItem;

	public bool IsEvent => Type == DisplayType.Event;

	public bool IsHabit => Type == DisplayType.Habit;

	public bool IsCourse => Type == DisplayType.Course;

	public bool IsNote => Type == DisplayType.Note;

	public bool IsCountdown => Type == DisplayType.Countdown;

	public bool IsNormalItem
	{
		get
		{
			if (!IsSection && !IsLoadMore)
			{
				return !IsSplit;
			}
			return false;
		}
	}

	public DisplayType Type => SourceViewModel.Type;

	public bool ReadOnly
	{
		get
		{
			if (Enable && !IsCourse && !IsHabit && !IsCountdown && !_inBatchSelected && !InMatrix)
			{
				return !_titleHitVisible;
			}
			return true;
		}
	}

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

	public bool ShowProject
	{
		get
		{
			if (_canShowProject)
			{
				return _showProject;
			}
			return false;
		}
		set
		{
			_showProject = value;
			OnPropertyChanged("ShowProject");
		}
	}

	public int Level
	{
		get
		{
			return _level;
		}
		set
		{
			_level = value;
			OnPropertyChanged("Level");
		}
	}

	public bool InOperation
	{
		get
		{
			return _inOperation;
		}
		set
		{
			_inOperation = value;
			OnPropertyChanged("InOperation");
		}
	}

	public bool UnderTaskItem
	{
		get
		{
			return _underTaskItem;
		}
		set
		{
			_underTaskItem = value;
			OnPropertyChanged("UnderTaskItem");
		}
	}

	public bool InBatchSelected
	{
		get
		{
			return _inBatchSelected;
		}
		set
		{
			if (_inBatchSelected != value || IsSection)
			{
				_inBatchSelected = value;
				OnPropertyChanged("InBatchSelected");
				OnPropertyChanged("ReadOnly");
				OnPropertyChanged("SectionRightActionText");
			}
		}
	}

	public string SectionRightActionText
	{
		get
		{
			if (!IsSection)
			{
				return null;
			}
			if (Section.Children == null || Section.Children.Count == 0)
			{
				return null;
			}
			if (InKanban || Section.Children.Any((DisplayItemModel m) => m.InMatrix))
			{
				return null;
			}
			if (IsCustomizedSection)
			{
				if (_inBatchSelected)
				{
					List<DisplayItemModel> childrenModels = GetChildrenModels(getAll: true);
					return Utils.GetString((childrenModels != null && childrenModels.All((DisplayItemModel m) => !m.CanBatchSelect || m.InBatchSelected)) ? "DeselectAll" : "SelectAll");
				}
			}
			else
			{
				if (InBatchSelected && !(Section is HabitSection) && !(Section is TimetableSection) && !(Section.SectionId == "8ac3038d93c54b80a67321b6a03df066"))
				{
					List<DisplayItemModel> children = Section.Children;
					if (children != null && children.Any((DisplayItemModel m) => m.CanBatchSelect))
					{
						List<DisplayItemModel> childrenModels2 = GetChildrenModels(getAll: true);
						return Utils.GetString((childrenModels2 != null && childrenModels2.All((DisplayItemModel m) => !m.CanBatchSelect || m.InBatchSelected)) ? "DeselectAll" : "SelectAll");
					}
				}
				if (Section is OutdatedSection { ShowPostpone: not false })
				{
					return Utils.GetString("Postpone");
				}
			}
			return null;
		}
	}

	public string ParentId
	{
		get
		{
			if (!IsItem)
			{
				return SourceViewModel.ParentId;
			}
			return string.Empty;
		}
	}

	public Geometry Icon { get; set; }

	public string CountdownIcon { get; set; }

	public double IconWidth { get; set; } = 17.0;

	public List<DisplayItemModel> Children { get; set; }

	public List<DisplayItemModel> BatchModels { get; set; }

	public List<string> ChildIds { get; set; }

	public bool HasChildren
	{
		get
		{
			if (Children != null)
			{
				return Children.Count > 0;
			}
			return false;
		}
	}

	public bool IsParentTask => TaskCache.IsParentTask(Id);

	public bool InDetail { get; set; }

	public int MoreCount { get; set; }

	public bool NewAdd { get; set; }

	public PomodoroSummaryModel PomoSummary { get; set; }

	public SectionAddTaskViewModel AddViewModel { get; set; }

	public bool IsNewAdd { get; set; }

	public TimeData ParseData { get; set; }

	public bool IsToggling { get; set; }

	public int? CaretIndex { get; set; }

	public bool InSticky { get; set; }

	public bool Linked { get; set; }

	public bool CanBatchSelect
	{
		get
		{
			if (IsTaskOrNote && Permission != "comment")
			{
				return Permission != "read";
			}
			return false;
		}
	}

	public CourseDisplayModel Course => SourceViewModel.Course;

	public bool InTomorrow { get; set; }

	public bool IsTomorrow
	{
		get
		{
			if (!InTomorrow || Level != 0)
			{
				return Section is TomorrowSection;
			}
			return true;
		}
	}

	public bool SearchComment { get; set; }

	public string DraggingFromColumnId { get; set; }

	public List<string> BatchSelectedIds { get; set; }

	public void SetKanbanDisplayTags()
	{
		int maxCount = 2;
		switch (LocalSettings.Settings.ExtraSettings.KbSize)
		{
		case 0:
			maxCount = 2;
			break;
		case 1:
			maxCount = 3;
			break;
		case 2:
			maxCount = 4;
			break;
		}
		_displayTags = BuildTagModels(SourceViewModel.Tags, InKanban, InMatrix, maxCount);
		OnPropertyChanged("DisplayTags");
	}

	public void SetDisplayTags(int maxCount)
	{
		_displayTags = BuildTagModels(SourceViewModel.Tags, InKanban, InMatrix, maxCount);
		OnPropertyChanged("DisplayTags");
	}

	public async void TrySetAvatar()
	{
		if (!string.IsNullOrEmpty(AvatarUrl) && Avatar == null)
		{
			SetAvatar();
		}
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

	public void NotifyShowIconsChanged()
	{
		OnPropertyChanged("ShowIcons");
	}

	public string GetTaskId()
	{
		return TaskId;
	}

	public string GetAttendId()
	{
		return AttendId;
	}

	public static DisplayItemModel Copy(DisplayItemModel origin)
	{
		DisplayItemModel displayItemModel = origin.Clone();
		displayItemModel.Dragging = false;
		return displayItemModel;
	}

	public DisplayItemModel()
	{
	}

	public DisplayItemModel(TaskBaseViewModel vm, bool showProject = false, bool isKanbanMode = false)
	{
		SourceViewModel = vm;
		_avatarUrl = ((!string.IsNullOrEmpty(vm.Assignee)) ? AvatarHelper.GetCacheUrl(vm.Assignee, vm.ProjectId) : string.Empty);
		if (vm.IsEvent)
		{
			SourceViewModel.ProjectName = SubscribeCalendarHelper.GetCalendarName(vm.CalendarId);
			CalendarType = SubscribeCalendarHelper.GetCalendarType(vm.CalendarId);
		}
		if (vm.IsTaskOrNote)
		{
			ProjectModel projectById = CacheManager.GetProjectById(vm.ProjectId);
			TeamId = projectById?.teamId;
			Permission = projectById?.permission;
		}
		if (vm is HabitBaseViewModel habitBaseViewModel)
		{
			ShowReminder = (habitBaseViewModel.HabitCheckIn == null || (habitBaseViewModel.HabitCheckIn.CheckStatus == 0 && habitBaseViewModel.HabitCheckIn.Value < habitBaseViewModel.HabitCheckIn.Goal)) && HabitUtils.IsReminderValid(habitBaseViewModel.Habit.Reminder);
		}
		_isOpen = vm.IsOpen;
		_showProject = showProject && (!vm.IsTaskOrNote || CacheManager.GetProjectById(vm.ProjectId) != null);
		InKanban = isKanbanMode;
	}

	private void AddVmPropertyChangedEvent()
	{
		if (SourceViewModel != null)
		{
			PropertyChangedEventManager.RemoveHandler(SourceViewModel, OnModelPropertyChanged, string.Empty);
			PropertyChangedEventManager.AddHandler(SourceViewModel, OnModelPropertyChanged, string.Empty);
		}
	}

	private void OnModelPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		string propertyName = e.PropertyName;
		if (propertyName == null)
		{
			return;
		}
		switch (propertyName.Length)
		{
		case 5:
			switch (propertyName[0])
			{
			case 'T':
				if (propertyName == "Title")
				{
					OnPropertyChanged("Title");
				}
				break;
			case 'C':
				if (propertyName == "Color")
				{
					OnPropertyChanged("Color");
					OnPropertyChanged("ShowProjectColor");
				}
				break;
			}
			break;
		case 7:
			switch (propertyName[0])
			{
			case 'C':
				if (propertyName == "Content")
				{
					if (SettingsHelper.GetShowDetail())
					{
						AssembleModelContent(this);
					}
					else
					{
						OnPropertyChanged("ShowIcons");
					}
				}
				break;
			case 'D':
				if (propertyName == "DueDate")
				{
					OnPropertyChanged("DueDate");
				}
				break;
			}
			break;
		case 4:
			switch (propertyName[0])
			{
			case 'D':
				if (propertyName == "Desc" && SettingsHelper.GetShowDetail())
				{
					AssembleModelContent(this);
				}
				break;
			case 'T':
				if (propertyName == "Type")
				{
					OnPropertyChanged("Type");
					OnPropertyChanged("ShowNoteDate");
				}
				break;
			case 'K':
				if (propertyName == "Kind")
				{
					SetIcon();
				}
				break;
			}
			break;
		case 12:
			switch (propertyName[0])
			{
			case 'M':
				if (propertyName == "ModifiedTime")
				{
					OnPropertyChanged("NoteDisplayDate");
				}
				break;
			case 'C':
				if (propertyName == "CommentCount")
				{
					OnPropertyChanged("ShowIcons");
				}
				break;
			}
			break;
		case 8:
			switch (propertyName[3])
			{
			case 't':
				if (propertyName == "Editable")
				{
					OnPropertyChanged("Enable");
				}
				break;
			case 'o':
				if (propertyName == "Priority")
				{
					OnPropertyChanged("Priority");
				}
				break;
			case 'l':
				if (propertyName == "IsAllDay")
				{
					OnPropertyChanged("IsAllDay");
				}
				break;
			case 'g':
				if (propertyName == "Progress")
				{
					OnPropertyChanged("ShowIcons");
				}
				break;
			case 'i':
				if (propertyName == "Assignee")
				{
					AvatarUrl = ((!string.IsNullOrEmpty(Assignee)) ? AvatarHelper.GetCacheUrl(Assignee, ProjectId) : string.Empty);
				}
				break;
			}
			break;
		case 16:
			if (propertyName == "CheckItemContent" && SettingsHelper.GetShowDetail())
			{
				AssembleModelContent(this);
			}
			break;
		case 11:
			if (propertyName == "ProjectName")
			{
				OnPropertyChanged("ProjectName");
			}
			break;
		case 10:
			if (propertyName == "RepeatFlag")
			{
				OnPropertyChanged("ShowRepeat");
				OnPropertyChanged("ShowIcons");
			}
			break;
		case 9:
			if (propertyName == "StartDate")
			{
				OnPropertyChanged("StartDate");
				OnPropertyChanged("ShowIcons");
				TaskItemLoadHelper.LoadShowReminder(this);
			}
			break;
		case 6:
			if (propertyName == "Status")
			{
				OnPropertyChanged("Status");
				SetIcon();
				OnPropertyChanged("IsAbandoned");
				OnPropertyChanged("IsCompleted");
				OnPropertyChanged("ShowIcons");
			}
			break;
		case 3:
			if (propertyName == "Tag")
			{
				OnPropertyChanged("Tags");
				if (InKanban)
				{
					SetKanbanDisplayTags();
				}
				OnPropertyChanged("ShowTag");
				OnPropertyChanged("ShowTagProjectPanel");
			}
			break;
		case 13:
		case 14:
		case 15:
			break;
		}
	}

	public static List<TagViewModel> BuildTagModels(IReadOnlyList<string> tags, bool isKanbanMode = false, bool inMatrix = false, int maxCount = 2)
	{
		if (tags != null && tags.Count > 0 && maxCount >= 0)
		{
			List<TagViewModel> list = new List<TagViewModel>();
			List<TagModel> cachedTags = TagDataHelper.GetTags();
			List<TagViewModel> source = (from tag in tags
				let local = cachedTags.FirstOrDefault((TagModel t) => t.name == tag)
				select (local == null) ? new TagViewModel(tag, tag) : new TagViewModel(local)
				{
					SortOrder = cachedTags.IndexOf(local)
				}).ToList();
			source = source.OrderBy((TagViewModel m) => m.SortOrder).ToList();
			if (source.Count > maxCount)
			{
				string text = string.Empty;
				for (int num = maxCount; num < source.Count; num++)
				{
					TagViewModel tagViewModel = source[num];
					text = text + tagViewModel.Tag + ((num == source.Count - 1) ? string.Empty : ", ");
				}
				list.AddRange(source.Take(maxCount));
				list.Add(new TagViewModel("+" + (source.Count - maxCount), text));
				return list;
			}
			return source;
		}
		return new List<TagViewModel>();
	}

	public static DisplayItemModel BuildSection(Section section, int num = 0)
	{
		bool flag = true;
		if (section is CustomizedSection)
		{
			flag = CacheManager.GetProjects().FirstOrDefault((ProjectModel p) => p.id == section.ProjectId)?.IsEnable() ?? false;
		}
		if (flag && section is OutdatedSection)
		{
			flag = false;
			foreach (DisplayItemModel child in section.Children)
			{
				if (child.Enable && !child.IsAgenda)
				{
					flag = true;
					break;
				}
				if (child.IsAgenda && child.AttendId == child.Id)
				{
					flag = true;
					break;
				}
			}
		}
		return new DisplayItemModel(new TaskBaseViewModel(section)
		{
			Editable = flag,
			ProjectId = section.ProjectId
		})
		{
			Section = section,
			_num = num
		};
	}

	public static DisplayItemModel BuildLoadMore()
	{
		return new DisplayItemModel(new TaskBaseViewModel
		{
			Type = DisplayType.LoadMore
		});
	}

	private string GetEntityType()
	{
		return Type switch
		{
			DisplayType.Task => "task", 
			DisplayType.CheckItem => "item", 
			DisplayType.Derivative => "derivative", 
			DisplayType.Event => "event", 
			DisplayType.Agenda => "agenda", 
			_ => string.Empty, 
		};
	}

	public static DisplayItemModel AssembleModelContent(DisplayItemModel model)
	{
		if (model?.SourceViewModel != null)
		{
			TaskBaseViewModel sourceViewModel = model.SourceViewModel;
			string text = string.Empty;
			if (!LocalSettings.Settings.InSearch || string.IsNullOrEmpty(SearchHelper.SearchKey))
			{
				text = ((!(sourceViewModel.Kind == "CHECKLIST")) ? sourceViewModel.Content : (string.IsNullOrEmpty(sourceViewModel.Desc) ? ("- " + TaskDetailItemCache.GetPrimarySubtaskInTask(model.TaskId)?.Title) : sourceViewModel.Desc));
			}
			else if (sourceViewModel.Kind == "CHECKLIST")
			{
				Match match = SearchHelper.SearchRegex.Match(sourceViewModel.Desc?.ToLower() ?? string.Empty);
				if (sourceViewModel.Desc != null && match.Success)
				{
					text = ((match.Index > 8) ? ("..." + sourceViewModel.Desc.Substring(match.Index - 8).Replace("\r\n", " ").Replace("\r", " ")
						.Replace("\n", " ")) : sourceViewModel.Desc);
				}
				else
				{
					List<TaskBaseViewModel> checkItemsByTaskId = TaskDetailItemCache.GetCheckItemsByTaskId(model.TaskId);
					if (checkItemsByTaskId != null)
					{
						checkItemsByTaskId.Sort((TaskBaseViewModel a, TaskBaseViewModel b) => a.SortOrder.CompareTo(b.SortOrder));
						foreach (TaskBaseViewModel item in checkItemsByTaskId)
						{
							Match match2 = SearchHelper.SearchRegex.Match(item.Title?.ToLower() ?? string.Empty);
							if (item.Title != null && match2.Success)
							{
								text = "- " + ((match2.Index > 8) ? ("..." + item.Title.Substring(match2.Index - 8)) : item.Title);
								break;
							}
						}
						if (string.IsNullOrEmpty(text))
						{
							if (!string.IsNullOrEmpty(sourceViewModel.Desc))
							{
								text = sourceViewModel.Desc;
							}
							else
							{
								TaskBaseViewModel taskBaseViewModel = checkItemsByTaskId.FirstOrDefault((TaskBaseViewModel i) => i.Status == 0) ?? checkItemsByTaskId.FirstOrDefault();
								text = (string.IsNullOrEmpty(taskBaseViewModel?.Title) ? string.Empty : ("- " + taskBaseViewModel.Title));
							}
						}
					}
				}
			}
			else if (!string.IsNullOrEmpty(sourceViewModel.Content))
			{
				string contentDisplayText = TaskUtils.GetContentDisplayText(sourceViewModel.Content);
				Match match3 = SearchHelper.SearchRegex.Match(contentDisplayText.ToLower());
				text = ((!match3.Success || match3.Index <= 11) ? contentDisplayText : ("..." + contentDisplayText.Substring(match3.Index - 10).Replace("\r\n", " ").Replace("\r", " ")
					.Replace("\n", " ")));
			}
			else
			{
				text = string.Empty;
			}
			if (model.Content != text)
			{
				model.Content = text;
			}
		}
		return model;
	}

	public static async void AssembleModelContent(ObservableCollection<DisplayItemModel> models)
	{
		if (models == null || !models.Any())
		{
			return;
		}
		List<string> taskIds = (from model in models
			where model.IsTask && !string.IsNullOrEmpty(model.TaskId) && !string.IsNullOrEmpty(model.Kind) && model.Kind == "CHECKLIST"
			select model.TaskId).ToList();
		if (!taskIds.Any())
		{
			return;
		}
		Dictionary<string, string> descDict = new Dictionary<string, string>();
		ObservableCollection<TaskModel> observableCollection = await TaskDao.GetTasksInTaskIds(taskIds);
		if (observableCollection != null && observableCollection.Any())
		{
			foreach (TaskModel item in observableCollection)
			{
				if (!string.IsNullOrEmpty(item.desc) && !descDict.ContainsKey(item.id))
				{
					descDict.Add(item.id, item.desc);
				}
			}
		}
		Dictionary<string, TaskDetailItemModel> dictionary = await TaskDetailItemDao.GetPrimarySubtaskInTaskIds(taskIds.Where((string id) => id != null && !descDict.Keys.Contains(id)).ToList());
		foreach (DisplayItemModel model in models)
		{
			if (model.IsTask && model.Kind == "CHECKLIST" && !string.IsNullOrEmpty(model.TaskId))
			{
				if (descDict.ContainsKey(model.TaskId))
				{
					model.Content = descDict[model.TaskId];
				}
				else if (dictionary.ContainsKey(model.TaskId))
				{
					TaskDetailItemModel taskDetailItemModel = dictionary[model.TaskId];
					model.Content = "- " + taskDetailItemModel.title;
				}
			}
		}
	}

	public TaskModel GetTaskModel()
	{
		return new TaskModel
		{
			id = Id,
			content = Content,
			sortOrder = SortOrder,
			projectId = ProjectId,
			priority = Priority,
			title = Title,
			status = Status,
			isAllDay = IsAllDay,
			startDate = StartDate,
			dueDate = DueDate,
			kind = Kind,
			deleted = Deleted,
			repeatFlag = RepeatFlag,
			repeatFrom = RepeatFrom,
			assignee = Assignee,
			commentCount = SourceViewModel.CommentCount,
			completedTime = CompletedTime,
			tag = TagSerializer.ToJsonContent(Tags?.ToList()),
			parentId = ParentId,
			columnId = ColumnId,
			pinnedTimeStamp = PinnedTime
		};
	}

	public new DisplayItemModel Clone()
	{
		return (DisplayItemModel)MemberwiseClone();
	}

	public List<DisplayItemModel> GetChildrenModels(bool getAll, HashSet<string> openedTaskIds = null)
	{
		List<DisplayItemModel> list = new List<DisplayItemModel>();
		if (IsSection)
		{
			List<DisplayItemModel> list2 = Section?.Children?.Where((DisplayItemModel c) => c._level == 0).ToList();
			if (list2 != null)
			{
				foreach (DisplayItemModel item in list2)
				{
					list.Add(item);
					GetChildren(item, list, getAllChild: true);
				}
			}
		}
		else
		{
			GetChildren(this, list, getAllChild: true);
		}
		return list;
		void GetChildren(DisplayItemModel model, List<DisplayItemModel> ms, bool getAllChild)
		{
			if ((getAllChild || model._isOpen) && model.Children != null)
			{
				foreach (DisplayItemModel child in model.Children)
				{
					if (openedTaskIds != null)
					{
						child._isOpen = openedTaskIds.Contains(child.Id);
					}
					ms.Add(child);
					GetChildren(child, ms, getAll);
				}
			}
		}
	}

	private async void OnChildrenChanged()
	{
		Level = Level;
		if (LocalSettings.Settings.ShowDetails)
		{
			CompletionRate = TaskCompletionRateDao.GetRateStrByIdInDb(TaskId, ProjectId);
		}
		OnPropertyChanged("HasChildren");
		OnPropertyChanged("ShowIcons");
	}

	public void AddChild(DisplayItemModel displayModel, DisplayItemModel front)
	{
		List<DisplayItemModel> children = Children;
		if (IsSection)
		{
			children = Section.Children;
		}
		if (children != null)
		{
			int num = ((front != null) ? children.IndexOf(front) : 0);
			children.Insert(Math.Min(num + 1, children.Count), displayModel);
		}
		else
		{
			Children = new List<DisplayItemModel> { displayModel };
		}
		OnChildrenChanged();
	}

	public void RemoveItem(DisplayItemModel item)
	{
		List<DisplayItemModel> children = Children;
		if (IsSection)
		{
			children = Section.Children;
		}
		children?.Remove(item);
		OnChildrenChanged();
		Num = children?.Count ?? 0;
	}

	public bool IsOutDate()
	{
		if (!DueDate.HasValue || !(DueDate <= DateTime.Today))
		{
			if (StartDate.HasValue && !DueDate.HasValue)
			{
				return StartDate < DateTime.Today;
			}
			return false;
		}
		return true;
	}

	public void RemoveParent()
	{
	}

	public void NotifyChanged()
	{
		OnPropertyChanged("ShowTag");
		OnPropertyChanged("ShowIndicator");
		OnPropertyChanged("IsHide");
	}

	public void SetIcon()
	{
		if (IsCountdown)
		{
			CountdownIcon = (SourceViewModel as CountDownBaseViewModel)?.Countdown.IconRes;
			OnPropertyChanged("CountdownIcon");
			OnPropertyChanged("IconWidth");
		}
		else
		{
			Icon = ThemeUtil.GetTaskIconGeometry(Type, Status, Kind, CalendarType);
			OnPropertyChanged("Icon");
			OnPropertyChanged("IconWidth");
		}
	}

	public bool OutDate()
	{
		if (!StartDate.HasValue)
		{
			return false;
		}
		if (!DueDate.HasValue)
		{
			return StartDate.Value < DateTime.Today;
		}
		return DueDate.Value <= DateTime.Today;
	}

	public void SetHabit(HabitModel habit)
	{
		if (SourceViewModel is HabitBaseViewModel habitBaseViewModel)
		{
			habitBaseViewModel.Habit = habit;
			OnPropertyChanged("Habit");
			OnPropertyChanged("HabitProgressText");
		}
	}

	public void SetHabitCheckIn(HabitCheckInModel checkIn)
	{
		if (SourceViewModel is HabitBaseViewModel habitBaseViewModel)
		{
			habitBaseViewModel.HabitCheckIn = checkIn;
			OnPropertyChanged("HabitCheckIn");
			OnPropertyChanged("HabitProgressText");
		}
	}

	public string GetAssignee()
	{
		if (!string.IsNullOrEmpty(Assignee))
		{
			return Assignee;
		}
		return "-1";
	}

	public void OnTimeParsed(TimeData parseData)
	{
		if (parseData != null || ParseData != null)
		{
			ParseData = parseData;
			OnPropertyChanged("StartDate");
			OnPropertyChanged("DueDate");
			OnPropertyChanged("IsAllDay");
		}
	}

	public void SetPropertyChangedEvent()
	{
		if (!_eventAdded)
		{
			_eventAdded = true;
			AddVmPropertyChangedEvent();
		}
	}

	public void NotifyPropertyChanged(string name)
	{
		OnPropertyChanged(name);
	}

	public async Task LoadPomo()
	{
		PomoSummary = await PomoService.GetAdjustPomoSummary(Id);
		ShowPomo = !(PomoSummary?.IsEmpty() ?? true);
		OnPropertyChanged("ShowPomo");
		NotifyShowIconsChanged();
	}

	public void SetShowAttachment()
	{
		ShowAttachment = IsTaskOrNote && AttachmentCache.GetTaskExistAttachment(SourceViewModel.Id);
		NotifyShowIconsChanged();
	}

	public bool GetShowAttachment()
	{
		if (!ShowAttachment.HasValue)
		{
			ShowAttachment = IsTaskOrNote && AttachmentCache.GetTaskExistAttachment(SourceViewModel.Id);
		}
		return ShowAttachment.Value;
	}

	public void SetShowReminder(bool val)
	{
		ShowReminder = val;
		OnPropertyChanged("ShowReminder");
		NotifyShowIconsChanged();
	}

	public bool CanHoverSwitch(int hoverIndex)
	{
		if ((hoverIndex != 0 || !IsSection || Section is CompletedSection) && (InDetail || Status == 0) && !IsLoadMore)
		{
			return !(Section is HabitSection);
		}
		return false;
	}

	public void SortChildren(IComparer<DisplayItemModel> comparer)
	{
		List<DisplayItemModel> children = Children;
		if (children == null || children.Count <= 0)
		{
			return;
		}
		if (comparer == null)
		{
			Children.Sort((DisplayItemModel a, DisplayItemModel b) => a.SortOrder.CompareTo(b.SortOrder));
		}
		else
		{
			Children.Sort(comparer);
		}
		foreach (DisplayItemModel child in Children)
		{
			child.SortChildren(comparer);
		}
	}

	public List<TitleChunk> GetTitleNum()
	{
		return SourceViewModel.GetTitleNum();
	}

	public static DisplayItemModel BuildSplit()
	{
		return new DisplayItemModel
		{
			SourceViewModel = new TaskBaseViewModel
			{
				Type = DisplayType.Split
			}
		};
	}

	public async Task SetDropDate(DateTime date)
	{
		if (IsTaskOrNote)
		{
			bool needSetReminder = !StartDate.HasValue;
			await TaskService.SetDate(TaskId, date, handleReminder: false, "", IsAllDay);
			if (needSetReminder)
			{
				await TaskService.SaveTaskReminders(new TaskModel
				{
					id = TaskId,
					reminders = ((IsAllDay ?? true) ? TimeData.GetDefaultAllDayReminders().ToArray() : TimeData.GetDefaultTimeReminders().ToArray())
				});
			}
		}
		if (IsItem)
		{
			TimeDataModel newDate = new TimeDataModel
			{
				TaskId = GetTaskId(),
				ItemId = Id,
				IsAllDay = (Utils.IsEmptyDate(StartDate) || (IsAllDay ?? true)),
				StartDate = ((!StartDate.HasValue) ? date : DateUtils.SetDateOnly(StartDate.Value, date)),
				DueDate = null
			};
			await TaskService.SetCheckItemDate(GetTaskId(), Id, newDate);
		}
	}

	public void SetForceReadOnly(bool readOnly)
	{
		_titleHitVisible = !readOnly;
		OnPropertyChanged("TitleHitVisible");
		OnPropertyChanged("ReadOnly");
	}

	public void SetCanShowProject(bool b)
	{
		if (_canShowProject != b)
		{
			_canShowProject = b;
			OnPropertyChanged("ShowProject");
		}
	}
}
