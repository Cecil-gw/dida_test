using System.Collections.Generic;

namespace ticktick_WPF.Models;

public class RecentlyColors : PreferenceBaseModel
{
	public List<string> colors { get; set; }

	public override bool SetRemoteValue(PreferenceBaseModel model)
	{
		if (model is RecentlyColors recentlyColors)
		{
			if (recentlyColors.mtime > base.mtime)
			{
				colors = recentlyColors.colors;
				base.mtime = recentlyColors.mtime;
			}
			else if (recentlyColors.mtime < base.mtime)
			{
				return true;
			}
		}
		return false;
	}
}
