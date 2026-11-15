namespace ticktick_WPF.Converter;

public class ShowSubtasksTextConverter : BoolTextConverter
{
	public ShowSubtasksTextConverter()
		: base("HideSubtask", "ShowSubtask", "HideSubtask")
	{
	}
}
