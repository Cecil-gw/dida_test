using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Entity.Calendar;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Project;
using ticktick_WPF.Views.Tag;

namespace ticktick_WPF.Util;

public class CalendarUtils
{
	private const string LocalDataIdSeparator = "_local_";

	private static string themeIdCache { get; set; }

	private static string customDarkCache { get; set; }

	private static ConcurrentDictionary<YearViewCalendarDay.YearViewCalendarDayStyle, SolidColorBrush> cacheDict { get; set; } = new ConcurrentDictionary<YearViewCalendarDay.YearViewCalendarDayStyle, SolidColorBrush>();

	public static string NormalizeLocalDataId(string id)
	{
		if (string.IsNullOrEmpty(id))
		{
			return id;
		}
		int num = id.IndexOf("_local_", StringComparison.Ordinal);
		if (num >= 0)
		{
			return id.Substring(num + "_local_".Length);
		}
		return id;
	}

	public static string NormalizeLocalCalendarId(string calendarId)
	{
		return NormalizeLocalDataId(calendarId);
	}

	public static string NormalizeLocalAccountId(string accountId)
	{
		return NormalizeLocalDataId(accountId);
	}

	public static bool IsSameCalendarId(string left, string right)
	{
		return IsSameLocalDataId(left, right);
	}

	public static bool IsSameAccountId(string left, string right)
	{
		return IsSameLocalDataId(left, right);
	}

	private static bool IsSameLocalDataId(string left, string right)
	{
		if (!string.IsNullOrEmpty(left) && !string.IsNullOrEmpty(right))
		{
			if (!(left == right))
			{
				return NormalizeLocalDataId(left) == NormalizeLocalDataId(right);
			}
			return true;
		}
		return false;
	}

	public static bool ContainsCalendarId(IEnumerable<string> calendarIds, string calendarId)
	{
		return calendarIds?.Any((string id) => IsSameCalendarId(id, calendarId)) ?? false;
	}

	public static bool ContainsAccountId(IEnumerable<string> accountIds, string accountId)
	{
		return accountIds?.Any((string id) => IsSameAccountId(id, accountId)) ?? false;
	}

	public static bool IsAllListId(string projectId)
	{
		if (!(projectId == "#alllists"))
		{
			return projectId == "ProjectAll2e4c103c57ef480997943206";
		}
		return true;
	}

	public static bool IsNotConcreteProjectFilterId(string projectId)
	{
		if (!IsAllListId(projectId))
		{
			return projectId == "Calendar5959a2259161d16d23a4f272";
		}
		return true;
	}

	public static bool ContainsAllListId(IEnumerable<string> projectIds)
	{
		return projectIds?.Any(IsAllListId) ?? false;
	}

	public static string GetCalendarDefaultAddProjectId(bool isParallel = false)
	{
		ProjectExtra projectExtra = (isParallel ? LocalSettings.Settings.ExtraSettings.CalendarParallelSelectProject : ProjectExtra.Deserialize(LocalSettings.Settings.CalendarFilterData));
		if (projectExtra == null || !projectExtra.ProjectIds.Any() || ContainsAllListId(projectExtra.ProjectIds) || projectExtra.ProjectIds.Contains(TaskDefaultDao.GetDefaultSafely().ProjectId))
		{
			return TaskDefaultDao.GetDefaultSafely().ProjectId;
		}
		if (projectExtra.ProjectIds.Count > 0 && projectExtra.FilterIds.Count == 0)
		{
			List<ProjectModel> list = (from project in projectExtra.ProjectIds.Select(CacheManager.GetProjectById)
				where project?.IsEnable() ?? false
				select project).ToList();
			if (list.Count > 0)
			{
				list.Sort((ProjectModel l, ProjectModel r) => l.CompareTo(r));
				return list[0].id;
			}
		}
		return TaskDefaultDao.GetDefaultSafely().ProjectId;
	}

