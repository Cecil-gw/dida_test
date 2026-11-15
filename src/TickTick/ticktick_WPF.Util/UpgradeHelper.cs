using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TickTickDao;
using TickTickUtils.Lang;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Service.SortOrder;
using ticktick_WPF.Util.Sync.Model;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Summary;
using ticktick_WPF.Views.Tag;
using ticktick_WPF.Views.Theme;

namespace ticktick_WPF.Util;

public static class UpgradeHelper
{
	private static readonly List<long> CheckPoints = new List<long>
	{
		1678896000000L, 1678904400000L, 1679000000000L, 1687654800000L, 1690005000000L, 1690010000000L, 1690020000000L, 1690030000000L, 1690040000000L, 1690060000000L,
		1690080000000L, 1690081000000L, 1742778000000L
	};

	public static async Task CheckUpgradePoint()
	{
		bool num = LocalSettings.Settings.UpgradeCheckPoint == 0;
		long checkPoint = LocalSettings.Settings.UpgradeCheckPoint;
		if (!num)
		{
			if (checkPoint < 1690080000000L)
			{
				CommonOtherSettings otherSettings = LocalSettings.Settings.Common.GetOtherSettings();
				if (!otherSettings.ShowParallelGuide.HasValue)
				{
					otherSettings.ShowParallelGuide = false;
				}
				LocalSettings.Settings.Common.SetOtherSettings(otherSettings);
				LocalSettings.Settings.Save(force: true);
			}
			if (checkPoint < 1687654800000L)
			{
				App.CheckTwitterLogin(logOut: false);
			}
			if (!Utils.IsNetworkAvailable())
			{
				return;
			}
			if (checkPoint < 1690060000000L && LocalSettings.Settings.ExtraSettings.AppFontFamily == "LXGW_CN")
			{
				FontFamilyUtils.DownLoadFontFile("LXGW_CN");
			}
			if (checkPoint < 1690081000000L)
			{
				UserPreferenceModel remotePreference = await SettingsHelper.PullRemotePreference(0L);
				if (checkPoint < 1690081000000L)
				{
					if (LocalSettings.Settings.UserPreference != null && remotePreference != null)
					{
						LocalSettings.Settings.UserPreference.timelineTimeZones = remotePreference.timelineTimeZones;
					}
					GlobalEventManager.NotifyTimeLineTimeZonesChangedChanged(countChanged: true, delay: true);
				}
				if (checkPoint < 1690040000000L)
				{
					try
					{
						if (Utils.IsTickPackage() && Utils.IsZhCn() && LocalSettings.Settings.EnableLunar && LocalSettings.Settings.UserPreference != null && remotePreference != null)
						{
							UtilLog.Info("migrate lunar calendar");
							LocalSettings.Settings.UserPreference.alternativeCalendar = new AlternativeCalendar
							{
								calendar = CalendarConverter.GetCalendarTypeName(CalendarConvertType.Lunar),
								mtime = Utils.GetNowTimeStampInMills()
							};
							LocalSettings.Settings.UserPreference.mtime = Utils.GetNowTimeStampInMills();
							LocalSettings.Settings.Save();
							await SettingsHelper.PushLocalPreference();
						}
					}
					catch (Exception ex)
					{
						UtilLog.Error(ex);
					}
				}
				if (checkPoint < 1690030000000L)
				{
					try
					{
						if (LocalSettings.Settings.UserPreference != null && remotePreference != null)
						{
							LocalSettings.Settings.UserPreference.summaryTemplates = remotePreference.summaryTemplates;
						}
						SummaryFilterViewModel.TryInitDefaultTemplate();
					}
					catch (Exception ex2)
					{
						UtilLog.Error(ex2);
					}
				}
				if (checkPoint < 1666627400000L && LocalSettings.Settings.UserPreference != null && remotePreference != null)
				{
					LocalSettings.Settings.UserPreference.TimeTable = remotePreference.TimeTable;
				}
				if (checkPoint < 1690010000000L && LocalSettings.Settings.UserPreference != null && remotePreference != null)
				{
					LocalSettings.Settings.UserPreference.RecentlyColors = remotePreference.RecentlyColors;
				}
				if (checkPoint < 1636646430000L)
				{
					if (remotePreference?.matrix == null || remotePreference.matrix.IsEmpty())
					{
						return;
					}
					if (MatrixManager.CheckNeedUpdateDefaultRule(LocalSettings.Settings.MatrixModel))
					{
						await MatrixManager.ResetMatrixRule(isSimple: false);
					}
				}
				if (checkPoint < 1690005000000L && LocalSettings.Settings.UserPreference != null && remotePreference != null)
				{
					LocalSettings.Settings.UserPreference.FocusConfig = remotePreference.FocusConfig;
				}
			}
			if (checkPoint < 1672000100000L)
			{
				UpdateSkipHabit();
			}
			if (checkPoint < 1679000000000L)
			{
				UpdateNewTaskSortOrders();
			}
			if (checkPoint < 1742778000000L)
			{
				UpdateProjects(1742778000000L);
			}
		}
		else
		{
			LocalSettings.Settings.ExtraSettings.ShowParentInCalArrange = true;
		}
		LocalSettings.Settings.UpgradeCheckPoint = CheckPoints.LastOrDefault();
		await LocalSettings.Settings.Save();
	}

