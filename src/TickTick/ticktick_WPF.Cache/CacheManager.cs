using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using TickTickUtils.Lang;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Provider;
using ticktick_WPF.Views.Search;

namespace ticktick_WPF.Cache;

public static class CacheManager
{
	private struct ProjectSortable
	{
		public string ProjectId;

		public long ProjectOrder;

		public long GroupOrder;

		public long TeamOrder;

		public string Name;

		public ProjectSortable(string projectId, long projectOrder, long groupOrder, long teamOrder, string name)
		{
			ProjectId = projectId;
			ProjectOrder = projectOrder;
			GroupOrder = groupOrder;
			TeamOrder = teamOrder;
			Name = name;
		}
	}

	private static class CacheKey
	{
		public const string Tag = "tag";

		public const string Project = "project";

		public const string ProjectGroup = "projectGroup";

		public const string Filter = "filter";

		public const string SyncTask = "syncTask";

		public const string CalendarProfile = "calendarProfile";

		public const string BindCalendarAccount = "bindCalendarAccount";

		public const string BindCalendars = "bindCalendars";

		public const string Team = "team";

		public const string SectionStatus = "sectionStatus";
	}

	private static readonly Dictionary<string, ICacheable> Cacheables = new Dictionary<string, ICacheable>();

	public static bool IsInit = false;

	public static event EventHandler CacheInited;

	public static async Task Init()
	{
		await InitData();
		await WarmUp();
		IsInit = true;
	}

	private static async Task InitData()
	{
		Cacheables.Clear();
		Cacheables["syncTask"] = new TaskSyncCache();
		Cacheables["tag"] = new TagCache();
		Cacheables["project"] = new ProjectCache();
		Cacheables["projectGroup"] = new ProjectGroupCache();
		Cacheables["calendarProfile"] = new CalendarProfileCache();
		Cacheables["bindCalendarAccount"] = new BindCalendarAccountCache();
		Cacheables["bindCalendars"] = new BindCalendarsCache();
		Cacheables["filter"] = new FilterCache();
		Cacheables["team"] = new TeamCache();
		Cacheables["sectionStatus"] = new SectionStatusCache();
		await Task.WhenAll(new List<Task>
		{
			Cacheables["project"].Load(),
			Cacheables["projectGroup"].Load(),
			Cacheables["team"].Load()
		});
	}

	private static async Task WarmUp()
	{
		_ = 1;
		try
		{
			await Task.WhenAll(new List<Task>
			{
				HandleCaches((ICacheable cache) => cache.Load()),
				TaskCompletionRateDao.WarmUp(),
				ArchiveCourseDao.CheckOldArchiveCourse()
			});
			await TaskViewModelHelper.Init();
		}
		catch (Exception ex)
		{
			UtilLog.Error(ex);
		}
	}

	public static async Task Clear()
	{
		await HandleCaches((ICacheable cache) => cache.Clear());
		TaskDefaultDao.ClearCache();
	}

	private static async Task HandleCaches(Func<ICacheable, Task> process)
	{
		if (Cacheables == null || Cacheables.Count <= 0)
		{
			return;
		}
		List<KeyValuePair<string, ICacheable>> list = Cacheables.ToList();
		List<Task> list2 = new List<Task>();
		foreach (KeyValuePair<string, ICacheable> item in list)
		{
			if (item.Key != "project" && item.Key != "projectGroup")
			{
				list2.Add(process(item.Value));
			}
		}
		await Task.WhenAll(list2);
	}

	public static void DeleteTag(string tag)
	{
		DeleteEntity<TagModel>("tag", tag);
		Application.Current?.Dispatcher?.InvokeAsync(delegate
		{
			SearchProjectHelper.DeleteModels(tag);
			ProjectAndTaskIdsCache.RemoveIdsModelByKey(tag);
		});
	}

	public static void UpdateTag(TagModel tag, bool onlyUpdate = false)
	{
		UpdateEntity("tag", tag, (TagModel p) => p.name);
		if (!onlyUpdate)
		{
			Application.Current?.Dispatcher?.InvokeAsync(delegate
			{
				SearchProjectHelper.UpdateModels(tag);
				DataChangedNotifier.NotifyTagChanged(tag);
			});
		}
	}

	public static void DeleteProjectGroup(string groupId)
	{
		DeleteEntity<ProjectGroupModel>("projectGroup", groupId);
	}

