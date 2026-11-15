using System;
using System.Windows.Media;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Models;

public class TomorrowProject : SmartProject
{
	public override string Id => "_special_id_tomorrow";

	public override string Name => Utils.GetString("Tomorrow");

	public override string UserEventId => "tomorrow";

	public override Geometry Icon => Utils.GetGeometry("Ic.ProjectList.Tomorrow");

	public override DateTime? DefaultDate => DateTime.Today.AddDays(1.0);

	public override int SortOrder
	{
		get
		{
			return LocalSettings.Settings.SortOrderOfTomorrow;
		}
		set
		{
			LocalSettings.Settings.SortOrderOfTomorrow = value;
		}
	}
}
