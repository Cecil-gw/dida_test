using System;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Misc;

public class TodaySmartProjectViewModel : SmartProjectViewModel
{
	public TodaySmartProjectViewModel()
	{
		base.Title = Utils.GetString("Today");
		base.Icon = Utils.GetGeometry("Ic.ProjectList.Date" + DateTime.Today.Day);
		base.Id = "_special_id_today";
	}
}
