using System.Windows.Media;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Misc;

public class BindAccountViewModel : SelectableItemViewModel
{
	public BindAccountViewModel(string id, string title, string kind)
	{
		base.Id = id;
		Geometry calendarProjectIconById = SubscribeCalendarHelper.GetCalendarProjectIconById(id);
		base.Icon = calendarProjectIconById;
		base.Title = title;
		base.Type = "bind_account";
		base.Selectable = true;
		base.CanMultiSelect = true;
	}
}