	public static void DeleteProjectGroup(ProjectGroupModel projectGroup)
	{
		DeleteEntity("projectGroup", projectGroup, (ProjectGroupModel p) => p.id);
		DataChangedNotifier.NotifyGroupChanged(projectGroup);
		Application.Current?.Dispatcher?.InvokeAsync(delegate
		{
			SearchProjectHelper.DeleteGroup(projectGroup.id);
		});
	}

	public static void UpdateProjectGroup(ProjectGroupModel projectGroup)
	{
		UpdateEntity("projectGroup", projectGroup, (ProjectGroupModel p) => p.id);
		DataChangedNotifier.NotifyGroupChanged(projectGroup);
		Application.Current?.Dispatcher?.InvokeAsync(delegate
		{
			SearchProjectHelper.UpdateModels(projectGroup);
		});
	}

	public static void DeleteFilter(string id)
	{
		DeleteEntity<FilterModel>("filter", id);
		Application.Current?.Dispatcher?.InvokeAsync(delegate
		{
			SearchProjectHelper.DeleteFilter(id);
			DataChangedNotifier.NotifyFilterChanged(new FilterChangeArgs
			{
				deleteId = id
			});
		});
	}

	public static void UpdateFilter(FilterModel filter)
	{
		FilterModel filterById = GetFilterById(filter?.id);
		bool ruleChanged = filter?.rule != filterById?.rule;
		UpdateEntity("filter", filter, (FilterModel p) => p.id);
		Application.Current?.Dispatcher?.InvokeAsync(delegate
		{
			SearchProjectHelper.UpdateModels(filter);
			if (ruleChanged)
			{
				TaskCountCache.ReloadProjectTaskCount(new FilterProjectIdentity(filter));
			}
			DataChangedNotifier.NotifyFilterChanged(new FilterChangeArgs
			{
				Filter = filter,
				RuleChanged = ruleChanged
			});
		});
	}

	public static void UpdateTeam(TeamModel team)
	{
		UpdateEntity("team", team, (TeamModel p) => p.id);
		DataChangedNotifier.NotifyTeamChanged();
	}

	public static void DeleteTeam(TeamModel team)
	{
		DeleteEntity("team", team, (TeamModel p) => p.id);
		DataChangedNotifier.NotifyTeamChanged();
	}

	public static void ClearCalendarProfile()
	{
		GetCacheable("calendarProfile")?.Clear();
		DataChangedNotifier.NotifyCalendarChanged();
	}

	public static void DeleteBindAccount(BindCalendarAccountModel model)
	{
		DeleteEntity("bindCalendarAccount", model, (BindCalendarAccountModel p) => p.Id);
		DataChangedNotifier.NotifyCalendarChanged();
	}

	public static void DeleteBindCalendar(BindCalendarModel model)
	{
		DeleteEntity("bindCalendars", model, (BindCalendarModel p) => p.Id);
	}

	public static void UpdateBindCalendar(BindCalendarModel model)
	{
		UpdateEntity("bindCalendars", model, (BindCalendarModel p) => p.Id);
		DataChangedNotifier.NotifyCalendarChanged();
	}

	public static void UpdateBindAccount(BindCalendarAccountModel account)
	{
		UpdateEntity("bindCalendarAccount", account, (BindCalendarAccountModel p) => p.Id);
		DataChangedNotifier.NotifyCalendarChanged();
	}

	public static void DeleteCalendarProfile(CalendarSubscribeProfileModel profile)
	{
		DeleteEntity("calendarProfile", profile, (CalendarSubscribeProfileModel p) => p.Id);
		DataChangedNotifier.NotifyCalendarChanged();
	}

	public static void UpdateCalendarProfile(CalendarSubscribeProfileModel profile)
	{
		UpdateEntity("calendarProfile", profile, (CalendarSubscribeProfileModel p) => p.Id);
		DataChangedNotifier.NotifyCalendarChanged();
	}

	public static void DeleteProject(ProjectModel project)
	{
		DeleteEntity("project", project, (ProjectModel p) => p.id);
		ThreadUtil.DetachedRunOnUiBackThread(delegate
		{
			DataChangedNotifier.NotifyProjectChanged();
			SearchProjectHelper.DeleteModels(project);
			TaskViewModelHelper.OnProjectDeleted(project);
			TaskCountCache.TryReloadGroupAndSmartCount();
		});
	}

