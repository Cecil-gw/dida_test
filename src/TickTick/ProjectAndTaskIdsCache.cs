using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Service.Display;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Provider;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.MainListView;

public static class ProjectAndTaskIdsCache
{
	private static ConcurrentDictionary<string, ProjectTaskIds> _projectTasksDict = new ConcurrentDictionary<string, ProjectTaskIds>();

	private static ConcurrentDictionary<string, ProjectModel> _projectDict = new ConcurrentDictionary<string, ProjectModel>();

	private static ConcurrentDictionary<string, int> _projectEventCount = new ConcurrentDictionary<string, int>();

	public static void TryDelayLoadCount()
	{
		DelayActionHandlerCenter.TryDoAction("ProjectAndTaskIdsCache", delegate
		{
			Utils.RunOnBackgroundThread(Application.Current?.Dispatcher, delegate
			{
				ListViewContainer.ReloadCount();
			});
		}, 400);
	}

	public static ProjectModel GetProjectById(string id)
	{
		return CacheManager.GetProjectById(id);
	}

	public static async Task<int> GetCountByIdentity(ProjectIdentity identity, bool withNote = false)
	{
		if (identity == null)
		{
			return 0;
		}
		ProjectTaskIds projectTaskIds = await GetProjectTaskIdsInDict(identity);
		int num = 0;
		if (projectTaskIds != null)
		{
			num += projectTaskIds.UnCompletedTaskIds.Count;
			if (withNote || LocalSettings.Settings.ExtraSettings.NumDisplayType == 0)
			{
				num += projectTaskIds.UnCompletedNoteIds.Count;
			}
			if (LocalSettings.Settings.ShowSubtasks)
			{
				num += projectTaskIds.UnCompletedItemIds.Count;
			}
		}
		int num2 = num;
		num = num2 + await TryGetEventsAndHabitsCountByIdentity(identity, (!(identity is FilterProjectIdentity)) ? null : projectTaskIds?.GetIds());
		if (num == 0)
		{
			num = await TryGetCountdownCountByIdentity(identity);
		}
		return num;
	}

	private static async Task<ProjectTaskIds> GetProjectTaskIdsInDict(ProjectIdentity identity)
	{
		if (_projectTasksDict.ContainsKey(identity.QueryId))
		{
			ProjectTaskIds projectTaskIds = _projectTasksDict[identity.QueryId];
			if (projectTaskIds != null)
			{
				return projectTaskIds;
			}
			_projectTasksDict.TryRemove(identity.QueryId, out var _);
		}
		ProjectTaskIds projectTaskIds2 = await GetProjectTaskIds(identity);
		if (projectTaskIds2 != null && !string.IsNullOrEmpty(identity.QueryId))
		{
			_projectTasksDict[identity.QueryId] = projectTaskIds2;
		}
		return projectTaskIds2;
	}

	public static async Task ResetProjectTaskIds(ProjectIdentity identity, List<TaskBaseViewModel> models)
	{
		if (identity != null)
		{
			ProjectTaskIds projectTaskIds = new ProjectTaskIds
			{
				Project = identity
			};
			ArrangeModelIds(projectTaskIds, models);
			if (!string.IsNullOrEmpty(identity.QueryId))
			{
				_projectTasksDict[identity.QueryId] = projectTaskIds;
			}
		}
	}

	public static async Task ResetProjectTaskIds(ProjectIdentity identity)
	{
		if (identity == null)
		{
			return;
		}
		ProjectTaskIds projectTaskIds = await GetProjectTaskIds(identity);
		if (!string.IsNullOrEmpty(identity.QueryId))
		{
			if (projectTaskIds == null)
			{
				_projectTasksDict.TryRemove(identity.QueryId, out var _);
			}
			else
			{
				_projectTasksDict[identity.QueryId] = projectTaskIds;
			}
		}
	}

