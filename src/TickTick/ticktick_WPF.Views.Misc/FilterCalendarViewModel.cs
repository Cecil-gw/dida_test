using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Misc;

public class FilterCalendarViewModel : SelectableItemViewModel
{
	public FilterCalendarViewModel()
	{
		base.Id = "Calendar5959a2259161d16d23a4f272";
		base.Title = Utils.GetString("Calendar");
		base.Icon = Utils.GetGeometry("Ic.TaskOM.DateRemind");
		base.Type = "normal";
	}
}
