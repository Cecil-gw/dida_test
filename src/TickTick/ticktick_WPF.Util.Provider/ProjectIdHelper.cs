using System.Linq;
using ticktick_WPF.Cache;
using ticktick_WPF.Models;
using ticktick_WPF.Service;

namespace ticktick_WPF.Util.Provider;

public static class ProjectIdHelper
{
	public static ProjectIdentity GetProjectIdentity(string projectId)
	{
		ProjectModel projectModel = CacheManager.GetProjects().FirstOrDefault((ProjectModel p) => p.id == projectId);
		if (projectModel == null)
		{
			return null;
		}
		NormalProjectIdentity normalProjectIdentity = new NormalProjectIdentity(projectModel)
		{
			SortOption = projectModel.GetSortOption()
		};
		if (projectModel.Isinbox)
		{
			normalProjectIdentity.SortOption = SmartProjectService.GetSmartProjectSortOption("inbox", inKanban: false);
		}
		return normalProjectIdentity;
	}

	public static ProjectIdentity GetGroupIdentity(string groupId)
	{
		ProjectGroupModel projectGroupModel = CacheManager.GetProjectGroups().FirstOrDefault((ProjectGroupModel g) => g.id == groupId);
		if (projectGroupModel == null)
		{
			return null;
		}
		return new GroupProjectIdentity(projectGroupModel, CacheManager.GetProjectsInGroup(groupId));
	}

	public static ProjectIdentity GetFilterIdentity(string id)
	{
		FilterModel filterModel = CacheManager.GetFilters().FirstOrDefault((FilterModel f) => f.id == id);
		if (filterModel != null)
		{
			return new FilterProjectIdentity(filterModel)
			{
				SortOption = filterModel.GetSortOption()
			};
		}
		return null;
	}

	public static ProjectIdentity GetCalendarIdentity(string id)
	{
		CalendarSubscribeProfileModel calendarSubscribeProfileModel = CacheManager.GetSubscribeCalendars().FirstOrDefault((CalendarSubscribeProfileModel c) => c.Id == id);
		if (calendarSubscribeProfileModel != null)
		{
			return new SubscribeCalendarProjectIdentity(calendarSubscribeProfileModel);
		}
		BindCalendarAccountModel bindCalendarAccountModel = CacheManager.GetBindCalendarAccounts().FirstOrDefault((BindCalendarAccountModel c) => c.Id == id);
		if (bindCalendarAccountModel != null)
		{
			return new BindAccountCalendarProjectIdentity(bindCalendarAccountModel);
		}
		return null;
	}

	public static ProjectIdentity GetTagIdentity(string name)
	{
		TagModel tagModel = CacheManager.GetTags().FirstOrDefault((TagModel f) => f.name == name);
		if (tagModel != null)
		{
			return new TagProjectIdentity(tagModel);
		}
		return null;
	}
}
