using System;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Misc;

public class WeekSmartProjectViewModel : SmartProjectViewModel
{
	public WeekSmartProjectViewModel()
	{
		base.Title = Utils.GetString("Next7Day");
		base.Icon = Utils.GetGeometry("Ic.ProjectList." + DateTime.Today.DayOfWeek);
		base.Id = "_special_id_week";
	}
}
