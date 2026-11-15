using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.SyncServices;
using ticktick_WPF.Util;
using ticktick_WPF.Util.KotlinUtils;
using ticktick_WPF.Util.Sync;
using ticktick_WPF.Util.Sync.ClosedLoader;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Habit;
using ticktick_WPF.Views.Project;
using ticktick_WPF.Views.Tag;

namespace ticktick_WPF.Service.Display;

public static class CalendarDisplayService
{
	private static readonly TimeSpan CalendarAddTempTaskIgnoreLifetime = TimeSpan.FromMinutes(30.0);

	private static readonly Dictionary<string, DateTime> CalendarAddTempTaskIds = new Dictionary<string, DateTime>();

	private static DateTime _previewStartDate;

	private static DateTime lastCheckMonthTime { get; set; }

	public static void IgnoreCalendarAddTempTask(string taskId)
	{
		if (string.IsNullOrEmpty(taskId))
		{
			return;
		}
		lock (CalendarAddTempTaskIds)
		{
			CalendarAddTempTaskIds[taskId] = DateTime.UtcNow;
		}
	}

	private static void RemoveIgnoredCalendarAddTempTasks(List<CalendarDisplayModel> models)
	{
		if (models == null)
		{
			return;
		}
		HashSet<string> ignoredIds;
		lock (CalendarAddTempTaskIds)
		{
			RemoveExpiredCalendarAddTempTaskIds();
			if (!CalendarAddTempTaskIds.Any())
			{
				return;
			}
			ignoredIds = new HashSet<string>(CalendarAddTempTaskIds.Keys);
		}
		models.RemoveAll((CalendarDisplayModel model) => model != null && (model.Type == DisplayType.Task || model.Type == DisplayType.Note || model.Type == DisplayType.Derivative) && ignoredIds.Contains(model.Id));
	}

	private static void RemoveExpiredCalendarAddTempTaskIds()
	{
		DateTime now = DateTime.UtcNow;
		foreach (string item in (from pair in CalendarAddTempTaskIds
			where now - pair.Value > CalendarAddTempTaskIgnoreLifetime
			select pair.Key).ToList())
		{
			CalendarAddTempTaskIds.Remove(item);
		}
	}

