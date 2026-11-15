namespace ticktick_WPF.Models;

public class CountdownConfig : PreferenceBaseModel
{
	public bool showSection { get; set; }

	public override bool SetRemoteValue(PreferenceBaseModel model)
	{
		if (model is CountdownConfig countdownConfig)
		{
			if (countdownConfig.mtime > base.mtime)
			{
				showSection = countdownConfig.showSection;
				base.mtime = countdownConfig.mtime;
			}
			else if (countdownConfig.mtime < base.mtime)
			{
				return true;
			}
		}
		return false;
	}
}
