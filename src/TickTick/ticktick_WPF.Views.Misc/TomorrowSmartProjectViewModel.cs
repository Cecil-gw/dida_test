using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Misc;

public class TomorrowSmartProjectViewModel : SmartProjectViewModel
{
	public TomorrowSmartProjectViewModel()
	{
		base.Title = Utils.GetString("Tomorrow");
		base.Icon = Utils.GetGeometry("Ic.ProjectList.Tomorrow");
		base.Id = "_special_id_tomorrow";
	}
}