	public static async Task<List<CalendarDisplayModel>> GetCalendarDisplayModels(DateTime startDate, DateTime endDate, ProjectExtra projectFilter = null)
	{
		bool showCompletedInCal = LocalSettings.Settings.ShowCompletedInCal;
		bool showCheckListInCal = LocalSettings.Settings.ShowCheckListInCal;
		projectFilter = ((projectFilter == null) ? new ProjectExtra() : new ProjectExtra
		{
			ProjectIds = projectFilter.ProjectIds.ToList(),
			GroupIds = projectFilter.GroupIds.ToList(),
			FilterIds = projectFilter.FilterIds.ToList(),
			Tags = projectFilter.Tags.ToList(),
			BindAccounts = projectFilter.BindAccounts.ToList(),
			SubscribeCalendars = projectFilter.SubscribeCalendars.ToList()
		});
		if (projectFilter.FilterIds.Any())
		{
			FilterModel filterModel = CacheManager.GetFilters().FirstOrDefault((FilterModel f) => f.id == projectFilter.FilterIds[0]);
			List<CalendarDisplayModel> list = new List<CalendarDisplayModel>();
			if (filterModel != null)
			{
				list = (from t in (await TaskDisplayService.GetDisplayTaskInFilter(filterModel.rule, showCompletedInCal, showCheckListInCal, withChild: false, inCal: true)).ToList().Where(delegate(TaskBaseViewModel t)
					{
						if (t.StartDate.HasValue)
						{
							DateTime? startDate2 = t.StartDate;
							DateTime dateTime = startDate;
							if (startDate2.HasValue && startDate2.GetValueOrDefault() >= dateTime && t.StartDate < endDate)
							{
								goto IL_01c9;
							}
						}
						if (t.DueDate.HasValue)
						{
							DateTime? startDate2 = t.DueDate;
							DateTime dateTime = startDate;
							if (startDate2.HasValue && startDate2.GetValueOrDefault() > dateTime && t.DueDate < endDate)
							{
								goto IL_01c9;
							}
						}
						if (t.StartDate.HasValue && t.DueDate.HasValue)
						{
							DateTime? startDate2 = t.StartDate;
							DateTime dateTime = startDate;
							if (startDate2.HasValue && startDate2.GetValueOrDefault() < dateTime && t.DueDate >= endDate)
							{
								goto IL_01c9;
							}
						}
						if (!t.StartDate.HasValue && !t.DueDate.HasValue && t.CompletedTime.HasValue && t.Status != 0 && t.CompletedTime >= startDate)
						{
							return t.CompletedTime < endDate;
						}
						return false;
						IL_01c9:
						return true;
					}).ToList()
					select new CalendarDisplayModel(t)).ToList();
			}
			AssemblyEventsColor(list.Where((CalendarDisplayModel m) => m.SourceViewModel.Type == DisplayType.Event).ToList());
			return list;
		}
		bool flag = IsAllCalendarScope(projectFilter);
		List<string> list2 = (flag ? new List<string>() : GetSpecificSubscribeCalendars(projectFilter));
		List<string> list3 = (flag ? new List<string>() : projectFilter.BindAccounts.ToList());
		bool flag2 = projectFilter.ProjectIds.Any((string id) => id != "Calendar5959a2259161d16d23a4f272");
		if ((flag || list2.Any() || list3.Any()) && projectFilter.GroupIds.Count == 0 && !flag2)
		{
			projectFilter.ProjectIds.RemoveAll((string id) => id == "Calendar5959a2259161d16d23a4f272");
			projectFilter.ProjectIds.Add("none");
		}
		List<FilterDatePair> startEndPairs = new List<FilterDatePair>
		{
			new FilterDatePair(startDate, endDate)
		};
		List<TaskBaseViewModel> source = TaskViewModelHelper.GetModel(projectFilter.GroupIds, projectFilter.ProjectIds, projectFilter.Tags, startEndPairs, showCompletedInCal, showCheckListInCal, inTodayOrWeek: false, projectFilter.GroupIds.Count == 0 && projectFilter.ProjectIds.Count == 0 && projectFilter.Tags.Count == 0, null, null, LogicType.Or, withChild: false, inCal: true, orTag: true).ToList();
		List<CalendarDisplayModel> models = source.Select((TaskBaseViewModel t) => new CalendarDisplayModel(t)).ToList();
		if (projectFilter.IsAll || flag || list2.Any() || list3.Any())
		{
			List<CalendarDisplayModel> list4 = await GetCalendarDisplayModelsBetweenSpan(startDate, endDate, list2, list3, showCompletedInCal);
			if (projectFilter.IsAll)
			{
				list4 = list4.Where((CalendarDisplayModel e) => string.IsNullOrEmpty(e.EventTaskId) || !TaskCache.ExistTask(e.EventTaskId)).ToList();
			}
			AssemblyEventsColor(list4);
			models.AddRange(list4);
		}
		return models;
	}

	private static void AssemblyEventsColor(List<CalendarDisplayModel> events)
	{
		List<BindCalendarModel> bindCalendars = CacheManager.GetBindCalendars();
		Dictionary<string, string> dict = new Dictionary<string, string>();
		if (bindCalendars.Any())
		{
			foreach (BindCalendarModel item in bindCalendars.Where((BindCalendarModel cal) => !dict.ContainsKey(cal.Id)))
			{
				dict.Add(item.Id, item.Color);
			}
		}
		List<CalendarSubscribeProfileModel> subscribeCalendars = CacheManager.GetSubscribeCalendars();
		if (subscribeCalendars.Any())
		{
			foreach (CalendarSubscribeProfileModel item2 in subscribeCalendars.Where((CalendarSubscribeProfileModel cal) => !dict.ContainsKey(cal.Id)))
			{
				dict.Add(item2.Id, item2.Color);
			}
		}
		if (events == null || !events.Any())
		{
			return;
		}
		foreach (CalendarDisplayModel @event in events)
		{
			@event.SourceViewModel.Color = (dict.ContainsKey(@event.SourceViewModel.CalendarId) ? dict[@event.SourceViewModel.CalendarId] : "#B1C5CF");
		}
	}

