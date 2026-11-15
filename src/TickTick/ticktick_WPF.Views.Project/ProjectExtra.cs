using System.Collections.Generic;
using System.Linq;
using System.Text;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Project;

public class ProjectExtra
{
	private const char TopDivider = '|';

	private const char InternalDivider = ',';

	private const string ProjectIdentify = "project:";

	private const string GroupIdentify = "group:";

	private const string FilterIdentify = "filter:";

	private const string TagIdentify = "tag:";

	private const string BindAccountIdentify = "bind_account:";

	private const string SubscribeCalendarIdentity = "subscribe_calendar:";

	public List<string> BindAccounts = new List<string>();

	public List<string> FilterIds = new List<string>();

	public List<string> GroupIds = new List<string>();

	public List<string> ProjectIds = new List<string>();

	public List<string> SubscribeCalendars = new List<string>();

	public List<string> Tags = new List<string>();

	public List<string> SmartIds = new List<string>();

	public bool NeedSave;

	public bool IsAll
	{
		get
		{
			if (ProjectIds.Count == 0 && GroupIds.Count == 0 && FilterIds.Count == 0 && Tags.Count == 0 && BindAccounts.Count == 0)
			{
				return SubscribeCalendars.Count == 0;
			}
			return false;
		}
	}

	public static ProjectExtra Deserialize(string data, bool withChild = true, bool needFix = true)
	{
		ProjectExtra result = new ProjectExtra();
		if (string.IsNullOrEmpty(data))
		{
			return result;
		}
		string[] array = data.Split('|');
		if (array.Length != 0)
		{
			string[] array2 = array;
			foreach (string text in array2)
			{
				if (string.IsNullOrEmpty(text))
				{
					continue;
				}
				if (text.StartsWith("project:"))
				{
					result.ProjectIds = DeserializeParts(text, "project:");
					if (needFix)
					{
						result.ProjectIds.RemoveAll((string id) => id != "Calendar5959a2259161d16d23a4f272" && id != "Habit2e4c103c57ef480997943206" && !CalendarUtils.IsAllListId(id) && !ProjectDao.IsValidProject(id));
					}
				}
				if (text.StartsWith("group:"))
				{
					result.GroupIds = DeserializeParts(text, "group:");
					if (result.GroupIds.Any())
					{
						foreach (ProjectModel item in from project in CacheManager.GetProjects()
							where project.IsValid() && !string.IsNullOrEmpty(project.groupId) && result.GroupIds.Contains(project.groupId)
							select project)
						{
							if (!result.ProjectIds.Contains(item.id))
							{
								result.ProjectIds.Add(item.id);
							}
						}
					}
				}
				if (text.StartsWith("filter:"))
				{
					result.FilterIds = DeserializeParts(text, "filter:");
				}
				if (text.StartsWith("tag:"))
				{
					List<string> tagNames = DeserializeParts(text, "tag:");
					if (tagNames.Contains("*withtags"))
					{
						result.Tags.Add("*withtags");
					}
					else
					{
						List<TagModel> tags = CacheManager.GetTags();
						foreach (TagModel tag in tags.Where((TagModel tagModel) => tagNames.Contains(tagModel.name)))
						{
							result.Tags.Add(tag.name);
							if (withChild && tag.IsParent())
							{
								result.Tags.AddRange(from t in tags
									where t.parent == tag.name && !tagNames.Contains(t.name)
									select t.name);
							}
						}
					}
				}
				if (text.StartsWith("bind_account:"))
				{
					result.BindAccounts = DeserializeParts(text, "bind_account:");
				}
				if (text.StartsWith("subscribe_calendar:"))
				{
					result.SubscribeCalendars = DeserializeParts(text, "subscribe_calendar:");
				}
			}
		}
		return result;
	}

	private static List<string> DeserializeParts(string data, string identify)
	{
		if (data.StartsWith(identify))
		{
			return data.Substring(identify.Length).Split(',').ToList();
		}
		return new List<string>();
	}