	private static async Task UpdateProjects(long lastCheckPoint)
	{
		string lastVersion = LocalSettings.Settings.ExtraSettings.LastVersion;
		SyncBean syncBean = await Communicator.BatchCheck(lastCheckPoint, lastVersion);
		if (syncBean?.projectProfiles == null)
		{
			return;
		}
		foreach (ProjectModel project in syncBean.projectProfiles)
		{
			ProjectModel projectModel = await ProjectDao.GetProjectById(project.id);
			if (projectModel != null)
			{
				projectModel.reminderType = project.reminderType;
				projectModel.showType = project.showType;
				CacheManager.UpdateProject(projectModel, notify: false);
				BaseDao<ProjectModel>.UpdateAsync(projectModel);
			}
		}
	}

	private static async Task UpdateTaskNotionBlocks(SyncBean syncBean)
	{
		if (syncBean == null || !(syncBean.syncTaskBean?.Update?.Count > 0))
		{
			return;
		}
		foreach (TaskModel remoteTask in syncBean.syncTaskBean.Update)
		{
			if (!string.IsNullOrEmpty(remoteTask.notionBlock))
			{
				TaskModel taskModel = await TaskDao.GetTaskById(remoteTask.id);
				if (taskModel != null)
				{
					taskModel.notionBlock = remoteTask.notionBlock;
					await TaskService.UpdateTask(taskModel, CheckMatchedType.None);
				}
			}
		}
	}

	private static async Task UpdateTagType(SyncBean syncBean)
	{
		List<TagModel> list = await TagDao.GetAllTags();
		Dictionary<string, TagModel> serverTag = syncBean?.tags?.ToDictionaryEx((TagModel t) => t.name, (TagModel t) => t);
		foreach (TagModel tag in list)
		{
			if (serverTag != null && serverTag.ContainsKey(tag.name))
			{
				TagModel tagModel = serverTag[tag.name];
				if (tagModel != null && tagModel.type != tag.type)
				{
					tag.type = tagModel.type;
					await BaseDao<TagModel>.UpdateAsync(tag);
					CacheManager.UpdateTag(tag, onlyUpdate: true);
				}
			}
		}
	}

	private static async Task UpdateKanbanView(SyncBean syncBean)
	{
		List<TagModel> list = await TagDao.GetAllTags();
		Dictionary<string, TagModel> serverTag = syncBean?.tags?.ToDictionaryEx((TagModel t) => t.name, (TagModel t) => t);
		foreach (TagModel tag in list)
		{
			if (serverTag != null && serverTag.ContainsKey(tag.name))
			{
				TagModel tagModel = serverTag[tag.name];
				if (tagModel != null && tagModel.viewMode != tag.viewMode)
				{
					tag.viewMode = tagModel.viewMode;
					await BaseDao<TagModel>.UpdateAsync(tag);
					CacheManager.UpdateTag(tag, onlyUpdate: true);
				}
			}
		}
	}