	public static async Task<List<CalendarDisplayModel>> GetRepeatDisplayModel(DateTime startDate, DateTime endDate, ProjectExtra projectFilter = null)
	{
		bool inAll = projectFilter == null || projectFilter.FilterIds.Any() || projectFilter.IsAll;
		List<TaskBaseViewModel> tasks = TaskCache.GetRepeatTasks(inAll);
		if (projectFilter != null)
		{
			if (projectFilter.FilterIds.Any())
			{
				FilterModel filterModel = CacheManager.GetFilters().FirstOrDefault((FilterModel f) => f.id == projectFilter.FilterIds[0]);
				if (filterModel != null)
				{
					List<string> filterIds = (await TaskDisplayService.GetDisplayTaskInFilter(filterModel.rule, getCompleted: false, getCheckItem: false, withChild: false)).Select((TaskBaseViewModel model) => model.Id).ToList();
					tasks = tasks.Where((TaskBaseViewModel taskBaseViewModel) => filterIds.Contains(taskBaseViewModel.Id)).ToList();
				}
			}
			else if (!projectFilter.IsAll)
			{
				List<string> projectIds = projectFilter.ProjectIds;
				List<TaskBaseViewModel> list;
				if (CalendarUtils.ContainsAllListId(projectIds))
				{
					list = tasks;
				}
				else
				{
					if (projectFilter.GroupIds != null && projectFilter.GroupIds.Any())
					{
						foreach (string groupId in projectFilter.GroupIds)
						{
							List<string> list2 = projectIds;
							list2.AddRange(await ProjectDao.GetProjectIdByGroupId(groupId));
						}
					}
					list = tasks.Where((TaskBaseViewModel taskBaseViewModel) => projectIds.Contains(taskBaseViewModel.ProjectId)).ToList();
				}
				List<TaskBaseViewModel> first = new List<TaskBaseViewModel>();
				if (projectFilter.Tags != null)
				{
					List<string> fullTags = projectFilter.Tags.Select((string tag) => "\"" + tag.ToLower().Trim() + "\"").ToList();
					if (tasks.Any())
					{
						first = ((!projectFilter.Tags.Contains("*withtags")) ? tasks.Where(delegate(TaskBaseViewModel taskBaseViewModel)
						{
							string text = taskBaseViewModel.Tag?.ToLower().Replace(" ", string.Empty).Replace("#", string.Empty)
								.Replace("＃", string.Empty)
								.Trim();
							return !string.IsNullOrEmpty(taskBaseViewModel.Tag) && fullTags.Any(text.Contains);
						}).ToList() : tasks.Where(delegate(TaskBaseViewModel taskBaseViewModel)
						{
							List<string> list3 = TagSerializer.ToTags(taskBaseViewModel.Tag);
							return list3 != null && list3.Count > 0;
						}).ToList());
					}
				}
				list.AddRange(first.Except(list));
				tasks = list;
			}
		}
		List<CalendarDisplayModel> models = new List<CalendarDisplayModel>();
		foreach (TaskBaseViewModel task in tasks)
		{
			if (!task.StartDate.HasValue)
			{
				continue;
			}
			double diffMinutes = 0.0;
			if (task.DueDate.HasValue)
			{
				diffMinutes = (task.DueDate.Value - task.StartDate.Value).TotalMinutes;
			}
			string timeZone = (((!task.IsAllDay.HasValue || !task.IsAllDay.Value) && !task.IsFloating && task.TimeZoneName != TimeZoneData.LocalTimeZoneModel?.TimeZoneName) ? task.TimeZoneName : TimeZoneData.LocalTimeZoneModel?.TimeZoneName);
			DateTime endTime = (((DateTime.MaxValue - endDate).Days >= 1) ? endDate.AddDays(1.0) : DateTime.MaxValue);
			RepeatDao.GetRepeatFlagDate(task, startDate.AddDays(-1 - (int)diffMinutes / 1440), endTime, preloadRange: false).ForEach(delegate(DateTime date)
			{
				DateTime? dateTime = null;
				if (task.DueDate.HasValue)
				{
					dateTime = TimeZoneUtils.ToLocalTime(TimeZoneUtils.LocalToTargetTzTime(date, timeZone).AddMinutes(diffMinutes), timeZone);
				}
				if (!(date >= startDate) || !(date < endDate))
				{
					DateTime? dateTime2 = dateTime;
					DateTime dateTime3 = startDate;
					if (!dateTime2.HasValue || !(dateTime2.GetValueOrDefault() > dateTime3) || !(dateTime <= endDate))
					{
						dateTime2 = dateTime;
						dateTime3 = endDate;
						if (!dateTime2.HasValue || !(dateTime2.GetValueOrDefault() > dateTime3) || !(date < startDate))
						{
							return;
						}
					}
				}
				CalendarDisplayModel item = new CalendarDisplayModel(task)
				{
					repeatDiff = (date - (task.StartDate ?? date)).TotalDays
				};
				models.Add(item);
			});
		}
		List<string> expiredProjectIds = CacheManager.GetExpiredProjectIds();
		return expiredProjectIds.Any() ? models.Where((CalendarDisplayModel model) => !expiredProjectIds.Contains(model.SourceViewModel.ProjectId)).ToList() : models;
	}

