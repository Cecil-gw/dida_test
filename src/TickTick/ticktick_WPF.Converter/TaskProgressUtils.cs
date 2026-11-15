namespace ticktick_WPF.Converter;

internal class TaskProgressUtils
{
	public static double GetPercentWidth(int percent, double width)
	{
		if (percent < 0 || percent > 100)
		{
			return 0.0;
		}
		return width * (double)((float)percent / 100f);
	}
}