	public static void UpdateProject(ProjectModel project, bool notify = true)
	{
		UpdateEntity("project", project, (ProjectModel p) => p.id);
		ThreadUtil.DetachedRunOnUiBackThread(delegate
		{
			NotificationHelper.OnProjectChanged(project);
			if (notify)
			{
				DataChangedNotifier.NotifyProjectChanged();
			}
			SearchProjectHelper.UpdateModels(project);
			TaskViewModelHelper.OnProjectChanged(project);
			TaskCountCache.TryReloadGroupAndSmartCount();
		});
	}

	public static void UpdateSyncTask(TaskSyncedJsonModel json)
	{
		UpdateEntity("syncTask", json, (TaskSyncedJsonModel p) => p.taskSid);
	}

	public static void DeleteSyncTask(TaskSyncedJsonModel json)
	{
		DeleteEntity("syncTask", json, (TaskSyncedJsonModel p) => p.taskSid);
	}

	public static void AddSectionStatus(SectionStatusModel model)
	{
		UpdateEntity("sectionStatus", model, (SectionStatusModel p) => p.Identity + "|" + p.Name);
	}

	public static void DeleteSectionStatus(SectionStatusModel model)
	{
		DeleteEntity("sectionStatus", model, (SectionStatusModel p) => p.Identity + "|" + p.Name);
	}

	private static void UpdateEntity<T>(string key, T data, Func<T, string> getId)
	{
		ICacheable cacheable = GetCacheable(key);
		if (cacheable != null && cacheable is CacheBase<T> cacheBase && !string.IsNullOrEmpty(getId(data)))
		{
			cacheBase.Update(getId(data), data);
		}
	}

	private static void DeleteEntity<T>(string key, string id)
	{
		ICacheable cacheable = GetCacheable(key);
		if (cacheable != null && cacheable is CacheBase<T> cacheBase)
		{
			cacheBase.Delete(id);
		}
	}

	private static void DeleteEntity<T>(string key, T data, Func<T, string> getId)
	{
		ICacheable cacheable = GetCacheable(key);
		if (cacheable != null && cacheable is CacheBase<T> cacheBase)
		{
			cacheBase.Delete(getId(data));
		}
	}

	public static List<ProjectModel> GetProjectsWithoutInbox()
	{
		return (from p in GetDataSafely<ProjectModel>("project")
			where !p.Isinbox
			select p).ToList();
	}

	public static List<ProjectModel> GetOpenedProjects()
	{
		return (from p in GetDataSafely<ProjectModel>("project")
			where !p.closed.HasValue || (p.closed.HasValue && !p.closed.Value)
			select p).ToList();
	}

	public static List<ProjectModel> GetProjects()
	{
		return GetDataSafely<ProjectModel>("project");
	}

	public static List<ProjectModel> GetEnableProjects()
	{
		return (from p in GetDataSafely<ProjectModel>("project")
			where p.IsEnable()
			select p).ToList();
	}

	public static Dictionary<string, long> GetProjectSortOrders()
	{
		Dictionary<string, long> dictionary = new Dictionary<string, long>();
		List<ProjectModel> projects = GetProjects();
		List<ProjectGroupModel> projectGroups = GetProjectGroups();
		List<TeamModel> list = GetTeams();
		List<ProjectSortable> list2 = new List<ProjectSortable>();
		if (list == null)
		{
			list = new List<TeamModel>();
		}
		foreach (ProjectModel project in projects)
		{
			TeamModel teamModel = list.FirstOrDefault((TeamModel t) => t.id == project.teamId);
			long teamOrder = ((teamModel != null) ? list.IndexOf(teamModel) : long.MaxValue);
			ProjectSortable item = new ProjectSortable(project.id, project.sortOrder, -1L, teamOrder, project.name);
			ProjectGroupModel projectGroupModel = projectGroups.FirstOrDefault((ProjectGroupModel p) => p.id == project.groupId);
			if (projectGroupModel != null)
			{
				item.GroupOrder = projectGroupModel.sortOrder.GetValueOrDefault();
			}
			list2.Add(item);
		}
		list2.Sort(delegate(ProjectSortable left, ProjectSortable right)
		{
			if (left.TeamOrder != right.TeamOrder)
			{
				return left.TeamOrder.CompareTo(right.TeamOrder);
			}
			long num2 = ((left.GroupOrder == -1) ? left.ProjectOrder : left.GroupOrder);
			long value = ((right.GroupOrder == -1) ? right.ProjectOrder : right.GroupOrder);
			int num3 = num2.CompareTo(value);
			if (num3 == 0)
			{
				num3 = left.ProjectOrder.CompareTo(right.ProjectOrder);
			}
			return num3;
		});
		for (int num = 0; num < list2.Count; num++)
		{
			ProjectSortable projectSortable = list2[num];
			if (!dictionary.ContainsKey(projectSortable.ProjectId))
			{
				dictionary.Add(projectSortable.ProjectId, num);
			}
		}
		return dictionary;
	}

