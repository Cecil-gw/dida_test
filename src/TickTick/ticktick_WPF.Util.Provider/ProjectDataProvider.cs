using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Tag;

namespace ticktick_WPF.Util.Provider;

public static class ProjectDataProvider
{
	public static List<PtfType> SortedPtfTypes = new List<PtfType>
	{
		PtfType.Project,
		PtfType.Tag,
		PtfType.Filter,
		PtfType.Subscribe
	};

	public static async Task<List<ProjectItemViewModel>> GetProjectData(bool ignoreCollapse = false)
	{
		return await AddPtf(await AddSpecialProjects(), ignoreCollapse);
	}

	private static async Task<List<ProjectItemViewModel>> AddSpecialProjects()
	{
		List<ProjectItemViewModel> specialProjects = new List<ProjectItemViewModel>();
		AllProject project = new AllProject();
		await TryAddSpecialProject("SmartListAll", project, "_special_id_all", specialProjects);
		TodayProject project2 = new TodayProject();
		await TryAddSpecialProject("SmartListToday", project2, "_special_id_today", specialProjects);
		TomorrowProject project3 = new TomorrowProject();
		await TryAddSpecialProject("SmartListTomorrow", project3, "_special_id_tomorrow", specialProjects);
		WeekProject project4 = new WeekProject();
		await TryAddSpecialProject("SmartList7Day", project4, "_special_id_week", specialProjects);
		AssignProject project5 = new AssignProject();
		await TryAddSpecialProject("SmartListForMe", project5, "_special_id_assigned", specialProjects);
		InboxProject inboxProject = new InboxProject();
		await TryAddInboxProject("SmartListInbox", inboxProject, inboxProject.Id, specialProjects);
		SummaryProject project6 = new SummaryProject();
		TryAddSpecialProject("SmartListSummary", project6, "", specialProjects);
		if (specialProjects.Count > 0)
		{
			specialProjects.Add(new LineViewModel
			{
				SortOrder = 8001L
			});
		}
		specialProjects.Add(new PtfAllViewModel(PtfType.Project)
		{
			SortOrder = 8002 + SortedPtfTypes.IndexOf(PtfType.Project)
		});
		if (LocalSettings.Settings.SmartListTag != 1)
		{
			specialProjects.Add(new PtfAllViewModel(PtfType.Tag)
			{
				SortOrder = 8002 + SortedPtfTypes.IndexOf(PtfType.Tag)
			});
		}
		if (LocalSettings.Settings.ShowCustomSmartList != 1)
		{
			specialProjects.Add(new PtfAllViewModel(PtfType.Filter)
			{
				SortOrder = 8002 + SortedPtfTypes.IndexOf(PtfType.Filter)
			});
		}
		TryAddCalendars(specialProjects, 8002 + SortedPtfTypes.IndexOf(PtfType.Subscribe));
		if (LocalSettings.Settings.SmartListTrash == 0 || LocalSettings.Settings.SmartListComplete == 0 || LocalSettings.Settings.SmartListSummary == 0)
		{
			LineViewModel item = new LineViewModel
			{
				SortOrder = 10000L
			};
			specialProjects.Add(item);
		}
		CompletedProject project7 = new CompletedProject();
		TryAddSpecialProject("SmartListComplete", project7, "", specialProjects);
		AbandonedProject project8 = new AbandonedProject();
		TryAddSpecialProject("SmartListAbandoned", project8, "_special_id_abandoned", specialProjects);
		TrashProject project9 = new TrashProject();
		TryAddSpecialProject("SmartListTrash", project9, "", specialProjects);
		return specialProjects.OrderBy((ProjectItemViewModel p) => p.SortOrder).ToList();
	}

