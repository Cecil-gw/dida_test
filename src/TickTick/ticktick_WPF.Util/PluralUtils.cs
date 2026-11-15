namespace ticktick_WPF.Util;

public class PluralUtils
{
	public static string GetDayText(int days)
	{
		return days + " " + Utils.GetString((days > 1) ? "PublicDays" : "PublicDay");
	}
}
