using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Misc;

public class CalendarGroupViewModel : SelectableItemViewModel
{
	public CalendarGroupViewModel()
	{
		base.Id = "#allSubscribe";
		base.Title = Utils.GetString("AllCalendars", "All Calendars");
		base.Selectable = true;
		base.Icon = Utils.GetGeometry("Ic.ProjectList.CalendarSubscribe");
		base.ShowSelected = true;
		base.ShowIndent = true;
		base.IsSectionGroup = true;
		base.Open = true;
		base.IsParent = true;
	}
}
