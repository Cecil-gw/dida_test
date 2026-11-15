using System.Collections.Generic;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;

namespace ticktick_WPF.Util.Provider;

public sealed class SubscribeCalendarProjectIdentity : ProjectIdentity
{
	public readonly CalendarSubscribeProfileModel Profile;

	public override string SortProjectId => null;

	public SubscribeCalendarProjectIdentity(CalendarSubscribeProfileModel profile)
	{
		Profile = profile;
		Id = profile.Id;
		base.SortOption = SortOptionUtils.GetSortOptionBySortType(Constants.SortType.dueDate, inKanban: false);
		base.CanDrag = false;
	}

	public override List<string> GetTags()
	{
		return new List<string>();
	}

	public override string GetDisplayTitle()
	{
		return Profile?.Name;
	}
}
