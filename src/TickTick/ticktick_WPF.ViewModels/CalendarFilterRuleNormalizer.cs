using System.Collections.Generic;
using System.Linq;
using ticktick_WPF.Cache;
using ticktick_WPF.Models;
using ticktick_WPF.Util;

namespace ticktick_WPF.ViewModels;

internal static class CalendarFilterRuleNormalizer
{
	public static void MoveAccountIdsFromCalendars(List<string> calendarAccounts, List<string> calendars)
	{
		if (calendarAccounts == null || calendars == null || calendars.Count == 0)
		{
			return;
		}
		List<string> list = (from account in CacheManager.GetBindCalendarAccounts()
			select account.Id into id
			where !string.IsNullOrEmpty(id)
			select id).ToList();
		if (!list.Any())
		{
			return;
		}
		foreach (string calendarId in calendars.ToList())
		{
			if (!(calendarId == "#allSubscribe") && CalendarUtils.ContainsAccountId(list, calendarId))
			{
				calendars.Remove(calendarId);
				if (!calendarAccounts.Any((string id) => CalendarUtils.IsSameAccountId(id, calendarId)))
				{
					calendarAccounts.Add(calendarId);
				}
			}
		}
	}
}
