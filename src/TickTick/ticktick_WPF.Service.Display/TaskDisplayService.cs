using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Filter;
using ticktick_WPF.Util.Provider;
using ticktick_WPF.Util.Sort;
using ticktick_WPF.Util.Sync;
using ticktick_WPF.Util.Sync.ClosedLoader;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Completed;
using ticktick_WPF.Views.Habit;
using ticktick_WPF.Views.Project;
using ticktick_WPF.Views.Search;

namespace ticktick_WPF.Service.Display;

public static class TaskDisplayService
{
	public static async Task<List<TaskBaseViewModel>> GetDisplayTaskInAll()
	{
		List<TaskBaseViewModel> tasks = TaskViewModelHelper.GetModel(null, null, null, null, showComplete: true, showCheckItem: false, inTodayOrWeek: false, inAll: true, null, null, LogicType.Or, withChild: true);
		List<TaskBaseViewModel> list = tasks;
		list.AddRange((await GetCalendarEventsInSpan(DateTime.Now.Date, DateTime.Now.Date.AddDays(90.0))).Item2);
		return tasks;
	}

	public static async Task<(int, List<TaskBaseViewModel>)> GetCalendarEventsInSpan(DateTime start, DateTime due, bool onlyCount = false)
	{
		var (item, item2) = await CalendarEventDao.GetTaskDisplayModelsBetweenSpan(start, due, inCal: false, null, null, LogicType.Or, onlyCount);
		return (item, item2);
	}

	public static async Task<List<TaskBaseViewModel>> GetDisplayTaskInToday()
	{
		List<TaskBaseViewModel> tasks = TaskViewModelHelper.GetModel(null, null, null, new List<FilterDatePair>
		{
			new FilterDatePair(DateTime.Today, DateTime.Today.AddDays(1.0))
		}, showComplete: true, LocalSettings.Settings.ShowSubtasks, inTodayOrWeek: true, inAll: true, null, null, LogicType.Or, withChild: true);
		tasks.AddRange(await GetCountdownsInToday());
		tasks.AddRange(await GetCalendarEventsInToday());
		List<HabitBaseViewModel> collection = SortHabitDisplayModels(await GetHabitsInToday());
		tasks.AddRange(collection);
		if (LocalSettings.Settings.UserPreference?.TimeTable?.ShowInSmart == true)
		{
			tasks.AddRange(await GetCourseViewModelsInSpan(DateTime.Today, DateTime.Today.AddDays(1.0)));
		}
		return tasks;
	}

	public static async Task<List<TaskBaseViewModel>> GetDisplayTaskInDate(string dateStamp)
	{
		DateTime dateTime = DateTime.ParseExact(dateStamp, "yyyyMMdd", CultureInfo.InvariantCulture);
		List<TaskBaseViewModel> tasks = TaskViewModelHelper.GetModel(null, null, null, new List<FilterDatePair>
		{
			new FilterDatePair(dateTime, dateTime.AddDays(1.0))
		}, showComplete: true, LocalSettings.Settings.ShowSubtasks, inTodayOrWeek: false, inAll: true, null, null, LogicType.Or, withChild: true);
		List<TaskBaseViewModel> item = (await GetCalendarEventsInSpan(dateTime, dateTime.AddDays(1.0))).Item2;
		tasks.AddRange(item);
		tasks.AddRange(await GetCountdownsInToday());
		return tasks;
	}

	public static List<HabitBaseViewModel> SortHabitDisplayModels(List<HabitBaseViewModel> models)
	{
		Dictionary<string, HabitSectionModel> sectionDict;
		if (models.Count > 1)
		{
			sectionDict = HabitSectionCache.GetSectionDict();
			models.Sort(delegate(HabitBaseViewModel left, HabitBaseViewModel right)
			{
				long num = GetHabitSectionOrder(left.Habit);
				long num2 = GetHabitSectionOrder(right.Habit);
				return (num != num2) ? num.CompareTo(num2) : left.SortOrder.CompareTo(right.SortOrder);
			});
		}
		return models;
		long GetHabitSectionOrder(HabitModel habit)
		{
			long num = 0L;
			if (habit == null || habit.SectionId == null || !sectionDict.ContainsKey(habit.SectionId))
			{
				return LocalSettings.Settings.HabitDefaultOrder;
			}
			return sectionDict[habit.SectionId].SortOrder;
		}
	}

	public static async Task<List<CountDownBaseViewModel>> GetCountdownsInTomorrow()
	{
		return (await CountdownDao.GetSmartListCountdowns(1)).Select((CountdownModel it) => new CountDownBaseViewModel(it)).ToList();
	}

	public static async Task<List<CountDownBaseViewModel>> GetCountdownsInToday()
	{
		return (await CountdownDao.GetSmartListCountdowns()).Select((CountdownModel it) => new CountDownBaseViewModel(it)).ToList();
	}

	public static async Task<List<CountDownBaseViewModel>> GetCountdownsInWeek()
	{
		return (await CountdownDao.GetSmartListCountdowns(2)).Select((CountdownModel it) => new CountDownBaseViewModel(it)).ToList();
	}

