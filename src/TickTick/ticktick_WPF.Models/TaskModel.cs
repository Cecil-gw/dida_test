using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Newtonsoft.Json;
using SQLite;
using TickTickModels;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Sync;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Tag;

namespace ticktick_WPF.Models;

[Serializable]
public class TaskModel : BaseModel, AgendaHelper.IAgenda
{
	private string _projectId;

	private string _completedUserId;

	private string _columnId;

	private string _userId;

	private DateTime? _startDate;

	private DateTime? _dueDate;

	private string _kind;

	private string _timeZone;

	private string[] _tags;

	[Indexed]
	public string id { get; set; }

	[Indexed]
	public string projectId
	{
		get
		{
			return _projectId;
		}
		set
		{
			_projectId = StringPool.GetOrCreate(value);
		}
	}

	public int imgMode { get; set; }

	public string parentId { get; set; }

	public int priority { get; set; }

	public string title { get; set; }

	public string content { get; set; }

	public bool? isAllDay { get; set; }

	public string repeatFlag { get; set; }

	public string reminder { get; set; }

	public string completedUserId
	{
		get
		{
			return _completedUserId;
		}
		set
		{
			_completedUserId = StringPool.GetOrCreate(value);
		}
	}

	public string repeatTaskId { get; set; }

	public int? progress { get; set; }

	public string etag { get; set; }

	public string attendId { get; set; }

	public int hasLocation { get; set; }

	public string columnId
	{
		get
		{
			return _columnId;
		}
		set
		{
			_columnId = StringPool.GetOrCreate(value);
		}
	}

	[Ignore]
	public string[] exDate { get; set; }

	[JsonIgnore]
	public string exDates { get; set; }

	[Ignore]
	public LocationModel location { get; set; }

	public string creator { get; set; }

	public string repeatFrom { get; set; }

	[JsonProperty("attachments")]
	[Ignore]
	public AttachmentModel[] Attachments { get; set; }

	[JsonProperty("focusSummaries")]
	[Ignore]
	public PomodoroSummaryModel[] FocusSummaries { get; set; }

	public string commentCount { get; set; }

	public string assignee { get; set; }

	public string userId
	{
		get
		{
			return _userId;
		}
		set
		{
			_userId = StringPool.GetOrCreate(value);
		}
	}

	[JsonIgnore]
	public string tag { get; set; }

	[Ignore]
	public TaskDetailItemModel[] items { get; set; }

	[Ignore]
	public TaskReminderModel[] reminders { get; set; }

	[Ignore]
	private DateTime? CompletedTime { get; set; }

	[JsonConverter(typeof(UtcDateTimeConverter))]
	public DateTime? completedTime
	{
		get
		{
			if (!Utils.IsEmptyDate(CompletedTime))
			{
				return CompletedTime;
			}
			return null;
		}
		set
		{
			CompletedTime = value;
		}
	}

	[Ignore]
	private DateTime? CreatedTime { get; set; }

	[JsonConverter(typeof(UtcDateTimeConverter))]
	public DateTime? createdTime
	{
		get
		{
			if (!Utils.IsEmptyDate(CreatedTime))
			{
				return CreatedTime;
			}
			return null;
		}
		set
		{
			CreatedTime = value;
		}
	}

	[JsonConverter(typeof(UtcDateTimeConverter))]
	public DateTime? startDate
	{
		get
		{
			if (!Utils.IsEmptyDate(_startDate))
			{
				return _startDate;
			}
			return null;
		}
		set
		{
			_startDate = (Utils.IsEmptyDate(value) ? ((DateTime?)null) : value);
		}
	}

	[Ignore]
	private DateTime? RepeatFirstDate { get; set; }

	[JsonConverter(typeof(UtcDateTimeConverter))]
	public DateTime? repeatFirstDate
	{
		get
		{
			if (!Utils.IsEmptyDate(RepeatFirstDate))
			{
				return RepeatFirstDate;
			}
			return null;
		}
		set
		{
			RepeatFirstDate = value;
		}
	}

	[Ignore]
	private DateTime? ModifiedTime { get; set; }

	[JsonConverter(typeof(UtcDateTimeConverter))]
	public DateTime? modifiedTime
	{
		get
		{
			if (!Utils.IsEmptyDate(ModifiedTime))
			{
				return ModifiedTime;
			}
			return DateTime.Now;
		}
		set
		{
			ModifiedTime = value;
		}
	}

	[Ignore]
	private DateTime? RemindTime { get; set; }

	[JsonConverter(typeof(UtcDateTimeConverter))]
	public DateTime? remindTime
	{
		get
		{
			if (!Utils.IsEmptyDate(RemindTime))
			{
				return RemindTime;
			}
			return null;
		}
		set
		{
			RemindTime = value;
		}
	}

