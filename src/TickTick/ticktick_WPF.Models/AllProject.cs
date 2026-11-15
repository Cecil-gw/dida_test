using System.Windows.Media;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Models;

public class AllProject : SmartProject
{
	public override string Id => "_special_id_all";

	public override string Name => Utils.GetString("All");

	public override Geometry Icon => Utils.GetGeometry("Ic.ProjectList.All");

	public override string UserEventId => "all";

	public override int SortOrder
	{
		get
		{
			return LocalSettings.Settings.SortOrderOfAll;
		}
		set
		{
			LocalSettings.Settings.SortOrderOfAll = value;
		}
	}
}
