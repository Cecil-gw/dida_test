using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ticktick_WPF.Cache;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Util.Drag;

public static class ProjectDragHelper
{
	private class SortIdentity
	{
		public List<SortIdentity> Children;

		public string Identity;

		public string Name;

		public long SortOrder;

		public long TeamIndex;
	}

	public static bool CanSwapItems(ProjectItemViewModel current, ProjectItemViewModel next, bool isUpDowm)
	{
		if (current != null && next != null)
		{
			if (!(current is SmartProjectViewModel smart))
			{
				if (!(current is FilterProjectViewModel))
				{
					if (current is TagProjectViewModel tagProjectViewModel)
					{
						if (next.InSubSection != tagProjectViewModel.InSubSection)
						{
							return false;
						}
						if (!tagProjectViewModel.IsParent)
						{
							if (!(next is TagProjectViewModel))
							{
								return next is EmptySubViewModel;
							}
							return true;
						}
						if (!(next is EmptySubViewModel && isUpDowm))
						{
							if (next is TagProjectViewModel tagProjectViewModel2)
							{
								if (tagProjectViewModel2.IsSubItem || tagProjectViewModel2.IsParent)
								{
									if (tagProjectViewModel2.IsParent)
									{
										if (isUpDowm)
										{
											return !tagProjectViewModel2.Open;
										}
										return true;
									}
									return false;
								}
								return true;
							}
							return false;
						}
						return true;
					}
					if (current is ProjectGroupViewModel)
					{
						if (!(next is EmptySubViewModel && isUpDowm) && (!(next is ProjectGroupViewModel) || (isUpDowm && next.Open)))
						{
							if (next is NormalProjectViewModel normalProjectViewModel)
							{
								return !normalProjectViewModel.IsSubItem;
							}
							return false;
						}
						return true;
					}
					if (current is PtfAllViewModel)
					{
						return next is PtfAllViewModel;
					}
					if (current is SubscribeCalendarProjectViewModel || current is BindCalendarAccountProjectViewModel)
					{
						if (!(next is BindCalendarAccountProjectViewModel))
						{
							return next is SubscribeCalendarProjectViewModel;
						}
						return true;
					}
				}
				else if (next is FilterProjectViewModel)
				{
					return true;
				}
			}
			else if (next is SmartProjectViewModel smart2)
			{
				if (CanSmartSwap(smart))
				{
					return CanSmartSwap(smart2);
				}
				return false;
			}
			if (IsProjectDragCategory(current) && IsProjectDragCategory(next))
			{
				return true;
			}
		}
		return false;
	}

	private static bool CanSmartSwap(SmartProjectViewModel smart)
	{
		if (!(smart.Project is AllProject) && !(smart.Project is TodayProject) && !(smart.Project is TomorrowProject) && !(smart.Project is WeekProject) && !(smart.Project is AssignProject) && !(smart.Project is InboxProject))
		{
			return smart.Project is SummaryProject;
		}
		return true;
	}

	private static bool IsProjectDragCategory(ProjectItemViewModel model)
	{
		if (model != null)
		{
			if (!(model is NormalProjectViewModel) && !(model is EmptySubViewModel))
			{
				if (model is ProjectGroupViewModel)
				{
					return !(model is ClosedProjectGroupViewModel);
				}
				return false;
			}
			return true;
		}
		return false;
	}

	public static long GetNewSortOrder(string groupId, string teamId)
	{
		SortIdentity sortIdentity = GetPrimaryIdentities(teamId).FirstOrDefault((SortIdentity p) => p.Identity == groupId);
		if (sortIdentity != null && sortIdentity.Children.Any())
		{
			return sortIdentity.Children[0].SortOrder - 268435456;
		}
		return -1L;
	}

	public static long GetNewSortOrder(bool upDown, string groupId, string projectId, string teamId)
	{
		SortIdentity sortIdentity = GetPrimaryIdentities(teamId).FirstOrDefault((SortIdentity p) => p.Identity == groupId);
		SortIdentity sortIdentity2 = sortIdentity?.Children.FirstOrDefault((SortIdentity p) => p.Identity == projectId);
		if (sortIdentity2 != null)
		{
			int num = sortIdentity.Children.IndexOf(sortIdentity2);
			bool flag = num == 0;
			bool flag2 = num == sortIdentity.Children.Count - 1;
			if (upDown)
			{
				if (flag2)
				{
					return sortIdentity.Children.Last().SortOrder + 268435456;
				}
				long sortOrder = sortIdentity.Children[num].SortOrder;
				long sortOrder2 = sortIdentity.Children[num + 1].SortOrder;
				return sortOrder + (sortOrder2 - sortOrder) / 2;
			}
			if (flag)
			{
				return sortIdentity.Children.First().SortOrder - 268435456;
			}
			long sortOrder3 = sortIdentity.Children[num].SortOrder;
			long sortOrder4 = sortIdentity.Children[num - 1].SortOrder;
			return sortOrder3 + (sortOrder4 - sortOrder3) / 2;
		}
		return GetNewSortOrder(upDown, projectId, teamId);
	}