	public static void ReSortPtfTypes()
	{
		Dictionary<PtfType, long> sortOrders = LocalSettings.Settings.SmartProjects?.Where((SmartProjectConf p) => SmartProjectConf.ProjectSectionName.Contains(p.name)).ToDictionary((SmartProjectConf p) => GetPtfTypeByName(p.name), (SmartProjectConf p) => p.order ?? 3);
		if (sortOrders == null)
		{
			return;
		}
		SortedPtfTypes.Sort(delegate(PtfType a, PtfType b)
		{
			if (sortOrders.ContainsKey(a) && sortOrders.ContainsKey(b))
			{
				return sortOrders[a].CompareTo(sortOrders[b]);
			}
			return (!sortOrders.ContainsKey(a) && sortOrders.ContainsKey(b)) ? 1 : 0;
		});
		static PtfType GetPtfTypeByName(string name)
		{
			return name switch
			{
				"project" => PtfType.Project, 
				"tag" => PtfType.Tag, 
				"filter" => PtfType.Filter, 
				"subscribedCalendar" => PtfType.Subscribe, 
				_ => PtfType.Project, 
			};
		}
	}

	public static void SetNewSortOrders(IEnumerable<PtfAllViewModel> ptfs)
	{
		long num = 0L;
		List<SmartProjectConf> list = LocalSettings.Settings.SmartProjects?.Where((SmartProjectConf p) => SmartProjectConf.ProjectSectionName.Contains(p.name)).ToList();
		foreach (PtfAllViewModel ptf in ptfs)
		{
			string name = GetNameByPtfType(ptf.Type);
			SmartProjectConf smartProjectConf = list?.FirstOrDefault((SmartProjectConf p) => p.name == name);
			if (smartProjectConf == null)
			{
				LocalSettings.Settings.AddSmartProjectConf(new SmartProjectConf
				{
					name = name,
					order = num
				});
			}
			else
			{
				smartProjectConf.order = num;
			}
			num++;
		}
		ReSortPtfTypes();
		SettingsHelper.PushLocalSettings();
		static string GetNameByPtfType(PtfType type)
		{
			return type switch
			{
				PtfType.Project => "project", 
				PtfType.Tag => "tag", 
				PtfType.Filter => "filter", 
				PtfType.Subscribe => "subscribedCalendar", 
				_ => "project", 
			};
		}
	}

	private static void TryAddCalendars(ICollection<ProjectItemViewModel> projects, int startSortOrder)
	{
		List<BindCalendarAccountModel> bindCalendarAccounts = CacheManager.GetBindCalendarAccounts();
		List<BindCalendarModel> bindCalendars = CacheManager.GetBindCalendars();
		List<BindCalendarAccountModel> source = bindCalendarAccounts.Where((BindCalendarAccountModel a) => bindCalendars.Any((BindCalendarModel cal) => cal.AccountId == a.Id && cal.Show != "hidden")).ToList();
		List<CalendarSubscribeProfileModel> source2 = (from profile in CacheManager.GetSubscribeCalendars()
			where profile.Show != "hidden"
			select profile).ToList();
		if ((source.Any() || source2.Any()) && UserDao.IsPro())
		{
			projects.Add(new PtfAllViewModel(PtfType.Subscribe)
			{
				SortOrder = startSortOrder
			});
		}
	}

