using System.Collections.Generic;

namespace ticktick_WPF.Models;

public class DesktopTabBar : PreferenceBaseModel
{
	public List<TabBarModel> bars { get; set; }

	public override bool SetRemoteValue(PreferenceBaseModel remote)
	{
		if (remote is DesktopTabBar desktopTabBar)
		{
			if (desktopTabBar.mtime > base.mtime)
			{
				bars = desktopTabBar.bars;
				base.mtime = desktopTabBar.mtime;
			}
			if (remote.mtime < base.mtime)
			{
				return true;
			}
		}
		return false;
	}
}
