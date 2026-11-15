using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using TickTickUtils.Lang;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.QuickAdd;
using ticktick_WPF.Views.TaskList;
using ticktick_WPF.Views.Timeline;

namespace ticktick_WPF.Util.Provider;

public class ProjectIdentity : IProjectTaskDefault, ITimelineProject
{
	private SortOption _sortOption;

	public virtual string Id { get; protected set; }

	public virtual string Title { get; protected set; }

	public virtual string LoadMoreId => "_special_id_all";

	public virtual string SortProjectId => "all";

	public virtual string QueryId => Id;

	public virtual bool CanEdit => true;

	public virtual bool LoadAll => false;

	public virtual string CatId => Id;

	public virtual bool IsNote { get; set; }

	public bool CanDrag { get; set; } = true;

	public virtual string ViewMode { get; set; } = "list";

	public virtual string ParallelViewMode { get; set; } = "list";

	public bool IsVirtual { get; set; }

	public bool ShowVirtualContent { get; set; }

	public SortOption SortOption
	{
		get
		{
			if (_sortOption == null)
			{
				UtilLog.Info("IdentitySortOptionIsNull, " + QueryId + GetType().Name);
				_sortOption = new SortOption
				{
					groupBy = "dueDate",
					orderBy = "dueDate"
				};
			}
			return _sortOption;
		}
		set
		{
			_sortOption = value;
		}
	}

	protected ProjectIdentity()
	{
	}

	public virtual string GetProjectId()
	{
		return TaskDefaultDao.GetDefaultSafely().ProjectId;
	}

	public virtual string GetProjectName()
	{
		return CacheManager.GetProjects().FirstOrDefault((ProjectModel p) => p.id == GetProjectId())?.name;
	}

	public virtual string GetColumnId()
	{
		return string.Empty;
	}

	public virtual TimeData GetTimeData()
	{
		if (!IsNote)
		{
			return TimeData.BuildFromDefault(TaskDefaultDao.GetDefaultSafely());
		}
		return new TimeData();
	}

	public virtual int GetPriority()
	{
		return TaskDefaultDao.GetDefaultSafely().Priority;
	}

	public virtual List<string> GetTags()
	{
		return TaskDefaultDao.GetDefaultSafely().Tags;
	}

	public virtual bool UseDefaultTags()
	{
		return true;
	}

	public virtual bool IsCalendar()
	{
		return false;
	}

	public virtual string GetAccountId()
	{
		return string.Empty;
	}

	public virtual bool GetIsNote()
	{
		return IsNote;
	}

	public virtual bool GetForceShowProjectName()
	{
		return false;
	}

	public virtual string GetAssignee()
	{
		return null;
	}

	public virtual async Task<int> LoadCount()
	{
		return 0;
	}

	public virtual DateTime? GetCompletedFromTime()
	{
		return null;
	}

	public virtual bool IsNormalProject()
	{
		return false;
	}

	public virtual string CheckTitleValid(string text)
	{
		return null;
	}

	public virtual void SaveTitle(string text)
	{
	}

	public static NormalProjectIdentity CreateInboxProject()
	{
		string text = "inbox" + LocalSettings.Settings.LoginUserId;
		return new NormalProjectIdentity(CacheManager.GetProjectById(text) ?? new ProjectModel
		{
			id = text,
			name = Utils.GetString("Inbox"),
			Isinbox = true
		});
	}

	public static NormalProjectIdentity GetDefaultProject()
	{
		ProjectModel projectModel = CacheManager.GetProjects().FirstOrDefault((ProjectModel p) => p.id == TaskDefaultDao.GetDefaultSafely().ProjectId);
		if (projectModel == null)
		{
			return CreateInboxProject();
		}
		return new NormalProjectIdentity(projectModel);
	}

	public virtual ProjectIdentity Copy(ProjectIdentity project)
	{
		return project;
	}

	public virtual bool CanAddTask()
	{
		return true;
	}

	public virtual string GetDisplayTitle()
	{
		return GetDefaultProject().GetProjectName();
	}

	public virtual Geometry GetProjectIcon()
	{
		return null;
	}

