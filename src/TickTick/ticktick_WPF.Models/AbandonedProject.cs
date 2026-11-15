using System.Windows.Media;
using ticktick_WPF.Util;

namespace ticktick_WPF.Models;

public class AbandonedProject : SmartProject
{
	public override string Id => "_special_id_abandoned";

	public override string UserEventId => "abandoned";

	public override string Name => Utils.GetString("Abandoned");

	public override Geometry Icon => Utils.GetGeometry("Ic.ProjectList.Abandon");

	public override bool IsCompleted => true;

	public override int SortOrder => 2147483637;
}
