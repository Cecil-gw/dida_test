using System;
using System.Collections.Generic;
using System.Linq;
using ticktick_WPF.Cache;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Provider;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views;

public static class MoveToastHelper
{
	public static bool CheckTaskMatched(ProjectIdentity identity, TaskModel task)
	{
		if (task == null || identity == null || LocalSettings.Settings.InSearch)
		{
			return true;
		}
		return CheckTaskMatched(identity, new TaskBaseViewModel(task));
	}

	public static bool CheckTaskMatched(ProjectIdentity identity, TaskBaseViewModel task)
	{
		List<TaskBaseViewModel> list = new List<TaskBaseViewModel> { task };
		if (!(identity is AllProjectIdentity))
		{
			if (!(identity is TodayProjectIdentity))
			{
				if (!(identity is TomorrowProjectIdentity))
				{
					if (!(identity is WeekProjectIdentity))
					{
						if (!(identity is GroupProjectIdentity groupProjectIdentity))
						{
							if (!(identity is AssignToMeProjectIdentity))
							{
								TagProjectIdentity tagProjectIdentity = identity as TagProjectIdentity;
								if (tagProjectIdentity == null)
								{
									if (!(identity is FilterProjectIdentity filterProjectIdentity))
									{
										if (!(identity is NormalProjectIdentity normalProjectIdentity))
										{
											if (identity is SearchProjectIdentity || identity is CompletedProjectIdentity || identity is AbandonedProjectIdentity)
											{
												return true;
											}
											return false;
										}
										return task.ProjectId == normalProjectIdentity.Id;
									}
									List<TaskBaseViewModel> tasksMatchedFilter = TaskService.GetTasksMatchedFilter(filterProjectIdentity.Filter, list);
									if (tasksMatchedFilter == null)
									{
										return false;
									}
									return tasksMatchedFilter.Count > 0;
								}
								List<string> tags = (from t in CacheManager.GetTags()
									where t.name == tagProjectIdentity.Tag || t.parent == tagProjectIdentity.Tag
									select t.name).ToList();
								List<TaskBaseViewModel> list2 = TaskViewModelHelper.GetTasks(list, null, null, tags, null, !LocalSettings.Settings.HideComplete).ToList();
								if (list2 == null)
								{
									return false;
								}
								return list2.Count > 0;
							}
							List<TaskBaseViewModel> list3 = TaskViewModelHelper.GetTasks(list, null, null, null, null, showComplete: true, inTodayOrWeek: false, inAll: false, new List<string> { "me" }).ToList();
							if (list3 == null)
							{
								return false;
							}
							return list3.Count > 0;
						}
						List<TaskBaseViewModel> list4 = TaskViewModelHelper.GetTasks(list, new List<string> { groupProjectIdentity.GroupId }).ToList();
						if (list4 == null)
						{
							return false;
						}
						return list4.Count > 0;
					}
					List<TaskBaseViewModel> list5 = TaskViewModelHelper.GetTasks(list, null, null, null, new List<FilterDatePair>
					{
						new FilterDatePair(DateTime.Today, DateTime.Today.AddDays(7.0))
					}, showComplete: true, inTodayOrWeek: true, inAll: true).ToList();
					if (list5 == null)
					{
						return false;
					}
					return list5.Count > 0;
				}
				List<TaskBaseViewModel> list6 = TaskViewModelHelper.GetTasks(list, null, null, null, new List<FilterDatePair>
				{
					new FilterDatePair(DateTime.Today.AddDays(1.0), DateTime.Today.AddDays(2.0))
				}, showComplete: true, inTodayOrWeek: false, inAll: true).ToList();
				if (list6 == null)
				{
					return false;
				}
				return list6.Count > 0;
			}
			List<TaskBaseViewModel> list7 = TaskViewModelHelper.GetTasks(list, null, null, null, new List<FilterDatePair>
			{
				new FilterDatePair(DateTime.Today, DateTime.Today.AddDays(1.0))
			}, showComplete: true, inTodayOrWeek: true, inAll: true).ToList();
			if (list7 == null)
			{
				return false;
			}
			return list7.Count > 0;
		}
		if (!TaskViewModelHelper.CheckShowInSmartList(ProjectAndTaskIdsCache.GetProjectById(task.ProjectId), task))
		{
			return false;
		}
		return true;
	}
}
