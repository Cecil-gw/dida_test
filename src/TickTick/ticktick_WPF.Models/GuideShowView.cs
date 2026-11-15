namespace ticktick_WPF.Models;

public class GuideShowView : PreferenceBaseModel
{
	public bool showGuide { get; set; } = true;

	public override bool SetRemoteValue(PreferenceBaseModel model)
	{
		if (model is GuideShowView guideShowView)
		{
			if (guideShowView.mtime > base.mtime)
			{
				showGuide = guideShowView.showGuide;
				base.mtime = guideShowView.mtime;
			}
			else if (guideShowView.mtime < base.mtime)
			{
				return true;
			}
		}
		return false;
	}
}
