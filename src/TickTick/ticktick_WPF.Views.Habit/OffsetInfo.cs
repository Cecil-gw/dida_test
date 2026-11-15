namespace ticktick_WPF.Views.Habit;

public class OffsetInfo
{
	public string Title { get; set; }

	public double UnitHeight { get; set; }

	public OffsetInfo(double offset, double unitHeight)
	{
		Title = offset.ToString("F0");
		UnitHeight = unitHeight;
	}
}
