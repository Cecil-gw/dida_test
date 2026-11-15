namespace ticktick_WPF.Models;

public class TeamConfig : PreferenceBaseModel
{
	public bool autoAcceptInvite { get; set; } = true;

	public override bool SetRemoteValue(PreferenceBaseModel model)
	{
		if (model is TeamConfig teamConfig)
		{
			if (teamConfig.mtime > base.mtime)
			{
				autoAcceptInvite = teamConfig.autoAcceptInvite;
				base.mtime = teamConfig.mtime;
			}
			else if (teamConfig.mtime < base.mtime)
			{
				return true;
			}
		}
		return false;
	}
}