	public static void AddSubscribeCalendar(IList<ProjectItemViewModel> itemList, int i)
	{
		List<BindCalendarAccountModel> bindCalendarAccounts = CacheManager.GetBindCalendarAccounts();
		List<BindCalendarModel> bindCalendars = CacheManager.GetBindCalendars();
		bindCalendarAccounts.Sort(delegate(BindCalendarAccountModel a, BindCalendarAccountModel b)
		{
			int accountType = SubscribeCalendarHelper.GetAccountType(a);
			int accountType2 = SubscribeCalendarHelper.GetAccountType(b);
			return (accountType == accountType2 && a.CreatedTime.HasValue) ? a.CreatedTime.Value.CompareTo(b.CreatedTime) : accountType.CompareTo(accountType2);
		});
		List<BindCalendarAccountModel> list = bindCalendarAccounts.Where(delegate(BindCalendarAccountModel a)
		{
			List<BindCalendarModel> source = bindCalendars.Where((BindCalendarModel cal) => cal.AccountId == a.Id).ToList();
			return !source.Any() || source.Any((BindCalendarModel cal) => cal.Show != "hidden");
		}).ToList();
		List<CalendarSubscribeProfileModel> list2 = (from profile in CacheManager.GetSubscribeCalendars()
			where profile.Show != "hidden"
			select profile).ToList();
		list2.Sort((CalendarSubscribeProfileModel a, CalendarSubscribeProfileModel b) => a.CreatedTime?.CompareTo(b.CreatedTime) ?? (-1));
		List<ProjectItemViewModel> list3 = new List<ProjectItemViewModel>();
		foreach (BindCalendarAccountModel item in list)
		{
			list3.Add(new BindCalendarAccountProjectViewModel(item));
		}
		foreach (CalendarSubscribeProfileModel item2 in list2)
		{
			list3.Add(new SubscribeCalendarProjectViewModel(item2));
		}
		foreach (ProjectItemViewModel item3 in list3.OrderBy((ProjectItemViewModel x) => x.SortOrder))
		{
			itemList.Insert(i++, item3);
		}
	}

	public static void AddNormalProjects(IList<ProjectItemViewModel> itemList, int insertIndex)
	{
		List<ProjectItemViewModel> list = AssembleProjects().ToList();
		if (list.Any())
		{
			foreach (ProjectItemViewModel item in list)
			{
				itemList.Insert(insertIndex++, item);
			}
			return;
		}
		itemList.Insert(insertIndex, new EmptyProjectViewModel
		{
			TeamId = string.Empty,
			Title = Utils.GetString("NoProjectHint")
		});
	}

	private static Dictionary<string, List<ProjectModel>> GroupClosedProject(List<ProjectModel> projects)
	{
		Dictionary<string, List<ProjectModel>> dictionary = new Dictionary<string, List<ProjectModel>>();
		if (projects != null && projects.Any())
		{
			foreach (string teamId in projects.Select((ProjectModel p) => p.teamId).Distinct().ToList())
			{
				string text = teamId;
				if (string.IsNullOrEmpty(text))
				{
					text = "c1a7e08345e444dea187e21a692f0d7a";
				}
				List<ProjectModel> list = projects.Where((ProjectModel p) => p.teamId == teamId).ToList();
				list.Sort((ProjectModel a, ProjectModel b) => a.sortOrder.CompareTo(b.sortOrder));
				if (!dictionary.ContainsKey(text))
				{
					dictionary.Add(text, list);
				}
				else
				{
					dictionary[text].AddRange(list);
				}
			}
		}
		return dictionary;
	}

	private static List<ClosedProjectGroupViewModel> GetClosedGroups(Dictionary<string, List<ProjectModel>> groups)
	{
		List<ClosedProjectGroupViewModel> list = new List<ClosedProjectGroupViewModel>();
		foreach (KeyValuePair<string, List<ProjectModel>> group in groups)
		{
			ProjectGroupModel projectGroupModel = new ProjectGroupModel
			{
				id = "_special_id_closed" + group.Key,
				name = Utils.GetString("ArchivedLists"),
				teamId = group.Key,
				open = (LocalSettings.Settings.ClosedSectionStatus == "True")
			};
			List<ProjectItemViewModel> list2 = new List<ProjectItemViewModel>();
			foreach (ProjectModel item2 in group.Value)
			{
				item2.groupId = projectGroupModel.id;
				list2.Add(new SubProjectViewModel(item2));
			}
			ClosedProjectGroupViewModel item = new ClosedProjectGroupViewModel(projectGroupModel)
			{
				TeamId = group.Key,
				Children = list2
			};
			list.Add(item);
		}
		return list;
	}