	public static async Task<List<CalendarDisplayModel>> GetRepeatDisplayModels(DateTime start, DateTime end, bool allSelected = false, ProjectExtra projectFilter = null)
	{
		List<CalendarEventModel> skipEvents = await CalendarEventDao.GetSkipEvents();
		string userId = Utils.GetCurrentUserIdInt().ToString();
		Dictionary<string, CalendarSubscribeProfileModel> subCal = (from cal in CacheManager.GetSubscribeCalendars()
			where cal.Show != "hidden" && cal.UserId == userId
			select cal).ToDictionary((CalendarSubscribeProfileModel cal) => cal.Id, (CalendarSubscribeProfileModel cal) => cal);
		List<string> subCalIds = subCal.Keys.ToList();
		List<string> accounts = (from cal in CacheManager.GetBindCalendarAccounts()
			select cal.Id).ToList();
		List<BindCalendarModel> source = (from cal in CacheManager.GetBindCalendars()
			where accounts.Contains(cal.AccountId) && cal.Show != "hidden" && cal.UserId == userId
			select cal).ToList();
		bool checkTickTaskId = projectFilter?.IsAll ?? false;
		subCalIds.AddRange(source.Select((BindCalendarModel c) => c.Id));
		if (!allSelected && projectFilter != null)
		{
			bool flag = IsAllCalendarScope(projectFilter);
			if (!projectFilter.IsAll && !flag)
			{
				List<string> subCalFilter = GetSpecificSubscribeCalendars(projectFilter);
				List<string> bindAccountFilter = projectFilter.BindAccounts ?? new List<string>();
				List<string> bindCalendarIds = (from cal in source
					where CalendarUtils.ContainsAccountId(bindAccountFilter, cal.AccountId)
					select cal.Id).ToList();
				subCalIds = subCalIds.Where((string id) => CalendarUtils.ContainsCalendarId(subCalFilter, id) || CalendarUtils.ContainsCalendarId(bindCalendarIds, id)).ToList();
			}
		}
		List<CalendarEventModel> repeatEvents = await CalendarEventDao.GetRepeatEvents();
		List<CalendarDisplayModel> calModels = new List<CalendarDisplayModel>();
		List<string> hiddenKeys = await ArchivedDao.GetArchivedKeys();
		foreach (CalendarEventModel model in repeatEvents)
		{
			CalendarEventModel calendarEventModel = model;
			if (calendarEventModel == null || !calendarEventModel.DueStart.HasValue || (checkTickTaskId && !string.IsNullOrEmpty(model.TickTaskId) && TaskCache.ExistTask(model.TickTaskId)) || !CalendarUtils.ContainsCalendarId(subCalIds, model.CalendarId))
			{
				continue;
			}
			if (!string.IsNullOrEmpty(model.ExDates))
			{
				try
				{
					model.ExDateList = JsonConvert.DeserializeObject<List<DateTime>>(model.ExDates);
				}
				catch (Exception)
				{
					model.ExDateList = null;
				}
			}
			double diff = 0.0;
			DateTime calStart = (model.IsAllDay ? model.DueStart.Value : TimeZoneUtils.LocalToTargetTzTime(model.DueStart.Value, model.TimeZone));
			if (model.DueEnd.HasValue)
			{
				diff = (model.DueEnd.Value - model.DueStart.Value).TotalMinutes;
			}
			RepeatDao.GetEventRepeatDates(model.RepeatFlag, calStart, start.AddDays(-2 - (int)diff / 1440), end.AddDays(1.0), model.TimeZone, model.IsAllDay, preloadRange: false).ForEach(delegate(DateTime date)
			{
				if (date < end)
				{
					model.DueStart = date;
					DateTime dateTime = (model.IsAllDay ? date : TimeZoneUtils.LocalToTargetTzTime(date, model.TimeZone));
					if (model.DueEnd.HasValue)
					{
						model.DueEnd = (model.IsAllDay ? dateTime.AddMinutes(diff) : TimeZoneUtils.ToLocalTime(dateTime.AddMinutes(diff), model.TimeZone));
					}
					bool flag2 = false;
					bool flag3 = false;
					if (model.ExDateList != null && model.ExDateList.Any() && model.ExDateList.Contains(dateTime.Date))
					{
						flag2 = true;
					}
					if (skipEvents.Any())
					{
						flag3 = model.IsSkipped(skipEvents, date);
					}
					if (!flag2 && !flag3)
					{
						DateTime? dueStart = model.DueStart;
						DateTime dateTime2 = start;
						if (!dueStart.HasValue || !(dueStart.GetValueOrDefault() >= dateTime2) || !(model.DueStart < end))
						{
							dueStart = model.DueEnd;
							dateTime2 = start;
							if (!dueStart.HasValue || !(dueStart.GetValueOrDefault() > dateTime2) || !(model.DueEnd <= end))
							{
								dueStart = model.DueEnd;
								dateTime2 = end;
								if (!dueStart.HasValue || !(dueStart.GetValueOrDefault() > dateTime2) || !(model.DueStart < start))
								{
									return;
								}
							}
						}
						string item = ArchivedEventDao.GenerateEventKey(new EventArchiveArgs(model));
						int? num = (hiddenKeys.Contains(item) ? new int?(2) : ((int?)null));
						string color = (subCal.ContainsKey(model.CalendarId) ? subCal[model.CalendarId].Color : null);
						if (LocalSettings.Settings.ShowCompletedInCal || num != 2)
						{
							CalendarDisplayModel item2 = new CalendarDisplayModel(model, num, color)
							{
								SourceViewModel = 
								{
									Editable = false
								}
							};
							calModels.Add(item2);
						}
					}
				}
			});
		}
		AssemblyEventsColor(calModels);
		return calModels;
	}

