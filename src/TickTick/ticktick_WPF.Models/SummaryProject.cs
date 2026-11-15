using System.Windows.Media;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Models;

public class SummaryProject : SmartProject
{
	public override string Id => "_special_id_summary";

	public override string UserEventId => "summary";

	public override string Name => Utils.GetString("Summary");

	public override Geometry Icon => Utils.GetGeometry("Ic.ProjectList.Summary");

	public override bool CanDrop => false;

	public override int SortOrder
	{
		get
		{
			return LocalSettings.Settings.SortOrderOfSummary;
		}
		set
		{
			LocalSettings.Settings.SortOrderOfSummary = value;
		}
	}
}