	private static async Task UpdateNewTaskSortOrders()
	{
		List<SyncSortOrderModel> source = await SyncSortOrderDao.GetVersion2Models();
		List<SyncSortOrderModel> list = new List<SyncSortOrderModel>();
		List<SyncSortOrderModel> add = new List<SyncSortOrderModel>();
		Regex regex = new Regex("taskBy#(.+)_(.+)");
		Dictionary<string, long> tagSortDict = TagDataHelper.GetTagSortDict();
		foreach (SyncSortOrderModel item in source.Where((SyncSortOrderModel model) => !model.SortOrderType.Contains("|")))
		{
			Match match = regex.Match(item.SortOrderType);
			if (match.Success)
			{
				string value = match.Groups[1].Value;
				string value2 = match.Groups[2].Value;
				string text = item.GroupId;
				switch (text)
				{
				case "today":
				case "tomorrow":
				case "week":
				case "n7ds":
					text = "all";
					break;
				}
				if (text == "assigned")
				{
					text = "assignee";
				}
				TaskBaseViewModel task = null;
				if (item.Type == 1)
				{
					task = TaskCache.GetTaskById(item.EntityId);
				}
				else if (item.Type == 2)
				{
					task = TaskDetailItemCache.GetCheckItemById(item.EntityId);
				}
				string groupNameByTask = GetGroupNameByTask(task, tagSortDict, value, text == "all");
				if (groupNameByTask != null)
				{
					string type = "taskBy#" + value + "|" + groupNameByTask + "_" + value2;
					add.Add(new SyncSortOrderModel(type)
					{
						Type = item.Type,
						SyncStatus = 1,
						SortOrder = item.SortOrder,
						EntityId = item.EntityId,
						GroupId = text,
						UserId = item.UserId
					});
				}
				else
				{
					list.Add(item);
				}
			}
		}
		foreach (SyncSortOrderModel item2 in list)
		{
			item2.SyncStatus = 1;
			item2.Deleted = 1;
		}
		await SyncSortOrderDao.UpdateAllAsync(list);
		await SyncSortOrderDao.InsertAllAsync(add);
		TaskSortOrderService.FetchAllAsync();
	}

	private static string GetGroupNameByTask(TaskBaseViewModel task, Dictionary<string, long> sortedTagDict, string groupBy, bool inAll)
	{
		switch (groupBy)
		{
		case "none":
			return "none";
		case "sortOrder":
			if (!string.IsNullOrEmpty(task?.ColumnId))
			{
				return task.ColumnId;
			}
			return "none";
		case "dueDate":
			if (task != null)
			{
				if (task.StartDate.HasValue)
				{
					DateTime? startDate = task.StartDate;
					DateTime today = DateTime.Today;
					if (!startDate.HasValue || !(startDate.GetValueOrDefault() >= today) || !(task.StartDate < DateTime.Today.AddDays(inAll ? 7 : 2)))
					{
						return null;
					}
					return task.StartDate.Value.ToString("yyyyMMdd", DateTimeFormatInfo.InvariantInfo);
				}
				return "noDate";
			}
			return DateTime.Today.ToString("yyyyMMdd", DateTimeFormatInfo.InvariantInfo);
		case "priority":
		{
			object obj;
			if (task != null)
			{
				obj = task.Priority.ToString();
				if (obj == null)
				{
					return "";
				}
			}
			else
			{
				obj = "0";
			}
			return (string)obj;
		}
		case "tag":
		{
			if (task == null)
			{
				return "noTag";
			}
			string primaryTag = TagDataHelper.GetPrimaryTag(sortedTagDict, task.Tags.ToList(), null);
			if (!string.IsNullOrEmpty(primaryTag))
			{
				return primaryTag;
			}
			return "noTag";
		}
		case "assignee":
			if (task != null)
			{
				if (!string.IsNullOrEmpty(task.Assignee))
				{
					return task.Assignee;
				}
				return "-1";
			}
			return "-1";
		default:
			return null;
		}
	}

