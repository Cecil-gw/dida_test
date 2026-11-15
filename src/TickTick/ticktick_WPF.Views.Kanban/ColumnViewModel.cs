using System;
using System.Collections.Generic;
using System.Linq;
using ticktick_WPF.Cache;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Provider;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.MainListView.TaskList;

namespace ticktick_WPF.Views.Kanban;

public class ColumnViewModel : BaseViewModel
{
	private bool _canDrop;

	private bool _dragging;

	private bool _isPinned;

	public const string ColumnNoDate = "date:no";

	public const string ColumnOutDate = "date:-1";

	public const string ColumnToday = "date:0";

	public const string ColumnTomorrow = "date:1";

	public const string ColumnWeek = "date:week";

	public const string ColumnLater = "date:later";

	public const string ColumnDate = "date:";

	public const string ColumnPriority = "priority:";

	public const string ColumnTag = "tag:";

	public const string ColumnNoTag = "tag:#notag";

	public const string ColumnAssign = "assign:";

	public const string ColumnNoAssign = "assign:-1";

	public const string ColumnProject = "project:";

	public const string ColumnCalendar = "calendar";

	public const string ColumnCourse = "course";

	public const string ColumnHabit = "habit";

	public const string ColumnCountdown = "countdown";

	public const string ColumnNote = "note";

	private string _name;

	private int _taskCount;

	private TaskListViewModel _taskListViewModel;

	private string _color;

	public List<TaskBaseViewModel> SourceItems = new List<TaskBaseViewModel>();

	public long SortOrder { get; set; }

	public long SortOrder2 { get; set; }

	public string ColumnId { get; set; }

	public string Color
	{
		get
		{
			return _color;
		}
		set
		{
			if (!string.Equals(_color, value, StringComparison.OrdinalIgnoreCase))
			{
				_color = value;
				OnPropertyChanged("Color");
			}
		}
	}

	public bool IsCustomColumn
	{
		get
		{
			if (Identity?.Project is NormalProjectIdentity)
			{
				if (!(Identity.Project.SortOption.groupBy == "none"))
				{
					return Identity.Project.SortOption.groupBy == "sortOrder";
				}
				return true;
			}
			return false;
		}
	}

	public string ProjectId => Identity.GetProjectId();

	public bool NewAdd { get; set; }

	public bool Enable { get; set; }

	public bool Editing { get; set; }

	public bool IsPinned
	{
		get
		{
			return _isPinned;
		}
		set
		{
			_isPinned = value;
			OnPropertyChanged("IsPinned");
		}
	}

	public ColumnProjectIdentity Identity { get; set; }

	public bool CanDropDown { get; set; } = true;

