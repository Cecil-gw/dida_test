using System;
using System.Windows.Media;
using ticktick_WPF.Resource;

namespace ticktick_WPF.Util;

public static class SpecialListUtils
{
	public const string SpecialListId = "_special_id_";

	public const string SpecialListTodaySid = "_special_id_today";

	public const string SpecialListTomorrowSid = "_special_id_tomorrow";

	public const string SpecialListWeekSid = "_special_id_week";

	public const string SpecialListTagSid = "_special_id_tag";

	public const string SpecialListEventSid = "_special_id_event";

	public const string SpecialListAssignedSid = "_special_id_assigned";

	public const string SpecialListAllSid = "_special_id_all";

	public const string SpecialListCompletedSid = "_special_id_completed";

	public const string SpecialListAbandonedSid = "_special_id_abandoned";

	public const string SpecialListTrashSid = "_special_id_trash";

	public const string SpecialListClosedGroupSid = "_special_id_closed";

	public const string SpecialListGroupAllSid = "_special_id_group_all";

	public const string SpecialListSearchSid = "_special_id_search";

	public const string SpecialListSummarySid = "_special_id_summary";

	public const string SpecialListDate = "_special_id_date";

	public const string TodaySid = "today";

	public const string TomorrowSid = "tomorrow";

	public const string WeekSid = "week";

	public const string AssignedSid = "assigned";

	public const string TagSid = "tag";

	public const string EventSid = "event";

	public const string AllSid = "all";

	public const string CompletedSid = "completed";

	public const string AbandonedSid = "abandoned";

	public const string TrashSid = "trash";

	public const string ClosedGroupSid = "closed";

	public const string GroupAllSid = "group_all";

	public const string SearchSid = "search";

	public const string SummarySid = "summary";

	public const string DateSid = "date";

	public static bool IsDefaultSmartList(string id)
	{
		if (!IsAllProject(id) && !IsTodayProject(id) && !IsTomorrowProject(id) && !IsWeekProject(id) && !IsAssignToMeProject(id) && !IsCompleteProject(id))
		{
			return IsTrashProject(id);
		}
		return true;
	}

	public static bool IsSmartProject(string id)
	{
		return id.Contains("_special_id_");
	}

	public static bool IsAllProject(string id)
	{
		return "_special_id_all" == id;
	}

	public static bool IsTodayProject(string id)
	{
		return "_special_id_today" == id;
	}

	private static bool IsTomorrowProject(string id)
	{
		return "_special_id_tomorrow" == id;
	}

	private static bool IsWeekProject(string id)
	{
		return "_special_id_week" == id;
	}

	public static bool IsAssignToMeProject(string id)
	{
		return "_special_id_assigned" == id;
	}

	public static bool IsAbandonedProject(string id)
	{
		return "_special_id_abandoned" == id;
	}

	public static bool IsCompleteProject(string id)
	{
		return "_special_id_completed" == id;
	}

	public static bool IsTrashProject(string id)
	{
		return "_special_id_trash" == id;
	}

	public static bool IsSummaryProject(string sid)
	{
		return sid == "_special_id_summary";
	}

	public static bool IsInboxProject(string itemId)
	{
		return itemId == Utils.GetInboxId();
	}

	public static bool IsTargetSmartList(SmartListType smartType, string itemId)
	{
		return smartType switch
		{
			SmartListType.All => IsAllProject(itemId), 
			SmartListType.Today => IsTodayProject(itemId), 
			SmartListType.Tomorrow => IsTomorrowProject(itemId), 
			SmartListType.Week => IsWeekProject(itemId), 
			SmartListType.Assign => IsAssignToMeProject(itemId), 
			SmartListType.Completed => IsCompleteProject(itemId), 
			SmartListType.Trash => IsTrashProject(itemId), 
			SmartListType.Summary => IsSummaryProject(itemId), 
			SmartListType.Inbox => IsInboxProject(itemId), 
			SmartListType.Abandoned => IsAbandonedProject(itemId), 
			_ => false, 
		};
	}

	public static SmartListType GetSmartTypeById(string id)
	{
		return id switch
		{
			"_special_id_all" => SmartListType.All, 
			"_special_id_today" => SmartListType.Today, 
			"_special_id_tomorrow" => SmartListType.Tomorrow, 
			"_special_id_week" => SmartListType.Week, 
			"_special_id_assigned" => SmartListType.Assign, 
			"_special_id_completed" => SmartListType.Completed, 
			"_special_id_abandoned" => SmartListType.Abandoned, 
			"_special_id_trash" => SmartListType.Trash, 
			"_special_id_summary" => SmartListType.Summary, 
			_ => SmartListType.Inbox, 
		};
	}

	public static Geometry GetIconBySmartType(SmartListType type)
	{
		return type switch
		{
			SmartListType.All => Utils.GetGeometry("Ic.ProjectList.All"), 
			SmartListType.Today => Utils.GetGeometry("Ic.ProjectList.Date" + DateTime.Today.Day), 
			SmartListType.Tomorrow => Utils.GetGeometry("Ic.ProjectList.Tomorrow"), 
			SmartListType.Week => Utils.GetGeometry("Ic.ProjectList." + DateTime.Today.DayOfWeek), 
			SmartListType.Assign => Utils.GetGeometry("Ic.ProjectList.AssignToMe"), 
			SmartListType.Completed => Utils.GetGeometry("Ic.ProjectList.Completed"), 
			SmartListType.Trash => Utils.GetGeometry("Ic.ProjectList.Delete"), 
			SmartListType.Summary => Utils.GetGeometry("Ic.ProjectList.Summary"), 
			SmartListType.Inbox => Utils.GetGeometry("Ic.ProjectList.Inbox"), 
			SmartListType.Abandoned => Utils.GetGeometry("Ic.ProjectList.Abandon"), 
			SmartListType.Tag => Utils.GetGeometry("Ic.ProjectList.Tag"), 
			SmartListType.Filter => Utils.GetGeometry("Ic.ProjectList.Filter"), 
			_ => null, 
		};
	}
}
