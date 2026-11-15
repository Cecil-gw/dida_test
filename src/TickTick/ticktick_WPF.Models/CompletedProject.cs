using System.Windows.Media;
using ticktick_WPF.Util;

namespace ticktick_WPF.Models;

public class CompletedProject : SmartProject
{
	public override string Id => "_special_id_completed";

	public override string UserEventId => "completed";

	public override string Name => Utils.GetString("Completed");

	public override Geometry Icon => Utils.GetGeometry("Ic.ProjectList.Completed");

	public override bool IsCompleted => true;

	public override int SortOrder => 2147483636;
}