	public static async Task<List<HabitBaseViewModel>> GetHabitsInToday()
	{
		List<HabitBaseViewModel> result = new List<HabitBaseViewModel>();
		if (LocalSettings.Settings.ShowHabit && LocalSettings.Settings.HabitInToday)
		{
			List<HabitModel> habits = await HabitDao.GetNeedCheckHabits();
			List<HabitCheckInModel> monthCheckIns = await HabitCheckInDao.GetCheckInsInSpan(DateTime.Today.AddDays(-30.0), DateTime.Today.AddDays(1.0));
			if (habits != null && habits.Any())
			{
				foreach (HabitModel habit in habits)
				{
					List<HabitCheckInModel> list = monthCheckIns.Where((HabitCheckInModel c) => c.HabitId == habit.Id).ToList();
					HabitBaseViewModel model = new HabitBaseViewModel(habit);
					HabitCheckInModel habitCheckInModel = list.FirstOrDefault((HabitCheckInModel v) => v.HabitId == habit.Id && v.CheckinStamp == DateTime.Today.ToString("yyyyMMdd", CultureInfo.InvariantCulture));
					if (habitCheckInModel != null)
					{
						model.HabitCheckIn = habitCheckInModel;
						if (habitCheckInModel.Value >= habitCheckInModel.Goal || habitCheckInModel.CheckStatus == 1)
						{
							model.CompletedTime = habitCheckInModel.CheckinTime;
							model.Status = (habitCheckInModel.IsComplete() ? 2 : (habitCheckInModel.IsUnComplete() ? (-1) : 0));
						}
					}
					bool flag = model.Status != 0;
					if (!flag)
					{
						flag = await HabitUtils.IsHabitValidInToday(habit, list);
					}
					if (flag)
					{
						result.Add(model);
					}
				}
			}
		}
		return result;
	}

	public static async Task<List<TaskBaseViewModel>> GetCalendarEventsInToday()
	{
		return (await GetCalendarEventsInSpan(DateTime.Now.Date, DateTime.Now.Date.AddDays(1.0))).Item2;
	}

	public static async Task<List<TaskBaseViewModel>> GetDisplayTaskInTomorrow()
	{
		List<TaskBaseViewModel> tasks = TaskViewModelHelper.GetModel(null, null, null, new List<FilterDatePair>
		{
			new FilterDatePair(DateTime.Today.AddDays(1.0), DateTime.Today.AddDays(2.0))
		}, showComplete: true, LocalSettings.Settings.ShowSubtasks, inTodayOrWeek: false, inAll: true, null, null, LogicType.Or, withChild: true);
		tasks.AddRange(await GetCalendarEventsInTomorrow());
		tasks.AddRange(await GetCountdownsInTomorrow());
		if (LocalSettings.Settings.UserPreference.TimeTable?.ShowInSmart ?? false)
		{
			tasks.AddRange(await GetCourseViewModelsInSpan(DateTime.Today.AddDays(1.0), DateTime.Today.AddDays(2.0)));
		}
		return tasks;
	}

	public static async Task<List<TaskBaseViewModel>> GetCourseViewModelsInSpan(DateTime start, DateTime end)
	{
		return (await ScheduleService.GetCoursesInSpan(start, end, checkArchive: true)).Select((CourseDisplayModel c) => new TaskBaseViewModel(c)).ToList();
	}

	public static async Task<List<TaskBaseViewModel>> GetCalendarEventsInTomorrow()
	{
		return (await GetCalendarEventsInSpan(DateTime.Now.Date.AddDays(1.0), DateTime.Now.Date.AddDays(2.0))).Item2;
	}

	public static async Task<List<TaskBaseViewModel>> GetDisplayTaskInWeek()
	{
		List<TaskBaseViewModel> tasks = TaskViewModelHelper.GetModel(null, null, null, new List<FilterDatePair>
		{
			new FilterDatePair(DateTime.Today, DateTime.Today.AddDays(7.0))
		}, showComplete: true, LocalSettings.Settings.ShowSubtasks, inTodayOrWeek: true, inAll: true, null, null, LogicType.Or, withChild: true);
		List<TaskBaseViewModel> events = await GetCalendarEventsInWeek();
		List<HabitBaseViewModel> habits = SortHabitDisplayModels(await GetHabitsInToday());
		tasks.AddRange(await GetCountdownsInWeek());
		tasks.AddRange(events);
		tasks.AddRange(habits);
		if (LocalSettings.Settings.UserPreference.TimeTable?.ShowInSmart ?? false)
		{
			tasks.AddRange(await GetCourseViewModelsInSpan(DateTime.Today, DateTime.Today.AddDays(7.0)));
		}
		return tasks;
	}

	public static async Task<List<TaskBaseViewModel>> GetCalendarEventsInWeek()
	{
		return (await GetCalendarEventsInSpan(DateTime.Now.Date, DateTime.Now.Date.AddDays(7.0))).Item2;
	}

	public static async Task<List<TaskBaseViewModel>> GetDisplayTaskInTag(string tag)
	{
		List<string> tags = (from t in CacheManager.GetTags()
			where t.name == tag || t.parent == tag
			select t.name).ToList();
		return TaskViewModelHelper.GetModel(null, null, tags, null, !LocalSettings.Settings.HideComplete, showCheckItem: false, inTodayOrWeek: false, inAll: false, null, null, LogicType.Or, withChild: true);
	}

