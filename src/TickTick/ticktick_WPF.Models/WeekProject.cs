using System;
using System.Windows.Media;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Models;

public class WeekProject : SmartProject
{
	public override string Id => "_special_id_week";

	public override string UserEventId => "n7d";

	public override string Name => Utils.GetString("Next7Day");

	public override Geometry Icon => Utils.GetGeometry("Ic.ProjectList." + DateTime.Today.DayOfWeek);

	public override string IconText => DateTime.Now.DayOfWeek.ToString().Substring(0, 2);

	public override DateTime? DefaultDate => DateTime.Today;

	public override int SortOrder
	{
		get
		{
			return LocalSettings.Settings.SortOrderOfWeek;
		}
		set
		{
			LocalSettings.Settings.SortOrderOfWeek = value;
		}
	}
}
