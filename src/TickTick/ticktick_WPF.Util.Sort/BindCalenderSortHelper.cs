using System.Collections.Generic;
using System.Linq;
using ticktick_WPF.Cache;
using ticktick_WPF.Models;

namespace ticktick_WPF.Util.Sort;

internal class BindCalenderSortHelper
{
	public static long? GetNewTopOrder()
	{
		List<CalendarSubscribeProfileModel> subscribeCalendars = CacheManager.GetSubscribeCalendars();
		List<BindCalendarAccountModel> bindCalendarAccounts = CacheManager.GetBindCalendarAccounts();
		if (subscribeCalendars.Count + bindCalendarAccounts.Count > 0)
		{
			if (!subscribeCalendars.Any((CalendarSubscribeProfileModel x) => x.Order.HasValue) && !bindCalendarAccounts.Any((BindCalendarAccountModel x) => x.Order.HasValue))
			{
				return null;
			}
			long num = (from sort in subscribeCalendars.Select((CalendarSubscribeProfileModel sub) => sub.Order.GetValueOrDefault()).Union(bindCalendarAccounts.Select((BindCalendarAccountModel acc) => acc.Order.GetValueOrDefault()))
				orderby sort
				select sort).ToList().Min() - 268435456;
			if (num == 0L)
			{
				num = -1L;
			}
			return num;
		}
		return null;
	}
}
