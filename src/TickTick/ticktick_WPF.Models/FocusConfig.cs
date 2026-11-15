namespace ticktick_WPF.Models;

public class FocusConfig : PreferenceBaseModel
{
	public bool keepInSync { get; set; } = true;

	public override bool SetRemoteValue(PreferenceBaseModel model)
	{
		if (model is FocusConfig focusConfig)
		{
			if (focusConfig.mtime > base.mtime)
			{
				keepInSync = focusConfig.keepInSync;
				base.mtime = focusConfig.mtime;
			}
			else if (focusConfig.mtime < base.mtime)
			{
				return true;
			}
		}
		return false;
	}
}