	public static long GetNewSortOrder(bool upDown, string id, string teamId)
	{
		List<SortIdentity> primaryIdentities = GetPrimaryIdentities(teamId);
		SortIdentity sortIdentity = primaryIdentities.FirstOrDefault((SortIdentity p) => p.Identity == id);
		if (sortIdentity != null)
		{
			int num = primaryIdentities.IndexOf(sortIdentity);
			if (upDown)
			{
				if (num == primaryIdentities.Count - 1)
				{
					return primaryIdentities.Last().SortOrder + 268435456;
				}
				long sortOrder = primaryIdentities[num].SortOrder;
				long sortOrder2 = primaryIdentities[num + 1].SortOrder;
				return sortOrder + (sortOrder2 - sortOrder) / 2;
			}
			if (num == 0)
			{
				return primaryIdentities.First().SortOrder - 268435456;
			}
			long sortOrder3 = primaryIdentities[num].SortOrder;
			long sortOrder4 = primaryIdentities[num - 1].SortOrder;
			return sortOrder3 + (sortOrder4 - sortOrder3) / 2;
		}
		return -1L;
	}

	public static long GetNewSortOrder(bool upDown, string groupId, bool isGroupAll, string teamId)
	{
		List<SortIdentity> primaryIdentities = GetPrimaryIdentities(teamId);
		List<SortIdentity> sortIdentities = GetSortIdentities(teamId);
		if (!isGroupAll)
		{
			return GetSortOrderOnGroup(upDown, groupId, primaryIdentities);
		}
		return GetSortOrderOnGroupAll(upDown, groupId, primaryIdentities, sortIdentities);
	}

	private static long GetSortOrderOnGroup(bool upDown, string groupId, IList<SortIdentity> primary)
	{
		if (upDown)
		{
			SortIdentity sortIdentity = primary.FirstOrDefault((SortIdentity v) => v.Identity == groupId);
			if (sortIdentity != null)
			{
				if (sortIdentity.Children.Count > 0)
				{
					return sortIdentity.Children[0].SortOrder - 268435456;
				}
				return sortIdentity.SortOrder + 268435456;
			}
		}
		else
		{
			SortIdentity sortIdentity2 = primary.FirstOrDefault((SortIdentity v) => v.Identity == groupId);
			if (sortIdentity2 != null)
			{
				int num = primary.IndexOf(sortIdentity2);
				if (num <= 0)
				{
					return sortIdentity2.SortOrder - 268435456;
				}
				return primary[num - 1].SortOrder + (primary[num].SortOrder - primary[num - 1].SortOrder) / 2;
			}
		}
		return -1L;
	}

	private static long GetSortOrderOnGroupAll(bool upDown, string groupId, List<SortIdentity> primary, List<SortIdentity> full)
	{
		if (upDown)
		{
			SortIdentity sortIdentity = primary.FirstOrDefault((SortIdentity v) => v.Identity == groupId);
			if (sortIdentity != null)
			{
				int num = primary.IndexOf(sortIdentity);
				if (num + 1 >= primary.Count)
				{
					return sortIdentity.SortOrder + 268435456;
				}
				return primary[num].SortOrder + (primary[num + 1].SortOrder - primary[num].SortOrder) / 2;
			}
		}
		else
		{
			SortIdentity sortIdentity2 = full.FirstOrDefault((SortIdentity v) => v.Identity == groupId);
			if (sortIdentity2 != null)
			{
				int num2 = full.IndexOf(sortIdentity2);
				return full[num2 + sortIdentity2.Children.Count].SortOrder + 268435456;
			}
		}
		return -1L;
	}

