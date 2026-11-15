using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TickTickDao;
using TickTickModels;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Util.ReminderTime;

namespace ticktick_WPF.Service;

public static class ScheduleService
{
	public static ConcurrentDictionary<string, List<CourseDetailModel>> CourseDetailDict = new ConcurrentDictionary<string, List<CourseDetailModel>>();

	public static ConcurrentDictionary<long, BlockingList<CourseDisplayModel>> CourseDisplayModelDict = new ConcurrentDictionary<long, BlockingList<CourseDisplayModel>>();

	public static async Task GetRemoteSchedules()
	{
		List<CourseScheduleModel> remotes = await Communicator.GetAllCourseSchedules();
		if (remotes == null)
		{
			return;
		}
		List<CourseScheduleModel> locals = await ScheduleDao.GetAllSchedulesAsync();
		bool flag = false;
		foreach (CourseScheduleModel remote in remotes)
		{
			CourseScheduleModel courseScheduleModel = locals.FirstOrDefault((CourseScheduleModel m) => m.Id == remote.Id);
			if (courseScheduleModel != null)
			{
				locals.Remove(courseScheduleModel);
				if (courseScheduleModel.Etag != remote.Etag || courseScheduleModel.WeekCount != remote.WeekCount)
				{
					remote._Id = courseScheduleModel._Id;
					remote.SyncStatus = 2;
					remote.UserId = LocalSettings.Settings.LoginUserId;
					await ScheduleDao.UpdateRemoteScheduleAsync(remote);
					flag = true;
				}
			}
			else
			{
				remote.SyncStatus = 2;
				await ScheduleDao.AddRemoteScheduleAsync(remote);
				flag = true;
			}
		}
		foreach (CourseScheduleModel item in locals)
		{
			if (item.SyncStatus == 2)
			{
				await ScheduleDao.DeleteScheduleAsync(item);
				flag = true;
			}
		}
		if (flag)
		{
			CourseDisplayModelDict.Clear();
			TaskCountCache.TryReloadSmartCount();
			DataChangedNotifier.OnScheduleChanged();
			if (ABTestManager.IsNewRemindCalculate())
			{
				await CourseReminderCalculator.InitCourseReminders();
			}
			else
			{
				ReminderCalculator.AssembleReminders();
			}
		}
	}

	private static async Task<List<CourseDisplayModel>> GetCoursesBetweenDate(DateTime start, DateTime end)
	{
		int num = (int)(end - start).TotalDays;
		bool flag = true;
		for (int i = 0; i < num; i++)
		{
			if (!CourseDisplayModelDict.ContainsKey(start.AddDays(i).Ticks))
			{
				flag = false;
				break;
			}
		}
		if (flag)
		{
			return GetCoursesBetweenDateInDict(start, Math.Max(num, 1));
		}
		return await GetCoursesBetweenDateAsync(start, end);
	}

	public static async Task<List<CourseDisplayModel>> GetCoursesInSpan(DateTime start, DateTime end, bool checkArchive)
	{
		List<CourseDisplayModel> courses = await GetCoursesBetweenDate(start, end);
		List<CourseDisplayModel> result = new List<CourseDisplayModel>();
		if (courses.Count > 0)
		{
			List<string> list = await ArchivedDao.GetArchivedKeys(ArchiveKind.Course);
			foreach (CourseDisplayModel item in courses)
			{
				if (item == null)
				{
					continue;
				}
				item.Archived = false;
				if (list != null && list.Contains(item.UniqueId))
				{
					item.Archived = true;
					if (checkArchive)
					{
						continue;
					}
				}
				result.Add(item);
			}
		}
		return result;
	}

	private static List<CourseDisplayModel> GetCoursesBetweenDateInDict(DateTime start, int days)
	{
		List<CourseDisplayModel> list = new List<CourseDisplayModel>();
		for (int i = 0; i < days; i++)
		{
			if (CourseDisplayModelDict.TryGetValue(start.Date.AddDays(i).Ticks, out var value))
			{
				List<CourseDisplayModel> collection = value.ToList();
				list.AddRange(collection);
			}
		}
		return list;
	}