	public static string Serialize(ProjectExtra data)
	{
		StringBuilder stringBuilder = new StringBuilder();
		if (data.ProjectIds.Count > 0)
		{
			SerializeItems(stringBuilder, "project:", data.ProjectIds);
		}
		if (data.GroupIds.Count > 0)
		{
			SerializeItems(stringBuilder, "group:", data.GroupIds);
		}
		if (data.FilterIds.Count > 0)
		{
			SerializeItems(stringBuilder, "filter:", data.FilterIds);
		}
		if (data.Tags.Count > 0)
		{
			SerializeItems(stringBuilder, "tag:", data.Tags);
		}
		if (data.BindAccounts.Count > 0)
		{
			SerializeItems(stringBuilder, "bind_account:", data.BindAccounts);
		}
		if (data.SubscribeCalendars.Count > 0)
		{
			SerializeItems(stringBuilder, "subscribe_calendar:", data.SubscribeCalendars);
		}
		return stringBuilder.ToString();
	}

	private static void SerializeItems(StringBuilder result, string identity, IEnumerable<string> items)
	{
		result.Append(identity);
		result.Append(string.Join(','.ToString(), items));
		result.Append('|');
	}

	public static string FormatDisplayText(string dataString, bool onlyShowPermission = false)
	{
		ProjectExtra projectExtra = Deserialize(dataString, withChild: true, needFix: false);
		if (!projectExtra.IsAll)
		{
			List<string> list = new List<string>();
			List<string> filterIds = projectExtra.FilterIds;
			if (filterIds != null && filterIds.Count > 0)
			{
				string filterId = projectExtra.FilterIds[0];
				FilterModel filterModel = CacheManager.GetFilters().FirstOrDefault((FilterModel f) => f.id == filterId);
				if (filterModel != null)
				{
					return filterModel.name;
				}
			}
			string text = FormatDisplayText(projectExtra.ProjectIds?.Where((string id) => id != "Calendar5959a2259161d16d23a4f272").ToList() ?? new List<string>(), projectExtra.GroupIds, onlyShowPermission);
			if (text != Utils.GetString("All"))
			{
				list.Add(text);
			}
			string text2 = FormatCalendarDisplayText(projectExtra);
			if (!string.IsNullOrEmpty(text2))
			{
				list.Add(text2);
			}
			if (list.Count > 0)
			{
				return string.Join(", ", list);
			}
		}
		return Utils.GetString("All");
	}

	private static string FormatCalendarDisplayText(ProjectExtra data)
	{
		if (IsAllCalendarSelected(data))
		{
			return Utils.GetString("AllCalendars", "All Calendars");
		}
		if ((data.BindAccounts == null || data.BindAccounts.Count == 0) && (data.SubscribeCalendars == null || data.SubscribeCalendars.Count == 0))
		{
			return string.Empty;
		}
		List<string> selectedCalendarDisplayNames = GetSelectedCalendarDisplayNames(data);
		if (selectedCalendarDisplayNames.Count == 0)
		{
			return Utils.GetString("AllCalendars");
		}
		if (selectedCalendarDisplayNames.Count <= 1)
		{
			return selectedCalendarDisplayNames[0];
		}
		return string.Format(Utils.GetString("ListDisplayText"), selectedCalendarDisplayNames[0], selectedCalendarDisplayNames.Count - 1);
	}