	public bool CanDrop
	{
		get
		{
			return _canDrop;
		}
		set
		{
			bool flag = value && CanDropDown;
			if (_canDrop != flag)
			{
				_canDrop = flag;
				OnPropertyChanged("CanDrop");
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

	public string Name
	{
		get
		{
			return _name;
		}
		set
		{
			_name = value;
			OnPropertyChanged("Name");
		}
	}

	public int TaskCount
	{
		get
		{
			return _taskCount;
		}
		set
		{
			_taskCount = value;
			OnPropertyChanged("TaskCount");
		}
	}

	public bool MouseOver { get; set; }

	public bool CanAdd { get; set; } = true;

	public ColumnViewModel()
	{
	}

	public ColumnViewModel(ColumnModel model, int ordinal)
		: this(model)
	{
	}

	public ColumnViewModel(ColumnModel model)
	{
		Name = model.name;
		ColumnId = model.id;
		SortOrder = model.sortOrder.GetValueOrDefault();
		Color = model.color;
	}

	public ColumnViewModel Clone()
	{
		return (ColumnViewModel)MemberwiseClone();
	}

	public void NotifyReload()
	{
		OnPropertyChanged("Reload");
	}

	public static List<ColumnViewModel> GetPriorityColumnViewModels()
	{
		return new List<ColumnViewModel>
		{
			new ColumnViewModel
			{
				Name = Utils.GetString("PriorityHigh"),
				SortOrder = 0L,
				ColumnId = "priority:" + 5
			},
			new ColumnViewModel
			{
				Name = Utils.GetString("PriorityMedium"),
				SortOrder = 1L,
				ColumnId = "priority:" + 3
			},
			new ColumnViewModel
			{
				Name = Utils.GetString("PriorityLow"),
				SortOrder = 2L,
				ColumnId = "priority:" + 1
			},
			new ColumnViewModel
			{
				Name = Utils.GetString("PriorityNull"),
				SortOrder = 3L,
				ColumnId = "priority:" + 0
			}
		};
	}

	public static List<ColumnViewModel> GetWeekDateColumnViewModels()
	{
		DateTime dateTime = Utils.GetWeekStart(DateTime.Today).AddDays(7.0);
		List<ColumnViewModel> list = new List<ColumnViewModel>
		{
			new ColumnViewModel
			{
				Name = Utils.GetString("overdue"),
				SortOrder = ((LocalSettings.Settings.PosOfOverdue != 0) ? 100 : 0),
				ColumnId = "date:-1"
			},
			new ColumnViewModel
			{
				Name = string.Format(Utils.GetString("7DayThisWeek"), Utils.GetString("Today"), DateTime.Today.ToString("ddd", App.Ci)),
				SortOrder = 1L,
				ColumnId = "date:0"
			},
			new ColumnViewModel
			{
				Name = string.Format(Utils.GetString("7DayThisWeek"), Utils.GetString("Tomorrow"), DateTime.Today.AddDays(1.0).ToString("ddd", App.Ci)),
				SortOrder = 4L,
				ColumnId = "date:1"
			}
		};
		for (int i = 2; i < 7; i++)
		{
			DateTime dateTime2 = DateTime.Today.AddDays(i);
			list.Add(new ColumnViewModel
			{
				Name = string.Format((dateTime2 >= dateTime) ? Utils.GetString("7DayTodayNextWeek") : Utils.GetString("7DayThisWeek"), DateUtils.FormatShortDate(dateTime2), DateUtils.FormatWeekDayName(dateTime2)),
				SortOrder = 3 + i,
				ColumnId = "date:" + i
			});
		}
		return list;
	}

	public static List<ColumnViewModel> GetDateColumnViewModels()
	{
		return new List<ColumnViewModel>
		{
			new ColumnViewModel
			{
				Name = Utils.GetString("overdue"),
				SortOrder = ((LocalSettings.Settings.PosOfOverdue != 0) ? 100 : 0),
				ColumnId = "date:-1"
			},
			new ColumnViewModel
			{
				Name = Utils.GetString("Today"),
				SortOrder = 1L,
				ColumnId = "date:0"
			},
			new ColumnViewModel
			{
				Name = Utils.GetString("Tomorrow"),
				SortOrder = 4L,
				ColumnId = "date:1"
			},
			new ColumnViewModel
			{
				Name = Utils.GetString("Next7Day"),
				SortOrder = 5L,
				ColumnId = "date:week"
			},
			new ColumnViewModel
			{
				Name = Utils.GetString("later"),
				SortOrder = 6L,
				ColumnId = "date:later"
			},
			new ColumnViewModel
			{
				Name = Utils.GetString("NoDate"),
				SortOrder = 7L,
				ColumnId = "date:no"
			}
		};
	}

	public void SetTaskListViewModel(TaskListViewModel taskListViewModel)
	{
		_taskListViewModel = taskListViewModel;
	}

	public TaskListViewModel GetListViewModel()
	{
		return _taskListViewModel;
	}

	public IEnumerable<string> GetSelectedTask()
	{
		return (from task in _taskListViewModel?.Items
			where task.Selected
			select task.Id);
	}

	public DateTime? GetDate()
	{
		return GetDate(ColumnId);
	}

	public static DateTime? GetDate(string columnId)
	{
		if (columnId.StartsWith("date:"))
		{
			if (int.TryParse(columnId.Replace("date:", ""), out var result))
			{
				return DateTime.Today.AddDays(result);
			}
			if (!(columnId == "date:week"))
			{
				if (columnId == "date:later")
				{
					return DateTime.Today.AddDays(8.0);
				}
				return null;
			}
			return DateTime.Today.AddDays(2.0);
		}
		return null;
	}

	public TagModel GetTag()
	{
		return GetTag(ColumnId);
	}

	public static TagModel GetTag(string columnId)
	{
		if (columnId.StartsWith("tag:"))
		{
			return CacheManager.GetTagByName(columnId.Replace("tag:", ""));
		}
		return null;
	}

	public int GetPriority()
	{
		return GetPriority(ColumnId);
	}

	public static int GetPriority(string columnId)
	{
		if (columnId.StartsWith("priority:") && int.TryParse(columnId.Replace("priority:", ""), out var result))
		{
			return result;
		}
		return 0;
	}

	public string GetProject()
	{
		return GetProject(ColumnId);
	}

	public static string GetProject(string columnId)
	{
		if (columnId.StartsWith("project:"))
		{
			return columnId.Replace("project:", "");
		}
		return null;
	}

	public string GetAssignee()
	{
		return GetAssignee(ColumnId);
	}

	public static string GetAssignee(string columnId)
	{
		if (columnId.StartsWith("assign:"))
		{
			return columnId.Replace("assign:", "");
		}
		return "-1";
	}

	public void Clear()
	{
		_taskListViewModel?.Clear();
	}

	public void Dispose()
	{
		_taskListViewModel?.Dispose();
	}

	public static ColumnViewModel GetCountdownColumn()
	{
		return new ColumnViewModel
		{
			Name = Utils.GetString("CountdownTitle"),
			SortOrder = long.MinValue,
			ColumnId = "countdown",
			CanDropDown = false,
			CanAdd = false
		};
	}

	public static ColumnViewModel GetHabitColumn(bool useInWeek = false)
	{
		ColumnViewModel columnViewModel = new ColumnViewModel
		{
			Name = Utils.GetString("TodayHabit"),
			SortOrder = long.MaxValue,
			ColumnId = "habit",
			CanDropDown = false,
			CanAdd = false
		};
		if (useInWeek)
		{
			if (Utils.IsCn() || Utils.IsJp())
			{
				columnViewModel.Name = Utils.GetString("TodayHabit") + ", " + DateUtils.FormatWeekDayName(DateTime.Today);
			}
			else
			{
				columnViewModel.Name = DateUtils.FormatWeekDayName(DateTime.Today) + ", " + Utils.GetString("TodayHabit");
			}
		}
		return columnViewModel;
	}

	public static ColumnViewModel GetNoteColumn()
	{
		return new ColumnViewModel
		{
			Name = Utils.GetString("Notes"),
			SortOrder = 1000L,
			ColumnId = "note",
			CanDropDown = false
		};
	}

	public static string GetTaskSortKey(SortOption sortOption, string columnId)
	{
		if (sortOption == null)
		{
			return null;
		}
		if (sortOption.orderByType == Constants.SortType.sortOrder)
		{
			return null;
		}
		switch (columnId)
		{
		case "date:later":
		case "date:-1":
		case "date:week":
		case "note":
			return null;
		default:
		{
			string sortKey = sortOption.GetSortKey();
			if (string.IsNullOrEmpty(sortKey))
			{
				return null;
			}
			switch (columnId)
			{
			case "tag:#notag":
				return string.Format(sortKey, "noTag");
			case "date:no":
				return string.Format(sortKey, "noDate");
			case "assign:-1":
				return string.Format(sortKey, "-1");
			default:
				if (columnId.StartsWith("assign:"))
				{
					return string.Format(sortKey, GetAssignee(columnId));
				}
				if (columnId.StartsWith("tag:"))
				{
					TagModel tag = GetTag(columnId);
					if (tag != null)
					{
						return string.Format(sortKey, tag.name);
					}
				}
				if (columnId.StartsWith("priority:"))
				{
					return string.Format(sortKey, GetPriority(columnId));
				}
				if (columnId.StartsWith("project:"))
				{
					return string.Format(sortKey, GetProject(columnId));
				}
				if (columnId.StartsWith("date:"))
				{
					DateTime? date = GetDate(columnId);
					if (date.HasValue)
					{
						return string.Format(sortKey, date.Value.ToString("yyyyMMdd", App.Ci));
					}
				}
				return string.Format(sortKey, columnId);
			}
		}
		}
	}

	public static bool CanTaskSortInColumn(string columnId)
	{
		switch (columnId)
		{
		case "date:later":
		case "date:-1":
		case "date:week":
		case "note":
			return false;
		default:
			return true;
		}
	}

	public void SetColumnIdentity(ProjectIdentity identity)
	{
		ColumnProjectIdentity columnProjectIdentity = (Identity = new ColumnProjectIdentity(identity, ColumnId));
		Enable = false;
		CanAdd = CanAdd && !(identity is AssignToMeProjectIdentity) && columnProjectIdentity.CanEdit && ColumnId != "habit" && ColumnId != "calendar" && ColumnId != "course";
	}

	public void RemoveSelectedId(string id)
	{
		DisplayItemModel displayItemModel = _taskListViewModel?.Items?.FirstOrDefault((DisplayItemModel m) => m.Id == id);
		if (displayItemModel != null)
		{
			displayItemModel.Selected = false;
		}
	}
}
