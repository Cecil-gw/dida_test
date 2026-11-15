using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Service.Display;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Search;

namespace ticktick_WPF.Util.Provider;

public static class ProjectTaskDataProvider
{
	public static async Task<SortProjectData> GetProjectData(ProjectIdentity project)
	{
		SortProjectData sortProjectData = ((project is TodayProjectIdentity) ? (await GetSortDataInToday()) : ((project is AllProjectIdentity) ? (await GetSortDataInAll()) : ((project is TomorrowProjectIdentity) ? (await GetSortDataInTomorrow()) : ((project is WeekProjectIdentity) ? (await GetSortDataInWeek()) : ((project is GroupProjectIdentity) ? (await GetSortDataInGroupAll(project)) : ((project is AssignToMeProjectIdentity) ? (await GetSortDataAssignedToMe()) : ((project is CompletedProjectIdentity) ? (await GetSortDataInCompleted()) : ((project is AbandonedProjectIdentity) ? (await GetSortDataInAbandoned()) : ((project is TrashProjectIdentity) ? (await GetSortDataInTrash()) : ((project is TagProjectIdentity) ? (await GetSortDataInTag(project)) : ((project is FilterProjectIdentity) ? (await GetSortDataInFilter(project)) : ((project is SearchProjectIdentity) ? (await GetSortDataInSearch()) : ((project is FilterPreviewIdentity preview) ? (await GetPreviewSortData(preview)) : ((project is SubscribeCalendarProjectIdentity) ? (await GetSortDataInSubscribeCalendar(project)) : ((project is BindAccountCalendarProjectIdentity) ? (await GetSortDataInBindCalendar(project)) : ((!(project is DateProjectIdentity)) ? (await GetSortDataInProject(project)) : (await GetSortDataInDate(project))))))))))))))))));
		if (sortProjectData != null)
		{
			sortProjectData.ProjectIdentity = project;
		}
		else
		{
			NormalProjectIdentity inbox = ProjectIdentity.CreateInboxProject();
			sortProjectData = await GetSortDataInProject(inbox);
			sortProjectData.ProjectIdentity = inbox;
		}
		return sortProjectData;
	}

	public static async Task<List<TaskBaseViewModel>> GetDisplayModels(ProjectIdentity project, bool withChild = true, bool? showCompete = null)
	{
		if (project == null)
		{
			return new List<TaskBaseViewModel>();
		}
		List<TaskBaseViewModel> list;
		if (!(project is AllProjectIdentity))
		{
			if (!(project is TodayProjectIdentity))
			{
				if (!(project is TomorrowProjectIdentity))
				{
					if (!(project is WeekProjectIdentity))
					{
						if (!(project is GroupProjectIdentity groupProjectIdentity))
						{
							if (!(project is AssignToMeProjectIdentity))
							{
								if (!(project is CompletedProjectIdentity))
								{
									if (!(project is AbandonedProjectIdentity))
									{
										if (!(project is TrashProjectIdentity trashProjectIdentity))
										{
											if (!(project is TagProjectIdentity tagProjectIdentity))
											{
												if (!(project is FilterProjectIdentity filterProjectIdentity))
												{
													if (!(project is FilterPreviewIdentity filterPreviewIdentity))
													{
														if (!(project is MatrixQuadrantIdentity matrixQuadrantIdentity))
														{
															if (!(project is SearchProjectIdentity))
															{
																if (!(project is SubscribeCalendarProjectIdentity subscribeCalendarProjectIdentity))
																{
																	if (!(project is BindAccountCalendarProjectIdentity bindAccountCalendarProjectIdentity))
																	{
																		if (!(project is ColumnProjectIdentity model))
																		{
																			list = ((!(project is DateProjectIdentity dateProjectIdentity)) ? TaskDisplayService.GetDisplayTasksInProject(project.Id, hideComplete: false) : (await TaskDao.GetDisplayTasksInDate(dateProjectIdentity.DateStamp)));
																		}
																		else
																		{
																			string text = await ColumnDao.GetProjectDefaultColumnId(model.GetProjectId());
																			list = await TaskDao.GetDisplayTasksInColumn(project.GetProjectId(), model.ColumnId, model.ColumnId == text);
																		}
																	}
																	else
																	{
																		list = await CalendarEventDao.GetDisplayEventsInBindAccount(bindAccountCalendarProjectIdentity.Account.Id, DateTime.Today, DateTime.Now.AddMonths(3));
																	}
																}
																else
																{
																	list = await CalendarEventDao.GetDisplayEventsInCalendar(subscribeCalendarProjectIdentity.Profile.Id, 1, DateTime.Now.Date, DateTime.Now.Date.AddMonths(4));
																}
															}
															else
															{
																list = await TaskDisplayService.GetDisplayTaskInSearch(SearchKind.Normal, SearchHelper.SearchKey, SearchHelper.SearchFilter, SearchHelper.Tags);
															}
														}
														else
														{
															list = await TaskDisplayService.GetDisplayTaskInFilter(matrixQuadrantIdentity.Quadrant.rule, showCompete ?? LocalSettings.Settings.MatrixShowCompleted, LocalSettings.Settings.ShowSubtasks, withChild: false, inCal: false, inAll: true);
														}
													}
													else
													{
														list = await TaskDisplayService.GetDisplayTaskInFilter(filterPreviewIdentity.Filter.rule, showCompete ?? (!LocalSettings.Settings.HideComplete), LocalSettings.Settings.ShowSubtasks);
													}
												}
												else
												{
													list = await TaskDisplayService.GetDisplayTaskInFilter(filterProjectIdentity.Filter.rule, getCompleted: true, LocalSettings.Settings.ShowSubtasks, withChild);
												}
											}
											else
											{
												list = await TaskDisplayService.GetDisplayTaskInTag(tagProjectIdentity.Tag);
											}
										}
										else
										{
											list = await TaskDisplayService.GetDisplayTasksInTrash(trashProjectIdentity.IsPerson);
										}
									}
									else
									{
										list = await TaskDisplayService.GetDisplayTasksInAbandoned();
									}
								}
								else
								{
									list = await TaskDisplayService.GetDisplayTasksInCompleted();
								}
							}
							else
							{
								list = await TaskDisplayService.GetDisplayTaskInAssigneeToMe(project);
							}
						}
						else
						{
							list = await TaskDisplayService.GetDisplayTaskInGroup(groupProjectIdentity.GroupId);
						}
					}
					else
					{
						list = await TaskDisplayService.GetDisplayTaskInWeek();
					}
				}
				else
				{
					list = await TaskDisplayService.GetDisplayTaskInTomorrow();
				}
			}
			else
			{
				list = await TaskDisplayService.GetDisplayTaskInToday();
			}
		}
		else
		{
			list = await TaskDisplayService.GetDisplayTaskInAll();
		}
		return list ?? new List<TaskBaseViewModel>();
	}