	private static IEnumerable<ProjectItemViewModel> AssembleProjects()
	{
		List<ProjectModel> projects = (from p in CacheManager.GetProjectsWithoutInbox()
			where !p.closed.HasValue || !p.closed.Value
			select p).ToList();
		List<ProjectGroupModel> projectGroups = CacheManager.GetProjectGroups();
		List<string> groupsId = projectGroups?.Select((ProjectGroupModel g) => g.id).ToList();
		List<TeamModel> teams = CacheManager.GetTeams();
		if (!UserManager.IsTeamUser())
		{
			teams = new List<TeamModel>();
		}
		List<ProjectModel> projects2 = (from p in CacheManager.GetProjectsWithoutInbox()
			where p.closed.HasValue && p.closed.Value
			select p).ToList();
		List<ClosedProjectGroupViewModel> closedGroups = GetClosedGroups(GroupClosedProject(projects2));
		List<ProjectModel> orphanProjects = projects.Where((ProjectModel p) => string.IsNullOrEmpty(p.groupId) || p.groupId.ToString().ToUpper() == "NONE" || (groupsId != null && !groupsId.Contains(p.groupId))).ToList();
		List<ProjectGroupViewModel> groupModels = projectGroups?.Select((ProjectGroupModel group) => new ProjectGroupViewModel(group)
		{
			Children = new List<ProjectItemViewModel>((from p in projects
				where p.groupId == @group.id
				select new SubProjectViewModel(p)).ToList())
		}).ToList();
		if (groupModels != null && groupModels.Any())
		{
			foreach (ProjectGroupViewModel item in groupModels)
			{
				if (item.Children != null && item.Children.Count > 1)
				{
					item.Children = (from p in item.Children
						orderby p.SortOrder, p.Title descending
						select p).ToList();
				}
				item.Children?.Add(new EmptySubViewModel(item.ProjectGroup));
			}
		}
		else
		{
			groupModels = new List<ProjectGroupViewModel>();
		}
		TeamGroupViewModel teamGroupViewModel = new TeamGroupViewModel(new TeamModel
		{
			id = "c1a7e08345e444dea187e21a692f0d7a",
			name = Utils.GetString("Personal"),
			open = LocalSettings.Settings.ExpandPersonalSection
		})
		{
			Id = "c1a7e08345e444dea187e21a692f0d7a",
			Children = (from p in new List<ProjectItemViewModel>(groupModels.Where((ProjectGroupViewModel @group) => string.IsNullOrEmpty(@group.TeamId))).Union(new List<ProjectItemViewModel>(new List<ProjectItemViewModel>(from p in orphanProjects
					where string.IsNullOrEmpty(p.teamId) || teams.All((TeamModel t) => t.id != p.teamId)
					select new NormalProjectViewModel(p))))
				orderby p.SortOrder, p.Title descending
				select p).ToList()
		};
		if (!UserDao.IsPro())
		{
			int num = CacheManager.GetProjectsWithoutInbox().Count((ProjectModel p) => !p.delete_status);
			if (num >= 8)
			{
				teamGroupViewModel.NotifyMessage = Utils.GetString("ListCountUsed") + (Utils.IsCn() ? "" : " ") + num + "/9";
			}
		}
		ClosedProjectGroupViewModel closedProjectGroupViewModel = closedGroups.FirstOrDefault((ClosedProjectGroupViewModel group) => group.TeamId == "c1a7e08345e444dea187e21a692f0d7a");
		if (closedProjectGroupViewModel != null)
		{
			teamGroupViewModel.Children.Add(closedProjectGroupViewModel);
		}
		if (teams.Count == 0)
		{
			teamGroupViewModel.Open = true;
			List<ProjectItemViewModel> list = ExpandItems(new List<TeamGroupViewModel> { teamGroupViewModel });
			if (list.Any())
			{
				list.RemoveAt(0);
				return list;
			}
		}
		List<TeamModel> list2 = teams.Where((TeamModel team) => !team.expired).ToList();
		list2.Sort((TeamModel a, TeamModel b) => b.joinedTime.CompareTo(a.joinedTime));
		List<TeamGroupViewModel> list3 = list2.Select((TeamModel team) => new TeamGroupViewModel(team)
		{
			Id = team.id,
			Open = team.open,
			Children = (from p in new List<ProjectItemViewModel>(groupModels.Where((ProjectGroupViewModel @group) => @group.TeamId == team.id)).Union(new List<ProjectItemViewModel>(new List<ProjectItemViewModel>(from p in orphanProjects
					where p.teamId == team.id
					select new NormalProjectViewModel(p))))
				orderby p.SortOrder, p.Title descending
				select p).Union(new List<ProjectItemViewModel>(new List<ProjectItemViewModel>(closedGroups.Where((ClosedProjectGroupViewModel group) => group.TeamId == team.id)))).ToList()
		}).ToList();
		list3.Add(teamGroupViewModel);
		List<TeamModel> list4 = teams.Where((TeamModel team) => team.expired).ToList();
		list4.Sort((TeamModel a, TeamModel b) => b.expiredDate.CompareTo(a.expiredDate));
		list3.AddRange(list4.Select((TeamModel team) => new TeamGroupViewModel(team)
		{
			Id = team.id,
			Open = team.open,
			Children = (from p in new List<ProjectItemViewModel>(groupModels.Where((ProjectGroupViewModel @group) => @group.TeamId == team.id)).Union(new List<ProjectItemViewModel>(new List<ProjectItemViewModel>(from p in orphanProjects
					where p.teamId == team.id
					select new NormalProjectViewModel(p))))
				orderby p.SortOrder, p.Title descending
				select p).Union(new List<ProjectItemViewModel>(new List<ProjectItemViewModel>(closedGroups.Where((ClosedProjectGroupViewModel group) => group.TeamId == team.id)))).ToList()
		}).ToList());
		foreach (TeamGroupViewModel item2 in list3)
		{
			SetTeamId(item2, item2.Id);
			if (!item2.Children.Any())
			{
				item2.Children.Add(new EmptyProjectViewModel
				{
					TeamId = item2.Team.id,
					Title = Utils.GetString((item2.TeamId == "c1a7e08345e444dea187e21a692f0d7a") ? "NoProjectHint" : "NoTeamProjectHint")
				});
			}
		}
		return ExpandItems(list3);
	}