	public static string CheckTaskCanShown(string taskId, bool inParallel = false)
	{
		TaskBaseViewModel task = TaskCache.GetTaskById(taskId);
		if (task == null)
		{
			return null;
		}
		ProjectModel projectById = CacheManager.GetProjectById(task.ProjectId);
		if (projectById == null)
		{
			return null;
		}
		ProjectExtra projectExtra = (inParallel ? LocalSettings.Settings.ExtraSettings.CalendarParallelSelectProject : ProjectExtra.Deserialize(LocalSettings.Settings.CalendarFilterData));
		if (projectExtra.IsAll)
		{
			if (!TaskViewModelHelper.CheckShowInSmartList(projectById, task))
			{
				return Utils.GetString("ProjectHideHintInCalendar");
			}
			return null;
		}
		if (projectExtra.FilterIds.Any())
		{
			FilterModel filterById = CacheManager.GetFilterById(projectExtra.FilterIds[0]);
			if (filterById != null && !TaskService.IsTaskMatchedFilter(filterById, task, inAll: true))
			{
				return Utils.GetString("TaskNotShowHintInCalendar");
			}
		}
		if (projectExtra.ProjectIds.Any() || projectExtra.GroupIds.Any() || projectExtra.Tags.Any() || projectExtra.SubscribeCalendars.Any() || projectExtra.BindAccounts.Any())
		{
			if (ContainsAllListId(projectExtra.ProjectIds))
			{
				return null;
			}
			if (projectExtra.Tags.Contains("*withtags") && task.Tags != null && task.Tags.Any())
			{
				return null;
			}
			if (!projectExtra.ProjectIds.Contains(task.ProjectId) && !projectExtra.GroupIds.Contains(projectById.groupId) && !projectExtra.Tags.Any((string tag) => task.Tags?.Contains(tag.ToLower()) ?? false))
			{
				return Utils.GetString("TaskNotShowHintInCalendar");
			}
		}
		return null;
	}

	public static string CheckCalendarEventCanShown(CalendarEventModel calEvent, bool inParallel = false)
	{
		if (calEvent == null || string.IsNullOrEmpty(calEvent.CalendarId))
		{
			return null;
		}
		BindCalendarModel bindCalendarModel = CacheManager.GetBindCalendars().FirstOrDefault((BindCalendarModel cal) => cal.Id == calEvent.CalendarId);
		CalendarSubscribeProfileModel calendarSubscribeProfileModel = CacheManager.GetSubscribeCalendars().FirstOrDefault((CalendarSubscribeProfileModel cal) => cal.Id == calEvent.CalendarId);
		if (bindCalendarModel?.Show == "hidden" || calendarSubscribeProfileModel?.Show == "hidden")
		{
			return Utils.GetString("TaskNotShowHintInCalendar");
		}
		ProjectExtra projectExtra = (inParallel ? LocalSettings.Settings.ExtraSettings.CalendarParallelSelectProject : ProjectExtra.Deserialize(LocalSettings.Settings.CalendarFilterData));
		if (projectExtra == null || projectExtra.IsAll)
		{
			return null;
		}
		if (projectExtra.FilterIds.Any())
		{
			return null;
		}
		if (projectExtra.SubscribeCalendars.Contains("#allSubscribe") || projectExtra.SubscribeCalendars.Contains(calEvent.CalendarId))
		{
			return null;
		}
		if (bindCalendarModel != null && projectExtra.BindAccounts.Contains(bindCalendarModel.AccountId))
		{
			return null;
		}
		return Utils.GetString("TaskNotShowHintInCalendar");
	}