	private static async Task UpdateTaskSortOrders()
	{
		string keyId = "taskBy#dueDate|{0}_dueDate";
		Dictionary<string, Dictionary<string, Dictionary<string, SyncSortOrderModel>>> dict = new Dictionary<string, Dictionary<string, Dictionary<string, SyncSortOrderModel>>>();
		foreach (TaskSortOrderInDateModel item in await TaskSortOrderInDateDao.GetAllSorts())
		{
			if (item.projectid != null && item.taskid != null)
			{
				string text = string.Format(keyId, item.date);
				if (!dict.ContainsKey(text))
				{
					dict[text] = new Dictionary<string, Dictionary<string, SyncSortOrderModel>>();
				}
				Dictionary<string, Dictionary<string, SyncSortOrderModel>> dictionary = dict[text];
				string projectid = item.projectid;
				if (!dictionary.ContainsKey(projectid))
				{
					dictionary[projectid] = new Dictionary<string, SyncSortOrderModel>();
				}
				dictionary[projectid][item.taskid] = SyncSortOrderModel.Build(text, item);
			}
		}
		await TaskSortOrderService.SaveInits(dict);
		keyId = "taskBy#project|{0}_dueDate";
		dict.Clear();
		foreach (TaskSortOrderInProjectModel item2 in await ProjectSortOrderDao.GetLocalSortOrders())
		{
			if (item2.ProjectId != null && item2.EntityId != null)
			{
				string text2 = string.Format(keyId, item2.ProjectId);
				if (!dict.ContainsKey(text2))
				{
					dict[text2] = new Dictionary<string, Dictionary<string, SyncSortOrderModel>>();
				}
				Dictionary<string, Dictionary<string, SyncSortOrderModel>> dictionary2 = dict[text2];
				if (!dictionary2.ContainsKey(item2.ProjectId))
				{
					dictionary2[item2.ProjectId] = new Dictionary<string, SyncSortOrderModel>();
				}
				dictionary2[item2.ProjectId][item2.EntityId] = SyncSortOrderModel.Build(text2, item2);
			}
		}
		await TaskSortOrderService.SaveInits(dict);
		keyId = "taskBy#priority|{0}_dueDate";
		dict.Clear();
		foreach (TaskSortOrderInPriorityModel item3 in await TaskSortOrderInPriorityDao.GetAllSortOrders())
		{
			if (item3.CatId != null && item3.EntityId != null)
			{
				string text3 = string.Format(keyId, item3.Priority);
				if (!dict.ContainsKey(text3))
				{
					dict[text3] = new Dictionary<string, Dictionary<string, SyncSortOrderModel>>();
				}
				Dictionary<string, Dictionary<string, SyncSortOrderModel>> dictionary3 = dict[text3];
				string catId = item3.CatId;
				if (!dictionary3.ContainsKey(catId))
				{
					dictionary3[catId] = new Dictionary<string, SyncSortOrderModel>();
				}
				dictionary3[catId][item3.EntityId] = SyncSortOrderModel.Build(text3, item3);
			}
		}
		await TaskSortOrderService.SaveInits(dict);
		keyId = "taskBy#tag|{0}_dueDate";
		dict.Clear();
		foreach (SyncSortOrderModel item4 in await SyncSortOrderDao.SetAllTaskOrderInTag())
		{
			if (item4.GroupId != null && item4.EntityId != null)
			{
				string text4 = string.Format(keyId, item4.SortOrderType.Replace("taskByTag_", ""));
				if (!dict.ContainsKey(text4))
				{
					dict[text4] = new Dictionary<string, Dictionary<string, SyncSortOrderModel>>();
				}
				Dictionary<string, Dictionary<string, SyncSortOrderModel>> dictionary4 = dict[text4];
				string groupId = item4.GroupId;
				if (!dictionary4.ContainsKey(groupId))
				{
					dictionary4[groupId] = new Dictionary<string, SyncSortOrderModel>();
				}
				dictionary4[groupId][item4.EntityId] = SyncSortOrderModel.Build(text4, item4);
			}
		}
		await TaskSortOrderService.SaveInits(dict);
		TaskSortOrderService.FetchAllAsync();
	}

	private static async Task UpdateSmartSortOptions()
	{
		UserPreferenceModel userPreferenceModel = await SettingsHelper.PullRemotePreference(0L);
		if (userPreferenceModel?.matrix != null && (userPreferenceModel.matrix.quadrants?.Any((QuadrantModel q) => q.SortOption != null) ?? false))
		{
			LocalSettings.Settings.UserPreference.matrix = userPreferenceModel.matrix;
		}
		else
		{
			LocalSettings.Settings.InitMatrixSortOption();
		}
		if (userPreferenceModel?.SmartProjectsOption != null)
		{
			LocalSettings.Settings.UserPreference.SmartProjectsOption = userPreferenceModel.SmartProjectsOption;
		}
		else
		{
			LocalSettings.Settings.InitSmartProjectsOption();
		}
	}

