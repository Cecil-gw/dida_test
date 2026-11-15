using System.Collections.Generic;
using ticktick_WPF.Models;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Util.Provider;

public class SubscribeCalendarListData : SortProjectData
{
	public CalendarSubscribeProfileModel Profile { get; set; }

	public SubscribeCalendarListData(CalendarSubscribeProfileModel profile)
	{
		base.ShowProjectSort = false;
		base.ShowCustomSort = false;
		Profile = profile;
		base.ShowLoadMore = false;
		base.EmptyTitle = Utils.GetString("CalendarA");
		base.EmptyContent = Utils.GetString("CalendarB");
		base.AddTaskHint = string.Empty;
	}

	public override EmptyIconsSourceModel GetEmptyIconsSource(bool allCompleted)
	{
		return new EmptyIconsSourceModel("NormalEmpty", new List<int> { 1, 2, 3, 4, 6 });
	}

	public override string GetTitle()
	{
		return Profile.Name;
	}

	public override bool ShowCalendarExpired()
	{
		return Profile.Expired;
	}
}
