using System;
using System.Collections.Generic;
using System.ComponentModel;
using Newtonsoft.Json;
using SQLite;
using TickTickModels;
using ticktick_WPF.Cache;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Twitter;

namespace ticktick_WPF.Models;

[Serializable]
public class ProjectModel : BaseModel, IDroppable, ITimeline
{
	private string _color;

	private string _notificationString;

	private string _sync_status;

	[Indexed]
	public string id { get; set; }

	public string userid { get; set; }

	public string name { get; set; } = string.Empty;

	public bool Isinbox { get; set; }

	public bool Ishide { get; set; }

	public bool muted { get; set; }

	public int? reminderType { get; set; }

	[Ignore]
	[JsonIgnore]
	public ShowReminderType ReminderTypeEnum
	{
		get
		{
			if (!reminderType.HasValue)
			{
				return ShowReminderType.None;
			}
			return (ShowReminderType)reminderType.Value;
		}
		set
		{
			reminderType = ((value != ShowReminderType.None) ? new int?((int)value) : ((int?)null));
			if (value != ShowReminderType.None)
			{
				muted = value != ShowReminderType.All;
			}
		}
	}

	[Ignore]
	[JsonIgnore]
	public ShowReminderType ReminderTypeState => CombineOldNewState(!muted, ReminderTypeEnum);

	public string permission { get; set; }

	public string sortType { get; set; }

	public bool? transferred { get; set; }

	[JsonIgnore]
	public bool needPullRemote { get; set; }

	[JsonProperty("sortOption")]
	[Ignore]
	public SortOption SortOption { get; set; }

	[JsonIgnore]
	public string SortOptionString
	{
		get
		{
			if (SortOption != null)
			{
				return JsonConvert.SerializeObject(SortOption);
			}
			return string.Empty;
		}
		set
		{
			SortOption = (string.IsNullOrEmpty(value) ? null : JsonConvert.DeserializeObject<SortOption>(value));
		}
	}

	[JsonProperty("timeline")]
	[Ignore]
	public TimelineSyncModel SyncTimeline { get; set; }

	[JsonIgnore]
	public string SyncTimelineModel
	{
		get
		{
			if (SyncTimeline != null)
			{
				return JsonConvert.SerializeObject(SyncTimeline);
			}
			return string.Empty;
		}
		set
		{
			SyncTimeline = (string.IsNullOrEmpty(value) ? new TimelineSyncModel() : JsonConvert.DeserializeObject<TimelineSyncModel>(value));
		}
	}

	[JsonIgnore]
	public string ParallelViewMode { get; set; }

	[Ignore]
	[JsonIgnore]
	public TimelineModel Timeline { get; set; }

	[JsonIgnore]
	public string TimelineModel
	{
		get
		{
			if (Timeline != null)
			{
				return JsonConvert.SerializeObject(Timeline);
			}
			return string.Empty;
		}
		set
		{
			Timeline = (string.IsNullOrEmpty(value) ? new TimelineModel() : JsonConvert.DeserializeObject<TimelineModel>(value));
		}
	}

	public string color
	{
		get
		{
			return _color;
		}
		set
		{
			_color = value;
		}
	}

	[JsonIgnore]
	public string NotificationString
	{
		get
		{
			return _notificationString;
		}
		set
		{
			_notificationString = value;
		}
	}

	[Ignore]
	public string[] notificationOptions
	{
		get
		{
			return _notificationString?.Split(';');
		}
		set
		{
			if (value != null)
			{
				_notificationString = ((value.Length == 0) ? string.Empty : ((value.Length > 1) ? value.Join(";") : value[0]));
			}
		}
	}

	[Indexed]
	public string sync_status
	{
		get
		{
			return _sync_status;
		}
		set
		{
			if ((!IsNew() || !(value == Constants.SyncStatus.SYNC_UPDATE.ToString())) && (!(_sync_status != Constants.SyncStatus.SYNC_DONE.ToString()) || !(value == Constants.SyncStatus.SYNC_INIT.ToString())))
			{
				_sync_status = value;
			}
		}
	}

	[Indexed]
	public bool delete_status { get; set; }

	public bool isOwner { get; set; } = true;

	public bool inAll { get; set; } = true;

	public int? showType { get; set; }

	[Ignore]
	[JsonIgnore]
	public ShowReminderType ShowTypeEnum
	{
		get
		{
			if (!showType.HasValue)
			{
				return ShowReminderType.None;
			}
			return (ShowReminderType)showType.Value;
		}
		set
		{
			showType = ((value != ShowReminderType.None) ? new int?((int)value) : ((int?)null));
			if (value != ShowReminderType.None)
			{
				inAll = value == ShowReminderType.All;
			}
		}
	}

	[Ignore]
	[JsonIgnore]
	public ShowReminderType ShowTypeState => CombineOldNewState(inAll, ShowTypeEnum);

	public bool? openToTeam { get; set; } = false;

	public bool? needAudit { get; set; } = true;

	public string teamMemberPermission { get; set; } = "write";

