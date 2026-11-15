using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Provider;
using ticktick_WPF.Views.Kanban;
using ticktick_WPF.Views.MarkDown;
using ticktick_WPF.Views.Tag;

namespace ticktick_WPF.ViewModels;

public class TaskBaseViewModel : BaseViewModel, INode, AgendaHelper.IAgenda
{
	public BlockingList<TaskBaseViewModel> CheckItems;

	public TaskBaseViewModel OwnerTask;

	private DateTime? _startDate;

	private DateTime? _dueDate;

	private DateTime? _completedTime;

	private DateTime? _modifiedTime;

	private DateTime? _createdTime;

	private DateTime? _remindTime;

	private long _sortOrder;

	private int _priority;

	private int _progress;

	private int _status;

	private int _deleted;

	private string _projectId;

	private string _timeZoneName = TimeZoneData.LocalTimeZoneModel?.TimeZoneName;

	private string _title;

	private string _content;

	private string _desc;

	private string _repeatFlag;

	private string _repeatFrom;

	private string _kind;

	private string _color;

	private string _projectName;

	private string _assignee;

	private string _tag;

	private string _commentCount;

	private bool? _isAllDay;

	private bool _editable = true;

	private CalendarAvailableType _calendarAvailableType;

	private DisplayType _type;

	private int _imageMode;

	public string Resources;

	public string Actions;

	public string Label;

	private int _propertyCheckCode;

	public bool IsTaskOrNote
	{
		get
		{
			if (Type != DisplayType.Task)
			{
				return Type == DisplayType.Note;
			}
			return true;
		}
	}

	public bool IsNote => Type == DisplayType.Note;

	public bool IsTask
	{
		get
		{
			if (Kind != "NOTE")
			{
				return Type == DisplayType.Task;
			}
			return false;
		}
	}

	public bool IsCheckItem => Type == DisplayType.CheckItem;

	public bool IsEvent => Type == DisplayType.Event;

	public bool IsPomo => Type == DisplayType.Pomo;

	public bool IsAgenda => Type == DisplayType.Agenda;

	public bool IsCourse => Type == DisplayType.Course;

	public bool IsHabit => Type == DisplayType.Habit;

	public bool IsCountdown => Type == DisplayType.Countdown;

	public string Id { get; set; }

	public string AttendId { get; set; }

	public string ParentId { get; set; }

	public long PinnedTime { get; set; }

	public bool IsPinned => PinnedTime > 0;

	public long ProjectOrder { get; set; }

	public string Permission { get; set; }

	public string TeamId { get; set; }

	public string TaskRepeatId { get; set; }

	public string CalendarId { get; set; }

	public string ColumnId { get; set; }

	public string ColumnName { get; set; }

	public long ParentOrder { get; set; }

	public bool IsOpen { get; set; }

	public string ChildrenIds { get; set; }

	public bool HasLocation { get; set; }

	public bool IsFloating { get; set; }

	public string[] Tags { get; set; }

	public string ReminderString { get; set; }

	public int? AnnoyingAlert { get; set; }

	public DisplayType Type
	{
		get
		{
			return _type;
		}
		set
		{
			_type = value;
			OnPropertyChanged("Type");
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
			_startDate = (Utils.IsEmptyDate(value) ? ((DateTime?)null) : value);
			OnPropertyChanged("StartDate");
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
			OnPropertyChanged("DueDate");
		}
	}

	public string CompletedUser { get; set; }

	public string Creator { get; set; }

	public DateTime? CompletedTime
	{
		get
		{
			return _completedTime;
		}
		set
		{
			_completedTime = value;
			OnPropertyChanged("CompletedTime");
		}
	}

	public DateTime? ModifiedTime
	{
		get
		{
			return _modifiedTime;
		}
		set
		{
			_modifiedTime = value;
			OnPropertyChanged("ModifiedTime");
		}
	}

	public DateTime? CreatedTime
	{
		get
		{
			return _createdTime;
		}
		set
		{
			_createdTime = value;
			OnPropertyChanged("CreatedTime");
		}
	}