	private static bool IsAllCalendarSelected(ProjectExtra data)
	{
		if (data == null)
		{
			return false;
		}
		bool num = data.SubscribeCalendars?.Contains("#allSubscribe") ?? false;
		bool flag = data.ProjectIds?.Contains("Calendar5959a2259161d16d23a4f272") ?? false;
		List<string> bindAccounts = data.BindAccounts;
		bool flag2 = (bindAccounts != null && bindAccounts.Any()) || (data.SubscribeCalendars?.Any((string id) => !string.IsNullOrEmpty(id) && id != "#allSubscribe") ?? false);
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

	private static List<string> GetSelectedCalendarDisplayNames(ProjectExtra data)
	{
		List<string> list = new List<string>();
		HashSet<string> selectedKeys = new HashSet<string>();
		if (data.BindAccounts != null && data.BindAccounts.Count > 0)
		{
			foreach (string accountId in data.BindAccounts.Where((string id) => !string.IsNullOrEmpty(id)))
			{
				BindCalendarAccountModel bindCalendarAccountModel = CacheManager.GetBindCalendarAccounts().FirstOrDefault((BindCalendarAccountModel item) => CalendarUtils.IsSameAccountId(item.Id, accountId));
				string key = ((bindCalendarAccountModel != null) ? ("account:" + CalendarUtils.NormalizeLocalAccountId(bindCalendarAccountModel.Id ?? accountId)) : "unavailable-local-calendar");
				AddUniqueDisplayName(list, selectedKeys, key, bindCalendarAccountModel?.DisplayName ?? Utils.GetString("LocalCalendars"));
			}
		}
		if (data.SubscribeCalendars != null && data.SubscribeCalendars.Count > 0)
		{
			List<BindCalendarModel> bindCalendars = CacheManager.GetBindCalendars();
			List<CalendarSubscribeProfileModel> subscribeCalendars = CacheManager.GetSubscribeCalendars();
			List<BindCalendarAccountModel> bindCalendarAccounts = CacheManager.GetBindCalendarAccounts();
			foreach (string calendarId in data.SubscribeCalendars.Where((string id) => !string.IsNullOrEmpty(id) && id != "#allSubscribe"))
			{
				BindCalendarModel bindCalendarModel = bindCalendars.FirstOrDefault((BindCalendarModel calendar) => CalendarUtils.IsSameCalendarId(calendar.Id, calendarId));
				CalendarSubscribeProfileModel calendarSubscribeProfileModel = subscribeCalendars.FirstOrDefault((CalendarSubscribeProfileModel calendar) => CalendarUtils.IsSameCalendarId(calendar.Id, calendarId));
				BindCalendarAccountModel bindCalendarAccountModel2 = bindCalendarAccounts.FirstOrDefault((BindCalendarAccountModel account) => CalendarUtils.IsSameAccountId(account.Id, calendarId));
				string text = bindCalendarModel?.Name ?? calendarSubscribeProfileModel?.Name ?? bindCalendarAccountModel2?.DisplayName;
				string key2 = ((bindCalendarAccountModel2 != null) ? ("account:" + CalendarUtils.NormalizeLocalAccountId(bindCalendarAccountModel2.Id ?? calendarId)) : ((text == null) ? "unavailable-local-calendar" : ("calendar:" + CalendarUtils.NormalizeLocalCalendarId(bindCalendarModel?.Id ?? calendarSubscribeProfileModel?.Id ?? calendarId))));
				AddUniqueDisplayName(list, selectedKeys, key2, text ?? Utils.GetString("LocalCalendars"));
			}
		}
		return list;
	}

	private static void AddUniqueDisplayName(List<string> names, HashSet<string> selectedKeys, string key, string displayName)
	{
		if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(displayName) && selectedKeys.Add(key))
		{
			names.Add(displayName);
		}
	}

	public static string FixExtraOnProjectChanged(ProjectModel project, string dataString)
	{
		ProjectExtra projectExtra = Deserialize(dataString);
		if (!project.IsValid() && projectExtra.ProjectIds.Contains(project.id))
		{
			projectExtra.ProjectIds.Remove(project.id);
			dataString = Serialize(projectExtra);
		}
		return dataString;
	}

	public static string FormatDisplayText(List<string> pIds, List<string> gIds, bool onlyShowPermission = false)
	{
		List<string> list = new List<string>();
		if ((pIds != null && pIds.Any()) || (gIds != null && gIds.Any()))
		{
			if (CalendarUtils.ContainsAllListId(pIds))
			{
				list.Add(Utils.GetString("AllList"));
			}
			else
			{
				List<ProjectModel> list2 = (from p in ProjectDao.GetProjectsInIdsOrGroupIds(pIds, gIds)
					where p.IsValid()
					select p).ToList();
				if (onlyShowPermission)
				{
					list2 = list2?.Where((ProjectModel p) => p.IsProjectPermit()).ToList();
				}
				if (list2 != null && list2.Count == 1)
				{
					list.Add(list2[0].name);
				}
				if (list2 != null && list2.Count > 1)
				{
					list.Add(string.Format(Utils.GetString("ListDisplayText"), list2[0].name, list2.Count - 1));
				}
			}
			if (pIds != null && pIds.Contains("Calendar5959a2259161d16d23a4f272"))
			{
				list.Add(Utils.GetString("Calendar"));
			}
			if (pIds != null && pIds.Contains("Habit2e4c103c57ef480997943206"))
			{
				list.Add(Utils.GetString("statistics_habit"));
			}
			if (list.Count <= 0)
			{
				return Utils.GetString("Expired");
			}
			return string.Join(", ", list);
		}
		return Utils.GetString("All");
	}
}