	private static async Task UpdateSortOptions(SyncBean syncBean)
	{
		ObservableCollection<ProjectModel> observableCollection = await ProjectDao.GetAllProjects();
		Dictionary<string, ProjectModel> serverProj = syncBean?.projectProfiles?.ToDictionaryEx((ProjectModel p) => p.id, (ProjectModel p) => p);
		foreach (ProjectModel project in observableCollection)
		{
			if (serverProj != null && serverProj.ContainsKey(project.id))
			{
				ProjectModel projectModel = serverProj[project.id];
				if (projectModel.SortOption != null)
				{
					project.SortOption = projectModel.SortOption;
					if (project.SyncTimeline != null && projectModel.SyncTimeline?.SortOption != null)
					{
						project.SyncTimeline.SortOption = projectModel.SyncTimeline.SortOption;
					}
					if (project.Timeline != null && projectModel.SyncTimeline?.SortOption != null)
					{
						project.Timeline.sortOption = projectModel.SyncTimeline.SortOption;
					}
					await BaseDao<ProjectModel>.UpdateAsync(project);
					CacheManager.UpdateProject(project, notify: false);
					continue;
				}
			}
			if (!project.Isinbox && project.SortOption == null)
			{
				project.SortOption = project.GetSortOption();
				project.SyncTimeline = new TimelineSyncModel(project.SyncTimeline.SortType);
				project.Timeline.sortOption = project.SyncTimeline.SortOption;
				project.sync_status = Constants.SyncStatus.SYNC_INIT.ToString();
				await BaseDao<ProjectModel>.UpdateAsync(project);
				CacheManager.UpdateProject(project, notify: false);
			}
		}
		List<TagModel> list = await TagDao.GetAllTags();
		Dictionary<string, TagModel> serverTag = syncBean?.tags?.ToDictionaryEx((TagModel t) => t.name, (TagModel t) => t);
		foreach (TagModel tag in list)
		{
			if (serverTag != null && serverTag.ContainsKey(tag.name))
			{
				TagModel tagModel = serverTag[tag.name];
				if (tagModel.SortOption != null)
				{
					tag.SortOption = tagModel.SortOption;
					await BaseDao<TagModel>.UpdateAsync(tag);
					CacheManager.UpdateTag(tag, onlyUpdate: true);
					continue;
				}
			}
			if (tag.SortOption == null)
			{
				tag.SortOption = tag.GetSortOption();
				if (tag.status == 2)
				{
					tag.status = 3;
				}
				await BaseDao<TagModel>.UpdateAsync(tag);
				CacheManager.UpdateTag(tag, onlyUpdate: true);
			}
		}
		List<FilterModel> list2 = await FilterDao.GetAllFilters();
		Dictionary<string, FilterModel> serverFilter = syncBean?.filters?.ToDictionaryEx((FilterModel m) => m.id, (FilterModel t) => t);
		foreach (FilterModel filter in list2)
		{
			if (serverFilter != null && serverFilter.ContainsKey(filter.id))
			{
				FilterModel filterModel = serverFilter[filter.id];
				if (filterModel.SortOption != null)
				{
					filter.SortOption = filterModel.SortOption;
					if (filter.SyncTimeline != null && filterModel.SyncTimeline?.SortOption != null)
					{
						filter.SyncTimeline.SortOption = filterModel.SyncTimeline.SortOption;
					}
					if (filter.Timeline != null && filterModel.SyncTimeline?.SortOption != null)
					{
						filter.Timeline.sortOption = filterModel.SyncTimeline.SortOption;
					}
					await BaseDao<FilterModel>.UpdateAsync(filter);
					CacheManager.UpdateFilter(filter);
					continue;
				}
			}
			if (filter.SortOption == null)
			{
				filter.SortOption = filter.GetSortOption();
				if (filter.syncStatus == 2)
				{
					filter.syncStatus = 3;
				}
				filter.SyncTimeline = new TimelineSyncModel(filter.SyncTimeline.SortType);
				filter.Timeline.sortOption = filter.SyncTimeline.SortOption;
				await BaseDao<FilterModel>.UpdateAsync(filter);
				CacheManager.UpdateFilter(filter);
			}
		}
		Dictionary<string, ProjectGroupModel>.ValueCollection values = (await ProjectGroupDao.GetLocalSyncedProjectGroupMap()).Values;
		Dictionary<string, ProjectGroupModel> serverGroup = syncBean?.projectGroups?.ToDictionaryEx((ProjectGroupModel g) => g.id, (ProjectGroupModel g) => g);
		foreach (ProjectGroupModel group in values)
		{
			if (serverGroup != null && serverGroup.ContainsKey(group.id))
			{
				ProjectGroupModel projectGroupModel = serverGroup[group.id];
				if (projectGroupModel.SortOption != null)
				{
					group.SortOption = projectGroupModel.SortOption;
					if (group.SyncTimeline != null && projectGroupModel.SyncTimeline?.SortOption != null)
					{
						group.SyncTimeline.SortOption = projectGroupModel.SyncTimeline.SortOption;
					}
					if (group.Timeline != null && projectGroupModel.SyncTimeline?.SortOption != null)
					{
						group.Timeline.sortOption = projectGroupModel.SyncTimeline.SortOption;
					}
					await BaseDao<ProjectGroupModel>.UpdateAsync(group);
					CacheManager.UpdateProjectGroup(group);
					continue;
				}
			}
			if (group.SortOption == null)
			{
				group.SortOption = group.GetSortOption();
				if (group.sync_status == Constants.SyncStatus.SYNC_DONE.ToString())
				{
					group.sync_status = Constants.SyncStatus.SYNC_INIT.ToString();
				}
				group.SyncTimeline = new TimelineSyncModel(group.SyncTimeline.SortType);
				group.Timeline.sortOption = group.SyncTimeline.SortOption;
				await BaseDao<ProjectGroupModel>.UpdateAsync(group);
				CacheManager.UpdateProjectGroup(group);
			}
		}
	}

