using System.Windows.Media;
using ticktick_WPF.Util;

namespace ticktick_WPF.Models;

public class TrashProject : SmartProject
{
	public override string Id => "_special_id_trash";

	public override string UserEventId => "trash";

	public override string Name => Utils.GetString("Trash");

	public override Geometry Icon => Utils.GetGeometry("Ic.ProjectList.Delete");

	public override bool IsDeleted => true;

	public override int SortOrder => 2147483638;
}
