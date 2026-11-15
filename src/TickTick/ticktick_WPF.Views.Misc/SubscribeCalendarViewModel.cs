using System.Windows.Media;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Misc;

public class SubscribeCalendarViewModel : SelectableItemViewModel
{
	public readonly string AccountId;

	public SubscribeCalendarViewModel(string id, string title, string accountId = "")
	{
		base.Id = id;
		base.Title = title;
		Geometry calendarProjectIconById = SubscribeCalendarHelper.GetCalendarProjectIconById(accountId);
		base.Icon = calendarProjectIconById;
		base.Type = "subscribe_calendar";
		AccountId = accountId;
		base.ParentId = accountId;
	}
}