	public static async Task<List<TaskBaseViewModel>> GetDisplayTaskInSearch(SearchKind kind, string searchKey, SearchFilterViewModel filter, List<string> tags = null, bool local = false, List<string> exceptTaskIds = null, SearchCategory? categoryOverride = null)
	{
		if (kind == SearchKind.Task && string.IsNullOrEmpty(searchKey))
		{
			return (await SearchHelper.GetRecentCreatedSearchModels(15, exceptTaskIds)).Select((TaskSearchModel it) => it.SourceModel).ToList();
		}
		SearchFilterModel filterModel = filter.ToSearchFilter();
		if (categoryOverride.HasValue)
		{
			filterModel.Category = categoryOverride.Value;
		}
		bool flag = filterModel.HasFilterConditions();
		int num;
		if (filterModel.Category == SearchCategory.Task && string.IsNullOrWhiteSpace(searchKey))
		{
			if (tags == null || tags.Count <= 0)
			{
				num = ((!flag) ? 1 : 0);
				goto IL_0161;
			}
		}
		num = 0;
		goto IL_0161;
		IL_0161:
		bool shouldLoadTaskCategoryResults = (byte)num != 0;
		int num2;
		if (string.IsNullOrEmpty(searchKey))
		{
			num2 = ((tags != null && tags.Count > 0) ? 1 : 0);
		}
		else
		{
			num2 = 1;
		}
		if (((uint)num2 | (flag ? 1u : 0u)) != 0 || filterModel.Category == SearchCategory.CalendarEvent || shouldLoadTaskCategoryResults)
		{
			filterModel.ExcludeTaskIds = exceptTaskIds;
			List<TaskSearchModel> list = await SearchHelper.GetTaskSearchModels(filterModel, tags);
			searchKey = searchKey?.Trim().ToLower();
			if (!filter.Searched && !local)
			{
				SearchHelper.SearchFilter.Searched = true;
				if (!shouldLoadTaskCategoryResults)
				{
					SearchHelper.TrySearchFromService(searchKey, filterModel);
				}
			}
			List<string> list2 = searchKey?.Split(' ').ToList();
			list2?.Remove("");
			if (list.Count > 0)
			{
				List<TaskSearchModel> list3 = new List<TaskSearchModel>();
				HashSet<string> hashSet = null;
				if (string.IsNullOrEmpty(searchKey))
				{
					return GetSearchModels(list.Select((TaskSearchModel m) => m.SourceModel).ToList(), new List<TaskBaseViewModel>());
				}
				List<TaskSearchModel> list4 = list.Where((TaskSearchModel item) => item.Contains(searchKey)).ToList();
				if (list4.Any())
				{
					hashSet = new HashSet<string>(list4.Select((TaskSearchModel m) => m.Id).ToList());
				}
				if (list2.Count > 0)
				{
					foreach (TaskSearchModel item in list)
					{
						if (hashSet != null && hashSet.Contains(item.Id))
						{
							continue;
						}
						string name = item.GetText();
						bool flag2 = true;
						foreach (string item2 in list2)
						{
							if (name.Contains(item2))
							{
								Utils.ReplaceFirst(ref name, item2, "");
								continue;
							}
							flag2 = false;
							break;
						}
						if (flag2)
						{
							list3.Add(item);
						}
					}
				}
				if (list4.Count > 0 || list3.Count > 0)
				{
					return GetSearchModels(list4.Select((TaskSearchModel m) => m.SourceModel).ToList(), list3.Select((TaskSearchModel m) => m.SourceModel).ToList());
				}
			}
		}
		return new List<TaskBaseViewModel>();
	}

	public static List<TaskBaseViewModel> GetSearchModels(List<TaskBaseViewModel> exactSearchTasks, List<TaskBaseViewModel> fussySearchTasks)
	{
		HashSet<string> exactSearchIds = new HashSet<string>(exactSearchTasks.Select((TaskBaseViewModel t) => t.Id));
		exactSearchTasks.AddRange(fussySearchTasks);
		exactSearchTasks.Sort(delegate(TaskBaseViewModel a, TaskBaseViewModel b)
		{
			if (a.Status == 0 && b.Status != 0)
			{
				return -1;
			}
			if (a.Status != 0 && b.Status == 0)
			{
				return 1;
			}
			if (exactSearchIds.Contains(a.Id) && !exactSearchIds.Contains(b.Id))
			{
				return -1;
			}
			if (!exactSearchIds.Contains(a.Id) && exactSearchIds.Contains(b.Id))
			{
				return 1;
			}
			if (a.Status == 0 && b.Status == 0)
			{
				if (!a.StartDate.HasValue && b.StartDate.HasValue)
				{
					return 1;
				}
				if (a.StartDate.HasValue && !b.StartDate.HasValue)
				{
					return -1;
				}
				if (a.StartDate.HasValue && b.StartDate.HasValue)
				{
					int num = DateSortHelper.CompareTaskByDate(a, b, outDateFirst: false);
					if (num != 0)
					{
						return num;
					}
				}
			}
			if (a.Status != 0 && b.Status != 0 && a.CompletedTime.HasValue && b.CompletedTime.HasValue)
			{
				return b.CompletedTime.Value.CompareTo(a.CompletedTime.Value);
			}
			int left = (a.IsNote ? (-1) : (a.IsEvent ? (-2) : a.Priority));
			int right = (b.IsNote ? (-1) : (b.IsEvent ? (-2) : b.Priority));
			int num2 = ComparePriority(left, right);
			if (num2 != 0)
			{
				return num2;
			}
			if (a.IsEvent && b.IsEvent)
			{
				return a.Title?.CompareTo(b.Title) ?? 0;
			}
			return (a.ProjectOrder != b.ProjectOrder) ? a.ProjectOrder.CompareTo(b.ProjectOrder) : a.SortOrder.CompareTo(b.SortOrder);
		});
		return exactSearchTasks;
	}

	private static int ComparePriority(int left, int right)
	{
		if (left > right)
		{
			return -1;
		}
		if (left < right)
		{
			return 1;
		}
		return 0;
	}

	public static async Task<List<TaskBaseViewModel>> GetDisplayTaskInAssigneeToMe(ProjectIdentity project)
	{
		return TaskViewModelHelper.GetModel(null, null, null, null, showComplete: true, showCheckItem: false, inTodayOrWeek: false, inAll: false, new List<string> { "me" }, null, LogicType.Or, withChild: true);
	}

