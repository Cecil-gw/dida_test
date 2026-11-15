using System.Collections.Generic;

namespace ticktick_WPF.Models;

public class DesktopConfigModel : PreferenceBaseModel
{
	public List<string> tabs { get; set; } = new List<string>();

	public override bool SetRemoteValue(PreferenceBaseModel remote)
	{
		if (remote is DesktopConfigModel desktopConfigModel)
		{
			if (desktopConfigModel.mtime > base.mtime)
			{
				tabs = desktopConfigModel.tabs;
				base.mtime = desktopConfigModel.mtime;
			}
			if (remote.mtime < base.mtime)
			{
				return true;
			}
		}
		return false;
	}
}
