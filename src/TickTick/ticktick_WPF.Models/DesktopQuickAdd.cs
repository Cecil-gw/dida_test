namespace ticktick_WPF.Models;

public class DesktopQuickAdd : PreferenceBaseModel
{
	public int style { get; set; }

	public override bool SetRemoteValue(PreferenceBaseModel model)
	{
		if (model is DesktopQuickAdd desktopQuickAdd)
		{
			if (desktopQuickAdd.mtime > base.mtime)
			{
				style = desktopQuickAdd.style;
				base.mtime = desktopQuickAdd.mtime;
			}
			else if (desktopQuickAdd.mtime < base.mtime)
			{
				return true;
			}
		}
		return false;
	}
}