	private static async Task UpdateProjectAndTeams(SyncBean syncBean)
	{
		if (syncBean?.projectProfiles != null)
		{
			foreach (ProjectModel project in syncBean.projectProfiles)
			{
				ProjectModel projectModel = await ProjectDao.GetProjectById(project.id);
				if (projectModel != null)
				{
					projectModel.openToTeam = project.openToTeam;
					projectModel.needAudit = project.needAudit;
					projectModel.teamMemberPermission = project.teamMemberPermission;
					CacheManager.UpdateProject(projectModel, notify: false);
					BaseDao<ProjectModel>.UpdateAsync(projectModel);
				}
			}
		}
		List<TeamModel> remoteTeams = await Communicator.GetAllTeams();
		if (remoteTeams == null)
		{
			return;
		}
		List<TeamModel> source = await TeamDao.GetAllTeams();
		foreach (TeamModel team in remoteTeams)
		{
			TeamModel teamModel = source.FirstOrDefault((TeamModel l) => l.id == team.id);
			if (teamModel != null)
			{
				teamModel.needAuditCode = team.needAuditCode;
				teamModel.needAuditUrl = team.needAuditUrl;
				teamModel.industry = team.industry;
				teamModel.scale = team.scale;
				teamModel.logo = team.logo;
				teamModel.teamPro = team.teamPro;
				CacheManager.UpdateTeam(teamModel);
				TeamDao.UpdateTeam(teamModel);
			}
		}
	}

	private static async Task UpdateSkipHabit()
	{
		List<HabitModel> remotes = (await HabitService.PullRemoteHabits(needMerge: false)) ?? new List<HabitModel>();
		List<HabitModel> localHabits = (await HabitDao.GetAllHabits()) ?? new List<HabitModel>();
		foreach (HabitModel habit in localHabits)
		{
			HabitModel habitModel = remotes.FirstOrDefault((HabitModel h) => h.Id == habit.Id);
			if (habitModel != null)
			{
				habit.ExDates = habitModel.ExDates;
			}
		}
		List<SkipHabitModel> list = (await HabitDao.GetAllSkipHabitModels()) ?? new List<SkipHabitModel>();
		if (list.Count > 0)
		{
			Dictionary<string, List<string>> dictionary = new Dictionary<string, List<string>>();
			foreach (SkipHabitModel item in list)
			{
				if (!string.IsNullOrEmpty(item.HabitId) && DateTime.TryParseExact(item.Stamp, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var result) && result > DateTime.Today.AddDays(-30.0))
				{
					if (dictionary.ContainsKey(item.HabitId))
					{
						dictionary[item.HabitId].Add(item.Stamp);
						continue;
					}
					dictionary[item.HabitId] = new List<string> { item.Stamp };
				}
			}
			foreach (string id in dictionary.Keys.ToList())
			{
				HabitModel habitModel2 = localHabits.FirstOrDefault((HabitModel h) => h.Id == id);
				if (habitModel2 != null)
				{
					List<string> list2 = dictionary[habitModel2.Id];
					if (habitModel2.ExDates != null)
					{
						list2.AddRange(habitModel2.ExDates);
						list2 = list2.Distinct().ToList();
					}
					habitModel2.ExDates = list2.ToArray();
					habitModel2.SyncStatus = ((habitModel2.SyncStatus != 0) ? 1 : 0);
				}
			}
		}
		await HabitDao.UpdateAllAsync(localHabits);
	}
}
