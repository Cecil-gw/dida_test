using System.Collections.Generic;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;

namespace ticktick_WPF.Service;

public class SmartProjectService
{
	public const string InboxId = "inbox";

	public const string AllId = "all";

	public const string TodayId = "today";

	public const string TomorrowId = "tomorrow";

	public const string AssignToMeId = "assignToMe";

	public const string WeekId = "week";

	public static void SaveSmartProjectViewMode(string identityId, string viewMode, bool inParallel = false)
	{
		string smartNameByIdentityId = GetSmartNameByIdentityId(identityId);
		if (string.IsNullOrEmpty(smartNameByIdentityId))
		{
			return;
		}
		if (inParallel)
		{
			if (LocalSettings.Settings.ExtraSettings.SmartProjectsOption == null)
			{
				LocalSettings.Settings.ExtraSettings.SmartProjectsOption = new SmartProjectsOption();
			}
			LocalSettings.Settings.ExtraSettings.SmartProjectsOption.SaveViewMode(smartNameByIdentityId, viewMode);
		}
		else
		{
			LocalSettings.Settings.SaveSmartViewMode(smartNameByIdentityId, viewMode);
		}
	}

	public static string GetSmartProjectViewMode(string identityId, bool inParallel = false)
	{
		string smartNameByIdentityId = GetSmartNameByIdentityId(identityId);
		if (smartNameByIdentityId == "assignToMe")
		{
			return "list";
		}
		if (inParallel)
		{
			return LocalSettings.Settings.ExtraSettings.SmartProjectsOption?.GetViewModeByName(smartNameByIdentityId) ?? "list";
		}
		string text = null;
		if (LocalSettings.Settings.UserPreference?.SmartProjectsOption != null)
		{
			text = LocalSettings.Settings.UserPreference.SmartProjectsOption.GetViewModeByName(smartNameByIdentityId);
		}
		if (!string.IsNullOrEmpty(text))
		{
			return text;
		}
		return "list";
	}

	public static SmartListType? GetSmartTypeByIdentityId(string identityId)
	{
		if (identityId.StartsWith("inbox"))
		{
			return SmartListType.Inbox;
		}
		return identityId switch
		{
			"_special_id_all" => SmartListType.All, 
			"_special_id_today" => SmartListType.Today, 
			"_special_id_tomorrow" => SmartListType.Tomorrow, 
			"_special_id_week" => SmartListType.Week, 
			"_special_id_assigned" => SmartListType.Assign, 
			_ => null, 
		};
	}

	private static string GetSmartNameByIdentityId(string identityId)
	{
		if (identityId.StartsWith("inbox"))
		{
			return "inbox";
		}
		return identityId switch
		{
			"_special_id_all" => "all", 
			"_special_id_today" => "today", 
			"_special_id_tomorrow" => "tomorrow", 
			"_special_id_week" => "week", 
			"_special_id_assigned" => "assignToMe", 
			_ => null, 
		};
	}

	public static SortOption GetSmartProjectSortOption(string name, bool inKanban)
	{
		SortOption sortOption = null;
		if (LocalSettings.Settings.UserPreference?.SmartProjectsOption == null)
		{
			LocalSettings.Settings.InitSmartProjectsOption(push: false);
		}
		if (LocalSettings.Settings.UserPreference?.SmartProjectsOption != null)
		{
			sortOption = LocalSettings.Settings.UserPreference.SmartProjectsOption.GetSortOptionByName(name);
		}
		return sortOption ?? new SortOption
		{
			groupBy = "dueDate",
			orderBy = "dueDate"
		};
	}

	private static SortOption GetOldSortOptionBySmartId(string name, bool inKanban = false)
	{
		return name switch
		{
			"inbox" => SortOption.GetOptionInSmartString(LocalSettings.Settings.SortTypeOfInbox), 
			"all" => SortOption.GetOptionInSmartString(LocalSettings.Settings.SortTypeOfAllProject), 
			"today" => SortOption.GetOptionInSmartString(LocalSettings.Settings.SortTypeOfToday), 
			"tomorrow" => SortOption.GetOptionInSmartString(LocalSettings.Settings.SortTypeOfTomorrow), 
			"assignToMe" => SortOption.GetOptionInSmartString(LocalSettings.Settings.SortTypeOfAssignMe), 
			"week" => SortOption.GetOptionInSmartString(LocalSettings.Settings.SortTypeOfWeek), 
			_ => new SortOption
			{
				groupBy = "dueDate",
				orderBy = "dueDate"
			}, 
		};
	}

	public static List<SmartProjectOption> InitSmartProjectOptions()
	{
		return new List<SmartProjectOption>
		{
			new SmartProjectOption
			{
				Name = "inbox",
				ViewMode = "list",
				SortOption = GetOldSortOptionBySmartId("inbox")
			},
			new SmartProjectOption
			{
				Name = "all",
				ViewMode = "list",
				SortOption = GetOldSortOptionBySmartId("all")
			},
			new SmartProjectOption
			{
				Name = "today",
				ViewMode = "list",
				SortOption = GetOldSortOptionBySmartId("today")
			},
			new SmartProjectOption
			{
				Name = "tomorrow",
				ViewMode = "list",
				SortOption = GetOldSortOptionBySmartId("tomorrow")
			},
			new SmartProjectOption
			{
				Name = "assignToMe",
				ViewMode = "list",
				SortOption = GetOldSortOptionBySmartId("assignToMe")
			},
			new SmartProjectOption
			{
				Name = "week",
				ViewMode = "list",
				SortOption = GetOldSortOptionBySmartId("week")
			}
		};
	}

	public static TimelineModel GetSmartProjectTimeline(string identityId)
	{
		string smartNameByIdentityId = GetSmartNameByIdentityId(identityId);
		TimelineModel timelineModel = null;
		if (LocalSettings.Settings.UserPreference?.SmartProjectsOption != null)
		{
			timelineModel = LocalSettings.Settings.UserPreference.SmartProjectsOption.GetTimeline(smartNameByIdentityId);
		}
		TimelineModel timelineModel2 = timelineModel;
		if (timelineModel2 == null)
		{
			if (!(smartNameByIdentityId == "inbox"))
			{
				return new TimelineModel("project");
			}
			timelineModel2 = new TimelineModel("sortOrder");
		}
		return timelineModel2;
	}

	public static void SaveSmartProjectTimeline(string identityId, TimelineModel timeline)
	{
		string smartNameByIdentityId = GetSmartNameByIdentityId(identityId);
		if (!string.IsNullOrEmpty(smartNameByIdentityId))
		{
			LocalSettings.Settings.SaveSmartProjectTimeline(smartNameByIdentityId, timeline);
		}
	}
}