	public static async Task<List<TaskBaseViewModel>> GetDisplayTasksInAbandoned()
	{
		ClosedTaskWithFilterLoader completionLoader = ClosedTaskWithFilterLoader.AbandonedLoader;
		LoadStatus status = completionLoader.CompletedProjectStatus;
		ClosedFilterViewModel filter = AbandonedProjectIdentity.Filter;
		CompletedFilterData completedFilterData = await filter.GetCompletedFilterData();
		DateTime? fromTime = completedFilterData.FromTime;
		DateTime toTime = completedFilterData.ToTime;
		List<TaskBaseViewModel> closedModels = TaskViewModelHelper.GetClosedModels(fromTime, toTime, completedFilterData.ProjectIds, status.Count, LocalSettings.Settings.ShowSubtasks, -1, !filter.ShowAll);
		if (closedModels.Count < 50 && filter.Changed)
		{
			filter.Changed = false;
			completionLoader.TryLoadTasks(null, loadMore: false, checkNum: true, delegate
			{
				filter.Changed = true;
			});
		}
		return closedModels;
	}

	public static async Task<List<TaskBaseViewModel>> GetDisplayTasksInCompleted()
	{
		ClosedTaskWithFilterLoader completionLoader = ClosedTaskWithFilterLoader.CompletionLoader;
		LoadStatus status = completionLoader.CompletedProjectStatus;
		ClosedFilterViewModel filter = CompletedProjectIdentity.Filter;
		CompletedFilterData completedFilterData = await filter.GetCompletedFilterData();
		DateTime? fromTime = completedFilterData.FromTime;
		DateTime toTime = completedFilterData.ToTime;
		List<TaskBaseViewModel> list = TaskViewModelHelper.GetClosedModels(fromTime, toTime, completedFilterData.ProjectIds, status.Count, LocalSettings.Settings.ShowSubtasks, 2, !filter.ShowAll);
		if (list.Count < 50 && filter.Changed)
		{
			filter.Changed = false;
			completionLoader.TryLoadTasks(null, loadMore: false, checkNum: true, delegate
			{
				filter.Changed = true;
			});
		}
		if (!filter.ShowAll)
		{
			list = list.Where((TaskBaseViewModel m) => m.CompletedUser == LocalSettings.Settings.LoginUserId).ToList();
		}
		return list;
	}

	private static async Task<List<TaskBaseViewModel>> GetSingleFilterDisplayTask(CardViewModel model, bool getCompleted, bool needLoadChecklist)
	{
		List<TaskBaseViewModel> list = new List<TaskBaseViewModel>();
		if (model != null)
		{
			if (!(model is PriorityCardViewModel priorityCardViewModel))
			{
				if (!(model is DateCardViewModel dateCardViewModel))
				{
					if (!(model is AssigneeCardViewModel assigneeCardViewModel))
					{
						if (!(model is ProjectOrGroupCardViewModel projectOrGroupCardViewModel))
						{
							if (!(model is TagCardViewModel tagCardViewModel))
							{
								if (!(model is TaskTypeViewModel { Values: var values } taskTypeViewModel))
								{
									if (model is KeywordsViewModel { Keyword: var keyword } keywordCard)
									{
										List<TaskBaseViewModel> list2 = await GetDisplayTaskInSearch(SearchKind.Normal, keyword, new SearchFilterViewModel
										{
											StatusFilter = ((!getCompleted) ? StatusFilter.Uncompleted : StatusFilter.All),
											Searched = true
										});
										if (keywordCard.LogicType == LogicType.Not)
										{
											HashSet<string> matchedIds = new HashSet<string>(list2.Select((TaskBaseViewModel task) => task.Id));
											list = (from task in TaskViewModelHelper.GetModel(null, null, null, null, getCompleted, needLoadChecklist)
												where !matchedIds.Contains(task.Id)
												select task).ToList();
										}
										else
										{
											list = list2;
										}
									}
								}
								else if (taskTypeViewModel.LogicType == LogicType.Not && values != null && values.Count > 1)
								{
									list = new List<TaskBaseViewModel>();
								}
								else
								{
									TaskType taskType = ((values == null || values.Count != 1) ? TaskType.TaskAndNote : ((values[0] != "task") ? TaskType.Note : TaskType.Task));
									if (taskTypeViewModel.LogicType == LogicType.Not)
									{
										taskType = ((taskType != TaskType.Note) ? TaskType.Note : TaskType.Task);
									}
									list = TaskViewModelHelper.GetModel(null, null, null, null, getCompleted, needLoadChecklist, inTodayOrWeek: false, inAll: false, null, null, LogicType.Or, withChild: false, inCal: false, orTag: false, taskType);
								}
							}
							else
							{
								list = TaskViewModelHelper.GetModel(null, null, tagCardViewModel.Values, null, getCompleted, needLoadChecklist, inTodayOrWeek: false, inAll: false, null, null, tagCardViewModel.LogicType);
							}
						}
						else
						{
							list = TaskViewModelHelper.GetModel(projectOrGroupCardViewModel.GroupIds, projectOrGroupCardViewModel.Values, null, null, getCompleted, needLoadChecklist, inTodayOrWeek: false, inAll: false, null, null, projectOrGroupCardViewModel.LogicType);
						}
					}
					else
					{
						list = TaskViewModelHelper.GetModel(null, null, null, null, getCompleted, needLoadChecklist, inTodayOrWeek: false, inAll: false, assigneeCardViewModel.Values, null, assigneeCardViewModel.LogicType);
					}
				}
				else
				{
					List<FilterDatePair> filterDatePairs = FilterUtils.GetFilterDatePairs(dateCardViewModel.Values);
					list = TaskViewModelHelper.GetModel(null, null, null, filterDatePairs, getCompleted, needLoadChecklist, inTodayOrWeek: false, inAll: false, null, null, dateCardViewModel.LogicType);
				}
			}
			else
			{
				list = TaskViewModelHelper.GetModel(null, null, null, null, getCompleted, needLoadChecklist, inTodayOrWeek: false, inAll: false, null, priorityCardViewModel.Values, priorityCardViewModel.LogicType);
			}
		}
		return list ?? new List<TaskBaseViewModel>();
	}