	public static async Task<List<CalendarDisplayModel>> GetCalendarDisplayModelsBetweenSpan(DateTime start, DateTime end, List<string> subscribes, List<string> accounts, bool withArchived = true, bool checkConnect = false)
	{
		subscribes = subscribes ?? new List<string>();
		accounts = accounts ?? new List<string>();
		bool allCal = (subscribes.Count == 0 || subscribes.Contains("#allSubscribe")) && accounts.Count == 0;
		subscribes = subscribes.Where((string id) => id != "#allSubscribe").ToList();
		List<string> calendarIds = (from c in CacheManager.GetSubscribeCalendars()
			where c.Show != "hidden" && (allCal || CalendarUtils.ContainsCalendarId(subscribes, c.Id))
			select c.Id).ToList();
		List<BindCalendarModel> bindCalendars = (from cal in CacheManager.GetBindCalendars()
			where (allCal || CalendarUtils.ContainsAccountId(accounts, cal.AccountId) || CalendarUtils.ContainsCalendarId(subscribes, cal.Id)) && cal.Show != "hidden"
			select cal).ToList();
		if (checkConnect)
		{
			List<string> connectedCalendars = await CalendarConnectService.GetConnectedCalendarIds();
			bindCalendars.RemoveAll((BindCalendarModel c) => connectedCalendars.Contains(c.OutId));
		}
		calendarIds.AddRange(bindCalendars.Select((BindCalendarModel cal) => cal.Id));
		if (!withArchived && start < DateTime.Today)
		{
			start = DateTime.Today;
		}
		List<CalendarEventModel> events = await CalendarEventDao.GetEventsBetweenSpan(start, end);
		List<CalendarEventModel> skipEvents = await CalendarEventDao.GetSkipEvents();
		List<string> list = await ArchivedDao.GetArchivedKeys();
		if (events.Any())
		{
			List<CalendarDisplayModel> list2 = new List<CalendarDisplayModel>();
			foreach (CalendarEventModel item in events)
			{
				if (calendarIds.Any() && !CalendarUtils.ContainsCalendarId(calendarIds, item.CalendarId))
				{
					continue;
				}
				if (!string.IsNullOrEmpty(item.ExDates))
				{
					try
					{
						DateTime dateTime = item.DueStart?.Date ?? default(DateTime);
						if (JsonConvert.DeserializeObject<List<DateTime>>(item.ExDates).Contains(dateTime.Date))
						{
							continue;
						}
					}
					catch (Exception)
					{
					}
				}
				if (!item.IsSkipped(skipEvents))
				{
					bool flag = list.Contains(ArchivedEventDao.GenerateEventKey(new EventArchiveArgs(item)));
					if (!(!withArchived && flag))
					{
						list2.Add(new CalendarDisplayModel(item, flag ? new int?(2) : ((int?)null)));
					}
				}
			}
			AssemblyEventsColor(list2);
			return list2;
		}
		return new List<CalendarDisplayModel>();
	}

