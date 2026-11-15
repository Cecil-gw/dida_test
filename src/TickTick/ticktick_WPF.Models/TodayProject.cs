using System;
using System.Windows.Media;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Models;

public class TodayProject : SmartProject
{
	public override string Id => "_special_id_today";

	public override string Name => Utils.GetString("Today");

	public override Geometry Icon => Utils.GetGeometry("Ic.ProjectList.Date" + DateTime.Today.Day);

	public override string IconText => DateTime.Now.Day.ToString();

	public override DateTime? DefaultDate => DateTime.Today;

	public override int SortOrder
	{
		get
		{
			return LocalSettings.Settings.SortOrderOfToday;
		}
		set
		{
			LocalSettings.Settings.SortOrderOfToday = value;
		}
	}

	public override string UserEventId => "today";
}
