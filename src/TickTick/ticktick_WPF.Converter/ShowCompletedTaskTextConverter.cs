namespace ticktick_WPF.Converter;

public class ShowCompletedTaskTextConverter : BoolTextConverter
{
	public ShowCompletedTaskTextConverter()
		: base("HideCompleted", "ShowCompleted", "HideCompleted")
	{
	}
}