	private static async Task<SortProjectData> GetSortDataInTag(ProjectIdentity projectId)
	{
		string tag = ((TagProjectIdentity)projectId).Tag;
		TagModel tagModel = CacheManager.GetTags().FirstOrDefault((TagModel t) => t.name == tag);
		if (tagModel != null)
		{
			projectId.SortOption = tagModel.GetSortOption();
		}
		return new TagListData(((TagProjectIdentity)projectId).Tag);
	}

	private static async Task<SortProjectData> GetSortDataInAll()
	{
		return new AllListData();
	}

	private static async Task<SortProjectData> GetSortDataInSearch()
	{
		return new SearchListData();
	}

	private static async Task<SortProjectData> GetPreviewSortData(FilterPreviewIdentity preview)
	{
		return new PreviewListData(preview);
	}

	private static async Task<SortProjectData> GetSortDataInBindCalendar(ProjectIdentity project)
	{
		return new BindAccountCalendarListData(((BindAccountCalendarProjectIdentity)project).Account);
	}

	private static async Task<SortProjectData> GetSortDataInDate(ProjectIdentity project)
	{
		return new DateListData((project as DateProjectIdentity)?.DateStamp);
	}

	private static async Task<SortProjectData> GetSortDataInSubscribeCalendar(ProjectIdentity project)
	{
		return new SubscribeCalendarListData(((SubscribeCalendarProjectIdentity)project).Profile);
	}

	private static async Task<SortProjectData> GetSortDataInFilter(ProjectIdentity projectId)
	{
		return new FilterListData(((FilterProjectIdentity)projectId).Filter);
	}

	private static async Task<SortProjectData> GetSortDataInGroupAll(ProjectIdentity projectId)
	{
		List<ProjectGroupModel> projectGroups = CacheManager.GetProjectGroups();
		if (projectId != null)
		{
			ProjectGroupModel projectGroupModel = projectGroups.FirstOrDefault((ProjectGroupModel g) => g.id == projectId.CatId);
			if (projectGroupModel != null)
			{
				return new ProjectGroupData(projectGroupModel, CacheManager.GetProjects());
			}
		}
		return new SortProjectData();
	}

	private static async Task<SortProjectData> GetSortDataInProject(ProjectIdentity projectId)
	{
		return new NormalListData(CacheManager.GetProjectById(projectId.Id) ?? new ProjectModel
		{
			id = "inbox" + LocalSettings.Settings.LoginUserId,
			name = Utils.GetString("Inbox"),
			Isinbox = true
		});
	}

	private static async Task<SortProjectData> GetSortDataAssignedToMe()
	{
		return new AssignListData();
	}

	private static async Task<SortProjectData> GetSortDataInTomorrow()
	{
		return new TomorrowListData();
	}

	private static async Task<SortProjectData> GetSortDataInWeek()
	{
		return new WeekListData();
	}

	private static async Task<SortProjectData> GetSortDataInToday()
	{
		return new TodayListData();
	}

	private static async Task<SortProjectData> GetSortDataInCompleted()
	{
		return new CompleteListData();
	}

	private static async Task<SortProjectData> GetSortDataInAbandoned()
	{
		return new AbandonedListData();
	}

	private static async Task<SortProjectData> GetSortDataInTrash()
	{
		return new TrashListData();
	}
}