	public static List<SectionStatusModel> GetSectionStatus()
	{
		return GetDataSafely<SectionStatusModel>("sectionStatus");
	}

	public static List<ProjectGroupModel> GetProjectGroups()
	{
		return GetDataSafely<ProjectGroupModel>("projectGroup");
	}

	public static List<FilterModel> GetFilters()
	{
		return GetDataSafely<FilterModel>("filter");
	}

	public static List<TeamModel> GetTeams()
	{
		return (from team in GetDataSafely<TeamModel>("team")
			orderby team.joinedTime descending
			select team).ToList();
	}

	public static List<BindCalendarModel> GetBindCalendars()
	{
		return GetDataSafely<BindCalendarModel>("bindCalendars");
	}

	public static List<BindCalendarAccountModel> GetBindCalendarAccounts()
	{
		return GetDataSafely<BindCalendarAccountModel>("bindCalendarAccount").ToList();
	}

	public static List<CalendarSubscribeProfileModel> GetSubscribeCalendars()
	{
		return GetDataSafely<CalendarSubscribeProfileModel>("calendarProfile");
	}

	public static List<TagModel> GetTags()
	{
		return GetDataSafely<TagModel>("tag");
	}

	private static List<T> GetDataSafely<T>(string key)
	{
		ICacheable cacheable = GetCacheable(key);
		if (cacheable != null && cacheable is CacheBase<T> cacheBase)
		{
			return cacheBase.GetData().Values.ToList();
		}
		return new List<T>();
	}

	private static ConcurrentDictionary<string, T> GetDictSafely<T>(string key)
	{
		ICacheable cacheable = GetCacheable(key);
		if (cacheable != null && cacheable is CacheBase<T> cacheBase)
		{
			return cacheBase.GetData();
		}
		return new ConcurrentDictionary<string, T>();
	}

	public static ProjectModel GetDefaultProjectInGroup(string groupId)
	{
		return (from p in GetProjects()
			where p.groupId == groupId && p.IsProjectPermit()
			orderby p.sortOrder
			select p).FirstOrDefault();
	}

	public static List<ProjectModel> GetProjectsInGroup(string groupId)
	{
		return (from p in GetProjects()
			where p.groupId == groupId && p.IsValid()
			select p).ToList();
	}

	private static ICacheable GetCacheable(string key)
	{
		if (Cacheables != null && Cacheables.Count > 0 && Cacheables.ContainsKey(key))
		{
			return Cacheables[key];
		}
		return null;
	}

	public static List<string> GetExpiredProjectIds()
	{
		List<string> expiredTeams = (from team in GetTeams()
			where team.expired
			select team.id).ToList();
		if (expiredTeams.Any())
		{
			List<string> list = (from project in GetProjects()
				where expiredTeams.Contains(project.teamId)
				select project.id).ToList();
			if (list.Any())
			{
				return list;
			}
		}
		return new List<string>();
	}

	public static bool IsShareListUser()
	{
		return GetProjects().Any((ProjectModel p) => p.IsShareList());
	}

	public static ConcurrentDictionary<string, ProjectModel> GetProjectDict()
	{
		return GetDictSafely<ProjectModel>("project");
	}

	public static ProjectModel GetProjectById(string projectId)
	{
		if (!string.IsNullOrEmpty(projectId) && GetProjectDict().TryGetValue(projectId, out var value))
		{
			return value;
		}
		return null;
	}

	public static ConcurrentDictionary<string, ProjectGroupModel> GetGroupDict()
	{
		return GetDictSafely<ProjectGroupModel>("projectGroup");
	}

	public static ProjectGroupModel GetGroupById(string id)
	{
		if (!string.IsNullOrEmpty(id) && GetGroupDict().TryGetValue(id, out var value))
		{
			return value;
		}
		return null;
	}

	public static ConcurrentDictionary<string, TagModel> GetTagDict()
	{
		return GetDictSafely<TagModel>("tag");
	}