	private static void SetTeamId(ProjectItemViewModel model, string teamId)
	{
		if (model == null)
		{
			return;
		}
		model.TeamId = teamId;
		if (model.Children == null)
		{
			return;
		}
		foreach (ProjectItemViewModel child in model.Children)
		{
			SetTeamId(child, teamId);
		}
	}

	private static List<ProjectItemViewModel> ExpandItems(List<TeamGroupViewModel> items)
	{
		List<ProjectItemViewModel> list = new List<ProjectItemViewModel>();
		if (items != null && items.Any())
		{
			foreach (TeamGroupViewModel item in items)
			{
				AddItems(item, list);
			}
		}
		return list;
	}

	private static void AddItems(ProjectItemViewModel item, List<ProjectItemViewModel> models)
	{
		models.Add(item);
		if (item.Children == null || !item.Children.Any() || !item.Open)
		{
			return;
		}
		foreach (ProjectItemViewModel child in item.Children)
		{
			AddItems(child, models);
		}
	}

	private static async Task TryAddSpecialProject(string index, SmartProject project, string smartListId, ICollection<ProjectItemViewModel> projectList)
	{
		switch ((ShowStatus)LocalSettings.Settings[index])
		{
		case ShowStatus.Hide:
			return;
		case ShowStatus.Auto:
		{
			SmartProjectIdentity smartProjectIdentity = SmartProjectIdentity.BuildSmartProject(smartListId);
			if (smartProjectIdentity is AbandonedProjectIdentity)
			{
				if (TaskViewModelHelper.GetClosedModels(null, DateTime.Now, null, 1, showCheckItem: false, -1).Count <= 0)
				{
					return;
				}
			}
			else if (await ProjectAndTaskIdsCache.GetCountByIdentity(smartProjectIdentity, withNote: true) <= 0)
			{
				return;
			}
			break;
		}
		}
		int count = TaskCountCache.TryGetCount(smartListId);
		projectList.Add(new SmartProjectViewModel(project)
		{
			Count = count
		});
	}