	public static async Task<List<TaskBaseViewModel>> GetDisplayTaskInFilter(string rule, bool getCompleted = true, bool getCheckItem = false, bool withChild = true, bool inCal = false, bool inAll = false)
	{
		if (rule == null)
		{
			return new List<TaskBaseViewModel>();
		}
		List<TaskBaseViewModel> tasks;
		if (Parser.GetFilterRuleType(rule) == 0)
		{
			tasks = await GetDisplayTaskInNormalFilter(Parser.ToNormalModel(rule), getCompleted, getCheckItem, withChild, inCal, inAll);
		}
		else
		{
			AdvancedFilterViewModel advancedModel = Parser.ToAdvanceModel(rule);
			if (!TaskService.IsAdvanceModelValid(advancedModel.CardList))
			{
				return new List<TaskBaseViewModel>();
			}
			tasks = await GetDisplayTaskInAdvancedFilter(advancedModel, getCompleted, getCheckItem);
			List<TaskBaseViewModel> list = await GetDisplayEventsInAdvancedFilter(advancedModel, inCal);
			if (list != null && list.Any())
			{
				List<TaskBaseViewModel> list2 = tasks;
				IEnumerable<TaskBaseViewModel> collection;
				if (!advancedModel.FilterEvent())
				{
					collection = list.Where((TaskBaseViewModel eve) => eve.EventTaskId == null || !TaskCache.ExistTask(eve.EventTaskId));
				}
				else
				{
					IEnumerable<TaskBaseViewModel> enumerable = list;
					collection = enumerable;
				}
				list2.AddRange(collection);
			}
			if (withChild)
			{
				List<TaskBaseViewModel> pinCheckChildren = TaskCache.GetPinCheckChildren(tasks.Select((TaskBaseViewModel t) => t.Id).ToList());
				tasks.AddRange(pinCheckChildren);
			}
		}
		return tasks ?? new List<TaskBaseViewModel>();
	}

	public static async Task<List<TaskBaseViewModel>> GetDisplayEventsInFilter(FilterModel filter, bool inCal)
	{
		if (Parser.GetFilterRuleType(filter.rule) == 0)
		{
			NormalFilterViewModel normal = Parser.ToNormalModel(filter.rule);
			List<TaskBaseViewModel> item = (await GetDisplayEventsInNormalFilter(normal, inCal)).Item2;
			if (item.Any())
			{
				if (normal.Groups.Any() || normal.Projects.Any())
				{
					return item;
				}
				return item.Where((TaskBaseViewModel eve) => eve.EventTaskId == null || !TaskCache.ExistTask(eve.EventTaskId)).ToList();
			}
		}
		else
		{
			AdvancedFilterViewModel advanced = Parser.ToAdvanceModel(filter.rule);
			List<TaskBaseViewModel> list = await GetDisplayEventsInAdvancedFilter(advanced, inCal);
			if (list != null && list.Any())
			{
				if (advanced.FilterEvent())
				{
					return list;
				}
				return list.Where((TaskBaseViewModel eve) => eve.EventTaskId == null || !TaskCache.ExistTask(eve.EventTaskId)).ToList();
			}
		}
		return null;
	}

	public static async Task<int> GetDisplayEventsCountInFilter(FilterModel filter, bool inCal)
	{
		if (Parser.GetFilterRuleType(filter.rule) == 0)
		{
			return (await GetDisplayEventsInNormalFilter(Parser.ToNormalModel(filter.rule), inCal)).Item1;
		}
		return (await GetDisplayEventsInAdvancedFilter(Parser.ToAdvanceModel(filter.rule), inCal)).Count;
	}

	private static async Task<List<TaskBaseViewModel>> GetDisplayEventsInAdvancedFilter(AdvancedFilterViewModel advanced, bool inCal)
	{
		List<CardViewModel> conds = advanced.CardList.Where((CardViewModel card) => card.Type == CardType.Filter).ToList();
		List<CardViewModel> logics = advanced.CardList.Where((CardViewModel card) => card.Type == CardType.LogicAnd || card.Type == CardType.LogicOr).ToList();
		switch (conds.Count)
		{
		case 1:
			return await GetSingleFilterDisplayEvents(conds[0], inCal);
		case 2:
		{
			List<TaskBaseViewModel> first = await GetSingleFilterDisplayEvents(conds[0], inCal);
			List<TaskBaseViewModel> second3 = await GetSingleFilterDisplayEvents(conds[1], inCal);
			if (logics.Count == 1)
			{
				switch (logics[0].Type)
				{
				case CardType.LogicAnd:
					return TaskService.GetInterSetOfModels(first, second3);
				case CardType.LogicOr:
					return TaskService.GetUnionOfModels(first, second3);
				}
			}
			break;
		}
		case 3:
		{
			List<TaskBaseViewModel> first = await GetSingleFilterDisplayEvents(conds[0], inCal);
			List<TaskBaseViewModel> second = await GetSingleFilterDisplayEvents(conds[1], inCal);
			List<TaskBaseViewModel> second2 = await GetSingleFilterDisplayEvents(conds[2], inCal);
			if (logics.Count == 2)
			{
				CardType type = logics[0].Type;
				CardType type2 = logics[1].Type;
				List<TaskBaseViewModel> list = null;
				switch (type)
				{
				case CardType.LogicAnd:
					list = TaskService.GetInterSetOfModels(first, second);
					break;
				case CardType.LogicOr:
					list = TaskService.GetUnionOfModels(first, second);
					break;
				}
				if (list == null)
				{
					list = new List<TaskBaseViewModel>();
				}
				switch (type2)
				{
				case CardType.LogicAnd:
					return TaskService.GetInterSetOfModels(list, second2);
				case CardType.LogicOr:
					return TaskService.GetUnionOfModels(list, second2);
				}
			}
			break;
		}
		}
		return new List<TaskBaseViewModel>();
	}