	private static async Task<List<CourseDisplayModel>> GetCoursesBetweenDateAsync(DateTime start, DateTime end)
	{
		List<CourseDisplayModel> result = new List<CourseDisplayModel>();
		List<CourseScheduleModel> list = await ScheduleDao.GetAllSchedulesAsync();
		if (list == null)
		{
			return result;
		}
		int days = (int)(end - start).TotalDays;
		foreach (CourseScheduleModel schedule in list)
		{
			if (schedule.StartDate > end || string.IsNullOrEmpty(schedule.LessonTimesStr))
			{
				continue;
			}
			int startWeek = DateUtils.GetWeekNum(start, schedule.StartDate);
			if (schedule.WeekCount > 0 && startWeek > schedule.WeekCount)
			{
				continue;
			}
			int endWeek = DateUtils.GetWeekNum(end, schedule.StartDate);
			List<CourseModel> list2 = await ScheduleDao.GetCoursedByScheduleIdAsync(schedule.Id);
			if (list2 == null || list2.Count == 0)
			{
				continue;
			}
			Dictionary<int, string[]> dictionary = JsonConvert.DeserializeObject<Dictionary<int, string[]>>(schedule.LessonTimesStr);
			if (dictionary == null || dictionary.Count == 0)
			{
				continue;
			}
			List<string> reminders = (string.IsNullOrEmpty(schedule.RemindersStr) ? null : JsonConvert.DeserializeObject<List<string>>(schedule.RemindersStr));
			foreach (CourseModel item5 in list2)
			{
				if (string.IsNullOrEmpty(item5.ItemsStr))
				{
					continue;
				}
				List<CourseDetailModel> list3 = (item5.Items = GetCourseDetailModelsFromJson(item5.ItemsStr));
				for (int i = 0; i < list3.Count; i++)
				{
					CourseDetailModel courseDetailModel = list3[i];
					if (courseDetailModel.Weeks == null || courseDetailModel.Weeks.Length == 0 || !dictionary.ContainsKey(courseDetailModel.StartLesson))
					{
						continue;
					}
					string[] array = dictionary[courseDetailModel.StartLesson];
					if (array == null || array.Length != 2 || !dictionary.ContainsKey(courseDetailModel.EndLesson))
					{
						continue;
					}
					string[] array2 = dictionary[courseDetailModel.EndLesson];
					if (array2 == null || array2.Length != 2)
					{
						continue;
					}
					(int, int) hourAndMinuteInString = DateUtils.GetHourAndMinuteInString(dictionary[courseDetailModel.StartLesson][0]);
					int item = hourAndMinuteInString.Item1;
					int item2 = hourAndMinuteInString.Item2;
					(int, int) hourAndMinuteInString2 = DateUtils.GetHourAndMinuteInString(dictionary[courseDetailModel.EndLesson][1]);
					int item3 = hourAndMinuteInString2.Item1;
					int item4 = hourAndMinuteInString2.Item2;
					int[] weeks = courseDetailModel.Weeks;
					foreach (int num in weeks)
					{
						if (num >= startWeek && num <= endWeek && (schedule.WeekCount <= 0 || num <= schedule.WeekCount))
						{
							DateTime dateByWeek = DateUtils.GetDateByWeek(schedule.StartDate, num, courseDetailModel.Weekday);
							if (dateByWeek >= schedule.StartDate && dateByWeek >= start && dateByWeek < end)
							{
								DateTime start2 = DateUtils.SetHourAndMinuteOnly(dateByWeek, item, item2);
								DateTime end2 = DateUtils.SetHourAndMinuteOnly(dateByWeek, item3, item4);
								result.Add(new CourseDisplayModel(item5, start2, end2)
								{
									Reminders = reminders,
									Room = courseDetailModel.Room,
									EndLesson = courseDetailModel.EndLesson,
									StartLesson = courseDetailModel.StartLesson,
									Teacher = courseDetailModel.Teacher,
									ScheduleName = schedule.Name,
									Index = i
								});
							}
						}
					}
				}
			}
		}
		for (int k = 0; k < days; k++)
		{
			long ticks = start.AddDays(k).Ticks;
			if (!CourseDisplayModelDict.ContainsKey(ticks))
			{
				CourseDisplayModelDict[ticks] = new BlockingList<CourseDisplayModel>();
			}
		}
		foreach (CourseDisplayModel model in result)
		{
			long ticks2 = model.CourseStart.Date.Ticks;
			if (CourseDisplayModelDict.ContainsKey(ticks2))
			{
				BlockingList<CourseDisplayModel> blockingList = CourseDisplayModelDict[ticks2];
				blockingList.RemoveAll((CourseDisplayModel m) => m.Id == model.Id && m.StartLesson == model.StartLesson);
				blockingList.Add(model);
			}
		}
		return GetCoursesBetweenDateInDict(start, days);
	}

	public static List<CourseDetailModel> GetCourseDetailModelsFromJson(string itemsStr)
	{
		List<CourseDetailModel> list;
		if (CourseDetailDict.ContainsKey(itemsStr))
		{
			list = CourseDetailDict[itemsStr];
		}
		else
		{
			list = JsonConvert.DeserializeObject<List<CourseDetailModel>>(itemsStr);
			CourseDetailDict[itemsStr] = list;
		}
		return list;
	}