	private static async Task TryAddInboxProject(string index, SmartProject project, string inboxId, ICollection<ProjectItemViewModel> projectList)
	{
		switch ((ShowStatus)LocalSettings.Settings[index])
		{
		case ShowStatus.Hide:
			return;
		case ShowStatus.Auto:
			if (await ProjectAndTaskIdsCache.GetCountByIdentity(ProjectIdentity.CreateInboxProject(), withNote: true) <= 0)
			{
				return;
			}
			break;
		}
		int count = TaskCountCache.TryGetCount(inboxId);
		projectList.Add(new InboxProjectViewModel(project)
		{
			Count = count
		});
	}

	public static async Task<List<ProjectItemViewModel>> AddPtf(List<ProjectItemViewModel> itemList, bool ignoreCollapse = false)
	{
		ProjectItemViewModel projectItemViewModel = itemList.FirstOrDefault((ProjectItemViewModel p) => p is PtfAllViewModel ptfAllViewModel && ptfAllViewModel.IsProject);
		int num = itemList.IndexOf(projectItemViewModel);
		if (num >= 0 && CacheManager.GetTeams().Count > 0)
		{
			itemList.RemoveAt(num);
			AddNormalProjects(itemList, num);
		}
		else if (projectItemViewModel != null && num >= 0)
		{
			if (!UserDao.IsPro())
			{
				int num2 = CacheManager.GetProjectsWithoutInbox().Count((ProjectModel p) => !p.delete_status);
				if (num2 >= 8)
				{
					projectItemViewModel.NotifyMessage = Utils.GetString("ListCountUsed").Trim() + (Utils.IsCn() ? "" : " ") + num2 + "/9";
				}
			}
			if (LocalSettings.Settings.AllProjectOpened || ignoreCollapse)
			{
				AddNormalProjects(itemList, num + 1);
			}
		}
		int num3 = itemList.IndexOf(itemList.FirstOrDefault((ProjectItemViewModel p) => p is PtfAllViewModel ptfAllViewModel && ptfAllViewModel.IsTag));
		if ((num3 >= 0 && LocalSettings.Settings.AllTagOpened) || ignoreCollapse)
		{
			await AddTagProject(itemList, num3 + 1);
		}
		int num4 = itemList.IndexOf(itemList.FirstOrDefault((ProjectItemViewModel p) => p is PtfAllViewModel ptfAllViewModel && ptfAllViewModel.IsFilter));
		if (num4 >= 0 && LocalSettings.Settings.AllFilterOpened)
		{
			AddFilterProject(itemList, num4 + 1);
		}
		int num5 = itemList.IndexOf(itemList.FirstOrDefault((ProjectItemViewModel p) => p is PtfAllViewModel ptfAllViewModel && ptfAllViewModel.IsSubscribe));
		if (num5 >= 0 && LocalSettings.Settings.AllSubscribeOpened)
		{
			AddSubscribeCalendar(itemList, num5 + 1);
		}
		AssembleTaskCount(itemList);
		return itemList;
	}

	private static void AssembleTaskCount(IEnumerable<ProjectItemViewModel> models)
	{
		foreach (ProjectItemViewModel model in models)
		{
			model.EnsureCountLoaded();
		}
	}

	public static bool IsTagCategory(ProjectItemViewModel model)
	{
		if (!(model is TagProjectViewModel) && !(model is EmptySubViewModel { IsEmptyTag: not false }) && !(model is EmptyTagViewModel))
		{
			return model is ShareTagAllViewModel;
		}
		return true;
	}