	public static async Task<string> CheckCheckItemCanShown(string taskId, string checkItemId, bool inParallel = false)
	{
		TaskModel taskModel = await TaskDao.GetThinTaskById(taskId);
		TaskBaseViewModel checkItemById = TaskDetailItemCache.GetCheckItemById(checkItemId);
		if (taskModel == null || checkItemById == null)
		{
			return null;
		}
		ProjectModel projectById = CacheManager.GetProjectById(taskModel.projectId);
		if (projectById == null)
		{
			return null;
		}
		ProjectExtra projectExtra = (inParallel ? LocalSettings.Settings.ExtraSettings.CalendarParallelSelectProject : ProjectExtra.Deserialize(LocalSettings.Settings.CalendarFilterData));
		if (projectExtra.IsAll)
		{
			if (!TaskViewModelHelper.CheckShowInSmartList(projectById, taskModel))
			{
				return Utils.GetString("ProjectHideHintInCalendar");
			}
			return null;
		}
		if (projectExtra.FilterIds.Any())
		{
			FilterModel filterById = CacheManager.GetFilterById(projectExtra.FilterIds[0]);
			if (filterById != null && TaskService.GetCheckItemsMatchedFilter(filterById, new List<TaskBaseViewModel> { checkItemById }).Count <= 0)
			{
				return Utils.GetString("TaskNotShowHintInCalendar");
			}
		}
		if (projectExtra.ProjectIds.Any() || projectExtra.GroupIds.Any() || projectExtra.Tags.Any() || projectExtra.SubscribeCalendars.Any() || projectExtra.BindAccounts.Any())
		{
			if (ContainsAllListId(projectExtra.ProjectIds))
			{
				return null;
			}
			List<string> taskTags = TagSerializer.ToTags(taskModel.tag);
			if (projectExtra.Tags.Contains("*withtags") && taskTags.Any())
			{
				return null;
			}
			if (!projectExtra.ProjectIds.Contains(taskModel.projectId) && !projectExtra.GroupIds.Contains(projectById.groupId) && !projectExtra.Tags.Any((string tag) => taskTags?.Contains(tag.ToLower()) ?? false))
			{
				return Utils.GetString("TaskNotShowHintInCalendar");
			}
		}
		return null;
	}

	public static SolidColorBrush GetYearViewDayCellColor(bool isShowHeatMap, int count, bool isToday, bool isCurrentMonth)
	{
		string themeId = LocalSettings.Settings.ThemeId;
		string customDarkThemeColor = LocalSettings.Settings.ExtraSettings.CustomDarkThemeColor;
		if (themeIdCache != themeId || (themeId == "DarkCustom" && customDarkThemeColor != customDarkCache))
		{
			cacheDict.Clear();
			themeIdCache = themeId;
			if (themeId == "DarkCustom")
			{
				customDarkCache = customDarkThemeColor;
			}
		}
		if (isToday)
		{
			return cacheDict.GetOrAdd(YearViewCalendarDay.YearViewCalendarDayStyle.Today, (YearViewCalendarDay.YearViewCalendarDayStyle key) => ThemeUtil.GetColor("PrimaryColor"));
		}
		if (!isShowHeatMap)
		{
			return null;
		}
		if (!isCurrentMonth || count <= 0)
		{
			return null;
		}
		Color color = ThemeUtil.GetColorValue("ColorPrimary");
		bool isDark = ThemeUtil.IsDark();
		if (count > 0 && count <= 3)
		{
			return cacheDict.GetOrAdd(YearViewCalendarDay.YearViewCalendarDayStyle.Light, (YearViewCalendarDay.YearViewCalendarDayStyle key) => new SolidColorBrush(color)
			{
				Opacity = (isDark ? 0.2 : 0.16)
			});
		}
		if (count > 3 && count <= 6)
		{
			return cacheDict.GetOrAdd(YearViewCalendarDay.YearViewCalendarDayStyle.Medium, (YearViewCalendarDay.YearViewCalendarDayStyle key) => new SolidColorBrush(color)
			{
				Opacity = (isDark ? 0.4 : 0.3)
			});
		}
		if (count > 6 && count <= 9)
		{
			return cacheDict.GetOrAdd(YearViewCalendarDay.YearViewCalendarDayStyle.MediumDeep, (YearViewCalendarDay.YearViewCalendarDayStyle key) => new SolidColorBrush(color)
			{
				Opacity = (isDark ? 0.6 : 0.5)
			});
		}
		return cacheDict.GetOrAdd(YearViewCalendarDay.YearViewCalendarDayStyle.Deep, (YearViewCalendarDay.YearViewCalendarDayStyle key) => new SolidColorBrush(color)
		{
			Opacity = (isDark ? 0.8 : 0.7)
		});
	}

	public static CalTaskColorMode GetTaskColorMode()
	{
		CalendarColorConfig obj = LocalSettings.Settings?.UserPreference?.CalendarColorConfig;
		CalTaskColorMode result = CalTaskColorMode.List;
		string value = obj?.Task?.ColorType;
		if (string.IsNullOrEmpty(value))
		{
			Enum.TryParse<CalTaskColorMode>(LocalSettings.Settings.CellColorType, ignoreCase: true, out result);
		}
		else
		{
			Enum.TryParse<CalTaskColorMode>(value, ignoreCase: true, out result);
		}
		return result;
	}
}