	private static async Task<int> TryGetEventsAndHabitsCountByIdentity(ProjectIdentity identity, HashSet<string> ignoreIds)
	{
		if (!_projectEventCount.TryGetValue(identity.QueryId, out var count))
		{
			List<TaskBaseViewModel> list = null;
			if (!(identity is AllProjectIdentity))
			{
				if (!(identity is TodayProjectIdentity))
				{
					if (!(identity is TomorrowProjectIdentity))
					{
						if (!(identity is WeekProjectIdentity))
						{
							if (!(identity is FilterProjectIdentity filterProjectIdentity))
							{
								if (!(identity is SubscribeCalendarProjectIdentity subscribeCalendarProjectIdentity))
								{
									if (identity is BindAccountCalendarProjectIdentity bindAccountCalendarProjectIdentity)
									{
										list = await CalendarEventDao.GetDisplayEventsInBindAccount(bindAccountCalendarProjectIdentity.Account.Id, DateTime.Today, DateTime.Now.AddMonths(3));
									}
								}
								else
								{
									list = await CalendarEventDao.GetDisplayEventsInCalendar(subscribeCalendarProjectIdentity.Profile.Id, 1, DateTime.Now.Date, DateTime.Now.AddMonths(3));
								}
							}
							else
							{
								list = await TaskDisplayService.GetDisplayEventsInFilter(filterProjectIdentity.Filter, inCal: false);
							}
						}
						else
						{
							list = (await TaskDisplayService.GetCalendarEventsInSpan(DateTime.Now.Date, DateTime.Now.Date.AddDays(7.0))).Item2;
						}
					}
					else
					{
						list = (await TaskDisplayService.GetCalendarEventsInSpan(DateTime.Now.Date.AddDays(1.0), DateTime.Now.Date.AddDays(2.0))).Item2;
					}
				}
				else
				{
					list = (await TaskDisplayService.GetCalendarEventsInSpan(DateTime.Now.Date, DateTime.Now.Date.AddDays(1.0))).Item2;
				}
			}
			else
			{
				list = (await TaskDisplayService.GetCalendarEventsInSpan(DateTime.Now.Date, DateTime.Now.Date.AddDays(90.0))).Item2;
			}
			count = list?.Count ?? 0;
			_projectEventCount[identity.QueryId] = count;
		}
		if (!(identity is TodayProjectIdentity) && !(identity is WeekProjectIdentity))
		{
			if (identity is TomorrowProjectIdentity && LocalSettings.Settings.UserPreference?.TimeTable?.ShowInSmart == true)
			{
				List<TaskBaseViewModel> list2 = await TaskDisplayService.GetCourseViewModelsInSpan(DateTime.Today.AddDays(1.0), DateTime.Today.AddDays(2.0));
				count += list2?.Count ?? 0;
			}
		}
		else
		{
			List<HabitBaseViewModel> list3 = await TaskDisplayService.GetHabitsInToday();
			count += list3?.Count((HabitBaseViewModel h) => h.Status == 0) ?? 0;
			if (LocalSettings.Settings.UserPreference?.TimeTable?.ShowInSmart == true)
			{
				List<TaskBaseViewModel> list4 = await TaskDisplayService.GetCourseViewModelsInSpan(DateTime.Today, DateTime.Today.AddDays((identity is TodayProjectIdentity) ? 1 : 7));
				count += list4?.Count ?? 0;
			}
		}
		return count;
	}

	private static async Task<int> TryGetCountdownCountByIdentity(ProjectIdentity identity)
	{
		if (!LocalSettings.Settings.EnableCountDownDays)
		{
			return 0;
		}
		if (!(identity is TodayProjectIdentity))
		{
			if (!(identity is TomorrowProjectIdentity))
			{
				if (identity is WeekProjectIdentity)
				{
					return (await TaskDisplayService.GetCountdownsInWeek()).Count;
				}
				return 0;
			}
			return (await TaskDisplayService.GetCountdownsInTomorrow()).Count;
		}
		return (await TaskDisplayService.GetCountdownsInToday()).Count;
	}

