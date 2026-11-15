using System;
using ticktick_WPF.Models;

namespace ticktick_WPF.ViewModels;

[Serializable]
public class ProjectViewModel : BaseViewModel
{
	public ProjectModel originalProject { get; set; }

	public int _Id { get; set; }

	public string id { get; set; }

	public string groupId { get; set; }

	private string color { get; set; }

	private int colorindex { get; set; }

	private string name { get; set; }

	private bool inAll { get; set; }

	private int? reminderType { get; set; }

	public bool? closed { get; set; }

	private int num { get; set; }

	public long sortOrder { get; set; }

	public int userCount { get; set; } = 1;

	public string sortType { get; set; }

	public string sync_status { get; set; }

	private string _teamId { get; set; }

	private bool _muted { get; set; }

	private int? showType { get; set; }

	public string Kind { get; set; }

	public bool IsNew { get; set; }

	public bool needAudit { get; set; } = true;

	public bool? openToTeam { get; set; }

	public string teamMemberPermission { get; set; }

	public SortOption SortOption { get; set; }

	private bool _isShowReminderNew { get; set; }

	public bool IsShowReminderNew
	{
		get
		{
			return _isShowReminderNew;
		}
		set
		{
			_isShowReminderNew = value;
			OnPropertyChanged("IsShowReminderNew");
		}
	}

	public string Color
	{
		get
		{
			return color ?? "transparent";
		}
		set
		{
			color = value;
			OnPropertyChanged("Color");
		}
	}

	public string Name
	{
		get
		{
			return name;
		}
		set
		{
			name = value;
			OnPropertyChanged("Name");
		}
	}

	public bool InAll
	{
		get
		{
			return inAll;
		}
		set
		{
			inAll = value;
			OnPropertyChanged("InAll");
		}
	}

	public int? ReminderType
	{
		get
		{
			return reminderType;
		}
		set
		{
			reminderType = value;
			OnPropertyChanged("ReminderType");
		}
	}

	public ShowReminderType ReminderTypeEnum
	{
		get
		{
			if (!ReminderType.HasValue)
			{
				return ShowReminderType.None;
			}
			return (ShowReminderType)ReminderType.Value;
		}
		set
		{
			ReminderType = ((value != ShowReminderType.None) ? new int?((int)value) : ((int?)null));
			if (value != ShowReminderType.None)
			{
				Muted = value != ShowReminderType.All;
			}
		}
	}

	public ShowReminderType ReminderTypeState => CombineOldNewState(!Muted, ReminderTypeEnum);

	public bool Muted
	{
		get
		{
			return _muted;
		}
		set
		{
			_muted = value;
			OnPropertyChanged("Disturb");
		}
	}

	public int? ShowType
	{
		get
		{
			return showType;
		}
		set
		{
			showType = value;
			OnPropertyChanged("ShowType");
		}
	}

	public ShowReminderType ShowTypeEnum
	{
		get
		{
			if (!ShowType.HasValue)
			{
				return ShowReminderType.None;
			}
			return (ShowReminderType)ShowType.Value;
		}
		set
		{
			ShowType = ((value != ShowReminderType.None) ? new int?((int)value) : ((int?)null));
			if (value != ShowReminderType.None)
			{
				InAll = value == ShowReminderType.All;
			}
		}
	}

	public ShowReminderType ShowTypeState => CombineOldNewState(InAll, ShowTypeEnum);

	public int Num
	{
		get
		{
			return num;
		}
		set
		{
			num = value;
			OnPropertyChanged("Num");
		}
	}

	public string TeamId
	{
		get
		{
			return _teamId;
		}
		set
		{
			_teamId = ((value == "") ? null : value);
			OnPropertyChanged("TeamId");
		}
	}

	public string ViewMode { get; set; } = "list";

	public ProjectViewModel()
	{
	}

	public ProjectViewModel(ProjectModel project)
	{
		originalProject = project;
		_Id = project._Id;
		id = project.id;
		color = ((project.color != "transparent") ? project.color : "#00ffffff");
		name = project.name;
		inAll = project.inAll;
		Muted = project.muted;
		ViewMode = project.viewMode;
		groupId = project.groupId;
		num = 0;
		sortOrder = project.sortOrder;
		userCount = ((project.userCount == 0) ? 1 : project.userCount);
		sortType = project.sortType;
		closed = project.closed;
		sync_status = project.sync_status;
		_teamId = project.teamId;
		Kind = project.kind;
		needAudit = project.needAudit ?? true;
		openToTeam = project.openToTeam;
		teamMemberPermission = project.teamMemberPermission;
		SortOption = project.GetSortOption();
		ShowType = project.showType;
		ReminderType = project.reminderType;
	}

	public void SetName(string newName)
	{
		name = newName;
	}

	public bool IsShareList()
	{
		if (userCount <= 1)
		{
			if (!string.IsNullOrEmpty(_teamId))
			{
				return openToTeam == true;
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