	private static async Task<List<CalendarDisplayModel>> GetCalendarDisplayHabits(DateTime start, DateTime end)
	{
		List<CalendarDisplayModel> result = new List<CalendarDisplayModel>();
		DateTime startDate = start.Date;
		DateTime endDate = end.Date;
		if (endDate.Date == startDate.Date)
		{
			endDate = endDate.AddDays(1.0);
		}
		HashSet<string> added = new HashSet<string>();
		if (LocalSettings.Settings.ShowHabit && LocalSettings.Settings.HabitInCal)
		{
			List<HabitModel> habits = await HabitDao.GetNeedCheckHabits(checkSkip: false);
			List<HabitCheckInModel> monthCheckIns = await HabitCheckInDao.GetCheckInsInSpan(DateTime.Today.AddDays(-30.0), DateTime.Today.AddDays(1.0));
			if (DateTime.Today >= startDate && DateTime.Today < endDate)
			{
				foreach (HabitModel habit in habits)
				{
					if (habit.IsSkipToday())
					{
						continue;
					}
					List<HabitCheckInModel> list = monthCheckIns.Where((HabitCheckInModel c) => c.HabitId == habit.Id).ToList();
					HabitCheckInModel checkIn = list.FirstOrDefault((HabitCheckInModel v) => v.HabitId == habit.Id && v.CheckinStamp == DateTime.Today.ToString("yyyyMMdd", CultureInfo.InvariantCulture));
					if ((checkIn == null || (!checkIn.IsComplete() && !checkIn.IsUnComplete())) && await HabitUtils.IsHabitValidInToday(habit, list))
					{
						int status = 0;
						CalendarDisplayModel calendarDisplayModel = CalendarDisplayModel.Build(habit, status, DateTime.Today, null);
						calendarDisplayModel.CheckIn = checkIn;
						(calendarDisplayModel.SourceViewModel as HabitBaseViewModel).HabitCheckIn = checkIn;
						if (calendarDisplayModel.StartDate >= start && calendarDisplayModel.StartDate < end)
						{
							added.Add(habit.Id + DateTime.Today.ToString("yyyyMMdd", CultureInfo.InvariantCulture));
							result.Add(calendarDisplayModel);
						}
					}
				}
			}
			if (LocalSettings.Settings.ShowCompletedInCal)
			{
				List<HabitCheckInModel> list2 = await HabitCheckInDao.GetCheckInsInSpan(startDate, endDate);
				if (list2.Any())
				{
					foreach (HabitCheckInModel checkIn2 in list2)
					{
						if (!(checkIn2.Value >= checkIn2.Goal) && checkIn2.CheckStatus != 1)
						{
							continue;
						}
						HabitModel habitModel = habits.FirstOrDefault((HabitModel habitModel2) => habitModel2.Id == checkIn2.HabitId);
						if (habitModel == null)
						{
							continue;
						}
						string item = habitModel.Id + checkIn2.CheckinStamp;
						if (added.Add(item))
						{
							int status2 = (checkIn2.IsComplete() ? 2 : (checkIn2.IsUnComplete() ? (-1) : 0));
							CalendarDisplayModel calendarDisplayModel2 = CalendarDisplayModel.Build(habitModel, status2, DateTime.ParseExact(checkIn2.CheckinStamp, "yyyyMMdd", CultureInfo.InvariantCulture), checkIn2.CheckinTime);
							calendarDisplayModel2.CheckIn = checkIn2;
							(calendarDisplayModel2.SourceViewModel as HabitBaseViewModel).HabitCheckIn = checkIn2;
							if (calendarDisplayModel2.StartDate >= start && calendarDisplayModel2.StartDate < end)
							{
								result.Add(calendarDisplayModel2);
							}
						}
					}
				}
			}
		}
		return result;
	}

