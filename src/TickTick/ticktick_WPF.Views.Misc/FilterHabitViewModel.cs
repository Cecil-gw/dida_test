using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Misc;

public class FilterHabitViewModel : SelectableItemViewModel
{
	public FilterHabitViewModel()
	{
		base.Id = "Habit2e4c103c57ef480997943206";
		base.Title = Utils.GetString("statistics_habit");
		base.Icon = Utils.GetGeometry("Ic.TaskOM.Habit");
		base.Type = "normal";
	}
}