	public static bool IsShareTagCategory(ProjectItemViewModel model)
	{
		if (!(model is TagProjectViewModel) || !model.InSubSection)
		{
			if (model is EmptySubViewModel { IsEmptyTag: not false })
			{
				return model.InSubSection;
			}
			return false;
		}
		return true;
	}

	public static bool IsFilterCategory(ProjectItemViewModel model)
	{
		if (!(model is FilterProjectViewModel))
		{
			return model is EmptyFilterViewModel;
		}
		return true;
	}

	public static bool IsSubscribeCategory(ProjectItemViewModel model)
	{
		if (!(model is SubscribeCalendarProjectViewModel))
		{
			return model is BindCalendarAccountProjectViewModel;
		}
		return true;
	}

	public static bool IsProjectCategory(ProjectItemViewModel model)
	{
		if (!(model is NormalProjectViewModel) && !(model is ProjectGroupViewModel) && !(model is EmptyProjectViewModel { Type: PtfType.Project }))
		{
			if (model is EmptySubViewModel emptySubViewModel)
			{
				return !emptySubViewModel.IsEmptyTag;
			}
			return false;
		}
		return true;
	}

	public static async Task AddTagProject(IList<ProjectItemViewModel> itemList, int insertIndex)
	{
		List<TagModel> tags = TagDataHelper.GetTags();
		if (tags != null && tags.Count > 0)
		{
			List<TagModel> sortTags = new List<TagModel>();
			HashSet<string> tagNames = new HashSet<string>(tags.Select((TagModel t) => t.name));
			foreach (TagModel tag in tags)
			{
				if (tag.parent == tag.name)
				{
					tag.parent = null;
				}
				if (LocalSettings.Settings.SmartListTag != 2 || await ProjectAndTaskIdsCache.GetCountByIdentity(new TagProjectIdentity(tag), withNote: true) > 0)
				{
					sortTags.Add(tag);
				}
			}
			if (sortTags.Count <= 0)
			{
				return;
			}
			List<TagModel> source = sortTags.Where((TagModel tagModel) => string.IsNullOrEmpty(tagModel.parent) || !tagNames.Contains(tagModel.parent)).ToList();
			List<TagModel> list = source.Where((TagModel t) => t.type != 2).ToList();
			List<TagModel> list2 = source.Where((TagModel t) => t.type == 2).ToList();
			if (list.Count == 0)
			{
				itemList.Insert(insertIndex++, new EmptyTagViewModel
				{
					TeamId = string.Empty,
					Title = Utils.GetString("NoTagHint")
				});
			}
			foreach (TagModel tag2 in list)
			{
				TagProjectViewModel tagProjectViewModel = new TagProjectViewModel(tag2);
				itemList.Insert(insertIndex++, tagProjectViewModel);
				if (!tagProjectViewModel.IsParent)
				{
					continue;
				}
				List<TagModel> source2 = sortTags.Where((TagModel t) => t.parent == tag2.name).ToList();
				tagProjectViewModel.Children = ((IEnumerable<TagModel>)source2).Select((Func<TagModel, ProjectItemViewModel>)((TagModel t) => new TagProjectViewModel(t))).ToList();
				tagProjectViewModel.Children.Add(new EmptySubViewModel(tag2));
				if (tag2.collapsed)
				{
					continue;
				}
				foreach (ProjectItemViewModel child in tagProjectViewModel.Children)
				{
					itemList.Insert(insertIndex++, child);
				}
			}
			if (list2.Count <= 0)
			{
				return;
			}
			ShareTagAllViewModel item = new ShareTagAllViewModel();
			itemList.Insert(insertIndex++, item);
			foreach (TagModel tag3 in list2)
			{
				TagProjectViewModel tagProjectViewModel2 = new TagProjectViewModel(tag3)
				{
					InSubSection = true
				};
				itemList.Insert(insertIndex++, tagProjectViewModel2);
				if (!tagProjectViewModel2.IsParent)
				{
					continue;
				}
				List<TagModel> source3 = sortTags.Where((TagModel t) => t.parent == tag3.name).ToList();
				tagProjectViewModel2.Children = ((IEnumerable<TagModel>)source3).Select((Func<TagModel, ProjectItemViewModel>)((TagModel t) => new TagProjectViewModel(t)
				{
					InSubSection = true
				})).ToList();
				tagProjectViewModel2.Children.Add(new EmptySubViewModel(tag3)
				{
					InSubSection = true
				});
				if (tag3.collapsed)
				{
					continue;
				}
				foreach (ProjectItemViewModel child2 in tagProjectViewModel2.Children)
				{
					itemList.Insert(insertIndex++, child2);
				}
			}
		}
		else
		{
			itemList.Insert(insertIndex, new EmptyTagViewModel
			{
				TeamId = string.Empty,
				Title = Utils.GetString("NoTagHint")
			});
		}
	}