	private static List<SortIdentity> GetPrimaryIdentities(string teamId)
	{
		if (teamId == "c1a7e08345e444dea187e21a692f0d7a")
		{
			teamId = null;
		}
		List<ProjectModel> projects = CacheManager.GetProjects().Where(delegate(ProjectModel p)
		{
			if (p.Isinbox)
			{
				return false;
			}
			return string.IsNullOrEmpty(teamId) ? string.IsNullOrEmpty(p.teamId) : (p.teamId == teamId);
		}).ToList();
		List<ProjectGroupModel> list = (from p in CacheManager.GetProjectGroups()
			where string.IsNullOrEmpty(teamId) ? string.IsNullOrEmpty(p.teamId) : (p.teamId == teamId)
			select p).ToList();
		List<SortIdentity> list2 = new List<SortIdentity>();
		if (projects.Any())
		{
			List<ProjectModel> source = projects.Where((ProjectModel p) => string.IsNullOrEmpty(p.groupId) || p.groupId.ToUpper().ToString() == "NONE").ToList();
			if (source.Any())
			{
				list2.AddRange(source.Select((ProjectModel project) => new SortIdentity
				{
					Identity = project.id,
					Name = project.name,
					SortOrder = project.sortOrder
				}));
			}
		}
		if (list.Count > 0)
		{
			list2.AddRange(list.Select((ProjectGroupModel group) => new SortIdentity
			{
				Identity = group.id,
				Name = group.name,
				SortOrder = group.sortOrder.GetValueOrDefault(),
				Children = (from p in projects
					where p.groupId == @group.id
					select new SortIdentity
					{
						Identity = p.id,
						Name = p.name,
						SortOrder = p.sortOrder
					} into p
					orderby p.SortOrder
					select p).ToList()
			}));
		}
		return list2.OrderBy((SortIdentity sort) => sort.SortOrder).ToList();
	}

	private static List<SortIdentity> GetSortIdentities(string teamId)
	{
		List<SortIdentity> list = new List<SortIdentity>();
		List<SortIdentity> primaryIdentities = GetPrimaryIdentities(teamId);
		if (primaryIdentities.Any())
		{
			foreach (SortIdentity item in primaryIdentities)
			{
				list.Add(item);
				if (item.Children == null || !item.Children.Any())
				{
					continue;
				}
				foreach (SortIdentity child in item.Children)
				{
					list.Add(child);
				}
			}
		}
		return list;
	}

	public static long CalculateInsertSortOrder(bool upDown, string identity, string teamId, string outGroupId = "")
	{
		return CalculateInsertOrder(upDown, identity, GetSortIdentities(teamId), outGroupId);
	}

	public static long CalculateInsertTagSortOrder(bool upDown, string tagName)
	{
		IList<SortIdentity> tagSortIdentities = GetTagSortIdentities();
		return CalculateInsertOrder(upDown, tagName, tagSortIdentities);
	}

	private static IList<SortIdentity> GetTagSortIdentities()
	{
		bool num = LocalSettings.Settings.SmartListTag == 2;
		List<TagModel> list = CacheManager.GetTags();
		if (num)
		{
			ConcurrentDictionary<string, int> countCache = TaskCountCache.CountData;
			list = list.Where((TagModel tag) => countCache.ContainsKey(tag.name) && countCache[tag.name] > 0).ToList();
		}
		List<SortIdentity> list2 = new List<SortIdentity>();
		if (list != null && list.Count > 0)
		{
			list2.AddRange(from tag in list
				select new SortIdentity
				{
					Identity = tag.name,
					SortOrder = tag.sortOrder
				} into tag
				orderby tag.SortOrder
				select tag);
		}
		return list2;
	}

	public static long GetTagMinSort()
	{
		IList<SortIdentity> tagSortIdentities = GetTagSortIdentities();
		if (!tagSortIdentities.Any())
		{
			return 0L;
		}
		return tagSortIdentities.First().SortOrder;
	}

	public static long CalculateInsertFilterSortOrder(bool upDown, string identity)
	{
		List<FilterModel> filters = CacheManager.GetFilters();
		if (filters != null && filters.Count > 0)
		{
			return CalculateInsertOrder(upDown, identity, (from filter in filters
				select new SortIdentity
				{
					Identity = filter.id,
					SortOrder = filter.sortOrder
				} into sort
				orderby sort.SortOrder
				select sort).ToList());
		}
		return 0L;
	}

	public static long CalculateInsertSubscribeSortOrder(bool upDown, string identity)
	{
		List<CalendarSubscribeProfileModel> subscribeCalendars = CacheManager.GetSubscribeCalendars();
		List<BindCalendarAccountModel> bindCalendarAccounts = CacheManager.GetBindCalendarAccounts();
		if (subscribeCalendars.Count + bindCalendarAccounts.Count > 0)
		{
			List<SortIdentity> sortIdentities = (from sort in subscribeCalendars.Select((CalendarSubscribeProfileModel sub) => new SortIdentity
				{
					Identity = sub.Id,
					SortOrder = sub.Order.GetValueOrDefault()
				}).Union(bindCalendarAccounts.Select((BindCalendarAccountModel acc) => new SortIdentity
				{
					Identity = acc.Id,
					SortOrder = acc.Order.GetValueOrDefault()
				}))
				orderby sort.SortOrder
				select sort).ToList();
			return CalculateInsertOrder(upDown, identity, sortIdentities);
		}
		return 0L;
	}

