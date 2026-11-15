namespace ticktick_WPF.Models;

public class AlternativeCalendar : PreferenceBaseModel
{
	public string calendar { get; set; }

	public override bool SetRemoteValue(PreferenceBaseModel model)
	{
		if (model is AlternativeCalendar alternativeCalendar)
		{
			if (alternativeCalendar.mtime > base.mtime)
			{
				calendar = alternativeCalendar.calendar;
				base.mtime = alternativeCalendar.mtime;
			}
			else if (alternativeCalendar.mtime < base.mtime)
			{
				return true;
			}
		}
		return false;
	}
}