	public static async Task AddShareTagProject(IList<ProjectItemViewModel> itemList, int insertIndex)
	{
		List<TagModel> list = TagDataHelper.GetTags(sort: false)?.Where((TagModel t) => t.type == 2).ToList();
		if (list != null && list.Count > 0)
		{
			List<TagModel> sortTags = new List<TagModel>();
			HashSet<string> tagNames = new HashSet<string>(list.Select((TagModel t) => t.name));
			foreach (TagModel tag in list)
			{
				if (tag.parent == tag.name)
				{
					tag.parent = null;
				}
				if (LocalSettings.Settings.SmartListTag != 2 || await ProjectAndTaskIdsCache.GetCountByIdentity(new TagProjectIdentity(tag), withNote: true) > 0)
				{
					sortTags.Add(tag);
				}
			}
			if (sortTags.Count <= 0)
			{
				return;
			}
			foreach (TagModel tag2 in sortTags.Where((TagModel tagModel) => string.IsNullOrEmpty(tagModel.parent) || !tagNames.Contains(tagModel.parent)).ToList())
			{
				TagProjectViewModel tagProjectViewModel = new TagProjectViewModel(tag2)
				{
					InSubSection = true
				};
				itemList.Insert(insertIndex++, tagProjectViewModel);
				if (!tagProjectViewModel.IsParent)
				{
					continue;
				}
				List<TagModel> source = sortTags.Where((TagModel t) => t.parent == tag2.name).ToList();
				tagProjectViewModel.Children = ((IEnumerable<TagModel>)source).Select((Func<TagModel, ProjectItemViewModel>)((TagModel t) => new TagProjectViewModel(t)
				{
					InSubSection = true
				})).ToList();
				tagProjectViewModel.Children.Add(new EmptySubViewModel(tag2)
				{
					InSubSection = true
				});
				if (tag2.collapsed)
				{
					continue;
				}
				foreach (ProjectItemViewModel child in tagProjectViewModel.Children)
				{
					itemList.Insert(insertIndex++, child);
				}
			}
		}
		else
		{
			itemList.Insert(insertIndex, new EmptyTagViewModel
			{
				TeamId = string.Empty,
				Title = Utils.GetString("NoTagHint")
			});
		}
	}

	public static void AddFilterProject(IList<ProjectItemViewModel> itemList, int insertIndex)
	{
		List<FilterModel> list = (from f in CacheManager.GetFilters()
			where f.deleted != 1
			orderby f.sortOrder
			select f).ToList();
		if (list.Any())
		{
			foreach (FilterModel item2 in list)
			{
				FilterProjectViewModel item = new FilterProjectViewModel(item2);
				itemList.Insert(insertIndex++, item);
			}
			return;
		}
		itemList.Insert(insertIndex, new EmptyFilterViewModel
		{
			TeamId = string.Empty,
			Title = Utils.GetString("NoFilterHint")
		});
	}
}