	public static TagModel GetTagByName(string name)
	{
		ConcurrentDictionary<string, TagModel> tagDict = GetTagDict();
		if (!string.IsNullOrEmpty(name) && tagDict.ContainsKey(name))
		{
			return tagDict[name];
		}
		return null;
	}

	public static ConcurrentDictionary<string, FilterModel> GetFilterDict()
	{
		return GetDictSafely<FilterModel>("filter");
	}

	public static FilterModel GetFilterById(string filterId)
	{
		ConcurrentDictionary<string, FilterModel> filterDict = GetFilterDict();
		if (!string.IsNullOrEmpty(filterId) && filterDict.ContainsKey(filterId))
		{
			return filterDict[filterId];
		}
		return null;
	}

	public static ConcurrentDictionary<string, CalendarSubscribeProfileModel> GetSubscribeDict()
	{
		return GetDictSafely<CalendarSubscribeProfileModel>("calendarProfile");
	}

	public static ConcurrentDictionary<string, BindCalendarAccountModel> GetAccountCalDict()
	{
		return GetDictSafely<BindCalendarAccountModel>("bindCalendarAccount");
	}

	public static ConcurrentDictionary<string, BindCalendarModel> GetBindCalendarDict()
	{
		return GetDictSafely<BindCalendarModel>("bindCalendars");
	}

	public static ConcurrentDictionary<string, TeamModel> GetTeamDict()
	{
		return GetDictSafely<TeamModel>("team");
	}

	public static BindCalendarAccountModel GetAccountCalById(string accountId)
	{
		ConcurrentDictionary<string, BindCalendarAccountModel> accountCalDict = GetAccountCalDict();
		if (!string.IsNullOrEmpty(accountId) && accountCalDict.TryGetValue(accountId, out var value))
		{
			return value;
		}
		return null;
	}

	public static bool CheckProjectValid(string projectId, bool inAll = false)
	{
		ProjectModel projectById = GetProjectById(projectId);
		if (projectById != null && projectById.IsValid())
		{
			if (inAll)
			{
				return projectById.inAll;
			}
			return true;
		}
		return false;
	}

	public static bool CheckProjectEnable(string projectId, bool inAll)
	{
		ProjectModel projectById = GetProjectById(projectId);
		if (projectById != null && projectById.IsEnable())
		{
			if (inAll)
			{
				return projectById.inAll;
			}
			return true;
		}
		return false;
	}

	public static List<ProjectModel> GetProjectInTeams(string teamId)
	{
		if (teamId == "c1a7e08345e444dea187e21a692f0d7a")
		{
			teamId = null;
		}
		return GetProjects().Where(delegate(ProjectModel p)
		{
			if (p.Isinbox)
			{
				return false;
			}
			return string.IsNullOrEmpty(teamId) ? string.IsNullOrEmpty(p.teamId) : (p.teamId == teamId);
		}).ToList();
	}

	public static List<TagModel> GetTags(string[] tags)
	{
		List<TagModel> list = new List<TagModel>();
		ConcurrentDictionary<string, TagModel> tagDict = GetTagDict();
		if (tags != null && tags.Length != 0)
		{
			foreach (string text in tags)
			{
				if (!string.IsNullOrEmpty(text) && tagDict.TryGetValue(text, out var value))
				{
					list.Add(value);
				}
			}
		}
		return list;
	}

	public static CalendarSubscribeProfileModel GetSubscribeCalById(string calendarId)
	{
		ConcurrentDictionary<string, CalendarSubscribeProfileModel> subscribeDict = GetSubscribeDict();
		if (!string.IsNullOrEmpty(calendarId) && subscribeDict.TryGetValue(calendarId, out var value))
		{
			return value;
		}
		return null;
	}

	public static BindCalendarModel GetBindCalendarById(string calendarId)
	{
		ConcurrentDictionary<string, BindCalendarModel> bindCalendarDict = GetBindCalendarDict();
		if (!string.IsNullOrEmpty(calendarId) && bindCalendarDict.TryGetValue(calendarId, out var value))
		{
			return value;
		}
		return null;
	}

	public static TeamModel GetTeamById(string teamId)
	{
		ConcurrentDictionary<string, TeamModel> teamDict = GetTeamDict();
		if (!string.IsNullOrEmpty(teamId) && teamDict.TryGetValue(teamId, out var value))
		{
			return value;
		}
		return null;
	}

	public static TeamModel GetTeam()
	{
		return GetTeams().FirstOrDefault((TeamModel t) => !t.expired);
	}
}