	[JsonConverter(typeof(UtcDateTimeConverter))]
	public DateTime? dueDate
	{
		get
		{
			if (!Utils.IsEmptyDate(_dueDate))
			{
				return _dueDate;
			}
			return null;
		}
		set
		{
			_dueDate = (Utils.IsEmptyDate(value) ? ((DateTime?)null) : value);
		}
	}

	public int deleted { get; set; }

	public int status { get; set; }

	[Ignore]
	[JsonIgnore]
	public bool isAbandoned => status == -1;

	[Ignore]
	[JsonIgnore]
	public bool isCompleted
	{
		get
		{
			if (status != 0)
			{
				return !isAbandoned;
			}
			return false;
		}
	}

	public string kind
	{
		get
		{
			return _kind ?? "TEXT";
		}
		set
		{
			switch (value)
			{
			case null:
			case "TEXT":
				_kind = "TEXT";
				break;
			case "CHECKLIST":
				_kind = "CHECKLIST";
				break;
			default:
				_kind = value;
				break;
			}
		}
	}

	[Ignore]
	public string Color { get; set; }

	public long sortOrder { get; set; }

	public string desc { get; set; }

	public string timeZone
	{
		get
		{
			if (!string.IsNullOrEmpty(_timeZone))
			{
				return _timeZone;
			}
			return Utils.GetLocalTimeZone();
		}
		set
		{
			_timeZone = ((value == Utils.GetLocalTimeZone()) ? Utils.GetLocalTimeZone() : value);
		}
	}

	public int attachmentCount { get; set; }

	public int reminderCount { get; set; }

	[Ignore]
	public string[] tags
	{
		get
		{
			return _tags;
		}
		set
		{
			string[] second = TagSerializer.NormalizeNames(value)?.ToArray();
			TagsNeedSync |= value != null && !value.SequenceEqual(second);
			_tags = second;
		}
	}

	[Ignore]
	[JsonIgnore]
	public bool TagsNeedSync { get; set; }

	public bool? isFloating { get; set; } = false;

	[Ignore]
	[JsonIgnore]
	public bool Floating => isFloating == true;

	[JsonIgnore]
	public string childrenString { get; set; }

	[Ignore]
	public List<string> childIds { get; set; }

	[Ignore]
	public List<TaskModel> children { get; set; }

	[NotNull]
	[DefaultValue("1")]
	[JsonIgnore]
	public bool isOpen { get; set; } = true;

	public string pinnedTime { get; set; }

