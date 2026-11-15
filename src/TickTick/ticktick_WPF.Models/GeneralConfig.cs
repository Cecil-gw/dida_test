using System.Collections.Generic;

namespace ticktick_WPF.Models;

public class GeneralConfig : PreferenceBaseModel
{
	public bool urlParseEnabled { get; set; } = true;

	public bool emailRemindEnabled { get; set; }

	public List<string> emailRemindItems { get; set; } = new List<string> { "task" };

	public override bool SetRemoteValue(PreferenceBaseModel model)
	{
		if (model is GeneralConfig generalConfig)
		{
			if (generalConfig.mtime > base.mtime)
			{
				urlParseEnabled = generalConfig.urlParseEnabled;
				emailRemindEnabled = generalConfig.emailRemindEnabled;
				emailRemindItems = generalConfig.emailRemindItems;
				base.mtime = generalConfig.mtime;
			}
			else if (generalConfig.mtime < base.mtime)
			{
				return true;
			}
		}
		return false;
	}
}