	private static async Task<(int, List<TaskBaseViewModel>)> GetDisplayEventsInNormalFilter(NormalFilterViewModel normal, bool inCal)
	{
		if (normal.Assignees.Any() || normal.Tags.Any() || normal.Priorities.Any() || normal.Status.Any() || normal.TaskTypes.Any() || (normal.Projects.Count > 0 && normal.Projects.All((string t) => t != "Calendar5959a2259161d16d23a4f272")))
		{
			return (0, new List<TaskBaseViewModel>());
		}
		List<string> list = normal.DueDates.ToList();
		if (list.Count == 0)
		{
			list.Add("all");
		}
		List<TaskBaseViewModel> item = (await GetDisplayEventsInDates(list, inCal, normal.Keywords.FirstOrDefault())).Item2;
		item = FilterEventsByCalendar(normal, item);
		return (item.Count, item);
	}

	private static List<TaskBaseViewModel> FilterEventsByCalendar(NormalFilterViewModel normal, List<TaskBaseViewModel> events)
	{
		if (events == null || events.Count == 0)
		{
			return new List<TaskBaseViewModel>();
		}
		if (normal.Calendars.Contains("#allSubscribe") || (!normal.CalendarAccounts.Any() && !normal.Calendars.Any() && (normal.Projects.Count == 0 || normal.Projects.Contains("Calendar5959a2259161d16d23a4f272"))))
		{
			return events;
		}
		HashSet<string> calendarIds = new HashSet<string>(normal.Calendars.Where((string id) => id != "#allSubscribe"));
		if (normal.CalendarAccounts.Any())
		{
			HashSet<string> accountIds = new HashSet<string>(normal.CalendarAccounts);
			foreach (string item in from calendar in CacheManager.GetBindCalendars()
				where CalendarUtils.ContainsAccountId(accountIds, calendar.AccountId)
				select calendar.Id)
			{
				calendarIds.Add(item);
			}
		}
		if (calendarIds.Count <= 0)
		{
			return new List<TaskBaseViewModel>();
		}
		return events.Where((TaskBaseViewModel e) => CalendarUtils.ContainsCalendarId(calendarIds, e.CalendarId)).ToList();
	}

	private static async Task<List<TaskBaseViewModel>> GetSingleFilterDisplayEvents(CardViewModel model, bool inCal)
	{
		List<TaskBaseViewModel> list = new List<TaskBaseViewModel>();
		if (model != null)
		{
			if (!(model is DateCardViewModel dateCardViewModel))
			{
				if (!(model is ProjectOrGroupCardViewModel projectCard))
				{
					if (!(model is TaskTypeViewModel))
					{
						if (model is KeywordsViewModel keywordsViewModel)
						{
							list = (await GetDisplayEventsInDates(new List<string> { "all" }, inCal, keywordsViewModel.Keyword, keywordsViewModel.LogicType)).Item2;
						}
					}
					else
					{
						list = new List<TaskBaseViewModel>();
					}
				}
				else if (ShouldLoadCalendarEventsForProjectFilter(projectCard))
				{
					list = (await GetDisplayEventsInDates(new List<string> { "all" }, inCal)).Item2;
					list = FilterEventsForProjectCard(list, projectCard);
				}
			}
			else
			{
				list = (await GetDisplayEventsInDates(dateCardViewModel.Values, inCal, null, dateCardViewModel.LogicType)).Item2;
			}
		}
		return list ?? new List<TaskBaseViewModel>();
	}

	private static List<TaskBaseViewModel> FilterEventsForProjectCard(List<TaskBaseViewModel> events, ProjectOrGroupCardViewModel projectCard)
	{
		if (projectCard == null || projectCard.LogicType != LogicType.Not)
		{
			return FilterEventsByCalendar(events, projectCard?.Calendars, projectCard?.CalendarAccounts);
		}
		bool num = projectCard.Values.Contains("Calendar5959a2259161d16d23a4f272");
		bool flag = HasSpecificCalendarFilter(projectCard);
		if (num && !flag)
		{
			return new List<TaskBaseViewModel>();
		}
		List<TaskBaseViewModel> list;
		if (!flag)
		{
			list = events;
			if (list == null)
			{
				return new List<TaskBaseViewModel>();
			}
		}
		else
		{
			list = ExcludeEventsByCalendar(events, projectCard.Calendars, projectCard.CalendarAccounts);
		}
		return list;
	}

	private static bool ShouldLoadCalendarEventsForProjectFilter(ProjectOrGroupCardViewModel projectCard)
	{
		if (projectCard == null)
		{
			return false;
		}
		bool flag = projectCard.Values.Contains("Calendar5959a2259161d16d23a4f272");
		if (projectCard.LogicType != LogicType.Not)
		{
			return flag;
		}
		bool flag2 = HasSpecificCalendarFilter(projectCard);
		if (flag && !flag2)
		{
			return false;
		}
		if (!flag2)
		{
			return projectCard.Values.Any(CalendarUtils.IsAllListId);
		}
		return true;
	}

	private static bool HasSpecificCalendarFilter(ProjectOrGroupCardViewModel projectCard)
	{
		if (!projectCard.CalendarAccounts.Any((string id) => !string.IsNullOrEmpty(id)))
		{
			return projectCard.Calendars.Any((string id) => !string.IsNullOrEmpty(id) && id != "#allSubscribe");
		}
		return true;
	}