	public DateTime? RemindTime
	{
		get
		{
			return _remindTime;
		}
		set
		{
			_remindTime = value;
			OnPropertyChanged("RemindTime");
		}
	}

	public long SortOrder
	{
		get
		{
			return _sortOrder;
		}
		set
		{
			_sortOrder = value;
			OnPropertyChanged("SortOrder");
			if (Type == DisplayType.CheckItem)
			{
				OwnerTask?.OnPropertyChanged("CheckItemContent");
			}
			CheckItems?.Value.ForEach(delegate(TaskBaseViewModel c)
			{
				c.ParentOrder = value;
			});
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
			SetCheckItemPriority();
			OnPropertyChanged("Priority");
		}
	}

	public int Progress
	{
		get
		{
			return _progress;
		}
		set
		{
			_progress = value;
			OnPropertyChanged("Progress");
		}
	}

	public int Status
	{
		get
		{
			if (Type != DisplayType.Note)
			{
				return _status;
			}
			return 0;
		}
		set
		{
			_status = value;
			OnPropertyChanged("Status");
		}
	}

	public int Deleted
	{
		get
		{
			return _deleted;
		}
		set
		{
			_deleted = value;
			Editable = value == 0 && (CacheManager.GetProjectById(_projectId)?.IsEnable() ?? false);
			OnPropertyChanged("Deleted");
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
				SetProject(value);
				OnPropertyChanged("ProjectId");
			}
		}
	}

	public List<TitleChunk> TitleChunks { get; set; }

	public string Title
	{
		get
		{
			return _title ?? string.Empty;
		}
		set
		{
			_title = value;
			OnPropertyChanged("Title");
			OnPropertyChanged("TitleWithoutLink");
			if (Type == DisplayType.CheckItem)
			{
				OwnerTask?.OnPropertyChanged("CheckItemContent");
			}
			TitleChunks = null;
		}
	}

	public string TitleWithoutLink => TaskUtils.EscapeLinkContent(_title);

	public string Content
	{
		get
		{
			return _content ?? "";
		}
		set
		{
			_content = value;
			OnPropertyChanged("Content");
			OnPropertyChanged("DisplayContent");
		}
	}

	public string DisplayContent
	{
		get
		{
			string text = "";
			if (Kind == "CHECKLIST")
			{
				if (!string.IsNullOrEmpty(Desc))
				{
					return Desc;
				}
				return "- " + TaskDetailItemCache.GetPrimarySubtaskInTask(Id)?.Title;
			}
			return Content;
		}
	}

	public string Desc
	{
		get
		{
			return _desc ?? "";
		}
		set
		{
			_desc = value;
			OnPropertyChanged("Desc");
			OnPropertyChanged("DisplayContent");
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
			OnPropertyChanged("RepeatFlag");
		}
	}

	public string RepeatFrom
	{
		get
		{
			return _repeatFrom;
		}
		set
		{
			_repeatFrom = value;
			OnPropertyChanged("RepeatFrom");
		}
	}

	public string CommentCount
	{
		get
		{
			return _commentCount;
		}
		set
		{
			_commentCount = value;
			OnPropertyChanged("CommentCount");
			NotifyCommentsChanged();
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
			Type = ((!string.IsNullOrEmpty(AttendId)) ? DisplayType.Agenda : ((_kind == "NOTE") ? DisplayType.Note : DisplayType.Task));
			OnPropertyChanged("Kind");
		}
	}

	public string TimeZoneName
	{
		get
		{
			return _timeZoneName;
		}
		set
		{
			_timeZoneName = value;
			OnPropertyChanged("TimeZoneName");
		}
	}

	public string Color
	{
		get
		{
			return _color;
		}
		set
		{
			string text = value;
			if (text != null && (text.Length == 6 || text.Length == 8) && !text.StartsWith("#"))
			{
				text = "#" + text;
			}
			_color = text;
			OnPropertyChanged("Color");
		}
	}

	public string ProjectName
	{
		get
		{
			if (string.IsNullOrEmpty(_projectName))
			{
				_projectName = CacheManager.GetProjectById(ProjectId)?.name;
			}
			return _projectName;
		}
		set
		{
			_projectName = value;
			OnPropertyChanged("ProjectName");
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

	public string Tag
	{
		get
		{
			return _tag;
		}
		set
		{
			_tag = value;
			SetCheckItemTag();
			OnPropertyChanged("Tag");
		}
	}

	public bool Editable
	{
		get
		{
			return _editable;
		}
		set
		{
			if (_editable != value)
			{
				_editable = value;
				OnPropertyChanged("Editable");
			}
		}
	}

	public CalendarAvailableType CalendarAvailableType
	{
		get
		{
			return _calendarAvailableType;
		}
		set
		{
			if (_calendarAvailableType != value)
			{
				_calendarAvailableType = value;
				OnPropertyChanged("CalendarAvailableType");
			}
		}
	}

	public bool? IsAllDay
	{
		get
		{
			return _isAllDay;
		}
		set
		{
			_isAllDay = value;
			OnPropertyChanged("IsAllDay");
		}
	}

	public int ImageMode
	{
		get
		{
			return _imageMode;
		}
		set
		{
			_imageMode = value;
			OnPropertyChanged("ImageMode");
		}
	}

	public string ExDates { get; set; }

	public string EntityId { get; set; }

	public string EventTaskId { get; set; }

	public string Etag { get; set; }

	public int? TaskType { get; set; }

	public ResponseStatusType ResponseStatusType { get; set; }

	public CourseDisplayModel Course { get; set; }

	public int PomoType { get; set; }

	public long PomoDuration { get; set; }

	public bool VirtualContent { get; set; }

	public string NotionBlock { get; set; }

	public void SetProject(string projectId)
	{
		ProjectModel projectById = CacheManager.GetProjectById(projectId);
		if (projectById != null)
		{
			Color = projectById.color;
			ProjectName = (projectById.Isinbox ? Utils.GetString("Inbox") : projectById.name);
			ProjectOrder = projectById.sortOrder;
			Permission = projectById.permission;
			TeamId = projectById.teamId;
			Editable = Deleted == 0 && projectById.IsEnable();
			SetItemsProject();
		}
	}

	public TaskBaseViewModel()
	{
	}

	public TaskBaseViewModel(TaskModel task)
	{
		ProjectModel projectById = CacheManager.GetProjectById(task.projectId);
		if (projectById != null)
		{
			if (projectById.color != null && (projectById.color.Length == 6 || projectById.color.Length == 8) && !projectById.color.StartsWith("#"))
			{
				projectById.color = "#" + projectById.color;
			}
			_color = projectById.color;
			_projectName = (projectById.Isinbox ? Utils.GetString("Inbox") : projectById.name);
			ProjectOrder = projectById.sortOrder;
			Permission = projectById.permission;
			TeamId = projectById.teamId;
		}
		_editable = task.deleted == 0 && projectById != null && projectById.IsEnable();
		Id = task.id;
		SortOrder = task.sortOrder;
		_projectId = task.projectId;
		AttendId = task.attendId;
		ParentId = task.parentId;
		PinnedTime = task.pinnedTimeStamp;
		ColumnId = task.columnId;
		TimeZoneName = task.timeZone;
		IsOpen = task.isOpen;
		ChildrenIds = task.childrenString;
		HasLocation = task.hasLocation == 1;
		IsFloating = task.isFloating == true;
		ExDates = task.exDates;
		_startDate = task.startDate;
		_dueDate = task.dueDate;
		_completedTime = task.completedTime;
		CompletedUser = task.completedUserId;
		_modifiedTime = task.modifiedTime;
		_createdTime = task.createdTime;
		_remindTime = task.remindTime;
		_priority = task.priority;
		_progress = task.progress.GetValueOrDefault();
		_status = task.status;
		_deleted = task.deleted;
		_title = task.title?.Replace("\n", " ");
		Content = task.content;
		ImageMode = task.imgMode;
		_desc = task.desc;
		_repeatFlag = task.repeatFlag;
		_repeatFrom = task.repeatFrom;
		_kind = task.kind;
		_type = ((!string.IsNullOrEmpty(task.attendId)) ? DisplayType.Agenda : ((Kind == "NOTE") ? DisplayType.Note : DisplayType.Task));
		_assignee = task.assignee;
		_tag = task.tag;
		_isAllDay = task.isAllDay;
		_commentCount = task.commentCount;
		Tags = TagSerializer.ToTags(task.tag).ToArray();
		TaskRepeatId = task.repeatTaskId;
		Creator = task.creator;
		Resources = task.Resources;
		Actions = task.Actions;
		Label = task.label;
		Etag = task.etag;
		NotionBlock = task.notionBlock;
		TaskType = task.taskType;
		AnnoyingAlert = task.annoyingAlert;
	}

	public TaskBaseViewModel(TaskDetailItemModel item)
	{
		BuildCheckItem(item, setItem: true);
	}

	public void RemoveParent()
	{
	}

	public TaskBaseViewModel Copy()
	{
		return (TaskBaseViewModel)MemberwiseClone();
	}

	public bool IsAssignToMe()
	{
		return Assignee == LocalSettings.Settings.LoginUserId;
	}

	public void SetProperties(TaskBaseViewModel model)
	{
		PropertyInfo[] properties = GetType().GetProperties();
		foreach (PropertyInfo propertyInfo in properties)
		{
			object value = propertyInfo.GetValue(this);
			object value2 = propertyInfo.GetValue(model);
			if ((value != null || value2 != null) && (value == null || value2 == null || !value.Equals(value2)) && propertyInfo.GetSetMethod() != null)
			{
				propertyInfo.SetValue(this, value2);
			}
		}
	}

	public TaskBaseViewModel(CourseDisplayModel course)
	{
		Id = course.UniqueId;
		_type = DisplayType.Course;
		Course = course;
		Title = course.Title;
		Content = course.Room + (string.IsNullOrEmpty(course.Room) ? "" : " ") + course.Teacher;
		IsAllDay = false;
		StartDate = course.CourseStart;
		DueDate = course.CourseEnd;
		_projectId = course.ScheduleId;
		_projectName = course.ScheduleName;
		_color = course.Color;
		ReminderString = ((course.Reminders == null) ? null : JsonConvert.SerializeObject(course.Reminders));
	}

	public TaskBaseViewModel(CalendarEventModel model, string id = null)
	{
		Id = id ?? model.Id;
		EntityId = model.Id;
		_type = DisplayType.Event;
		Title = model.Title?.Replace("\n", " ");
		Content = model.Content;
		IsAllDay = model.IsAllDay;
		StartDate = model.DueStart;
		DueDate = model.DueEnd;
		_projectId = "8ac3038d93c54b80a67321b6a03df066";
		RepeatFlag = model.RepeatFlag;
		_repeatFrom = "0";
		CalendarId = model.CalendarId;
		ReminderString = model.Reminders;
		Editable = CacheManager.GetBindCalendarById(model.CalendarId)?.Accessible ?? false;
		EventTaskId = model.TickTaskId;
		ResponseStatusType = model.ResponseStatusType;
		CalendarAvailableType = model.AvailableType;
	}

	public TaskBaseViewModel(Section section)
	{
		_title = section.Name;
		Id = section.SectionId;
		_type = DisplayType.Section;
		_projectId = section.ProjectId;
		_sortOrder = section.Ordinal;
	}

	public async Task<TaskModel> ToTask()
	{
		if (!IsTaskOrNote)
		{
			return null;
		}
		TaskModel taskModel = await TaskDao.GetTaskById(Id);
		if (taskModel == null)
		{
			return null;
		}
		taskModel.sortOrder = SortOrder;
		taskModel.projectId = ProjectId;
		taskModel.attendId = AttendId;
		taskModel.parentId = ParentId;
		taskModel.pinnedTimeStamp = PinnedTime;
		taskModel.columnId = ColumnId;
		taskModel.timeZone = TimeZoneName;
		taskModel.isOpen = IsOpen;
		taskModel.childrenString = ChildrenIds;
		taskModel.isFloating = IsFloating;
		taskModel.startDate = _startDate;
		taskModel.dueDate = _dueDate;
		taskModel.completedTime = _completedTime;
		taskModel.completedUserId = CompletedUser;
		taskModel.modifiedTime = _modifiedTime;
		taskModel.remindTime = _remindTime;
		taskModel.priority = _priority;
		taskModel.progress = _progress;
		taskModel.status = _status;
		taskModel.deleted = _deleted;
		taskModel.title = _title;
		taskModel.content = Content;
		taskModel.desc = _desc;
		taskModel.repeatFlag = _repeatFlag;
		taskModel.repeatFrom = _repeatFrom;
		taskModel.kind = _kind;
		taskModel.assignee = _assignee;
		taskModel.tag = _tag;
		taskModel.isAllDay = _isAllDay;
		taskModel.annoyingAlert = AnnoyingAlert;
		return taskModel;
	}

	public TaskModel CovertToNewTask()
	{
		return new TaskModel
		{
			id = Id,
			sortOrder = SortOrder,
			projectId = ProjectId,
			attendId = AttendId,
			parentId = ParentId,
			pinnedTimeStamp = PinnedTime,
			columnId = ColumnId,
			timeZone = TimeZoneName,
			isOpen = IsOpen,
			childrenString = ChildrenIds,
			isFloating = IsFloating,
			startDate = _startDate,
			dueDate = _dueDate,
			completedTime = _completedTime,
			completedUserId = CompletedUser,
			modifiedTime = _modifiedTime,
			remindTime = _remindTime,
			priority = _priority,
			progress = _progress,
			status = _status,
			deleted = _deleted,
			title = _title,
			content = Content,
			desc = _desc,
			repeatFlag = _repeatFlag,
			repeatFrom = _repeatFrom,
			kind = _kind,
			assignee = _assignee,
			tag = _tag,
			isAllDay = _isAllDay,
			annoyingAlert = AnnoyingAlert
		};
	}

	public string GetTaskId()
	{
		if (!IsCheckItem)
		{
			return Id;
		}
		return ParentId;
	}

	public string GetAttendId()
	{
		return AttendId;
	}

	public void BuildCheckItem(TaskDetailItemModel item, bool setItem = false)
	{
		Id = item.id;
		Type = DisplayType.CheckItem;
		SortOrder = item.sortOrder;
		ParentId = item.TaskServerId;
		StartDate = item.startDate;
		CompletedTime = item.completedTime;
		Status = item.status;
		Title = item.title?.Replace("\n", " ");
		IsAllDay = item.isAllDay;
		RemindTime = item.snoozeReminderTime;
		SetOwnerTask(item.TaskServerId, setItem);
	}

	public void SetOwnerTask(string taskId, bool setItem)
	{
		TaskBaseViewModel taskById = TaskCache.GetTaskById(taskId);
		if (taskById != null)
		{
			Color = taskById._color;
			ProjectName = taskById._projectName;
			ProjectId = taskById._projectId;
			ProjectOrder = taskById.ProjectOrder;
			Permission = taskById.Permission;
			TeamId = taskById.TeamId;
			ColumnId = taskById.ColumnId;
			Editable = taskById.Editable;
			Priority = taskById._priority;
			Tags = taskById.Tags;
			Tag = taskById._tag;
			ParentOrder = taskById.SortOrder;
			if (!CreatedTime.HasValue)
			{
				CreatedTime = taskById.CreatedTime;
			}
			if (setItem)
			{
				OwnerTask = taskById;
				taskById.AddItems(this);
			}
			else
			{
				OwnerTask?.CheckItem(this);
			}
		}
	}

	private void CheckItem(TaskBaseViewModel item)
	{
		if (item != null)
		{
			CheckItems.RemoveAll((TaskBaseViewModel c) => c.Id == item.Id && !c.Equals(item));
			if (CheckItems?.FirstOrDefault((TaskBaseViewModel c) => c.Id == item.Id) == null)
			{
				CheckItems?.Add(item);
				OnPropertyChanged("CheckItemContent");
				OnPropertyChanged("CheckItems");
			}
		}
	}

	private void AddItems(TaskBaseViewModel item)
	{
		if (item != null)
		{
			if (CheckItems == null)
			{
				CheckItems = new BlockingList<TaskBaseViewModel>();
			}
			CheckItems.RemoveAll((TaskBaseViewModel c) => c.Id == item.Id && !c.Equals(item));
			if (CheckItems.All((TaskBaseViewModel i) => i.Id != item.Id))
			{
				CheckItems.Add(item);
				OnPropertyChanged("CheckItemContent");
				OnPropertyChanged("CheckItems");
			}
		}
	}

	public void RemoveCheckItem(TaskBaseViewModel value)
	{
		if (CheckItems != null)
		{
			CheckItems.Remove(value);
			OnPropertyChanged("CheckItemContent");
			OnPropertyChanged("CheckItems");
		}
	}

	private void SetItemsProject()
	{
		if (CheckItems == null)
		{
			return;
		}
		foreach (TaskBaseViewModel item in CheckItems.Value)
		{
			item.Color = _color;
			item.ProjectName = _projectName;
			item.ProjectId = _projectId;
			item.ProjectOrder = ProjectOrder;
			item.Permission = Permission;
			item.TeamId = TeamId;
			item.Editable = Editable;
		}
	}

	private void SetCheckItemTag()
	{
		if (CheckItems == null)
		{
			return;
		}
		foreach (TaskBaseViewModel item in CheckItems.Value)
		{
			item.Tags = Tags;
			item.Tag = _tag;
		}
	}

	private void SetCheckItemPriority()
	{
		if (CheckItems == null)
		{
			return;
		}
		foreach (TaskBaseViewModel item in CheckItems.Value)
		{
			item.Priority = _priority;
		}
	}

	public void SetTags(List<string> tags)
	{
		if (tags != null && tags.Count > 0)
		{
			Tags = tags.Select((string t) => t.ToLower()).ToArray();
			Tag = TagSerializer.ToJsonContent(tags);
		}
		else
		{
			Tags = new string[0];
			Tag = null;
		}
		OnPropertyChanged("Tags");
	}

	public void CheckCheckItems(List<TaskDetailItemModel> items)
	{
		if (CheckItems == null)
		{
			CheckItems = new BlockingList<TaskBaseViewModel>();
		}
		if (items == null)
		{
			return;
		}
		List<TaskBaseViewModel> list = CheckItems.Where((TaskBaseViewModel c) => items.All((TaskDetailItemModel i) => i.id != c.Id));
		List<TaskDetailItemModel> list2 = items.Where((TaskDetailItemModel i) => CheckItems.ToList().All((TaskBaseViewModel c) => i.id != c.Id)).ToList();
		foreach (TaskBaseViewModel item in list)
		{
			CheckItems.Remove(item);
			TaskDetailItemCache.DeleteCheckItemById(item.Id);
		}
		foreach (TaskDetailItemModel item2 in list2)
		{
			TaskBaseViewModel checkItemById = TaskDetailItemCache.GetCheckItemById(item2?.id);
			if (checkItemById == null)
			{
				TaskDetailItemCache.AddCheckItemToDict(item2);
			}
			else
			{
				CheckItems.Add(checkItemById);
			}
		}
	}

	public void NotifyCommentsChanged()
	{
		OnPropertyChanged("Comments");
	}

	public string GetAssignee()
	{
		if (!string.IsNullOrEmpty(Assignee))
		{
			return Assignee;
		}
		return "-1";
	}

	internal bool InToday()
	{
		if (!StartDate.HasValue)
		{
			return false;
		}
		if (!DueDate.HasValue)
		{
			return StartDate.Value.Date == DateTime.Today;
		}
		if (StartDate.Value.Date <= DateTime.Today)
		{
			return DueDate.Value.Date >= DateTime.Today;
		}
		return false;
	}

	public bool OutDate()
	{
		if (!StartDate.HasValue)
		{
			return false;
		}
		if (!DueDate.HasValue)
		{
			return StartDate.Value.Date < DateTime.Today;
		}
		return DueDate.Value <= DateTime.Today;
	}

	public void SetDependenceModel(TaskBaseViewModel displayTask, params string[] properties)
	{
		if (displayTask == null || properties == null || properties.Length == 0)
		{
			return;
		}
		foreach (string propertyName in properties)
		{
			PropertyChangedEventManager.AddHandler(displayTask, delegate(object o, PropertyChangedEventArgs e)
			{
				PropertyInfo property = GetType().GetProperty(e.PropertyName);
				if (property != null)
				{
					property.SetValue(this, property.GetValue(o));
				}
			}, propertyName);
		}
	}

	public bool IsPropertiesEqual(TaskBaseViewModel model)
	{
		if (model == null)
		{
			return false;
		}
		if (_propertyCheckCode == 0)
		{
			string[] obj = new string[8] { Id, null, null, null, null, null, null, null };
			DateTime? startDate = _startDate;
			obj[1] = startDate.ToString();
			startDate = _dueDate;
			obj[2] = startDate.ToString();
			bool? isAllDay = _isAllDay;
			obj[3] = isAllDay.ToString();
			obj[4] = _timeZoneName;
			obj[5] = _repeatFlag;
			obj[6] = _repeatFrom;
			obj[7] = _title;
			_propertyCheckCode = string.Concat(obj).GetHashCode();
		}
		if (model._propertyCheckCode == 0)
		{
			string[] obj2 = new string[8] { model.Id, null, null, null, null, null, null, null };
			DateTime? startDate = model._startDate;
			obj2[1] = startDate.ToString();
			startDate = model._dueDate;
			obj2[2] = startDate.ToString();
			bool? isAllDay = model._isAllDay;
			obj2[3] = isAllDay.ToString();
			obj2[4] = model._timeZoneName;
			obj2[5] = model._repeatFlag;
			obj2[6] = model._repeatFrom;
			obj2[7] = model._title;
			model._propertyCheckCode = string.Concat(obj2).GetHashCode();
		}
		return _propertyCheckCode == model._propertyCheckCode;
	}

	public List<TitleChunk> GetTitleNum()
	{
		if (TitleChunks == null)
		{
			TitleChunks = Title?.CompareNumber();
		}
		return TitleChunks;
	}

	private async Task<string> TryCopy(Window owner)
	{
		if (IsCheckItem)
		{
			return (await TaskDetailItemDao.CopyItem(Id)).id;
		}
		if (IsTaskOrNote)
		{
			return (await TaskService.CopyTask(GetTaskId(), owner, notify: true, delayNotify: true)).id;
		}
		return null;
	}

	public async Task SetDropDate(DateTime date, Window window = null, bool toAllDay = false)
	{
		string id = Id;
		if (Utils.IfCtrlPressed())
		{
			id = await TryCopy(window);
			if (string.IsNullOrEmpty(id))
			{
				id = Id;
			}
		}
		if (toAllDay)
		{
			TimeSpan? timeSpan = _dueDate - _startDate;
			if (timeSpan.HasValue && timeSpan.GetValueOrDefault().TotalHours > 24.0)
			{
				toAllDay = false;
			}
		}
		if (IsTaskOrNote)
		{
			bool needSetReminder = !StartDate.HasValue;
			await TaskService.SetDate(id, date, handleReminder: false, "", toAllDay || (IsAllDay ?? true));
			if (needSetReminder)
			{
				await TaskService.SaveTaskReminders(new TaskModel
				{
					id = id,
					reminders = ((IsAllDay ?? true) ? TimeData.GetDefaultAllDayReminders().ToArray() : TimeData.GetDefaultTimeReminders().ToArray())
				});
			}
		}
		if (IsCheckItem)
		{
			TimeDataModel newDate = new TimeDataModel
			{
				TaskId = GetTaskId(),
				ItemId = id,
				IsAllDay = (toAllDay || Utils.IsEmptyDate(StartDate) || (IsAllDay ?? true)),
				StartDate = ((!StartDate.HasValue || toAllDay) ? date : DateUtils.SetDateOnly(StartDate.Value, date)),
				DueDate = null
			};
			await TaskService.SetCheckItemDate(GetTaskId(), id, newDate);
		}
	}

	public async Task SetDropTimeSpan(DateTime? start, DateTime? end, Window window = null)
	{
		string id = Id;
		if (Utils.IfCtrlPressed())
		{
			id = await TryCopy(window);
			if (string.IsNullOrEmpty(id))
			{
				id = Id;
			}
		}
		if (IsTaskOrNote)
		{
			bool needSetReminder = !StartDate.HasValue || (IsAllDay.HasValue && (IsAllDay ?? true));
			await TaskService.SetDateSpan(id, start, end, needSetReminder, false);
			if (needSetReminder)
			{
				await TaskService.SaveTaskReminders(new TaskModel
				{
					id = id,
					reminders = ((IsAllDay ?? true) ? TimeData.GetDefaultAllDayReminders().ToArray() : TimeData.GetDefaultTimeReminders().ToArray())
				});
			}
		}
		if (IsCheckItem)
		{
			TimeDataModel newDate = new TimeDataModel
			{
				TaskId = GetTaskId(),
				ItemId = id,
				IsAllDay = false,
				StartDate = start,
				DueDate = null
			};
			await TaskService.SetCheckItemDate(GetTaskId(), id, newDate);
		}
	}

	public async Task SetIdentityProperties(ProjectIdentity identity, IToastShowWindow toastWindow, bool changeDate = true)
	{
		if (!(identity is NormalProjectIdentity proj))
		{
			if (identity is FilterProjectIdentity filterProjectIdentity)
			{
				await TaskService.BatchUpdateTaskQuadrantProperties(new List<string> { Id }, "", filterProjectIdentity.Filter.rule, toastWindow);
			}
			else if (!(identity is TodayProjectIdentity))
			{
				if (!(identity is TomorrowProjectIdentity))
				{
					if (!(identity is WeekProjectIdentity))
					{
						if (!(identity is TagProjectIdentity tagProjectIdentity))
						{
							if (identity is GroupProjectIdentity groupProjectIdentity)
							{
								ProjectModel projectById = CacheManager.GetProjectById(ProjectId);
								if (projectById == null || projectById.groupId != groupProjectIdentity.Id)
								{
									await TaskService.BatchMoveParentTaskProject(new List<string> { Id }, groupProjectIdentity.DefaultProject);
								}
							}
						}
						else if (tagProjectIdentity.SortOption.groupByType != Constants.SortType.tag && TaskViewModelHelper.GetMatchedTasks(tagProjectIdentity, new List<string> { Id }).Count == 0)
						{
							await TaskService.BatchAddTag(new List<string> { Id }, tagProjectIdentity.Tag);
						}
					}
					else if (changeDate && (!StartDate.HasValue || StartDate.Value >= DateTime.Today.AddDays(7.0)))
					{
						await SetDropDate(DateTime.Today.AddDays(1.0));
					}
				}
				else if (changeDate && (!StartDate.HasValue || (StartDate.Value.Date < DateTime.Today.AddDays(1.0) && (!DueDate.HasValue || DueDate.Value.Date < DateTime.Today.AddDays(1.0))) || StartDate.Value >= DateTime.Today.AddDays(2.0)))
				{
					await SetDropDate(DateTime.Today.AddDays(1.0));
				}
			}
			else if (changeDate && (!StartDate.HasValue || StartDate.Value >= DateTime.Today.AddDays(1.0)))
			{
				await SetDropDate(DateTime.Today);
			}
		}
		else if (ProjectId != proj.Id)
		{
			string text = ((!KanbanHelper.IsProjectInKanbanMode(proj.Id)) ? null : (await ColumnDao.GetProjectDefaultColumnId(proj.Id)));
			string columnId = text;
			await TaskService.BatchMoveParentTaskProject(new List<string> { Id }, proj.Project, keepAssignee: false, columnId);
		}
		TaskChangeNotifier.NotifyTaskBatchChanged(new List<string> { Id });
	}
}