	private static long CalculateInsertOrder(bool upDown, string identity, IList<SortIdentity> sortIdentities, string outGroupId = "")
	{
		if (sortIdentities.Count > 0)
		{
			SortIdentity sortIdentity = sortIdentities.FirstOrDefault((SortIdentity sort) => sort.Identity == identity);
			if (sortIdentity != null)
			{
				int num = sortIdentities.IndexOf(sortIdentity);
				int num2 = num + (upDown ? 1 : (-1));
				if (!string.IsNullOrEmpty(outGroupId))
				{
					if (!upDown)
					{
						num = GetNextIndexOfGroup(sortIdentities, outGroupId);
						return sortIdentities[num].SortOrder - 268435456;
					}
					num = GetIndexOfItem(sortIdentities, outGroupId);
					num2 = GetNextIndexOfGroup(sortIdentities, outGroupId);
				}
				if (num2 < 0)
				{
					return sortIdentities.First().SortOrder - 268435456;
				}
				if (num2 >= sortIdentities.Count)
				{
					return sortIdentities.Last().SortOrder + 268435456;
				}
				return sortIdentities[num].SortOrder + (sortIdentities[num2].SortOrder - sortIdentities[num].SortOrder) / 2;
			}
		}
		return -1L;
	}

	private static int GetNextIndexOfGroup(IList<SortIdentity> sortIdentities, string id)
	{
		if (sortIdentities != null && sortIdentities.Any())
		{
			SortIdentity sortIdentity = sortIdentities.FirstOrDefault((SortIdentity p) => p.Identity == id);
			if (sortIdentity != null)
			{
				return sortIdentities.IndexOf(sortIdentity) + sortIdentity.Children.Count;
			}
		}
		return -1;
	}

	private static int GetIndexOfItem(IList<SortIdentity> sortIdentities, string id)
	{
		if (sortIdentities != null && sortIdentities.Any())
		{
			SortIdentity sortIdentity = sortIdentities.FirstOrDefault((SortIdentity p) => p.Identity == id);
			if (sortIdentity != null)
			{
				return sortIdentities.IndexOf(sortIdentity);
			}
		}
		return -1;
	}

	public static long? GetNewSortOrderInClosed(bool upDown, string projectId, string teamId)
	{
		List<ProjectModel> list = CacheManager.GetProjectInTeams(teamId)?.Where((ProjectModel p) => p.closed == true).ToList();
		if (list == null || list.Count == 0)
		{
			return null;
		}
		list.Sort((ProjectModel a, ProjectModel b) => a.sortOrder.CompareTo(b.sortOrder));
		ProjectModel projectModel = list.FirstOrDefault((ProjectModel p) => p.id == projectId);
		if (projectModel == null)
		{
			return null;
		}
		int num = list.IndexOf(projectModel);
		if (upDown)
		{
			if (num == list.Count - 1)
			{
				return projectModel.sortOrder + 268435456;
			}
			long sortOrder = projectModel.sortOrder;
			long sortOrder2 = list[num + 1].sortOrder;
			return sortOrder + (sortOrder2 - sortOrder) / 2;
		}
		if (num == 0)
		{
			return projectModel.sortOrder - 268435456;
		}
		long sortOrder3 = projectModel.sortOrder;
		long sortOrder4 = list[num - 1].sortOrder;
		return sortOrder3 + (sortOrder4 - sortOrder3) / 2;
	}

	public static long GetNewSortOrderOnCloseChanged(ProjectModel project)
	{
		bool isClosed = project.closed == true;
		List<ProjectModel> list = CacheManager.GetProjectInTeams(project.teamId)?.Where((ProjectModel p) => p.closed == true == isClosed).ToList();
		if (list != null && list.Count > 0)
		{
			list.Sort((ProjectModel a, ProjectModel b) => a.sortOrder.CompareTo(b.sortOrder));
			ProjectModel projectModel = list[(!isClosed) ? (list.Count - 1) : 0];
			if (projectModel.id == project.id)
			{
				return project.sortOrder;
			}
			return projectModel.sortOrder + (long)((!isClosed) ? 1 : (-1)) * 268435456L;
		}
		return project.sortOrder;
	}
}
