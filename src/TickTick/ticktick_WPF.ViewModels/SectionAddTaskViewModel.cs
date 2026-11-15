namespace ticktick_WPF.ViewModels;

public class SectionAddTaskViewModel
{
	public string Text;

	public string SectionId;

	public string ColumnId;

	public bool Invalid;

	public void SetInvalid()
	{
		Invalid = true;
	}
}
