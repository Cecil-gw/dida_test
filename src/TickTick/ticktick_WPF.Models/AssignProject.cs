using System.Windows.Media;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Models;

public class AssignProject : SmartProject
{
	public override string Id => "_special_id_assigned";

	public override string UserEventId => "assign_to_me";

	public override string Name => Utils.GetString("AssignToMe");

	public override Geometry Icon => Utils.GetGeometry("Ic.ProjectList.AssignToMe");

	public override bool CanDrop => false;

	public override int SortOrder
	{
		get
		{
			return LocalSettings.Settings.SortOrderOfAssign;
		}
		set
		{
			LocalSettings.Settings.SortOrderOfAssign = value;
		}
	}
}