	public static string GetDaysDescription(DateTime targetDate, bool isLunar = false)
	{
		DateTime today = DateTime.Today;
		int days = (targetDate.Date - today).Days;
		string empty = string.Empty;
		if (isLunar)
		{
			LunarUtils.ChineseCalendar chineseCalendar = new LunarUtils.ChineseCalendar(targetDate);
			string text = (chineseCalendar.IsChineseLeapMonth ? "闰" : string.Empty);
			empty = text + DateUtils.GetChineseMonthName(chineseCalendar.ChineseMonth) + DateUtils.GetChineseDayName(chineseCalendar.ChineseDay) + " (" + DateUtils.FormatYearMonthDay(targetDate, DateFormatLen.Middle) + ")";
		}
		else
		{
			empty = DateUtils.FormatYearMonthDay(targetDate, DateFormatLen.Middle) ?? "";
		}
		object key;
		switch (days)
		{
		case 0:
			return empty;
		default:
			key = "reminder_xx_days_until";
			break;
		case 1:
			key = "reminder_xx_day_until";
			break;
		}
		return string.Format(Utils.GetString((string)key), Math.Abs(days), empty);
	}

	public static async Task<List<ReminderModel>> LoadCourseReminders()
	{
		List<ReminderModel> result = new List<ReminderModel>();
		List<CourseDisplayModel> courses = await GetCoursesInSpan(DateTime.Today, DateTime.Today.AddDays(1.0), checkArchive: true);
		Dictionary<string, ReminderDelayModel> dictionary = (await ReminderDelayDao.GetDelayModelByType("course")).Where((ReminderDelayModel delay) => delay.SyncStatus != -1).ToList().ToDictionaryEx((ReminderDelayModel d) => d.ObjId, (ReminderDelayModel d) => d);
		foreach (CourseDisplayModel item in courses)
		{
			if (item.Reminders == null)
			{
				continue;
			}
			DateTime? dateTime = null;
			if (!string.IsNullOrEmpty(item.Id) && dictionary.ContainsKey(item.Id))
			{
				ReminderDelayModel reminderDelayModel = dictionary[item.Id];
				if (reminderDelayModel.RemindTime == item.CourseStart && reminderDelayModel.NextTime.HasValue)
				{
					dateTime = ((reminderDelayModel.NextTime >= DateTime.Now) ? reminderDelayModel.NextTime : ((DateTime?)null));
					if (dateTime.HasValue)
					{
						result.Add(new ReminderModel
						{
							Id = item.Id,
							GroupId = item.ScheduleId,
							Type = ReminderModel.RemindType.Course,
							IsAllDay = false,
							StartDate = item.CourseStart,
							DueDate = item.CourseEnd,
							ReminderTime = reminderDelayModel.NextTime.Value,
							Title = item.Title,
							Content = string.Format(Utils.GetString("LessonNum"), (item.StartLesson == item.EndLesson) ? (item.StartLesson.ToString() ?? "") : (item.StartLesson + " - " + item.EndLesson)) + (string.IsNullOrEmpty(item.Room) ? "" : (", " + item.Room)) + (string.IsNullOrEmpty(item.Teacher) ? "" : (", " + item.Teacher))
						});
					}
				}
			}
			foreach (string reminder in item.Reminders)
			{
				DateTime dateTime2 = item.CourseStart - TriggerUtils.ParseTrigger(reminder);
				TimeSpan timeSpan = dateTime2 - DateTime.Now;
				if (timeSpan.TotalMinutes >= 0.0 && timeSpan.TotalMinutes < 15.0 && (!dateTime.HasValue || dateTime2 > dateTime.Value))
				{
					result.Add(new ReminderModel
					{
						Id = item.Id,
						GroupId = item.ScheduleId,
						Type = ReminderModel.RemindType.Course,
						IsAllDay = false,
						StartDate = item.CourseStart,
						DueDate = item.CourseEnd,
						ReminderTime = dateTime2,
						Title = item.Title,
						Content = string.Format(Utils.GetString("LessonNum"), (item.StartLesson == item.EndLesson) ? (item.StartLesson.ToString() ?? "") : (item.StartLesson + " - " + item.EndLesson)) + (string.IsNullOrEmpty(item.Room) ? "" : (", " + item.Room)) + (string.IsNullOrEmpty(item.Teacher) ? "" : (", " + item.Teacher))
					});
				}
			}
		}
		return result;
	}

	public static void Clear()
	{
		CourseDetailDict.Clear();
		CourseDisplayModelDict.Clear();
	}
}
