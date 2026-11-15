namespace ticktick_WPF.Models;

public class HabitConfig : PreferenceBaseModel
{
	public string style { get; set; }

	public override bool SetRemoteValue(PreferenceBaseModel model)
	{
		if (model is HabitConfig habitConfig)
		{
			if (habitConfig.mtime > base.mtime)
			{
				style = habitConfig.style;
				base.mtime = habitConfig.mtime;
			}
			else if (habitConfig.mtime < base.mtime)
			{
				return true;
			}
		}
		return false;
	}
}