	private static async Task<ProjectTaskIds> GetProjectTaskIds(ProjectIdentity project)
	{
		if (project == null || !TaskViewModelHelper.Loaded)
		{
			return null;
		}
		ProjectTaskIds idsModel = new ProjectTaskIds
		{
			Project = project
		};
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
								TagProjectIdentity tagProjectIdentity = project as TagProjectIdentity;
								if (tagProjectIdentity == null)
								{
									if (!(project is FilterProjectIdentity filterProjectIdentity))
									{
										if (project is NormalProjectIdentity)
										{
											List<TaskBaseViewModel> modelsInProject = TaskCache.GetModelsInProject(project.Id);
											ArrangeModelIds(idsModel, modelsInProject);
										}
									}
									else
									{
										ArrangeModelIds(idsModel, await TaskDisplayService.GetDisplayTaskInFilter(filterProjectIdentity.Filter.rule, getCompleted: false, getCheckItem: true, withChild: false));
									}
								}
								else
								{
									List<string> tags = (from t in CacheManager.GetTags()
										where t.name == tagProjectIdentity.Tag || t.parent == tagProjectIdentity.Tag
										select t.name).ToList();
									IEnumerable<TaskBaseViewModel> tasks = TaskViewModelHelper.GetTasks(TaskCache.LocalTaskViewModels.Values.ToList(), null, null, tags, null, showComplete: false);
									ArrangeTaskIds(idsModel, tasks);
								}
							}
							else
							{
								IEnumerable<TaskBaseViewModel> tasks2 = TaskViewModelHelper.GetTasks(TaskCache.LocalTaskViewModels.Values.ToList(), null, null, null, null, showComplete: false, inTodayOrWeek: false, inAll: false, new List<string> { "me" });
								ArrangeTaskIds(idsModel, tasks2);
							}
						}
						else
						{
							IEnumerable<TaskBaseViewModel> tasks3 = TaskViewModelHelper.GetTasks(TaskCache.LocalTaskViewModels.Values.ToList(), new List<string> { groupProjectIdentity.GroupId }, null, null, null, showComplete: false);
							ArrangeTaskIds(idsModel, tasks3);
						}
					}
					else
					{
						IEnumerable<TaskBaseViewModel> tasks4 = TaskViewModelHelper.GetTasks(TaskCache.LocalTaskViewModels.Values.ToList(), null, null, null, new List<FilterDatePair>
						{
							new FilterDatePair(DateTime.Today, DateTime.Today.AddDays(7.0))
						}, showComplete: false, inTodayOrWeek: true, inAll: true);
						ArrangeTaskIds(idsModel, tasks4);
						IEnumerable<TaskBaseViewModel> items = TaskViewModelHelper.GetItems(TaskDetailItemCache.LocalCheckItemViewModels.Values.ToList(), null, null, null, new List<FilterDatePair>
						{
							new FilterDatePair(DateTime.Today, DateTime.Today.AddDays(7.0))
						}, showComplete: false, inTodayOrWeek: true, checkInAll: true);
						ArrangeItemIds(idsModel, items);
					}
				}
				else
				{
					IEnumerable<TaskBaseViewModel> tasks5 = TaskViewModelHelper.GetTasks(TaskCache.LocalTaskViewModels.Values.ToList(), null, null, null, new List<FilterDatePair>
					{
						new FilterDatePair(DateTime.Today.AddDays(1.0), DateTime.Today.AddDays(2.0))
					}, showComplete: false, inTodayOrWeek: false, inAll: true);
					ArrangeTaskIds(idsModel, tasks5);
					IEnumerable<TaskBaseViewModel> items2 = TaskViewModelHelper.GetItems(TaskDetailItemCache.LocalCheckItemViewModels.Values.ToList(), null, null, null, new List<FilterDatePair>
					{
						new FilterDatePair(DateTime.Today.AddDays(1.0), DateTime.Today.AddDays(2.0))
					}, showComplete: false, inTodayOrWeek: false, checkInAll: true);
					ArrangeItemIds(idsModel, items2);
				}
			}
			else
			{
				IEnumerable<TaskBaseViewModel> tasks6 = TaskViewModelHelper.GetTasks(TaskCache.LocalTaskViewModels.Values.ToList(), null, null, null, new List<FilterDatePair>
				{
					new FilterDatePair(DateTime.Today, DateTime.Today.AddDays(1.0))
				}, showComplete: false, inTodayOrWeek: true, inAll: true);
				ArrangeTaskIds(idsModel, tasks6);
				IEnumerable<TaskBaseViewModel> items3 = TaskViewModelHelper.GetItems(TaskDetailItemCache.LocalCheckItemViewModels.Values.ToList(), null, null, null, new List<FilterDatePair>
				{
					new FilterDatePair(DateTime.Today, DateTime.Today.AddDays(1.0))
				}, showComplete: false, inTodayOrWeek: true, checkInAll: true);
				ArrangeItemIds(idsModel, items3);
			}
		}
		else
		{
			IEnumerable<TaskBaseViewModel> tasks7 = TaskCache.LocalTaskViewModels.Values.Where(delegate(TaskBaseViewModel t)
			{
				if (t == null || t.Deleted != 0 || t.Status != 0)
				{
					return false;
				}
				return TaskViewModelHelper.CheckShowInSmartList(GetProjectById(t.ProjectId), t) ? true : false;
			});
			ArrangeTaskIds(idsModel, tasks7);
		}
		return idsModel;
	}

	private static void ArrangeModelIds(ProjectTaskIds idsModel, List<TaskBaseViewModel> models)
	{
		if (idsModel == null || models == null)
		{
			return;
		}
		foreach (TaskBaseViewModel model in models)
		{
			if (model == null)
			{
				continue;
			}
			if (model.IsTaskOrNote)
			{
				if (model.IsNote)
				{
					idsModel.UnCompletedNoteIds.Add(model.Id);
				}
				else if (model.Status == 0)
				{
					idsModel.UnCompletedTaskIds.Add(model.Id);
				}
			}
			else if (model.IsCheckItem && model.Status == 0)
			{
				idsModel.UnCompletedItemIds.Add(model.Id);
			}
		}
	}

	private static void ArrangeTaskIds(ProjectTaskIds idsModel, IEnumerable<TaskBaseViewModel> tasks)
	{
		foreach (TaskBaseViewModel task in tasks)
		{
			if (task.IsNote)
			{
				idsModel.UnCompletedNoteIds.Add(task.Id);
			}
			else if (task.Status == 0)
			{
				idsModel.UnCompletedTaskIds.Add(task.Id);
			}
		}
	}

	private static void ArrangeItemIds(ProjectTaskIds idsModel, IEnumerable<TaskBaseViewModel> items)
	{
		foreach (TaskBaseViewModel item in items)
		{
			if (item.Status == 0)
			{
				idsModel.UnCompletedItemIds.Add(item.Id);
			}
		}
	}

	public static async void OnTasksChanged(List<TaskBaseViewModel> tasks, CheckMatchedType checkMatched)
	{
		ParallelQuery<ProjectTaskIds> identities = _projectTasksDict.Values.ToList().AsParallel();
		Task.Run(delegate
		{
			foreach (ProjectTaskIds item in identities)
			{
				CheckTasksMatched(item, tasks, checkMatched);
			}
			if (checkMatched != CheckMatchedType.None)
			{
				TryDelayLoadCount();
			}
		});
	}

	private static async void CheckTasksMatched(ProjectTaskIds projectTasksModel, List<TaskBaseViewModel> tasks, CheckMatchedType checkMatched)
	{
		if (tasks != null && projectTasksModel != null && checkMatched != CheckMatchedType.None)
		{
			CheckMatchedTasks(projectTasksModel, tasks, checkMatched);
		}
	}

	public static void OnTaskChanged(TaskBaseViewModel task, CheckMatchedType checkMatched)
	{
		OnTasksChanged(new List<TaskBaseViewModel> { task }, checkMatched);
	}

	private static async Task CheckMatchedTasks(ProjectTaskIds projectTasksModel, List<TaskBaseViewModel> tasks, CheckMatchedType checkMatched)
	{
		if (tasks == null || tasks.Count == 0)
		{
			return;
		}
		List<TaskBaseViewModel> list = null;
		List<TaskBaseViewModel> list2 = null;
		List<TaskBaseViewModel> list3 = null;
		ProjectIdentity project = projectTasksModel.Project;
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
								TagProjectIdentity tagProjectIdentity = project as TagProjectIdentity;
								if (tagProjectIdentity == null)
								{
									if (!(project is FilterProjectIdentity filterProjectIdentity))
									{
										NormalProjectIdentity normalProjectIdentity = project as NormalProjectIdentity;
										if (normalProjectIdentity != null)
										{
											if (checkMatched != CheckMatchedType.All && checkMatched != CheckMatchedType.CheckProject)
											{
												return;
											}
											list = tasks.Where((TaskBaseViewModel p) => p.ProjectId == normalProjectIdentity.Id).ToList();
										}
									}
									else
									{
										list = TaskService.GetTasksMatchedFilter(filterProjectIdentity.Filter, tasks);
										list2 = TaskDetailItemCache.GetCheckItemsInTaskIds(tasks.Select((TaskBaseViewModel t) => t.Id).ToList());
										list3 = TaskService.GetCheckItemsMatchedFilter(filterProjectIdentity.Filter, list2);
									}
								}
								else
								{
									if (checkMatched != CheckMatchedType.All && checkMatched != CheckMatchedType.CheckTag)
									{
										return;
									}
									List<string> tags = (from t in CacheManager.GetTags()
										where t.name == tagProjectIdentity.Tag || t.parent == tagProjectIdentity.Tag
										select t.name).ToList();
									list = TaskViewModelHelper.GetTasks(tasks, null, null, tags, null, !LocalSettings.Settings.HideComplete).ToList();
								}
							}
							else
							{
								if (checkMatched != CheckMatchedType.All && checkMatched != CheckMatchedType.CheckAssign)
								{
									return;
								}
								list = TaskViewModelHelper.GetTasks(tasks, null, null, null, null, showComplete: true, inTodayOrWeek: false, inAll: false, new List<string> { "me" }).ToList();
							}
						}
						else
						{
							if (checkMatched != CheckMatchedType.All && checkMatched != CheckMatchedType.CheckGroup && checkMatched != CheckMatchedType.CheckProject)
							{
								return;
							}
							list = TaskViewModelHelper.GetTasks(tasks, new List<string> { groupProjectIdentity.GroupId }).ToList();
						}
					}
					else
					{
						if (checkMatched != CheckMatchedType.All && checkMatched != CheckMatchedType.CheckSmart)
						{
							return;
						}
						list = TaskViewModelHelper.GetTasks(tasks, null, null, null, new List<FilterDatePair>
						{
							new FilterDatePair(DateTime.Today, DateTime.Today.AddDays(7.0))
						}, showComplete: true, inTodayOrWeek: true).ToList();
						if (checkMatched == CheckMatchedType.All)
						{
							list2 = TaskDetailItemCache.GetCheckItemsInTaskIds(tasks.Select((TaskBaseViewModel t) => t.Id).ToList());
							list3 = TaskViewModelHelper.GetItems(list2, null, null, null, new List<FilterDatePair>
							{
								new FilterDatePair(DateTime.Today, DateTime.Today.AddDays(7.0))
							}, showComplete: true, inTodayOrWeek: true).ToList();
						}
					}
				}
				else
				{
					if (checkMatched != CheckMatchedType.All && checkMatched != CheckMatchedType.CheckSmart)
					{
						return;
					}
					list = TaskViewModelHelper.GetTasks(tasks, null, null, null, new List<FilterDatePair>
					{
						new FilterDatePair(DateTime.Today.AddDays(1.0), DateTime.Today.AddDays(2.0))
					}).ToList();
					if (checkMatched == CheckMatchedType.All)
					{
						list2 = TaskDetailItemCache.GetCheckItemsInTaskIds(tasks.Select((TaskBaseViewModel t) => t.Id).ToList());
						list3 = TaskViewModelHelper.GetItems(list2, null, null, null, new List<FilterDatePair>
						{
							new FilterDatePair(DateTime.Today.AddDays(1.0), DateTime.Today.AddDays(2.0))
						}).ToList();
					}
				}
			}
			else
			{
				if (checkMatched != CheckMatchedType.All && checkMatched != CheckMatchedType.CheckSmart)
				{
					return;
				}
				list = TaskViewModelHelper.GetTasks(tasks, null, null, null, new List<FilterDatePair>
				{
					new FilterDatePair(DateTime.Today, DateTime.Today.AddDays(1.0))
				}, showComplete: true, inTodayOrWeek: true).ToList();
				if (checkMatched == CheckMatchedType.All)
				{
					list2 = TaskDetailItemCache.GetCheckItemsInTaskIds(tasks.Select((TaskBaseViewModel t) => t.Id).ToList());
					list3 = TaskViewModelHelper.GetItems(list2, null, null, null, new List<FilterDatePair>
					{
						new FilterDatePair(DateTime.Today, DateTime.Today.AddDays(1.0))
					}, showComplete: true, inTodayOrWeek: true).ToList();
				}
			}
		}
		else
		{
			if (checkMatched != CheckMatchedType.All && checkMatched != CheckMatchedType.CheckProject)
			{
				return;
			}
			list = tasks.Where(delegate(TaskBaseViewModel t)
			{
				if (t == null || t.Deleted != 0)
				{
					return false;
				}
				return TaskViewModelHelper.CheckShowInSmartList(GetProjectById(t.ProjectId), t) ? true : false;
			}).ToList();
		}
		foreach (TaskBaseViewModel task in tasks)
		{
			projectTasksModel.UnCompletedTaskIds.Remove(task.Id);
			projectTasksModel.UnCompletedNoteIds.Remove(task.Id);
		}
		if (list != null)
		{
			foreach (TaskBaseViewModel item in list.Where((TaskBaseViewModel task) => task.Status == 0 && task.Deleted == 0))
			{
				if (item.IsNote)
				{
					projectTasksModel.UnCompletedNoteIds.Add(item.Id);
				}
				else
				{
					projectTasksModel.UnCompletedTaskIds.Add(item.Id);
				}
			}
		}
		if (list2 != null && list2.Count > 0)
		{
			foreach (TaskBaseViewModel item2 in list2)
			{
				projectTasksModel.UnCompletedItemIds.Remove(item2.Id);
			}
		}
		if (list3 == null || list3.Count <= 0)
		{
			return;
		}
		foreach (TaskBaseViewModel item3 in list3)
		{
			if (item3.Status == 0)
			{
				projectTasksModel.UnCompletedItemIds.Add(item3.Id);
			}
		}
	}

	public static void OnTaskRemoved(TaskBaseViewModel task)
	{
		if (task == null || string.IsNullOrEmpty(task.Id))
		{
			return;
		}
		foreach (ProjectTaskIds value in _projectTasksDict.Values)
		{
			if (value != null)
			{
				value.UnCompletedTaskIds?.Remove(task.Id);
				value.UnCompletedNoteIds?.Remove(task.Id);
			}
		}
	}

	public static async void OnCheckItemsChanged(List<TaskBaseViewModel> models)
	{
		bool changed = false;
		models = models?.Where((TaskBaseViewModel m) => m != null).ToList();
		if (models == null || models.Count == 0)
		{
			return;
		}
		foreach (ProjectTaskIds value in _projectTasksDict.Values)
		{
			if (await GetMatchedItems(value, models))
			{
				changed = true;
			}
		}
		if (changed)
		{
			TryDelayLoadCount();
			await Task.Delay(200);
		}
	}

	private static async Task<bool> GetMatchedItems(ProjectTaskIds projectTasksModel, List<TaskBaseViewModel> items)
	{
		if (items == null || items.Count == 0 || projectTasksModel == null)
		{
			return false;
		}
		bool result = false;
		List<TaskBaseViewModel> list = null;
		ProjectIdentity project = projectTasksModel.Project;
		if (!(project is TodayProjectIdentity))
		{
			if (!(project is TomorrowProjectIdentity))
			{
				if (!(project is WeekProjectIdentity))
				{
					if (project is FilterProjectIdentity filterProjectIdentity)
					{
						list = TaskService.GetCheckItemsMatchedFilter(filterProjectIdentity.Filter, items);
					}
				}
				else
				{
					list = TaskViewModelHelper.GetItems(items, null, null, null, new List<FilterDatePair>
					{
						new FilterDatePair(DateTime.Today, DateTime.Today.AddDays(7.0))
					}, showComplete: true, inTodayOrWeek: true).ToList();
				}
			}
			else
			{
				list = TaskViewModelHelper.GetItems(items, null, null, null, new List<FilterDatePair>
				{
					new FilterDatePair(DateTime.Today.AddDays(1.0), DateTime.Today.AddDays(2.0))
				}, showComplete: true, inTodayOrWeek: true).ToList();
			}
		}
		else
		{
			list = TaskViewModelHelper.GetItems(items, null, null, null, new List<FilterDatePair>
			{
				new FilterDatePair(DateTime.Today, DateTime.Today.AddDays(1.0))
			}, showComplete: true, inTodayOrWeek: true).ToList();
		}
		foreach (TaskBaseViewModel item in items)
		{
			if (projectTasksModel.UnCompletedItemIds.Remove(item.Id))
			{
				result = true;
			}
		}
		if (list != null && list.Count > 0)
		{
			foreach (TaskBaseViewModel item2 in list)
			{
				if (item2.Status == 0 && projectTasksModel.UnCompletedItemIds.Add(item2.Id))
				{
					result = true;
				}
			}
		}
		return result;
	}

	public static async void OnCheckItemsRemoved(List<TaskBaseViewModel> items)
	{
		bool flag = false;
		foreach (ProjectTaskIds value in _projectTasksDict.Values)
		{
			if (value == null)
			{
				continue;
			}
			foreach (TaskBaseViewModel item in items)
			{
				if (value.UnCompletedItemIds.Remove(item.Id))
				{
					flag = true;
				}
			}
		}
		if (flag)
		{
			TryDelayLoadCount();
		}
	}

	public static void RemoveIdsModelByKey(string tag)
	{
		try
		{
			_projectTasksDict.TryRemove(tag, out var _);
		}
		catch (Exception)
		{
		}
	}

	public static async void ResetIds(bool all = false)
	{
		List<Task> list = new List<Task>
		{
			ResetProjectTaskIds(new TodayProjectIdentity()),
			ResetProjectTaskIds(new TomorrowProjectIdentity()),
			ResetProjectTaskIds(new WeekProjectIdentity())
		};
		foreach (FilterModel filter in CacheManager.GetFilters())
		{
			list.Add(ResetProjectTaskIds(new FilterProjectIdentity(filter)));
		}
		if (all)
		{
			list.Add(ResetProjectTaskIds(new AllProjectIdentity()));
			list.Add(ResetProjectTaskIds(new AssignToMeProjectIdentity()));
			foreach (TagModel tag in CacheManager.GetTags())
			{
				list.Add(ResetProjectTaskIds(new TagProjectIdentity(tag)));
			}
			foreach (ProjectModel project in CacheManager.GetProjects())
			{
				list.Add(ResetProjectTaskIds(new NormalProjectIdentity(project)));
			}
			foreach (ProjectGroupModel projectGroup in CacheManager.GetProjectGroups())
			{
				list.Add(ResetProjectTaskIds(new GroupProjectIdentity(projectGroup)));
			}
		}
		await Task.WhenAny(Task.WhenAll(list), Task.Delay(1000));
		TryDelayLoadCount();
	}

	public static void Clear()
	{
		_projectTasksDict.Clear();
		_projectDict.Clear();
		TaskCountCache.Clear();
	}

	public static void RemoveEventCache(ProjectIdentity project)
	{
		if (project != null)
		{
			_projectEventCount.TryRemove(project.QueryId, out var _);
		}
	}
}