	[JsonIgnore]
	[Ignore]
	public long pinnedTimeStamp
	{
		get
		{
			if (string.IsNullOrEmpty(pinnedTime))
			{
				return 0L;
			}
			if (pinnedTime == "-1")
			{
				return -1L;
			}
			if (DateTime.TryParse(pinnedTime, out var result))
			{
				if (result.Year <= 2000)
				{
					return 0L;
				}
				return Utils.GetTimeStampInSecond(result);
			}
			return 0L;
		}
		set
		{
			if (value < 0)
			{
				pinnedTime = "-1";
				return;
			}
			if (value == 0L)
			{
				pinnedTime = null;
				return;
			}
			DateTime date = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1)).AddSeconds(value);
			pinnedTime = date.ToString(UtcDateTimeConverter.GetConverterValue(date));
		}
	}

	[JsonIgnore]
	[Ignore]
	public bool ParseTz
	{
		get
		{
			if (!(isAllDay ?? true))
			{
				return isFloating != true;
			}
			return false;
		}
	}

	[JsonIgnore]
	public string Actions { get; set; }

	[JsonIgnore]
	public string Resources { get; set; }

	[JsonIgnore]
	public string label { get; set; }

	public string notionBlock { get; set; }

	public int? annoyingAlert { get; set; }

	public int? taskType { get; set; }

	public TaskModel()
	{
	}

	public TaskModel(TaskModel task)
	{
		base._Id = task._Id;
		id = task.id;
		projectId = task.projectId;
		priority = task.priority;
		title = task.title;
		content = task.content;
		isAllDay = task.isAllDay;
		repeatFlag = task.repeatFlag;
		completedTime = task.completedTime;
		createdTime = task.createdTime;
		deleted = task.deleted;
		startDate = task.startDate;
		dueDate = task.dueDate;
		status = task.status;
		kind = task.kind;
		Color = task.Color;
		sortOrder = task.sortOrder;
		desc = task.desc;
		timeZone = task.timeZone;
		reminder = task.reminder;
		repeatFirstDate = task.repeatFirstDate;
		completedUserId = task.completedUserId;
		repeatTaskId = task.repeatTaskId;
		progress = task.progress;
		modifiedTime = task.modifiedTime;
		etag = task.etag;
		remindTime = task.remindTime;
		location = task.location;
		repeatFrom = task.repeatFrom;
		Attachments = task.Attachments;
		commentCount = task.commentCount;
		assignee = task.assignee;
		userId = task.userId;
		items = task.items;
		reminders = task.reminders;
		tag = task.tag;
		exDates = task.exDates;
		exDate = task.exDate;
		columnId = task.columnId;
		isFloating = task.Floating;
		parentId = task.parentId;
		pinnedTime = task.pinnedTime;
		creator = LocalSettings.Settings.LoginUserId;
		notionBlock = task.notionBlock;
		annoyingAlert = task.annoyingAlert;
	}

	public string GetTaskId()
	{
		return id;
	}

	public string GetAttendId()
	{
		return attendId;
	}

	public TaskModel Clone()
	{
		TaskModel taskModel = new TaskModel
		{
			_Id = base._Id,
			id = id,
			projectId = projectId,
			priority = priority,
			title = title,
			content = content,
			isAllDay = isAllDay,
			repeatFlag = repeatFlag,
			completedTime = completedTime,
			createdTime = createdTime,
			startDate = startDate,
			dueDate = dueDate,
			status = status,
			kind = kind,
			Color = Color,
			sortOrder = sortOrder,
			desc = desc,
			timeZone = timeZone,
			reminder = reminder,
			repeatFirstDate = repeatFirstDate,
			completedUserId = completedUserId,
			remindTime = remindTime,
			etag = etag,
			progress = progress,
			modifiedTime = modifiedTime,
			location = location,
			repeatFrom = repeatFrom,
			commentCount = commentCount,
			tag = tag,
			exDate = exDate,
			exDates = exDates,
			columnId = columnId,
			isFloating = isFloating,
			parentId = parentId,
			isOpen = isOpen,
			pinnedTime = pinnedTime,
			imgMode = imgMode,
			annoyingAlert = annoyingAlert
		};
		if (items != null && items.Length != 0)
		{
			TaskDetailItemModel[] array = new TaskDetailItemModel[items.Length];
			int num = 0;
			TaskDetailItemModel[] array2 = items;
			foreach (TaskDetailItemModel taskDetailItemModel in array2)
			{
				TaskDetailItemModel taskDetailItemModel2 = new TaskDetailItemModel
				{
					id = taskDetailItemModel.id,
					sortOrder = taskDetailItemModel.sortOrder,
					TaskId = taskDetailItemModel.TaskId,
					TaskServerId = taskDetailItemModel.TaskServerId
				};
				array[num++] = taskDetailItemModel2;
			}
			taskModel.items = array;
		}
		if (reminders != null && reminders.Length != 0)
		{
			TaskReminderModel[] array3 = new TaskReminderModel[reminders.Length];
			int num2 = 0;
			TaskReminderModel[] array4 = reminders;
			foreach (TaskReminderModel taskReminderModel in array4)
			{
				TaskReminderModel taskReminderModel2 = new TaskReminderModel
				{
					_Id = taskReminderModel._Id,
					id = taskReminderModel.id,
					Taskid = taskReminderModel.Taskid,
					taskserverid = taskReminderModel.taskserverid,
					trigger = taskReminderModel.trigger
				};
				array3[num2++] = taskReminderModel2;
			}
			taskModel.reminders = array3;
		}
		taskModel.assignee = assignee;
		taskModel.userId = userId;
		return taskModel;
	}

	public string GetProjectId()
	{
		return projectId;
	}

	public TaskModel Copy()
	{
		return (TaskModel)MemberwiseClone();
	}

	public bool IsAssignToMe()
	{
		return assignee == LocalSettings.Settings.LoginUserId;
	}

	public void AddPinnedSecond()
	{
		long num = pinnedTimeStamp;
		if (num > 0)
		{
			pinnedTimeStamp = num + 1;
		}
	}

	public bool CheckEnable(bool defaultValue = false)
	{
		return ProjectAndTaskIdsCache.GetProjectById(projectId)?.IsEnable() ?? defaultValue;
	}

	public TimeData GetTimeData()
	{
		return new TimeData
		{
			StartDate = startDate,
			DueDate = dueDate,
			IsAllDay = (!startDate.HasValue || (isAllDay ?? true)),
			RepeatFrom = repeatFrom,
			RepeatFlag = repeatFlag,
			ExDates = ((exDate != null) ? exDate.ToList() : new List<string>()),
			AnnoyingAlert = annoyingAlert
		};
	}

	public void GetStringFromArray()
	{
		if (tags != null)
		{
			tag = TagSerializer.ToJsonContent(tags.ToList());
		}
		if (exDate != null)
		{
			exDates = ExDateSerilizer.ToString(exDate);
		}
		if (childIds != null)
		{
			childrenString = JsonConvert.SerializeObject(childIds);
		}
	}
}
