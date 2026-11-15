using System.Windows.Media;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Models;

public class InboxProject : SmartProject
{
	public override string Id => Utils.GetInboxId();

	public override string Name => Utils.GetString("Inbox");

	public override Geometry Icon => Utils.GetGeometry("Ic.ProjectList.Inbox");

	public override string ProjectId => Id;

	public override int SortOrder
	{
		get
		{
			return LocalSettings.Settings.SortOrderOfInbox;
		}
		set
		{
			LocalSettings.Settings.SortOrderOfInbox = value;
		}
	}
}