	public static async Task<List<CalendarDisplayModel>> GetDisplayModels(DateTime startDate, DateTime endDate, bool withCourses = false, bool withFocus = false, ProjectExtra projectFilter = null, bool pullRemote = true, bool forcePullPomo = false)
	{
		pullRemote = pullRemote && _previewStartDate != startDate;
		if (pullRemote)
		{
			_previewStartDate = startDate;
			PullRemoveData(startDate, endDate);
		}
		List<CalendarDisplayModel> tasks = await GetCalendarDisplayModels(startDate, endDate, projectFilter);
		await Task.WhenAll(CircleTask(), RepeatEvent(), HabitTask(), PomoTask(), GetCourses(), CountdownTask());
		RemoveIgnoredCalendarAddTempTasks(tasks);
		return tasks;
		async Task CircleTask()
		{
			if (LocalSettings.Settings.ShowRepeatCircles)
			{
				List<CalendarDisplayModel> list = await GetRepeatDisplayModel(startDate, endDate, projectFilter);
				if (list.Count > 0)
				{
					lock (tasks)
					{
						tasks.AddRange(list);
					}
				}
			}
		}
		async Task CountdownTask()
		{
			if (LocalSettings.Settings.EnableCountDownDays && LocalSettings.Settings.ExtraSettings.ShowCountdownInCal)
			{
				List<CalendarDisplayModel> list = await GetDisplayCountdowns(startDate, endDate);
				if (list.Any())
				{
					lock (tasks)
					{
						tasks.AddRange(list);
					}
				}
			}
		}
		async Task GetCourses()
		{
			if (withCourses && LocalSettings.Settings.ShowTimeTableInCal)
			{
				List<CalendarDisplayModel> list = await GetDisplayCourses(startDate, endDate);
				if (list.Count > 0)
				{
					list = list.Where(delegate(CalendarDisplayModel c)
					{
						if (!LocalSettings.Settings.ShowCompletedInCal && c.DueDate <= DateTime.Today)
						{
							return false;
						}
						DateTime? startDate2 = c.StartDate;
						DateTime dateTime = startDate;
						return (startDate2.HasValue && startDate2.GetValueOrDefault() >= dateTime && c.StartDate < endDate) || (c.DueDate > startDate && c.DueDate <= endDate);
					}).ToList();
					lock (tasks)
					{
						tasks.AddRange(list);
					}
				}
			}
		}
		async Task HabitTask()
		{
			List<CalendarDisplayModel> list = await GetCalendarDisplayHabits(startDate, endDate);
			if (list.Any())
			{
				lock (tasks)
				{
					tasks.AddRange(list);
				}
			}
		}
		async Task PomoTask()
		{
			if (withFocus || LocalSettings.Settings.ShowFocusRecord)
			{
				bool needPull = pullRemote;
				if (!needPull && forcePullPomo)
				{
					if ((DateTime.Today - startDate).Days < 30)
					{
						needPull = true;
					}
					else if ((DateTime.Now - lastCheckMonthTime).Seconds > 1)
					{
						PomoMonthChangeResultModel pomoMonthChangeResultModel = await Communicator.CheckPomoRecChangedMonth(LocalSettings.Settings.ExtraSettings.LastPomoChangeMonthCheckPoint);
						lastCheckMonthTime = DateTime.Now;
						if (pomoMonthChangeResultModel != null)
						{
							LocalSettings.Settings.ExtraSettings.LastPomoChangeMonthCheckPoint = pomoMonthChangeResultModel.checkPoint;
							if (pomoMonthChangeResultModel?.pomodoroRecords != null)
							{
								List<DateTime> list = new List<DateTime>();
								List<DateTime> timingChangeMonths = new List<DateTime>();
								foreach (PomoMonthResultModel pomodoroRecord in pomoMonthChangeResultModel.pomodoroRecords)
								{
									if (pomodoroRecord?.months != null)
									{
										foreach (string month in pomodoroRecord.months)
										{
											if (DateTime.TryParseExact(month, "yyyyMM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
											{
												if (pomodoroRecord.type == "pomodoro")
												{
													if (!list.Contains(result))
													{
														list.Add(result);
													}
												}
												else if (pomodoroRecord.type == "timing" && !timingChangeMonths.Contains(result))
												{
													timingChangeMonths.Add(result);
												}
											}
										}
									}
								}
								foreach (DateTime item in list)
								{
									await PomoSyncService.Pull(item, item.AddMonths(1), isPomo: true);
								}
								foreach (DateTime item2 in timingChangeMonths)
								{
									await PomoSyncService.Pull(item2, item2.AddMonths(1), isPomo: false);
								}
							}
						}
					}
				}
				if (needPull)
				{
					PomoSyncService.TryPullRemote(startDate, endDate);
				}
				List<PomodoroModel> list2 = await PomoDao.GetPomoByDateSpan(startDate, endDate);
				if (list2.Any())
				{
					List<CalendarDisplayModel> displayModels = new List<CalendarDisplayModel>();
					foreach (PomodoroModel pomo in list2)
					{
						if (pomo != null)
						{
							CalendarDisplayModel calendarDisplayModel = new CalendarDisplayModel(await PomoService.GetAdjustPomo(pomo));
							if (calendarDisplayModel.StartDate.HasValue && calendarDisplayModel.DueDate.HasValue && calendarDisplayModel.StartDate.Value.Date != calendarDisplayModel.DueDate.Value.Date)
							{
								PomoTask[] tasks2 = pomo.Tasks;
								if (tasks2 != null && tasks2.Length != 0)
								{
									DateTime value = tasks2.Max((PomoTask m) => m.EndTime);
									if (calendarDisplayModel.SourceViewModel != null)
									{
										calendarDisplayModel.SourceViewModel.DueDate = value;
									}
								}
							}
							displayModels.Add(calendarDisplayModel);
						}
					}
					lock (tasks)
					{
						tasks.AddRange(displayModels);
					}
				}
			}
		}
		async Task RepeatEvent()
		{
			List<CalendarDisplayModel> list = await GetRepeatDisplayModels(startDate, endDate, allSelected: false, projectFilter);
			if (list.Count > 0)
			{
				lock (tasks)
				{
					tasks.AddRange(list);
				}
			}
		}
	}

	private static async Task PullRemoveData(DateTime startDate, DateTime endDate)
	{
		CalendarEventLoader.PullRemoteEvent(startDate, endDate);
		if (await ClosedTaskWithFilterLoader.CalClosedLoader.LoadTasksInSpan(startDate, endDate))
		{
			GlobalEventManager.NotifyReloadCalendar();
		}
	}

	private static async Task<List<CalendarDisplayModel>> GetDisplayCourses(DateTime startDate, DateTime endDate)
	{
		return (await ScheduleService.GetCoursesInSpan(startDate, endDate, !LocalSettings.Settings.ShowCompletedInCal)).Select((CourseDisplayModel c) => new CalendarDisplayModel(c)).ToList();
	}

	private static async Task<List<CalendarDisplayModel>> GetDisplayCountdowns(DateTime startDate, DateTime endDate)
	{
		List<CalendarDisplayModel> result = new List<CalendarDisplayModel>();
		List<CountdownModel> list = await CountdownDao.GetActiveCountdowns();
		if (list != null && list.Any())
		{
			foreach (CountdownModel item2 in list)
			{
				DateTime dateAsDateTime = item2.GetDateAsDateTime();
				if (dateAsDateTime >= startDate && dateAsDateTime <= endDate && (!item2.IgnoreYear || dateAsDateTime >= item2.CreatedTime.Date))
				{
					result.Add(new CalendarDisplayModel(item2, dateAsDateTime));
				}
				if (item2.RepeatFlag == null || item2.RepeatFlag.Contains("NONE"))
				{
					continue;
				}
				List<DateTime> repeatDates = RepeatCalculator.GetRepeatDates(item2.RepeatFlag, "0", dateAsDateTime, startDate, endDate, null, null, toLocalTime: true, 0, includeToday: true, ignoreLunarIfNotLunar: true, preloadRange: false);
				if (dateAsDateTime.Month == 2 && dateAsDateTime.Day == 29 && item2.RepeatFlag.Contains("RRULE:FREQ=YEARLY"))
				{
					int result2;
					int num = ((!int.TryParse(Regex.Match(item2.RepeatFlag, "INTERVAL=(\\d+)").Groups[1]?.Value, out result2)) ? 1 : result2);
					if (num == 1)
					{
						int year = startDate.Year;
						if (!DateTime.IsLeapYear(year))
						{
							DateTime dateTime = new DateTime(year, 2, 28);
							if (repeatDates != null && !repeatDates.Contains(dateTime) && dateTime >= startDate && dateTime <= endDate && dateTime >= dateAsDateTime)
							{
								repeatDates.Add(dateTime);
							}
						}
					}
					else
					{
						for (int i = 1; i < 100; i++)
						{
							int num2 = startDate.Year + num * i;
							if (num2 - endDate.Year >= 2)
							{
								break;
							}
							DateTime dateTime2 = (DateTime.IsLeapYear(num2) ? new DateTime(num2, 2, 29) : new DateTime(num2, 2, 28));
							if (repeatDates != null && !repeatDates.Contains(dateTime2) && dateTime2 >= startDate && dateTime2 <= endDate && dateTime2 >= dateAsDateTime)
							{
								repeatDates.Add(dateTime2);
							}
						}
					}
				}
				if (repeatDates == null || repeatDates.Count <= 0)
				{
					continue;
				}
				foreach (DateTime item3 in repeatDates)
				{
					if (item3 >= startDate && item3 <= endDate && item3.Date != dateAsDateTime && (!item2.IgnoreYear || item3.Date >= item2.CreatedTime.Date))
					{
						CalendarDisplayModel item = new CalendarDisplayModel(item2, item3);
						result.Add(item);
					}
				}
			}
		}
		return result;
	}

	private static bool IsAllCalendarScope(ProjectExtra projectFilter)
	{
		if (projectFilter == null)
		{
			return false;
		}
		bool num = projectFilter.SubscribeCalendars?.Contains("#allSubscribe") ?? false;
		bool flag = projectFilter.ProjectIds?.Contains("Calendar5959a2259161d16d23a4f272") ?? false;
		List<string> bindAccounts = projectFilter.BindAccounts;
		bool flag2 = (bindAccounts != null && bindAccounts.Any()) || (projectFilter.SubscribeCalendars?.Any((string id) => !string.IsNullOrEmpty(id) && id != "#allSubscribe") ?? false);
		if (!num)
		{
			if (flag)
			{
				return !flag2;
			}
			return false;
		}
		return true;
	}

	private static List<string> GetSpecificSubscribeCalendars(ProjectExtra projectFilter)
	{
		return projectFilter?.SubscribeCalendars?.Where((string id) => !string.IsNullOrEmpty(id) && id != "#allSubscribe").ToList() ?? new List<string>();
	}
}
