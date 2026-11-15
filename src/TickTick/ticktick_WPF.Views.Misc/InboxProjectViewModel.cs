using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Misc;

public class InboxProjectViewModel : ProjectViewModel
{
	public InboxProjectViewModel()
	{
		base.Title = Utils.GetString("Inbox");
		base.Id = Utils.GetInboxId();
		base.Icon = Utils.GetGeometry("Ic.ProjectList.Inbox");
		base.Type = "normal";
	}
}