	private static List<TaskBaseViewModel> FilterEventsByCalendar(List<TaskBaseViewModel> events, List<string> calendarIds, List<string> accountIds)
	{
		if (events == null || events.Count == 0)
		{
			return new List<TaskBaseViewModel>();
		}
		if ((calendarIds != null && calendarIds.Contains("#allSubscribe")) || ((calendarIds == null || calendarIds.Count == 0) && (accountIds == null || accountIds.Count == 0)))
		{
			return events;
		}
		HashSet<string> ids = new HashSet<string>(calendarIds?.Where((string id) => id != "#allSubscribe") ?? Enumerable.Empty<string>());
		if (accountIds != null && accountIds.Count > 0)
		{
			HashSet<string> accounts = new HashSet<string>(accountIds);
			foreach (string item in from calendar in CacheManager.GetBindCalendars()
				where CalendarUtils.ContainsAccountId(accounts, calendar.AccountId)
				select calendar.Id)
			{
				ids.Add(item);
			}
		}
		if (ids.Count <= 0)
		{
			return new List<TaskBaseViewModel>();
		}
		return events.Where((TaskBaseViewModel e) => CalendarUtils.ContainsCalendarId(ids, e.CalendarId)).ToList();
	}

	private static List<TaskBaseViewModel> ExcludeEventsByCalendar(List<TaskBaseViewModel> events, List<string> calendarIds, List<string> accountIds)
	{
		if (events == null || events.Count == 0)
		{
			return new List<TaskBaseViewModel>();
		}
		if ((calendarIds != null && calendarIds.Contains("#allSubscribe")) || ((calendarIds == null || calendarIds.Count == 0) && (accountIds == null || accountIds.Count == 0)))
		{
			return new List<TaskBaseViewModel>();
		}
		HashSet<string> ids = new HashSet<string>(calendarIds?.Where((string id) => id != "#allSubscribe") ?? Enumerable.Empty<string>());
		if (accountIds != null && accountIds.Count > 0)
		{
			HashSet<string> accounts = new HashSet<string>(accountIds);
			foreach (string item in from calendar in CacheManager.GetBindCalendars()
				where CalendarUtils.ContainsAccountId(accounts, calendar.AccountId)
				select calendar.Id)
			{
				ids.Add(item);
			}
		}
		if (ids.Count <= 0)
		{
			return events;
		}
		return events.Where((TaskBaseViewModel e) => !CalendarUtils.ContainsCalendarId(ids, e.CalendarId)).ToList();
	}

	private static async Task<List<TaskBaseViewModel>> GetDisplayTaskInAdvancedFilter(AdvancedFilterViewModel advanced, bool getCompleted, bool withSubTask = false)
	{
		List<CardViewModel> conds = advanced.CardList.Where((CardViewModel card) => card.Type == CardType.Filter).ToList();
		List<CardViewModel> logics = advanced.CardList.Where((CardViewModel card) => card.Type == CardType.LogicAnd || card.Type == CardType.LogicOr).ToList();
		DateCardViewModel dateCardViewModel = (DateCardViewModel)conds.FirstOrDefault((CardViewModel c) => c is DateCardViewModel);
		bool num = withSubTask && dateCardViewModel?.Values != null && (dateCardViewModel.Values.Any((string v) => v != "nodue") || dateCardViewModel.LogicType == LogicType.Not);
		bool needLoadChecklist = false;
		bool secondLoadChecklist = false;
		bool thirdLoadChecklist = false;
		if (num)
		{
			switch (conds.IndexOf(dateCardViewModel))
			{
			case 0:
				needLoadChecklist = true;
				switch (conds.Count)
				{
				case 2:
					secondLoadChecklist = logics[0].Type == CardType.LogicAnd;
					break;
				case 3:
					secondLoadChecklist = logics[0].Type == CardType.LogicAnd;
					thirdLoadChecklist = logics[1].Type == CardType.LogicAnd;
					break;
				}
				break;
			case 1:
				secondLoadChecklist = true;
				switch (conds.Count)
				{
				case 2:
					needLoadChecklist = logics[0].Type == CardType.LogicAnd;
					break;
				case 3:
					needLoadChecklist = logics[0].Type == CardType.LogicAnd;
					thirdLoadChecklist = logics[1].Type == CardType.LogicAnd;
					break;
				}
				break;
			case 2:
				thirdLoadChecklist = true;
				if (conds.Count == 3)
				{
					secondLoadChecklist = logics[1].Type == CardType.LogicAnd;
					needLoadChecklist = secondLoadChecklist;
				}
				break;
			}
		}
		switch (conds.Count)
		{
		case 1:
			return await GetSingleFilterDisplayTask(conds[0], getCompleted, needLoadChecklist);
		case 2:
		{
			List<TaskBaseViewModel> first = await GetSingleFilterDisplayTask(conds[0], getCompleted, needLoadChecklist);
			List<TaskBaseViewModel> second3 = await GetSingleFilterDisplayTask(conds[1], getCompleted, secondLoadChecklist);
			if (logics.Count == 1)
			{
				switch (logics[0].Type)
				{
				case CardType.LogicAnd:
					return TaskService.GetInterSetOfModels(first, second3);
				case CardType.LogicOr:
					return TaskService.GetUnionOfModels(first, second3);
				}
			}
			break;
		}
		case 3:
		{
			List<TaskBaseViewModel> first = await GetSingleFilterDisplayTask(conds[0], getCompleted, needLoadChecklist);
			List<TaskBaseViewModel> second = await GetSingleFilterDisplayTask(conds[1], getCompleted, secondLoadChecklist);
			List<TaskBaseViewModel> second2 = await GetSingleFilterDisplayTask(conds[2], getCompleted, thirdLoadChecklist);
			if (logics.Count == 2)
			{
				CardType type = logics[0].Type;
				CardType type2 = logics[1].Type;
				List<TaskBaseViewModel> list = null;
				switch (type)
				{
				case CardType.LogicAnd:
					list = TaskService.GetInterSetOfModels(first, second);
					break;
				case CardType.LogicOr:
					list = TaskService.GetUnionOfModels(first, second);
					break;
				}
				if (list == null)
				{
					list = new List<TaskBaseViewModel>();
				}
				switch (type2)
				{
				case CardType.LogicAnd:
					return TaskService.GetInterSetOfModels(list, second2);
				case CardType.LogicOr:
					return TaskService.GetUnionOfModels(list, second2);
				}
			}
			break;
		}
		}
		return new List<TaskBaseViewModel>();
	}