	public long sortOrder { get; set; }

	public int userCount { get; set; } = 1;

	public int source { get; set; } = 1;

	public string etag { get; set; }

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

	public bool? closed { get; set; }

	public string groupId { get; set; }

	public string viewMode { get; set; }

	public string teamId { get; set; }

	public string kind { get; set; }

	[JsonIgnore]
	[DefaultValue("1")]
	public bool ShowAddColumn { get; set; } = true;

	[Ignore]
	public bool CanDrop => IsEnable();

	[Ignore]
	[JsonIgnore]
	public string ProjectId => id;

	[Ignore]
	[JsonIgnore]
	public DateTime? DefaultDate => null;

	[Ignore]
	[JsonIgnore]
	public bool IsCompleted => false;

	[Ignore]
	[JsonIgnore]
	public bool IsAbandoned => false;

	[Ignore]
	[JsonIgnore]
	public bool IsDeleted => false;

	[Ignore]
	[JsonIgnore]
	public List<string> Tags => new List<string>();

	[Ignore]
	[JsonIgnore]
	public int Priority => 0;

	[Ignore]
	[JsonIgnore]
	public bool Multiple => false;

	[JsonIgnore]
	public bool IsNote => kind == Constants.ProjectKind.NOTE.ToString();

	[JsonIgnore]
	[Ignore]
	public bool OpenToTeam
	{
		get
		{
			if (!string.IsNullOrEmpty(teamId))
			{
				return openToTeam == true;
			}
			return false;
		}
	}

	public void SetSyncStatus(string status)
	{
		_sync_status = status;
	}

	public bool IsProjectPermit()
	{
		if (permission != null && (!(permission != "read") || !(permission != "comment")))
		{
			if (openToTeam == true && teamMemberPermission != "read" && teamMemberPermission != "comment")
			{
				return CacheManager.GetTeamById(teamId) != null;
			}
			return false;
		}
		return true;
	}

	public bool IsEnable()
	{
		if (IsValid())
		{
			return IsProjectPermit();
		}
		return false;
	}

	public bool IsValid()
	{
		if (closed != true)
		{
			return !delete_status;
		}
		return false;
	}

	public bool IsBelongGroup(string group)
	{
		if (string.IsNullOrEmpty(group) || group == "NONE")
		{
			if (!string.IsNullOrEmpty(groupId))
			{
				return groupId == "NONE";
			}
			return true;
		}
		if (groupId != null)
		{
			return groupId.Equals(group);
		}
		return false;
	}

	public bool IsEquals(ProjectModel project)
	{
		if (project.name == name && project.sortType == sortType)
		{
			return project.IsShareList() == IsShareList();
		}
		return false;
	}

	public int CompareTo(ProjectModel projectB)
	{
		if (projectB == null)
		{
			return -1;
		}
		if (Isinbox)
		{
			return -1;
		}
		if (projectB.Isinbox)
		{
			return 1;
		}
		long num = CacheManager.GetGroupById(groupId)?.sortOrder ?? sortOrder;
		long num2 = CacheManager.GetGroupById(projectB.groupId)?.sortOrder ?? projectB.sortOrder;
		if (num != num2)
		{
			return num.CompareTo(num2);
		}
		return sortOrder.CompareTo(projectB.sortOrder);
	}

	public bool InGroup()
	{
		if (!string.IsNullOrEmpty(groupId))
		{
			return groupId != "NONE";
		}
		return false;
	}

	public bool IsOverLimit()
	{
		return sync_status == Constants.SyncStatus.SYNC_ERROR_UP_LIMIT.ToString();
	}

	public bool IsNew()
	{
		if (!(_sync_status == Constants.SyncStatus.SYNC_NEW.ToString()))
		{
			return _sync_status == Constants.SyncStatus.SYNC_ERROR_UP_LIMIT.ToString();
		}
		return true;
	}

	public SortOption GetSortOption()
	{
		if (SortOption == null)
		{
			if (string.IsNullOrEmpty(sortType))
			{
				sortType = "sortOrder";
			}
			return SortOptionUtils.GetSortOptionBySortType(Utils.LoadSortType(sortType), viewMode == "kanban");
		}
		return new SortOption
		{
			groupBy = SortOption.groupBy,
			orderBy = SortOption.orderBy,
			order = SortOption.order
		};
	}

	public bool IsShareList()
	{
		if (userCount <= 1)
		{
			if (openToTeam == true)
			{
				return !string.IsNullOrEmpty(teamId);
			}
			return false;
		}
		return true;
	}

	private ShowReminderType CombineOldNewState(bool enable, ShowReminderType type)
	{
		if (type == ShowReminderType.None)
		{
			if (enable)
			{
				return ShowReminderType.All;
			}
			if (IsShareList())
			{
				return ShowReminderType.OnlySelf;
			}
			return ShowReminderType.Never;
		}
		if (!IsShareList() && (type == ShowReminderType.OnlySelf || type == ShowReminderType.ExceptOther))
		{
			return ShowReminderType.All;
		}
		return type;
	}
}
