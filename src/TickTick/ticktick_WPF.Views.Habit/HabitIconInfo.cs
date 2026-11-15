namespace ticktick_WPF.Views.Habit;

public class HabitIconInfo
{
	public string Key { get; set; }

	public string Describe { get; set; }

	public string BackColor { get; set; }

	public string TextColor { get; set; }

	public string IconOutlineColor { get; set; }

	public HabitIconInfo(string key, string describe, string backColor, string textColor, string outLineColor)
	{
		Key = key;
		Describe = describe;
		BackColor = backColor;
		TextColor = textColor;
		IconOutlineColor = outLineColor;
	}
}