	public virtual string GetSortProjectId()
	{
		SortOption sortOption = SortOption;
		if (sortOption == null || sortOption.groupByType != Constants.SortType.dueDate)
		{
			SortOption sortOption2 = SortOption;
			if (sortOption2 == null || sortOption2.groupByType != Constants.SortType.project)
			{
				return CatId;
			}
		}
		return SortProjectId;
	}

	public static ProjectIdentity CreateSmartIdentity(string id)
	{
		return id switch
		{
			"_special_id_all" => new AllProjectIdentity(), 
			"_special_id_today" => new TodayProjectIdentity(), 
			"_special_id_tomorrow" => new TomorrowProjectIdentity(), 
			"_special_id_week" => new WeekProjectIdentity(), 
			"_special_id_assigned" => new AssignToMeProjectIdentity(), 
			"_special_id_completed" => new CompletedProjectIdentity(), 
			"_special_id_abandoned" => new AbandonedProjectIdentity(), 
			"_special_id_trash" => new TrashProjectIdentity(), 
			"_special_id_summary" => new SummaryProjectIdentity(), 
			_ => CreateInboxProject(), 
		};
	}

	public static ProjectIdentity BuildProject(string saveId, bool getDefault = true)
	{
		if (!string.IsNullOrEmpty(saveId) && saveId.Contains(":"))
		{
			string[] array = saveId.Split(':');
			if (array.Length == 2)
			{
				string text = array[0];
				string identity = array[1];
				switch (text)
				{
				case "project":
				{
					ProjectModel projectModel = CacheManager.GetProjects().FirstOrDefault((ProjectModel p) => p.id == identity);
					if (projectModel != null)
					{
						return new NormalProjectIdentity(projectModel);
					}
					break;
				}
				case "smart":
					return CreateSmartIdentity(identity);
				case "group":
				{
					ProjectGroupModel projectGroupModel = CacheManager.GetProjectGroups().FirstOrDefault((ProjectGroupModel p) => p.id == identity);
					if (projectGroupModel != null)
					{
						List<ProjectModel> projectsInGroup = CacheManager.GetProjectsInGroup(identity);
						return new GroupProjectIdentity(projectGroupModel, projectsInGroup);
					}
					break;
				}
				case "filter":
				{
					FilterModel filterModel = CacheManager.GetFilters().FirstOrDefault((FilterModel f) => f.id == identity);
					if (filterModel != null)
					{
						return new FilterProjectIdentity(filterModel);
					}
					break;
				}
				case "tag":
				{
					TagModel tagModel = CacheManager.GetTags().FirstOrDefault((TagModel t) => t.name == identity);
					if (tagModel != null)
					{
						return new TagProjectIdentity(tagModel);
					}
					break;
				}
				case "subscribe_calendar":
				{
					CalendarSubscribeProfileModel calendarSubscribeProfileModel = CacheManager.GetSubscribeCalendars().FirstOrDefault((CalendarSubscribeProfileModel c) => c.Id == identity);
					if (calendarSubscribeProfileModel != null)
					{
						return new SubscribeCalendarProjectIdentity(calendarSubscribeProfileModel);
					}
					break;
				}
				case "bind_account":
				{
					BindCalendarAccountModel bindCalendarAccountModel = CacheManager.GetBindCalendarAccounts().FirstOrDefault((BindCalendarAccountModel account) => account.Id == identity);
					if (bindCalendarAccountModel != null)
					{
						return new BindAccountCalendarProjectIdentity(bindCalendarAccountModel);
					}
					break;
				}
				}
			}
		}
		if (!getDefault)
		{
			return null;
		}
		return CreateInboxProject();
	}

	public virtual TimelineModel GetTimelineModel()
	{
		return null;
	}

	public virtual void CommitTimeline(TimelineModel model)
	{
	}

	public virtual List<SortTypeViewModel> GetTimelineSortTypes()
	{
		return null;
	}

	public virtual async Task SwitchViewMode(string viewMode, bool isParallel = false)
	{
	}

	public virtual List<string> GetSwitchViewModes()
	{
		return null;
	}
}