	private static async Task<(int, List<TaskBaseViewModel>)> GetDisplayEventsInDates(List<string> dates, bool inCal, string keyWord = null, LogicType logicType = LogicType.Or, bool onlyCount = false)
	{
		var (item, item2) = await CalendarEventDao.GetTaskDisplayModelsBetweenSpan(DateTime.Now.Date, DateTime.Now.Date.AddDays(180.0), inCal, dates, keyWord, logicType, onlyCount, checkConnect: false);
		return (item, item2);
	}

	public static async Task<List<TaskBaseViewModel>> GetDisplayTaskInNormalFilter(NormalFilterViewModel normal, bool getCompleted, bool getSubtask, bool withChild = true, bool inCal = false, bool inAll = false)
	{
		if (normal != null)
		{
			List<FilterDatePair> startEndPairs = null;
			if (normal.DueDates != null && normal.DueDates.Count > 0)
			{
				startEndPairs = FilterUtils.GetFilterDatePairs(normal.DueDates);
				List<string> list = normal.DueDates.ToList();
				list.Remove("nodue");
				list.Remove("recurring");
				if (list.Count == 0)
				{
					getSubtask = false;
				}
			}
			else
			{
				getSubtask = false;
			}
			TaskType taskType = ((normal.TaskTypes.Count != 1) ? TaskType.TaskAndNote : ((normal.TaskTypes[0] != "task") ? TaskType.Note : TaskType.Task));
			List<string> keywords = normal.Keywords;
			string keyword = ((keywords != null && keywords.Count > 0) ? normal.Keywords[0] : null);
			List<TaskBaseViewModel> tasks = TaskViewModelHelper.GetModel(normal.Groups, normal.Projects, normal.Tags, startEndPairs, getCompleted, getSubtask, inTodayOrWeek: false, inAll, normal.Assignees, normal.Priorities, LogicType.Or, withChild, inCal, orTag: false, taskType, keyword);
			List<TaskBaseViewModel> item = (await GetDisplayEventsInNormalFilter(normal, inCal)).Item2;
			if (item.Any())
			{
				if (normal.Groups.Any() || normal.Projects.Any())
				{
					tasks.AddRange(item);
				}
				else
				{
					tasks.AddRange(item.Where((TaskBaseViewModel eve) => eve.EventTaskId == null || !TaskCache.ExistTask(eve.EventTaskId)));
				}
			}
			return tasks;
		}
		return new List<TaskBaseViewModel>();
	}

	public static async Task<List<TaskBaseViewModel>> GetCalendarArrangeTasks(ProjectExtra projectFilter)
	{
		FilterModel filterModel = null;
		List<string> filterIds = projectFilter.FilterIds;
		if (filterIds != null && filterIds.Count > 0)
		{
			filterModel = CacheManager.GetFilters().FirstOrDefault((FilterModel f) => f.id == projectFilter.FilterIds[0] && f.deleted != 1);
		}
		List<TaskBaseViewModel> source;
		if (filterModel != null)
		{
			source = await GetDisplayTaskInFilter(filterModel.rule, getCompleted: false);
			HashSet<string> notPermitProjectIds = new HashSet<string>(from p in CacheManager.GetProjects()
				where !p.IsProjectPermit()
				select p.id);
			source = source.Where(delegate(TaskBaseViewModel t)
			{
				if (LocalSettings.Settings.ArrangeTaskDateType == 1)
				{
					if (!t.StartDate.HasValue)
					{
						return false;
					}
					if ((t.DueDate.HasValue && t.DueDate.Value > DateTime.Today) || (!t.DueDate.HasValue && t.StartDate >= DateTime.Today))
					{
						return false;
					}
				}
				else if (t.StartDate.HasValue)
				{
					return false;
				}
				if (projectFilter.IsAll && !TaskViewModelHelper.CheckShowInSmartList(CacheManager.GetProjectById(t.ProjectId), t))
				{
					return false;
				}
				return (!notPermitProjectIds.Any() || !notPermitProjectIds.Contains(t.ProjectId) || t.Assignee == LocalSettings.Settings.LoginUserId) && t.Status == 0;
			}).ToList();
		}
		else
		{
			source = TaskViewModelHelper.GetTasksInArrange(projectFilter);
		}
		string inServerBoxId = LocalSettings.Settings.InServerBoxId;
		foreach (TaskBaseViewModel item in source)
		{
			if (item.ProjectId == inServerBoxId)
			{
				item.ProjectOrder = long.MinValue;
			}
		}
		if (!LocalSettings.Settings.ExtraSettings.ShowNoteInCalArrange)
		{
			source = source.Where((TaskBaseViewModel t) => !t.IsNote).ToList();
		}
		return source;
	}

	public static async Task<List<TaskBaseViewModel>> GetDisplayTasksInTrash(bool isPerson)
	{
		return TaskViewModelHelper.GetTrashModels(isPerson);
	}

	public static async Task<List<TaskBaseViewModel>> GetDisplayTaskInGroup(string groupId)
	{
		return TaskViewModelHelper.GetModel(new List<string> { groupId }, null, null, null, !LocalSettings.Settings.HideComplete);
	}

	public static List<TaskBaseViewModel> GetDisplayTasksInProject(string projectId, bool hideComplete)
	{
		List<TaskBaseViewModel> list = TaskCache.GetModelsInProject(projectId);
		if (hideComplete)
		{
			list = list.Where((TaskBaseViewModel t) => t.Status == 0).ToList();
		}
		return list;
	}
}
