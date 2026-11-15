using System.Collections.Generic;
using System.Linq;
using ticktick_WPF.Cache;
using ticktick_WPF.Models;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Misc;

public static class ProjectDataAssembler
{
	public static List<SelectableItemViewModel> AssembleProjects(bool onlyShowPermission = false, bool inCal = false)
	{
		List<ProjectModel> projects = (from p in CacheManager.GetProjectsWithoutInbox()
			where !p.closed.HasValue || !p.closed.Value
			select p).ToList();
		if (onlyShowPermission)
		{
			projects = projects.Where((ProjectModel p) => p.IsProjectPermit()).ToList();
		}
		List<ProjectGroupModel> projectGroups = CacheManager.GetProjectGroups();
		HashSet<string> groupIds = new HashSet<string>(projectGroups.Select((ProjectGroupModel group) => group.id));
		List<TeamModel> source = (from team in CacheManager.GetTeams()
			where !team.expired
			select team).ToList();
		if (!UserManager.IsTeamUser())
		{
			source = new List<TeamModel>();
		}
		List<ProjectModel> orphanProjects = projects.Where((ProjectModel p) => string.IsNullOrEmpty(p.groupId) || p.groupId.Trim().ToUpper() == "NONE" || !groupIds.Contains(p.groupId)).ToList();
		List<ProjectGroupViewModel> groupModels = projectGroups.Select((ProjectGroupModel group) => new ProjectGroupViewModel(group)
		{
			SortOrder = group.sortOrder.GetValueOrDefault(),
			TeamId = group.teamId,
			InCalFilter = inCal,
			Children = new List<SelectableItemViewModel>((from p in projects
				where p.groupId == @group.id
				orderby p.sortOrder
				select new SubProjectViewModel(p.id, p.groupId, p.name, p.IsShareList(), p.IsNote)
				{
					SortOrder = p.sortOrder,
					InCalFilter = inCal
				}).ToList())
		}).ToList();
		groupModels = groupModels.Where(delegate(ProjectGroupViewModel g)
		{
			List<SelectableItemViewModel> children = g.Children;
			return children != null && children.Count > 0;
		}).ToList();
		List<TeamSectionViewModel> list = source.Select((TeamModel team) => new TeamSectionViewModel(team.id, team.name)
		{
			Children = (from selectableItemViewModel in new List<SelectableItemViewModel>(groupModels.Where((ProjectGroupViewModel @group) => @group.TeamId == team.id).ToList()).Union(new List<SelectableItemViewModel>(new List<SelectableItemViewModel>(from p in orphanProjects
					where p.teamId == team.id
					select new ProjectViewModel(p.id, p.name, p.IsShareList(), p.IsNote)
					{
						SortOrder = p.sortOrder,
						InCalFilter = inCal
					})))
				orderby selectableItemViewModel.SortOrder
				select selectableItemViewModel).ToList()
		}).ToList();
		TeamSectionViewModel item = new TeamSectionViewModel("c1a7e08345e444dea187e21a692f0d7a", Utils.GetString("Personal"))
		{
			Children = (from selectableItemViewModel in new List<SelectableItemViewModel>((from p in orphanProjects
					where string.IsNullOrEmpty(p.teamId) || CacheManager.GetTeamById(p.teamId) == null
					select new ProjectViewModel(p.id, p.name, p.IsShareList(), p.IsNote)
					{
						SortOrder = p.sortOrder,
						InCalFilter = inCal
					}).ToList()).Union(new List<SelectableItemViewModel>(groupModels.Where((ProjectGroupViewModel @group) => string.IsNullOrEmpty(@group.TeamId))))
				orderby selectableItemViewModel.SortOrder
				select selectableItemViewModel).ToList()
		};
		list.Add(item);
		if (source.Any())
		{
			foreach (TeamSectionViewModel item2 in list)
			{
				SetTeamId(item2, item2.Id);
			}
		}
		List<SelectableItemViewModel> list2 = ExpandItems(list);
		if (!source.Any() && list2.Any())
		{
			list2.RemoveAt(0);
		}
		return list2;
	}

	private static void SetTeamId(SelectableItemViewModel model, string teamId)
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
		foreach (SelectableItemViewModel child in model.Children)
		{
			SetTeamId(child, teamId);
		}
	}

	private static List<SelectableItemViewModel> ExpandItems(List<TeamSectionViewModel> items)
	{
		List<SelectableItemViewModel> list = new List<SelectableItemViewModel>();
		if (items != null && items.Any())
		{
			foreach (TeamSectionViewModel item in items)
			{
				AddItems(item, list);
			}
		}
		return list;
	}

	private static void AddItems(SelectableItemViewModel item, List<SelectableItemViewModel> models)
	{
		models.Add(item);
		if (item.Children == null || !item.Children.Any())
		{
			return;
		}
		foreach (SelectableItemViewModel child in item.Children)
		{
			AddItems(child, models);
		}
	}
}
